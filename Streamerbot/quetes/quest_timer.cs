// sb-action: QuestCheck
// sb-subaction-id: e8d9be72-b002-4cac-879f-d34147361320
using System;
using System.IO;

public class CPHInline
{
    private const string DOSSIER_JOUEURS = @"C:\Users\Florian\pjt\Pointu-PJT\Donnees\joueurs";
    private const string CONFIG_ENNEMIS  = @"C:\Users\Florian\pjt\Pointu-PJT\Donnees\config_ennemis.json";
    private const string CONFIG_ITEMS    = @"C:\Users\Florian\pjt\Pointu-PJT\Donnees\config_items.json";
    private const string CONFIG_QUETES   = @"C:\Users\Florian\pjt\Pointu-PJT\Donnees\config_quetes.json";
    private const string CONFIG_LEVEL    = @"C:\Users\Florian\pjt\Pointu-PJT\Donnees\config_level.json";
    private const string CONFIG_ALLIES   = @"C:\Users\Florian\pjt\Pointu-PJT\Donnees\config_allies.json";
    private const string CONFIG_GLOBAL   = @"C:\Users\Florian\pjt\Pointu-PJT\Donnees\config_global.json";
    private const string CONFIG_LORE     = @"C:\Users\Florian\pjt\Pointu-PJT\Donnees\config_lore_textes.json";

    public bool Execute()
    {
        string[] fichiers = Directory.GetFiles(DOSSIER_JOUEURS, "*.json");
        long maintenant   = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        Random rng        = new Random();
        string cfgAllies  = File.ReadAllText(CONFIG_ALLIES);
        string cfgG       = File.ReadAllText(CONFIG_GLOBAL);
        int maxSac               = int.Parse(LireValeur(cfgG, "max_sac"));
        int tauxEnnemi           = int.Parse(LireValeur(cfgG, "rencontre_taux_ennemi"));
        int tauxAllie            = int.Parse(LireValeur(cfgG, "rencontre_taux_allie"));
        int tauxEchec            = int.Parse(LireValeur(cfgG, "quete_taux_echec"));
        int chanceLootArtefact   = int.Parse(LireValeur(cfgG, "quete_chance_loot_artefact"));
        int chanceEcorce         = int.Parse(LireValeur(cfgG, "quete_chance_ecorce"));
        int cooldownDefaite      = int.Parse(LireValeur(cfgG, "quete_cooldown_defaite_secondes"));
        int expireSecs           = int.Parse(LireValeur(cfgG, "rencontre_expire_secondes"));
        int intervalleRenc       = int.Parse(LireValeur(cfgG, "quete_rencontre_intervalle_secondes"));
        int miniBossNivMin       = int.Parse(LireValeur(cfgG, "mini_boss_niveau_min"));
        int miniBossChance       = int.Parse(LireValeur(cfgG, "mini_boss_chance"));
        int rondeAgeMin          = int.Parse(LireValeur(cfgG, "quete_ronde_age_min_secondes"));
        int seuilGroupement      = int.Parse(LireValeur(cfgG, "quete_seuil_groupement"));
        if (rondeAgeMin     == 0) rondeAgeMin     = 60;
        if (seuilGroupement == 0) seuilGroupement = 3;

        // Accumulateurs de la ronde : on n'envoie RIEN pendant la boucle, on décide à la fin.
        // Sous le seuil -> un message clair par événement. Au-dessus -> on groupe par famille
        // de commande de réponse (jamais mélanger : !combat/!discuter/!fuir vs !accepter/!refuser).
        string indivCombat = "", indivOffre = "", indivClasse = "";
        string listeCombat = "", listeOffre = "", listeClasse = "";
        int nbCombat = 0, nbOffre = 0, nbClasse = 0;

        bool auMoinsUneActive = false;
        foreach (string chemin in fichiers)
        {
          // Un profil corrompu ne doit pas geler le traitement des autres joueurs.
          try
          {
            string json = File.ReadAllText(chemin);
            if (LireValeur(json, "enQuete") != "true") continue;

            string nomJoueur = LireValeur(json, "nomJoueur");
            json = EnsureChamp(json, "rencontreExpire", "0", false);
            json = EnsureChamp(json, "compagnonActif",  "",  true);
            json = EnsureChamp(json, "offreValeur",     "0", false);

            // === CAS 1 : rencontre en cours — attendre ou expirer (ennemi ET allié) ===
            if (LireValeur(json, "enRencontre") == "true")
            {
                long expire = long.Parse(LireValeur(json, "rencontreExpire"));
                if (expire > 0 && maintenant >= expire)
                {
                    long pauseDebut = long.Parse(LireValeur(json, "quetePauseDebut"));
                    long totalPause = long.Parse(LireValeur(json, "queteTotalPause"));
                    if (pauseDebut > 0) totalPause += maintenant - pauseDebut;

                    json = ModifierValeur(json, "enCombat",        "false", false);
                    json = ModifierValeur(json, "enRencontre",     "false", false);
                    json = ModifierValeur(json, "rencontreType",   "",      true);
                    json = ModifierValeur(json, "rencontreExpire", "0",     false);
                    json = ModifierValeur(json, "quetePauseDebut", "0",     false);
                    json = ModifierValeur(json, "queteTotalPause", totalPause.ToString(), false);
                    File.WriteAllText(chemin, json);
                    // Expiration SILENCIEUSE : annoncer « untel n'a pas répondu » n'apporte rien
                    // à personne et c'était l'un des plus gros postes de bruit du chat.
                }
                auMoinsUneActive = true; continue; // attendre le choix du joueur
            }

            // === CAS 2 : check rencontre toutes les N secondes ===
            bool encounterLancee = false;
            long dernierCheck    = long.Parse(LireValeur(json, "dernierCheckRencontre"));
            long debutQuete      = long.Parse(LireValeur(json, "queteDernierTick"));

            // Rondes SYNCHRONISÉES. Avant : un compteur par joueur, donc des rencontres
            // dispersées et un message chacune. Maintenant tout le monde bascule sur la même
            // frontière de ronde — la division entière suffit, aucun état global à stocker.
            // L'âge minimum évite d'attraper un joueur qui vient de taper !quete.
            bool nouvelleRonde     = (maintenant / intervalleRenc) > (dernierCheck / intervalleRenc);
            bool queteAssezVieille = (maintenant - debutQuete) >= rondeAgeMin;

            if (nouvelleRonde && queteAssezVieille)
            {
                json = ModifierValeur(json, "dernierCheckRencontre", maintenant.ToString(), false);
                int roll = rng.Next(100);

                if (roll < tauxEnnemi)
                {
                    // — Rencontre ennemie (40%) —
                    int niveauJoueur = int.Parse(LireValeur(json, "niveau"));
                    bool estMiniBoss = niveauJoueur >= miniBossNivMin && rng.Next(100) < miniBossChance;
                    string poolKey   = estMiniBoss ? "rencontre_mini_boss" : "rencontre_ennemis";
                    string[] ennemis = LireValeurString(cfgG, poolKey).Split(',');
                    string ennemiChoisi = ennemis[rng.Next(ennemis.Length)].Trim();

                    json = ModifierValeur(json, "enRencontre",     "true",                              false);
                    json = ModifierValeur(json, "rencontreType",   "combat",                            true);
                    json = ModifierValeur(json, "enCombat",        "true",                              false);
                    json = ModifierValeur(json, "ennemiNom",       ennemiChoisi,                        true);
                    json = ModifierValeur(json, "quetePauseDebut", maintenant.ToString(),               false);
                    json = ModifierValeur(json, "rencontreExpire", (maintenant + expireSecs).ToString(), false);
                    File.WriteAllText(chemin, json);

                    // Pas d'article devant le nom : « un Sentinelle du Castor » était fautif.
                    string ligne = estMiniBoss
                        ? "⚠️ " + nomJoueur + " — MINI-BOSS ! " + ennemiChoisi + " te barre la route ! !combat · !discuter · !fuir (" + (expireSecs / 60) + " min)"
                        : "⚔️ " + nomJoueur + " — " + ennemiChoisi + " te barre la route ! !combat · !discuter · !fuir (" + (expireSecs / 60) + " min)";
                    indivCombat += (indivCombat == "" ? "" : "\n") + ligne;
                    listeCombat += (listeCombat == "" ? "" : " · ") + nomJoueur + " vs " + ennemiChoisi;
                    nbCombat++;
                    encounterLancee = true;
                }
                else if (roll < tauxEnnemi + tauxAllie)
                {
                    // — Rencontre alliée (30%) —
                    int niveauJoueur = int.Parse(LireValeur(json, "niveau"));
                    // Sélection pondérée : vieux_sage rendu plus rare (config), marchand_classe niv 5+
                    int vieuxSageFreq = int.Parse(LireValeur(cfgAllies, "vieux_sage_frequence_pct"));
                    if (vieuxSageFreq == 0) vieuxSageFreq = 12;
                    string[] poolBase = { "marchand_potion", "bonus_ram", "alcove_chene" };
                    string type;
                    if (niveauJoueur >= 5 && rng.Next(100) < 10)
                        type = "marchand_classe";
                    else if (rng.Next(100) < vieuxSageFreq)
                        type = "vieux_sage";
                    else
                        type = poolBase[rng.Next(poolBase.Length)];

                    int    offreVal = 0;
                    string msg      = "";   // message individuel (chat calme)
                    string court    = "";   // étiquette compacte (chat chargé, message groupé)
                    string delai    = " (" + (expireSecs / 60) + " min)";

                    if (type == "marchand_potion")
                    {
                        int prix = int.Parse(LireValeur(cfgAllies, "marchand_prix_potion"));
                        offreVal = prix;
                        msg   = "🧪 " + nomJoueur + " — un marchand ambulant propose une Potion pour " + prix + " RAM. !accepter · !refuser" + delai;
                        court = nomJoueur + " → 🧪Potion " + prix + " RAM";
                    }
                    else if (type == "vieux_sage")
                    {
                        int xpMin = int.Parse(LireValeur(cfgAllies, "vieux_sage_xp_min"));
                        int xpMax = int.Parse(LireValeur(cfgAllies, "vieux_sage_xp_max"));
                        offreVal  = rng.Next(xpMin, xpMax + 1);
                        msg   = "🧙 " + nomJoueur + " — le Vieux Sage surgit de la brume et propose un marché : " + offreVal + " XP. !accepter · !refuser" + delai;
                        court = nomJoueur + " → 🧙Sage " + offreVal + " XP";
                    }
                    else if (type == "bonus_ram")
                    {
                        int ramMin = int.Parse(LireValeur(cfgAllies, "source_ram_min"));
                        int ramMax = int.Parse(LireValeur(cfgAllies, "source_ram_max"));
                        offreVal   = rng.Next(ramMin, ramMax + 1);
                        msg   = "💰 " + nomJoueur + " — une bourse de " + offreVal + " RAM brille sur le sol. !accepter · !refuser" + delai;
                        court = nomJoueur + " → 💰" + offreVal + " RAM";
                    }
                    else if (type == "alcove_chene")
                    {
                        msg   = "🌿 " + nomJoueur + " — une alcôve de chêne-serveur t'offre le repos. !accepter · !refuser" + delai;
                        court = nomJoueur + " → 🌿alcôve";
                    }
                    else // marchand_classe — famille à part : il se répond par !choisirclasse
                    {
                        msg   = "🎭 " + nomJoueur + " — un Marchand de Classe t'aborde. !choisirclasse [nom] pour changer · !refuser" + delai;
                        court = nomJoueur;
                    }

                    if (type == "marchand_classe")
                    {
                        indivClasse += (indivClasse == "" ? "" : "\n") + msg;
                        listeClasse += (listeClasse == "" ? "" : " · ") + court;
                        nbClasse++;
                    }
                    else
                    {
                        indivOffre += (indivOffre == "" ? "" : "\n") + msg;
                        listeOffre += (listeOffre == "" ? "" : " · ") + court;
                        nbOffre++;
                    }

                    json = ModifierValeur(json, "enRencontre",     "true",                              false);
                    json = ModifierValeur(json, "rencontreType",   type,                                true);
                    json = ModifierValeur(json, "enCombat",        "false",                             false);
                    json = ModifierValeur(json, "offreValeur",     offreVal.ToString(),                 false);
                    json = ModifierValeur(json, "quetePauseDebut", maintenant.ToString(),               false);
                    json = ModifierValeur(json, "rencontreExpire", (maintenant + expireSecs).ToString(), false);
                    File.WriteAllText(chemin, json);
                    encounterLancee = true;
                }
                else
                {
                    // Pas de rencontre (30%) — sauvegarder le nouveau dernierCheck
                    File.WriteAllText(chemin, json);
                }
            }

            if (encounterLancee) { auMoinsUneActive = true; continue; }

            // === CAS 3 : vérifier si la quête est terminée (en soustrayant les pauses) ===
            string queteId = LireValeur(json, "queteId");
            int ticksRequis = int.Parse(LireValeur(json, "queteTicksRestants"));
            long debutTimestamp = long.Parse(LireValeur(json, "queteDernierTick"));
            long totalPauseFin = long.Parse(LireValeur(json, "queteTotalPause"));
            long secondesEcoulees = (maintenant - debutTimestamp) - totalPauseFin;
            long secondesRequises = ticksRequis * 5 * 60L;

            if (secondesEcoulees < secondesRequises) { auMoinsUneActive = true; continue; }

            // Résoudre la quête
            string[] data = GetQueteData(queteId);
            bool succes = rng.Next(100) >= tauxEchec;

            json = ModifierValeur(json, "enQuete", "false", false);
            json = ModifierValeur(json, "queteTicksRestants", "0", false);
            json = ModifierValeur(json, "compagnonActif", "", true);  // le compagnon ne suit que sur une quête

            if (succes)
            {
                int xp = int.Parse(data[2]);
                int ram = int.Parse(data[3]);
                json = AjouterValeur(json, "experience", xp);
                json = AjouterValeur(json, "ram", ram);
                json = AjouterValeur(json, "quetesTerminees", 1);
                json = VerifierMonteeNiveau(json, nomJoueur);

                string cfgQr = File.ReadAllText(CONFIG_QUETES);
                string keyR  = QueteKeyDeId(cfgQr, queteId);
                json = EnsureChamp(json, "quetesFaites", "", true);

                // Artefacts et secondaires ne se refont pas ; les services, si.
                if (data[5] != "service") json = MarquerFaite(json, queteId);

                // Une secondaire paie en equipement : 1 item du pool de sa zone.
                string lootMsg = RecompenseItem(ref json, cfgQr, keyR, rng, maxSac);
                if (data[5] == "artefact" && rng.Next(100) < chanceLootArtefact)
                {
                    string inventaire = LireValeurString(json, "inventaire");
                    int nbItems = inventaire == "" ? 0 : inventaire.Split(',').Length;
                    if (nbItems < maxSac)
                    {
                        // Tirage de rareté : légendaire / épique / rare / commun (chances dans config_global)
                        int rar  = rng.Next(100);
                        int cLeg = int.Parse(LireValeur(cfgG, "loot_chance_legendaire"));
                        int cEpi = int.Parse(LireValeur(cfgG, "loot_chance_epique"));
                        int cRar = int.Parse(LireValeur(cfgG, "loot_chance_rare"));
                        string pool;
                        if      (rar < cLeg)               pool = "loot_legendaire";
                        else if (rar < cLeg + cEpi)        pool = "loot_epique";
                        else if (rar < cLeg + cEpi + cRar) pool = "loot_rare";
                        else                               pool = "loot_commun";

                        string   cfgLoot  = File.ReadAllText(CONFIG_QUETES);
                        string   lootRaw  = LireValeurString(cfgLoot, pool);
                        if (lootRaw == "") lootRaw = LireValeurString(cfgLoot, "loot_commun");
                        string[] lootPool = lootRaw != "" ? lootRaw.Split(',') : new string[] { "Potion" };
                        string   loot     = lootPool[rng.Next(lootPool.Length)].Trim();
                        string nouvInventaire = inventaire == "" ? loot : inventaire + "," + loot;
                        json = ModifierValeurString(json, "inventaire", nouvInventaire);
                        lootMsg = " Tu as trouvé : " + loot + " !";
                    }
                }

                // Loot secret : morceau d'écorce gravé (20% — seulement les lettres manquantes)
                if (rng.Next(100) < chanceEcorce)
                {
                    string invEcorce = LireValeurString(json, "inventaire");
                    int nbEcorce = invEcorce == "" ? 0 : invEcorce.Split(',').Length;
                    if (nbEcorce < maxSac)
                    {
                        string[] pieces = { "Ecorce-R", "Ecorce-A", "Ecorce-C", "Ecorce-I", "Ecorce-N", "Ecorce-E" };
                        string invAvecVirgules = "," + invEcorce + ",";
                        int nbManquantes = 0;
                        for (int k = 0; k < pieces.Length; k++)
                            if (!invAvecVirgules.Contains("," + pieces[k] + ",")) nbManquantes++;

                        if (nbManquantes > 0)
                        {
                            int pickPiece = rng.Next(nbManquantes);
                            int cntPiece  = 0;
                            string ecorceLoot = "";
                            for (int k = 0; k < pieces.Length; k++)
                            {
                                if (!invAvecVirgules.Contains("," + pieces[k] + ","))
                                {
                                    if (cntPiece == pickPiece) { ecorceLoot = pieces[k]; break; }
                                    cntPiece++;
                                }
                            }
                            string nouvInvEcorce = invEcorce == "" ? ecorceLoot : invEcorce + "," + ecorceLoot;
                            json = ModifierValeurString(json, "inventaire", nouvInvEcorce);
                            lootMsg += " Un morceau d'écorce gravé tombe de ta besace... (" + ecorceLoot + ")";
                        }
                    }
                }

                File.WriteAllText(chemin, json);
                // Fin de quête : TOUJOURS son propre message, jamais groupée. C'est le moment
                // de récompense — il doit rester lisible. Le demandeur du lore prend la parole.
                CPH.SendMessage(TexteResolution(cfgQr, keyR, "succes", data[6], rng) + " " + nomJoueur
                              + " +" + xp + " XP · +" + ram + " RAM." + lootMsg);
            }
            else
            {
                File.WriteAllText(chemin, json);
                string cfgQe = File.ReadAllText(CONFIG_QUETES);
                CPH.SendMessage(TexteResolution(cfgQe, QueteKeyDeId(cfgQe, queteId), "echec", data[6], rng)
                              + " " + nomJoueur + " repart bredouille.");
            }
          }
          catch (Exception ex)
          {
              CPH.LogWarn("QuestCheck : profil ignoré (" + chemin + ") — " + ex.Message);
          }
        }

        // === Émission des annonces de rencontre, une fois tous les joueurs traités ===
        // Sous le seuil : un message clair par événement — la lisibilité passe avant le compteur.
        // Au-dessus : on groupe par famille, parce qu'un mur de 3+ lignes est pire que la compression.
        int totalEvts = nbCombat + nbOffre + nbClasse;
        string delaiTxt = " (" + (expireSecs / 60) + " min)";
        if (totalEvts > 0 && totalEvts < seuilGroupement)
        {
            EnvoyerLignes(indivCombat);
            EnvoyerLignes(indivOffre);
            EnvoyerLignes(indivClasse);
        }
        else if (totalEvts > 0)
        {
            EnvoyerGroupe("⚔️ Arbonet attaque — ",      listeCombat, " | !combat !discuter !fuir" + delaiTxt);
            EnvoyerGroupe("🤝 Arbonet tend la main — ", listeOffre,  " | !accepter !refuser"      + delaiTxt);
            EnvoyerGroupe("🎭 Marchand de Classe — ",   listeClasse, " | !choisirclasse [nom] · !refuser" + delaiTxt);
        }

        if (!auMoinsUneActive) CPH.DisableTimer("QuestCheck");

        return true;
    }

    private void EnvoyerLignes(string bloc)
    {
        if (bloc == "") return;
        string[] lignes = bloc.Split('\n');
        for (int i = 0; i < lignes.Length; i++)
            if (lignes[i] != "") CPH.SendMessage(lignes[i]);
    }

    // Découpe un message groupé si la liste dépasse la limite Twitch (500 caractères).
    private void EnvoyerGroupe(string prefixe, string liste, string suffixe)
    {
        if (liste == "") return;
        string[] items = liste.Split(new string[] { " · " }, StringSplitOptions.None);
        string bloc = "";
        for (int i = 0; i < items.Length; i++)
        {
            string essai = bloc == "" ? items[i] : bloc + " · " + items[i];
            if (bloc != "" && (prefixe + essai + suffixe).Length > 480)
            {
                CPH.SendMessage(prefixe + bloc + suffixe);
                bloc = items[i];
            }
            else bloc = essai;
        }
        if (bloc != "") CPH.SendMessage(prefixe + bloc + suffixe);
    }

    // Tire une variante narrative dans config_lore_textes.json.
    // Énumère <prefixe>_<genre>_01, _02... jusqu'à clé absente, puis replie sur defaut_<genre>_XX.
    // LireValeurString est OBLIGATOIRE ici : ces textes contiennent des virgules, LireValeur
    // s'arrêterait à la première et couperait la phrase en plein milieu.
    private string TexteLore(string prefixe, string genre, Random rng)
    {
        try
        {
            if (!File.Exists(CONFIG_LORE)) return "";
            string cfg    = File.ReadAllText(CONFIG_LORE);
            string trouve = TirerVariante(cfg, prefixe, genre, rng);
            if (trouve == "") trouve = TirerVariante(cfg, "defaut", genre, rng);
            return trouve;
        }
        catch (Exception ex)
        {
            CPH.LogWarn("Lore : " + ex.Message);
            return "";
        }
    }

    private string TirerVariante(string cfg, string prefixe, string genre, Random rng)
    {
        if (prefixe == "") return "";
        int nb = 0;
        for (int i = 1; i <= 99; i++)
        {
            if (LireValeurString(cfg, prefixe + "_" + genre + "_" + Deux(i)) == "") break;
            nb++;
        }
        if (nb == 0) return "";
        return LireValeurString(cfg, prefixe + "_" + genre + "_" + Deux(rng.Next(nb) + 1));
    }

    private string Deux(int i) { return i < 10 ? "0" + i : "" + i; }

    private string VerifierMonteeNiveau(string json, string nomJoueur)
    {
        int niveauActuel  = int.Parse(LireValeur(json, "niveau"));
        int nouvelXP      = int.Parse(LireValeur(json, "experience"));
        int nouveauNiveau = CalculerNiveau(nouvelXP);
        if (nouveauNiveau > niveauActuel)
        {
            json = ModifierValeur(json, "niveau", nouveauNiveau.ToString(), false);
            json = AppliquerBonusNiveau(json, nouveauNiveau);
            CPH.SendMessage(MessageNiveau(nomJoueur, nouveauNiveau));
        }
        return json;
    }

    private int CalculerNiveau(int xp)
    {
        string cfg    = File.ReadAllText(CONFIG_LEVEL);
        int niveauMax = int.Parse(LireValeur(cfg, "niveauMax"));
        for (int i = niveauMax; i >= 2; i--)
            if (xp >= int.Parse(LireValeur(cfg, "niveau_" + i + "_xp"))) return i;
        return 1;
    }

    private string AppliquerBonusNiveau(string json, int niveau)
    {
        string cfg        = File.ReadAllText(CONFIG_LEVEL);
        int pvBonus       = int.Parse(LireValeur(cfg, "niveau_" + niveau + "_pvBonus"));
        int caBonus       = int.Parse(LireValeur(cfg, "niveau_" + niveau + "_caBonus"));
        int ramBonus      = int.Parse(LireValeur(cfg, "niveau_" + niveau + "_ramBonus"));
        int charismeBonus = int.Parse(LireValeur(cfg, "niveau_" + niveau + "_charismeBonus"));
        if (pvBonus > 0)
        {
            json = AjouterValeur(json, "pvMax",     pvBonus);
            json = AjouterValeur(json, "pvActuels", pvBonus);
        }
        if (caBonus       > 0) json = AjouterValeur(json, "classeArmure", caBonus);
        if (ramBonus      > 0) json = AjouterValeur(json, "ram",          ramBonus);
        if (charismeBonus > 0) json = AjouterValeur(json, "charisme",     charismeBonus);
        return json;
    }

    // Message unique de montée de niveau — identique dans tous les fichiers qui donnent de l'XP
    private string MessageNiveau(string nomJoueur, int niveau)
    {
        string cfg   = File.ReadAllText(CONFIG_LEVEL);
        string bonus = LireValeur(cfg, "niveau_" + niveau + "_message");
        if (bonus == "0") bonus = "";

        int stats = int.Parse(LireValeur(cfg, "niveau_" + niveau + "_pvBonus"))
                  + int.Parse(LireValeur(cfg, "niveau_" + niveau + "_caBonus"))
                  + int.Parse(LireValeur(cfg, "niveau_" + niveau + "_ramBonus"))
                  + int.Parse(LireValeur(cfg, "niveau_" + niveau + "_charismeBonus"));

        string msg = "🎉 " + nomJoueur + " gagne 1 niveau (niveau " + niveau + ")";
        msg += stats > 0 && bonus != ""
             ? ", augmente sa stat de " + bonus + " et progresse vers le sommet !"
             : " et progresse vers le sommet !" + (bonus != "" ? " " + bonus : "");

        if (niveau >= int.Parse(LireValeur(cfg, "niveauMax"))) msg += " 👑 Niveau maximum atteint !";
        return msg;
    }

    // [0]=nom [1]=ticks [2]=xp [3]=ram [4]=demandeur [5]=type [6]=demandeurCle (textes du lore)
    // Retrouve la cle quete0NN correspondant a un _id (pour lire les champs annexes).
    private string QueteKeyDeId(string cfg, string id)
    {
        for (int i = 1; i <= 99; i++)
        {
            string key = QueteKey(i);
            string qid = LireValeurString(cfg, key + "_id");
            if (qid == "") break;
            if (qid == id) return key;
        }
        return "";
    }

    private bool EstFaite(string json, string qid)
    {
        string faites = LireValeurString(json, "quetesFaites");
        if (faites == "" || qid == "") return false;
        foreach (string f in faites.Split(','))
            if (f.Trim() == qid) return true;
        return false;
    }

    private string MarquerFaite(string json, string qid)
    {
        if (EstFaite(json, qid)) return json;
        string faites = LireValeurString(json, "quetesFaites");
        return ModifierValeurString(json, "quetesFaites", faites == "" ? qid : faites + "," + qid);
    }

    // Texte de resolution PROPRE a la quete ; a defaut, la replique du PNJ.
    private string TexteResolution(string cfgQ, string key, string genre, string demandeurCle, Random rng)
    {
        string t = LireValeurString(cfgQ, key + "_texte" + (genre == "succes" ? "Victoire" : "Echec"));
        if (t != "") return t;
        return TexteLore(demandeurCle, genre, rng);
    }

    // Recompense en equipement des secondaires : 1 item du pool de la zone.
    private string RecompenseItem(ref string json, string cfgQ, string key, Random rng, int maxSac)
    {
        string pool = LireValeurString(cfgQ, key + "_recompensePool");
        if (pool == "") return "";                       // le Desert paie en RAM, pas en objet

        string brut = LireValeurString(cfgQ, pool);
        if (brut == "") return "";

        string inv = LireValeurString(json, "inventaire");
        int nb = inv == "" ? 0 : inv.Split(',').Length;
        if (nb >= maxSac) return " (sac plein, la recompense est perdue !)";

        string[] items = brut.Split(',');
        string item = items[rng.Next(items.Length)].Trim();
        json = ModifierValeurString(json, "inventaire", inv == "" ? item : inv + "," + item);
        return " \U0001F381 " + item + " !";
    }

    private string[] GetQueteData(string id)
    {
        string cfg = File.ReadAllText(CONFIG_QUETES);
        for (int i = 1; i <= 99; i++)
        {
            string key = QueteKey(i);
            string qid = LireValeurString(cfg, key + "_id");
            if (qid == "") break;
            if (qid != id) continue;
            return new string[] {
                LireValeurString(cfg, key + "_nom"),
                LireValeur(cfg,       key + "_ticks"),
                LireValeur(cfg,       key + "_xp"),
                LireValeur(cfg,       key + "_ram"),
                LireValeurString(cfg, key + "_demandeur"),
                LireValeurString(cfg, key + "_type"),
                LireValeurString(cfg, key + "_demandeurCle")
            };
        }
        return new string[] { "", "1", "0", "0", "Arbonet", "service", "" };
    }

    private string QueteKey(int i)
    {
        if (i < 10)  return "quete00" + i;
        if (i < 100) return "quete0"  + i;
        return "quete" + i;
    }

    // Insère un champ s'il est absent du JSON (migration des anciens profils)
    private string EnsureChamp(string json, string cle, string valeurDefaut, bool estTexte)
    {
        if (json.Contains("\"" + cle + "\"")) return json;
        int    pos = json.LastIndexOf('}');
        string val = estTexte ? "\"" + valeurDefaut + "\"" : valeurDefaut;
        return json.Substring(0, pos) + ",\n  \"" + cle + "\": " + val + "\n}";
    }

    private string LireValeur(string json, string cle)
    {
        string marqueur = "\"" + cle + "\": ";
        int posDebut = json.IndexOf(marqueur);
        if (posDebut == -1) return "0";
        posDebut += marqueur.Length;
        int posFin = json.IndexOfAny(new char[] { ',', '\n', '}' }, posDebut);
        return json.Substring(posDebut, posFin - posDebut).Trim().Trim('"');
    }

    private string AjouterValeur(string json, string cle, int montant)
    {
        string marqueur = "\"" + cle + "\": ";
        int posDebut = json.IndexOf(marqueur);
        if (posDebut == -1) return json;
        posDebut += marqueur.Length;
        int posFin = json.IndexOfAny(new char[] { ',', '\n', '}' }, posDebut);
        string ancienneStr = json.Substring(posDebut, posFin - posDebut).Trim().Trim('"');
        int ancienne = int.TryParse(ancienneStr, out int v) ? v : 0;
        return json.Substring(0, posDebut) + (ancienne + montant).ToString() + json.Substring(posDebut + (posFin - posDebut));
    }

    private string ModifierValeur(string json, string cle, string val, bool estTexte)
    {
        string marqueur = "\"" + cle + "\": ";
        int posDebut = json.IndexOf(marqueur);
        if (posDebut == -1) return json;
        posDebut += marqueur.Length;
        int posFin = json.IndexOfAny(new char[] { ',', '\n', '}' }, posDebut);
        string ancienne = json.Substring(posDebut, posFin - posDebut);
        string nouvelle = estTexte ? "\"" + val + "\"" : val;
        return json.Substring(0, posDebut) + nouvelle + json.Substring(posDebut + ancienne.Length);
    }

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
}
