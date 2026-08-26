# Pointu-PJT V3 — Plan d'implémentation

### Document de handoff pour Claude Code (VS Code)

> **Lis ce fichier en premier.** Il récapitule toute la conception V3 et donne l'ordre d'exécution. Les trois documents de référence sont dans `Lore/` et à la racine :  
> 

> - `RESTRUCTURATION_POINTU_V3.md` — architecture des messages (Twitch/Discord)  
> - `LORE_ARBONET_APPROFONDI.md` — zones, familles, bestiaire, quêtes, récompenses  
> - `ECONOMIE_ET_BOUTIQUE_V3.md` — RAM, items, boutique, échange, équilibrage

---

## 0\. Contexte en 10 lignes

Pointu-PJT est un mini-jeu RPG dans le chat Twitch, écrit en **C\# inline dans Streamer.bot**, sans base de données (un fichier JSON par joueur), sans librairie externe (parsing manuel). Le projet fonctionne déjà : 5 classes, 10 sous-classes, quêtes, combat probabiliste, boss d'arène communautaire, inventaire, duels.

La V3 répond à trois problèmes constatés en stream :

1. **Trop de texte dans le chat Twitch** à chaque événement.  
2. **Pollution du chat partagé** lors des raids/co-streams.  
3. **Le lore est riche mais inexploité** — jamais présent dans le combat, les quêtes ou les rencontres.

Et ajoute deux systèmes demandés : **une boutique** et **un échange entre joueurs**.

---

## 1\. ⛔ Décisions bloquantes — à confirmer avec Florian avant de coder

Ces cinq points changent des valeurs de gameplay. Ne pas les appliquer sans validation explicite.

| \# | Décision | Défaut proposé | Pourquoi ça bloque |
| :---- | :---- | :---- | :---- |
| 1 | `combat_plafond_joueur` : **80 → 90** | oui | Sans ça, tout l'équipement au-dessus du palier I est inutile en `!combat` (démonstration chiffrée : `ECONOMIE §3`). La boutique perdrait son sens. |
| 2 | `Ecaille-de-Pointu` : **\+3/+2 → \+6 atq / \+4 CA / \+4 cha** | oui | Sinon le Croc-de-Fenrir (palier III, achetable) dépasse l'item secret du jeu. |
| 3 | **Migration `ram × 5`** sur les profils existants | oui | Les joueurs actuels auraient un solde d'ancienne échelle face aux nouveaux prix. Alternative : reset assumé. |
| 4 | **`!accepter` à 4 contextes** (duel, échange, vieux sage, marchand) ou commandes dédiées `!troc-ok` / `!troc-non` | 4 contextes avec ordre de priorité | Risque de comportement imprévisible si un joueur a deux offres en attente. |
| 5 | **URL du webhook Discord** placée dans `secrets/secrets.json` | à faire par Florian | Le repo est **public** — l'URL ne doit jamais être commitée. |

---

## 2\. Conventions à respecter absolument

Reprises de `CLAUDE.md` — toute violation casse le projet.

- Chaque fichier `.cs` est **autonome** : aucune méthode partagée entre actions. `LireValeur`, `ModifierValeur`, `AjouterValeur` sont **recopiées dans chaque fichier**.  
- **Jamais** `Newtonsoft.Json` ni `System.Text.Json` → parsing manuel de chaînes uniquement.  
- Classe `CPHInline`, méthode `Execute()` retournant `bool`. `args["user"]` pour le pseudo (jamais `args["nomJoueur"]`).  
- Pas de `CPH.Wait()` dans les quêtes (bloque Streamer.bot) — tout passe par timestamps Unix.  
- Chemins en `@"..."`. `CPH.LogWarn` pour les logs.  
- **Zéro valeur de gameplay en dur** : tout vit dans les `config_*.json`. Zéro texte en dur : tout vient de `config_lore_textes.json`.  
- `estTexte = true` pour les chaînes, `false` pour nombres et booléens.  
- Tout nouveau champ de profil doit être ajouté aux anciens profils via un `EnsureChamp` (pattern déjà utilisé dans `quest_timer.cs`).  
- **Sécurité** : tout pseudo utilisé pour construire un chemin de fichier passe par `EstPseudoValide` (`[a-zA-Z0-9_]`) — voir `commande_duel.cs`. C'est une faille de traversée de répertoire sinon.

---

## 3\. Plan d'exécution en 6 phases

Chaque phase est testable indépendamment. Ne pas démarrer la suivante avant d'avoir validé la précédente en conditions réelles.

### Phase 1 — Le canal Discord (fondation technique)

**Objectif : prouver que Streamer.bot peut écrire sur Discord.**

1. Créer `secrets/secrets.json` depuis `secrets.example.json`, y mettre `discord_webhook_url`. Vérifier que `secrets/` est bien gitignoré (il l'est déjà).  
2. Écrire la méthode `EnvoyerDiscord(string message)` — code complet dans `RESTRUCTURATION §2`.  
   - ⚠️ **Premier test à faire** : confirmer que `using System.Net.Http;` est accepté par l'éditeur inline de Streamer.bot. Si ce n'est pas le cas, replier sur `System.Net.WebClient` ou `HttpWebRequest`.  
   - Le webhook poste sous `username: "Pointu"` (`avatar_url` viendra plus tard, l'artiste travaille dessus).  
   - Timeout 3 s \+ `try/catch` : une panne Discord ne doit **jamais** bloquer une commande de jeu.  
3. Brancher sur **une seule commande** (`!profil`) et tester en live.

>   
> ✅ Le webhook a déjà été testé hors Streamer.bot : `HTTP 204`, messages reçus. Le seul inconnu restant est le namespace HTTP côté Streamer.bot.

**Critère de réussite** : `!profil` poste dans `#lantre-de-pointu` sans latence perceptible dans le chat.

---

### Phase 2 — Le lore en configuration (aucun code de gameplay)

**Objectif : poser toute la donnée narrative avant de toucher aux mécaniques.**

1. **`config_quetes.json`** — ajouter `_zone` aux 13 quêtes ; remplacer `_nom` / `_demandeur` / `_description` par les textes de `LORE §5`. Ticks et XP **inchangés**, RAM ×5.  
2. **`config_ennemis.json`** — ajouter `_zone` à chaque ennemi (table complète dans `LORE §6`). Appliquer les 3 corrections d'XP :  
   - Insecte-Bug 10 → **20**  
   - Sanglier-Crash 22 → **38**  
   - Loup-Firewall 60 → **40** et Castor-Rootkit 40 → **60** (échange, conséquence de la correction des familles)  
   - RAM ×5 sur toute la table.  
3. **`config_lore_textes.json`** — **nouveau fichier**. Format `"<Ennemi>_victoire_01"`, `"_victoire_02"`, `"_defaite_01"`… énumération jusqu'à chaîne vide, comme `quete001`→`quete099`. Les fiches de `LORE §3, §3bis, §3ter et §4` donnent le ton de chaque créature.  
4. **`config_allies.json`** — ajouter la Rainette-Cache : `rainette_frequence_pct` (10), `rainette_bonus_combat_pct` (15).  
5. **`config_global.json`** — `quete_rencontre_intervalle_secondes` **180 → 360** ; seuils de zones (`zone_plaines_niveau_min` 3, `zone_marais_niveau_min` 6, `zone_vide_niveau_min` 9\) ; RAM des boss ×5.  
6. **`config_level.json`** — bonus RAM du niveau 8 : 100 → **500**.

**Critère de réussite** : le jeu tourne exactement comme avant (aucun code modifié), mais les quêtes affichent les nouveaux textes.

---

### Phase 3 — Le système de zones

**Objectif : la progression géographique.**

1. Ajouter `zoneActuelle` au JSON joueur (+ `EnsureChamp` pour les anciens profils). Valeur initiale : `"Forêt-Mémoire"`.  
2. `quest_system.cs` : le tirage de quête ne propose que les quêtes dont `_zone` correspond à une zone débloquée par le niveau du joueur.  
3. `quest_timer.cs` : le tirage de rencontre filtre `rencontre_ennemis` / `rencontre_mini_boss` par `_zone`.  
4. Ajouter la Rainette-Cache comme 6ᵉ type de rencontre alliée (`rencontreType = "rainette_cache"`), résolue par `!accepter` / `!refuser`. Nouveau champ `buffRainetteActif` (bool), vidé aux **mêmes endroits que `compagnonActif`** : fin de quête (succès/échec), `!abandon`, défaite KO.  
5. `commande_combat.cs` : appliquer `+rainette_bonus_combat_pct` si `buffRainetteActif`. Cumulable avec `compagnonActif` — le plafond existant gère l'équilibrage.

**Critère de réussite** : un joueur niveau 1 ne reçoit que des quêtes et ennemis de Forêt-Mémoire ; un niveau 6 accède au Marais.

---

### Phase 4 — Les messages discrets (le gros du travail)

**Objectif : régler les problèmes 1 et 2\.**

Pour chaque commande : **une ligne courte sur Twitch** \+ **le détail via `EnvoyerDiscord`**. Formats exacts dans `RESTRUCTURATION §1`.

À migrer : `!profil`, `!inventaire`, `!equipement`, `!classement`, `!choisirclasse`, `!sousclasse`, et la résolution de quête dans `quest_timer.cs`.

**À garder volontairement visible sur Twitch** (valeur de spectacle) : montée de niveau, victoire de boss d'arène, top 3 du classement.

**Pour `!sousclasse`** : afficher explicitement **l'effet devenu actif** (ex. `Bloc-Hex : +8 PV max, appliqué : 25 → 33`). C'était le manque signalé par Florian.

Utiliser les textes de `config_lore_textes.json` (phase 2\) pour les résolutions de combat.

**Critère de réussite** : une session complète de stream sans que le chat Twitch soit noyé, et lisible même en chat partagé.

---

### Phase 5 — L'économie et la boutique

**Objectif : donner un usage à la RAM.**

1. **Script de migration ponctuel** : `ram × 5` sur tous les fichiers de `Donnees/joueurs/`. À lancer une seule fois, avec sauvegarde préalable du dossier.  
2. **`config_items.json`** — les 15 items des 4 paliers (`ECONOMIE §2`), avec `_prixAchat`, `_niveauMin`, `_zone`. Revaloriser `Ecaille-de-Pointu`.  
   - ⚠️ **Règle absolue : tous les bonus sont pairs (+2 minimum).** Un bonus de \+1 est mathématiquement invisible (division entière dans les tranches — démonstration dans `ECONOMIE §3`).  
3. **`config_global.json`** — `combat_plafond_joueur` 80 → 90, `max_sac` 8 → 12\.  
4. **`commande_boutique.cs`** (nouveau) — catalogue filtré par niveau, détail sur Discord.  
5. **`commande_acheter.cs`** — **étendre l'existant**, ne pas créer de doublon :  
     
   si rencontreType \== "marchand\_potion" → comportement actuel  
     
   sinon                                  → boutique de Faîne  
     
6. `_prixVente` \= 30 % du prix d'achat sur les nouveaux items.

**Critère de réussite** : un joueur peut gagner de la RAM, acheter une Lame-Rouillée, l'équiper, et **voir sa chance de combat augmenter** (vérifiable via `!equipement`).

---

### Phase 6 — L'échange entre joueurs

**Objectif : l'interaction joueur-à-joueur. La phase la plus risquée — à faire en dernier.**

1. `commande_echange.cs` (nouveau) : `!echange @joueur [mon_item] [son_item | montant_ram]`.  
2. Nouveaux champs profil : `echangeVers`, `echangeDe`, `echangeItem`, `echangeContre`, `echangeExpire`, `echangeCooldownFin`.  
3. Résolution dans `commande_accepter.cs` avec un **ordre de priorité déterministe et commenté** : `duel` → `echange` → rencontre alliée.  
4. **Sécurité — s'inspirer directement de `commande_duel.cs`** qui a déjà résolu ces problèmes :  
   - `EstPseudoValide` obligatoire.  
   - Blocages : `enCombat`, à terre (`pvActuels <= 0`), sac plein côté receveur, échange avec soi-même.  
   - Cooldown `echange_cooldown_secondes` (300) contre le blanchiment de RAM entre comptes multiples.  
   - **Écriture sur deux fichiers** : valider tout → retirer chez l'expéditeur → ajouter chez le destinataire. Si la 2ᵉ écriture échoue, `CPH.LogWarn` explicite (les deux pseudos \+ l'item) pour réparation manuelle. C'est le risque de duplication/perte d'item.

**Critère de réussite** : deux comptes échangent un item dans les deux sens sans perte ni duplication, y compris en coupant Streamer.bot en plein échange.

---

## 4\. Checklist de test par phase

Phase 1  □ using System.Net.Http accepté dans Streamer.bot

         □ \!profil poste sur Discord · □ aucune latence chat · □ Discord coupé \= jeu OK

Phase 2  □ 13 quêtes ont \_zone · □ textes affichés · □ XP inchangée · □ RAM ×5

         □ 3 corrections XP appliquées · □ le jeu tourne comme avant

Phase 3  □ niv 1 \= Forêt seulement · □ niv 6 \= Marais accessible

         □ Rainette apparaît · □ buff appliqué · □ buff vidé en fin de quête/abandon/KO

Phase 4  □ chaque commande \= 1 ligne Twitch · □ détail Discord complet

         □ \!sousclasse montre l'effet actif · □ level up encore visible sur Twitch

         □ test en chat partagé réel

Phase 5  □ migration ram×5 (avec backup) · □ achat · □ équipement · □ \!equipement montre le gain

         □ vente à 30 % · □ palier bloqué par niveau · □ sac plein géré

Phase 6  □ échange objet↔objet · □ objet↔RAM · □ refus · □ expiration

         □ sac plein receveur · □ pseudo invalide rejeté · □ cooldown

         □ crash simulé en plein échange \= pas de duplication

---

## 5\. Ce qui n'est PAS dans cette V3 (assumé)

- **Commandes de jeu jouables depuis Discord** (`/profil` en direct) : nécessite un pont réseau entre Discloud et le PC de Florian. Le bot `DiscordBot/bot_discord.py` reste limité aux commandes **statiques** (`/lore`, `/classes`, `/bestiaire`, `/aide`) qui ne lisent pas l'état de jeu.  
- **Carnet Discord qui s'édite** (PATCH webhook au lieu de POST, champ `discordMessageId`) : prévu, mais après validation du POST simple en conditions réelles.  
- **Avatar de Pointu** : confié à un artiste. Le webhook tourne avec `username: "Pointu"` seul en attendant.  
- **Détection automatique du chat partagé** : écartée volontairement — le mode discret par défaut règle le problème sans dépendre d'une capacité incertaine de Streamer.bot.  
- **Quêtes supplémentaires du Vide Binaire** (2 seulement aujourd'hui) : à écrire quand l'arc Hector-Pierre sera implémenté. C'est du pur config, zéro code.  
- **Arc narratif Hector-Pierre** (jalons scénarisés, compteur communautaire de fragments, `!fragments`) : conçu dans `RESTRUCTURATION §4.2`, à implémenter après la phase 6\.

---

## 6\. Prompt de démarrage suggéré pour Claude Code

Lis PLAN\_IMPLEMENTATION\_V3.md, puis CLAUDE.md.

Nous démarrons la Phase 1 (canal Discord).

Avant d'écrire du code :

1\. Confirme que tu as bien compris les conventions de la section 2

   (fichiers autonomes, parsing manuel, zéro valeur en dur).

2\. Vérifie si System.Net.Http est utilisable dans l'éditeur inline

   de Streamer.bot — si tu n'es pas sûr, propose-moi une alternative

   avec System.Net.WebClient et explique la différence.

Je suis débutant en C\# : explique chaque bloc de code ligne par ligne.

Ne passe pas à la Phase 2 sans mon accord explicite.

---

## 7\. Récapitulatif des fichiers touchés

| Fichier | Phase | Nature |
| :---- | :---- | :---- |
| `secrets/secrets.json` | 1 | création (hors git) |
| Tous les `.cs` écrivant sur Discord | 1, 4 | ajout de `EnvoyerDiscord` |
| `config_quetes.json` | 2 | `_zone`, textes, RAM ×5 |
| `config_ennemis.json` | 2 | `_zone`, 3 corrections XP, RAM ×5 |
| `config_lore_textes.json` | 2 | **nouveau** |
| `config_allies.json` | 2 | Rainette-Cache |
| `config_level.json` | 2 | bonus RAM niv 8 |
| `config_global.json` | 2, 3, 5 | intervalle 360, seuils de zones, plafond 90, `max_sac` 12, cooldown échange |
| `quest_system.cs` | 3 | filtrage par zone |
| `quest_timer.cs` | 3, 4 | filtrage, Rainette, messages courts |
| `commande_combat.cs` | 3, 4 | buff Rainette, textes narratifs |
| `commande_!profil.cs`, `inventaire`, `equipement`, `classement`, `choisirclasse`, `sousclasse` | 4 | format court \+ Discord |
| `config_items.json` | 5 | 15 items, Écaille revalorisée |
| `commande_boutique.cs` | 5 | **nouveau** |
| `commande_acheter.cs` | 5 | extension 2 contextes |
| `commande_echange.cs` | 6 | **nouveau** |
| `commande_accepter.cs` | 6 | \+ échange, ordre de priorité |
| Script de migration RAM | 5 | ponctuel, avec backup |

---

*Projet Pointu-PJT © Florian alias kikaby67 — 2026*  
