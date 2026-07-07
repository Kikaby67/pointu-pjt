# CLAUDE.md
> Guide technique complet pour Claude Code (VS Code / claude.ai/code)
> Projet Twitch bot game — Streamer.bot + C# inline

---

## Vue d'ensemble du projet

**Pointu-PJT** est un mini-jeu RPG textuel dans le chat Twitch.
Les viewers tapent des commandes (`!rejoindre`, `!quete`, `!attaque`...).
Chaque joueur a un fichier JSON sur le disque local. Pas de base de données.

**Machine** : Windows 11, AMD Ryzen 5 5600X, 32 Go RAM
**Stack** : Streamer.bot (C# inline), JSON fichiers plats, .NET 10.0 (prototypage)
**Repo GitHub** : https://github.com/Kikaby67/pjt (public) — ⚠️ l'ancien repo `Pointupjt` est abandonné

---

## Structure des dossiers

```
Pointu-PJT/
├── Apprentissage/Pointu-pjt/   # .NET 10.0 console app (prototypage C# uniquement)
├── Streamerbot/                 # Fichiers C# collés dans Streamer.bot
│   ├── Bonjour/
│   ├── Rejoindre/
│   ├── Commandes/
│   │   ├── Profil/
│   │   ├── Choisirclasse/
│   │   ├── ChoisirSousClasse/
│   │   ├── InfoClasses/         # !hexadécimeur, !cryptolame, etc.
│   │   ├── Repos/               # !repos
│   │   ├── Arbonet/             # !arbonet
│   │   ├── Inventaire/          # !inventaire
│   │   ├── Equiper/             # !equiper
│   │   ├── Vendre/              # !vendre
│   │   ├── Utiliser/            # !utiliser
│   │   ├── Abandon/             # !abandon
│   │   ├── Accepter/            # !accepter
│   │   ├── Refuser/             # !refuser
│   │   └── Secret/              # !racine (commande secrète)
│   ├── quetes/
│   │   ├── quest_system.cs      # !quete (lancer / consulter)
│   │   └── quest_timer.cs       # Timer QuestCheck (30s, auto-résolution + rencontres + mini-boss)
│   ├── combat/
│   │   ├── commande_combat.cs   # !combat (rencontre + mini-boss)
│   │   ├── commande_discuter.cs # !discuter (rencontre)
│   │   ├── combat_fuir.cs       # !fuir (rencontre)
│   │   ├── combat_soin.cs       # !soin (HORS combat)
│   │   ├── combat_attaque.cs    # !attaque (DÉPRÉCIÉ → redirige)
│   │   └── combat_defense.cs    # !defense (DÉPRÉCIÉ → redirige)
│   ├── Boss/                     # Boss communautaire (tour par tour, étape etat_global)
│   │   ├── spawn_boss.cs        # !spawnboss (streamer) → recrutement
│   │   ├── commande_arene.cs    # !arene → rejoindre le recrutement
│   │   ├── commande_attaquer.cs # !attaquer → frapper à son tour
│   │   └── arena_check.cs       # Timer ArenaCheck (transition/riposte/AFK)
│   ├── Timer_Xp/
│   │   ├── Timer_XP_visionnage.cs
│   │   └── tracker_activite.cs      # Twitch Chat Message → met à jour derniereActivite
│   └── Reward/
│       ├── Jet de dé/           # 1d6_PV.cs, 1d4_CA.cs
│       └── Bonus +2/            # +2_PV.cs, +2_CA.cs, +2_Attaque.cs
├── Donnees/
│   ├── joueurs/                 # Un .json par joueur (ex: kikabygaming.json)
│   ├── config_classes.json      # ★ Source unique : stats classes + sous-classes
│   ├── config_ennemis.json      # ★ Source unique : stats tous les ennemis
│   ├── config_items.json        # ★ Source unique : stats tous les items
│   ├── config_quetes.json       # ★ Source unique : quêtes (format quete001_*)
│   ├── config_global.json       # ★ Source unique : constantes de jeu (cooldowns, %, seuils)
│   ├── config_level.json        # ★ Source unique : seuils XP et bonus de niveau
│   ├── config_allies.json       # ★ Source unique : paramètres alliés/marchands
│   ├── secret_recu.txt          # Liste des joueurs ayant reçu l'Ecaille-de-Pointu
│   └── etat_global.json         # État partagé du boss communautaire (arène)
├── Lore/
│   ├── LA_LEGENDE_DE_POINTU_V2.md
│   ├── FICHES_CLASSES.md
│   └── BESTIAIRE.md
└── DiscordBot/
    ├── bot_discord.py           # Bot Python discord.py (Discloud)
    └── discloud.config
```

---

## Build & Run (prototypage)

```bash
cd Apprentissage/Pointu-pjt
dotnet build
dotnet run
```

> Les fichiers `Streamerbot/` ne sont PAS compilés comme projet .NET.
> Ils sont collés directement dans Streamer.bot → Execute C# Code.

---

## Conventions de code CRITIQUES — Streamer.bot

### Règles absolues
- Chaque fichier est **autonome** — pas de partage de méthodes entre fichiers
- `using System;` et `using System.IO;` en tête de chaque fichier
- Classe toujours `CPHInline`, méthode `Execute()` retourne `bool`
- `args["user"]` pour le pseudo viewer (JAMAIS `args["nomJoueur"]`)
- `CPH.SendMessage(string)` pour envoyer dans le chat
- `CPH.Wait(int ms)` pour attendre (ne PAS utiliser dans les quêtes — bloque Streamer.bot)
- `CPH.LogWarn(string)` pour les logs
- **Jamais** `Newtonsoft.Json` ni `System.Text.Json` → parser manuel uniquement
- Chemins avec `@"..."` pour éviter les doubles backslashes

### Les 3 méthodes utilitaires (copier dans CHAQUE fichier)

```csharp
private string LireValeur(string json, string cle)
{
    string marqueur = "\"" + cle + "\": ";
    int posDebut    = json.IndexOf(marqueur);
    if (posDebut == -1) return "0";
    posDebut       += marqueur.Length;
    int posFin      = json.IndexOfAny(new char[] { ',', '\n', '}' }, posDebut);
    return json.Substring(posDebut, posFin - posDebut).Trim().Trim('"');
}

private string ModifierValeur(string json, string cle, string val, bool estTexte)
{
    string marqueur = "\"" + cle + "\": ";
    int posDebut    = json.IndexOf(marqueur);
    if (posDebut == -1) return json;
    posDebut       += marqueur.Length;
    int posFin      = json.IndexOfAny(new char[] { ',', '\n', '}' }, posDebut);
    string ancienne = json.Substring(posDebut, posFin - posDebut);
    string nouvelle = estTexte ? "\"" + val + "\"" : val;
    return json.Substring(0, posDebut) + nouvelle + json.Substring(posDebut + ancienne.Length);
}

private string AjouterValeur(string json, string cle, int montant)
{
    int val = int.Parse(LireValeur(json, cle));
    return ModifierValeur(json, cle, (val + montant).ToString(), false);
}
```

### Quand utiliser estTexte
```
estTexte = false → nombres (ram, pvMax, experience...)
                 → booléens (true, false)
estTexte = true  → chaînes de texte (classe, queteId, ennemiNom...)
```

---

## Architecture config — Source unique de vérité

Toutes les stats et constantes vivent dans des **fichiers JSON** — jamais dans le code.
Pour rééquilibrer le jeu : modifier le JSON uniquement, zéro code à toucher.

### `config_global.json` — Constantes de jeu

```
max_sac                        ← taille max de l'inventaire (8)
repos_cooldown_secondes        ← cooldown !repos (1800 = 30 min)
combat_mana_cout_soin          ← coût mana de !soin HORS combat (5)
quete_taux_echec               ← % d'échec de quête (20)
quete_chance_rencontre         ← % de rencontre par check (50)
quete_rencontre_intervalle_secondes ← intervalle entre 2 checks de rencontre (180)
quete_chance_loot_artefact     ← % de loot sur quête artefact (60)
quete_chance_ecorce            ← % de drop morceau d'écorce (5)
quete_cooldown_defaite_secondes ← cooldown après défaite (600 = 10 min)
quete_cooldown_abandon_secondes ← cooldown après !abandon (300 = 5 min)
timer_xp_gain / timer_regen_pv / timer_regen_mana ← timer 15 min (5 / 2 / 3)
timer_activite_seuil_secondes (1800) ← inactivité max pour recevoir XP (30 min)

rencontre_taux_ennemi (40)     ← % de chance de rencontre ennemie par check
rencontre_taux_allie (30)      ← % de chance de rencontre alliée (30% = rien)
rencontre_ennemis              ← CSV des ennemis de rencontre de quête
rencontre_expire_secondes      ← délai avant timeout auto d'une rencontre (120)

— mini-boss (solo, en quête) —
rencontre_mini_boss            ← CSV des mini-boss (Insecte-Bug,Corbeau-Daemon,Castor-Rootkit,Loup-Firewall)
mini_boss_niveau_min (5)       ← niveau requis pour qu'un mini-boss puisse apparaître
mini_boss_chance (25)          ← % qu'une rencontre de combat devienne un mini-boss
combat_tier_miniboss_mod (-35) ← modificateur de difficulté (plus dur que fort)
mini_boss_loot_pool (loot_rare)← pool de loot garanti à la victoire

— boss communautaire (arène, tour par tour) —
rencontre_boss                 ← CSV des boss (Reine-Bug,Munin-Daemon,Hector-Pierre Castor,Fenrir-Firewall)
arene_recrutement_secondes (300) ← durée de la phase !arene (5 min)
arene_tour_timeout_secondes (120) ← délai avant saut de tour AFK (2 min)
boss_pv_par_participant (40)   ← PV ajoutés au boss par combattant
boss_degats_base/alea (5/6)    ← dégâts d'un joueur sur le boss (+ (atk+niveau)*nbAttaques)
boss_recompense_base_xp/ram (50/20) ← récompense à TOUS les participants
boss_top_bonus_xp/ram (100/50) ← bonus au meilleur dégâteur
boss_loot_pool (loot_legendaire) ← loot du meilleur dégâteur

— !combat —
combat_base_pct (50) · combat_plancher_joueur (20) · combat_plafond_joueur (80) · combat_min/max (20/100)
combat_pv_ref/tranche/pct (16/5/3) · combat_ca_ref/tranche/pct (12/2/3) · combat_atk_ref/tranche/pct (2/2/3)
combat_mana_ref/tranche/pct (5/10/2) · combat_cha_ref/tranche/pct (8/4/1) · combat_agi_ref/tranche/pct (8/3/1)
combat_niveau_ref/tranche/pct (1/2/2) · combat_attaques_pct (6)   ← bonus par attaque au-delà de 1 (sous-classes)
combat_tier_faible_mod (100) · combat_tier_moyen_mod (0) · combat_tier_fort_mod (-20)
combat_pv_perte_diviseur (4) · combat_pv_perte_echec_facteur (3) · combat_pv_perte_alea (2)
compagnon_combat_bonus (15)    ← bonus % du compagnon recruté

— !fuir —
fuite_base_pct (30) · fuite_agilite_pct (3) · fuite_poids_pct (6) · fuite_min/max (10/95) · agilite_defaut (8)

— !discuter —
discuter_base_pct (25) · discuter_charisme_pct (3) · discuter_min/max (10/90)

— création (!choisirclasse) —
creation_pv_de (6) · creation_ca_de (4) · creation_atq_de (4)

— replis —
ennemi_ca_defaut (12) · ennemi_degats_defaut (6) · ennemi_xp_defaut (15) · ennemi_ram_defaut (3) · soin_max_defaut (4)

⚠️ Obsolètes (ancien combat tour par tour, plus lus) : combat_defense_bonus_ca,
   combat_fuite_seuil_normal, combat_fuite_seuil_cryptolame, attaque_degats_defaut, attaque_des_defaut
```

### `config_quetes.json` — Format quêtes

Clés format numéroté : `"quete001_champ": valeur` (quete001 à quete099)

```
_id          ← identifiant interne stocké dans le JSON joueur (ex: "artefact_01")
_type        ← "artefact" | "service" | "entretien" (détermine le loot)
_nom         ← nom court affiché dans le chat
_demandeur   ← NPC qui donne la quête (ex: "Pointu", "Aldric le Marchand")
_description ← description complète
_ticks       ← durée en ticks (1 tick = 5 min réelles)
_xp          ← XP récompense
_ram         ← RAM récompense
```

**Énumération dynamique** : `quest_system.cs` scanne `quete001` → `quete099` jusqu'à `_id == ""`.
Ajouter une quête = ajouter un bloc dans le JSON, **aucun code à toucher**.

**`GetQueteData(id)` retourne** : `[0]=nom [1]=ticks [2]=xp [3]=ram [4]=demandeur [5]=type`

### `config_classes.json`

Clés format plat : `"NomClasse_stat": valeur`

**Stats de classe** (utilisées à la création + combat) :
```
_pvBase, _caBase, _manaBase, _charisme, _typeArme
_agilite                    ← base de la stat agilité (réussite de !fuir) — posée à la création
_soinMax, _soinBonus        ← dés de soin (!soin)
_degatsMax, _nbDes, _nbAttaques   ← OBSOLÈTES (ancien combat tour par tour)
```

**Stats de sous-classe** (deux catégories) :

*Bonus de sélection* — appliqués UNE FOIS à la sélection, stockés dans le JSON joueur :
```
_pvMaxBonus   ← ajout permanent à pvMax et pvActuels
_caModif      ← modificateur permanent de classeArmure (peut être négatif)
_typeArme     ← remplacement de l'arme
```

*Comportements combat* — lus à l'exécution, affecter le config change tous les joueurs actifs :
```
_degatsMax, _nbDes, _nbAttaques   ← dégâts et nombre d'attaques
_soinMax, _soinBonus              ← soin (Patch-Mélodique)
_buffAttaque                      ← buff allié (Compilateur)
_auraDefense                      ← aura défensive (Protocole-Sacré)
```

**Pattern de lookup (sous-classe prioritaire sur classe) :**
```csharp
string key = sousClasse != "" && LireValeur(cfg, sousClasse + "_degatsMax") != "0"
             ? sousClasse : classe;
```

> ⚠️ `Pointeur-Null_nbAttaques: 1` est explicite pour éviter l'héritage de `Cryptolame_nbAttaques: 2`.

### `config_ennemis.json`

Clés format plat : `"NomEnnemi_stat": valeur`
```
_xp, _ram        ← récompenses (gagnées sur une victoire !combat)
_tier            ← "faible" | "moyen" | "fort" | "miniboss" | "boss"
                   chance de !combat : faible +100 / moyen 0 / fort -20 / miniboss -35 ; "boss" = arène (pas !combat)
_pv              ← OBSOLÈTE pour les rencontres classiques, mais SERT de PV de base aux boss d'arène
_degatsMax       ← OBSOLÈTE pour !combat, mais SERT à la riposte du boss d'arène (× nb joueurs)
_ca              ← OBSOLÈTE (gardé pour compat)
```

---

## JSON joueur — structure complète

Fichier : `Donnees/joueurs/{nomJoueur.ToLower()}.json`

```json
{
  "nomJoueur": "kikabygaming",
  "ram": 10,
  "niveau": 1,
  "experience": 0,
  "classeChoisie": false,
  "classe": "",
  "sousClasseChoisie": false,
  "sousClasse": "",
  "typeArme": "",
  "pvMax": 0,
  "pvActuels": 0,
  "classeArmure": 0,
  "bonusAttaque": 0,
  "manaMax": 0,
  "manaActuels": 0,
  "charisme": 0,
  "agilite": 0,
  "enCombat": false,
  "enQuete": false,
  "queteId": "",
  "queteTicksRestants": 0,
  "queteDernierTick": 0,
  "enRencontre": false,
  "rencontreType": "",
  "rencontreExpire": 0,
  "offreValeur": 0,
  "compagnonActif": "",
  "quetePauseDebut": 0,
  "queteTotalPause": 0,
  "queteCooldownFin": 0,
  "dernierCheckRencontre": 0,
  "reposCooldownFin": 0,
  "derniereActivite": 0,
  "combatActuel": {
    "ennemiNom": "",
    "ennemiPVActuels": 0,
    "buffActif": false,
    "protectionActive": false,
    "tourCombat": 0
  },
  "inventaire": [],
  "statistiques": {
    "combatsGagnes": 0,
    "combatsPerdus": 0,
    "quetesTerminees": 0
  }
}
```

> ⚠️ L'ancien format (`xp`, `sac`) est obsolète. Le champ XP s'appelle `experience`.
>
> Les champs combat sont dans `combatActuel` (objet imbriqué) ; `LireValeur` les trouve par recherche de chaîne.
> Seul `ennemiNom` est encore utilisé (le combat n'est plus tour par tour) — `ennemiPVActuels`, `buffActif`,
> `protectionActive`, `tourCombat` sont **obsolètes** (laissés pour compat).
>
> Champs récents : `agilite` (posé à la création depuis la classe), `compagnonActif` (allié recruté, `""` si aucun),
> `rencontreType` (type de rencontre en attente : `"combat"` | `"marchand_potion"` | `"vieux_sage"` | `"bonus_ram"` | `"alcove_chene"` | `"marchand_classe"`),
> `offreValeur` (valeur numérique stockée lors de la pose d'une offre alliée),
> `derniereActivite` (timestamp du dernier message chat — utilisé par le timer XP).
> `offreEnAttente` et `offreExpire` sont **supprimés** (remplacés par `rencontreType` + `rencontreExpire`).

---

## `etat_global.json` — État partagé du boss communautaire (arène)

```json
{
  "bossActif": false,
  "bossPhase": "",          // "" | "recrutement" | "combat"
  "bossNom": "",
  "bossPVMax": 0,
  "bossPVActuels": 0,
  "areneFin": 0,            // timestamp fin du recrutement
  "ordre": "",              // CSV des pseudos dans l'ordre d'initiative
  "tourIndex": 0,           // index du joueur dont c'est le tour (>= nb = riposte boss)
  "tourDeadline": 0,        // timestamp du saut de tour AFK
  "participants": ""        // CSV "pseudo:degatsCumulés" (pour le top dégâts)
}
```

Lu/écrit par les 4 fichiers `Streamerbot/Boss/`. Un seul boss à la fois.
> ⚠️ N'est plus la « rencontre manuelle streamer » d'avant (cette feature n'avait jamais été codée).

---

## Les 5 classes

| Classe | PV | CA | Mana | Charisme | Agilité | Arme | Soin |
|--------|----|----|------|---------|--------|------|------|
| Hexadécimeur | 25 | 14 | 5 | 8 | 8 | Épée | 1d4 |
| Cryptolame | 16 | 13 | 5 | 11 | 14 | Double-Dagues | 1d4 |
| Hackmancien | 14 | 10 | 30 | 10 | 10 | Bâton-Magique | 1d6 |
| Firewaller | 22 | 15 | 25 | 13 | 8 | Marteau-Rune | 1d8+3 |
| Algorythmancien | 16 | 11 | 20 | 16 | 12 | Luth-Code | 1d6 |

> ⚠️ Le nom exact en code est `"Algorythmancien"` (pas `"Algorythmien"`).
> Charisme → réussite de `!discuter` · Agilité → réussite de `!fuir`.

Toutes ces valeurs sont dans `config_classes.json`. Le code ne les duplique pas.

**Jets de création** (faces dans `config_global` : `creation_pv_de`/`creation_ca_de`/`creation_atq_de`) :
- PV final = pvBase + 1d`creation_pv_de` (défaut 1d6)
- CA finale = caBase + 1d`creation_ca_de` (défaut 1d4)
- bonusAttaque = 1d`creation_atq_de` (défaut 1d4)
- `agilite` = valeur de classe (`<classe>_agilite`)

---

## Sous-classes (niveau 5) ✅

| Classe | Sous-classe A | Sous-classe B |
|--------|--------------|--------------|
| Hexadécimeur | **Bloc-Hex** : +8 PV max | **Surcharge** : 2 attaques 1d8, -2 CA |
| Cryptolame | **Byte-Fantôme** : 3 attaques 1d6 | **Pointeur-Null** : 1d10, Arc-Binaire |
| Hackmancien | **Faille-Zéro** : 2d8 | **Compilateur** : buff allié +2 attaque |
| Firewaller | **Protocole-Sacré** : aura défense | **Serment-Binaire** : Smite +1d8 (2d8 total) |
| Algorythmancien | **Barde-Binaire** : 1d10 + buff TOUS | **Patch-Mélodique** : soin 1d8+3 |

Toutes les valeurs sont dans `config_classes.json` (voir section Architecture config).

Les **bonus de sélection** (pvMaxBonus, caModif, typeArme) sont appliqués une fois et stockés dans le JSON joueur — changer le config n'affecte pas les joueurs existants.

Les **comportements combat** (degatsMax, nbAttaques, soinMax…) sont lus à l'exécution — changer le config affecte immédiatement tous les joueurs actifs.

---

## Tableau de niveaux

| Niveau | XP requis | Bonus |
|--------|-----------|-------|
| 1 | 0 | — |
| 2 | 300 | +3 PV max |
| 3 | 900 | +1 CA, +1 PV max |
| 4 | 2 700 | +3 PV max |
| 5 | 6 500 | Sous-classe débloquée |
| 6 | 14 000 | +3 PV max |
| 7 | 23 000 | +1 CA, +1 PV max |
| 8 | 34 000 | +100 Ram |
| 9 | 48 000 | +3 PV max |
| 10 | 64 000 | +2 Charisme |

```csharp
private static readonly int[] XP_SEUILS =
    { 0, 0, 300, 900, 2700, 6500, 14000, 23000, 34000, 48000, 64000 };

private int CalculerNiveau(int xp)
{
    for (int i = XP_SEUILS.Length - 1; i >= 1; i--)
        if (xp >= XP_SEUILS[i]) return i;
    return 1;
}

private string AppliquerBonusNiveau(string json, int niveau)
{
    switch (niveau)
    {
        case 2: case 4: case 6: case 9:
            json = AjouterValeur(json, "pvMax",     3);
            json = AjouterValeur(json, "pvActuels", 3); break;
        case 3: case 7:
            json = AjouterValeur(json, "classeArmure", 1);
            json = AjouterValeur(json, "pvMax",        1);
            json = AjouterValeur(json, "pvActuels",    1); break;
        case 8:  json = AjouterValeur(json, "ram",      100); break;
        case 10: json = AjouterValeur(json, "charisme", 2);   break;
    }
    return json;
}
```

---

## Ennemis

Stats ennemis dans `config_ennemis.json`. Pour `!combat` : **`_tier`** (chance) + **`_xp`/`_ram`** (récompenses).
Pour les **boss d'arène** : **`_pv`** (PV de base) + **`_degatsMax`** (riposte). `_ca` reste obsolète.

```csharp
private string GetEnnemiTier(string nom)   // commande_combat.cs
{
    string t = LireValeurString(File.ReadAllText(CONFIG_ENNEMIS), nom + "_tier");
    return t == "" ? "moyen" : t;          // faible / moyen / fort / miniboss / boss
}

private int[] GetRecompensesEnnemi(string nom)   // XP/RAM sur victoire !combat
{
    string cfg = File.ReadAllText(CONFIG_ENNEMIS);
    int xp  = int.Parse(LireValeur(cfg, nom + "_xp"));
    int ram = int.Parse(LireValeur(cfg, nom + "_ram"));
    return new int[] { xp != 0 ? xp : 15, ram != 0 ? ram : 3 };
}
```

> Liste des ennemis de rencontre = clé `rencontre_ennemis` (CSV) dans `config_global.json`.
> Paliers rencontre : Drone-racine = faible · Martre/Taupe/Parasite/Ombre/Sentinelle = moyen · Sanglier-Crash = fort.
> Mini-boss (`rencontre_mini_boss`, tier `miniboss`) : Insecte-Bug, Corbeau-Daemon, Castor-Rootkit, Loup-Firewall.
> Boss d'arène (`rencontre_boss`, tier `boss`) : Reine-Bug, Munin-Daemon, Hector-Pierre Castor, Fenrir-Firewall.
> Vieux-Sage (`fort`) reste réservé à son événement (offre du Vieux Sage).

### Ennemis de rencontre de quête

| Nom | PV | CA | Dégâts | XP | RAM |
|-----|----|----|--------|-----|-----|
| Martre-Trojan | 30 | 12 | 1d6 | 20 | 4 |
| Sentinelle du Castor | 25 | 14 | 1d6 | 30 | 6 |
| Ombre de la mémoire | 20 | 11 | 1d8 | 25 | 5 |
| Drone-racine | 15 | 10 | 1d4 | 15 | 3 |
| Parasite de données | 18 | 12 | 1d4 | 18 | 4 |
| Sanglier-Crash | 35 | 9 | 1d8 | 22 | 5 |
| Taupe-Malware | 22 | 13 | 1d6 | 20 | 4 |

### Mini-boss (solo, en quête — tier `miniboss`)

| Nom | XP | RAM |
|-----|-----|-----|
| Insecte-Bug | 10 | 2 |
| Corbeau-Daemon | 25 | 5 |
| Castor-Rootkit | 40 | 8 |
| Loup-Firewall | 60 | 12 |

### Boss d'arène (communautaire — tier `boss`)

| Nom | PV base | degatsMax | XP | RAM |
|-----|---------|-----------|-----|-----|
| Reine-Bug | 300 | 8 | 80 | 30 |
| Munin-Daemon | 400 | 10 | 100 | 40 |
| Hector-Pierre Castor | 600 | 12 | 150 | 60 |
| Fenrir-Firewall | 500 | 10 | 120 | 50 |

> PV réels du boss = `_pv` + `boss_pv_par_participant` × nb joueurs. Riposte = `_degatsMax` × nb joueurs.

---

## Commandes implémentées ✅

### `!bonjour` → `Commande_Bonjour.cs`
Accueil du viewer. Indique `!rejoindre`.
```
Trigger : Command Triggered → !bonjour
```

### `!rejoindre` → `Commande_Rejoindre.cs`
Crée le JSON joueur avec tous les champs (y compris `reposCooldownFin`). Indique `!choisirclasse`.
```
Trigger : Command Triggered → !rejoindre
```

### `!profil` → `commande_!profil.cs`
Affiche les stats en 4 messages :
1. Nom | Niv | XP | Ram
2. Classe | Sous-classe | Arme (ou invite à choisir)
3. PV/pvMax | CA | Atq | Mana (si > 0) | Charisme (si > 0)
4. Combats V/D | Quêtes | EN COMBAT / EN QUETE si actif | items équipés (Arme/Armure/Acc si présents)
```
Trigger : Command Triggered → !profil
```

### `!choisirclasse [nom]` → `Commande_ChoisirClasse.cs`
Lit les stats depuis `config_classes.json`. Jets de dés à la création.
`args["rawInput"]` contient le nom de la classe en minuscules.
```
Trigger : Command Triggered → !choisirclasse
```

### `!sousclasse [nom]` → `commande_sousclasse.cs`
Disponible si `niveau >= 5` et `sousClasseChoisie = false`.
Lit les bonus depuis `config_classes.json` : `_pvMaxBonus`, `_caModif`, `_typeArme`.
Met `sousClasseChoisie = true`.
```
Trigger : Command Triggered → !sousclasse
```

### `!repos` → `Commandes/Repos/commande_repos.cs`
Restauration complète hors combat + hors quête.
- Restaure `pvActuels = pvMax` et `manaActuels = manaMax`
- Cooldown 30 minutes via `reposCooldownFin` (timestamp Unix)
- **Fonctionne à 0 PV** (à terre) : c'est ainsi qu'on se remet d'un effondrement.
```
Trigger : Command Triggered → !repos
```

### `!arbonet` → `Commandes/Arbonet/commande_arbonet.cs`
Affiche le synopsis du lore d'Arbonet en 4 messages. Termine par `!rejoindre`.
```
Trigger : Command Triggered → !arbonet
```

### `!quete` → `quetes/quest_system.cs`
Système basé sur timestamps Unix — **pas de `CPH.Wait()`**.

**Flux :**
1. Vérifie `queteCooldownFin` → si en cooldown, annonce les minutes restantes
2. Vérifie `classeChoisie`, `enCombat`
3. Si `enQuete == true` :
   - Si `enRencontre == true` → "tu es en pleine rencontre !"
   - Sinon : calcule `secondesEcoulees = (maintenant - queteDernierTick) - queteTotalPause`, si terminé → résoudre (80% succès)
4. Sinon : choisit une quête aléatoire, initialise tous les champs, `CPH.EnableTimer("QuestCheck")`

**Quêtes** — Format `{ description, ticks, xp, ram }` :
```
artefact_01 : 6 ticks (30 min) → 100 XP · 10 Ram
artefact_02 : 5 ticks (25 min) → 80 XP  · 8 Ram
artefact_03 : 3 ticks (15 min) → 50 XP  · 5 Ram
artefact_04 : 2 ticks (10 min) → 30 XP  · 3 Ram
artefact_05 : 1 tick  (5 min)  → 10 XP  · 1 Ram
service_01  : 3 ticks (15 min) → 50 XP  · 5 Ram
service_02  : 4 ticks (20 min) → 70 XP  · 7 Ram
service_03  : 5 ticks (25 min) → 90 XP  · 9 Ram
service_04  : 6 ticks (30 min) → 120 XP · 12 Ram
service_05  : 2 ticks (10 min) → 40 XP  · 4 Ram
entretien_01: 3 ticks (15 min) → 50 XP  · 5 Ram
entretien_02: 4 ticks (20 min) → 70 XP  · 7 Ram
entretien_03: 5 ticks (25 min) → 90 XP  · 9 Ram
```
> 1 tick = 5 minutes réelles
```
Trigger : Command Triggered → !quete
```

### `quest_timer.cs` — Timer QuestCheck (30s)
Parcourt tous les fichiers joueurs. Pour chaque joueur `enQuete == true` (ajoute au passage `rencontreExpire` /
`compagnonActif` aux anciens profils via `EnsureChamp`) :

**CAS 1** — `enRencontre == true` (rencontre en attente, toutes types) :
- Si `maintenant > rencontreExpire` → **timeout auto** : quête reprend.
  Message différencié : ennemi = "s'éloigne" / allié = "rencontre prend fin".
- Sinon → on attend le choix du joueur (2 min).
  - Rencontre ennemie (`rencontreType == "combat"`) : résolution par `!combat`/`!discuter`/`!fuir`.
  - Rencontre alliée : résolution par `!accepter`/`!refuser`.

**CAS 2** — Check rencontre tous les `quete_rencontre_intervalle_secondes` (180 s) :
Roll 0–99 contre `rencontre_taux_ennemi` (40) puis `rencontre_taux_allie` (30) — sinon rien.
- **Combat (40%)** : ennemi de `rencontre_ennemis` + `rencontreExpire`. Si `niveau >= mini_boss_niveau_min`
  et jet `< mini_boss_chance` → mini-boss de `rencontre_mini_boss`. Pose `enCombat=true`, `rencontreType="combat"`.
- **Allié (30%)** : 5 types, quête mise en pause, `enCombat=false`, timeout 2 min.
  - `marchand_potion` : achat Potion → `!accepter` / `!refuser`
  - `vieux_sage` : offre XP → `!accepter` (double roll XP + risque de perte d'item) / `!refuser` (risque de combat)
  - `bonus_ram` : bourse de RAM → `!accepter` / `!refuser`
  - `alcove_chene` : mini-repos PV+Mana → `!accepter` / `!refuser`
  - `marchand_classe` (**10% des alliés**, niv 5+) : change de classe via `!choisirclasse [nom]` / `!refuser`
- **Rien (30%)** : mise à jour de `dernierCheckRencontre`, pas de message.

**CAS 3** — Fin de quête : succès `(100 - quete_taux_echec)%`. Vide `compagnonActif`.
- Succès artefact : `quete_chance_loot_artefact`% de loot 1 item de `loot_commun` (config_quetes) si sac < `max_sac`.
- Drop d'écorce gravée : `quete_chance_ecorce`% (lettres manquantes uniquement).
```
Trigger : Timed Action → QuestCheck (30s, repeat)
```

### Rencontres à choix unique — `!combat` / `!discuter` / `!fuir`

> **Remplace l'ancien combat tour par tour.** Une rencontre se résout en **un seul message**.
> État « rencontre en attente » : `enCombat == true` + `enRencontre == true` + `ennemiNom` (dans `combatActuel`)
> + `rencontreExpire` (timestamp). Quête en pause (`quetePauseDebut`). Si le joueur ignore la rencontre au-delà de
> `rencontre_expire_secondes`, `quest_timer.cs` la résout en **fuite automatique**.
> Toutes les valeurs sont dans `config_global.json` (`combat_*`, `fuite_*`, `discuter_*`, `compagnon_*`).

#### `!combat` → `combat/commande_combat.cs`
Résolution probabiliste en un jet. Chance de réussite :
```
score = combat_base_pct
      + Tranche(pvMax,   "pv")    + Tranche(CAeff,   "ca")   + Tranche(ATQeff, "atk")
      + Tranche(manaEff, "mana")  + Tranche(chaEff,  "cha")  + Tranche(agilite, "agi")
      + Tranche(niveau,  "niveau")
      + (nbAttaques - 1) * combat_attaques_pct        // réintègre les sous-classes (multi-attaque)
Tranche(v, p) = ((v - combat_<p>_ref) / combat_<p>_tranche) * combat_<p>_pct
CAeff   = classeArmure + GetBonusItems("caBonus")     ATQeff = bonusAttaque + GetBonusItems("attaqueBonus")
manaEff = manaMax + GetBonusItems("manaBonus")        chaEff = charisme + GetBonusItems("charismeBonus")
agilite, niveau = champs du profil
nbAttaques : lu sur la sous-classe (prioritaire), sinon classe, défaut 1 (config_classes)
score  = clamp(score, combat_plancher_joueur, combat_plafond_joueur)   // ex. 20..80
si compagnonActif != "" : score += compagnon_combat_bonus
final  = clamp(score + tierMod, combat_min, combat_max)                // 20..100
tierMod (config) : faible = +100 (→100%), moyen = 0, fort = -20, miniboss = -35
```
Le **palier** vient de `config_ennemis.json` (`<Ennemi>_tier` : `faible`/`moyen`/`fort`/`miniboss`).
Calibrage de référence (kikabygaming) : moyen 80 % · fort 60 % · faible 100 % · miniboss ~45 %.

**Résolution** (`rng.Next(100) < final`) :
- **Réussite** → `pvPerdus = ceil((100-final)/combat_pv_perte_diviseur) + rng(0..combat_pv_perte_alea)`,
  +XP/+RAM de l'ennemi (`GetRecompensesEnnemi`), `combatsGagnes++`, quête reprend.
  Si PV → 0 : effondré (quête terminée, **sans** cooldown — il faut juste se soigner).
  **Mini-boss** : loot garanti d'un item de `mini_boss_loot_pool` (config_global → pool de config_quetes) si sac < `max_sac`.
- **Échec vs faible/moyen** → perte = `pvPerdus * combat_pv_perte_echec_facteur` ; survie si PV>0 (quête reprend),
  sinon effondrement + cooldown. `combatsPerdus++`.
- **Échec vs fort/miniboss** → **KO** : `pvActuels=0`, `enQuete=false`, `queteCooldownFin`, `combatsPerdus++`, compagnon perdu.
```
Trigger : Command Triggered → !combat
```

#### `!discuter` → `combat/commande_discuter.cs`
Réussite = `discuter_base_pct + charismeEff * discuter_charisme_pct` (clamp), `charismeEff = charisme + items`.
- **Sans compagnon** : réussite → **recrute** l'ennemi (`compagnonActif = ennemiNom`, booste `!combat`), quête reprend.
  Échec → rencontre maintenue (→ `!combat`/`!fuir`).
- **Avec compagnon** : réussite → passe la rencontre sans combattre. Échec → doit `!combat` (le compagnon booste).
- `compagnonActif` est vidé à la **fin de quête** (succès/échec/abandon) et à la **défaite KO**.
```
Trigger : Command Triggered → !discuter
```

#### `!fuir` → `combat/combat_fuir.cs`
Réussite = `fuite_base_pct + agiliteEff * fuite_agilite_pct - poidsEquipe * fuite_poids_pct` (clamp).
- `agiliteEff = agilite` (profil) ; repli sur `<classe>_agilite` (config_classes) si le champ est absent.
- `poidsEquipe = GetBonusItems("poids")` (config_items ; armures lourdes > 0 → fuite plus dure).
- **Réussite** → quitte la rencontre, quête reprend. **Échec** → rencontre maintenue, **pas de riposte** (→ `!combat`/`!discuter`).
```
Trigger : Command Triggered → !fuir
```

#### `!soin` → `combat/combat_soin.cs` — **HORS combat**
Soin hors rencontre uniquement (**bloqué si `enCombat == true`**). Coûte `combat_mana_cout_soin` mana, plafonné à
`pvMax`, **sans riposte**. Utilisable **à terre** (PV=0). `RollSoin` lit `_soinMax`/`_soinBonus` (sous-classe sinon classe).
```
Trigger : Command Triggered → !soin
```

#### `!acheter` → `Commandes/Acheter/commande_acheter.cs`
Achète une **Potion** au marchand. Actif tant que `offreEnAttente == "marchand_soin"`. Vérifie
`marchand_prix_potion` (config_allies) et `max_sac`. **Indépendant** de `!accepter` (soin) → deux choix séparés.
```
Trigger : Command Triggered → !acheter
```

> ⚠️ **Déprécié** : `combat_attaque.cs` (`!attaque`) et `combat_defense.cs` (`!defense`) ne contiennent plus qu'un
> message de redirection vers `!combat`/`!discuter`/`!fuir`. Triggers à retirer quand plus personne ne les tape.

### `!inventaire` → `Commandes/Inventaire/commande_inventaire.cs`
Affiche le sac et les slots équipés en 2 messages.
- Message 1 : `NomJoueur — Sac (N) : item1 · item2 · ...` (vide si rien)
- Message 2 : `NomJoueur — Équipé : Arme X · Armure X · Accessoire X` (slots vides = rien)
- Lit `inventaire` via `LireValeurString` (CSV entre guillemets)
```
Trigger : Command Triggered → !inventaire
```

### `!equiper [nom_item]` → `Commandes/Equiper/commande_equiper.cs`
Équipe un item du sac dans son slot (arme/armure/accessoire).
- Swap : item du sac → slot, ancien item équipé → sac (taille inchangée)
- Lit `_slot` depuis `config_items.json` → détermine le champ (`armeEquipee`, `armureEquipee`, `accessoireEquipe`)
- Bloqué si `enCombat == true`
- Refuse les consommables et items de vente (redirect vers `!utiliser` / `!vendre`)
- Recherche insensible à la casse, stocke le nom exact du config
```
Trigger : Command Triggered → !equiper
```

### `!vendre [nom_item]` → `Commandes/Vendre/commande_vendre.cs`
Vend un item du sac ou d'un slot équipé contre des RAM.
- Cherche d'abord dans l'inventaire (CSV), puis dans les slots équipés
- Lit `_prixVente` depuis config (défaut 5 RAM)
- Bloqué si `enCombat == true`
```
Trigger : Command Triggered → !vendre
```

### `!utiliser [nom_item]` → `Commandes/Utiliser/commande_utiliser.cs`
Consomme un item du sac (`_slot == "consommable"`).
- Lit `_pvSoin` et `_manaSoin` depuis config, applique plafonnés à pvMax/manaMax
- Retire **un seul** exemplaire du sac (flag `dejaRetire`)
- Fonctionne en et hors combat
- Cache le message mana si `manaMax == 0`
```
Trigger : Command Triggered → !utiliser
```

### `!abandon` → `Commandes/Abandon/commande_abandon.cs`
Abandonne la quête en cours.
- Bloqué si `enCombat == true` (fuir ou se battre d'abord)
- Réinitialise tous les champs quête (rencontre, pause, offres, events)
- Applique `quete_cooldown_abandon_secondes` (config_global, 5 min par défaut)
- Plus court que le cooldown de défaite (10 min) mais quand même pénalisant
```
Trigger : Command Triggered → !abandon
```

### `!racine` → `Commandes/Secret/commande_secret.cs`
Commande secrète — **non documentée dans le jeu**.
- Donne `Ecaille-de-Pointu` (meilleur accessoire : +3 atq, +2 CA)
- Une seule fois par joueur (suivi dans `Donnees/secret_recu.txt`)
- Le mot `racine` se reconstitue via 6 morceaux d'écorce (Ecorce-R/A/C/I/N/E)
- Les écorces droppent aléatoirement en quête (20%, lettres manquantes seulement)
```
Trigger : Command Triggered → !racine
```

### `!classement` → `Commandes/Classement/commande_classement.cs`
Affiche le top 5 des aventuriers — **broadcaster uniquement** (lu depuis `config_global.json → broadcaster`).
- Scanne tous les joueurs avec `classeChoisie == true`
- Trie par XP décroissant (tri à bulles)
- Affiche : rang · pseudo · niveau · XP · victoires combat · quêtes terminées
- 1 message d'en-tête + 1 message par joueur (max 6 messages)
```
Trigger : Command Triggered → !classement
```

### `!accepter` / `!refuser` → `Commandes/Accepter|Refuser/`
Répondent aux offres interactives en quête. Vérifient `offreEnAttente` et `offreExpire`.

**Vieux Sage (`offreEnAttente == "vieux_sage"`) :**
- `!accepter` → double roll indépendant : `vieux_sage_chance_xp`% → +XP (`offreValeur`) ;
  `vieux_sage_chance_perte_item`% → perd 1 item aléatoire (les deux peuvent arriver ensemble).
- `!refuser` → `vieux_sage_chance_combat`% → **pose une rencontre** contre le `Vieux-Sage`
  (3 choix `!combat`/`!discuter`/`!fuir` + `rencontreExpire`) ; sinon il disparaît sans effet.

**Marchand (`offreEnAttente == "marchand_soin"`) :**
- `!accepter` → soigne les PV (`offreValeur`, plafonné à pvMax) · `!refuser` → annule · `!acheter` → Potion (**choix séparé**).
- L'offre reste posée même si soin = 0 (PV pleins), pour autoriser `!acheter`.

> Stats de combat du Vieux-Sage : `config_ennemis.json` (`Vieux-Sage_*`, palier `fort`).
> % de l'offre : `config_allies.json` (`vieux_sage_*`).
```
Trigger : Command Triggered → !accepter / !refuser
```

### `!duel @cible` → `Commandes/Duel/commande_duel.cs`
Duel **amical** entre deux joueurs (aucune perte de PV). Le challenger pose un défi ; la cible répond via
`!accepter` (résolution du combat) ou `!refuser` (annulation).

**`!duel` (pose le défi)** — vérifie, dans l'ordre :
- Les deux ont `classeChoisie == true`
- Cible parsée depuis `args["rawInput"]` (`@` retiré), existe, **≠ soi-même**, pas déjà défiée (`duelDe` vivant)
- Challenger hors cooldown (`duelCooldownFin`), sans défi sortant déjà en cours (`duelVers`, nettoyé si expiré)
- Les deux « dans l'Antre » : `enQuete == false`, `enCombat == false`, `pvActuels > 0`
- **Niveau** : `niveauCible == niveauChallenger` **ou** `niveauChallenger + 1` (même niveau ou 1 au-dessus)
- Pose `duelDe`/`duelExpire` sur la cible + `duelVers` sur le challenger (`duel_expire_secondes`, 60s)

**Résolution (dans `!accepter`, méthode `ResoudreDuel`)** — réutilise la formule de puissance de `!combat`
(`ScorePuissance` = `combat_base_pct + Tranche(pv/ca/atk/mana/cha/agi/niveau) + (nbAtq-1)*combat_attaques_pct + items`,
clampé `combat_plancher_joueur`..`combat_plafond_joueur`). Vainqueur tiré au sort :
```
probaChallenger = scoreA * 100 / (scoreA + scoreB)   →   rng.Next(100) < probaChallenger
```
- Vainqueur : `+duel_recompense_xp_gagnant` (100) XP, `duelsGagnes++`
- Perdant : `+duel_recompense_xp_perdant` (50) XP, `duelsPerdus++`
- Montée de niveau vérifiée pour les deux · marqueurs de duel nettoyés sur les deux profils
- **Cooldown 1h posé sur le challenger uniquement à l'acceptation** (`duel_cooldown_secondes`) — refus/expiration ⇒ aucun cooldown
- Re-validation « dans l'Antre » des deux au moment de l'acceptation (sinon duel annulé)

**`!refuser`** : nettoie `duelDe`/`duelExpire` (cible) + `duelVers` (challenger), aucun cooldown.
> Champs config : `config_global.json` (`duel_expire_secondes`, `duel_cooldown_secondes`,
> `duel_recompense_xp_gagnant`, `duel_recompense_xp_perdant`).
> Nouveaux champs JSON joueur (via `EnsureChamp`) : `duelDe`, `duelVers`, `duelExpire`, `duelCooldownFin`,
> `duelsGagnes`, `duelsPerdus`.
```
Trigger : Command Triggered → !duel
```

### Channel Point Rewards

| Fichier | Reward Twitch | Coût | Logique |
|---------|--------------|------|---------|
| `1d6_PV.cs` | 🎲 Jet de dé — PV | 2 000 | pvMax = pvBase (config) + 1d6 |
| `1d4_CA.cs` | 🎲 Jet de dé — CA | 2 000 | CA = caBase (config) + 1d4 |
| `+2_PV.cs` | ⭐ Boost +2 — PV | 20 000 | pvMax += 2 (stack permanent) |
| `+2_CA.cs` | ⭐ Boost +2 — CA | 20 000 | CA += 2 |
| `+2_Attaque.cs` | ⭐ Boost +2 — Attaque | 20 000 | bonusAttaque += 2 |

> Jet de dé : repart de la BASE classe depuis config, ne stack pas.
> Boost +2 : s'empile sur tout.
```
Trigger : Channel Point Reward (un fichier par reward)
```

### `Timer_XP_Visionnage.cs` + `tracker_activite.cs` — Timer 15 min

`tracker_activite.cs` est déclenché sur **Twitch Chat Message** (tout message, pas une commande).
Il enregistre `derniereActivite = maintenant` dans le JSON du joueur à chaque fois qu'il écrit.

`Timer_XP_Visionnage.cs` parcourt les joueurs avec classe et vérifie que `derniereActivite`
est récent (`< timer_activite_seuil_secondes` = 30 min). Seuls les viewers actifs gagnent :
+5 XP, vérification montée de niveau.
Régénération passive si `enCombat != true` : +2 PV (plafonné pvMax) + +3 Mana (plafonné manaMax).
```
Trigger : Timed Action → Timer_XP_Visionnage (900s, repeat)
Trigger : Twitch Chat Message → tracker_activite (tout message)
```

### Boss communautaire (arène) — `Streamerbot/Boss/`

Combat **tour par tour** partagé, indépendant des quêtes. État dans `etat_global.json` (un seul boss à la fois).

**Déroulé :**
1. `!spawnboss [nom]` (broadcaster) → phase **recrutement** (`arene_recrutement_secondes`, 5 min), active le timer `ArenaCheck`.
2. `!arene` → un joueur rejoint (vivant + classe choisie) pendant le recrutement.
3. Fin du recrutement (`arena_check.cs`) → **ordre d'initiative** = agilité décroissante, égalité = ordre d'arrivée (tri stable) ;
   PV boss = `<boss>_pv` + `boss_pv_par_participant` × nb joueurs.
4. `!attaquer` → chacun frappe à son tour (refus si pas son tour). AFK > `arene_tour_timeout_secondes` (2 min) → saut auto.
   Dégâts joueur = `boss_degats_base` + (atk+niveau)×nbAttaques + alea.
5. Après le dernier joueur → **riposte boss** (`arena_check.cs`) : total = `<boss>_degatsMax` × nb joueurs,
   réparti **inversement à la CA effective** (CA haute = encaisse moins). PV à 0 = tombé, retiré de l'ordre.
6. Boucle jusqu'à boss mort ou groupe anéanti.
   - **Victoire** (`commande_attaquer.cs`) : XP/RAM de base à TOUS les participants ; bonus + loot `boss_loot_pool` au **meilleur dégâteur**.
   - **Défaite** : tous à terre, aucune récompense.

```
Triggers : Command → !spawnboss (broadcaster) · !arene · !attaquer
           Timed Action → ArenaCheck (~10-15s, repeat ; démarre désactivé, activé/désactivé par le code)
```

---

## Checklist pour écrire un nouveau fichier

1. `using System;` + `using System.IO;` en tête
2. Constantes en tête (selon besoin) :
   ```csharp
   private const string DOSSIER_JOUEURS = @"C:\Users\Florian\pjt\Pointu-PJT\Donnees\joueurs";
   private const string CONFIG_CLASSES  = @"C:\Users\Florian\pjt\Pointu-PJT\Donnees\config_classes.json";
   private const string CONFIG_ENNEMIS  = @"C:\Users\Florian\pjt\Pointu-PJT\Donnees\config_ennemis.json";
   private const string CONFIG_ITEMS    = @"C:\Users\Florian\pjt\Pointu-PJT\Donnees\config_items.json";
   private const string CONFIG_QUETES   = @"C:\Users\Florian\pjt\Pointu-PJT\Donnees\config_quetes.json";
   private const string CONFIG_GLOBAL   = @"C:\Users\Florian\pjt\Pointu-PJT\Donnees\config_global.json";
   private const string CONFIG_LEVEL    = @"C:\Users\Florian\pjt\Pointu-PJT\Donnees\config_level.json";
   private const string CONFIG_ALLIES   = @"C:\Users\Florian\pjt\Pointu-PJT\Donnees\config_allies.json";
   ```
   > ⚠️ **Ne jamais hardcoder** de valeurs numériques de jeu — lire depuis `config_global.json`.
3. Vérifier `File.Exists(cheminFichier)` en premier → message + `return true`
4. Vérifier `classeChoisie == "true"` avant toute action de jeu
5. Vérifier `enCombat` et `enQuete` selon la logique
6. **Ne jamais hardcoder de stats de classe/sous-classe/ennemi** → lire depuis config
7. Toujours `File.WriteAllText(cheminFichier, json)` après modification
8. Après modification de `experience` → vérifier `CalculerNiveau` + `AppliquerBonusNiveau`
9. Copier les méthodes utilitaires en bas du fichier (selon besoin) :
   - `LireValeur` / `ModifierValeur` / `AjouterValeur` → toujours
   - `LireValeurString` / `ModifierValeurString` → si on lit/écrit `inventaire` (CSV entre guillemets)
   - `GetBonusItems` → dans tous les fichiers combat et dans les commandes item
10. Utiliser `nomJoueur.ToLower()` pour le nom du fichier
11. Ne jamais utiliser `CPH.Wait()` dans un flux de quête — bloque Streamer.bot

---

## Système d'items ✅

### `config_items.json` — Structure

Clés format plat : `"NomItem_stat": valeur`

**Items équipables** (`_slot: "arme"` | `"armure"` | `"accessoire"`) :
```
_slot          ← détermine le champ joueur cible (armeEquipee / armureEquipee / accessoireEquipe)
_rarete        ← rareté : "commun" | "rare" | "epique" | "legendaire"
_attaqueBonus  ← bonus d'attaque (entre dans le calcul de !combat)
_caBonus       ← bonus de CA (entre dans le calcul de !combat)
_manaBonus     ← bonus mana
_charismeBonus ← bonus charisme (entre dans le calcul de !discuter)
_poids         ← poids (armures lourdes) → RÉDUIT la réussite de !fuir (défaut 0 si absent)
_prixVente     ← RAM obtenus à la vente
_description   ← texte affiché dans !inventaire
```

**Consommables** (`_slot: "consommable"`) :
```
_rarete
_pvSoin    ← PV restaurés (direct, pas un dé)
_manaSoin  ← Mana restauré
_prixVente
```

**Items de vente seule** (`_slot: "vente"`) :
```
_rarete
_prixVente
```

### Items disponibles

| Nom | Slot | Rareté | Bonus | Prix vente |
|-----|------|--------|-------|-----------|
| Lame-de-Pointu | arme | commun | +2 atq | 100 RAM |
| Croc-de-Loup | arme | commun | +2 atq | 75 RAM |
| Sceptre-de-L'Antre | arme | commun | +2 atq, +5 mana | 120 RAM |
| Marteau-Carapace | arme | commun | +1 atq, +1 cha | 150 RAM |
| Luth-01 | arme | commun | +1 cha | 200 RAM |
| Arc-Fleau | arme | commun | +3 atq | 180 RAM |
| Armure-d'ecorce | armure | commun | +2 CA, +2 cha | 150 RAM |
| Armure-de-feuille | armure | commun | +1 CA, +1 cha | 120 RAM |
| Robe-de-code | armure | commun | +1 CA, +5 mana | 130 RAM |
| Apparat-Système | armure | commun | +2 CA, +1 cha | 160 RAM |
| Armure-renforcée | armure | commun | +3 CA | 200 RAM |
| Bague-de-protection | accessoire | commun | +1 CA | 80 RAM |
| Cape-de-furtivité | accessoire | commun | +2 cha | 90 RAM |
| Amulette-de-mana | accessoire | commun | +5 mana | 100 RAM |
| Gants-de-force | accessoire | commun | +2 atq | 110 RAM |
| Chapeau-de-charisme | accessoire | commun | +3 cha | 120 RAM |
| Potion | consommable | commun | +8 PV, +10 Mana | 3 RAM |
| Morceau-Arbre-Serveur | vente | commun | — | 50 RAM |
| Ligne-Reseau | vente | commun | — | 25 RAM |
| Ecorce-R/A/C/I/N/E | vente | commun | — | 5 RAM |
| Ecaille-de-Pointu | accessoire | **legendaire** | +3 atq, +2 CA, +5 mana, +2 cha | 999 RAM |

### Inventaire joueur
- Champ `inventaire` : CSV entre guillemets (`"item1,item2,item3"`)
- Max 8 items
- Slots équipés : `armeEquipee`, `armureEquipee`, `accessoireEquipe` (string, hors inventaire)
- Utiliser `LireValeurString` / `ModifierValeurString` pour lire/écrire `inventaire`
- Utiliser `LireValeur` normal pour les slots équipés (une seule valeur, pas de CSV)

### `GetBonusItems` — méthode helper combat

```csharp
private int GetBonusItems(string json, string stat)
{
    string   cfgItems = File.ReadAllText(CONFIG_ITEMS);
    string[] slots    = { "armeEquipee", "armureEquipee", "accessoireEquipe" };
    int total = 0;
    foreach (string slot in slots)
    {
        string item = LireValeur(json, slot);
        if (item != "" && item != "0")
            total += int.Parse(LireValeur(cfgItems, item + "_" + stat));
    }
    return total;
}
```

### Méthodes string pour l'inventaire CSV

```csharp
private string LireValeurString(string json, string cle)
{
    string marqueur = "\"" + cle + "\": \"";
    int posDebut    = json.IndexOf(marqueur);
    if (posDebut == -1) return "";
    posDebut       += marqueur.Length;
    int posFin      = json.IndexOf("\"", posDebut);
    if (posFin == -1) return "";
    return json.Substring(posDebut, posFin - posDebut);
}

private string ModifierValeurString(string json, string cle, string val)
{
    string marqueur = "\"" + cle + "\": \"";
    int posDebut    = json.IndexOf(marqueur);
    if (posDebut == -1) return json;
    posDebut       += marqueur.Length;
    int posFin      = json.IndexOf("\"", posDebut);
    if (posFin == -1) return json;
    return json.Substring(0, posDebut) + val + json.Substring(posFin);
}
```

> ⚠️ `LireValeur` s'arrête à la première `,` ou `\n` — elle **casse** les valeurs CSV.
> Toujours utiliser `LireValeurString` pour `inventaire`.

---

## Discord Bot

Bot Python `discord.py` déployé sur **Discloud** (plan gratuit).
Lit les profils depuis le repo GitHub public.

**Commandes** : `!profil`, `!arbonet`, `!aide`
**Channel** : `CHANNEL_ID = 1490232175382102016`

> ⚠️ `DISCORD_TOKEN` hardcodé dans `bot_discord.py` — **ne jamais pusher ce fichier sur GitHub**

**Déploiement** : zipper `bot_discord.py` + `discloud.config` + `requirements.txt` → uploader sur discloud.app

---

## Lore (résumé)

- **Arbonet** : monde nature + technologie hybride (chênes-serveurs, créatures cyber)
- **Pointu** : tortue ancienne, gardienne de l'Antre, NPC principal
- **Hector-Pierre Castor** : antagoniste, détruit les chênes-serveurs
- **Ram** : monnaie du jeu (mémoire vive d'Arbonet)
- **Fragment de Carapace** : artefact qui sauvegarde le profil joueur (lore du JSON)

---

*Projet Pointu © Florian alias kikaby67 — 2026*
