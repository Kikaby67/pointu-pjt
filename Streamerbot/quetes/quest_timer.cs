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
                    string typeR    = LireValeur(json, "rencontreType");
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

                    if (typeR == "combat")
                        CPH.SendMessage(nomJoueur + ", l'ennemi se lasse de t'attendre et s'éloigne. Ta quête reprend !");
                    else
                        CPH.SendMessage(nomJoueur + ", la rencontre prend fin — tu n'as pas répondu à temps. Ta quête reprend !");
                }
                auMoinsUneActive = true; continue; // attendre le choix du joueur
            }

            // === CAS 2 : check rencontre toutes les N secondes ===
            bool encounterLancee = false;
            long dernierCheck = long.Parse(LireValeur(json, "dernierCheckRencontre"));

            if (maintenant - dernierCheck >= intervalleRenc)
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

                    if (estMiniBoss)
                        CPH.SendMessage(nomJoueur + ", ⚠️ MINI-BOSS ! " + ennemiChoisi + " te barre la route ! Quête en pause — !combat, !discuter ou !fuir. (" + (expireSecs / 60) + " min)");
                    else
                        CPH.SendMessage(nomJoueur + ", un " + ennemiChoisi + " surgit sur ta route ! Quête en pause — !combat, !discuter ou !fuir. (" + (expireSecs / 60) + " min)");
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
                    string msg      = "";

                    if (type == "marchand_potion")
                    {
                        int prix = int.Parse(LireValeur(cfgAllies, "marchand_prix_potion"));
                        offreVal = prix;
                        msg = nomJoueur + ", un marchand ambulant te propose une Potion pour " + prix + " RAM ! Quête en pause — !accepter pour acheter | !refuser pour décliner. (" + (expireSecs / 60) + " min)";
                    }
                    else if (type == "vieux_sage")
                    {
                        int xpMin = int.Parse(LireValeur(cfgAllies, "vieux_sage_xp_min"));
                        int xpMax = int.Parse(LireValeur(cfgAllies, "vieux_sage_xp_max"));
                        offreVal  = rng.Next(xpMin, xpMax + 1);
                        string[] scenariosSage = {
                            nomJoueur + ", le Vieux Sage d'Arbonet surgit de la brume et te propose un marché pour " + offreVal + " XP ! Quête en pause — !accepter ou !refuser. (" + (expireSecs / 60) + " min)",
                            nomJoueur + ", un Vieux Sage t'interpelle sur ta route — sagesse contre passage... (" + offreVal + " XP en jeu). Quête en pause — !accepter ou !refuser. (" + (expireSecs / 60) + " min)",
                            nomJoueur + ", le Vieux Sage apparaît, silencieux. Il tend la main vers toi en offrant " + offreVal + " XP. Quête en pause — !accepter ou !refuser. (" + (expireSecs / 60) + " min)"
                        };
                        msg = scenariosSage[rng.Next(scenariosSage.Length)];
                    }
                    else if (type == "bonus_ram")
                    {
                        int ramMin = int.Parse(LireValeur(cfgAllies, "source_ram_min"));
                        int ramMax = int.Parse(LireValeur(cfgAllies, "source_ram_max"));
                        offreVal   = rng.Next(ramMin, ramMax + 1);
                        msg = nomJoueur + ", une bourse de " + offreVal + " RAM brille sur le sol ! Quête en pause — !accepter pour ramasser | !refuser pour passer. (" + (expireSecs / 60) + " min)";
                    }
                    else if (type == "alcove_chene")
                    {
                        msg = nomJoueur + ", tu croises une alcôve de chêne-serveur apaisante — elle peut restaurer entièrement ta vitalité ! Quête en pause — !accepter pour te reposer | !refuser pour continuer. (" + (expireSecs / 60) + " min)";
                    }
                    else // marchand_classe
                    {
                        msg = nomJoueur + ", un Marchand de Classe t'aborde ! Tu peux changer de classe. Tape !choisirclasse [nom] pour changer (Hexadécimeur · Cryptolame · Hackmancien · Firewaller · Algorythmancien) | !refuser pour décliner. (" + (expireSecs / 60) + " min)";
                    }

                    json = ModifierValeur(json, "enRencontre",     "true",                              false);
                    json = ModifierValeur(json, "rencontreType",   type,                                true);
                    json = ModifierValeur(json, "enCombat",        "false",                             false);
                    json = ModifierValeur(json, "offreValeur",     offreVal.ToString(),                 false);
                    json = ModifierValeur(json, "quetePauseDebut", maintenant.ToString(),               false);
                    json = ModifierValeur(json, "rencontreExpire", (maintenant + expireSecs).ToString(), false);
                    File.WriteAllText(chemin, json);
                    CPH.SendMessage(msg);
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

                string lootMsg = "";
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
                CPH.SendMessage(nomJoueur + ", ta quête est terminée ! Succès ! Tu gagnes " + xp + " XP et " + ram + " RAM." + lootMsg + " Bien joué aventurier !");
            }
            else
            {
                File.WriteAllText(chemin, json);
                CPH.SendMessage(nomJoueur + ", ta quête est terminée... Échec. Le destin ne t'a pas souri cette fois. Retente ta chance bientôt !");
            }
          }
          catch (Exception ex)
          {
              CPH.LogWarn("QuestCheck : profil ignoré (" + chemin + ") — " + ex.Message);
          }
        }

        if (!auMoinsUneActive) CPH.DisableTimer("QuestCheck");

        return true;
    }

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

    // [0]=nom [1]=ticks [2]=xp [3]=ram [4]=demandeur [5]=type
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
                LireValeurString(cfg, key + "_type")
            };
        }
        return new string[] { "", "1", "0", "0", "Arbonet", "service" };
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
