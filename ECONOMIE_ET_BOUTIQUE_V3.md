# Économie d'Arbonet — RAM, Items, Boutique, Échange

### Spécification V3 — à valider avant implémentation

> Complète `LORE_ARBONET_APPROFONDI.md` (lore, zones, quêtes) et `RESTRUCTURATION_POINTU_V3.md` (messages, Discord). Contient une **alerte d'équilibrage importante** (section 3\) : deux vérifications chiffrées montrent que le système d'items actuel ne peut pas fonctionner tel quel. Lire avant de coder.

---

## 1\. Pourquoi revaloriser la RAM

Aujourd'hui une quête rapporte 1 à 12 RAM. Impossible de construire une boutique à plusieurs paliers avec cette granularité : soit les prix sont à 2-3 RAM (aucune sensation de progression), soit ils sont inatteignables.

**Décision : RAM × 5 partout** (XP inchangée — ta courbe de niveaux est calibrée dessus et fonctionne).

| Source | Avant | Après |
| :---- | :---- | :---- |
| Quête Forêt-Mémoire | 1-5 | **5-25** |
| Quête Plaines | 5-7 | **25-35** |
| Quête Marais | 8-9 | **40-45** |
| Quête Vide | 10-12 | **50-60** |
| Ennemi de rencontre | 3-6 | **15-30** |
| Mini-boss | 2-12 | **25-60** |
| Boss d'arène (tous) | 20 | **100** |
| Boss d'arène (top dégâts) | 50 | **250** |
| Bonus niveau 8 | 100 | **500** |

**Revenu estimé** : \~40-60 RAM par tranche de 15 min de jeu actif en Forêt-Mémoire, \~150-200 en zone Vide.

> ⚠️ **Profils existants** : les joueurs actuels garderaient un solde d'ancienne échelle face aux nouveaux prix. Prévoir un script de migration ponctuel (`ram × 5` sur tous les fichiers de `Donnees/joueurs/`) avant le premier stream post-mise à jour.

---

## 2\. Les quatre paliers d'équipement

Un palier par zone, comme demandé : bois/rouille → fer → acier → runique.

### 🌲 Palier I — Bois & Rouille *(Forêt-Mémoire, niveau 1+)*

| Item | Slot | Bonus | Poids | Prix |
| :---- | :---- | :---- | :---- | :---- |
| Lame-Rouillée | arme | \+2 attaque | 1 | 100 |
| Bâton-Noueux | arme | \+2 attaque, \+10 mana | 1 | 100 |
| Plastron-d'Écorce | armure | \+2 CA | 1 | 120 |
| Gland-Porte-Sève | accessoire | \+4 charisme | 0 | 150 |

### ⚡ Palier II — Fer & Cuivre *(Plaines de Pixel, niveau 3+)*

| Item | Slot | Bonus | Poids | Prix |
| :---- | :---- | :---- | :---- | :---- |
| Lame-de-Fer | arme | \+4 attaque | 2 | 350 |
| Bâton-de-Cuivre | arme | \+4 attaque, \+20 mana | 1 | 350 |
| Cuirasse-Tressée | armure | \+4 CA | 2 | 400 |
| Plume-de-Munin | accessoire | \+2 attaque, \+4 charisme | 0 | 450 |

### 🔥 Palier III — Acier & Pétrifié *(Marais du Buffer, niveau 6+)*

| Item | Slot | Bonus | Poids | Prix |
| :---- | :---- | :---- | :---- | :---- |
| Lame-d'Acier-Trempé | arme | \+6 attaque | 3 | 900 |
| Sceptre-Pétrifié | arme | \+6 attaque, \+30 mana | 2 | 900 |
| Plaques-Pétrifiées | armure | \+6 CA | 4 | 1000 |
| Croc-de-Fenrir | accessoire | \+4 attaque, \+2 CA | 0 | 1200 |

### 🌌 Palier IV — Runique & Quantique *(Vide Binaire, niveau 9+)*

| Item | Slot | Bonus | Poids | Prix |
| :---- | :---- | :---- | :---- | :---- |
| Lame-Runique | arme | \+8 attaque | 3 | 2200 |
| Sceptre-Quantique | arme | \+8 attaque, \+40 mana | 2 | 2200 |
| Carapace-Runique | armure | \+8 CA | 5 | 2500 |
| Œil-du-Vide | accessoire | \+4 attaque, \+2 CA, \+8 charisme | 0 | 3000 |

### Consommables

| Item | Effet | Prix |
| :---- | :---- | :---- |
| Potion | \+8 PV | 40 |
| Potion-Majeure | \+20 PV | 120 |
| Élixir-de-Sève | \+40 PV, \+20 mana | 300 |

### ⚠️ L'Écaille-de-Pointu doit être revalorisée

L'item secret (`!racine`, 6 écorces à collecter) donne actuellement **\+3 atq / \+2 CA** — le Croc-de-Fenrir du palier III (+4/+2) le surpasserait, ce qui viderait le secret de son intérêt. **Proposition : Écaille-de-Pointu → \+6 attaque / \+4 CA / \+4 charisme**, meilleur accessoire du jeu, mais réservé à ceux qui ont assemblé les 6 morceaux d'écorce. Le secret reste le sommet.

### Deux règles de conception non négociables

1. **Tous les bonus sont pairs (+2 minimum).** Voir section 3 : un bonus de \+1 est mathématiquement invisible dans ta formule.  
2. **Le poids est le contrepoids des armures.** `fuite_poids_pct = 6` : une Carapace-Runique (poids 5\) coûte \-30 % de chance de `!fuir`. Le tank ne fuit pas — c'est un vrai choix, pas une punition.

---

## 3\. ⚠️ Alerte d'équilibrage — vérifié par le calcul

J'ai fait tourner ta formule de `!combat` avec les valeurs réelles de `config_global.json`. Deux problèmes bloquants apparaissent.

### Problème 1 — Les bonus de \+1 ne servent à rien

La formule utilise des **tranches en division entière** : `Tranche(v) = ((v - ref) / tranche) * pct`, avec `tranche = 2` pour la CA et l'attaque.

Hexadécimeur niveau 1, sans équipement ............. 62 %

  le même avec \+1 CA et \+1 attaque ................. 62 %   (aucun changement)

  le même avec \+2 CA et \+2 attaque ................. 68 %   (+6 points)

→ D'où la règle des bonus pairs. Un item « \+1 attaque » serait vendu au joueur sans lui donner quoi que ce soit.

### Problème 2 — Le plafond de 80 % annule tout l'équipement en fin de jeu

`combat_plancher_joueur..combat_plafond_joueur = 20..80`. Or un joueur **niveau 9 sans aucun équipement** atteint déjà 79 %, uniquement par ses PV et son niveau.

Joueur niveau 9 (45 PV)      score brut    avec plafond 80    avec plafond 90

  Sans équipement ........       79              79                 79

  Palier I  (+2/+2) ......       87              80  ← plafonné     87

  Palier II (+4/+4) ......       95              80  ← plafonné     90  ← plafonné

  Palier III (+6/+6) .....      103              80                 90

  Palier IV (+8/+8) ......      111              80                 90

→ **Avec le plafond actuel, acheter du palier II, III ou IV ne change strictement rien en `!combat`.** La boutique haut de gamme serait une arnaque.

### La solution : l'arène n'a pas de plafond

Bonne nouvelle — les dégâts sur les boss communautaires se calculent tout autrement : `dégâts = boss_degats_base + aléa + (attaque + niveau) × nbAttaques` — **aucun plafond**. L'attaque compte linéairement.

Dégâts moyens par tour sur un boss (joueur niveau 9\)

                        1 attaque     3 attaques (Byte-Fantôme)

  Sans équipement .....   18,5              40,5

  Palier IV (+8) ......   26,5              64,5

  Gain ................   ×1,43             ×1,59

Hector-Pierre (800 PV, 5 joueurs) : 8,6 tours sans équipement → 6,0 tours en palier IV

**Recommandation en trois points :**

1. **Monter `combat_plafond_joueur` de 80 à 90\.** Un joueur équipé palier I-II sent enfin la différence en `!combat`, et le mini-boss (-35) devient jouable (45 % → 55 %).  
2. **Positionner narrativement les paliers III et IV comme de l'équipement d'arène.** C'est là qu'ils brillent réellement, sans aucun rééquilibrage nécessaire. Faîne peut le dire elle-même : *« Ça ? Ça ne t'aidera pas à fuir un sanglier. Ça sert à faire saigner quelque chose de beaucoup plus gros. »*  
3. **Charger les accessoires haut de gamme en mana et charisme** (déjà fait dans les tables ci-dessus) : ces stats alimentent `!discuter` et `!soin`, qui ne sont pas plafonnés de la même manière — l'équipement garde une utilité hors `!combat`.

>   
> Option plus lourde si tu veux que l'équipement domine partout : baisser `combat_base_pct` de 50 à 40\. Le joueur nu passe à \~69 % et l'équipement reprend tout son espace. Plus propre mathématiquement, mais ça rend le jeu plus dur pour les débutants sans équipement — je ne la recommande pas en premier choix.

---

## 4\. La boutique de Faîne

**Tenue par Faîne l'Écureuil-Archive** (lore section 3bis) — elle siège dans l'Antre, donc accessible en permanence, pas seulement en rencontre.

### `!boutique`

Liste ce que Faîne propose **pour les zones débloquées du joueur** (palier I dès le niveau 1, II au 3, III au 6, IV au 9).

- **Twitch** : `🐿️ Faîne ouvre ses réserves pour kikaby (catalogue → #lantre-de-pointu)`  
- **Discord** : le catalogue complet avec prix, bonus et solde RAM du joueur.

### `!acheter [nom_item]`

⚠️ **Cette commande existe déjà** (achat de Potion au marchand ambulant en quête, `Commandes/Acheter/commande_acheter.cs`). Ne pas créer un second fichier : **étendre l'existant** avec deux contextes.

si rencontreType \== "marchand\_potion"  → comportement actuel (potion du marchand)

sinon                                  → boutique de Faîne (nouveau)

Vérifications : RAM suffisante · palier débloqué (niveau) · place dans le sac · pas `enCombat`.

### `!vendre` — déjà en place

Fonctionne déjà avec `_prixVente`. **Règle proposée : prix de revente \= 30 % du prix d'achat** (Lame-Rouillée 100 → revente 30). Faîne ne fait pas de cadeau, c'est dans son caractère.

### ⚠️ Taille du sac

`max_sac = 8` devient très juste avec 4 paliers × 3 slots \+ consommables. **Proposition : 12\.**

---

## 5\. L'échange entre joueurs

### `!echange @joueur [mon_item] [son_item | montant_ram]`

Le demandeur propose ; la cible répond par `!accepter` ou `!refuser` (délai 60 s, comme le duel).

Exemples :

\!echange @viewer2 Lame-de-Fer Cuirasse-Tressée     → troc objet contre objet

\!echange @viewer2 Lame-de-Fer 300                  → vente entre joueurs contre 300 RAM

### Sécurité — à ne pas négliger

Le code de `!duel` a déjà résolu la plupart de ces problèmes : **s'en inspirer directement**.

- **Validation du pseudo** : réutiliser `EstPseudoValide` (`[a-zA-Z0-9_]`) — le pseudo construit un chemin de fichier, c'est une faille de traversée de répertoire si on ne filtre pas.  
- **Écriture sur deux fichiers** : c'est le vrai risque. Si Streamer.bot plante entre l'écriture du donneur et celle du receveur, un item est **dupliqué ou perdu**. Ordre recommandé : tout valider → retirer chez l'expéditeur → ajouter chez le destinataire → si la 2ᵉ écriture échoue, `CPH.LogWarn` explicite avec les deux pseudos et l'item, pour réparation manuelle.  
- **Blocages** : `enCombat`, à terre (`pvActuels <= 0`), sac plein côté receveur, échange avec soi-même.  
- **Anti-abus** : cooldown (`echange_cooldown_secondes`, proposition 300\) pour éviter le blanchiment de RAM entre comptes multiples.

### ⚠️ `!accepter` gère maintenant 4 contextes

Vieux Sage · marchand · duel · échange. Le fichier `commande_accepter.cs` doit tester dans un **ordre de priorité déterministe** et documenté, sinon un joueur avec un duel *et* un échange en attente aura un comportement imprévisible. Ordre proposé : `duel` → `echange` → rencontre alliée (`vieux_sage` / `marchand`).

> Alternative plus sûre si le fichier devient trop lourd : commandes dédiées `!troc-ok` / `!troc-non`. Moins élégant, mais zéro ambiguïté. À trancher au moment de coder.

---

## 6\. Récapitulatif des fichiers à modifier

| Fichier | Modification |
| :---- | :---- |
| `config_items.json` | \+15 items (4 paliers \+ consommables) avec `_slot`, `_attaqueBonus`, `_caBonus`, `_manaBonus`, `_charismeBonus`, `_poids`, `_prixVente`, `_zone`, `_prixAchat`, `_niveauMin` · revaloriser `Ecaille-de-Pointu` |
| `config_global.json` | `combat_plafond_joueur` 80 → 90 · `max_sac` 8 → 12 · `echange_cooldown_secondes` · `echange_expire_secondes` · RAM des boss ×5 |
| `config_quetes.json` | RAM ×5 (détail dans le doc de lore) |
| `config_ennemis.json` | RAM ×5 \+ 3 corrections d'XP (détail dans le doc de lore) |
| `config_level.json` | Bonus RAM niveau 8 : 100 → 500 |
| `Commandes/Acheter/commande_acheter.cs` | Étendre : contexte marchand **ou** boutique |
| `Commandes/Boutique/commande_boutique.cs` | **Nouveau** — catalogue filtré par niveau |
| `Commandes/Echange/commande_echange.cs` | **Nouveau** — proposition d'échange |
| `Commandes/Accepter/commande_accepter.cs` | Ajouter la résolution d'échange \+ ordre de priorité |
| JSON joueur | `echangeVers`, `echangeDe`, `echangeItem`, `echangeContre`, `echangeExpire`, `echangeCooldownFin` |
| Script ponctuel | Migration `ram × 5` sur les profils existants |

---

## 7\. Ce qui reste à valider avant de coder

1. **Plafond de combat 80 → 90** — sans ça, la boutique haut de gamme n'a pas de sens en `!combat` (section 3).  
2. **Écaille-de-Pointu revalorisée** à \+6/+4/+4 pour rester au-dessus du palier III.  
3. **Prix et bonus** des 15 items : à ajuster selon le rythme de tes streams (je suis parti sur \~1 h 30 de jeu pour un set palier I complet).  
4. **Migration des profils existants** (`ram × 5`) ou reset assumé.  
5. **`!accepter` à 4 contextes** ou commandes d'échange dédiées.

---

*Projet Pointu-PJT © Florian alias kikaby67 — 2026*  
