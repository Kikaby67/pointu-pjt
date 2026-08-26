# Les Chroniques d'Arbonet — Lore approfondi

### *Complément à LA\_LEGENDE\_DE\_POINTU\_V2.md — Cycle 404*

### Zones · Familles de la Corruption · Bestiaire · Quêtes

> ⚠️ **Périmé sur deux points depuis le 21/08/2026** — `ZONES_ALLIES_ENNEMIS.md` fait désormais foi pour :
> - **le découpage des zones** : 6 zones (Arbonet, Plaines, Lacs, Montagne, Désert, Marais) remplacent les 4 territoires ci-dessous ; « Vide Binaire » et « Forêt-Mémoire » n'existent plus comme zones de jeu.
> - **les boss d'arène** : 1 par zone. Reine-Bug, Munin-Daemon et Fenrir-Firewall sont retirés ; Hector-Pierre reste, en final du Marais.
>
> Tout le reste de ce document (fiches de créatures, ton, récompenses, textes) reste valide et sert de matière première.

> Ce document approfondit le lore **sur la base existante** : aucun ennemi, boss ou quête n'est inventé — tout ce qui a des stats dans les configs reçoit ici son histoire, sa zone et des textes de quête cohérents. Les valeurs chiffrées proposées (section 6\) restent à valider avant de toucher aux configs.

---

## 1\. Les Quatre Familles de la Corruption

La clé de voûte qui manquait : tes ennemis appartiennent déjà, par leurs noms, à **quatre lignées**. Chaque lignée a son mini-boss et son boss d'arène, et chaque lignée règne sur un territoire. Ce n'est pas un ajout — c'est une structure qui dormait dans tes fichiers.

| Famille | Territoire | Mini-boss (solo) | Boss d'arène (communautaire) |
| :---- | :---- | :---- | :---- |
| 🦟 **Les Bugs** | Forêt-Mémoire | Insecte-Bug | **Reine-Bug** (300 PV) |
| 🦅 **Les Daemons** | Plaines de Pixel | Corbeau-Daemon | **Munin-Daemon** (400 PV) |
| 🐺 **Les Firewalls** | Marais du Buffer | Loup-Firewall | **Fenrir-Firewall** (500 PV) |
| 🦫 **Les Rootkits** | Vide Binaire | Castor-Rootkit | **Hector-Pierre Castor** (600 PV) |

> ✅ **Correction appliquée.** La version précédente de ce document croisait les deux dernières familles — c'était une erreur : Hector-Pierre **est** un Castor (famille Rootkit) et Fenrir **est** un Firewall. Chaque boss commande désormais sa propre lignée, et son mini-boss est le petit frère de son boss.

**Ce que ça change (et ce que ça ne change pas) :**

- **Les PV des boss : rien à toucher.** 300 → 400 → 500 (Fenrir) → 600 (Hector) était déjà parfaitement croissant dans `config_ennemis.json`. C'est ma table qui était fausse, pas ta config.  
- **Les zones des deux dernières familles s'échangent** : les Firewalls tiennent le Marais, les Rootkits le Vide Binaire. Le lore y gagne : le Chapitre V dit que les Loups-Firewall « patrouillent **les zones corrompues** » — c'est la définition même du Marais. Et Hector-Pierre creuse au cœur du système, dans le Vide, avec ses Castors.  
- **⚠️ Seul ajustement nécessaire : échanger l'XP/RAM des deux mini-boss.** Aujourd'hui Castor-Rootkit vaut 40/8 et Loup-Firewall 60/12. Comme le Loup passe en 3ᵉ zone et le Castor en 4ᵉ, il faut inverser : **Loup-Firewall 40/8** (Marais) et **Castor-Rootkit 60/12** (Vide).

La progression des mini-boss et des boss suit alors exactement l'ordre des zones. Le lore ne fait que nommer ce que tes chiffres disaient déjà.

---

## 2\. Les Territoires — fiches détaillées

### 🌲 Forêt-Mémoire — *« Là où tout commence, là où tout est stocké »*

**Accès : niveau 1 · Famille : les Bugs**

La plus ancienne forêt d'Arbonet, celle où naquit le premier chêne-serveur. Ses arbres stockent la mémoire collective dans leurs anneaux de croissance — chaque cercle est une année de souvenirs, lisible par qui sait poser la main sur l'écorce. C'est ici que Pointu est né, ici que les anciens ont enfoui la vérité.

C'est aussi pour ça que la corruption y a envoyé ses agents les plus discrets : les Bugs. Pas de destruction spectaculaire — des anomalies minuscules, glissées une à une dans les anneaux. Un souvenir qui change imperceptiblement. Une date qui se décale. La Forêt-Mémoire ne brûle pas : elle se trompe, de plus en plus souvent.

**On y croise** : Drone-racine, Parasite de données · **Mini-boss** : Insecte-Bug · **Boss** : Reine-Bug **On y fait** : les premières quêtes de Pointu — retrouver des fragments encore intacts avant que les Bugs ne les atteignent.

### ⚡ Plaines de Pixel — *« Le ciel y est un écran, et quelque chose le regarde »*

**Accès : niveau 3 · Famille : les Daemons**

D'immenses étendues où l'hybridation s'est faite par fragments : des carrés de prairie parfaitement naturels côtoient des dalles de pur affichage, et l'horizon scintille comme un rendu jamais terminé. C'est le territoire du vent et de la vitesse — tout ce qui vole, tout ce qui court, finit par traverser les Plaines.

Les Daemons y prospèrent parce que les Plaines sont une **route** : tout ce qui circule entre les territoires passe ici, et eux se servent au passage. Ils ne tuent pas — ils prélèvent. Un souvenir par voyageur. Personne ne s'en aperçoit sur le moment ; c'est en rentrant chez soi qu'on découvre le trou.

**On y croise** : Martre-Trojan, Taupe-Malware, Ombre de la mémoire · **Mini-boss** : Corbeau-Daemon · **Boss** : Munin-Daemon **On y fait** : des quêtes d'escorte et de récupération — protéger ce qui traverse, reprendre ce qui a été prélevé.

### 🔥 Marais du Buffer — *« Ce qui déborde finit ici »*

**Accès : niveau 6 · Famille : les Rootkits**

Quand un chêne-serveur sature, l'excédent s'écoule. Depuis des cycles, tout ce qu'Arbonet n'arrive plus à traiter — données corrompues, souvenirs orphelins, fragments de code abandonnés — ruisselle vers cette dépression au sud, où il stagne et fermente. Le Marais n'est pas une zone morte : c'est une zone **saturée**, où la frontière entre le vivant et le corrompu se dissout dans une brume verdâtre.

C'est le territoire des Loups-Firewall : le Chapitre V dit qu'ils patrouillent les zones corrompues, et aucune ne l'est autant que celle-ci. Ils encerclent le Marais depuis des cycles pour l'empêcher de déborder sur le reste d'Arbonet — une quarantaine vivante. Hector-Pierre y envoie aussi ses convois : ses Sentinelles démontent les chênes-serveurs affaiblis par la saturation et remontent le bois vers ses chantiers.

Et au centre, il y a Fenrir. Le plus grand des gardiens, celui qui a tenu la frontière le plus longtemps — et qui a absorbé, cycle après cycle, tout ce que le Marais exhale.

**On y croise** : Sentinelle du Castor, Sanglier-Crash · **Mini-boss** : Loup-Firewall · **Boss** : Fenrir-Firewall **On y fait** : les quêtes dangereuses — renforcer la quarantaine, récupérer dans les zones saturées, intercepter les convois.

### 🌌 Vide Binaire — *« Il n'y a plus de nature ici. C'est bien le problème. »*

**Accès : niveau 9 · Famille : les Firewalls**

Le cœur digital pur d'Arbonet — l'endroit où le réseau existe sans support physique, un espace de logique nue où les lois de la forêt ne s'appliquent plus. Peu d'êtres vivants peuvent y pénétrer sans se dissoudre ; c'est pour ça que les Loups-Firewall en gardent les portes depuis toujours. Ce qu'ils protègent, personne ne le savait.

On le sait maintenant : c'est ici que convergent les racines du premier chêne-serveur. Ici que la vérité des anciens est enfouie. Et c'est ici qu'Hector-Pierre a percé — il a franchi ce que les Loups gardaient, il creuse vers les racines avec ses Castors-Rootkit, persuadé d'y trouver l'arme finale contre la technologie. Sans comprendre qu'il s'apprête à détruire le seul remède contre la maladie qui a réellement tué son monde.

**On y croise** : les chantiers de forage · **Mini-boss** : Castor-Rootkit · **Boss final** : Hector-Pierre Castor **On y fait** : les dernières quêtes — atteindre la vérité avant lui. C'est une course, pas une bataille.

### 🏛️ L'Antre — *hub, pas une zone de combat*

Le sanctuaire de Pointu, d'où partent toutes les quêtes et où les aventuriers reviennent se reposer. Dans le jeu : c'est le salon Discord `#lantre-de-pointu` (cf. RESTRUCTURATION\_POINTU\_V3.md §1.1) — l'endroit où Pointu parle, montre les Fragments de Carapace et confie les missions. Aucun ennemi n'y pénètre. `!repos`, `!profil`, `!inventaire` sont narrativement des actions faites *dans l'Antre*.

---

## 3\. Bestiaire complété — les fiches manquantes

Les 4 fiches existantes (Castor-Rootkit, Corbeau-Daemon, Loup-Firewall, Insecte-Bug) restent inchangées. Voici les 7 ennemis de rencontre qui n'avaient que des stats :

**🌱 Le Drone-racine** *(Forêt-Mémoire — faible)* Une racine morte réanimée par un essaim de Bugs qui l'habite comme un gant. Maladroit, lent, presque pitoyable — mais il y en a toujours un autre derrière. Les anciens disent que chaque Drone-racine était autrefois une racine du réseau vivant, et qu'on peut encore entendre, en le vainquant, l'écho du dernier message qu'elle a transporté.

**🦠 Le Parasite de données** *(Forêt-Mémoire — moyen)* Il ne ressemble à rien — littéralement : son corps est un agrégat de fragments volés, souvenirs à moitié digérés qui affleurent à sa surface comme des reflets. Il s'accroche aux anneaux des arbres-mémoire et boit. Vaincre un Parasite, c'est parfois rendre à quelqu'un un souvenir qu'il ne savait même pas avoir perdu.

**🦡 La Martre-Trojan** *(Plaines de Pixel — moyen)* Elle se présente toujours comme autre chose : un paquet de données inoffensif, un petit animal blessé, un cadeau abandonné sur la route. Puis on la laisse approcher. Les voyageurs des Plaines ont un dicton : *« Si c'est trop beau pour être vrai, c'est une Martre. »*

**🦔 La Taupe-Malware** *(Plaines de Pixel — moyen)* Elle creuse sous les dalles d'affichage des Plaines et ronge les connexions par en dessous. On ne la voit jamais venir — on voit juste le sol pixeliser en cercles concentriques, de plus en plus près. Les dégâts qu'elle cause ne se voient qu'après coup, quand un pan entier de prairie se fige en écran mort.

**👤 L'Ombre de la mémoire** *(Plaines de Pixel — moyen)* Le plus troublant des serviteurs des Daemons : une silhouette faite de tous les souvenirs que les Corbeaux ont volés et jetés, agglomérés en quelque chose qui marche. Elle ne veut pas se battre — elle veut *être quelqu'un*. Elle attaque parce qu'elle prend chaque voyageur pour le propriétaire du souvenir qui lui manque.

**🛡️ La Sentinelle du Castor** *(Marais du Buffer — moyen)* Les soldats d'élite d'Hector-Pierre : des Castors-Rootkit vétérans, équipés d'armures de bois pétrifié prélevé sur les chênes-serveurs abattus. Ils gardent les chantiers du Marais avec la discipline de ceux qui croient défendre une cause juste. Ce sont les seuls ennemis qui parlent parfois avant de frapper — toujours pour dire la même chose : *« Vous ne comprenez pas ce qu'ils vous ont pris. »*

**🐗 Le Sanglier-Crash** *(Marais du Buffer — fort)* Quand une créature du Marais absorbe trop de données saturées, elle plante. Le Sanglier-Crash est ce qui reste : une masse de muscle et d'erreurs système qui charge tout ce qui bouge, se fige quelques secondes — écran bleu, disent les anciens — puis recharge. Imprévisible, brutal, incapable de s'arrêter. Le tuer est une délivrance, pour lui aussi.

---

## 3bis. Le Cercle de Pointu — les animaux alliés

Pointu n'est pas seul. Avant que les aventuriers n'arrivent, d'autres créatures d'Arbonet l'écoutaient déjà — des animaux ordinaires de la forêt classique, que la Grande Mutation a touchés sans les corrompre. Ils ne se battent pas : ils tiennent le monde en état pendant que d'autres le défendent. Ce sont eux qui confient les quêtes.

Convention de nommage : *Prénom* \+ *Espèce-TermeTechnique*, comme les ennemis — mais côté vivant.

**🐿️ Faîne, l'Écureuil-Archive** *(Forêt-Mémoire — marchande)* Elle stocke. C'est tout ce qu'elle sait faire et elle le fait mieux que quiconque : des milliers de caches réparties dans toute la Forêt-Mémoire, chacune référencée dans une tête minuscule qui n'oublie jamais rien. Quand la corruption a commencé à effacer les anneaux, Faîne a été la première à comprendre ce qui se passait — parce qu'elle, elle savait ce qui aurait dû s'y trouver. C'est elle qui tient **la boutique** de l'Antre : elle échange, elle troque, elle ne donne jamais rien. *« Rien ne se perd chez moi. Rien ne se donne non plus. »*

**🦔 Bogue, le Hérisson-Pare-feu** *(Forêt-Mémoire — entretien)* Son nom est une double vérité : la bogue est l'enveloppe piquante qui protège la châtaigne, et le bug est ce qu'il passe ses journées à écraser. Il patrouille les racines de la Forêt-Mémoire, roulé en boule autour des nœuds fragiles, et confie aux aventuriers les réparations qu'il ne peut pas faire seul. Bourru, méthodique, incapable de dire merci autrement qu'en donnant du travail plus important la fois d'après.

**🦊 Braise, le Renard-Routeur** *(Plaines de Pixel — services et escortes)* Personne ne connaît les Plaines comme lui : quel chemin est encore stable, quelle dalle va se figer, quelle route les Daemons surveillent aujourd'hui. Il route les voyageurs, les convois, les messages — et il prend sa part au passage, toujours. Braise n'est pas un saint, et il ne prétend pas l'être. Mais il n'a jamais livré personne aux Corbeaux, et dans les Plaines, c'est une réputation qui vaut de l'or.

**🦡 Sillon, le Blaireau-Racine** *(Marais du Buffer — entretien lourd)* Il creuse les digues qui empêchent le Marais de remonter vers la Forêt. Un travail sans fin, sans gloire, que personne ne remarque tant qu'il tient. Sillon a perdu deux frères dans les zones saturées et n'en parle jamais ; il demande juste des bras quand une digue cède. Il est le seul allié qui envoie régulièrement les aventuriers là où Fenrir patrouille — et le seul qui prévienne honnêtement de ce qui les attend.

**🦉 Nyx, la Chouette-Veille** *(Vide Binaire — artefacts et vérité)* Elle ne dort pas. Jamais. Perchée à la lisière du Vide Binaire, elle observe ce que personne d'autre ne peut regarder sans se dissoudre, et note. Nyx est la plus proche de Pointu — la seule à qui il ait confié une partie de ce qu'il sait. Elle parle peu, par phrases coupées, et donne toujours l'impression de répondre à une question qu'on n'a pas encore posée. C'est elle qui envoie les aventuriers dans le Vide, quand ils sont prêts. Et elle seule décide de ce « prêt ».

---

## 3ter. 🐸 La Rainette-Cache — la rencontre bienveillante

**Nouvelle créature. Alliée, jamais ennemie.**

Une petite grenouille arboricole d'un vert impossible, qui apparaît sans bruit sur une branche, un rocher, le bord d'une digue — toujours au milieu d'une quête, jamais dans l'Antre. Son nom vient de ce qu'elle fait : elle *met en cache*. Elle absorbe un instant de chance — un rayon qui traverse les feuilles, un souvenir intact, un silence entre deux dangers — et le garde. Puis elle le recrache sur qui passe.

Personne ne sait pourquoi elle choisit certains voyageurs et pas d'autres. Faîne prétend qu'elle sent ceux qui vont en avoir besoin. Bogue dit qu'elle s'ennuie. Nyx, elle, ne dit rien du tout — et c'est peut-être la réponse la plus inquiétante.

**Ce qu'elle fait en jeu (proposition mécanique) :**

- Nouveau type de rencontre alliée : `rencontreType = "rainette_cache"`, aux côtés de `marchand_potion`, `vieux_sage`, `bonus_ram`, `alcove_chene`, `marchand_classe`.  
- `!accepter` → elle accorde un **buff pour le restant de la quête en cours** : `+rainette_bonus_combat_pct` (proposition : **\+15 %**) sur les jets de `!combat` jusqu'à la fin de la quête.  
- `!refuser` → elle disparaît sans rancune, aucune pénalité (c'est la seule rencontre 100 % sans risque du jeu — elle récompense la curiosité, pas le calcul).  
- Champs à ajouter au JSON joueur : `buffRainetteActif` (bool). Vidé au même endroit que `compagnonActif` — fin de quête, abandon, défaite KO.  
- Cumulable avec `compagnonActif` (+15 % déjà existant) : un joueur très chanceux peut donc atteindre \+30 %, plafonné par `combat_plafond_joueur` — le plafond existant protège déjà l'équilibrage, rien à ajouter.  
- Apparition **rare** : `rainette_frequence_pct` dans `config_allies.json` (proposition : **10 %** des rencontres alliées, comme le `marchand_classe`).

>   
> Pourquoi alliée et pas ennemie : le jeu a déjà 11 ennemis et 4 boss, mais une seule rencontre purement positive (`alcove_chene`). La Rainette équilibre le ressenti des quêtes — et elle donne à Arbonet un moment de douceur, ce qui rend la corruption d'autant plus lisible par contraste.

---

## 4\. Les Boss d'arène — fiches narratives

**👑 La Reine-Bug** *(Forêt-Mémoire — 300 PV)* Mère de tous les essaims. Elle niche au cœur d'un chêne-serveur creux dont elle a remplacé la mémoire par ses pontes : chaque œuf est une anomalie prête à éclore. Tant qu'elle vit, la Forêt-Mémoire se trompera de plus en plus. Elle n'est pas maligne — elle est *nombreuse*, et c'est bien pire.

**🐦‍⬛ Munin-Daemon** *(Plaines de Pixel — 400 PV)* Le premier des Corbeaux, celui dont toutes les plumes noires descendent. Son nom est le dernier vestige d'un très vieux monde : *Munin* — « la Mémoire ». L'ironie n'échappe à personne : la créature qui a volé le plus de souvenirs d'Arbonet porte le nom de ce qu'elle détruit. Certains disent qu'il ne vole pas par malice, mais parce qu'il a perdu quelque chose autrefois, et qu'il cherche encore. Personne n'a jamais pu le lui demander : il attaque avant.

**🐺 Fenrir-Firewall** *(Marais du Buffer — 500 PV)* Il fut le plus grand des gardiens — le Loup qui tenait la quarantaine du Marais, celui dont tous les autres Loups apprenaient. Son travail était d'absorber ce qui débordait pour que rien n'atteigne la Forêt. Il l'a fait. Cycle après cycle : données saturées, souvenirs pourris, code corrompu. Le gardien est devenu ce qu'il filtrait.

Il patrouille encore. C'est ça, le plus terrible : il fait toujours son devoir. Mais sa frontière n'existe plus que dans ce qui lui reste de mémoire, et tout ce qui s'en approche — aventurier, allié, Loup de sa propre meute — est traité comme une contamination à contenir.

**🦫 Hector-Pierre Castor** *(Vide Binaire — 600 PV — boss final)* *(Sa fiche complète est le Chapitre IV de la légende — rien à réécrire, elle est déjà excellente.)* Ce qui change : on sait désormais **où** il se trouve. Il a percé les défenses du Vide Binaire et creuse vers les racines du premier chêne-serveur, convaincu d'y trouver le cœur de la technologie à abattre. Il est à quelques cycles de détruire, sans le savoir, le seul remède contre la maladie qui a effacé son peuple. Le combat contre lui n'est pas une exécution — c'est une course.

---

## 5\. Les quêtes — réaffectées, réécrites, récompensées

### Principes

- Chaque quête reçoit un champ `_zone` (spec RESTRUCTURATION §4.1).  
- Le **type** garde sa logique actuelle : `artefact` \= retrouver un fragment (loot possible), `service` \= aider un habitant, `entretien` \= maintenir le réseau.  
- Les `_demandeur` sont désormais les **animaux du Cercle de Pointu** (section 3bis) — plus aucun PNJ humain. Chaque allié donne les quêtes de sa zone et de sa spécialité.  
- **Les ticks et l'XP ne changent pas.** Seule la **RAM est multipliée par 5** pour alimenter la boutique et l'échange entre joueurs (justification complète dans `ECONOMIE_ET_BOUTIQUE_V3.md`).

### 🌲 Forêt-Mémoire (niveau 1+)

| ID | Nom | Demandeur | Ticks | XP | RAM (av. → ap.) |
| :---- | :---- | :---- | :---- | :---- | :---- |
| artefact\_05 | L'Anneau effacé | Pointu | 1 | 10 | 1 → **5** |
| artefact\_04 | Le Souvenir du Drone | Nyx la Chouette-Veille | 2 | 30 | 3 → **15** |
| service\_05 | La Sève électrique | Faîne l'Écureuil-Archive | 2 | 40 | 4 → **20** |
| entretien\_01 | Les Racines emmêlées | Bogue le Hérisson-Pare-feu | 3 | 50 | 5 → **25** |

artefact\_05 : "Un anneau de croissance du vieux chêne s'est vidé cette nuit. Pointu veut savoir ce qu'il contenait — avant que l'oubli ne devienne définitif."

artefact\_04 : "Un Drone-racine abattu près de l'Antre transportait encore un message. Nyx a vu d'où il venait. Elle ne dira pas comment."

service\_05  : "Faîne a besoin de sève de chêne-serveur pour ses réserves — la vraie, celle qui luit en vert. Elle paie. Elle ne remercie pas, mais elle paie."

entretien\_01: "Bogue signale un nœud du réseau étranglé par des racines mortes. Ses piquants ne suffisent pas à le démêler. Il déteste devoir le demander."

### ⚡ Plaines de Pixel (niveau 3+)

| ID | Nom | Demandeur | Ticks | XP | RAM (av. → ap.) |
| :---- | :---- | :---- | :---- | :---- | :---- |
| artefact\_03 | La Plume noire | Nyx la Chouette-Veille | 3 | 50 | 5 → **25** |
| service\_01 | La Route surveillée | Braise le Renard-Routeur | 3 | 50 | 5 → **25** |
| service\_02 | Le Convoi éparpillé | Braise le Renard-Routeur | 4 | 70 | 7 → **35** |
| entretien\_02 | Les Dalles mortes | Bogue le Hérisson-Pare-feu | 4 | 70 | 7 → **35** |

artefact\_03 : "Une plume de Corbeau-Daemon est tombée dans les Plaines. Chaque plume est un souvenir volé — Nyx veut celui-ci. Elle n'a pas dit pourquoi, et on ne lui demande pas."

service\_01  : "Braise doit faire passer quelque chose par une route que les Daemons surveillent depuis trois cycles. Il connaît le chemin. Il lui manque quelqu'un devant."

service\_02  : "Un convoi que Braise avait routé s'est éparpillé en fuyant une Martre-Trojan. Rapporte ce qui peut l'être — et méfie-toi de ce qui semble trop facile à trouver."

entretien\_02: "Des dalles d'affichage se figent en écrans morts au nord des Plaines — du travail de Taupe-Malware. Bogue veut les connexions rétablies avant que la zone entière ne s'éteigne."

### 🔥 Marais du Buffer (niveau 6+)

| ID | Nom | Demandeur | Ticks | XP | RAM (av. → ap.) |
| :---- | :---- | :---- | :---- | :---- | :---- |
| artefact\_02 | Le Bois pétrifié | Nyx la Chouette-Veille | 5 | 80 | 8 → **40** |
| service\_03 | La Meute silencieuse | Sillon le Blaireau-Racine | 5 | 90 | 9 → **45** |
| entretien\_03 | La Digue saturée | Sillon le Blaireau-Racine | 5 | 90 | 9 → **45** |

artefact\_02 : "Les Sentinelles remontent du bois de chêne-serveur vers les chantiers d'Hector-Pierre. Dans l'un de ces troncs, un fragment de mémoire des anciens est encore lisible. Récupère-le avant qu'il ne devienne un livre."

service\_03  : "Trois Loups-Firewall de la quarantaine ont cessé de répondre. Sillon veut savoir s'ils sont morts ou s'ils sont devenus comme Fenrir. Il te prévient : les deux réponses sont mauvaises."

entretien\_03: "Une digue cède sous la saturation : si elle lâche, le Marais déborde vers la Forêt-Mémoire. Sillon a déjà perdu deux frères sur celle-là. Il n'en parlera pas."

### 🌌 Vide Binaire (niveau 9+)

| ID | Nom | Demandeur | Ticks | XP | RAM (av. → ap.) |
| :---- | :---- | :---- | :---- | :---- | :---- |
| artefact\_01 | La Première Racine | Pointu | 6 | 100 | 10 → **50** |
| service\_04 | Le Chantier de forage | Nyx la Chouette-Veille | 6 | 120 | 12 → **60** |

artefact\_01 : "Les racines du premier chêne-serveur convergent quelque part dans le Vide. Pointu t'envoie là où lui-même ne peut plus aller. Ce que tu en rapporteras changera ce que tu crois savoir d'Arbonet."

service\_04  : "Nyx a vu les Castors creuser. Ils approchent des racines. Elle ne te demande pas de les arrêter — elle te demande d'aller voir jusqu'où ils sont descendus. Et de revenir le lui dire."

> **Le Vide n'a que 2 quêtes** — c'est voulu pour l'instant (zone end-game, accès niveau 9). Si tu veux en ajouter, c'est du pur config (`quete014_*`...), zéro code. Je peux en écrire 2-3 de plus orientées "course contre Hector-Pierre" quand l'arc narratif (§4.2 de la restructuration) sera implémenté.

---

## 6\. Cohérence des récompenses — audit chiffré ✅ corrigé

### Règle posée

XP/RAM doivent croître avec (1) la zone, puis (2) le tier. **L'XP ne change pas** (ta courbe de niveaux est calibrée dessus) — **seule la RAM est ×5** pour alimenter la boutique.

### Les trois corrections d'XP appliquées

1. ✅ **Insecte-Bug (mini-boss Forêt) : 10 → 20 XP.** Il rapportait moins qu'un Drone-racine (15), simple ennemi *faible*, alors qu'il a un malus de \-35 % et un loot rare garanti. Un premier mini-boss doit se sentir.  
2. ✅ **Sanglier-Crash (fort) : 22 → 38 XP.** Il rapportait moins que la Sentinelle du Castor (30), qui n'est que *moyenne*. Le seul ennemi *fort* du jeu doit dominer les moyens.  
3. ✅ **Échange Loup-Firewall ↔ Castor-Rootkit** (conséquence de la correction des familles, section 1\) : Loup-Firewall 60 → **40 XP** (Marais, 3ᵉ zone), Castor-Rootkit 40 → **60 XP** (Vide, 4ᵉ zone).

### Table finale — ennemis (XP / RAM)

| Zone | Ennemi | Tier | XP | RAM (×5) |
| :---- | :---- | :---- | :---- | :---- |
| Forêt | Drone-racine | faible | 15 | 15 |
| Forêt | Parasite de données | moyen | 18 | 20 |
| Forêt | **Insecte-Bug** | miniboss | **20** ⚠️ | 25 |
| Forêt | **Reine-Bug** | boss | 80 | 150 |
| Plaines | Martre-Trojan | moyen | 20 | 20 |
| Plaines | Taupe-Malware | moyen | 20 | 20 |
| Plaines | Ombre de la mémoire | moyen | 25 | 25 |
| Plaines | **Corbeau-Daemon** | miniboss | 25 | 25 |
| Plaines | **Munin-Daemon** | boss | 100 | 200 |
| Marais | Sentinelle du Castor | moyen | 30 | 30 |
| Marais | Sanglier-Crash | fort | **38** ⚠️ | 40 |
| Marais | **Loup-Firewall** | miniboss | **40** ⚠️ | 40 |
| Marais | **Fenrir-Firewall** | boss | 120 | 250 |
| Vide | **Castor-Rootkit** | miniboss | **60** ⚠️ | 60 |
| Vide | **Hector-Pierre Castor** | boss final | 150 | 300 |

⚠️ \= valeur modifiée. Tout le reste est inchangé.

La chaîne est maintenant strictement croissante à l'intérieur de chaque zone :

faible \< moyen \< fort \< mini-boss \< boss   —   et chaque zone domine la précédente

> Un mini-boss de zone débutante (Insecte-Bug, 20\) vaut moins qu'un ennemi fort de zone avancée (Sanglier, 38\) : c'est voulu, **la zone prime sur le tier**.

### Autres valeurs RAM à multiplier par 5

boss\_recompense\_base\_ram   20  → 100    (versé à tous les participants)

boss\_top\_bonus\_ram         50  → 250    (meilleur dégâteur)

niveau 8 : bonus RAM      100  → 500

> ⚠️ **Impact sur les profils existants** : les joueurs actuels garderont leur ancien solde (petit) face aux nouveaux prix. Deux options — soit multiplier leur `ram` par 5 une fois (script de migration ponctuel), soit accepter le reset implicite. À trancher avant le premier stream post-mise à jour.

---

## 7\. Specs pour la session Claude Code

1. `config_quetes.json` : ajouter `_zone` ; remplacer `_nom`/`_demandeur`/`_description` par la section 5 ; **RAM ×5**, ticks et XP inchangés.  
2. `config_ennemis.json` : ajouter `_zone` (sections 2-4) ; appliquer les 3 corrections d'XP et la RAM ×5 de la section 6\.  
3. `config_global.json` : `rencontre_ennemis` / `rencontre_mini_boss` / `rencontre_boss` filtrés par zone — le tirage lit `zoneActuelle` et ne propose que les ennemis du territoire courant. Multiplier aussi `boss_recompense_base_ram` et `boss_top_bonus_ram` par 5\.  
4. `config_allies.json` : ajouter la **Rainette-Cache** (section 3ter) — `rainette_frequence_pct` (10) et `rainette_bonus_combat_pct` (15).  
5. JSON joueur : nouveaux champs `zoneActuelle` et `buffRainetteActif` (vidé aux mêmes endroits que `compagnonActif`).  
6. `config_level.json` : bonus RAM du niveau 8 : 100 → 500\.  
7. Les fiches des sections 3, 3bis, 3ter et 4 alimentent `config_lore_textes.json` (spec §4.3) — chaque fiche donne le ton des variantes `_victoire_XX` / `_defaite_XX`.  
8. Déblocage des zones : Forêt 1 · Plaines 3 · Marais 6 · Vide 9 — en config (`zone_plaines_niveau_min`…), jamais en dur.  
9. **Boutique, items et échange entre joueurs : voir le document dédié `ECONOMIE_ET_BOUTIQUE_V3.md`.**  
10. Ce document rejoint `Lore/` dans le repo, à côté de `LA_LEGENDE_DE_POINTU_V2.md` (qu'il complète) et `BESTIAIRE.md` (dont il comble les trous).

---

*Document vivant — Cycle 404\.* *Projet Pointu-PJT © Florian alias kikaby67 — 2026*  
