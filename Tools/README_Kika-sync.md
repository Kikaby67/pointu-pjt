# Kika-sync — Sync `.cs` → Streamer.bot `actions.json`

Pousse tes fichiers `.cs` locaux (VS Code) vers les sub-actions « Execute C# Code »
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

Double-clique sur **`Lancer Kika-sync.bat`** (ou `python kika_sync_gui.py`).

**3 boutons principaux :**
- **🗂️ Synchroniser tout le dossier** → pousse tous les `.cs` du dossier VS Code (`--once`).
- **💾 Synchroniser la dernière save** → pousse uniquement le `.cs` modifié le plus récemment (`--last`).
- **🔍 Scanner les doublons** → analyse `actions.json` et signale les actions avec plusieurs
  sous-actions C# ou des GUID partagés (`--check-duplicates`, lecture seule).

**Rangée sync** : **🔀 Modifiés (git)** (pousse ce qui a changé depuis le dernier commit),
**🏷️ Écrire les en-têtes**, **▶ Surveillance auto**, **■ Stop**.

**Rangée diagnostics & sécurité** :
- **🩺 Docteur** — désynchronisés / non mappés / orphelins / doublons, d'un coup d'œil.
- **👁️ Aperçu diff** — ce qui changerait (local → SB) sans rien écrire.
- **✔️ Vérifier le code** — compile la dernière save avec Roslyn (dotnet) → attrape les vraies erreurs C#.
- **🗂 Backups** / **♻️ Restaurer** — liste et restaure un `actions.json` sauvegardé (SB fermé).

Options : mode reload **A / B**, case **verrouiller**, case **vérif compile après relance (A)**.
Pastille **● Streamer.bot actif / ○ arrêté** en direct, **son** + bannière colorée à la fin, garde-fou
si tu lances un patch en Mode B alors que SB tourne.

Tout le reste de ce README décrit la version ligne de commande (identique sous le capot).

## Intégration VS Code

Le repo fournit `.vscode/tasks.json` : **`Ctrl+Shift+B`** synchronise la **dernière save** (Mode A)
sans quitter l'éditeur. Autres tâches (menu *Terminal → Run Task…*) : tout le dossier, docteur,
vérifier le code.

## Sécurité — pré-vol C#

Avant chaque push, Kika-sync **valide** le `.cs` (équilibre accolades/parenthèses, présence de
`CPHInline`/`Execute`). Un fichier invalide est **bloqué** (pas poussé) — car un seul `.cs` cassé
empêche SB de compiler **toutes** ses actions au démarrage. `--no-lint` pour désactiver.
Le **compile-check** (Roslyn, optionnel) va plus loin : erreurs de type, variables non déclarées, etc.

## Installation

1. Python 3.8+ installé (Tkinter est inclus, rien à faire pour l'interface).
2. Pour le mode surveillance : `pip install watchdog`
   (inutile pour `--once`, `--file`, `--discover`).

## Configuration

Les chemins machine-spécifiques ne sont **pas** en dur dans le code versionné.
Copie **`kika_sync.local.example.json`** en **`kika_sync.local.json`** (ce dernier est git-ignoré,
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
`kika_sync.py`. Le mode reload est surchargeable en ligne de commande (`--reload A|B`).

## Mise en route (recommandé, **Streamer.bot fermé**)

```bash
cd Tools

# 1) Pré-remplir les en-têtes (GUID) sur tous les .cs qui matchent à coup sûr
python kika_sync.py --discover          # aperçu, n'écrit rien
python kika_sync.py --discover --write  # insère // sb-action + // sb-subaction-id (matchs ≥ 0.97)

# 2) Premier patch complet
python kika_sync.py --once --reload B

# 3) Relance Streamer.bot toi-même → le code est recompilé et actif
```

> Pour les `.cs` non auto-mappés (nouveau code, fichiers modifiés), ajoute manuellement
> `// sb-action: <NomExactDeLAction>` en tête, puis relance ; le GUID se stampe au passage.

## Utilisation courante

```bash
# Surveillance continue (patch à chaque sauvegarde), sans toucher au process SB
python kika_sync.py --reload B

# Surveillance + redémarrage automatique de SB à chaque sauvegarde
python kika_sync.py --reload A

# Patcher un seul fichier
python kika_sync.py --file ..\Streamerbot\Commandes\Secret\commande_secret.cs

# Mode B "blindé" : verrouille actions.json après patch pour empêcher SB de l'écraser
python kika_sync.py --reload B --lock
```

### Arguments

| Argument | Effet |
|---|---|
| `--reload A\|B` | force le mode reload pour cette exécution |
| `--once` | synchronise tout le dossier (tous les `.cs`) puis quitte |
| `--last` | synchronise le `.cs` modifié le plus récemment puis quitte |
| `--changed` | synchronise les `.cs` modifiés depuis le dernier commit (git) |
| `--file CHEMIN` | patch un seul `.cs` puis quitte |
| `--doctor` | diagnostic : désync / non mappés / orphelins / doublons (lecture seule) |
| `--diff` | aperçu des écarts local → SB (lecture seule) |
| `--check-duplicates` | signale les actions à >1 sous-action C# ou GUID partagés (lecture seule) |
| `--compile-check [--file X\|--last]` | compile le(s) `.cs` avec Roslyn (dotnet) et liste les erreurs |
| `--list-backups` | liste les backups horodatés de `actions.json` |
| `--restore [N]` | restaure le backup N (0 = le plus récent), **SB fermé** |
| `--discover` / `--discover --write` | mapping par similarité / + insère les en-têtes sûrs (≥ 0.97) |
| `--lock` | mode B : `actions.json` en lecture seule après patch |
| `--verify` | mode A : scanne le log SB après relance pour détecter une erreur de compilation |
| `--no-lint` | désactive la pré-vérification C# (déconseillé) |

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

- **Backup horodaté** de `actions.json` avant chaque écriture → `…\data\_kikasync_backups\`
  (les 50 derniers sont conservés). Indépendant du `.bak` que SB gère lui-même.
- **Logs** dans la console **et** dans `Tools\kika_sync.log` : fichier détecté, action/sub-action
  trouvée ou non, backup, patch, reload.
- Écriture **atomique** (fichier temporaire puis `os.replace`) + BOM UTF-8 et format minifié
  préservés à l'identique.

## Linux Mint

Le script tourne aussi sous Linux (chemins à adapter en tête). Le kill/relaunch utilise `pkill`
et lance l'exécutable via `SB_EXE` — adapte selon ton installation (SB tourne nativement sous Windows).
```
