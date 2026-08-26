# Restructuration Pointu-PJT — V3

### Document de conception — validé, prêt pour implémentation dans Claude Code (VS Code)

> Toutes les décisions ci-dessous ont été validées avec Florian. Ce fichier est destiné à être déposé dans le repo (à côté de `CLAUDE.md`) pour qu'une session Claude Code implémente le code réel — ce document ne contient volontairement aucun fichier `.cs` ou `.json` final, seulement les spécifications et le raisonnement derrière chaque choix.

---

## 0\. Pourquoi ce document

Trois problèmes identifiés sur la V2 :

1. Trop de texte envoyé dans le chat Twitch à chaque événement (commandes multi-messages, timer de quête toutes les 30s).  
2. En chat partagé (raid / co-stream), les commandes et réponses du jeu polluent le chat des autres streamers.  
3. Le lore (Arbonet, Pointu, Hector-Pierre, les territoires) existe et est riche, mais n'est mobilisé que dans des commandes statiques (`!arbonet`, `!hexadecimeur`...) — jamais dans le combat, les quêtes ou les rencontres.

Ce document propose une architecture cible pour les trois, plus des pistes de refonte de la dynamique de jeu. Il complète `CLAUDE.md` — une fois validé, il servira de base à l'implémentation fichier par fichier.

---

## 1\. Principe directeur : deux canaux, deux rôles

**Twitch chat \= confirmation courte.** Une ligne, publique, lisible par n'importe qui — y compris un viewer d'un autre stream en chat partagé. Aucune donnée sensible au contexte du jeu (pas de stats détaillées, pas d'inventaire, pas de texte de lore long).

**Discord (`#lantre-de-pointu`, webhook) \= détail complet.** Stats, inventaire, résolution de quête, texte narratif. Un seul salon alimenté par un webhook Discord appelé directement depuis Streamer.bot. Pas de liaison de compte Twitch↔Discord nécessaire pour cette V3 — chaque message webhook est préfixé par le pseudo Twitch. Le webhook poste sous l'identité **Pointu** (voir 1.1) — narrativement, ce n'est pas "le bot" qui parle, c'est le gardien lui-même.

### 1.1 Pointu, la voix qui parle — cohérence narrative

Un webhook Discord peut surcharger son nom et son avatar à chaque envoi (`username` \+ `avatar_url`). Résultat : sans construire de bot, chaque message dans `#lantre-de-pointu` apparaît comme posté par **Pointu**, avec son propre visuel — pas une intégration anonyme.

Ça se raccorde à du lore déjà écrit et jamais exploité :

- Le Chapitre VI nomme déjà **L'Antre** comme la zone sacrée des quêtes de Pointu → le salon Discord *est* L'Antre.  
- Votre convention de sauvegarde appelle chaque JSON joueur un **Fragment de Carapace** → la carte qui s'affiche/s'édite sur Discord (piste B, section 3\) devient littéralement *"Pointu qui te montre ton Fragment de Carapace"*, pas une fiche de stats froide.  
- Les messages courts sur Twitch peuvent être signés implicitement ("Pointu note ta progression...") sans que ce soit lourd, et le renvoi vers Discord devient une invitation narrative : *"→ consulte ton Fragment dans l'Antre"* plutôt que *"→ voir \#salon-discord"*.

Visuel (avatar, emotes, pp) confié à un artiste — pas de génération automatique. Une fois livré, héberger l'image (Imgur, CDN Discord via un premier envoi manuel, ou ton propre site) pour obtenir l'URL à mettre dans `avatar_url`. En attendant, le webhook peut déjà tourner avec `username: "Pointu"` seul (Discord affiche un avatar par défaut tant que `avatar_url` n'est pas renseigné).

### 1.2 Le bot Pointu (`DiscordBot/bot_discord.py`) — rôle clarifié

Le bot existant reste utile, mais pas pour cette étape : comme il n'est qu'un squelette et que les données joueurs vivent en local sur ton PC (contrainte `CLAUDE.md` : « un fichier JSON par joueur, pas de base de données »), le faire répondre *en direct* à des commandes de jeu demanderait un pont réseau entre Discloud (où il tourne) et Streamer.bot (chez toi) — tunnel type Cloudflare Tunnel/ngrok, qui ne fonctionne que quand ton PC et Streamer.bot sont allumés.

Décision : le bot est réservé, pour l'instant, aux **commandes statiques** qui n'ont pas besoin de lire l'état de jeu — `/lore`, `/classes`, `/bestiaire`, `/aide` — aucun pont requis, réutilise le code déjà écrit. Les commandes "vivantes" (`/profil` en direct depuis Discord) restent une étape 2, à construire une fois qu'on aura décidé ensemble comment bâtir ce pont.

Ce choix règle les problèmes 1 et 2 **en même temps**, sans dépendre d'une détection technique du chat partagé (qui reste incertaine côté Streamer.bot) : le chat Twitch est discret par construction, tout le temps, peu importe le contexte.

### Exemple : `!profil`

**Avant (4 messages Twitch) :**

kikabygaming | Niv 3 | XP 950 | Ram 42

Classe: Hexadécimeur | Sous-classe: — | Arme: Épée

PV 22/25 | CA 15 | Atq 4 | Mana 0

Combats: 3V/1D | Quêtes: 2 | Arme équipée: Épée-Rouille

**Après (Twitch, 1 message) :**

📋 kikaby consulte son profil (détail → \#lantre-de-pointu)

**Après (Discord, 1 embed) :**

🐢 kikabygaming — Niveau 3 (950 XP · 42 Ram)

Hexadécimeur · sous-classe non choisie · Épée-Rouille

PV 22/25 · CA 15 · Atq 4

Combats 3V / 1D · Quêtes terminées : 2

Le même principe s'applique à `!inventaire`, `!equipement`, `!classement`, à la résolution de quête (`quest_timer.cs`) et aux montées de niveau : un accusé de réception court sur Twitch, le détail sur Discord.

### `!choisirclasse` / `!sousclasse` — à ajouter au périmètre

Tu as raison, ces deux commandes manquaient à la liste. Ma lecture de ta remarque : aujourd'hui `!sousclasse` confirme juste le choix, sans que le joueur voie clairement **ce qui est devenu actif** (quel bonus, quel comportement combat) — à confirmer si c'est bien ça que tu vises.

**Avant (Twitch) :** `✅ kikaby a choisi la sous-classe Bloc-Hex !`

**Après (Twitch) :** `✅ kikaby active Bloc-Hex ! (détail → #lantre-de-pointu)`

**Après (Discord, embed) :**

🔧 kikabygaming débloque sa sous-classe : Bloc-Hex

Effet actif : \+8 PV max (appliqué immédiatement : 25 → 33 PV max)

Même traitement pour `!choisirclasse` à la création : confirmation courte sur Twitch, le détail des stats tirées aux dés (PV/CA/Attaque) affiché sur Discord.

### Cas à garder visibles sur Twitch (volontairement)

Certains événements ont une valeur de spectacle et doivent rester publics et courts :

- Level up : `🎉 kikaby atteint le niveau 4 !` (sans le détail du bonus, qui va sur Discord)  
- Victoire de boss d'arène communautaire : `🏆 Le Munin-Daemon est vaincu !`  
- `!classement` (déjà broadcaster-only) : garder un top 3 très court sur Twitch, le top 5 détaillé sur Discord

---

## 2\. Implémentation technique du webhook Discord

Contrainte du projet : pas de `Newtonsoft.Json`, pas de `System.Text.Json`, chaque fichier autonome. Le JSON du payload webhook est donc construit **en concaténation de chaîne**, comme le reste du projet — cohérent avec `LireValeur`/`ModifierValeur`.

Méthode à copier dans chaque fichier qui doit écrire sur Discord — elle réutilise `LireValeur` (déjà copiée dans chaque fichier par convention) pour lire l'URL depuis `secrets/secrets.json` plutôt que de l'avoir en dur (voir 2.1) :

private void EnvoyerDiscord(string message)

{

    string webhookUrl;

    try

    {

        string secrets \= File.ReadAllText(@"CHEMIN\_VERS\\secrets\\secrets.json"); // chemin réel à renseigner

        webhookUrl \= LireValeur(secrets, "discord\_webhook\_url");

        if (webhookUrl \== "0" || webhookUrl \== "") return; // pas configuré → on n'envoie rien, silencieux

        string contenuEchappe \= message.Replace("\\\\", "\\\\\\\\").Replace("\\"", "\\\\\\"").Replace("\\n", "\\\\n");

        string payload \= "{\\"content\\": \\"" \+ contenuEchappe \+ "\\", "

                        \+ "\\"username\\": \\"Pointu\\", "

                        \+ "\\"avatar\_url\\": \\"URL\_AVATAR\_POINTU\_A\_HEBERGER\\"}";

        using (var client \= new System.Net.Http.HttpClient())

        {

            client.Timeout \= TimeSpan.FromSeconds(3);

            var contenu \= new System.Net.Http.StringContent(payload, System.Text.Encoding.UTF8, "application/json");

            var reponse \= client.PostAsync(webhookUrl, contenu).GetAwaiter().GetResult();

        }

    }

    catch (Exception ex)

    {

        CPH.LogWarn("Erreur webhook Discord : " \+ ex.Message);

    }

}

Points d'attention (à tester avant généralisation) :

- Usings à ajouter en tête du fichier, en plus de `System` et `System.IO` déjà systématiques : `using System.Net.Http;` (ce namespace fait partie du framework .NET standard, mais **à confirmer qu'il est bien accessible depuis l'éditeur inline de Streamer.bot** — c'est la première chose à tester).  
- L'appel est **synchrone** (`GetAwaiter().GetResult()`) car `Execute()` doit retourner un `bool` classique. Le `Timeout = 3s` \+ `try/catch` garantissent qu'une panne Discord ne bloque jamais une commande de jeu.  
- ✅ **Testé le 09/08 avec l'URL réelle du webhook** : deux appels simulant `!profil` et `!classement` envoyés, réponse `HTTP 204` (succès) dans les deux cas. Le webhook fonctionne, le format `content` \+ `embeds` s'affiche correctement.

### 2.1 Sécurité de l'URL du webhook — pourquoi pas de hachage

Ta question : peut-on hacher la donnée sensible dans le fichier de config, même s'il est déjà gitignoré ?

Techniquement non — et ce n'est pas l'outil qu'il te faut ici. Le **hachage est à sens unique** : il sert à *vérifier* une valeur sans jamais la reconstituer (c'est exactement ce que fait déjà `exemple_mot_de_passe_hash` dans ton `secrets.example.json`, comparé à un hash au moment de la connexion via `livraison.py hash-password`). Une URL de webhook, elle, doit être **relisible en clair** au moment de l'appel HTTP — si tu la hashes, ton code ne peut plus jamais la reconstituer pour poster sur Discord. Ce serait comme hacher ta propre clé de voiture : tu ne peux plus t'en servir.

Ce qui protège réellement un secret, c'est l'**isolement**, pas le chiffrement à ce niveau — et ton projet a déjà la bonne convention : `secrets.example.json` (versionné, sert de modèle) → copié en local vers `secrets/secrets.json` (gitignoré, jamais poussé). Je m'appuie dessus :

- `discord_webhook_url` va dans `secrets/secrets.json`, jamais dans `config_global.json` (qui lui est versionné et public sur ton repo).  
- Le hachage reste réservé à ce qu'il fait déjà bien chez toi : vérifier un mot de passe, pas stocker un token/URL qu'on doit réutiliser tel quel.

---

## 3\. Nouvelle dynamique de jeu — pistes à valider une par une

Tu as ouvert la porte à repenser plus que la couche messages. Je liste des pistes concrètes, chacune indépendante — on choisit ensemble lesquelles retenir, je n'en implémente aucune sans ton accord :

**A. ⏸️ En attente.** Commandes → Discord plutôt que Twitch pour certaines interactions : idée intéressante mais mise de côté pour cette V3, on y revient une fois le socle (webhook \+ zones) en place.

**B. ✅ Adopté — et meilleur que prévu.** Bonne nouvelle technique : un webhook Discord peut **éditer ses propres messages** (`PATCH /webhooks/{id}/{token}/messages/{message_id}`), pas seulement en poster de nouveaux. Pas besoin de faire tourner le bot Python de `DiscordBot/` pour ça — le webhook seul suffit :

- Ajouter un champ `discordMessageId` au JSON joueur.  
- `!profil` : si `discordMessageId` existe déjà, on fait un **PATCH** (on met à jour la même carte) ; sinon un **POST** (on crée la carte et on stocke l'ID retourné dans le JSON joueur).  
- Résultat : un seul message par joueur dans `#lantre-de-pointu`, qui se met à jour à chaque consultation, au lieu d'un nouveau message à chaque fois.

**C. ✅ Adopté.** `quete_rencontre_intervalle_secondes` passe de `180` à `360` dans `config_global.json` — un simple changement de config, aucun code à toucher, aucun rééquilibrage des taux de rencontre.

**D. ✅ Adopté.** Fil rouge Hector-Pierre — détaillé en section 4.2.

---

## 4\. Intégration du lore

Trois axes retenus : progression par zones, arc Hector-Pierre plus présent, textes narratifs en combat/quête.

### 4.1 Progression par zones

Les 5 territoires existent déjà dans `LA_LEGENDE_DE_POINTU_V2.md` (Chapitre VI) : Forêt-Mémoire, Plaines de Pixel, Marais du Buffer, l'Antre, Vide Binaire. Aucun n'est actuellement relié au système de quêtes/ennemis.

Proposition, dans le pattern config-JSON existant :

- Ajouter un champ `_zone` à chaque entrée de `config_quetes.json` et `config_ennemis.json` (`Forêt-Mémoire`, `Plaines de Pixel`, etc. — cohérent avec le tableau du Chapitre VI).  
- Ajouter au profil joueur `zoneActuelle` (déverrouillée par niveau, ex. Plaines à niveau 3, Marais à niveau 6, Vide Binaire à niveau 9 — à caler sur ton tableau de niveaux existant).  
- `!quete` ne propose que les quêtes de la zone débloquée la plus avancée (ou une zone choisie si plusieurs sont débloquées).

Zéro changement de moteur de combat/quête — uniquement un filtre supplémentaire sur le tirage aléatoire, donc risque d'implémentation faible.

### 4.2 Arc Hector-Pierre

Le twist (Épilogue) et le personnage (Chapitre IV) sont écrits mais jamais montrés en jeu. Proposition : des **jalons scénarisés**, déclenchés par des seuils déjà trackés (niveau 5, nombre de quêtes terminées, fragments de secret `!racine`), qui postent un extrait du lore sur Discord (embed distinct, ton "je découvre un fragment de vérité") plutôt qu'un simple gain de stats.

Exemple de jalon : à la 10e quête réussie d'un joueur, poster sur Discord un extrait du Chapitre IV, et incrémenter un compteur **communautaire** de fragments découverts (cohérent avec le système déjà collectif du boss d'arène) affiché par `!classement` ou une nouvelle commande `!fragments`.

### 4.3 Textes narratifs en combat/quête — exemple concret

Remplacer les messages froids (« \+20 XP ») par une ligne courte piochée dans un pool de texte par ennemi, cohérente avec sa fiche du Chapitre V (le Corbeau-Daemon "vole", le Castor-Rootkit "ronge", etc.).

Proposition technique : nouveau fichier `config_lore_textes.json`, format `"<Ennemi>_victoire_01"`, `"<Ennemi>_victoire_02"`... — même pattern que `config_quetes.json` (énumération jusqu'à chaîne vide), pour rester à « zéro texte en dur dans le code ».

**Exemple projeté — victoire sur un Corbeau-Daemon (`!combat`) :**

"Corbeau-Daemon\_victoire\_01": "L'oiseau s'effondre dans une pluie de plumes noires — un fragment de mémoire volé te revient.",

"Corbeau-Daemon\_victoire\_02": "Tu arraches le dernier souvenir des serres du Corbeau avant qu'il ne s'échappe pour de bon.",

"Corbeau-Daemon\_defaite\_01": "Le Corbeau-Daemon t'échappe dans un tourbillon de plumes — une part de toi part avec lui.",

"Castor-Rootkit\_victoire\_01": "Le Castor-Rootkit lâche prise, dents cassées contre l'écorce du chêne-serveur qu'il rongeait.",

"Castor-Rootkit\_victoire\_02": "Il fuit en boitant, laissant derrière lui des copeaux de code corrompu."

**Avant / après pour le message Twitch de résolution :**

Avant : ✅ kikaby gagne le combat \! \+20 XP \+5 Ram

Après : ⚔️ L'oiseau s'effondre dans une pluie de plumes noires. (+20 XP · \+5 Ram)

Le code tire un numéro aléatoire (1 à N, N \= nombre de variantes trouvées par `LireValeur` jusqu'à chaîne vide, même logique que l'énumération `quete001`→`quete099`) pour varier les lignes d'un combat à l'autre contre le même ennemi.

---

## 5\. Décisions verrouillées

- **Webhook Discord** : URL fournie et **testée avec succès** (`HTTP 204` sur simulation `!profil` et `!classement`, deux messages visibles dans ton salon). À déplacer dans `secrets/secrets.json` avant tout code réel (voir 2.1) — je ne l'ai pas mise en dur dans ce document par précaution.  
- **Ordre d'implémentation** : on suit l'ordre logique du document. Prochaine étape concrète : étoffer le lore (zones \+ arc Hector-Pierre \+ textes narratifs), puis migrer les commandes vers le format discret/Discord en s'appuyant dessus.  
- **Piste B affinée** : carnet Discord qui s'édite (PATCH webhook) plutôt que de spammer un nouveau message à chaque `!profil`.  
- **Timer de rencontre** : `quete_rencontre_intervalle_secondes` 180 → 360\.  
- **Nouveau dans le périmètre** : `!choisirclasse` / `!sousclasse` migrées vers le même format (confirmation courte \+ détail Discord), avec affichage explicite de l'effet actif de la sous-classe.

### Prochaine étape — pour la session Claude Code

1. Écrire `secrets/secrets.json` (copié depuis `secrets.example.json`) avec `discord_webhook_url`, et brancher `EnvoyerDiscord` dessus (section 2).  
2. Étoffer le lore : créer/étendre `config_zones` (ou champ `_zone` dans `config_quetes.json`/`config_ennemis.json`) et `config_lore_textes.json` (sections 4.1 à 4.3) — c'est la fondation dont dépend la suite.  
3. Migrer les commandes listées en section 1 vers le format court Twitch \+ détail Discord (`!profil`, `!inventaire`, `!equipement`, `!classement`, `!choisirclasse`, `!sousclasse`, résolution de quête), en s'appuyant sur les textes narratifs de l'étape 2\.  
4. Implémenter le PATCH webhook (carte qui s'édite, piste B) une fois le POST simple validé en conditions réelles de stream.  
5. `quete_rencontre_intervalle_secondes` : 180 → 360 dans `config_global.json`.

---

*Document de travail — Projet Pointu-PJT © Florian alias kikaby67 — 2026*  
