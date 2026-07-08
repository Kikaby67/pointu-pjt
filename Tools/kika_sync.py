#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Kika-sync (kika_sync.py) — Synchronise des fichiers .cs locaux vers les
sub-actions "Execute C# Code" de Streamer.bot (data/actions.json).

Mapping fichier .cs <-> sub-action :
  En-tete du .cs (commentaires C#, dans les ~40 premieres lignes) :
      // sb-action: !racine            <- nom de l'action parente (tu l'ecris)
      // sb-subaction-id: <guid>       <- rempli automatiquement au 1er run

  Resolution :
    1. Si sb-subaction-id present -> cible directement cette sub-action (rename-proof).
    2. Sinon, resolution par nom d'action -> la sub-action C# (type 99999).
       - exactement 1 sub-action C# -> ciblee + GUID auto-stamp dans le .cs
       - 0 sub-action C# + CREATE_IF_MISSING -> creation d'une nouvelle sub-action
       - plusieurs -> ambigu : log des candidats, fichier ignore (colle un GUID)

Le code est stocke dans le champ byteCode = base64(source UTF-8) (pas de gzip).

Reload apres patch :
  Mode A : kill (force) de Streamer.bot -> patch -> relaunch. 100% fiable.
  Mode B : patch seulement, SB reste ouvert. Le code N'EST PAS applique tant que
           tu ne relances pas SB, et il y a un risque d'ecrasement si SB sauvegarde.

Usage :
  python kika_sync.py                      # watch, mode par defaut (voir RELOAD_MODE)
  python kika_sync.py --reload A           # watch + restart auto de SB a chaque patch
  python kika_sync.py --once               # synchronise TOUT le dossier VS Code (tous les .cs)
  python kika_sync.py --last               # synchronise le .cs modifie le plus recemment
  python kika_sync.py --check-duplicates   # scanne actions.json et signale les doublons de sous-actions C#
  python kika_sync.py --file chemin.cs     # patch un seul fichier puis quitte
  python kika_sync.py --discover           # propose un mapping par similarite de code
  python kika_sync.py --discover --write   # + insere les en-tetes pour les matchs surs
  python kika_sync.py --lock               # (mode B) passe actions.json en lecture seule apres patch

Dependance : pip install watchdog   (uniquement pour le mode watch)
"""

import argparse
import base64
import datetime
import difflib
import json
import os
import re
import shutil
import stat
import subprocess
import sys
import tempfile
import threading
import time

# ============================ CONFIGURATION ============================
_HERE = os.path.dirname(os.path.abspath(__file__))

# Chemins machine-specifiques : ces valeurs sont des EXEMPLES generiques.
# Mets tes vrais chemins dans "kika_sync.local.json" (non versionne, cf. .gitignore)
# pour ne pas exposer tes chemins d'acces sur GitHub. Voir kika_sync.local.example.json.
ACTIONS_JSON      = r"C:\Chemin\Vers\Streamer.bot\data\actions.json"
SB_EXE            = r"C:\Chemin\Vers\Streamer.bot\Streamer.bot.exe"
# Par defaut : le dossier Streamerbot du repo, deduit de l'emplacement de ce script.
WATCH_DIR         = os.path.join(os.path.dirname(_HERE), "Streamerbot")

RELOAD_MODE       = "A"          # "A" = kill+relaunch SB (RECOMMANDE) | "B" = patch seul (SB doit etre FERME)
CREATE_IF_MISSING = True         # creer une sub-action C# si l'action n'en a pas
LOCK_AFTER_PATCH  = False        # mode B : passer actions.json en lecture seule apres patch

CSHARP_TYPE       = 99999        # type des sub-actions "Execute C# Code"
DEFAULT_REFERENCES = [r"C:\Windows\Microsoft.NET\Framework64\v4.0.30319\mscorlib.dll"]

DEBOUNCE_SECONDS  = 0.6          # regroupe les sauvegardes rapprochees
KEEP_BACKUPS      = 50           # nombre de backups horodates conserves
DISCOVER_THRESHOLD = 0.97        # similarite mini pour un auto-stamp en --discover --write

# Surcharge locale (non versionnee) : {"actions_json":"...","sb_exe":"...","watch_dir":"...","reload_mode":"A"}
_local = os.path.join(_HERE, "kika_sync.local.json")
if os.path.exists(_local):
    try:
        _c = json.load(open(_local, encoding="utf-8"))
        ACTIONS_JSON = _c.get("actions_json", ACTIONS_JSON)
        SB_EXE       = _c.get("sb_exe",       SB_EXE)
        WATCH_DIR    = _c.get("watch_dir",    WATCH_DIR)
        RELOAD_MODE  = _c.get("reload_mode",  RELOAD_MODE)
    except (OSError, ValueError):
        pass

BACKUP_DIR = os.path.join(os.path.dirname(ACTIONS_JSON), "_kikasync_backups")
LOG_FILE   = os.path.join(_HERE, "kika_sync.log")

# Derives : logs SB (…\Streamer.bot\logs) et racine du repo (parent de Streamerbot\)
SB_LOGS_DIR = os.path.join(os.path.dirname(os.path.dirname(ACTIONS_JSON)), "logs")
REPO_ROOT   = os.path.dirname(WATCH_DIR)

DO_LINT   = True     # pre-verification C# (bloque un fichier invalide) ; --no-lint pour couper
DO_VERIFY = False    # verif post-relance (scan log SB) apres un reload Mode A ; --verify
# ======================================================================

# Console UTF-8 (evite le mojibake des accents/emoji sous Windows)
for _stream in (sys.stdout, sys.stderr):
    try:
        _stream.reconfigure(encoding="utf-8")
    except (AttributeError, ValueError):
        pass

RE_ACTION = re.compile(r"^\s*//\s*sb-action:\s*(.+?)\s*$", re.MULTILINE)
RE_SUBID  = re.compile(r"^\s*//\s*sb-subaction-id:\s*([0-9a-fA-F-]{36})\s*$", re.MULTILINE)


def log(msg, level="INFO"):
    line = f"{datetime.datetime.now():%Y-%m-%d %H:%M:%S} [{level}] {msg}"
    print(line, flush=True)
    try:
        with open(LOG_FILE, "a", encoding="utf-8") as f:
            f.write(line + "\n")
    except OSError:
        pass


# ------------------------- actions.json I/O -------------------------
def read_actions():
    with open(ACTIONS_JSON, "rb") as f:
        raw = f.read()
    had_bom = raw[:3] == b"\xef\xbb\xbf"
    obj = json.loads(raw.decode("utf-8-sig"))
    return obj, had_bom


def backup_actions():
    os.makedirs(BACKUP_DIR, exist_ok=True)
    ts = datetime.datetime.now().strftime("%Y%m%d_%H%M%S_%f")
    dst = os.path.join(BACKUP_DIR, f"actions_{ts}.json")
    shutil.copy2(ACTIONS_JSON, dst)
    # rotation
    backups = sorted(
        (os.path.join(BACKUP_DIR, n) for n in os.listdir(BACKUP_DIR)
         if n.startswith("actions_") and n.endswith(".json")),
        key=os.path.getmtime,
    )
    for old in backups[:-KEEP_BACKUPS]:
        try:
            os.remove(old)
        except OSError:
            pass
    log(f"Backup cree : {dst}")
    return dst


def _clear_readonly(path):
    if os.path.exists(path):
        os.chmod(path, stat.S_IWRITE | stat.S_IREAD)


def write_actions(obj, had_bom, lock=False):
    _clear_readonly(ACTIONS_JSON)
    body = json.dumps(obj, ensure_ascii=False, separators=(",", ":"))
    data = ("﻿" if had_bom else "") + body
    tmp = ACTIONS_JSON + ".sbsync.tmp"
    for attempt in range(5):
        try:
            with open(tmp, "w", encoding="utf-8", newline="") as f:
                f.write(data)
            os.replace(tmp, ACTIONS_JSON)
            break
        except PermissionError:
            if attempt == 4:
                raise
            time.sleep(0.4)
    if lock:
        os.chmod(ACTIONS_JSON, stat.S_IREAD)
        log("actions.json passe en LECTURE SEULE (garde anti-ecrasement). "
            "Il sera reautorise au prochain patch.", "WARN")


# ------------------------- helpers mapping -------------------------
def parse_header(text):
    a = RE_ACTION.search(text)
    s = RE_SUBID.search(text)
    return (a.group(1).strip() if a else None,
            s.group(1).strip() if s else None)


def stamp_action_and_id(path, text, action_name, guid):
    new = apply_stamp(text, action_name, guid)
    if new != text:
        with open(path, "w", encoding="utf-8", newline="") as f:
            f.write(new)


def apply_stamp(text, action_name, guid):
    """Retourne le texte avec les en-tetes // sb-action + // sb-subaction-id
    (idempotent). Sert AVANT encodage pour que byteCode == contenu disque."""
    if RE_SUBID.search(text):
        return RE_SUBID.sub(f"// sb-subaction-id: {guid}", text, count=1)
    if RE_ACTION.search(text):
        m = RE_ACTION.search(text)
        return text[:m.end()] + f"\n// sb-subaction-id: {guid}" + text[m.end():]
    return f"// sb-action: {action_name}\n// sb-subaction-id: {guid}\n" + text


def find_subaction_by_id(obj, guid):
    for a in obj["actions"]:
        for sa in a.get("subActions", []):
            if sa.get("id") == guid:
                return a, sa
    return None, None


def resolve_action_by_name(obj, name):
    hits = [a for a in obj["actions"] if a.get("name") == name]
    return hits


def csharp_subactions(action):
    return [sa for sa in action.get("subActions", []) if sa.get("type") == CSHARP_TYPE]


def new_csharp_subaction(action, bytecode_b64):
    idx = len(action.get("subActions", []))
    return {
        "name": None,
        "description": None,
        "references": list(DEFAULT_REFERENCES),
        "byteCode": bytecode_b64,
        "precompile": False,
        "delayStart": False,
        "saveResultToVariable": False,
        "saveToVariable": None,
        "id": _new_guid(),
        "weight": 0.0,
        "type": CSHARP_TYPE,
        "parentId": None,
        "enabled": True,
        "index": idx,
    }


def _new_guid():
    import uuid
    return str(uuid.uuid4())


def encode_bytecode(src_text):
    return base64.b64encode(src_text.encode("utf-8")).decode("ascii")


# ------------------------- coeur du patch -------------------------
def resolve_target(obj, path, text, safe=False):
    """
    Retourne (action, subaction, need_stamp_guid) ou (None, None, None) si a ignorer.
    safe=True (SB ferme, ou mode A qui l'a tue) autorise la creation/recreation sans risque.
    """
    action_name, sub_id = parse_header(text)

    if sub_id:
        a, sa = find_subaction_by_id(obj, sub_id)
        if sa is not None:
            return a, sa, False
        log(f"  sb-subaction-id {sub_id} introuvable dans actions.json "
            f"(supprimee ?). Tentative de resolution par nom d'action.", "WARN")

    if not action_name:
        log(f"  IGNORE {os.path.basename(path)} : aucun en-tete de mapping. "
            f"Pour CREER ou mettre a jour une sous-action C#, ajoute en tete du .cs : "
            f"'// sb-action: <NomExactDeLActionDansSB>'. "
            f"L'action + son trigger doivent deja exister dans SB ; la sous-action C#, "
            f"elle, sera creee automatiquement si absente.", "WARN")
        return None, None, None

    actions = resolve_action_by_name(obj, action_name)
    if not actions:
        log(f"  IGNORE : aucune action nommee '{action_name}' dans SB. "
            f"Cree d'abord l'ACTION + son trigger (ex: Command '!duel') dans Streamer.bot, "
            f"puis re-sauvegarde : la sous-action C# sera creee toute seule. "
            f"(Creer l'action+trigger automatiquement n'est pas pris en charge : "
            f"les commandes sont enregistrees separement dans commands.db.)", "WARN")
        return None, None, None
    if len(actions) > 1:
        log(f"  IGNORE : {len(actions)} actions nommees '{action_name}'. "
            f"Ajoute un // sb-subaction-id pour lever l'ambiguite.", "WARN")
        return None, None, None

    action = actions[0]
    had_stamp = bool(sub_id)
    cs = csharp_subactions(action)
    if len(cs) == 1:
        # SB a pu re-attribuer l'id au redemarrage : on se recale dessus (pas de doublon)
        return action, cs[0], True
    if len(cs) == 0:
        if had_stamp and not safe:
            # id stampe mais sous-action disparue ALORS QUE SB TOURNE (mode B) :
            # recreer maintenant partirait en boucle de doublons a chaque redemarrage de SB.
            log(f"  IGNORE : la sous-action C# de '{action_name}' a DISPARU de "
                f"actions.json et Streamer.bot tourne encore — il la reecrasera. "
                f"Ferme SB (ou passe en Mode A) et re-sauvegarde : elle sera recreee.", "WARN")
            return None, None, None
        if CREATE_IF_MISSING:
            if not safe:
                log(f"  ATTENTION : SB tourne — la sous-action creee dans '{action_name}' "
                    f"sera perdue au prochain redemarrage de SB. Utilise le Mode A pour "
                    f"un resultat durable.", "WARN")
            sa = new_csharp_subaction(action, encode_bytecode(text))
            action.setdefault("subActions", []).append(sa)
            log(f"  CREATION d'une sub-action C# dans '{action_name}' "
                f"(id {sa['id']}).", "INFO")
            return action, sa, True
        log(f"  IGNORE : '{action_name}' n'a pas de sub-action C# et "
            f"CREATE_IF_MISSING=False.", "WARN")
        return None, None, None
    # plusieurs sub-actions C#
    log(f"  AMBIGU : '{action_name}' contient {len(cs)} sub-actions C#. "
        f"Colle le bon // sb-subaction-id parmi :", "WARN")
    for sa in cs:
        first = base64.b64decode(sa["byteCode"]).decode("utf-8", "replace").splitlines()
        first = first[0] if first else ""
        log(f"      - {sa['id']}  ({first[:60]})")
    return None, None, None


def patch_files(paths, reload_mode, lock):
    """Charge actions.json une fois, patche tous les fichiers, ecrit une fois, reload."""
    paths = [p for p in paths if p.lower().endswith(".cs") and os.path.isfile(p)]
    if not paths:
        return

    if not os.path.exists(ACTIONS_JSON):
        log(f"actions.json introuvable : {ACTIONS_JSON}", "ERROR")
        return

    # Mode A : on tue SB AVANT d'ecrire pour qu'il ne puisse pas ecraser le fichier.
    killed = False
    if reload_mode == "A":
        killed = kill_streamerbot()

    try:
        obj, had_bom = read_actions()
    except (OSError, json.JSONDecodeError) as e:
        log(f"Lecture actions.json impossible : {e}", "ERROR")
        return

    # Sûr de créer/recréer si SB n'est pas en cours (mode A l'a déjà tué juste avant).
    safe = not sb_is_running()

    changed = 0
    stamps = []   # (path, text, guid)
    for path in paths:
        try:
            with open(path, "r", encoding="utf-8-sig") as f:
                text = f.read()
        except OSError as e:
            log(f"  Lecture .cs impossible {path} : {e}", "ERROR")
            continue

        # Pré-vol : un .cs invalide (accolades, etc.) casse la compilation de TOUT
        # SB au démarrage → on le bloque plutôt que de le pousser.
        if DO_LINT:
            ok, msg = lint_csharp(text)
            if not ok:
                log(f"  BLOQUÉ {os.path.basename(path)} : C# invalide — {msg}. "
                    f"Non poussé (protège SB).", "ERROR")
                continue

        action, sa, need_stamp = resolve_target(obj, path, text, safe=safe)
        if sa is None:
            continue

        # Stamp AVANT encodage -> byteCode identique au fichier disque (idempotent)
        final_text = apply_stamp(text, action.get("name"), sa["id"]) if need_stamp else text
        sa["byteCode"] = encode_bytecode(final_text)
        changed += 1
        log(f"  OK  {os.path.basename(path)} -> action '{action.get('name')}' "
            f"/ sub {sa['id']}")
        if final_text != text:
            stamps.append((path, final_text))

    if changed == 0:
        log("Aucune sub-action patchee.", "WARN")
        if killed:
            launch_streamerbot()
        return

    backup_actions()
    try:
        write_actions(obj, had_bom, lock=(lock and reload_mode == "B"))
    except OSError as e:
        log(f"Ecriture actions.json impossible : {e}", "ERROR")
        if killed:
            launch_streamerbot()
        return
    log(f"actions.json patche ({changed} sub-action(s)).")

    # auto-stamp des GUID dans les .cs (meme contenu que le byteCode ecrit)
    for path, final_text in stamps:
        try:
            with open(path, "w", encoding="utf-8", newline="") as f:
                f.write(final_text)
            log(f"  GUID auto-stamp dans {os.path.basename(path)}")
        except OSError as e:
            log(f"  Auto-stamp impossible {path} : {e}", "ERROR")

    # Reload
    if reload_mode == "A":
        since = datetime.datetime.now()
        launch_streamerbot()
        if DO_VERIFY:
            verify_after_launch(since)
    else:
        log("Mode B : modif ECRITE mais NON APPLIQUEE. Relance Streamer.bot "
            "pour recompiler. Ne sauvegarde rien cote SB d'ici la (risque d'ecrasement).",
            "WARN")


# ------------------------- process Streamer.bot -------------------------
# Empeche l'apparition d'une fenetre console (tasklist/taskkill) quand l'appli
# tourne sans console (ex: lancee via pythonw depuis l'interface).
CREATE_NO_WINDOW = 0x08000000 if sys.platform.startswith("win") else 0


def sb_is_running():
    exe = os.path.basename(SB_EXE)
    try:
        if sys.platform.startswith("win"):
            out = subprocess.run(["tasklist", "/FI", "IMAGENAME eq " + exe],
                                 capture_output=True, text=True,
                                 creationflags=CREATE_NO_WINDOW).stdout or ""
            return exe.lower() in out.lower()
        return subprocess.run(["pgrep", "-f", exe],
                              capture_output=True).returncode == 0
    except OSError:
        return False


def kill_streamerbot():
    exe = os.path.basename(SB_EXE)
    if sys.platform.startswith("win"):
        r = subprocess.run(["taskkill", "/F", "/IM", exe],
                           capture_output=True, text=True,
                           creationflags=CREATE_NO_WINDOW)
        if r.returncode == 0:
            log(f"Streamer.bot ({exe}) tue (force).")
            time.sleep(1.0)
            return True
        log(f"Streamer.bot non tue (peut-etre deja ferme) : {r.stdout.strip()} {r.stderr.strip()}")
        return False
    else:
        subprocess.run(["pkill", "-f", exe])
        log("pkill Streamer.bot envoye (Linux).")
        return True


def launch_streamerbot():
    if not os.path.exists(SB_EXE):
        log(f"Executable introuvable, relance impossible : {SB_EXE}", "ERROR")
        return
    try:
        if sys.platform.startswith("win"):
            subprocess.Popen([SB_EXE], cwd=os.path.dirname(SB_EXE),
                             creationflags=0x00000008)  # DETACHED_PROCESS
        else:
            subprocess.Popen([SB_EXE], cwd=os.path.dirname(SB_EXE))
        log("Streamer.bot relance.")
    except OSError as e:
        log(f"Relance impossible : {e}", "ERROR")


# ------------------------- discover -------------------------
def norm(s):
    return "\n".join(line.rstrip() for line in s.replace("\r\n", "\n").split("\n")).strip()


def discover(write=False):
    obj, _ = read_actions()
    cs_index = []
    for a in obj["actions"]:
        for sa in csharp_subactions(a):
            src = base64.b64decode(sa["byteCode"]).decode("utf-8", "replace")
            cs_index.append((a.get("name"), sa["id"], norm(src)))

    cs_files = []
    for root, _dirs, files in os.walk(WATCH_DIR):
        for n in files:
            if n.lower().endswith(".cs"):
                cs_files.append(os.path.join(root, n))

    log(f"--discover : {len(cs_files)} fichiers .cs vs {len(cs_index)} sub-actions C#")
    for path in cs_files:
        with open(path, "r", encoding="utf-8-sig") as f:
            text = f.read()
        _, sub_id = parse_header(text)
        if sub_id:
            continue  # deja mappe
        target = norm(text)
        best_ratio, best = 0.0, None
        for aname, sid, src in cs_index:
            r = difflib.SequenceMatcher(None, target, src).ratio()
            if r > best_ratio:
                best_ratio, best = r, (aname, sid)
        rel = os.path.relpath(path, WATCH_DIR)
        if best:
            aname, sid = best
            flag = "MATCH" if best_ratio >= DISCOVER_THRESHOLD else "proche"
            log(f"  [{flag} {best_ratio:.2f}] {rel} -> action '{aname}' / {sid}")
            if write and best_ratio >= DISCOVER_THRESHOLD:
                stamp_action_and_id(path, text, aname, sid)
                log(f"      en-tetes inserees dans {rel}")
        else:
            log(f"  [aucun]        {rel}")


# ------------------------- dernière sauvegarde -------------------------
def newest_cs():
    """Chemin du .cs modifie le plus recemment sous WATCH_DIR (ou None)."""
    newest, newest_mtime = None, -1.0
    for root, _d, names in os.walk(WATCH_DIR):
        for n in names:
            if n.lower().endswith(".cs"):
                p = os.path.join(root, n)
                try:
                    m = os.path.getmtime(p)
                except OSError:
                    continue
                if m > newest_mtime:
                    newest, newest_mtime = p, m
    return newest


# ------------------------- scan doublons -------------------------
def check_duplicates():
    """Scanne actions.json : signale les actions ayant PLUSIEURS sous-actions C#
    (doublons probables) et les GUID de sous-action partages. Ne modifie rien."""
    _warn_stale_if_running()
    try:
        obj, _ = read_actions()
    except (OSError, json.JSONDecodeError) as e:
        log(f"Lecture actions.json impossible : {e}", "ERROR")
        return

    nb_multi, nb_dup_id = 0, 0
    seen_ids = {}
    for a in obj["actions"]:
        cs = csharp_subactions(a)
        if len(cs) > 1:
            nb_multi += 1
            log(f"  DOUBLON : action '{a.get('name')}' a {len(cs)} sous-actions C# :", "WARN")
            for sa in cs:
                lignes = base64.b64decode(sa["byteCode"]).decode("utf-8", "replace").splitlines()
                apercu = next((l.strip() for l in lignes
                               if l.strip() and not l.strip().startswith("//")),
                              lignes[0] if lignes else "")
                log(f"      - id {sa['id']} | index {sa.get('index')} | {apercu[:60]}")
        for sa in a.get("subActions", []):
            sid = sa.get("id")
            if sid in seen_ids:
                nb_dup_id += 1
                log(f"  ID PARTAGE : {sid} present dans '{seen_ids[sid]}' ET '{a.get('name')}'", "WARN")
            else:
                seen_ids[sid] = a.get("name")

    if nb_multi == 0 and nb_dup_id == 0:
        log("✅ Aucun doublon : chaque action a au plus 1 sous-action C#, aucun id partage.")
    else:
        log(f"Scan termine : {nb_multi} action(s) avec doublon C#, {nb_dup_id} id(s) partage(s). "
            f"Supprime les sous-actions en trop dans Streamer.bot (garde la bonne).", "WARN")


# ------------------------- helpers communs -------------------------
def all_cs_files():
    out = []
    for root, _d, names in os.walk(WATCH_DIR):
        for n in names:
            if n.lower().endswith(".cs"):
                out.append(os.path.join(root, n))
    return out


def _warn_stale_if_running():
    """Les diagnostics lisent le fichier disque : si SB tourne, il peut etre en retard."""
    if sb_is_running():
        log("⚠️ Streamer.bot est ACTIF — actions.json sur disque peut être en retard "
            "sur la mémoire de SB (modifs UI non encore sauvegardées). "
            "Ferme SB pour un diagnostic fiable.", "WARN")


# ------------------------- lint C# (pre-vol) -------------------------
def _strip_cs(text):
    """Retire commentaires, chaines et char-literals — pour compter les
    accolades sans faux positif (ex: le char literal '}')."""
    out = []
    i, n = 0, len(text)
    while i < n:
        two = text[i:i + 2]
        c = text[i]
        if two == "//":
            j = text.find("\n", i)
            i = n if j == -1 else j
        elif two == "/*":
            j = text.find("*/", i + 2)
            i = n if j == -1 else j + 2
        elif c == "@" and i + 1 < n and text[i + 1] == '"':      # verbatim @"..."
            i += 2
            while i < n:
                if text[i] == '"':
                    if i + 1 < n and text[i + 1] == '"':
                        i += 2; continue
                    i += 1; break
                i += 1
            out.append(" ")
        elif c == '"':                                           # "..."
            i += 1
            while i < n:
                if text[i] == "\\":
                    i += 2; continue
                if text[i] == '"':
                    i += 1; break
                i += 1
            out.append(" ")
        elif c == "'":                                           # '.'
            i += 1
            while i < n:
                if text[i] == "\\":
                    i += 2; continue
                if text[i] == "'":
                    i += 1; break
                i += 1
            out.append(" ")
        else:
            out.append(c); i += 1
    return "".join(out)


def lint_csharp(text):
    """(ok, message) — verif structurelle rapide, zero dependance."""
    s = _strip_cs(text)
    for o, c, nom in (("{", "}", "accolades"), ("(", ")", "parenthèses"),
                      ("[", "]", "crochets")):
        if s.count(o) != s.count(c):
            return False, f"{nom} déséquilibrées ({s.count(o)} '{o}' / {s.count(c)} '{c}')"
    if "class CPHInline" not in text:
        return False, "classe 'CPHInline' absente"
    if "Execute" not in text:
        return False, "méthode 'Execute' absente"
    return True, ""


# ------------------------- docteur -------------------------
def doctor():
    """Diagnostic global : désync, non mappés, orphelins, doublons."""
    _warn_stale_if_running()
    try:
        obj, _ = read_actions()
    except (OSError, json.JSONDecodeError) as e:
        log(f"Lecture actions.json impossible : {e}", "ERROR")
        return

    cs_by_id = {}
    for a in obj["actions"]:
        for sa in csharp_subactions(a):
            cs_by_id[sa["id"]] = (a.get("name"), sa)

    desync, unmapped, a_pousser, mapped = [], [], [], set()
    for path in all_cs_files():
        rel = os.path.relpath(path, WATCH_DIR)
        with open(path, "r", encoding="utf-8-sig") as f:
            text = f.read()
        aname, sub_id = parse_header(text)
        if sub_id and sub_id in cs_by_id:
            mapped.add(sub_id)
            code = base64.b64decode(cs_by_id[sub_id][1]["byteCode"]).decode("utf-8", "replace")
            if norm(code) != norm(text):
                desync.append(rel)
        elif aname or sub_id:
            a_pousser.append(rel)   # en-tête présent mais pas encore dans SB (à pousser)
        else:
            unmapped.append(rel)    # aucun en-tête

    orphans = [nom for sid, (nom, _sa) in cs_by_id.items() if sid not in mapped]
    dups = [(a.get("name"), len(csharp_subactions(a)))
            for a in obj["actions"] if len(csharp_subactions(a)) > 1]

    log(f"🩺 Docteur Kika-sync — {len(all_cs_files())} .cs vs {len(cs_by_id)} sous-actions C#")
    _section("DÉSYNCHRONISÉS (code local ≠ SB, à pousser)", desync, "WARN")
    _section("À POUSSER (en-tête ok, pas encore dans SB)", a_pousser, "WARN")
    _section("NON MAPPÉS (pas d'en-tête // sb-action → ignorés)", unmapped, "WARN")
    _section("SANS .cs LOCAL (dans SB uniquement — peut être normal)", orphans, "INFO")
    if dups:
        log(f"  ⚠️ DOUBLONS : {len(dups)} action(s) avec >1 sous-action C#", "WARN")
        for nom, k in dups:
            log(f"      - '{nom}' : {k} sous-actions C#", "WARN")
    if not any((desync, a_pousser, unmapped, dups)):
        log("✅ Tout est sain : tout est mappé, synchronisé, sans doublon.")


def _section(titre, items, niveau):
    if items:
        log(f"  {titre} : {len(items)}", niveau)
        for it in items:
            log(f"      - {it}", niveau)


# ------------------------- aperçu diff -------------------------
def diff_preview(paths=None):
    """Affiche le diff (SB actuel -> fichier local) pour les .cs désynchronisés."""
    import difflib
    _warn_stale_if_running()
    try:
        obj, _ = read_actions()
    except (OSError, json.JSONDecodeError) as e:
        log(f"Lecture actions.json impossible : {e}", "ERROR")
        return
    paths = paths or all_cs_files()
    n_diff = 0
    for path in paths:
        with open(path, "r", encoding="utf-8-sig") as f:
            text = f.read()
        aname, sub_id = parse_header(text)
        sa = None
        if sub_id:
            _a, sa = find_subaction_by_id(obj, sub_id)
        if sa is None and aname:
            hits = resolve_action_by_name(obj, aname)
            cs = csharp_subactions(hits[0]) if hits else []
            sa = cs[0] if len(cs) == 1 else None
        rel = os.path.relpath(path, WATCH_DIR)
        if sa is None:
            log(f"  {rel} : non mappé (rien à comparer).", "WARN")
            continue
        current = base64.b64decode(sa["byteCode"]).decode("utf-8", "replace")
        if norm(current) == norm(text):
            continue
        n_diff += 1
        log(f"  ▼ DIFF {rel} (SB → local) :")
        d = difflib.unified_diff(current.splitlines(), text.splitlines(),
                                 lineterm="", n=1)
        for k, line in enumerate(d):
            if k < 3:
                continue  # saute l'en-tête ---/+++/@@
            if k > 44:
                log("      … (diff tronqué)")
                break
            log(f"      {line}")
    if n_diff == 0:
        log("✅ Aucun écart : tous les .cs mappés sont synchronisés avec SB.")


# ------------------------- backups : liste / restauration -------------------------
def _backups_sorted():
    try:
        b = [os.path.join(BACKUP_DIR, n) for n in os.listdir(BACKUP_DIR)
             if n.startswith("actions_") and n.endswith(".json")]
    except OSError:
        return []
    return sorted(b, key=os.path.getmtime, reverse=True)   # plus récent en premier


def list_backups():
    b = _backups_sorted()
    if not b:
        log("Aucun backup dans " + BACKUP_DIR, "WARN")
        return
    log(f"Backups disponibles ({len(b)}, plus récent = 0) :")
    for i, p in enumerate(b[:20]):
        ts = datetime.datetime.fromtimestamp(os.path.getmtime(p)).strftime("%Y-%m-%d %H:%M:%S")
        ko = os.path.getsize(p) // 1024
        log(f"      [{i}] {ts}  ({ko} Ko)  {os.path.basename(p)}")


def restore_backup(which=0):
    b = _backups_sorted()
    if not b:
        log("Aucun backup à restaurer.", "ERROR")
        return
    if which < 0 or which >= len(b):
        log(f"Index de backup invalide : {which} (0..{len(b) - 1}).", "ERROR")
        return
    src = b[which]
    if sb_is_running():
        log("⚠️ Streamer.bot tourne — restaure SB FERMÉ, sinon il réécrasera. "
            "Ferme SB puis relance la restauration.", "WARN")
        return
    backup_actions()  # snapshot de l'état courant avant d'écraser
    _clear_readonly(ACTIONS_JSON)
    shutil.copy2(src, ACTIONS_JSON)
    ts = datetime.datetime.fromtimestamp(os.path.getmtime(src)).strftime("%Y-%m-%d %H:%M:%S")
    log(f"✅ Restauré : {os.path.basename(src)} ({ts}) → actions.json. Relance Streamer.bot.")


# ------------------------- git : fichiers modifiés -------------------------
def git_changed_cs():
    """.cs modifiés/ajoutés depuis le dernier commit (sous WATCH_DIR)."""
    def _git(args):
        try:
            r = subprocess.run(["git", "-C", REPO_ROOT] + args,
                               capture_output=True, text=True,
                               creationflags=CREATE_NO_WINDOW)
            return r.stdout.splitlines() if r.returncode == 0 else []
        except OSError:
            return []
    rels = set(_git(["diff", "--name-only", "HEAD"]))
    rels |= set(_git(["ls-files", "--others", "--exclude-standard"]))
    out = []
    for rel in rels:
        if rel.lower().endswith(".cs"):
            ap = os.path.join(REPO_ROOT, rel.replace("/", os.sep))
            if os.path.isfile(ap) and os.path.abspath(ap).lower().startswith(
                    os.path.abspath(WATCH_DIR).lower()):
                out.append(ap)
    return out


# ------------------------- compile-check (Roslyn best-effort) -------------------------
_COMPILE_DIR = os.path.join(tempfile.gettempdir(), "kikasync_check")


def _compile_setup():
    os.makedirs(_COMPILE_DIR, exist_ok=True)
    with open(os.path.join(_COMPILE_DIR, "kikacheck.csproj"), "w", encoding="utf-8") as f:
        f.write('<Project Sdk="Microsoft.NET.Sdk">\n'
                '  <PropertyGroup>\n'
                '    <TargetFramework>net10.0</TargetFramework>\n'
                '    <Nullable>disable</Nullable>\n'
                '    <ImplicitUsings>disable</ImplicitUsings>\n'
                '    <EnableDefaultCompileItems>false</EnableDefaultCompileItems>\n'
                '    <LangVersion>latest</LangVersion>\n'
                '    <OutputType>Library</OutputType>\n'
                '  </PropertyGroup>\n'
                '  <ItemGroup>\n'
                '    <Compile Include="Stub.cs" />\n'
                '    <Compile Include="User.cs" />\n'
                '  </ItemGroup>\n'
                '</Project>\n')
    with open(os.path.join(_COMPILE_DIR, "Stub.cs"), "w", encoding="utf-8") as f:
        f.write("using System.Collections.Generic;\n"
                "public partial class CPHInline {\n"
                "    protected dynamic CPH = null;\n"
                "    protected Dictionary<string, object> args = new Dictionary<string, object>();\n"
                "}\n")


def compile_check(paths):
    if shutil.which("dotnet") is None:
        log("Compile-check indisponible (dotnet introuvable) — lint light seulement.", "WARN")
        return None
    _compile_setup()
    user = os.path.join(_COMPILE_DIR, "User.cs")
    log("Compile-check (Roslyn) — la 1re passe restaure le projet (~10-20 s)…")
    ok_all = True
    for path in paths:
        try:
            with open(path, "r", encoding="utf-8-sig") as f:
                text = f.read()
        except OSError as e:
            log(f"  Lecture impossible {path} : {e}", "ERROR")
            continue
        # rend la classe 'partial' pour lui injecter CPH/args via Stub.cs
        code = text.replace("public class CPHInline", "public partial class CPHInline", 1)
        with open(user, "w", encoding="utf-8") as f:
            f.write(code)
        try:
            r = subprocess.run(["dotnet", "build", "-nologo"], cwd=_COMPILE_DIR,
                               capture_output=True, text=True,
                               creationflags=CREATE_NO_WINDOW, timeout=180)
        except (OSError, subprocess.TimeoutExpired) as e:
            log(f"  Compile-check échoué ({os.path.basename(path)}) : {e}", "WARN")
            continue
        errs = [l.strip() for l in (r.stdout + r.stderr).splitlines() if "error CS" in l]
        if errs:
            ok_all = False
            log(f"  ✗ {os.path.basename(path)} : {len(errs)} erreur(s)", "ERROR")
            seen = set()
            for e in errs:
                if e not in seen:
                    seen.add(e)
                    log(f"      {e}", "ERROR")
        else:
            log(f"  ✓ {os.path.basename(path)} : compile OK")
    log("Compile-check : tout compile ✅" if ok_all
        else "Compile-check : des erreurs ci-dessus.", "INFO" if ok_all else "ERROR")
    return ok_all


# ------------------------- vérif post-relance (log SB) -------------------------
_RE_LOGTS = re.compile(r"\[(\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2})")


def verify_after_launch(since):
    try:
        logs = [os.path.join(SB_LOGS_DIR, n) for n in os.listdir(SB_LOGS_DIR)
                if n.lower().endswith(".log")]
    except OSError:
        log("Vérif post-relance : dossier logs SB introuvable.", "WARN")
        return
    if not logs:
        return
    newest = max(logs, key=os.path.getmtime)
    log("Vérif post-relance : attente du démarrage de SB (8 s)…")
    time.sleep(8)
    try:
        with open(newest, "r", encoding="utf-8", errors="replace") as f:
            lines = f.readlines()
    except OSError:
        return
    errs = []
    for line in lines:
        if "error CS" in line:
            m = _RE_LOGTS.match(line.lstrip())
            if m:
                try:
                    t = datetime.datetime.strptime(m.group(1), "%Y-%m-%d %H:%M:%S")
                    if t < since:
                        continue
                except ValueError:
                    pass
            errs.append(line.strip())
    if errs:
        log("⚠️ Streamer.bot signale des erreurs de compilation après relance :", "ERROR")
        for e in errs[-8:]:
            log(f"      {e}", "ERROR")
    else:
        log("Vérif post-relance : aucune erreur de compilation détectée ✅")


# ------------------------- watch -------------------------
def run_watch(reload_mode, lock):
    try:
        from watchdog.observers import Observer
        from watchdog.events import FileSystemEventHandler
    except ImportError:
        log("watchdog non installe. Fais : pip install watchdog "
            "(ou utilise --once / --file).", "ERROR")
        sys.exit(1)

    pending = set()
    lock_obj = threading.Lock()
    timer = {"t": None}

    def flush():
        with lock_obj:
            batch = list(pending)
            pending.clear()
        if batch:
            log(f"--- Batch de {len(batch)} fichier(s) ---")
            patch_files(batch, reload_mode, lock)

    def schedule():
        if timer["t"]:
            timer["t"].cancel()
        timer["t"] = threading.Timer(DEBOUNCE_SECONDS, flush)
        timer["t"].daemon = True
        timer["t"].start()

    class Handler(FileSystemEventHandler):
        def _on(self, path):
            if path.lower().endswith(".cs"):
                with lock_obj:
                    pending.add(path)
                schedule()

        def on_modified(self, e):
            if not e.is_directory:
                self._on(e.src_path)

        def on_created(self, e):
            if not e.is_directory:
                self._on(e.src_path)

        def on_moved(self, e):
            if not e.is_directory:
                self._on(e.dest_path)

    obs = Observer()
    obs.schedule(Handler(), WATCH_DIR, recursive=True)
    obs.start()
    log(f"Surveillance de {WATCH_DIR} (recursive). Mode reload={reload_mode}. "
        f"Ctrl+C pour arreter.")
    try:
        while True:
            time.sleep(1)
    except KeyboardInterrupt:
        log("Arret demande.")
    finally:
        obs.stop()
        obs.join()


# ------------------------- main -------------------------
def main():
    global RELOAD_MODE, LOCK_AFTER_PATCH, DO_LINT, DO_VERIFY
    p = argparse.ArgumentParser(description="Kika-sync : .cs -> Streamer.bot actions.json")
    p.add_argument("--reload", choices=["A", "B"], help="A=kill+relaunch, B=patch seul")
    p.add_argument("--once", action="store_true", help="synchronise tout le dossier (tous les .cs)")
    p.add_argument("--last", action="store_true", help="synchronise le .cs modifie le plus recemment")
    p.add_argument("--changed", action="store_true", help="synchronise les .cs modifies depuis le dernier commit (git)")
    p.add_argument("--file", help="patch un seul fichier .cs puis quitte")
    p.add_argument("--check-duplicates", action="store_true", help="scanne actions.json pour les doublons C#")
    p.add_argument("--doctor", action="store_true", help="diagnostic : desync / non mappes / orphelins / doublons")
    p.add_argument("--diff", action="store_true", help="apercu des ecarts local <-> SB (lecture seule)")
    p.add_argument("--compile-check", action="store_true", help="verifie la compilation C# (Roslyn/dotnet)")
    p.add_argument("--list-backups", action="store_true", help="liste les backups de actions.json")
    p.add_argument("--restore", nargs="?", type=int, const=0, default=None,
                   metavar="N", help="restaure le backup N (0=le plus recent) — SB fermé")
    p.add_argument("--discover", action="store_true", help="propose un mapping par similarite")
    p.add_argument("--write", action="store_true", help="(avec --discover) insere les en-tetes surs")
    p.add_argument("--lock", action="store_true", help="(mode B) actions.json en lecture seule apres patch")
    p.add_argument("--verify", action="store_true", help="(mode A) verifie le log SB apres relance")
    p.add_argument("--no-lint", action="store_true", help="desactive la pre-verification C#")
    args = p.parse_args()

    reload_mode = args.reload or RELOAD_MODE
    lock = args.lock or LOCK_AFTER_PATCH
    if args.no_lint:
        DO_LINT = False
    if args.verify:
        DO_VERIFY = True

    # --- diagnostics (lecture seule / hors patch) ---
    if args.doctor:
        doctor(); return
    if args.diff:
        diff_preview(); return
    if args.check_duplicates:
        check_duplicates(); return
    if args.list_backups:
        list_backups(); return
    if args.restore is not None:
        restore_backup(args.restore); return
    if args.compile_check:
        if args.file:
            compile_check([os.path.abspath(args.file)])
        elif args.last:
            nc = newest_cs()
            compile_check([nc] if nc else [])
        else:
            compile_check(all_cs_files())
        return
    if args.discover:
        discover(write=args.write); return

    # --- synchronisations (patch) ---
    if args.file:
        patch_files([os.path.abspath(args.file)], reload_mode, lock)
        return
    if args.last:
        nc = newest_cs()
        if nc:
            log(f"Derniere sauvegarde : {os.path.relpath(nc, WATCH_DIR)}")
            patch_files([nc], reload_mode, lock)
        else:
            log("Aucun fichier .cs trouve sous WATCH_DIR.", "WARN")
        return
    if args.changed:
        files = git_changed_cs()
        if files:
            log(f"{len(files)} .cs modifie(s) depuis le dernier commit.")
            patch_files(files, reload_mode, lock)
        else:
            log("Aucun .cs modifie depuis le dernier commit.")
        return
    if args.once:
        patch_files(all_cs_files(), reload_mode, lock)
        return
    run_watch(reload_mode, lock)


if __name__ == "__main__":
    main()
