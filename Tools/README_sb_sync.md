# sb_sync — Sync `.cs` → Streamer.bot `actions.json`

Pousse automatiquement tes fichiers `.cs` locaux vers les sub-actions « Execute C# Code »
de Streamer.bot, sans copier-coller dans l'UI.

## Comment ça marche

- Streamer.bot stocke chaque sub-action C# dans `actions.json`, champ **`byteCode` = base64(source UTF-8)**
  (pas de gzip, malgré le nom).
- Le script surveille un dossier de `.cs`, encode le fichier et remplace le `byteCode` de la bonne sub-action.
- Il **ne peut pas** recharger le code à chaud (Streamer.bot ne compile qu'au démarrage) → voir Reload.

### Mapping fichier ↔ sub-action (par GUID, insensible aux renommages)

Chaque `.cs` porte 2 commentaires d'en-tête :

```csharp
// sb-action: !racine            ← toi : le nom EXACT de l'action parente dans SB
// sb-subaction-id: <auto>       ← rempli tout seul au 1er run, ne pas y toucher ensuite
```

- **1er passage** : le script trouve l'action nommée `!racine`, y repère la sub-action C# unique,
  récupère son GUID et **l'écrit dans l'en-tête**. Ensuite le lien est basé sur le GUID → tu peux
  renommer l'action dans SB sans rien casser.
- Si l'action a **plusieurs** sub-actions C# (rare) : le script logue les candidats, à toi de coller
  le bon `// sb-subaction-id`.
- Si l'action **n'existe pas encore** : il faut d'abord créer l'action (avec son trigger) dans SB.
  Le script crée une **sub-action** manquante dans une action existante, pas une action complète.

## Interface graphique (sans PowerShell)

Double-clique sur **`Lancer interface.bat`** (ou `python sb_sync_gui.py`).
Fenêtre avec :
- choix du mode reload **A / B** + case « verrouiller » (mode B),
- boutons **Découvrir**, **Écrire les en-têtes**, **Patch complet**, **Patch un fichier…**,
  **Démarrer/Arrêter la surveillance**, **Stop**,
- journal en direct (mêmes logs que la console) + bouton **Ouvrir le fichier log**.

Tout le reste de ce README décrit la version ligne de commande (identique sous le capot).

## Installation

1. Python 3.8+ installé (Tkinter est inclus, rien à faire pour l'interface).
2. Pour le mode surveillance : `pip install watchdog`
   (inutile pour `--once`, `--file`, `--discover`).

## Configuration

Les chemins machine-spécifiques ne sont **pas** en dur dans le code versionné.
Copie **`sb_sync.local.example.json`** en **`sb_sync.local.json`** (ce dernier est git-ignoré,
donc tes chemins ne partent jamais sur GitHub) et renseigne :

```json
{
  "actions_json": "C:\\...\\Streamer.bot\\data\\actions.json",
  "sb_exe": "C:\\...\\Streamer.bot\\Streamer.bot.exe",
  "reload_mode": "A"
}
```

| Clé (local.json) / variable | Rôle |
|---|---|
| `actions_json` | fichier `actions.json` patché |
| `sb_exe` | exécutable Streamer.bot (pour le reload mode A) |
| `watch_dir` | dossier des `.cs` — **optionnel**, par défaut le `Streamerbot/` du repo (déduit) |
| `reload_mode` | `A` = kill+relaunch (défaut), `B` = patch seul |

Les autres réglages (`CREATE_IF_MISSING`, `LOCK_AFTER_PATCH`, seuils…) restent en tête de
`sb_sync.py`. Le mode reload est surchargeable en ligne de commande (`--reload A|B`).

## Mise en route (recommandé, **Streamer.bot fermé**)

```bash
cd Tools

# 1) Pré-remplir les en-têtes (GUID) sur tous les .cs qui matchent à coup sûr
python sb_sync.py --discover          # aperçu, n'écrit rien
python sb_sync.py --discover --write  # insère // sb-action + // sb-subaction-id (matchs ≥ 0.97)

# 2) Premier patch complet
python sb_sync.py --once --reload B

# 3) Relance Streamer.bot toi-même → le code est recompilé et actif
```

> Pour les `.cs` non auto-mappés (nouveau code, fichiers modifiés), ajoute manuellement
> `// sb-action: <NomExactDeLAction>` en tête, puis relance ; le GUID se stampe au passage.

## Utilisation courante

```bash
# Surveillance continue (patch à chaque sauvegarde), sans toucher au process SB
python sb_sync.py --reload B

# Surveillance + redémarrage automatique de SB à chaque sauvegarde
python sb_sync.py --reload A

# Patcher un seul fichier
python sb_sync.py --file ..\Streamerbot\Commandes\Secret\commande_secret.cs

# Mode B "blindé" : verrouille actions.json après patch pour empêcher SB de l'écraser
python sb_sync.py --reload B --lock
```

### Arguments

| Argument | Effet |
|---|---|
| `--reload A\|B` | force le mode reload pour cette exécution |
| `--once` | patch tous les `.cs` une fois puis quitte (pas de surveillance) |
| `--file CHEMIN` | patch un seul `.cs` puis quitte |
| `--discover` | propose un mapping par similarité de code (n'écrit rien) |
| `--discover --write` | + insère les en-têtes pour les matchs sûrs (≥ 0.97) |
| `--lock` | mode B : `actions.json` en lecture seule après patch |

## Reload : A ou B ? (⭐ lis ça)

**Fait crucial** : Streamer.bot charge `actions.json` **au démarrage**, garde tout **en mémoire**,
et **réécrit le fichier à sa fermeture**. Donc **tant que SB tourne, tout patch externe est écrasé
quand SB se ferme** — et comme il faut fermer/relancer SB pour appliquer le code, patcher pendant
que SB tourne ne sert à rien. **Il faut patcher pendant que SB est FERMÉ.**

| | Mode A (kill + relaunch) | Mode B (patch seul) |
|---|---|---|
| Ce qu'il fait | tue SB → patche → relance | patche seulement |
| SB pendant l'écriture | **fermé** (garanti) | tel quel — **doit être fermé par toi** |
| Fiable si SB tourne | ✅ oui | ❌ non (SB écrasera au prochain close) |
| Code appliqué | ✅ au relaunch | ❌ tu relances SB toi-même |
| Bon pour | **usage normal, création + màj** | grouper des edits **SB déjà fermé**, relance manuelle |

➡️ **Mode A = défaut recommandé.** Il garantit la séquence sûre « SB fermé pendant l'écriture ».

**Mode B** n'est correct que si **tu as fermé SB toi-même d'abord** : tu patches autant que tu veux
(fichier fermé, aucun écrasement), puis tu démarres SB quand tu as fini. Si SB tourne, le mode B
te fera perdre tes modifs à la prochaine fermeture de SB (utilise `--lock` à tes risques, ou Mode A).

**Créer une sous-action** suit la même règle : ce n'est fiable **que SB fermé** (donc Mode A). Sinon
SB, qui n'a jamais connu la nouvelle sous-action, l'efface en se refermant. Le tool refuse d'ailleurs
de recréer en boucle une sous-action stampée disparue (anti-doublon) — c'est le signe qu'il faut
repasser en Mode A.

## Sécurité / traçabilité

- **Backup horodaté** de `actions.json` avant chaque écriture → `…\data\_sbsync_backups\`
  (les 50 derniers sont conservés). Indépendant du `.bak` que SB gère lui-même.
- **Logs** dans la console **et** dans `Tools\sb_sync.log` : fichier détecté, action/sub-action
  trouvée ou non, backup, patch, reload.
- Écriture **atomique** (fichier temporaire puis `os.replace`) + BOM UTF-8 et format minifié
  préservés à l'identique.

## Linux Mint

Le script tourne aussi sous Linux (chemins à adapter en tête). Le kill/relaunch utilise `pkill`
et lance l'exécutable via `SB_EXE` — adapte selon ton installation (SB tourne nativement sous Windows).
```
