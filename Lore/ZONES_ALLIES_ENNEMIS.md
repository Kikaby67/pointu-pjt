# Arbonet — Zones, Alliés & Ennemis
### Document de cohérence — La Légende de Pointu

*Document vivant — à intégrer à Pointu-PJT*
*Basé sur la carte manuscrite des 6 zones (Cycle 404)*

---

## Principe directeur

Chaque zone d'Arbonet cache un **fragment de vérité** — un souvenir volé, corrompu ou caché depuis que la mémoire collective du monde s'effrite. Les alliés de chaque zone sont des **témoins** qui ont vu quelque chose et donnent quête pour le prouver. Les ennemis mineurs sont des animaux locaux **infectés** par le même mal qui ronge Arbonet. Les boss de zone sont des **points de résistance** plus importants, parfois directement liés à Hector-Pierre Castor.

Un fil rouge relie les zones entre elles :
**Les Corbeaux-Daemon volent les souvenirs en Plaines → les cachent en Montagne → la piste remonte jusqu'au Marais, repaire d'Hector-Pierre.**
Ce chemin peut structurer une progression narrative zone par zone plutôt que 6 zones isolées.

---

## Les peuples d'Arbonet, la langue commune & la carapace de Pointu

Tous les habitants d'Arbonet — animaux hybrides, gardiens, marchands, ermites — ont une **forme humanoïde et bipède**. Ils se tiennent debout sur leurs pattes arrière, s'habillent, s'équipent, construisent, et parlent tous la **langue commune d'Arbonet**.

Les aventuriers (joueurs) ne comprennent pas cette langue par défaut. Ils ne peuvent la comprendre qu'en possédant un **morceau de la carapace de Pointu** — le même fragment qui **débloque leur classe** au moment de `!choisirclasse`.

**Implications de cohérence :**
- Le fragment de carapace n'est donc pas qu'un item de gameplay : c'est littéralement ce qui permet au joueur de *comprendre* et *d'exister* pleinement à Arbonet — un pont narratif fort entre lore et mécanique de jeu.
- Avant de l'obtenir, un aventurier peut croiser des habitants d'Arbonet sans les comprendre : dialogues brouillés, gestes, méfiance mutuelle. Ça justifie une courte phase de "non-communication" en tout début d'aventure, résolue par la quête d'obtention du fragment.
- **Marqueur visuel corruption vs santé :** les ennemis mineurs et boss listés dans ce document sont des habitants d'Arbonet **corrompus** par le mal qui ronge le monde. La corruption leur fait perdre leur forme humanoïde — ils retombent à quatre pattes, redeviennent des bêtes sauvages, et perdent l'usage de la langue commune (grognements, cris, silence). C'est ce qui les distingue immédiatement des alliés, qui eux restent debout, habillés/équipés, et parlent clairement.
- **La corruption se propage par le vivant.** C'est un mal de la nature : il circule par la sève, les racines, les bêtes. Là où la nature manque, il n'a rien pour voyager — d'où le **Désert** (minéral, presque sans végétation) et la **Montagne** (roche et froid), les deux zones les moins atteintes. Ce sont aussi les deux plus **difficiles d'accès**, ce qui justifie leurs paliers de niveau élevés : on n'y va pas par hasard, et ce qu'on y trouve d'intact s'y trouve précisément parce que le mal n'a pas su y monter.
- Cette règle explique aussi pourquoi on ne "négocie" jamais avec un ennemi mineur ou un boss classique : ils ont perdu la capacité de dialoguer. Seuls des personnages comme Hector-Pierre — corrompu par le chagrin plus que par le "bug" lui-même — gardent leur forme humanoïde et la parole, ce qui renforce sa tragédie (il choisit la destruction en pleine conscience, contrairement aux bêtes qui la subissent).

### Les trois exceptions à la règle de corruption

| Qui | Pourquoi il parle encore |
|---|---|
| **Hector-Pierre Castor** | Corrompu par le **chagrin**, pas par le mal. Il garde forme et parole, et choisit la destruction en pleine conscience. |
| **La Sentinelle du Castor** | Ce n'est **pas une bête infectée** : c'est un soldat **volontaire** d'Hector-Pierre, un Castor-Rootkit vétéran en armure de bois pétrifié. Elle a choisi sa cause, comme lui — d'où sa réplique récurrente : *« Vous ne comprenez pas ce qu'ils vous ont pris. »* À ne pas ranger avec les animaux corrompus. |
| **La Grenouille-Corrompue** | Corruption **incomplète** : on la sauve en lui parlant (`!discuter`). Seul cas de guérison connu à Arbonet. |


### Vocabulaire — trois objets à ne pas confondre

| Terme | Ce que c'est | Rôle |
|---|---|---|
| **Fragment / Morceau de Carapace** | Un éclat de la carapace de Pointu, remis à `!rejoindre` | **Outil.** Comprendre la langue commune · choisir sa classe (`!choisirclasse`) · sauvegarder sa progression dans l'Antre — c'est littéralement le fichier JSON du joueur · être soigné après une défaite (`!repos`) |
| **Écaille-de-Pointu** | Un morceau de sa **peau**, pas de sa carapace | **Relique.** Accessoire le plus puissant du jeu, obtenu via `!racine` en réunissant les 6 morceaux d'écorce. Aucun pouvoir de langue, de classe ni de sauvegarde — du pur bonus de stats. |
| **Morceau d'écorce** (`Ecorce-R/A/C/I/N/E`) | Débris de chêne-serveur droppés en quête | Les 6 lettres reconstituent le mot `racine` et ouvrent l'accès à l'Écaille |

Le fragment **ouvre le monde**, l'Écaille **récompense ceux qui l'ont fouillé jusqu'au bout**. Un aventurier sans fragment n'existe pas à Arbonet ; un aventurier sans Écaille joue simplement sans le meilleur accessoire.

---

## La scène d'ouverture — l'Antre de Pointu

**Pourquoi Pointu peut parler à l'aventurier alors qu'il n'a pas encore de fragment :** Pointu n'a pas besoin du fragment pour communiquer, car les fragments **viennent de sa propre carapace**. Il en est la source, pas un utilisateur comme les autres habitants d'Arbonet. C'est le seul être capable de s'adresser directement à un aventurier "sourd" à la langue commune — un peu comme si sa voix passait par un canal à part, au-dessus du réseau normal.

**Déroulé de la scène :**

1. L'aventurier arrive dans l'Antre, face à Pointu. Le reste du monde est encore silencieux/incompréhensible pour lui — mais Pointu, lui, se fait comprendre.
2. Pointu explique la situation d'Arbonet (texte déjà établi dans le lore : l'oubli qui progresse, les fragments volés, personne ne le croit).
3. Il propose à l'aventurier de partir à l'aventure pour l'aider à récupérer des preuves.
4. Il précise qu'un **morceau de sa carapace** sera nécessaire pour deux raisons, qu'il énonce clairement :
   - **comprendre** les habitants d'Arbonet (accéder à la langue commune)
   - **se battre si nécessaire** (le fragment débloque la classe du joueur)
5. Il remet le fragment — c'est le moment charnière où `!choisirclasse` devient disponible, et où le monde "s'allume" : les PNJ deviennent compréhensibles à partir de cet instant.

**Proposition de texte pour Pointu (ton cohérent avec le prologue existant) :**

> *"Tu ne comprends pas encore ce que disent les miens. C'est normal — la langue commune d'Arbonet ne s'entend pas, elle se ressent, à travers ce que je porte sur le dos depuis toujours.*
>
> *Je peux te parler, moi, parce que cette carapace est mienne. Mais eux — les marchands, les gardiens, les créatures qui rôdent dans les zones — resteront silencieux pour toi tant que tu n'en porteras pas un morceau.*
>
> *Prends ce fragment. Il te permettra de comprendre ceux que tu croiseras. Et si le besoin s'en fait sentir — car tout le monde n'a pas gardé sa raison ici — il te permettra aussi de te défendre.*
>
> *Arbonet a besoin de témoins. Es-tu prêt à en devenir un ?"*

**Lien mécanique/commandes :** cette scène correspond au moment `!bonjour` → dialogue Pointu → `!rejoindre` → remise du fragment → `!choisirclasse` débloqué. On peut envisager que la langue commune ne soit "activée" dans l'interface (sous-titres, PNJ qui répondent) qu'après cette étape, pour renforcer la sensation de bascule.

### Le fragment ne se perd pas — mais il peut être refusé

Une fois donné, le fragment de carapace **ne peut plus être perdu, volé ou retiré**. Il est lié à l'aventurier de manière définitive. C'est un choix de cohérence fort : la classe et la compréhension de la langue commune restent acquises tout au long de l'aventure, sans risque de régression punitive.

En revanche, l'aventurier peut **refuser** le fragment au moment où Pointu le lui propose. Dans ce cas, il est **expulsé d'Arbonet** — renvoyé hors de ce monde, sans autre explication immédiate. Pointu ne force jamais la main : Arbonet a besoin de témoins volontaires, pas de héros forcés.

### Plot twist possible — Hector-Pierre récupère le refus

Un aventurier qui refuse et se retrouve expulsé n'est pas nécessairement perdu pour l'histoire. Piste à garder sous le coude :

- **Hector-Pierre intercepte** ceux qui ont refusé, quelque part entre les mondes (ou au moment de leur expulsion).
- Il leur propose un **marché** différent du sien — pas le fragment de Pointu, mais quelque chose issu de lui, avec sans doute d'autres conditions ou une autre finalité (allégeance à sa cause, un pouvoir différent, une vision alternative des événements).
- **Décision actée : c'est un vrai chemin alternatif jouable**, pas un simple easter egg. Voir section dédiée ci-dessous.

---

## Chemin alternatif — La Voie d'Hector-Pierre

**Ce n'est pas une deuxième campagne symétrique.** C'est un mécanisme communautaire : refuser le fragment de Pointu peut mener un viewer à devenir un **boss créé par lui-même**, intégré manuellement au jeu par Florian.

### Déroulé complet

1. **Le refus.** Le joueur refuse le fragment de Pointu → il est expulsé du monde (comme établi précédemment).
2. **L'interception d'Hector-Pierre.** Hector-Pierre intercepte ce moment et propose un choix, en fiction : suivre sa cause, ou non.
3. **Choix "non"** → le joueur ne joue simplement pas. Fin du parcours pour cette session, sans conséquence supplémentaire.
4. **Choix "oui"** → bascule hors-fiction : le joueur reçoit un **message privé Twitch de Florian** l'informant qu'il peut devenir l'un des boss du jeu.
5. **Fiche de personnage.** Le joueur remplit une fiche avec les caractéristiques et le lore qu'il souhaite pour son personnage (aligné narrativement sur la cause d'Hector-Pierre).
6. **Évaluation & équilibrage.** Florian évalue la faisabilité, équilibre les stats proposées, et intègre officiellement le personnage au jeu comme boss.
7. **En combat.** Le joueur peut alors piloter son boss via des commandes de combat contre d'autres joueurs — **uniquement lorsqu'il est présent en live**. Hors de sa présence, le boss reste probablement injouable ou géré autrement (à définir).

### Ce que garde la scène d'interception

Le texte narratif d'Hector-Pierre (voir plus haut) reste valable comme moment fictionnel de bascule — c'est ce qui justifie *pourquoi* ce joueur devient un boss à sa solde plutôt qu'un simple aventurier. L'idée de la **page arrachée à un livre** peut rester comme signature narrative de ces personnages : leur fiche de lore peut s'ouvrir sur "une page" qui raconte leur histoire personnelle, en écho au procédé d'Hector-Pierre (préserver en figeant sur papier plutôt qu'en connectant au réseau).

### Placement dans les zones

Chaque boss créé par un viewer peut être rattaché à l'une des 6 zones existantes, en fonction de son lore et du choix de Florian au moment de l'intégration — en complément des boss de zone déjà définis dans ce document, pas nécessairement à leur place. Ça permet d'enrichir progressivement chaque zone avec du contenu généré par la communauté sans toucher à la structure de base.

**Points encore ouverts, à trancher plus tard :**
- Format exact de la fiche de personnage à donner aux viewers (quels champs, quelles limites de stats pour rester équilibré).
- Comportement du boss hors présence du créateur (inactif, IA basique, ou simplement non-invocable).
- Nombre de boss-joueurs actifs en simultané, et répartition entre les 6 zones.

---

## Z1 — Arbonet (Forêt centrale)

**Statut :** zone tutoriel, point de départ, l'Antre de Pointu s'y trouve.

- **Allié / donneur de quête :** Pointu lui-même. Premier contact, explique `!rejoindre`, `!choisirclasse`, `!arbonet`.
- **Ennemis mineurs :** Insectes-Bug (existants) — petites nuisances, servent de tutoriel de combat.
- **Boss de zone :** Sanglier-Virus — garde un chêne-serveur légèrement infecté en bordure de la clairière centrale visible sur la carte.
- **Lien lore :** la corruption y est visible mais encore faible. C'est ici que le joueur comprend que "quelque chose cloche" sans en saisir l'ampleur.

---

## Z2 — Plaines

**Statut :** zone ouverte, caravanes et voyageurs, ciel dégagé.

- **Allié / donneur de quête :** une caravanière-marchande, témoin d'un vol de souvenir en plein jour. Elle envoie le joueur enquêter.
- **Ennemis mineurs :** Corbeaux-Daemon (existants) — voleurs de données/souvenirs, logiques en terrain ouvert et ciel dégagé.
- **Boss de zone :** Faucon-Firewall — rapace qui garde un point de passage stratégique vers la Montagne.
- **Lien lore :** c'est ici que les Corbeaux-Daemon opèrent le plus activement. Battre le Faucon-Firewall ou fouiller son nid peut révéler où les souvenirs volés sont acheminés (→ Montagne).

---

## Z3 — Lacs

**Statut :** archipel d'îlots, chênes-serveurs partiellement immergés.

- **Allié / donneur de quête :** un pêcheur, gardien officieux des chênes-serveurs aquatiques.
- **Ennemis mineurs :** Castors-Rootkit (existants) — rongent et corrompent les chênes-serveurs.
- **Boss de zone :** une Loutre-Rootkit ou un lieutenant d'Hector-Pierre en éclaireur.
- **Lien lore :** zone clé narrativement — les Castors-Rootkit sont l'espèce d'Hector-Pierre lui-même. C'est ici que le joueur trouve les **premiers indices** sur son identité et sa motivation, sans encore le rencontrer.

---

## Z4 — Montagne enneigée

**Statut :** zone difficile, accès par le pont central visible sur la carte.

- **Allié / donneur de quête :** un ermite du froid, forme les joueurs à survivre en altitude (parallèle avec `!repos`).
- **Ennemis mineurs :** Loups-Firewall (existants) — meutes qui bloquent les passages.
- **Boss de zone :** Ours-Hexadecimeur — boss massif, résistant, point culminant de la carte.
- **Lien lore :** c'est ici que les Corbeaux-Daemon planquent une partie des souvenirs volés en Plaines. Une quête de récupération peut être déclenchée après Z2.

---

## Z5 — Désert

**Statut :** zone reculée, peu affectée par la corruption — sert de contraste.

- **Allié / donneur de quête :** un nomade, guide de caravane.
- **Ennemis mineurs :** Scorpions-Malware *(nouveau)* — infligent des dégâts sur la durée (poison/malware qui persiste).
- **Boss de zone :** Vautour-Rootkit *(nouveau)* — plane et attend l'épuisement du joueur avant d'attaquer.
- **Mini-boss :** **Mirage-Bug** — une anomalie qui prend la forme de ce que le voyageur veut voir. On la combat sans jamais être sûr de frapper au bon endroit.
- **Quête :** *L'Artefact de mémoire* (`artefact_06`, 5 ticks · 95 XP · 10 RAM). Le Désert ayant échappé à la corruption, c'est là qu'on a caché un artefact de mémoire **intact** avant que l'effritement ne commence.
- **⭐ Commerce :** la **boutique d'objets rares et légendaires** siège dans cette zone. C'est ce qui justifie qu'un joueur y revienne malgré son statut optionnel — et pourquoi Faîne l'Écureuil-Archive y tient comptoir plutôt que dans l'Antre.
- **Lien lore :** zone volontairement plus "saine" pour varier le rythme entre deux zones lourdes en lore (Lacs et Marais). Respiration narrative **et** place de marché haut de gamme.

---

## Z6 — Marais

**Statut :** repaire d'Hector-Pierre Castor. Zone finale de cette portion de carte.

- **Allié / donneur de quête :** une sorcière-guérisseuse, quêtes d'antidotes et de soins liées à la décomposition du marais.
- **Ennemis mineurs :** Serpents-Phishing *(nouveau)* — créent de fausses illusions/faux loot, cohérent avec le thème de tromperie.
- **Boss de zone :** Crocodile-Firewaller — garde l'accès au cœur du Marais.
- **Mini-boss :** **la Grenouille-Corrompue** — la seule créature corrompue qui n'a pas *entièrement* perdu la langue commune. On ne la bat pas en la tuant : on la bat en **lui parlant** (`!discuter`) jusqu'à ce que la corruption lâche prise. Elle se relève alors sur ses deux pattes — le seul cas connu de guérison à Arbonet. C'est **l'exception qui éclaire la règle** énoncée plus haut : la corruption fait tomber à quatre pattes et coupe la parole, mais elle n'est pas toujours irréversible. `!combat` la tue — et fait perdre au joueur ce qu'elle aurait pu lui confier.
- **Lien lore :** **c'est le repaire d'Hector-Pierre.** La décomposition et l'isolement du marais reflètent son deuil. C'est ici que la piste des souvenirs volés (Plaines → Montagne) et les indices des Lacs convergent. Zone de confrontation potentielle avec Hector-Pierre ou l'un de ses proches lieutenants.

---

## Tableau récapitulatif

| Zone | Niv. min | Allié (donneur de quête) | Ennemis mineurs | Boss d'arène |
|------|----------|--------------------------|------------------|--------------|
| Z1 Arbonet | 1 | **Pointu** | Drone-racine, Parasite de données, *Insecte-Bug (mini-boss)* | **Sanglier-Virus** (250 PV) |
| Z2 Plaines | 3 | **Braise le Renard-Routeur** *(la caravanière du doc)* | Martre-Trojan, Taupe-Malware, Ombre de la mémoire, *Corbeau-Daemon (mini-boss)* | **Faucon-Firewall** (350 PV) |
| Z3 Lacs | 5 | **Sillon le Blaireau-Racine** *(le pêcheur)* | Écrevisse-Cache, Brochet-Injection, *Castor-Rootkit (mini-boss)* | **Loutre-Rootkit** (420 PV) |
| Z4 Montagne | 6 | **Nyx la Chouette-Veille** *(l'ermite du froid)* | Marmotte-Veille, Lynx-Proxy, *Loup-Firewall (mini-boss)* | **Ours-Hexadecimeur** (550 PV) |
| Z5 Désert | 7 | **Faîne l'Écureuil-Archive** — elle tient la boutique rare/légendaire, installée ici | Fennec-Spoof, Scorpion-Malware, *Mirage-Bug (mini-boss)* | **Vautour-Rootkit** (450 PV) |
| Z6 Marais | 9 | **Bogue le Hérisson-Pare-feu** *(la sorcière-guérisseuse)* | Serpent-Phishing, Sentinelle du Castor, Sanglier-Crash, *Grenouille-Corrompue (mini-boss ♥)* | **Crocodile-Firewaller** (600 PV) puis **Hector-Pierre Castor** (700 PV, final) |

> Les alliés anonymes du document (caravanière, pêcheur, ermite, nomade, sorcière) ont été **remplacés par le casting déjà écrit** — Pointu, Braise, Sillon, Nyx, Bogue ont chacun leurs répliques de succès/échec dans `config_lore_textes.json`. Inventer cinq PNJ de plus aurait dédoublé le casting pour rien.

> **Les boss de zone sont les boss d'arène** (`!spawnboss`, tour par tour, capacités de classe) — décision du 21/08/2026. Reine-Bug, Munin-Daemon et Fenrir-Firewall sont retirés ; Hector-Pierre reste, en final de Z6. Les quatre mini-boss solo (Insecte-Bug, Corbeau-Daemon, Castor-Rootkit, Loup-Firewall) ne bougent pas et couvrent Z1→Z4.

---

## Pistes de progression suggérées

1. **Z1 (tutoriel)** → le joueur apprend les bases avec Pointu.
2. **Z2 Plaines** → premier vol de souvenir observé, piste vers la Montagne.
3. **Z4 Montagne** → récupération des souvenirs cachés par les Corbeaux.
4. **Z3 Lacs** → indices sur l'identité d'Hector-Pierre via les Castors-Rootkit.
5. **Z5 Désert** *(optionnelle)* → zone de respiration, quêtes annexes.
6. **Z6 Marais** → confrontation finale, tout converge chez Hector-Pierre.

Cet ordre n'est qu'une suggestion — les zones peuvent rester ouvertes/non-linéaires selon le format Twitch (commandes `!` accessibles à tout moment).

---

*Projet Pointu © Florian alias kikaby — 2026*
*Arbonet vous attend.*
