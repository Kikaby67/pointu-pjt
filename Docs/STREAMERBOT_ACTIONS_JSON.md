---
name: streamerbot-actions-json
description: Injecter des actions, triggers et code C# directement dans actions.json de Streamer.bot depuis un éditeur, sans copier-coller dans l'UI et sans outil tiers. Déclencher sur "ajouter une action Streamer.bot", "injecter une commande dans SB", "éditer actions.json", "créer un trigger Streamer.bot".
allowed-tools: Read, Write, Edit, Bash, Glob
---

# Injecter une action dans Streamer.bot en éditant `actions.json`

Streamer.bot stocke toutes ses actions dans un fichier JSON. On peut donc créer une action
complète — trigger compris — depuis un éditeur, sans jamais ouvrir son interface.

Ce document décrit le format **tel qu'il est réellement sur disque**, pas une approximation.

---

## ⚠️ La règle qui fait échouer presque toutes les tentatives

**Streamer.bot lit `actions.json` au démarrage, garde tout en mémoire, et RÉÉCRIT le fichier
quand il se ferme.**

Conséquence : si on modifie le fichier pendant que Streamer.bot tourne, la modification est
écrasée dès qu'il se ferme. Elle semble avoir fonctionné (le fichier sur disque est correct),
puis elle disparaît sans message d'erreur.

> **Toute écriture doit se faire Streamer.bot FERMÉ.**
> Séquence obligatoire : fermer SB → écrire → relancer SB.

Corollaire : le code C# inline n'est **compilé qu'au démarrage**. Il n'y a pas de rechargement
à chaud. Relancer SB n'est pas une précaution, c'est la seule façon d'activer le code.

Second corollaire : **une seule action au C# invalide empêche SB de compiler *toutes* les
autres**. Il faut donc valider le C# avant d'écrire, pas après.

---

## 1. Les fichiers

Les deux fichiers vivent dans le sous-dossier `data/` de l'installation Streamer.bot :

| Fichier | Contenu |
|---|---|
| `<Streamer.bot>/data/actions.json` | actions, triggers, sous-actions, code C# |
| `<Streamer.bot>/data/commands.json` | définitions des commandes chat (`!xxx`) |

Le chemin exact dépend de l'installation. Ne jamais le supposer : demander à l'utilisateur,
ou chercher `actions.json` à côté de `Streamer.bot.exe`.

### Contraintes de forme — non négociables

| Contrainte | Détail |
|---|---|
| **Encodage** | UTF-8 **avec BOM** (`EF BB BF`). Le perdre peut casser la lecture par SB. |
| **Format** | **Minifié**, sans espaces (`separators=(",", ":")` en Python). Le reformater produit un diff illisible et alourdit le fichier. |
| **Écriture** | **Atomique** : écrire dans un fichier temporaire puis le renommer. Une écriture interrompue sur le fichier réel détruit toute la configuration. |
| **Sauvegarde** | Copier `actions.json` avant toute écriture. Toujours. |

### Structure racine de `actions.json`

```json
{
  "collapsedGroups": [],
  "groups": ["Joueurs", "Quetes", "Combat"],
  "version": 23,
  "t": "2026-07-20T07:25:17.7950255+02:00",
  "blocking": [],
  "queues": [],
  "actions": []
}
```

- `groups` — liste **plate** de noms de groupes. Si une action référence un groupe absent de
  cette liste, elle fonctionne mais le groupe peut ne pas apparaître dans l'UI. **Ajouter le
  nom du groupe ici** en même temps que l'action, ou mettre `"group": null`.
- `version` et `t` — laisser tels quels, SB les gère.

### Structure racine de `commands.json`

```json
{ "collapsedGroups": [], "version": 4, "t": "...", "commands": [] }
```

---

## 2. Anatomie d'une action

Une **action** contient des **triggers** (ce qui la déclenche) et des **sous-actions**
(ce qu'elle fait, dans l'ordre). Le code C# est une sous-action parmi d'autres types possibles.

```
action
├── triggers[]      ← commande chat, message, follow, timer…
└── subActions[]    ← Execute C# Code, envoyer un message, attendre…
```

### L'objet action

```json
{
  "id": "eb084a48-b7b4-4c06-a518-41a89e5865bd",
  "queue": "00000000-0000-0000-0000-000000000000",
  "enabled": true,
  "excludeFromHistory": false,
  "excludeFromPending": false,
  "name": "!bonjour",
  "group": "Joueurs",
  "alwaysRun": false,
  "randomAction": false,
  "concurrent": false,
  "triggers": [],
  "subActions": [],
  "collapsedGroups": []
}
```

- `id` — GUID unique, à générer (`uuid.uuid4()`).
- `queue` — GUID nul = file par défaut. Ne pas inventer d'autre valeur.
- `name` — **doit être unique**. Un doublon rend les deux actions indiscernables dans l'UI.

### L'objet sous-action « Execute C# Code »

```json
{
  "name": null,
  "description": null,
  "references": ["C:\\Windows\\Microsoft.NET\\Framework64\\v4.0.30319\\mscorlib.dll"],
  "byteCode": "Ly8gc2ItYWN0aW9uOiAhYm9uam91cg...",
  "precompile": false,
  "delayStart": false,
  "saveResultToVariable": false,
  "saveToVariable": null,
  "id": "24be61f7-405a-4e73-81d9-d2e28357a2bf",
  "weight": 0.0,
  "type": 99999,
  "parentId": null,
  "enabled": true,
  "index": 0
}
```

- **`type: 99999`** identifie une sous-action « Execute C# Code ».
- **`byteCode` = base64 de la source C# encodée en UTF-8.** Rien d'autre : malgré son nom,
  ce n'est ni du bytecode ni du gzip.
  ```python
  byteCode = base64.b64encode(source.encode("utf-8")).decode("ascii")
  source   = base64.b64decode(byteCode).decode("utf-8")
  ```
- `index` — position dans `subActions`, à partir de 0. Doit rester cohérent avec l'ordre du
  tableau, sinon SB exécute dans un ordre inattendu.
- `references` — copier la valeur d'une sous-action C# existante de la même installation.
  Le chemin dépend de la machine : **ne pas l'inventer**, le relever.

---

## 3. Les triggers

Un trigger est un petit objet dans `action.triggers`. Tous partagent :

```json
{ "id": "<GUID>", "type": <numéro>, "enabled": true, "exclusions": [] }
```

Certains types ajoutent un champ de référence.

| Type | Déclencheur | Champ supplémentaire |
|---|---|---|
| `401` | **Commande chat** (`!xxx`) | `commandId` → voir §4 |
| `133` | Message chat (tous) | — |
| `101` | Follow | — |
| `102` | Cheer / Bits | — |
| `103` | Sub | — |
| `104` | Resub | — |
| `105` | Sub offert | — |
| `107` | Raid | — |
| `112` | Récompense de points de chaîne | `rewardId` |
| `701` | Timer | `timerId` |
| `14004` | Changement de scène OBS | `sceneName`, `obsId` |

> Ces numéros ont été relevés sur une installation réelle. Ils sont stables, mais **la liste
> n'est pas exhaustive**.

### Relever soi-même un type inconnu

C'est la méthode fiable, et elle marche pour n'importe quel trigger :

1. Créer **une fois** l'action avec son trigger dans l'interface de Streamer.bot.
2. Fermer SB (il sauvegarde en se fermant).
3. Lire `actions.json`, trouver l'action, regarder son objet trigger.
4. Réutiliser ce schéma pour les suivantes.

Cette méthode est aussi **la seule** pour les triggers qui référencent un objet créé ailleurs
dans l'UI : timers (`timerId`), récompenses (`rewardId`), watchers de fichiers, WebSocket…
Ces identifiants ne peuvent pas être fabriqués : l'objet cible doit exister d'abord.

---

## 4. Cas particulier : les commandes chat (le plus courant)

Une commande `!xxx` demande d'écrire dans **deux fichiers**. C'est le piège principal :
un trigger `401` dont le `commandId` ne correspond à aucune entrée de `commands.json` produit
une **commande morte** — aucune erreur, aucun message, elle ne répond simplement jamais.

**Étape 1** — ajouter l'entrée dans `commands.json` :

```json
{
  "permittedUsers": [], "permittedGroups": [],
  "id": "96337867-0ec8-4857-9425-38b56e1a3f04",
  "name": "!bonjour",
  "enabled": true,
  "include": false,
  "mode": 0,
  "command": "!bonjour",
  "regexExplicitCapture": false,
  "location": 0,
  "ignoreBotAccount": true,
  "ignoreInternal": true,
  "sources": 1,
  "persistCounter": false,
  "persistUserCounter": false,
  "caseSensitive": false,
  "globalCooldown": 0,
  "userCooldown": 0,
  "group": null,
  "grantType": 0
}
```

- `location: 0` = la commande doit être au **début** du message.
- `sources: 1` = Twitch.
- `globalCooldown` / `userCooldown` en secondes.

**Étape 2** — référencer son `id` dans le trigger de l'action :

```json
{ "commandId": "96337867-0ec8-4857-9425-38b56e1a3f04",
  "id": "<nouveau GUID>", "type": 401, "enabled": true, "exclusions": [] }
```

**Avant de créer**, vérifier qu'une commande du même texte n'existe pas déjà dans
`commands.json` : si oui, **réutiliser son `id`** au lieu d'en créer une seconde. Deux
définitions pour `!bonjour` produisent un comportement imprévisible.

---

## 5. Le code C#

Streamer.bot compile chaque sous-action comme une classe autonome. Squelette minimal :

```csharp
using System;

public class CPHInline
{
    public bool Execute()
    {
        string user = args["user"].ToString();
        CPH.SendMessage("Bonjour " + user + " !");
        return true;
    }
}
```

Règles :

- La classe s'appelle **obligatoirement** `CPHInline`, la méthode `Execute()` retourne un `bool`.
- `CPH` — objet fourni par Streamer.bot : `CPH.SendMessage`, `CPH.LogWarn`, `CPH.TwitchTimeoutUser`…
- `args` — dictionnaire des données du trigger : `args["user"]`, `args["rawInput"]`,
  `args["message"]`… Le contenu dépend du trigger.
- **Chaque sous-action est isolée** : aucune méthode n'est partagée entre fichiers. Les
  helpers doivent être copiés dans chacun.
- `return true` = succès ; `return false` interrompt la suite des sous-actions.

### En-tête de repérage (recommandé)

Sans convention, on ne sait plus quel fichier local correspond à quelle sous-action. Deux
commentaires en tête suffisent :

```csharp
// sb-action: !bonjour
// sb-subaction-id: 24be61f7-405a-4e73-81d9-d2e28357a2bf
using System;
```

Le GUID est la **seule** ancre fiable : le nom de l'action peut être renommé dans l'UI, le
GUID non. Ces commentaires font partie de la source, donc du `byteCode` — ils voyagent avec
le code sans effet à l'exécution.

---

## 6. Procédure complète

1. **Écrire et relire le C#.** Vérifier au minimum : accolades et parenthèses équilibrées,
   présence de `class CPHInline` et de `Execute`. Rappel : un fichier invalide bloque la
   compilation de *toutes* les actions.
2. **Demander à l'utilisateur de fermer Streamer.bot**, et le vérifier.
3. **Sauvegarder** `actions.json` (et `commands.json` si commande chat) — copie horodatée.
4. **Lire** les fichiers en préservant l'information de BOM.
5. **Vérifier les doublons** : nom d'action déjà pris ? commande déjà définie ?
6. **Construire** l'action, son trigger, sa sous-action C#. Générer des GUID uniques.
   Ajouter le groupe à `groups` s'il est nouveau.
7. **Écrire** de façon atomique, minifiée, avec le BOM.
8. **Demander la relance** de Streamer.bot.
9. **Vérifier** : l'action apparaît dans l'UI, et le test réel du trigger fonctionne.

---

## 7. Script de référence

Sans dépendance externe. À adapter — les chemins et le type de trigger changent à chaque cas.

```python
import base64, json, os, shutil, uuid, datetime

ACTIONS = r"<Streamer.bot>\data\actions.json"

def read_json(path):
    """Retourne (objet, had_bom) — l'info BOM doit survivre à l'aller-retour."""
    with open(path, "rb") as f:
        raw = f.read()
    return json.loads(raw.decode("utf-8-sig")), raw[:3] == b"\xef\xbb\xbf"

def write_json(path, obj, had_bom):
    """Écriture ATOMIQUE, minifiée, BOM préservé."""
    data = ("﻿" if had_bom else "") + json.dumps(obj, ensure_ascii=False,
                                                      separators=(",", ":"))
    tmp = path + ".tmp"
    with open(tmp, "w", encoding="utf-8", newline="") as f:
        f.write(data)
    os.replace(tmp, path)          # remplacement atomique

def backup(path):
    stamp = datetime.datetime.now().strftime("%Y%m%d_%H%M%S")
    shutil.copy2(path, f"{path}.{stamp}.bak")

# --- 1. sauvegarder puis lire -------------------------------------------------
backup(ACTIONS)
obj, bom = read_json(ACTIONS)

NOM = "Mon Action"
if any(a.get("name") == NOM for a in obj["actions"]):
    raise SystemExit(f"Une action nommée '{NOM}' existe déjà.")

# --- 2. relever les references sur une sous-action C# existante ---------------
#     (chemin .NET dépendant de la machine : ne jamais l'inventer)
refs = next(sa["references"] for a in obj["actions"]
            for sa in a["subActions"] if sa.get("type") == 99999)

# --- 3. construire ------------------------------------------------------------
source = open("mon_action.cs", encoding="utf-8-sig").read()

sub = {
    "name": None, "description": None, "references": list(refs),
    "byteCode": base64.b64encode(source.encode("utf-8")).decode("ascii"),
    "precompile": False, "delayStart": False,
    "saveResultToVariable": False, "saveToVariable": None,
    "id": str(uuid.uuid4()), "weight": 0.0, "type": 99999,
    "parentId": None, "enabled": True, "index": 0,
}

action = {
    "id": str(uuid.uuid4()),
    "queue": "00000000-0000-0000-0000-000000000000",
    "enabled": True, "excludeFromHistory": False, "excludeFromPending": False,
    "name": NOM, "group": None,
    "alwaysRun": False, "randomAction": False, "concurrent": False,
    "triggers": [{"id": str(uuid.uuid4()), "type": 133,     # 133 = message chat
                  "enabled": True, "exclusions": []}],
    "subActions": [sub],
    "collapsedGroups": [],
}

# --- 4. insérer et écrire (Streamer.bot doit être FERMÉ) ----------------------
obj["actions"].append(action)
write_json(ACTIONS, obj, bom)
print(f"Action '{NOM}' créée. Relancer Streamer.bot pour l'activer.")
```

Pour **mettre à jour** le code d'une action existante, ne pas la recréer : retrouver la
sous-action par son GUID et remplacer son seul champ `byteCode`.

---

## 8. Pièges

| Piège | Conséquence | Parade |
|---|---|---|
| Écrire pendant que SB tourne | modification écrasée en silence à la fermeture | fermer SB d'abord |
| Oublier de relancer SB | l'action existe mais le C# n'est pas compilé | relancer |
| BOM perdu ou JSON reformaté | fichier gonflé, diff illisible, lecture potentiellement cassée | préserver BOM + minification |
| `commandId` sans entrée dans `commands.json` | commande morte, aucune erreur | créer l'entrée d'abord |
| Nom d'action dupliqué | actions indiscernables dans l'UI | vérifier avant d'insérer |
| GUID réutilisé | comportements incohérents | `uuid.uuid4()` à chaque objet |
| `index` incohérent | sous-actions exécutées dans le désordre | renuméroter 0..n-1 |
| Groupe absent de `groups` | groupe invisible dans l'UI | ajouter le nom à `groups` |
| `references` recopiées d'une autre machine | chemin .NET inexistant → compilation échouée | relever localement |
| C# invalide | **toutes** les actions cessent de compiler | valider avant d'écrire |
| Pas de sauvegarde | configuration entière perdue | copie horodatée systématique |

---

## 9. Vérification

Après relance de Streamer.bot :

1. L'action apparaît dans l'UI, avec son trigger et sa sous-action C#.
2. Le log de Streamer.bot (`<Streamer.bot>/logs/`) ne signale pas d'erreur de compilation.
3. **Déclencher le trigger pour de vrai** — c'est la seule preuve qui compte. Une action
   bien formée peut échouer à l'exécution (droits manquants, chemin invalide, API refusée).
4. Relire le `byteCode` déployé et le comparer au fichier local : c'est ce qui détecte un
   décalage silencieux entre ce qu'on croit avoir poussé et ce qui tourne.

---

> **Note** — Ce document décrit la méthode manuelle, sans dépendance. Il existe des outils qui
> automatisent ce flux (surveillance de fichiers, synchronisation, diagnostic, restauration).
> Ils appliquent exactement le format décrit ici : comprendre ce document, c'est pouvoir s'en
> passer, ou en écrire un.
