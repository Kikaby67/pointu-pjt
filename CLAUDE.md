# CLAUDE.md
> Guide technique complet pour Claude Code (VS Code / claude.ai/code)
> Projet Twitch bot game — Streamer.bot + C# inline

---

## Vue d'ensemble du projet

**Pointu-PJT** est un mini-jeu RPG textuel dans le chat Twitch.
Les viewers tapent des commandes (`!rejoindre`, `!quete`, `!combat`...).
Chaque joueur a un fichier JSON sur le disque local. Pas de base de données.

**Machine** : Windows 11, AMD Ryzen 5 5600X, 32 Go RAM
**Stack** : Streamer.bot (C# inline), JSON fichiers plats, .NET 10.0 (prototypage)
**Repo GitHub** : https://github.com/Kikaby67/Pointupjt (public) — remote `origin`, branche `main`

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
│   │   ├── Equipement/          # !equipement (lecture seule)
│   │   ├── Vendre/              # !vendre
│   │   ├── Utiliser/            # !utiliser
│   │   ├── Abandon/             # !abandon
│   │   ├── Accepter/            # !accepter
│   │   ├── Refuser/             # !refuser
│   │   ├── OuvrirDesert/        # !ouvrirdesert (broadcaster — filet si le reward n'est pas échangé)
│   │   ├── NonMerci/            # !nonmerci (refus du fragment → proposition boss)
│   │   └── Secret/              # !racine (commande secrète)
│   ├── quetes/
│   │   ├── quest_system.cs      # !quete (lancer / consulter)
│   │   └── quest_timer.cs       # Timer QuestCheck (30s, auto-résolution + rencontres + mini-boss)
│   ├── combat/
│   │   ├── commande_combat.cs   # !combat (rencontre + mini-boss)
│   │   ├── commande_discuter.cs # !discuter (rencontre)
│   │   ├── combat_fuir.cs       # !fuir (rencontre)
│   │   └── combat_soin.cs       # !soin (HORS combat)
│   ├── Boss/                     # Boss communautaire (tour par tour, étape etat_global)
│   │   ├── spawn_boss.cs        # !spawnboss (streamer) → recrutement
│   │   ├── commande_arene.cs    # !arene → rejoindre le recrutement
│   │   ├── commande_attaquer.cs # !attaquer → frapper à son tour
│   │   ├── commande_defense.cs  # !defense → +CA le temps d'un tour
│   │   ├── arena_check.cs       # Timer ArenaCheck (transition/riposte/AFK/victoire)
│   │   └── Capacites/           # 2 capacités par classe (voir section Boss)
│   │       ├── egidebinaire.cs · provocation.cs           (Hexadécimeur)
│   │       ├── zeroday.cs · soudoyer.cs                   (Cryptolame)
│   │       ├── bouledefeu.cs · surcharge.cs               (Hackmancien)
│   │       ├── restoreup.cs · anatheme.cs                 (Firewaller)
│   │       └── dansesensuelle.cs · serenadeinsultante.cs  (Algorythmancien)
│   ├── Timer_Xp/
│   │   ├── Timer_XP_visionnage.cs
│   │   └── tracker_activite.cs      # Twitch Chat Message → met à jour derniereActivite
│   ├── Moderation/                  # HORS jeu RPG — "Prison de Sagesse" phase A
│   │   ├── chat_logger.cs           # log complet du chat → JSONL (hors repo)
│   │   └── timeout_manuel.cs        # timeout 5 min piloté par Touch Portal
│   └── Reward/
│       ├── Jet de dé/           # 1d6_PV.cs, 1d4_CA.cs
│       ├── Bonus +2/            # +2_PV.cs, +2_CA.cs, +2_Attaque.cs
│       ├── Caravane/            # caravane_desert.cs (ouvre le Désert + jeton d'achat)
│       └── DonationGoal/        # suggestion_donationgoal.cs (Suggestion de Donation Goal → DonationGoal.txt)
├── Donnees/
│   ├── joueurs/                 # Un .json par joueur (ex: kikabygaming.json) — GIT-IGNORÉ (état de jeu mouvant)
│   ├── config_classes.json      # ★ Source unique : stats classes + sous-classes
│   ├── config_ennemis.json      # ★ Source unique : stats tous les ennemis
│   ├── config_items.json        # ★ Source unique : stats tous les items
│   ├── config_quetes.json       # ★ Source unique : quêtes (format quete001_*)
│   ├── config_global.json       # ★ Source unique : constantes de jeu (cooldowns, %, seuils)
│   ├── config_level.json        # ★ Source unique : seuils XP et bonus de niveau
│   ├── config_allies.json       # ★ Source unique : paramètres alliés/marchands
│   ├── secret_recu.txt          # Liste des joueurs ayant reçu l'Ecaille-de-Pointu
│   ├── refus_fragment.txt       # Viewers ayant fait !nonmerci — GIT-IGNORÉ (pseudos de tiers)
│   └── etat_global.json         # État partagé du boss communautaire (arène)
├── Docs/
│   ├── STREAMERBOT_ACTIONS_JSON.md  # Injecter une action dans SB en éditant actions.json (sans kika-sync)
│   └── PANNEAU_TWITCH.md
├── Lore/
│   ├── LA_LEGENDE_DE_POINTU_V2.md
│   ├── LORE_ARBONET_APPROFONDI.md   # ★ V3 : familles, bestiaire, quêtes, récompenses
│   ├── ZONES_ALLIES_ENNEMIS.md      # ★ CARTE DE RÉFÉRENCE : 6 zones, alliés, boss, langue commune
│   ├── FICHES_CLASSES.md
│   └── BESTIAIRE.md
├── .claude/agents/
│   └── gardien-lore.md              # ★ Agent de cohérence narrative — à lancer avant d'écrire du lore
├── PLAN_IMPLEMENTATION_V3.md        # ★ V3 : ordre d'exécution en 6 phases — À LIRE EN PREMIER
├── RESTRUCTURATION_POINTU_V3.md     # ★ V3 : architecture des messages (Twitch court / Discord détaillé)
├── ECONOMIE_ET_BOUTIQUE_V3.md       # ★ V3 : RAM ×5, items 4 paliers, boutique, échange
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

> 📄 **Déployer un `.cs` dans Streamer.bot** : l'outil `Tools/kika_sync.py` (synchro auto).
> Pour comprendre le format `actions.json` et injecter une action à la main sans outil, voir
> [`Docs/STREAMERBOT_ACTIONS_JSON.md`](Docs/STREAMERBOT_ACTIONS_JSON.md).

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
moderation_timeout_secondes (300) ← durée du timeout manuel (hors jeu RPG, cf. Modération)

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
rencontre_boss                 ← CSV des boss d'arène, 1 par zone (Sanglier-Virus,Faucon-Firewall,Loutre-Rootkit,
                                 Ours-Hexadecimeur,Vautour-Rootkit,Crocodile-Firewaller,Hector-Pierre Castor)
caravane_achats_par_ouverture (1) ← jetons d'achat accordés par redemption du reward Caravane
zone_ordre                     ← ordre canonique des 6 zones (Arbonet,Plaines,Lacs,Montagne,Désert,Marais)
zone_<nom>_niveau_min          ← niveau d'accès : arbonet 1 · plaines 3 · lacs 5 · montagne 6 · desert 7 · marais 9
arene_recrutement_secondes (300) ← durée de la phase !arene (5 min)
arene_tour_timeout_secondes (120) ← délai avant saut de tour AFK (2 min)
boss_pv_par_participant (40)   ← PV ajoutés au boss par combattant
boss_degats_base/alea (5/6)    ← dégâts d'un joueur sur le boss (+ (atk+niveau)*nbAttaques)
boss_recompense_base_xp/ram (50/100) ← récompense à TOUS les participants
boss_top_bonus_xp/ram (100/250) ← bonus au meilleur dégâteur
boss_loot_pool (loot_legendaire) ← loot du meilleur dégâteur

— !combat : système de PUISSANCE (26/08/2026) —
combat_poids_pv (1) · _ca (3) · _atk (5) · _niveau (4) · _mana (1, appliqué par tranche de 10) ·
  _charisme (1) · _agilite (1)              ← poids de chaque stat dans la puissance
combat_puissance_par_attaque (20)           ← par attaque au-delà de la 1re (sous-classes)
combat_puissance_compagnon (45)             ← le compagnon est de la PUISSANCE, plus un bonus % plat
combat_socle (107)                          ← puissance d'un niv 1 nu : point 50 % de la courbe
combat_base_mille (500) · combat_pente_num/den (43/25)   ← chance‰ = base + (puiss − socle) × num ÷ den
combat_mille_min/max (100/950)              ← bornes, en millièmes
combat_tier_<faible|moyen|fort|miniboss>_mille (1000 / 0 / -200 / -350)   ← palier ennemi, en millièmes
combat_pv_perte_diviseur (4) · combat_pv_perte_echec_facteur (3) · combat_pv_perte_alea (2)

— !fuir —
fuite_base_pct (30) · fuite_agilite_pct (3) · fuite_poids_pct (6) · fuite_min/max (10/95) · agilite_defaut (8)

— !discuter —
discuter_base_pct (25) · discuter_charisme_pct (3) · discuter_min/max (10/90)

— création (!choisirclasse) —
creation_pv_de (6) · creation_ca_de (4) · creation_atq_de (4)

— replis —
ennemi_ca_defaut (12) · ennemi_degats_defaut (6) · ennemi_xp_defaut (15) · ennemi_ram_defaut (15) · soin_max_defaut (4)

✅ Clés obsolètes de l'ancien combat tour par tour (combat_defense_bonus_ca, combat_fuite_seuil_normal,
   combat_fuite_seuil_cryptolame, attaque_degats_defaut, attaque_des_defaut) : **supprimées** (audit v2.2.1).
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
_zone            ← zone d'appartenance (Arbonet/Plaines/Lacs/Montagne/Désert/Marais ; "" = hors zone, ex. Vieux-Sage)
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
  "participants": "",       // CSV "pseudo:degatsCumulés" (pour le top dégâts)

  "buffCaTours": 0,         // >0 : +CA groupe actif (Égide) pour N rounds
  "buffAtkTours": 0,        // >0 : +attaque groupe (Surcharge)
  "buffDesTours": 0,        // >0 : +jets groupe (Danse sensuelle)
  "bossCaMalusTours": 0,    // >0 : armure boss réduite (Anathème) → +dégâts par attaque
  "bossAtkMalusTours": 0,   // >0 : attaque boss réduite (Sérénade) → riposte -%
  "defenseurs": "",         // CSV des pseudos ayant fait !defense ce round (+CA à la riposte)
  "provocateur": "",        // pseudo qui a fait !provocation (encaisse seul la riposte)
  "bribeSafe": "",          // pseudo soudoyé avec succès (épargné par la riposte)
  "bribeCible": "",         // pseudo dont le !soudoyer a échoué (riposte focalisée sur lui)

  "desertDecouvert": false  // le Désert est-il ouvert à tout le chat ? (reward Caravane ou !ouvrirdesert)
}
```

Lu/écrit par les fichiers `Streamerbot/Boss/` (+ `combat/` pour !soin/!discuter/!fuir). Un seul boss à la fois.
Les compteurs `*Tours` sont décrémentés à chaque riposte ; les champs de ciblage (`defenseurs`,
`provocateur`, `bribeSafe`, `bribeCible`) sont remis à zéro après chaque riposte.
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

> ♻️ **Refonte du 25/08/2026.** Cinq sous-classes sur dix n'avaient **aucun effet** (leurs clés
> `degatsMax`/`nbDes`/`buffAttaque`/`auraDefense` n'étaient lues par aucun code — vestiges du combat
> tour par tour), et **Pointeur-Null était un piège** : `nbAttaques: 1` contre 2 pour un Cryptolame de
> base, soit un downgrade. Toutes ont désormais **un effet solo ET un effet d'arène**, tous deux lus
> par du code réel. Les 14 clés mortes ont été supprimées.

| Classe | Sous-classe | Effet SOLO | Effet ARÈNE |
|---|---|---|---|
| Hexadécimeur | **Bloc-Hex** | +8 PV max | `!egidebinaire` +2 tours |
| Hexadécimeur | **Surcharge** | 2 attaques, −2 CA | (via nbAttaques) |
| Cryptolame | **Byte-Fantôme** | 3 attaques | (via nbAttaques) |
| Cryptolame | **Pointeur-Null** | Arc-Binaire, **2 attaques**, +15% à `!fuir` | `!zeroday` facteur crit 2→4 |
| Hackmancien | **Faille-Zéro** | +8% à `!combat` | `!bouledefeu` +15 dégâts |
| Hackmancien | **Compilateur** | +15% à `!discuter` | `!surcharge` +2 tours |
| Firewaller | **Protocole-Sacré** | +2 CA | `!restoreup` +8 PV |
| Firewaller | **Serment-Binaire** | +8% à `!combat` | `!anatheme` +2 tours |
| Algorythmancien | **Barde-Binaire** | +15% à `!discuter` | `!dansesensuelle` +2 tours |
| Algorythmancien | **Patch-Mélodique** | soin 1d8+3 | `!soin` en arène |

**Les clés d'effet** (`config_classes.json`, une par sous-classe) :
```
pvMaxBonus · caModif · typeArme        ← appliques UNE FOIS a la selection (graves dans le profil)
nbAttaques                              ← !combat, !attaquer, !zeroday, duel
bonusCombatPct                          ← ajoute au score de !combat ET au duel (ScorePuissance)
bonusDiscuterPct / bonusFuitePct        ← ajoutes a !discuter / !fuir
soinMax / soinBonus                     ← RollSoin
<capacite>Bonus                         ← lu par le FICHIER de la capacite (egide/zeroday/bouledefeu/
                                          surcharge/restoreup/anatheme/danse)
_libelle                                ← texte affiche au joueur — DOIT decrire ce que le code fait
_lore                                   ← phrase d'ambiance
<Classe>_sousClasses                    ← CSV des 2 options (pilote la validation ET l'affichage)
```

> ⚠️ Une capacité qui pose un **compteur** dans `etat_global` (surcharge, danse, anathème, égide)
> ne peut pas porter de magnitude par joueur : la valeur est relue ailleurs, hors contexte du lanceur.
> Ces sous-classes allongent donc la **durée**, pas la puissance. Celles dont la capacité calcule sa
> valeur sur place (bouledefeu, restoreup, zeroday) portent bien une magnitude.

**Bonus de sélection** (pvMaxBonus, caModif, typeArme) : appliqués une fois, chang er le config
n'affecte pas les joueurs déjà spécialisés. **Tout le reste** est lu à l'exécution — rééquilibrer
le config affecte immédiatement tous les joueurs.

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
| 8 | 34 000 | +500 Ram |
| 9 | 48 000 | +3 PV max |
| 10 | 64 000 | +2 Charisme |

### Annonce de montée de niveau

**Toute** source d'XP doit appeler `VerifierMonteeNiveau` puis annoncer via `MessageNiveau` — le joueur
reçoit son message quel que soit son état (en quête, en repos, en combat, en arène, hors ligne côté timer).

Format unique (méthode `MessageNiveau`, à copier telle quelle dans chaque fichier) :
```
🎉 {pseudo} gagne 1 niveau (niveau {N}), augmente sa stat de {niveau_N_message} et progresse vers le sommet !
```
- Palier **sans gain de stat** (niveau 5) : `🎉 {pseudo} gagne 1 niveau (niveau 5) et progresse vers le sommet ! Sous-classe débloquée ! Tape !sousclasse !`
- Palier `niveauMax` : suffixe ` 👑 Niveau maximum atteint !` ajouté automatiquement (ne pas le remettre dans `niveau_N_message`).

Le libellé du bonus vient de `config_level.json → niveau_N_message` ; la présence d'un gain de stat est
déduite de `_pvBonus`/`_caBonus`/`_ramBonus`/`_charismeBonus`. **Zéro texte de bonus en dur dans le code.**

Les 7 fichiers concernés : `quest_system.cs`, `quest_timer.cs`, `commande_combat.cs`, `commande_discuter.cs`,
`commande_accepter.cs` (Vieux Sage + duel), `arena_check.cs`, `Timer_XP_visionnage.cs`.

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
        case 8:  json = AjouterValeur(json, "ram",      500); break;
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
> Boss d'arène (`rencontre_boss`, tier `boss`) : 1 par zone — voir la table plus bas.
> Chaque ennemi porte un `_zone` (6 zones) ; le filtrage par zone n'est **pas encore lu par le code**.
> Vieux-Sage (`fort`) reste réservé à son événement (offre du Vieux Sage).

### Ennemis de rencontre de quête

| Nom | Zone | Tier | XP | RAM |
|-----|------|------|-----|-----|
| Drone-racine | Arbonet | faible | 15 | 3 |
| Parasite de données | Arbonet | moyen | 18 | 4 |
| Martre-Trojan | Plaines | moyen | 20 | 4 |
| Taupe-Malware | Plaines | moyen | 20 | 4 |
| Ombre de la mémoire | Plaines | moyen | 25 | 5 |
| Écrevisse-Cache ✱ | Lacs | faible | 16 | 3 |
| Brochet-Injection ✱ | Lacs | moyen | 24 | 5 |
| Marmotte-Veille ✱ | Montagne | faible | 18 | 4 |
| Lynx-Proxy ✱ | Montagne | moyen | 28 | 6 |
| Fennec-Spoof ✱ | Désert | faible | 18 | 4 |
| Scorpion-Malware | Désert | moyen | 22 | 5 |
| Serpent-Phishing | Marais | moyen | 24 | 5 |
| Sentinelle du Castor | Marais | moyen | 30 | 6 |
| Sanglier-Crash | Marais | fort | 22 | 5 |

> ✱ **Noms provisoires**, inventés pour que les zones Lacs/Montagne/Désert aient des rencontres —
> à valider ou renommer. Seul `rencontre_ennemis` (config_global) dépend de ces noms.

### Mini-boss (solo, en quête — tier `miniboss`)

| Nom | Zone | XP | RAM |
|-----|------|-----|-----|
| Insecte-Bug | Arbonet | 10 | 2 |
| Corbeau-Daemon | Plaines | 25 | 5 |
| Castor-Rootkit | Lacs | 40 | 8 |
| Loup-Firewall | Montagne | 60 | 12 |
| Mirage-Bug | Désert | 50 | 10 |
| Grenouille-Corrompue ♥ | Marais | 70 | 14 |

> ♥ **Sauvable par la parole.** `Grenouille-Corrompue_discuterSauve: "true"` dans `config_ennemis.json` :
> un `!discuter` réussi ne la recrute pas, il la **libère de la corruption** — c'est une victoire
> (XP + RAM + butin de `_discuterLootPool`, ici `loot_epique`, meilleur que le loot de mini-boss tué).
> `!combat` reste possible : on la tue au lieu de la sauver, avec le butin normal.
> **Le mécanisme est générique** — poser ces deux clés sur n'importe quel ennemi le rend sauvable,
> aucun nom n'est en dur dans le code.

### Boss d'arène (communautaire — tier `boss`)

Un boss par zone (décision 21/08/2026). Reine-Bug, Munin-Daemon et Fenrir-Firewall sont **retirés**.

| Nom | Zone | PV base | degatsMax | XP | RAM |
|-----|------|---------|-----------|-----|-----|
| Sanglier-Virus | Arbonet | 250 | 6 | 60 | 25 |
| Faucon-Firewall | Plaines | 350 | 8 | 85 | 35 |
| Loutre-Rootkit | Lacs | 420 | 9 | 100 | 40 |
| Ours-Hexadecimeur | Montagne | 550 | 11 | 130 | 55 |
| Vautour-Rootkit | Désert | 450 | 10 | 110 | 45 |
| Crocodile-Firewaller | Marais | 600 | 12 | 150 | 60 |
| Hector-Pierre Castor | Marais (final) | 700 | 14 | 200 | 80 |

> PV réels du boss = `_pv` + `boss_pv_par_participant` × nb joueurs. Riposte = `_degatsMax` × nb joueurs.

---

## Commandes implémentées ✅

### `!bonjour` → `Commande_bonjour.cs`
Pointu **propose le fragment de sa carapace** — c'est le choix d'entrée dans Arbonet.
Deux issues annoncées : `!rejoindre` (accepter) ou `!nonmerci` (refuser).
Écrit aussi la **fenêtre d'offre** dans `Donnees/offre_fragment.txt` (`pseudo|timestamp`, git-ignoré,
purge auto au-delà de 24 h). C'est elle qui autorise `!nonmerci` — voir ci-dessous.
```
Trigger : Command Triggered → !bonjour
```

### `!nonmerci` → `Commandes/NonMerci/commande_nonmerci.cs`
Refus du fragment → le viewer est « expulsé d'Arbonet » (narratif), puis reçoit en **message privé**
la proposition d'Hector-Pierre : devenir l'un des boss du jeu (cf. `Lore/ZONES_ALLIES_ENNEMIS.md`).
- **Ne crée aucun profil** et **ne bloque pas** un `!rejoindre` ultérieur — un viewer peut changer d'avis.
- Déjà inscrit → refus poli, le fragment ne se rend pas.
- `CPH.TwitchSendWhisper(..., bot: false)` → le MP part du compte **broadcaster**, pas du bot,
  pour que le viewer puisse répondre à Florian directement.
- Dans un `try/catch` : le whisper Twitch **échoue souvent en silence**
  (compte expéditeur sans vérification téléphonique, destinataire qui n'a jamais écrit au bot).
  La source fiable est donc le journal `Donnees/refus_fragment.txt`
  (`AAAA-MM-JJ HH:MM | pseudo | whisper:ok|echec`), **git-ignoré** (pseudos de tiers, repo public).
**Alias partagés avec la commande son `!no`** (`!no` / `!non` / `!noo` / `!nooo`) : l'action porte
**deux triggers**, le sien et celui de `!no`. Taper `!non` joue donc le son **et** vaut refus — mais
seulement si l'offre est ouverte.

**Garde-fou de la fenêtre d'offre** (`fragment_offre_expire_secondes`, 600 s) : sans lui, chaque `!no`
lancé pour rire enverrait un MP depuis le compte broadcaster à un viewer qui n'a rien demandé.
- Offre ouverte (`!bonjour` il y a moins de 10 min) → refus complet, puis l'offre est **consommée**
  (un seul MP par `!bonjour`).
- Offre absente/périmée → `!nonmerci` tapé explicitement répond « tape !bonjour d'abord » ;
  un alias son (`!no`…) ne dit **rien** et laisse le son seul.
- Distinction faite via `args["command"]` (le mot réellement tapé).
```
Trigger : Command Triggered → !nonmerci  +  !no / !non / !noo / !nooo (trigger de la commande son)
```

### `!rejoindre` → `Commande_rejoindre.cs`
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

### `!choisirclasse [nom]` → `commande_choisirclasse.cs`
Lit les stats depuis `config_classes.json`. Jets de dés à la création.
`args["rawInput"]` contient le nom de la classe en minuscules.
```
Trigger : Command Triggered → !choisirclasse
```

### `!sousclasse [nom]` → `commande_sousclasse.cs`
**Entièrement pilotée par le config** — plus aucune liste ni description en dur dans le code.
- Options lues dans `<Classe>_sousClasses` (CSV), descriptions dans `<SousClasse>_libelle`.
- Palier de déblocage **déduit de `config_level.json`** (le niveau dont le message contient
  « sous-classe ») — déplacer le palier ne demande aucune modification de code. Repli sur 5.
- Saisie **insensible aux accents** : `faille-zero` trouve `Faille-Zéro`.
- À la sélection : affiche l'**AVANT → APRÈS** chiffré (`PV max 25→33`, `CA 14→12`,
  `Arme Épée→Arc-Binaire`) puis une ligne 📜 de lore. C'était le manque signalé par Florian.
- **Déjà spécialisé** : rappelle la sous-classe **et son effet actif** au lieu d'un simple
  « tu as déjà choisi » — seul moyen pour le joueur de revérifier ce que sa voie lui apporte.
- 🐛 **Corrigé** : le message sans argument renvoyait vers `!choisirSousClasse`, **commande qui
  n'existe pas** (l'action n'a que le trigger `!sousclasse`).
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

**72 quêtes** — 6 zones × (1 artefact + 6 secondaires + 5 services), numérotées `quete001`→`quete072`.

| Famille | `_type` | Donneur | Récompense | Répétable |
|---|---|---|---|---|
| **Artefact de mémoire** | `artefact` | **Pointu** uniquement | l'artefact + XP/RAM | non |
| **Secondaire** | `secondaire` | le PNJ nommé de la zone, **par missive dans l'Antre** | 1 item du `_recompensePool` de la zone + XP/RAM | non |
| **Service** | `service` | population anonyme (missive affichée) | XP/RAM seuls | **oui** |

**Champs propres à la V3** (en plus de `_id`/`_type`/`_zone`/`_nom`/`_demandeur`/`_demandeurCle`/`_description`/`_ticks`/`_xp`/`_ram`) :
```
_texteVictoire / _texteEchec   ← texte de résolution PROPRE à la quête (prioritaire sur les
                                 répliques PNJ de config_lore_textes, qui deviennent le repli)
_recompensePool                ← secondaires : pool du palier de la zone (loot_bois → loot_mythique).
                                 Vide pour le Désert, qui paie en RAM au lieu de donner un item.
— artefacts uniquement —
_niveauMin · _quetesMin        ← seuils de déblocage
_equipementComplet             ← "true" : les 3 slots doivent être remplis (n'importe quel palier)
_requiert                      ← `_id` de l'artefact précédent — la chaîne principale
```

**La chaîne des artefacts** : Arbonet (niv 3) → Plaines (5) → Lacs (6) → Montagne (8) → **Désert** (8)
→ Marais (10). Le Désert porte le retournement narratif ; comme il dépend de la découverte de la zone,
`!ouvrirdesert` est le filet qui empêche la chaîne de se bloquer.

Le Désert paie en **RAM** là où les autres zones donnent de l'équipement (son palier, le Légendaire,
ne s'achète que chez Faîne) : ses services rapportent 100 RAM et sont répétables — c'est le moteur
économique du jeu de fin.

> 1 tick = 5 minutes réelles

### Tirage d'une quête — `quest_system.cs` ✅

Le tirage n'est plus aléatoire sur les 72 : il **filtre** puis **pondère**.

**Filtre** (`QueteEligible`) — une quête est écartée si :
- le niveau du joueur est sous `zone_<zone>_niveau_min` ;
- c'est une quête du **Désert** et `etat_global.desertDecouvert != true` (la caravane n'est pas passée) ;
- elle est déjà dans `quetesFaites` **et** n'est pas un `service` (seuls les services se refont) ;
- c'est un **artefact** et l'une de ses conditions manque : `_niveauMin`, `_quetesMin`,
  `_equipementComplet` (les 3 slots remplis, n'importe quel palier), `_requiert` (artefact précédent).

**Pondération** — `quete_poids_zone_courante` (75 %) vers la zone la plus avancée que le niveau
débloque ; les 25 % restants vont aux zones précédentes, qui ne se ferment jamais. Si un seul des
deux paquets est non vide, il prend tout. Aucune quête éligible → message invitant à monter en niveau.

> Le **Désert est hors progression linéaire** (`quete_zone_speciale`) : il n'est jamais « la zone
> courante », ses quêtes arrivent par le paquet des autres zones une fois la caravane passée.

**Paliers de zone** : Arbonet 1 · Plaines 4 · Lacs 6 · Montagne 7 · Désert 6 (+ découverte) · Marais 9.

### Résolution — `quest_system.cs` **et** `quest_timer.cs`

⚠️ La résolution est **dupliquée** dans les deux fichiers (le timer résout tout seul ; `!quete`
résout aussi si le joueur retape la commande après l'échéance). **Toute modification doit être
reportée dans les deux.**

- `_texteVictoire` / `_texteEchec` de la quête sont **prioritaires** ; à défaut, la réplique du
  PNJ (`config_lore_textes`) sert de repli (`TexteResolution`).
- Succès d'une **secondaire** → 1 item tiré dans `_recompensePool` (`RecompenseItem`). Sac plein :
  la récompense est perdue, et le message le dit. Le **Désert** n'a pas de pool — il paie en RAM.
- Succès d'un **artefact** ou d'une **secondaire** → `MarquerFaite` ajoute l'`_id` à `quetesFaites`
  (CSV du profil, créé par `EnsureChamp`). Les **services** ne sont jamais marqués : ils restent farmables.

> ℹ️ **Arme de départ** : `!choisirclasse` équipe déjà `<Classe>_typeArme` dans `armeEquipee`, et ces
> armes (`Épée-octet`, `Double-Dagues`…) **existent** dans `config_items` avec **+1 attaque**. Sous
> l'ancienne formule à division entière ce +1 était invisible ; avec le système de puissance il vaut
> +0,9 %. Le slot arme est donc rempli dès la création — seuls l'armure et l'accessoire manquent pour
> débloquer le premier artefact.
```
Trigger : Command Triggered → !quete
```

### `quest_timer.cs` — Timer QuestCheck (30s)
Parcourt tous les fichiers joueurs. Pour chaque joueur `enQuete == true` (ajoute au passage `rencontreExpire` /
`compagnonActif` aux anciens profils via `EnsureChamp`) :

> 🛡️ Chaque profil est traité dans un `try/catch` (audit v2.2.1) : un JSON corrompu est logué (`CPH.LogWarn`)
> et ignoré, sans bloquer le traitement des autres joueurs.

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
  - `vieux_sage` : offre XP → `!accepter` (double roll XP + risque de perte d'item sac **ou** équipement) / `!refuser` (risque de combat).
    Apparition **pondérée et rare** via `vieux_sage_frequence_pct` (config_allies, défaut 12 %) — les autres alliés se partagent le reste.
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

> ♻️ **Refonte du 26/08/2026 — système de puissance.** L'ancienne formule additionnait 7 « tranches »
> à division entière puis plafonnait à 80. Avec les 6 paliers d'équipement, elle **saturait dès le
> palier Commun** : Rare, Épique, Mythique et Légendaire donnaient tous **exactement 80 %**. La division
> entière avalait en plus tout bonus inférieur à la tranche (d'où l'ancienne règle des bonus pairs).

```
puissance = PV×poids_pv + CA×poids_ca + ATQ×poids_atk + niveau×poids_niveau
          + (mana÷10)×poids_mana + charisme×poids_charisme + agilité×poids_agilite
          + (nbAttaques − 1) × combat_puissance_par_attaque
          + <Classe>_puissanceModif           // Algorythmancien : −45
          + <SousClasse>_bonusPuissance       // Faille-Zéro, Serment-Binaire : +45
          + combat_puissance_compagnon        // si compagnonActif != ""

chance‰ = combat_base_mille + (puissance − combat_socle) × pente_num ÷ pente_den
         + combat_tier_<tier>_mille
chance‰ = clamp(chance‰, combat_mille_min, combat_mille_max)

réussite si rng.Next(1000) < chance‰
```

**Le tirage se fait sur 1000**, pas sur 100 : sinon un `+1 attaque` (0,9 point) disparaîtrait dans
l'arrondi au pourcent. C'est ce qui supprime définitivement la contrainte des bonus pairs — tout compte.

Courbe obtenue face à un ennemi **moyen** (Hexadécimeur de référence) :

| Profil | faible | moyen | fort | mini-boss |
|---|---|---|---|---|
| niv 1 nu | 95 % | **50,0 %** | 30,0 % | 15,0 % |
| niv 3 Bois | 95 % | **57,5 %** | 37,5 % | 22,5 % |
| niv 5 Commun | 95 % | **64,4 %** | 44,4 % | 29,4 % |
| niv 6 Rare | 95 % | **70,6 %** | 50,6 % | 35,6 % |
| niv 8 Épique | 95 % | **77,6 %** | 57,6 % | 42,6 % |
| niv 10 Mythique | 95 % | **84,9 %** | 64,9 % | 49,9 % |
| niv 10 Légendaire | 95 % | **89,9 %** | 69,9 % | 54,9 % |

Paliers réguliers de 6 à 7 points, **aucune saturation**. Granularité à niv 5 Commun :
+1 attaque = +0,9 pt · +1 CA = +0,5 · +1 charisme = +0,2.

**L'Algorythmancien** porte `puissanceModif: -45`, exactement compensé par les +45 d'un compagnon :
seul il combat à −8 points des autres classes, accompagné il est à parité. Il recrute à **73 %**
(88 % en Barde-Binaire) contre 49 % pour un Hexadécimeur. *Il ne se bat pas, il fait combattre.*

**Multi-attaque visible** : le message de `!combat` annonce les frappes (`3 frappes portées`,
`1 frappe sur 3` en échec) dès que `nbAttaques > 1`. Purement narratif — la résolution reste le jet unique.

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
- **Créature sauvable** (`<Ennemi>_discuterSauve == "true"`, testé **avant** la logique de compagnon) :
  réussite = **victoire** (XP/RAM de l'ennemi, `combatsGagnes++`, montée de niveau, butin de
  `<Ennemi>_discuterLootPool`, quête reprise) ; échec = rencontre maintenue, on peut réessayer.
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

#### `!acheter [objet]` → `Commandes/Acheter/commande_acheter.cs`
**Deux contextes, arbitrés par la présence d'un argument** — sans cette règle, un joueur en
rencontre marchande ne pourrait jamais atteindre la boutique.

| Saisie | Contexte |
|---|---|
| `!acheter` (sans argument) | **Marchand ambulant** : Potion, tant que `rencontreType == "marchand_potion"` et que la rencontre n'a pas expiré. Prix dans `offreValeur`. Résout la rencontre et reprend la quête. |
| `!acheter [objet]` | **Boutique de Faîne** (Désert) : objets légendaires. |

**Boutique de Faîne** — deux monnaies, l'une donne le *droit*, l'autre paie :
- **1 jeton** (`caravaneAchats`, posé par le reward 🐪 Caravane du Désert) — consommé à l'achat.
- **La RAM** — `<Item>_prixAchat` : 6 000 pour une armure/accessoire, 9 000 pour une arme de sous-classe.
  Une éventuelle `reductionBoutique` (prime de pionnier) s'applique avant le contrôle de solde,
  et n'est consommée **qu'en cas d'achat réussi**.
- Catalogue = `config_global → boutique_catalogue` (nom du pool) → le CSV correspondant dans
  `config_quetes` (par défaut `loot_legendaire`, les 20 légendaires). **Aucun nom d'item en dur.**
- Recherche **insensible à la casse et aux accents**.
- Bloqué si `enCombat`. Sac plein ou RAM insuffisante → **le jeton n'est pas consommé**.
- Le catalogue n'est pas affiché en chat (20 lignes) : il est épinglé dans `#boutique` sur Discord.
```
Trigger : Command Triggered → !acheter
```

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

### `!equipement` → `Commandes/Equipement/commande_equipement.cs`
Affiche les 3 slots équipés **et le cumul des bonus** — lecture seule, n'écrit jamais dans le profil.
- Message 1 : `Arme: X [+2atq] · Armure: Y [+1CA] · Accessoire: Rien` (slot vide = `Rien`,
  item sans bonus = `déco`)
- Message 2 : `Bonus total : +N atq +N CA +N mana +N charisme` (ou « Aucun bonus d'équipement actif »)
- Additionne `_attaqueBonus` / `_caBonus` / `_manaBonus` / `_charismeBonus` depuis `config_items.json`
- Complémentaire de `!inventaire` (qui liste le sac) : ici on ne voit que l'équipé, avec le détail chiffré
```
Trigger : Command Triggered → !equipement
```

### `!vendre [nom_item] [quantité]` → `Commandes/Vendre/commande_vendre.cs`
Vend un item du sac ou d'un slot équipé contre des RAM.
- Cherche d'abord dans l'inventaire (CSV), puis dans les slots équipés
- **Quantité optionnelle** (dernier mot numérique) : `!vendre Potion 2` vend 2 Potions et laisse le reste.
  Plafonnée au stock réel (note « tu n'en avais que N »). Un slot équipé ne vend qu'1 exemplaire.
- Lit `_prixVente` depuis config (défaut 5 RAM), gain = prix × nb vendus
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

### `!ouvrirdesert` → `Commandes/OuvrirDesert/commande_ouvrirdesert.cs`
**Broadcaster uniquement** (silence total pour les autres). Force `desertDecouvert = true` dans
`etat_global.json` — filet de sécurité : la découverte passe normalement par le reward **Caravane
du Désert** (1 000 points), mais l'artefact du Désert est dans la **chaîne principale**. Sans
ouverture, personne ne peut finir l'histoire. Idéal aussi pour tester.
```
Trigger : Command Triggered → !ouvrirdesert
```

### 🐪 Reward **Caravane du Désert** → `Reward/Caravane/caravane_desert.cs`
Deux effets de portée différente, dans la même redemption :
- **Découverte — globale et définitive** : pose `desertDecouvert = true`. Les missives du Désert
  entrent dans le tirage pour tous les joueurs qui ont le niveau. Annoncée en chat **une seule fois**.
- **Jeton d'achat — personnel** : `caravaneAchats += caravane_achats_par_ouverture` sur le profil de
  celui qui a payé. **Seul lui** peut acheter chez Faîne ; les autres profitent de la zone, pas du comptoir.

Un jeton plutôt qu'une fenêtre de temps : une redemption trois minutes avant la fin du stream reste
utilisable, donc aucun remboursement à la main.

**🎖️ Prime de pionnier** — celui dont la redemption **ouvre** la zone (la première, donc) recoit
`reductionBoutique = caravane_reduction_pionnier_pct` (50 %) sur son profil. Sans elle, le premier
acheteur paie 1 000 points pour offrir le Désert à tout le chat et ne garde qu'un jeton souvent
inutilisable dans l'immédiat. La réduction s'applique à **un** achat puis se remet à 0 — elle
n'expire pas, elle attend.

**Remboursement automatique** si le viewer n'a pas de profil ou pas de classe :
`CPH.TwitchRedemptionCancel(rewardId, redemptionId)` rend les points. Ne fonctionne que si le reward
n'est **pas** en « skip request queue » côté Twitch — sinon la redemption est déjà validée.
```
Trigger : Channel Point Reward → à attacher à la main dans SB (kika-sync ne crée pas ce type de trigger)
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
  `vieux_sage_chance_perte_item`% → perd 1 item aléatoire tiré d'un **pool combiné sac + équipement**
  (un item équipé volé est retiré de son slot ; les deux rolls peuvent arriver ensemble).
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
- Cible parsée depuis `args["rawInput"]` (`@` retiré), **validée** `[a-zA-Z0-9_]` via `EstPseudoValide`
  (anti path traversal — le pseudo construit un chemin de fichier), existe, **≠ soi-même**, pas déjà défiée (`duelDe` vivant)
- Challenger hors cooldown (`duelCooldownFin`), sans défi sortant déjà en cours (`duelVers`, nettoyé si expiré)
- Disponibilité des deux joueurs : **duel autorisé en quête**, mais bloqué si `enCombat == true`,
  en repos (`reposCooldownFin > maintenant`) ou à terre (`pvActuels <= 0`)
- **Niveau** : `niveauCible == niveauChallenger` **ou** `niveauChallenger + 1` (même niveau ou 1 au-dessus)
- Pose `duelDe`/`duelExpire` sur la cible + `duelVers` sur le challenger (`duel_expire_secondes`, 60s)

**Résolution (dans `!accepter`, méthode `ResoudreDuel`)** — réutilise la formule de puissance de `!combat`
(`ScorePuissance` = la **puissance** de `!combat`, mêmes poids, même `puissanceModif` de classe,
compagnon inclus — **sans plafond** : l'ancien clamp 20..80 écrasait les écarts entre duellistes).
Vainqueur tiré au sort :
```
probaChallenger = scoreA * 100 / (scoreA + scoreB)   →   rng.Next(100) < probaChallenger
```
- Vainqueur : `+duel_recompense_xp_gagnant` (100) XP, `duelsGagnes++`
- Perdant : `+duel_recompense_xp_perdant` (50) XP, `duelsPerdus++`
- Montée de niveau vérifiée pour les deux · marqueurs de duel nettoyés sur les deux profils
- **Cooldown 1h posé sur le challenger uniquement à l'acceptation** (`duel_cooldown_secondes`) — refus/expiration ⇒ aucun cooldown
- Re-validation des deux au moment de l'acceptation (bloqué si combat/repos/à terre, la quête reste OK ; sinon duel annulé)
- **Message de victoire commenté** (`CommentaireDuel`) : ajoute une ligne 📜 expliquant l'issue —
  renversement du pronostic (marge < 0), duel serré (marge ≤ 4), sinon met en avant la stat dominante
  du vainqueur (attaque / CA / agilité / charisme). Ex. « ses coups d'une puissance brutale ont fini par briser la garde de X. »

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
| `1d6_PV.cs` | 🎲 Jet de dé — PV | 300 | pvMax = pvBase (config) + 1d6 |
| `1d4_CA.cs` | 🎲 Jet de dé — CA | 300 | CA = caBase (config) + 1d4 |
| `+2_PV.cs` | ⭐ Boost +2 — PV | 20 000 | pvMax += 2 (stack permanent) |
| `+2_CA.cs` | ⭐ Boost +2 — CA | 20 000 | CA += 2 |
| `+2_Attaque.cs` | ⭐ Boost +2 — Attaque | 20 000 | bonusAttaque += 2 |
| `caravane_desert.cs` | 🐪 Caravane du Désert | 1 000 | découvre le Désert (global) + 1 jeton d'achat (perso) |
| `suggestion_donationgoal.cs` | 💡 Suggestion de Donation Goal | 1 | Append de la saisie viewer dans un fichier texte |

> Jet de dé : repart de la BASE classe depuis config, ne stack pas.
> Boost +2 : s'empile sur tout.
```
Trigger : Channel Point Reward (un fichier par reward)
```

**`suggestion_donationgoal.cs` → `Reward/DonationGoal/`** — récompense **silencieuse** (aucun message chat).
- Reward Twitch avec **saisie de texte obligatoire** (coût 1). Action SB nommée `Suggestion de Donation Goal`.
- Lit la saisie via `args["rawInput"]` (repli `args["message"]`), aplatit les retours à la ligne.
- **Append** dans `C:\Users\Florian\Desktop\Stream\Donation goal\DonationGoal.txt` (dossier + fichier créés au besoin).
- Format d'une ligne : `AAAA-MM-JJ HH:MM | pseudo : suggestion`. Les échanges suivants s'ajoutent à la suite.

---

## Modération — « Prison de Sagesse » (hors jeu RPG)

Feature **indépendante du RPG** : aucun fichier joueur n'est lu ni écrit.
Déroulement en deux phases ; **seule la phase A est implémentée**.

> ⚠️ **Les logs vivent HORS du repo** (`C:\Users\Florian\Desktop\Stream\Moderation`) : ils
> contiennent des messages nominatifs de tiers et le repo est **public**. `.gitignore` bloque
> `*.jsonl` en filet de sécurité. Usage strictement personnel de modération, aucune diffusion.

### Phase A — sourcing & modération manuelle ✅

Objectif : accumuler de la donnée réelle pour bâtir une liste de badwords *pertinente*
(pas générique) avant toute automatisation.

**`Moderation/chat_logger.cs`** — action silencieuse, append pur, aucune lecture.
```
{"ts":"2026-07-20T14:32:11Z","user":"pseudo","userId":"123456","role":"mod","sub":true,"msg":"..."}
```
- Rotation mensuelle : `chat_log_AAAA-MM.jsonl`. Horodatage **UTC**.
- `userId` en plus du pseudo : un pseudo se change, l'ID non.
- `role` : broadcaster > mod > vip > sub > viewer (repli sur l'entier `role`).
- `Echapper()` : échappement JSON manuel (pas de Newtonsoft) — sans lui, un `"` d'un viewer
  casse le fichier. `new UTF8Encoding(false)` : **sans BOM**, sinon la 1re ligne du mois
  est illisible pour un parseur strict.
```
Trigger : Twitch Chat Message (type 133)
```

**`Moderation/timeout_manuel.cs`** — timeout piloté depuis Touch Portal, aucune détection auto.
- Lit `timeout_cible.txt`, `Trim()` + retrait du `@`, valide `[a-zA-Z0-9_]` (le pseudo sert
  d'appel API — même garde-fou que `!duel`), puis `CPH.TwitchTimeoutUser(pseudo, durée, …, bot)`.
- **Vide le fichier AVANT l'appel** : un échec ne laisse pas la cible « armée ».
- **Idempotence** : un watcher émet 2 événements par écriture (l'écriture + notre effacement) ;
  la sortie sur fichier vide garantit **un seul** timeout par clic. Vérifié en log.
- Journalise dans `timeouts_AAAA-MM.jsonl` (`ok` / `echec` / `pseudo_invalide`).
  Ce journal est la **vérité terrain** qui servira à construire `badwords.json` en phase B :
  croisé avec `chat_log`, il donne de la donnée labellisée.
- Durée : `moderation_timeout_secondes` (config_global), relue à chaque exécution.
```
Trigger : File/Folder Watcher → Changed sur timeout_cible.txt
          (à configurer dans SB → Services : kika-sync ne crée pas ce type de trigger)
```

**`Tools/tp_timeout.vbs`** — pont Touch Portal → fichier, deux modes dans un seul script :
| Appel | Comportement | Usage |
|---|---|---|
| `wscript tp_timeout.vbs <pseudo>` | silencieux, **aucune fenêtre** | bouton dédié à un récidiviste |
| `wscript tp_timeout.vbs` | boîte de saisie Windows | bouton générique |

> Touch Portal (v4.6) **n'a aucune action de saisie de texte libre** — vérifié dans *Valeurs*,
> *Manipulation de texte*, *File IO*, *HTTP*. D'où la boîte `InputBox` côté script.
> Le `.vbs` est préféré au `.bat` : un `.bat` fait clignoter une console, visible à l'écran en live.
> Le champ n'est **jamais** pré-rempli avec le dernier viewer : un appui sur Entrée par réflexe
> sanctionnerait un innocent.

### Phase B — automatisation ⏳ (non implémentée)

À lancer **seulement** quand les logs de la phase A auront produit un `badwords.json` validé :
détection auto → énigme de sagesse en chat → si le viewer persiste, timeout + overlay OBS
+ scène « prison » + mur des prisonniers (grille de pp, reset mensuel).

### `Timer_XP_visionnage.cs` + `tracker_activite.cs` — Timer 15 min

`tracker_activite.cs` est déclenché sur **Twitch Chat Message** (tout message, pas une commande).
Il enregistre `derniereActivite = maintenant` dans le JSON du joueur à chaque fois qu'il écrit.

`Timer_XP_visionnage.cs` parcourt les joueurs avec classe et vérifie que `derniereActivite`
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
4. **À son tour**, chaque joueur choisit **UNE action** (elle consomme le tour, puis on passe au suivant).
   AFK > `arene_tour_timeout_secondes` (2 min) → saut auto.
5. Après le dernier joueur → **riposte boss** (`arena_check.cs`) : total = `<boss>_degatsMax` × nb joueurs,
   modulé par les buffs/débuffs et le ciblage (voir ci-dessous), réparti **inversement à la CA effective**
   (CA haute = encaisse moins). PV à 0 = tombé, retiré de l'ordre. Les compteurs `*Tours` sont décrémentés ici.
6. Boucle jusqu'à boss mort ou groupe anéanti.
   - **Victoire centralisée** (`arena_check.cs`, au tick suivant le coup fatal) : XP/RAM de base à TOUS les
     participants ; bonus + loot `boss_loot_pool` au **meilleur dégâteur**. Les commandes d'attaque posent
     juste `bossPVActuels = 0` puis le timer distribue.
   - **Défaite** : tous à terre, aucune récompense.

**Actions génériques (toutes classes)** — 1 par tour :
| Commande | Effet (config `boss_*`) |
|---|---|
| `!attaquer` | dégâts = `boss_degats_base` + (atk+niveau)×nbAttaques + alea (+ buffs surcharge/danse/anathème) |
| `!defense` | +`boss_defense_ca_bonus` CA pour soi jusqu'à la riposte de ce round |
| `!soin` | soigne selon les dés de classe (RollSoin, +danse), coûte `boss_soin_mana` — géré dans `combat/combat_soin.cs` |
| `!discuter` | `boss_discuter_chance` % → fin **pacifique** (récompense de base aux participants, pas de loot top) ; sinon tour perdu — `combat/commande_discuter.cs` |
| `!fuir` | quitte l'arène, **−`<boss>_degatsMax`** PV, retiré de l'ordre/participants, **0 récompense** — `combat/combat_fuir.cs` |

**Capacités de classe** (`Boss/Capacites/`, réservées à la classe, coût mana sauf mention) — 1 par tour :
| Classe | Commande | Effet |
|---|---|---|
| Hexadécimeur | `!egidebinaire` | +`boss_egide_ca_bonus` CA groupe (`boss_egide_tours` rounds), `boss_egide_mana` mana |
| Hexadécimeur | `!provocation` | la prochaine riposte le vise **seul** (groupe épargné), il encaisse ×`boss_provocation_facteur_pct`% — gratuit |
| Cryptolame | `!zeroday` | crit croissant selon PV manquants du boss : ×(1 + (1−pv%)×`boss_zeroday_crit_facteur`) — gratuit |
| Cryptolame | `!soudoyer` | paie `boss_soudoyer_ram` RAM, `boss_soudoyer_chance` % → épargné à la riposte ; échec → riposte focalisée sur lui |
| Hackmancien | `!bouledefeu` | dégâts fixes `boss_bouledefeu_base` + niveau, `boss_bouledefeu_mana` mana |
| Hackmancien | `!surcharge` | +`boss_surcharge_atk` attaque groupe (`boss_surcharge_tours` rounds), `boss_surcharge_mana` mana |
| Firewaller | `!restoreup` | +`boss_restoreup_pv` PV à chaque combattant vivant, `boss_restoreup_mana` mana |
| Firewaller | `!anatheme` | armure boss ↓ : +`boss_anatheme_degats` dégâts/attaque (`boss_anatheme_tours` rounds), `boss_anatheme_mana` mana |
| Algorythmancien | `!dansesensuelle` | +`boss_danse_bonus` à tous les jets groupe (`boss_danse_tours` rounds), `boss_danse_mana` mana |
| Algorythmancien | `!serenadeinsultante` | riposte boss −`boss_serenade_reduction_pct`% (`boss_serenade_tours` rounds) + insulte tirée de `config_insultes.json` (séparées par `|`), `boss_serenade_mana` mana |

> ⚠️ Les répliques de `!serenadeinsultante` vivent dans **`Donnees/config_insultes.json`**, **git-ignoré** :
> le repo est public et le contenu est volontairement trash. Le modèle versionné est
> `config_insultes.example.json`. Fichier absent = repli sur une réplique neutre en dur, la capacité
> fonctionne quand même. **Ne jamais remettre ces textes dans `config_global.json`.**

```
Triggers : Command → !spawnboss (broadcaster) · !arene · !attaquer · !defense
           Command → capacités : !egidebinaire · !provocation · !zeroday · !soudoyer · !bouledefeu ·
                     !surcharge · !restoreup · !anatheme · !dansesensuelle · !serenadeinsultante
           (!soin · !discuter · !fuir : commandes existantes, désormais context-aware boss)
           Timed Action → ArenaCheck (~10-15s, repeat ; démarre désactivé, activé/désactivé par le code)
```
> ℹ️ `!soin`, `!discuter`, `!fuir` détectent le combat de boss (joueur présent dans `ordre` + phase `combat`)
> et basculent en logique boss **prioritaire** ; sinon ils gardent leur comportement de rencontre de quête.

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
**Aucun lien avec le RPG** : il ne lit ni les profils joueurs ni les configs du jeu.
C'est un bot de **planning de streams**, autonome, avec son propre `streams.json`.

**Slash commands** :

| Commande | Rôle |
|---|---|
| `/calendrier` | affiche le planning de la semaine en cours (image générée) |
| `/addstream` | ajoute un stream au planning — **admin** |
| `/delstream` | supprime un stream du planning — **admin** |

- `calendrier_image.py` génère l'image du planning (`banners/`, `fonts/`).
- Données dans `streams.json`, **à côté du script** (il part avec le zip Discloud).

> Le token Discord n'est **pas** versionné : lu via `os.getenv("DISCORD_TOKEN")` depuis un
> `.env` chargé à l'exécution. `.env` est dans `.gitignore`.

**Déploiement** : zipper `bot_discord.py` + `calendrier_image.py` + `discloud.config`
+ `requirements.txt` + `banners/` + `fonts/` → uploader sur discloud.app

---

## Garde-fou de cohérence narrative

Avant d'écrire ou de modifier du lore, une quête, une créature, un PNJ ou un message joueur :
lancer l'agent **`gardien-lore`** (`.claude/agents/gardien-lore.md`). Il relit la bible du monde
et signale les contradictions (carte des 6 zones, règle de corruption, vocabulaire
fragment/écaille, casting des alliés, boss retirés) en trois niveaux : 🔴 contradiction,
🟠 tension, 🔵 angle mort. Lecture seule — il rapporte, il ne corrige pas.

---

## Lore (résumé)

- **Arbonet** : monde nature + technologie hybride (chênes-serveurs, créatures cyber)
- **Pointu** : tortue ancienne, gardienne de l'Antre, NPC principal
- **Hector-Pierre Castor** : antagoniste, détruit les chênes-serveurs
- **Ram** : monnaie du jeu (mémoire vive d'Arbonet). Échelle **×5 appliquée le 22/08/2026** — profils, ennemis, boss, alliés, niveau 8 et prix de revente. Ne jamais rejouer la migration.
- **Les 6 zones** (`Lore/ZONES_ALLIES_ENNEMIS.md`) : Arbonet → Plaines → Lacs → Montagne → Désert → Marais.
  Niveaux d'accès dans `config_global.json` (`zone_*_niveau_min`), `zone_ordre` donne l'ordre canonique.

### ⚠️ Trois objets à ne pas confondre

| Terme | Ce que c'est | Rôle |
|---|---|---|
| **Fragment / Morceau de Carapace** | Éclat de la carapace de Pointu, remis à `!rejoindre` | **Outil.** Comprendre la langue commune d'Arbonet · choisir sa classe · sauvegarder sa progression dans l'Antre (= le fichier JSON joueur) · être soigné après une défaite |
| **Écaille-de-Pointu** | Un morceau de sa **peau**, pas de sa carapace | **Relique.** Meilleur accessoire du jeu, via `!racine`. Aucun pouvoir de langue/classe/sauvegarde. |
| **Morceau d'écorce** (`Ecorce-R/A/C/I/N/E`) | Débris de chêne-serveur droppés en quête | Les 6 lettres reconstituent `racine` |

---

*Projet Pointu © Florian alias kikaby67 — 2026*
