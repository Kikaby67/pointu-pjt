#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Kika-sync — Interface graphique (thème sombre vert & noir).
Pilote la synchro .cs -> Streamer.bot (kika_sync.py) sans passer par PowerShell.

Lancement : double-clic sur "Lancer Kika-sync.bat"
         ou : python kika_sync_gui.py
"""

import importlib.util
import os
import queue
import subprocess
import sys
import threading
import tkinter as tk
from tkinter import ttk

HERE      = os.path.dirname(os.path.abspath(__file__))
KIKA_SYNC = os.path.join(HERE, "kika_sync.py")
LOG_FILE  = os.path.join(HERE, "kika_sync.log")

# python.exe meme si l'appli est lancee via pythonw.exe
PY = sys.executable
if PY.lower().endswith("pythonw.exe"):
    cand = PY[:-len("pythonw.exe")] + "python.exe"
    if os.path.exists(cand):
        PY = cand

CREATE_NO_WINDOW = 0x08000000 if sys.platform.startswith("win") else 0

# Réutilise sb_is_running() de kika_sync pour la pastille d'état (best effort)
SB_IS_RUNNING = None
try:
    _spec = importlib.util.spec_from_file_location("kika_sync", KIKA_SYNC)
    _ks = importlib.util.module_from_spec(_spec)
    _spec.loader.exec_module(_ks)
    SB_IS_RUNNING = _ks.sb_is_running
except Exception:
    SB_IS_RUNNING = None

# ---- Palette vert & noir ----
BG       = "#0c100c"   # fond général (noir légèrement verdâtre)
BG_ALT   = "#141b14"   # panneaux / journal
BG_BTN2  = "#18221a"   # boutons secondaires
GREEN    = "#39ff14"   # accent principal (repris du bot Discord)
GREEN_H  = "#5bff45"   # survol
GREEN_D  = "#2bcf10"   # pressé
INK      = "#06140b"   # texte sur bouton vert
FG       = "#d7e5d7"   # texte principal
FG_DIM   = "#6f8a6f"   # texte discret
RED      = "#ff6b6b"
AMBER    = "#e8b84b"


class App:
    def __init__(self, root):
        self.root = root
        self.proc = None
        self.watching = False
        self.q = queue.Queue()

        root.title("Kika-sync — Streamer.bot")
        root.geometry("900x600")
        root.minsize(720, 500)
        root.configure(bg=BG)

        self._init_style()

        # ---- En-tête + état ----
        head = ttk.Frame(root, padding=(16, 14, 16, 6))
        head.pack(fill="x")
        ttk.Label(head, text="⚡ Kika-sync", style="Kika.Header.TLabel").pack(side="left")
        self.sb_dot = ttk.Label(head, text="", style="Kika.Dim.TLabel")
        self.sb_dot.pack(side="right")

        # ---- Options reload ----
        opt = ttk.Frame(root, padding=(16, 0))
        opt.pack(fill="x")
        ttk.Label(opt, text="Reload :", style="Kika.Dim.TLabel").pack(side="left")
        self.mode = tk.StringVar(value="A")
        ttk.Radiobutton(opt, text="A (auto)", variable=self.mode, value="A",
                        command=self._sync_lock).pack(side="left", padx=(6, 2))
        ttk.Radiobutton(opt, text="B (SB fermé)", variable=self.mode, value="B",
                        command=self._sync_lock).pack(side="left", padx=(0, 10))
        self.lock = tk.BooleanVar(value=False)
        self.chk_lock = ttk.Checkbutton(opt, text="verrouiller", variable=self.lock)
        self.chk_lock.pack(side="left")

        ttk.Label(root, style="Kika.Dim.TLabel", padding=(16, 4),
                  text="Mode A = ferme et relance Streamer.bot tout seul (recommandé).  "
                       "Mode B = patch seul, SB doit être fermé.").pack(fill="x")

        # ---- 3 actions principales ----
        prim = ttk.Frame(root, padding=(14, 8))
        prim.pack(fill="x")
        for i in range(3):
            prim.columnconfigure(i, weight=1, uniform="prim")

        self.btn_all = ttk.Button(prim, text="🗂️  Synchroniser\ntout le dossier",
                                  style="Big.TButton",
                                  command=lambda: self.run(["--once"] + self._reload_args()))
        self.btn_all.grid(row=0, column=0, sticky="ew", padx=(0, 6))

        self.btn_last = ttk.Button(prim, text="💾  Synchroniser\nla dernière save",
                                   style="Big.TButton",
                                   command=lambda: self.run(["--last"] + self._reload_args()))
        self.btn_last.grid(row=0, column=1, sticky="ew", padx=6)

        self.btn_dup = ttk.Button(prim, text="🔍  Scanner\nles doublons",
                                  style="Big.TButton",
                                  command=lambda: self.run(["--check-duplicates"]))
        self.btn_dup.grid(row=0, column=2, sticky="ew", padx=(6, 0))

        # ---- Actions secondaires ----
        sec = ttk.Frame(root, padding=(14, 2))
        sec.pack(fill="x")
        self.btn_stamp = ttk.Button(sec, text="🏷️ Écrire les en-têtes (auto-map)",
                                    style="Ghost.TButton",
                                    command=lambda: self.run(["--discover", "--write"]))
        self.btn_stamp.pack(side="left", padx=(0, 6))
        self.btn_watch = ttk.Button(sec, text="▶ Surveillance auto",
                                    style="Ghost.TButton", command=self.toggle_watch)
        self.btn_watch.pack(side="left", padx=6)
        self.btn_stop = ttk.Button(sec, text="■ Stop", style="Ghost.TButton",
                                   command=self.stop, state="disabled")
        self.btn_stop.pack(side="left", padx=6)

        # ---- Journal ----
        logframe = ttk.Frame(root, padding=(14, 8))
        logframe.pack(fill="both", expand=True)
        self.txt = tk.Text(logframe, wrap="word", height=15,
                           background=BG_ALT, foreground=FG,
                           insertbackground=GREEN, relief="flat", borderwidth=0,
                           highlightthickness=1, highlightbackground="#20301f",
                           padx=10, pady=8, font=("Consolas", 9))
        self.txt.pack(side="left", fill="both", expand=True)
        sb = ttk.Scrollbar(logframe, command=self.txt.yview, style="Kika.Vertical.TScrollbar")
        sb.pack(side="right", fill="y")
        self.txt.configure(yscrollcommand=sb.set, state="disabled")
        self.txt.tag_config("warn", foreground=AMBER)
        self.txt.tag_config("err",  foreground=RED)
        self.txt.tag_config("ok",   foreground=GREEN)
        self.txt.tag_config("dim",  foreground=FG_DIM)

        # ---- Barre d'état ----
        bottom = ttk.Frame(root, padding=(16, 8))
        bottom.pack(fill="x")
        self.status = tk.StringVar(value="Prêt.")
        ttk.Label(bottom, textvariable=self.status, style="Kika.Dim.TLabel").pack(side="left")
        ttk.Button(bottom, text="Vider le journal", style="Ghost.TButton",
                   command=self.clear_log).pack(side="right")
        ttk.Button(bottom, text="Ouvrir le log", style="Ghost.TButton",
                   command=self.open_log).pack(side="right", padx=6)

        self._log("Kika-sync prêt. Chemins : configurés dans kika_sync.local.json.\n", "dim")
        self._sync_lock()
        self.root.after(80, self._poll)
        self.root.after(300, self._update_sb_status)
        self.root.protocol("WM_DELETE_WINDOW", self._on_close)

    # ------------------ style ------------------
    def _init_style(self):
        st = ttk.Style()
        try:
            st.theme_use("clam")  # le plus stylable des thèmes intégrés
        except tk.TclError:
            pass
        st.configure(".", background=BG, foreground=FG, bordercolor=BG,
                     focuscolor=BG, font=("Segoe UI", 10))
        st.configure("TFrame", background=BG)
        st.configure("TLabel", background=BG, foreground=FG)
        st.configure("Kika.Header.TLabel", background=BG, foreground=GREEN,
                     font=("Segoe UI", 16, "bold"))
        st.configure("Kika.Dim.TLabel", background=BG, foreground=FG_DIM)

        st.configure("TRadiobutton", background=BG, foreground=FG)
        st.map("TRadiobutton",
               foreground=[("selected", GREEN), ("active", GREEN_H)],
               background=[("active", BG)])
        st.configure("TCheckbutton", background=BG, foreground=FG_DIM)
        st.map("TCheckbutton",
               foreground=[("selected", GREEN), ("active", FG)],
               background=[("active", BG)])

        # Boutons principaux : fond vert, texte encre
        st.configure("Big.TButton", background=GREEN, foreground=INK,
                     font=("Segoe UI", 11, "bold"), padding=(10, 14),
                     borderwidth=0, relief="flat")
        st.map("Big.TButton",
               background=[("disabled", "#25361f"), ("pressed", GREEN_D),
                           ("active", GREEN_H)],
               foreground=[("disabled", "#5f7a5f")])

        # Boutons secondaires : contour discret, texte vert
        st.configure("Ghost.TButton", background=BG_BTN2, foreground=GREEN,
                     font=("Segoe UI", 9), padding=(10, 6),
                     borderwidth=1, relief="flat")
        st.map("Ghost.TButton",
               background=[("disabled", BG), ("pressed", "#0f170f"),
                           ("active", "#213021")],
               foreground=[("disabled", "#4b5f4b"), ("active", GREEN_H)],
               bordercolor=[("!disabled", "#2c4029")])

        st.configure("Kika.Vertical.TScrollbar", background=BG_BTN2,
                     troughcolor=BG, bordercolor=BG, arrowcolor=FG_DIM)
        st.map("Kika.Vertical.TScrollbar", background=[("active", "#263824")])

    # ------------------ helpers UI ------------------
    def _reload_args(self):
        a = ["--reload", self.mode.get()]
        if self.lock.get() and self.mode.get() == "B":
            a.append("--lock")
        return a

    def _sync_lock(self):
        self.chk_lock.configure(state="normal" if self.mode.get() == "B" else "disabled")

    def _action_buttons(self):
        return [self.btn_all, self.btn_last, self.btn_dup,
                self.btn_stamp, self.btn_watch]

    def _set_busy(self, busy):
        for b in self._action_buttons():
            b.configure(state="disabled" if busy else "normal")
        self.btn_stop.configure(state="normal" if busy else "disabled")

    def _log(self, text, tag=None):
        self.txt.configure(state="normal")
        self.txt.insert("end", text, tag or ())
        self.txt.see("end")
        self.txt.configure(state="disabled")

    def _log_line(self, line):
        tag = None
        if "[ERROR]" in line:
            tag = "err"
        elif "[WARN]" in line or "DOUBLON" in line or "ATTENTION" in line:
            tag = "warn"
        elif " OK  " in line or "MATCH" in line or "patché" in line \
                or "relancé" in line or "Aucun doublon" in line or "✅" in line:
            tag = "ok"
        self._log(line, tag)

    def clear_log(self):
        self.txt.configure(state="normal")
        self.txt.delete("1.0", "end")
        self.txt.configure(state="disabled")

    def open_log(self):
        if os.path.exists(LOG_FILE):
            try:
                os.startfile(LOG_FILE)  # noqa (Windows)
            except (AttributeError, OSError):
                self.status.set("Impossible d'ouvrir le log ici.")
        else:
            self.status.set("Pas encore de fichier log.")

    def _update_sb_status(self):
        if SB_IS_RUNNING is not None:
            try:
                running = SB_IS_RUNNING()
            except Exception:
                running = None
            if running is True:
                self.sb_dot.configure(text="●  Streamer.bot actif", foreground=GREEN)
            elif running is False:
                self.sb_dot.configure(text="○  Streamer.bot arrêté", foreground=FG_DIM)
            else:
                self.sb_dot.configure(text="")
        self.root.after(3000, self._update_sb_status)

    # ------------------ exécution ------------------
    def run(self, args, watch=False):
        if self.proc is not None:
            self.status.set("Une opération est déjà en cours.")
            return
        if not os.path.exists(KIKA_SYNC):
            self._log(f"[ERROR] Introuvable : {KIKA_SYNC}\n", "err")
            return

        cmd = [PY, "-u", KIKA_SYNC] + args
        env = dict(os.environ, PYTHONIOENCODING="utf-8")
        self._log(f"\n$ {' '.join(args)}\n", "dim")
        try:
            self.proc = subprocess.Popen(
                cmd, stdout=subprocess.PIPE, stderr=subprocess.STDOUT,
                text=True, encoding="utf-8", errors="replace",
                env=env, creationflags=CREATE_NO_WINDOW, cwd=HERE)
        except OSError as e:
            self._log(f"[ERROR] Lancement impossible : {e}\n", "err")
            self.proc = None
            return

        self.watching = watch
        self._set_busy(True)
        self.status.set("Surveillance en cours…" if watch else "Traitement…")
        threading.Thread(target=self._reader, args=(self.proc,), daemon=True).start()

    def _reader(self, proc):
        try:
            for line in iter(proc.stdout.readline, ""):
                self.q.put(line)
        finally:
            try:
                proc.stdout.close()
            except OSError:
                pass
            self.q.put(("__END__", proc.returncode))

    def _poll(self):
        try:
            while True:
                item = self.q.get_nowait()
                if isinstance(item, tuple) and item and item[0] == "__END__":
                    self._finished()
                else:
                    self._log_line(item)
        except queue.Empty:
            pass
        self.root.after(80, self._poll)

    def _finished(self):
        self.proc = None
        was_watch = self.watching
        self.watching = False
        self._set_busy(False)
        self.btn_watch.configure(text="▶ Surveillance auto")
        self.status.set("Surveillance arrêtée." if was_watch else "Terminé.")

    # ------------------ boutons ------------------
    def toggle_watch(self):
        if self.proc is None:
            self.btn_watch.configure(text="⏳ Surveillance…")
            self.run(self._reload_args(), watch=True)

    def stop(self):
        if self.proc is not None:
            self.status.set("Arrêt…")
            try:
                self.proc.terminate()
            except OSError:
                pass

    def _on_close(self):
        if self.proc is not None:
            try:
                self.proc.terminate()
            except OSError:
                pass
        self.root.destroy()


def main():
    root = tk.Tk()
    App(root)
    root.mainloop()


if __name__ == "__main__":
    main()
