# La Légende de Pointu — Trame de campagne pour table classique
### *Guide du Maître du Jeu — Cycle 404*

---

> *"Le monde n'a pas besoin de héros. Il a besoin de gens qui savent attendre, observer, et agir au bon moment."*
> — **Pointu, Cycle 404**

---

## Comment utiliser ce document

Ce document adapte le lore et le système déjà existants de **Pointu-PJT** (le mini-jeu Twitch) en une **campagne jouable à table** — papier ou Discord vocal, avec de vrais joueurs qui incarnent des personnages, lancent des dés et dialoguent.

Il ne remplace pas les documents de lore (`Lore/*.md`) — il s'appuie dessus. Je m'y suis tenu strictement :
- **La carte** : six zones, dans l'ordre `Arbonet (1) → Plaines (3) → Lacs (5) → Montagne (6) → Désert (7) → Marais (9)`. Les niveaux entre parenthèses sont ceux du jeu Twitch (accès par palier) — à table, tu contrôles le rythme toi-même, voir plus bas.
- **La langue commune, la règle de corruption, le vocabulaire fragment/écaille/écorce** : identiques à `ZONES_ALLIES_ENNEMIS.md`. Rien n'est modifié.
- **Le casting** : Pointu (Arbonet), Braise le Renard-Routeur (Plaines), Sillon le Blaireau-Racine (Lacs), Nyx la Chouette-Veille (Montagne), Faîne l'Écureuil-Archive (Désert), Bogue le Hérisson-Pare-feu (Marais).
- **Les boss d'arène retirés** (Reine-Bug, Munin-Daemon, Fenrir-Firewall) n'apparaissent nulle part ici — seuls les boss de zone actuels sont utilisés.

Chaque séance suit le même moule : **texte d'ouverture prêt à lire**, **PNJ et répliques**, **rencontres**, **ce que les joueurs découvrent**, **boss ou événement clé**, **accroche vers la suite**. Tu peux lire les textes en italique tels quels ou t'en servir de squelette pour improviser avec tes propres mots — les deux fonctionnent.

---

## 1. Le système à table

Tu as déjà un système complet et cohérent dans `Lore/FICHES_CLASSES.md` : c'est **la version tour par tour originale** du jeu (avant que le bot Twitch ne la simplifie en un seul jet pour tenir la cadence du chat). Elle est parfaite pour une table — utilise-la telle quelle.

**Résumé rapide :**
- Chaque personnage a **PV, CA (Classe d'Armure), Mana, Charisme, Bonus d'Attaque**, une arme et un dé de dégâts propres à sa classe.
- `!attaque` devient : jet de **d20 + bonus d'attaque** ≥ CA de la cible → dégâts au dé de classe.
- `!soin`, `!defense`, `!fuir` fonctionnent comme décrit dans les fiches de classe (coût en mana, effet, seuil de réussite).
- **Initiative** (absente du bot, à ajouter à table) : chaque participant lance **1d20 + Agilité** (valeur de classe, voir `config_classes.json` si besoin — sinon utilise le Charisme à défaut) en début de rencontre. Ordre décroissant.
- **Sous-classe au niveau 5** : moment fort de mi-campagne, à mettre en scène (voir Séance 3 ou 4 selon le rythme de ton groupe).

**Progression** : deux options, à choisir selon ton groupe —
- **XP réel** : utilise la table de `Lore/FICHES_CLASSES.md` (300 / 900 / 2 700 / 6 500 / 14 000…) et les XP de quêtes/ennemis du `Lore/BESTIAIRE.md`.
- **Paliers narratifs (recommandé pour une campagne en 8 séances)** : niveau 2 à la fin de la Séance 1 (Arbonet), niveau 3 à la fin de la Séance 2 (Plaines), niveau 4 pendant la Séance 3 (Montagne), **niveau 5 et choix de sous-classe** à la fin de la Séance 3, niveau 6 à la fin de la Séance 4 (Lacs), niveau 7-8 pendant/après la Séance 5 (Désert), niveau 9 à l'entrée du Marais (Séance 6), niveau 10 juste avant la confrontation finale (Séance 7).

**Ennemis sans fiche complète** (Écrevisse-Cache, Brochet-Injection, Marmotte-Veille, Lynx-Proxy, Fennec-Spoof, Scorpion-Malware, Serpent-Phishing — introduits par `_tier` dans les configs mais sans PV/CA/dégâts détaillés) : calibre-les sur l'ennemi du même tier déjà chiffré dans `Lore/BESTIAIRE.md`. Un ennemi *faible* ≈ Drone-racine (15 PV / CA 10 / 1d4) ; *moyen* ≈ Taupe-Malware ou Martre-Trojan (20-30 PV / CA 12-13 / 1d6) ; *fort* ≈ Sanglier-Crash (35 PV / CA 9 / 1d8). Rien à valider dans les configs — c'est une astuce de table, pas une donnée à répercuter dans le code.

---

## 2. Le fil rouge de la campagne

**Ce que le groupe ne sait pas au départ, et découvre par étapes :**

1. Des souvenirs disparaissent d'Arbonet. Les Corbeaux-Daemon les volent en **Plaines**.
2. Les souvenirs volés sont planqués en **Montagne**, sous la garde des Loups-Firewall.
3. Les indices retrouvés en Montagne, croisés avec ceux glanés aux **Lacs** (territoire des Castors-Rootkit), pointent vers l'identité du responsable : **Hector-Pierre Castor**.
4. Le **Désert**, presque intact, offre une respiration — et une preuve que le monde valait la peine d'être sauvé *avant* la corruption.
5. Tout converge au **Marais**, repaire d'Hector-Pierre, où la piste, les indices et la vérité se rejoignent pour la confrontation finale.

Pointu connaît une partie de la vérité depuis le début (la mutation était une vaccination, pas une invasion) mais ne la révèle jamais frontalement — il sème, il attend, il fait confiance aux joueurs pour assembler les fragments eux-mêmes. **Ne le fais jamais l'exposer d'un bloc.** S'il en dit trop, une séance perd son moteur.

---

## 3. Séance 0 — L'Antre, l'appel de Pointu

**Niveau : création de personnage · Durée indicative : 1 séance courte ou en ouverture de la Séance 1**

### Ce que les joueurs ne savent pas encore
Rien du tout — c'est le point d'entrée. Ils ne comprennent même pas la langue commune d'Arbonet tant qu'ils n'ont pas reçu leur fragment.

### Texte d'ouverture *(à lire)*

> *Vous ne savez pas comment vous êtes arrivés ici. Une seconde vous étiez ailleurs — et maintenant vous êtes debout dans une clairière tapissée de mousse phosphorescente, sous un dôme de racines qui pulsent d'une lueur verte, lente, régulière, comme un cœur qui bat au ralenti.*
>
> *Face à vous, une tortue ancienne. Sa carapace est parcourue de lignes lumineuses, comme des circuits gravés dans l'écorce. Elle ne bouge pas vite. Elle n'a pas besoin de bouger vite.*
>
> *Autour d'elle, des silhouettes s'affairent — certaines à quatre pattes, certaines debout, habillées, qui parlent entre elles dans une langue que vous ne comprenez pas. Aucune ne vous prête attention. Comme si vous n'étiez pas tout à fait là.*
>
> *La tortue, elle, vous regarde. Et elle, vous la comprenez parfaitement.*

### Pointu parle *(texte déjà validé, `ZONES_ALLIES_ENNEMIS.md`)*

> *"Tu ne comprends pas encore ce que disent les miens. C'est normal — la langue commune d'Arbonet ne s'entend pas, elle se ressent, à travers ce que je porte sur le dos depuis toujours.*
>
> *Je peux te parler, moi, parce que cette carapace est mienne. Mais eux — les marchands, les gardiens, les créatures qui rôdent dans les zones — resteront silencieux pour toi tant que tu n'en porteras pas un morceau.*
>
> *Prends ce fragment. Il te permettra de comprendre ceux que tu croiseras. Et si le besoin s'en fait sentir — car tout le monde n'a pas gardé sa raison ici — il te permettra aussi de te défendre.*
>
> *Arbonet a besoin de témoins. Es-tu prêt à en devenir un ?"*

### Déroulé de table

1. Chaque joueur, à tour de rôle, **accepte le fragment** — c'est le moment de la création de personnage : classe choisie en fiction (l'un des cinq archétypes de `FICHES_CLASSES.md`), jets de création (PV, CA, bonus d'attaque).
2. Dès qu'un personnage porte son fragment, **le monde s'allume pour lui** : les silhouettes autour de la clairière deviennent audibles, compréhensibles. Marque bien cette bascule à voix haute — c'est le seul moment de toute la campagne où l'incompréhension devient clarté d'un coup.
3. Pointu explique la situation en une poignée de phrases (l'oubli qui progresse, les fragments de vérité disséminés, personne ne l'a cru) — **sans jamais parler d'Hector-Pierre par son nom**. Il dit juste : *"Il y a quelqu'un derrière tout ça. Vous le découvrirez à votre rythme. Je préférerais que ce soit par vous-mêmes que par moi."*
4. Il indique l'existence des zones, mais laisse le groupe libre de son ordre — recommandation : commencer par Arbonet elle-même (la forêt autour de l'Antre), la seule où la corruption reste discrète.

### Option de table — un joueur refuse

Si un joueur veut jouer ce refus (voir `ZONES_ALLIES_ENNEMIS.md`, "chemin alternatif") : le personnage est expulsé de la scène, puis abordé en aparté par une présence patiente, presque triste — Hector-Pierre lui-même, ou l'un de ses émissaires. Traite ça comme une **scène à deux**, en dehors de la table commune si possible (message privé, aparté). Voir l'Annexe B pour une version adaptée au jeu de table.

### Accroche de fin
Pointu remet, en plus du fragment, une **carte incomplète** d'Arbonet — les six zones dessinées, mais floues, comme redessinées de mémoire. *"Elle se précisera. Zone après zone."* (Un bel objet à donner physiquement à la table, si tu joues en présentiel.)

---

## 4. Séance 1 — Arbonet, la forêt de l'Antre

**Niveau conseillé : 1 → 2**

### Ce que les joueurs découvrent
Que la corruption est réelle, discrète, et déjà là — sous forme d'anomalies minuscules plutôt que de monstres spectaculaires. C'est un tutoriel de ton autant que de mécaniques.

### Texte d'ouverture *(à lire)*

> *La forêt autour de l'Antre ressemble, au premier regard, à n'importe quelle forêt. Puis vous remarquez les détails : une écorce qui scintille par endroits d'un vert électrique, des racines qui courent en surface comme des câbles enterrés à moitié, un silence qui revient un peu trop vite après chaque bruit — comme si la forêt oubliait ce qu'elle venait d'entendre.*
>
> *Quelque part entre deux arbres, un anneau de croissance a l'air vide. Littéralement vide — un cercle blanc dans le bois, là où il devrait y avoir une année entière de mémoire.*

### PNJ clé — Pointu (donneur de quête principal de la zone)
> 🐢 *"Un anneau de croissance du vieux chêne s'est vidé cette nuit. Je veux savoir ce qu'il contenait — avant que l'oubli ne devienne définitif."*

Autres répliques utiles (succès / échec) déjà écrites, ton à conserver : *"Tu as bien fait de le rapporter avant l'oubli."* / *"L'oubli avance plus vite que nous."*

### Rencontres de la zone
| Ennemi | Rôle | Notes de table |
|---|---|---|
| Drone-racine | Faible, tutoriel de combat | CA 10, facile à toucher — sert à montrer les règles sans risque |
| Parasite de données | Faible/moyen, combat d'usure | Dégâts faibles mais ne fuit jamais — bon pour montrer le mana/soin |
| Insecte-Bug *(mini-boss)* | Inoffensif seul, dangereux en essaim | Fais-en apparaître 3-4 d'un coup pour un vrai moment de mini-boss, plutôt qu'un seul Insecte-Bug isolé |

### Ce que la session révèle
Le premier fragment de vérité : la corruption **grignote**, elle ne détruit pas d'un coup. Les joueurs comprennent qu'ils ne combattent pas une armée, mais quelque chose de plus insidieux.

### Boss de zone — Sanglier-Virus (250 PV)

*Texte d'intro :* *"Le chêne qu'il garde n'est infecté qu'en bordure — une plaie superficielle comparée à ce qui vous attend ailleurs. Mais le Sanglier-Virus, lui, ne fait pas de nuances. Il charge quiconque approche, sain ou corrompu, ami ou ennemi. C'est peut-être la dernière chose qu'il sait encore faire."*

*Victoire :* *"Le Sanglier-Virus s'effondre contre l'écorce qu'il croyait protéger. Le chêne, derrière lui, respire un peu mieux."*
*Défaite/fuite :* *"Vous reculez. Le Sanglier ne vous poursuit pas — il retourne charger sa propre ombre, encore et encore."*

### Accroche de fin
En fouillant la clairière du Sanglier, un objet incongru : **une plume noire**, bien trop grande pour venir d'un oiseau d'Arbonet. Pointu, en la voyant, se tait plus longtemps que d'habitude. *"Ça vient des Plaines. Ça ne devrait pas être ici."*

---

## 5. Séance 2 — Plaines de Pixel

**Niveau conseillé : 3**

### Ce que les joueurs découvrent
Le mécanisme du vol : les Corbeaux-Daemon prélèvent des souvenirs en pleine circulation, sur les routes. Et une piste claire vers la Montagne.

### Texte d'ouverture *(à lire)*

> *Le ciel des Plaines est trop net. Un horizon qui scintille comme un rendu jamais tout à fait terminé, des dalles de pur affichage qui affleurent entre les herbes hautes. Le vent y porte loin — et avec lui, quelque chose qui ressemble à des cris d'oiseaux, mais en accéléré, comme rejoués trop vite.*
>
> *Sur la route, une caravane est arrêtée. Un renard roux, une besace en bandoulière, les inspecte d'un œil qui a déjà évalué votre valeur avant même que vous n'ouvriez la bouche.*

### PNJ clé — Braise le Renard-Routeur
> 🦊 *"Je dois faire passer quelque chose par une route que les Daemons surveillent depuis trois cycles. Je connais le chemin. Il me manque quelqu'un devant."*

Ton du personnage : **pragmatique, jamais tout à fait honnête, jamais tout à fait malhonnête**. Il prend toujours sa part. Il n'a jamais livré personne aux Corbeaux — c'est sa seule ligne rouge, et elle compte pour lui.
> Succès : *"Tu m'as fait passer. Je prends ma part, comme toujours — mais tu m'as fait passer."*
> Échec : *"Dans les Plaines, un convoi perdu, ça arrive."* (il hausse les épaules, sans rancune)

### Rencontres de la zone
| Ennemi | Rôle | Notes de table |
|---|---|---|
| Martre-Trojan | Embuscade, mimétisme | Interdit le `!fuir`/retrait au premier tour — elle est toujours "déjà là" avant le combat |
| Taupe-Malware | Sabotage furtif | Sa première attaque touche automatiquement (elle frappe "par en dessous") |
| Ombre de la mémoire | Spectre errant, presque tragique | Immunisée au charme/charisme — elle ne cherche pas à convaincre, elle cherche *quelqu'un* |
| Corbeau-Daemon *(mini-boss)* | Voleur, fuit sous 3 PV | S'il s'échappe, un joueur perd temporairement un point de sa fiche (au choix du MJ, narrativement réversible plus tard) |

### Ce que la session révèle
En suivant un Corbeau-Daemon (ou en fouillant son nid après le mini-boss), le groupe trouve des traces qui remontent clairement vers la **Montagne** — cachette des souvenirs volés.

### Boss de zone — Faucon-Firewall (350 PV)

*Texte d'intro :* *"Il garde le point de passage vers la Montagne comme si c'était sa propre frontière. Peut-être que ça l'est devenu. Il ne descend jamais en dessous des nuages — sauf pour attaquer."*

*Victoire :* *"Le Faucon-Firewall s'écrase, ailes déployées une dernière fois. Le passage vers la Montagne est ouvert."*

### Accroche de fin
Dans le nid du Faucon, un fragment de mémoire — flou, incomplet, mais assez pour voir une **silhouette de castor**, de dos, en train de creuser quelque chose. Personne à table ne sait encore qui c'est. Toi, si.

---

## 6. Séance 3 — Montagne enneigée

**Niveau conseillé : 4 → 6 · Sous-classe débloquée en cours de séance**

### Ce que les joueurs découvrent
Où sont cachés les souvenirs volés — et une partie de la vérité sur la corruption : le mal circule par le vivant, et la Montagne, minérale, y échappe presque.

### Texte d'ouverture *(à lire)*

> *L'air se raréfie à mesure que vous montez. Le froid n'est pas hostile — il est juste indifférent, ce qui est peut-être pire. Ici, la corruption n'a presque pas prise : pas assez de sève, pas assez de racines pour qu'elle voyage. C'est la première fois depuis l'Antre que l'air vous semble... propre.*
>
> *Un pont suspendu, taillé dans des câbles de données figées en glace, mène à l'entrée du territoire. Une chouette est perchée sur le premier pilier, immobile depuis si longtemps qu'elle pourrait être une statue — jusqu'à ce qu'elle tourne la tête vers vous, d'un seul geste, sans un bruit.*

### PNJ clé — Nyx la Chouette-Veille
> 🦉 *"Bientôt. Pas encore."* (elle répond souvent à des questions qu'on ne lui a pas posées)

Ton du personnage : **économe en mots, jamais surprise, toujours en avance d'un coup**. C'est la plus proche confidente de Pointu — elle sait des choses qu'elle ne dit qu'au compte-goutte.
> Elle confie une quête liée aux souvenirs cachés : *"Les Corbeaux planquent ce qu'ils volent ici. Trouve où. Je ne te dirai pas comment je le sais."*

### Rencontres de la zone
| Ennemi | Rôle | Notes de table |
|---|---|---|
| Marmotte-Veille | Faible, sentinelle discrète | Calibre-la sur Drone-racine (faible) — elle alerte plutôt qu'elle n'attaque |
| Lynx-Proxy | Moyen, rapide | Calibre-le sur Taupe-Malware/Martre-Trojan (moyen) — avantage à l'initiative |
| Loup-Firewall *(mini-boss)* | Gardien, résiste à la magie | -1 dégât reçu par attaque magique ; ne fuit jamais |

### Le moment clé — les souvenirs retrouvés
Quelque part dans une grotte ou une crevasse, le groupe retrouve une cache de souvenirs volés — des fragments de mémoire d'Arbonet, littéralement stockés comme des objets. Parmi eux, un souvenir plus ancien, plus profond, qui montre (au choix du MJ, en vision ou en texte lu) **la culture du peuple d'Hector-Pierre avant la mutation** — des digues, des bibliothèques de bois, une communauté entière. Sans le nommer encore.

*C'est le moment idéal pour la montée au niveau 5 et le choix de sous-classe : mets-le en scène comme un instant de bascule personnelle, en écho à ce que le personnage vient de comprendre du monde.*

### Boss de zone — Ours-Hexadecimeur (550 PV)

*Texte d'intro :* *"Le plus massif des gardiens de Montagne. Il ne garde rien de précis — il EST la Montagne, dans ce qu'elle a de plus intraitable. On ne le bat pas en le comprenant. On le bat en tenant plus longtemps que lui."*

*Victoire :* *"L'Ours-Hexadecimeur s'effondre dans la neige, qui commence déjà à recouvrir sa masse. La Montagne redevient silencieuse — pas vide. Silencieuse."*

### Accroche de fin
Nyx, en voyant ce que le groupe a rapporté, laisse tomber un nom pour la première fois — presque malgré elle : *"Hector-Pierre."* Puis, devant leur silence : *"Vous ne savez pas encore ce que ça veut dire. Allez aux Lacs. Sillon, lui, pourra vous le dire — si vous savez écouter ce qu'il ne dit pas."*

---

## 7. Séance 4 — Lacs

**Niveau conseillé : 6 → 7**

### Ce que les joueurs découvrent
Qui est Hector-Pierre — son espèce, son deuil, sa conviction — sans jamais le rencontrer en personne. C'est la séance de la **compréhension**, pas de l'action spectaculaire.

### Texte d'ouverture *(à lire)*

> *Un archipel de petits îlots, reliés par des pontons de bois-données qui grincent à chaque pas. Sous la surface, on devine des chênes-serveurs à moitié immergés, leurs racines dérivant dans l'eau trouble comme des cheveux. Quelque chose ronge, quelque part sous la surface — un bruit sourd, régulier, presque une respiration.*
>
> *Un blaireau est assis au bord de l'eau, les mains couvertes de terre, à surveiller une digue de fortune qui tient à peine.*

### PNJ clé — Sillon le Blaireau-Racine
> 🦡 *"Trois Loups-Firewall de la quarantaine ont cessé de répondre. Je veux savoir s'ils sont morts, ou s'ils sont devenus comme lui. Je te préviens : les deux réponses sont mauvaises."*

Ton du personnage : **taiseux, endurant, marqué**. Il a perdu deux frères dans les zones saturées et n'en parle jamais directement — mais c'est visible dans tout ce qu'il fait.

### Rencontres de la zone
| Ennemi | Rôle | Notes de table |
|---|---|---|
| Écrevisse-Cache | Faible, aquatique | Calibre-la sur Drone-racine |
| Brochet-Injection | Moyen, embuscade sous l'eau | Calibre-le sur Ombre de la mémoire (moyen, dégâts 1d8) |
| Castor-Rootkit *(mini-boss)* | Soldat d'Hector-Pierre, méthodique | CA 16 — le plus blindé de son niveau. Jamais seul : 1-2 Insectes-Bug l'accompagnent |

### Le moment clé — comprendre Hector-Pierre
En affrontant ou en observant les Castors-Rootkit, le groupe assemble ce que Nyx a laissé entendre et ce que la Montagne a révélé : **Hector-Pierre est un Castor**. Son peuple avait bâti une culture entière — digues, bibliothèques de bois, transmission orale — engloutie par la mutation en quelques cycles, remplacée sans qu'on le lui explique jamais. Il ne détruit pas par malice. Il combat ce qui, à ses yeux, a effacé son monde.

*Ne fais jamais dire ce texte tel quel par un PNJ — laisse les joueurs le reconstituer à partir d'indices épars (une réplique de Sentinelle croisée plus tôt, un objet trouvé aux Lacs, une phrase de Sillon). C'est un puzzle narratif, pas un exposé.*

### Boss de zone — Loutre-Rootkit (420 PV)

*Texte d'intro :* *"Une éclaireuse d'Hector-Pierre, envoyée surveiller les Lacs avant que ses Sentinelles n'y installent un chantier. Elle nage plus vite que vous ne pouvez viser, et elle sait exactement pourquoi elle se bat."*

### Accroche de fin
Sillon, en guise d'au revoir : *"Le Désert, c'est calme. Vous devriez y aller avant le Marais. Pas parce qu'il le faut. Parce que vous en aurez besoin."*

---

## 8. Séance 5 — Désert *(respiration, optionnelle mais recommandée)*

**Niveau conseillé : 7 → 8**

### Ce que les joueurs découvrent
Ce à quoi ressemblait Arbonet avant l'oubli — et donc, ce qu'il y a vraiment à sauver. C'est la seule zone quasi épargnée par la corruption : joue-la comme un contraste volontaire après trois zones lourdes.

### Texte d'ouverture *(à lire)*

> *Le silence, ici, n'a rien d'inquiétant. Pas de sève qui goutte, pas de racines qui craquent — juste du sable, de la chaleur sèche, et un ciel d'un bleu presque insultant après le Marais qui approche. Pour la première fois depuis l'Antre, vous respirez sans y penser.*
>
> *Une écureuille tient boutique à l'ombre d'un rocher sculpté en étagères — des objets rares, soigneusement rangés, aucun prix affiché. Elle vous regarde arriver avec l'expression de quelqu'un qui a déjà évalué ce que vous pouvez lui apporter.*

### PNJ clé — Faîne l'Écureuil-Archive
> 🐿️ *"Rien ne se perd chez moi. Rien ne se donne non plus."*

Elle tient la **boutique d'objets rares et légendaires** — moment idéal pour équiper le groupe avant le Marais. Elle confie aussi une quête : retrouver un artefact de mémoire *intact*, cachée avant même le début de la corruption.
> *"Le Désert n'a presque pas été touché. C'est pour ça qu'on y a caché un artefact intact, avant que tout ne commence à s'effriter. Je sais où chercher. Je n'ai jamais eu le courage d'y aller seule."*

### Rencontres de la zone
| Ennemi | Rôle | Notes de table |
|---|---|---|
| Fennec-Spoof | Faible, illusion | Calibre-le sur Drone-racine |
| Scorpion-Malware | Moyen, dégâts sur la durée | Poison léger qui persiste 1-2 tours après le combat |
| Mirage-Bug *(mini-boss)* | Anomalie qui prend la forme de ce qu'on veut voir | Joue-le comme un combat où le joueur ne sait jamais s'il frappe la vraie cible — décris des coups qui semblent toucher sans certitude |

### Ce que la session révèle
L'artefact intact — à toi de décider ce qu'il contient exactement, mais garde-le **positif et concret** : la preuve tangible qu'Arbonet, avant la mutation, avait déjà quelque chose qui valait d'être défendu. Une antithèse directe au deuil d'Hector-Pierre.

*Cette zone n'a pas de boss d'arène classique imposé narrativement comme point de passage obligé — le Vautour-Rootkit (450 PV) peut être joué comme une rencontre optionnelle de haut niveau, ou gardé en réserve pour une séance annexe si le groupe veut prolonger la campagne.*

### Accroche de fin
Faîne, en les regardant repartir vers le Marais : *"Vous allez chez lui. Je ne vous retiens pas — mais revenez. Je tiens mes comptes, même pour les vivants."*

---

## 9. Séance 6 — Marais du Buffer, la Quarantaine

**Niveau conseillé : 9**

### Ce que les joueurs découvrent
L'ampleur du deuil d'Hector-Pierre, et la possibilité — rare — d'une guérison plutôt que d'une victoire par les armes.

### Texte d'ouverture *(à lire)*

> *La brume ici ne se lève jamais tout à fait. Le sol cède sous vos pas comme s'il hésitait à vous porter. Tout ce qu'Arbonet n'a pas su traiter finit par s'écouler vers cette dépression et y stagne — des données corrompues, des souvenirs orphelins, du bois qui n'a jamais fini de pourrir. Ce n'est pas une zone morte. C'est une zone qui déborde.*
>
> *Un hérisson roulé en boule surveille un nœud de racines à moitié étranglées, sans lever les yeux à votre approche — pas par mépris, juste par habitude.*

### PNJ clé — Bogue le Hérisson-Pare-feu
> 🦔 *"Ça tiendra."* (son plus grand compliment)

Ton du personnage : **bourru, méthodique, incapable de dire merci autrement qu'en confiant un travail plus lourd**. Il confie la dernière quête d'entretien avant la confrontation finale.

### Rencontres de la zone
| Ennemi | Rôle | Notes de table |
|---|---|---|
| Serpent-Phishing | Moyen, tromperie | Fait apparaître un faux butin ou une fausse sortie — un ennemi qui ment par nature |
| Sentinelle du Castor | Moyen, **parle** | Seul ennemi (avec Hector-Pierre) qui garde la parole — soldate volontaire, pas une bête infectée. Réplique récurrente : *"Vous ne comprenez pas ce qu'ils vous ont pris."* Joue-la avec dignité, jamais comme un sbire jetable |
| Sanglier-Crash | Fort, imprévisible | CA 9 (facile à toucher), 35 PV (long) — combat d'attrition brutal |

### Le moment clé — la Grenouille-Corrompue (mini-boss, sauvable)

C'est l'unique cas de guérison connu à Arbonet. **Ne la joue jamais comme un combat par défaut.** Présente-la clairement à quatre pattes, corrompue, comme n'importe quel ennemi — puis laisse un joueur tenter de lui parler.

*Texte d'intro :* *"Elle est là, tapie sur une pierre à moitié engloutie, à quatre pattes, muette comme tout ce que la corruption a touché. Sauf que quelque chose, dans son regard, hésite encore."*

Si un joueur **dialogue** avec elle (jet de charisme ou action de RP dédiée) plutôt que de l'attaquer : elle se redresse, lentement, sur ses deux pattes — et parle, pour la première fois. *C'est le seul instant de guérison de toute la campagne. Prends ton temps dessus.* Si le groupe la combat directement, elle meurt comme n'importe quel ennemi — sans révéler ce qu'elle aurait pu confier.

### Accroche vers la Séance 7
Le chemin vers le cœur du Marais est gardé par un **Crocodile-Firewaller** (600 PV) — dernier rempart avant Hector-Pierre lui-même. Termine la séance juste avant ou juste après ce combat, selon le rythme de ta table : c'est un bon point de bascule vers la finale.

---

## 10. Séance 7 — Finale : Hector-Pierre Castor

**Niveau conseillé : 9 → 10**

### Ce qu'il faut absolument tenir
Ce n'est **pas une exécution**. C'est une tragédie qu'on peut encore, peut-être, infléchir. Hector-Pierre garde sa forme et sa parole jusqu'au bout — c'est ce qui fait de lui le seul véritable dialogue possible de toute la campagne, et la seule vraie tension morale.

### Texte d'ouverture *(à lire)*

> *Au cœur du Marais, la brume s'arrête net — comme si même elle refusait d'approcher. Un chantier s'étend devant vous : des chênes-serveurs éventrés, du bois empilé en piles régulières, des pages arrachées séchant sur des cordes tendues entre les troncs morts. Et au centre, de dos, une silhouette massive qui creuse — sans relâche, sans un bruit, vers quelque chose que vous ne pouvez pas encore voir.*
>
> *Il s'arrête. Il ne se retourne pas tout de suite.*
>
> *"Vous êtes venus me dire que j'ai tort," dit-il enfin. "Vous n'êtes pas les premiers. Vous ne serez pas les derniers. Mais dites-moi — qu'est-ce que vous, vous avez perdu, pour avoir le droit de me juger ?"*

### Avant le combat — laisse une vraie fenêtre de dialogue

Hector-Pierre **parle**, et il écoute — brièvement. Un joueur qui tente de le raisonner (en s'appuyant sur ce que le groupe a appris aux Lacs et en Montagne : que la mutation était une vaccination, pas une invasion, que ce qu'il creuse va détruire le seul remède contre le mal qui a réellement tué son peuple) peut obtenir une réaction — pas une reddition immédiate, mais une hésitation. Décide à l'avance, selon ta table, si un dialogue suffisamment réussi peut :
- **Retarder le combat** et donner un avantage tactique (il baisse sa garde un instant) ;
- **Changer la fin du combat** (il s'arrête avant la mort, vaincu mais vivant, plutôt que détruit) ;
- **Ne rien changer du tout** — auquel cas dis-le clairement à tes joueurs après coup, pour que l'effort ait quand même compté narrativement, même sans effet mécanique.

*Aucune de ces options n'est "la bonne". Choisis celle qui convient à ta table avant la séance, pas pendant.*

### Le combat — Hector-Pierre Castor (700 PV, boss final)

Traite-le comme le point culminant mécanique de toute la campagne — la seule vraie bataille rangée, longue, en plusieurs vagues si tu veux (des Sentinelles peuvent intervenir en soutien). Rappelle-toi : **c'est une course**, pas seulement un combat — il est à quelques instants de percer jusqu'aux racines du premier chêne-serveur. Une tension de temps (un décompte, une jauge de forage) peut se superposer aux PV.

*Texte de victoire (proposition, à adapter à la manière dont ta table l'a joué) :*
> *Il tombe à genoux dans la boue de son propre chantier. Pas de dernier mot de défi — juste un silence, long, pendant lequel personne ne sait s'il faut s'approcher.*
>
> *"J'ai perdu," dit-il enfin. Ce n'est pas une plainte. C'est un constat, du même genre que ceux qu'il faisait sur son peuple, il y a des cycles. "Dites-moi au moins... que ce n'était pas pour rien."*

### Après la finale
Pointu referme la boucle — sans triomphalisme. La corruption n'a pas disparu avec Hector-Pierre ; elle continue, ailleurs, plus discrètement. Mais la vérité, elle, est désormais assemblée, et racontée. C'est la victoire que le jeu promettait depuis l'épilogue de `LA_LEGENDE_DE_POINTU_V2.md` : *"Il a besoin de gens qui savent regarder, assembler les fragments, et dire la vérité à ceux qui ne veulent pas l'entendre."*

---

## Annexe A — Cast complet (aide-mémoire de table)

| Nom | Zone | Rôle | Une réplique |
|---|---|---|---|
| Pointu | Arbonet (hub) | Gardien, donneur de quête principal | *"L'oubli avance plus vite que nous."* |
| Braise le Renard-Routeur | Plaines | Convois, services, escortes | *"Je n'ai jamais livré personne aux Corbeaux."* |
| Sillon le Blaireau-Racine | Lacs | Digues, entretien lourd | *"Ça tiendra."* (rare chez lui) |
| Nyx la Chouette-Veille | Montagne | Artefacts, vérité | *"Bientôt. Pas encore."* |
| Faîne l'Écureuil-Archive | Désert | Boutique rare/légendaire | *"Rien ne se perd chez moi. Rien ne se donne non plus."* |
| Bogue le Hérisson-Pare-feu | Marais | Entretien, dernière étape | *"Ça tiendra."* |
| Hector-Pierre Castor | Marais (final) | Antagoniste, tragique, parlant | *"Qu'est-ce que vous avez perdu, pour avoir le droit de me juger ?"* |

## Annexe B — La Voie d'Hector-Pierre, adaptée à la table

Dans le jeu Twitch, refuser le fragment peut mener un viewer à devenir un boss piloté par lui-même. À table, l'équivalent le plus fidèle est un **arc secret pour un seul joueur** plutôt qu'un personnage jouable séparé :

- En Séance 0, si un joueur choisit le refus, ne l'exclus pas de la table — propose-lui plutôt de revenir avec un personnage **marqué par la rencontre avec Hector-Pierre**, sans que les autres joueurs le sachent au départ (une sympathie secrète pour sa cause, une dette, une promesse).
- Ce fil peut rester dormant toute la campagne, ou éclater en Séance 7 — un moment de tension où ce joueur doit choisir, en jeu, de quel côté il se tient réellement.
- Ce n'est qu'une proposition : ne l'utilise que si un joueur de ta table est à l'aise avec ce genre de secret prolongé.

## Annexe C — Ce que je n'ai pas tranché

Quelques décisions restent à toi :
- Le contenu exact de l'artefact intact du Désert (Séance 5) — je l'ai volontairement laissé ouvert.
- L'issue précise du dialogue avec Hector-Pierre en Séance 7 (retard, fin non-létale, ou aucun effet mécanique) — à fixer avant la séance finale.
- Le nombre de séances entre chaque étape ci-dessus si ton groupe veut ralentir ou accélérer — cette trame suppose une séance par zone, mais rien n'empêche d'en jouer deux sur le Marais, ou de fusionner Lacs et Désert si le temps manque.

---

*Document vivant — pensé pour accompagner tes séances, pas pour les remplacer.*
*Projet Pointu © Florian alias kikaby67 — 2026*
*Arbonet vous attend.*
