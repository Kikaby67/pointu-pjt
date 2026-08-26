// sb-action: !discuter
// sb-subaction-id: 339f8db5-bc42-42a5-97c7-a15830a78a40
using System;
using System.IO;

public class CPHInline
{
    private const string DOSSIER_JOUEURS = @"C:\Users\Florian\pjt\Pointu-PJT\Donnees\joueurs";
    private const string CONFIG_ITEMS    = @"C:\Users\Florian\pjt\Pointu-PJT\Donnees\config_items.json";
    private const string CONFIG_CLASSES  = @"C:\Users\Florian\pjt\Pointu-PJT\Donnees\config_classes.json";
    private const string CONFIG_GLOBAL   = @"C:\Users\Florian\pjt\Pointu-PJT\Donnees\config_global.json";
    private const string CONFIG_ENNEMIS  = @"C:\Users\Florian\pjt\Pointu-PJT\Donnees\config_ennemis.json";
    private const string CONFIG_QUETES   = @"C:\Users\Florian\pjt\Pointu-PJT\Donnees\config_quetes.json";
    private const string CONFIG_LEVEL    = @"C:\Users\Florian\pjt\Pointu-PJT\Donnees\config_level.json";
    private const string ETAT_GLOBAL     = @"C:\Users\Florian\pjt\Pointu-PJT\Donnees\etat_global.json";

    public bool Execute()
    {
        string nomJoueur     = args["user"].ToString();
        string cheminFichier = Path.Combine(DOSSIER_JOUEURS, nomJoueur.ToLower() + ".json");

        if (!File.Exists(cheminFichier))
        {
            CPH.SendMessage(nomJoueur + ", tu n'es pas encore inscrit ! Tape !rejoindre.");
            return true;
        }

        string json = File.ReadAllText(cheminFichier);
        json = EnsureChamp(json, "compagnonActif", "", true);

        // === Contexte COMBAT DE BOSS (arène) : amadouer le boss (faible chance de fin pacifique) ===
        string etatB = File.ReadAllText(ETAT_GLOBAL);
        if (LireValeur(etatB, "bossActif") == "true" && LireValeurString(etatB, "bossPhase") == "combat")
        {
            string pseudoKey = nomJoueur.ToLower();
            string ordreB = LireValeurString(etatB, "ordre");
            string[] ordreArr = ordreB == "" ? new string[0] : ordreB.Split(',');
            if (DansListe(ordreArr, pseudoKey))
            {
                int ti = int.Parse(LireValeur(etatB, "tourIndex"));
                if (ti >= ordreArr.Length) { CPH.SendMessage(nomJoueur + ", tout le monde a agi — riposte imminente !"); return true; }
                if (ordreArr[ti] != pseudoKey) { CPH.SendMessage(nomJoueur + ", ce n'est pas ton tour ! C'est à " + ordreArr[ti] + " d'agir."); return true; }

                string cfgGb   = File.ReadAllText(CONFIG_GLOBAL);
                string bossNom = LireValeurString(etatB, "bossNom");
                int chance = int.Parse(LireValeur(cfgGb, "boss_discuter_chance"));
                long maintB = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

                if (new Random().Next(100) < chance)
                {
                    // Le boss change d'avis → fin PACIFIQUE : récompense de base aux participants (pas de loot top)
                    DistribuerBase(LireValeurString(etatB, "participants"), cfgGb);
                    etatB = ReinitEtatBoss(etatB);
                    File.WriteAllText(ETAT_GLOBAL, etatB);
                    CPH.DisableTimer("ArenaCheck");
                    CPH.SendMessage("🕊️ " + nomJoueur + " entraîne " + bossNom + " dans une discussion profonde sur le sens de la vie... et le convainc ! " + bossNom + " renonce au combat. Paix retrouvée.");
                    return true;
                }

                AvancerTour(etatB, ordreArr, ti, cfgGb, maintB, "🗣️ " + nomJoueur + " philosophe avec " + bossNom + " sur le sens de la vie... mais le monstre reste de marbre.");
                return true;
            }
        }

        if (LireValeur(json, "enCombat") != "true" || LireValeur(json, "enRencontre") != "true")
        {
            CPH.SendMessage(nomJoueur + ", il n'y a personne avec qui discuter pour l'instant.");
            return true;
        }

        string ennemNom  = LireValeur(json, "ennemiNom");
        string cfgG      = File.ReadAllText(CONFIG_GLOBAL);
        long maintenant  = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        Random rng       = new Random();

        int charismeEff = int.Parse(LireValeur(json, "charisme")) + GetBonusItems(json, "charismeBonus");
        int discuter = int.Parse(LireValeur(cfgG, "discuter_base_pct"))
                     + charismeEff * int.Parse(LireValeur(cfgG, "discuter_charisme_pct"))
                     + BonusSousClasse(File.ReadAllText(CONFIG_CLASSES), LireValeur(json, "sousClasse"), "bonusDiscuterPct");
        discuter = Clamp(discuter, int.Parse(LireValeur(cfgG, "discuter_min")), int.Parse(LireValeur(cfgG, "discuter_max")));

        bool reussite    = rng.Next(100) < discuter;
        string compagnon = LireValeurString(json, "compagnonActif");

        // === Créature SAUVABLE par la parole (ex. Grenouille-Corrompue du Marais) ===
        // Elle n'a pas totalement perdu la langue commune : lui parler la libère de la
        // corruption. C'est une VICTOIRE (récompenses + butin), pas un recrutement.
        // Piloté par config_ennemis.json — aucun nom d'ennemi en dur ici.
        string cfgE = File.ReadAllText(CONFIG_ENNEMIS);
        if (LireValeurString(cfgE, ennemNom + "_discuterSauve") == "true")
        {
            if (!reussite)
            {
                File.WriteAllText(cheminFichier, json);   // rencontre maintenue
                CPH.SendMessage(nomJoueur + " tend la main vers " + ennemNom
                    + " → ÉCHEC ! Elle n'entend que le bruit de la corruption. Réessaie !discuter, ou !combat / !fuir.");
                return true;
            }

            int gainXp  = int.Parse(LireValeur(cfgE, ennemNom + "_xp"));
            int gainRam = int.Parse(LireValeur(cfgE, ennemNom + "_ram"));
            json = AjouterValeur(json, "experience",    gainXp);
            json = AjouterValeur(json, "ram",           gainRam);
            json = AjouterValeur(json, "combatsGagnes", 1);

            // Butin : pool dédié au sauvetage (meilleur que le loot de mini-boss tué)
            string lootMsg   = "";
            string invSauve  = LireValeurString(json, "inventaire");
            int nbSacSauve   = invSauve == "" ? 0 : invSauve.Split(',').Length;
            if (nbSacSauve < int.Parse(LireValeur(cfgG, "max_sac")))
            {
                string cfgLoot = File.ReadAllText(CONFIG_QUETES);
                string poolNom = LireValeurString(cfgE, ennemNom + "_discuterLootPool");
                string lootRaw = LireValeurString(cfgLoot, poolNom);
                if (lootRaw == "") lootRaw = LireValeurString(cfgLoot, "loot_rare");
                string[] pool  = lootRaw != "" ? lootRaw.Split(',') : new string[] { "Potion" };
                string loot    = pool[rng.Next(pool.Length)].Trim();
                json    = ModifierValeurString(json, "inventaire", invSauve == "" ? loot : invSauve + "," + loot);
                lootMsg = " 🎁 Elle te confie " + loot + " !";
            }
            else
            {
                lootMsg = " (sac plein, son présent est perdu !)";
            }

            json = VerifierMonteeNiveau(json, nomJoueur);
            json = ReprendreQuete(json, maintenant);
            File.WriteAllText(cheminFichier, json);
            CPH.SendMessage("💚 " + nomJoueur + " parle à " + ennemNom
                + " jusqu'à ce que la corruption lâche prise → SAUVÉE ! Elle se redresse sur ses deux pattes et te remercie. +"
                + gainXp + " XP · +" + gainRam + " RAM." + lootMsg + " Ta quête reprend.");
            return true;
        }

        if (compagnon == "")
        {
            if (reussite)
            {
                // Recrutement : l'ennemi devient compagnon (jusqu'à fin de quête / défaite)
                json = ModifierValeur(json, "compagnonActif", ennemNom, true);
                json = ReprendreQuete(json, maintenant);
                File.WriteAllText(cheminFichier, json);
                CPH.SendMessage(nomJoueur + " tente de parlementer avec " + ennemNom + " → RÉUSSITE ! "
                    + ennemNom + " rejoint ta cause et combattra à tes côtés ! Ta quête reprend.");
            }
            else
            {
                File.WriteAllText(cheminFichier, json); // rencontre maintenue
                CPH.SendMessage(nomJoueur + " tente de parlementer avec " + ennemNom + " → ÉCHEC ! "
                    + ennemNom + " ne veut rien entendre. Tape !combat ou !fuir.");
            }
        }
        else
        {
            if (reussite)
            {
                // Déjà un compagnon → simple passage pacifique
                json = ReprendreQuete(json, maintenant);
                File.WriteAllText(cheminFichier, json);
                CPH.SendMessage(nomJoueur + " et son compagnon " + compagnon + " désamorcent la rencontre avec " + ennemNom
                    + " → RÉUSSITE ! Vous passez sans combattre. Ta quête reprend.");
            }
            else
            {
                File.WriteAllText(cheminFichier, json); // rencontre maintenue
                CPH.SendMessage(nomJoueur + " tente de parlementer → ÉCHEC ! " + ennemNom
                    + " refuse. Tape !combat (ton compagnon " + compagnon + " t'aidera) ou !fuir.");
            }
        }
        return true;
    }

    // Bonus plat accorde par la sous-classe (config_classes). 0 si la cle n'existe pas :
    // toutes les sous-classes n'agissent pas sur tous les leviers.
    private int BonusSousClasse(string cfgCls, string sousClasse, string cle)
    {
        if (sousClasse == "" || sousClasse == "0") return 0;
        int v;
        return int.TryParse(LireValeur(cfgCls, sousClasse + "_" + cle), out v) ? v : 0;
    }

    private string ReprendreQuete(string json, long maintenant)
    {
        long pauseDebut = long.Parse(LireValeur(json, "quetePauseDebut"));
        long totalPause = long.Parse(LireValeur(json, "queteTotalPause"));
        if (pauseDebut > 0) totalPause += maintenant - pauseDebut;

        json = ModifierValeur(json, "enCombat", "false", false);
        json = ModifierValeur(json, "enRencontre", "false", false);
        json = ModifierValeur(json, "rencontreType", "", true);
        json = ModifierValeur(json, "rencontreExpire", "0", false);
        json = ModifierValeur(json, "quetePauseDebut", "0", false);
        json = ModifierValeur(json, "queteTotalPause", totalPause.ToString(), false);
        return json;
    }

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

    // ===== Contexte boss (arène) =====
    private bool DansListe(string[] arr, string pseudo)
    {
        foreach (string p in arr) if (p == pseudo) return true;
        return false;
    }

    private string AvancerTour(string etat, string[] ordreArr, int tourIndex, string cfgG, long maintenant, string prefix)
    {
        int suivant = tourIndex + 1;
        if (suivant >= ordreArr.Length)
        {
            etat = ModifierValeur(etat, "tourIndex", suivant.ToString(), false);
            etat = ModifierValeur(etat, "tourDeadline", maintenant.ToString(), false);
            File.WriteAllText(ETAT_GLOBAL, etat);
            CPH.SendMessage(prefix + " ⏳ Tous ont agi — " + LireValeurString(etat, "bossNom") + " prépare sa riposte !");
        }
        else
        {
            int timeout = int.Parse(LireValeur(cfgG, "arene_tour_timeout_secondes"));
            etat = ModifierValeur(etat, "tourIndex", suivant.ToString(), false);
            etat = ModifierValeur(etat, "tourDeadline", (maintenant + timeout).ToString(), false);
            File.WriteAllText(ETAT_GLOBAL, etat);
            CPH.SendMessage(prefix + " ➡️ Au tour de " + ordreArr[suivant] + " ! (!attaquer, !defense, !soin, !discuter, !fuir ou ta capacité)");
        }
        return etat;
    }

    // Récompense de base (fin pacifique) : XP/RAM à tous les participants, pas de bonus top ni de loot
    private void DistribuerBase(string participants, string cfgG)
    {
        if (participants == "") return;
        int baseXp  = int.Parse(LireValeur(cfgG, "boss_recompense_base_xp"));
        int baseRam = int.Parse(LireValeur(cfgG, "boss_recompense_base_ram"));
        foreach (string p in participants.Split(','))
        {
            string[] kv = p.Split(':');
            if (kv.Length != 2) continue;
            string chemin = Path.Combine(DOSSIER_JOUEURS, kv[0] + ".json");
            if (!File.Exists(chemin)) continue;
            string pj = File.ReadAllText(chemin);
            pj = AjouterValeur(pj, "experience", baseXp);
            pj = AjouterValeur(pj, "ram", baseRam);
            pj = VerifierMonteeNiveau(pj, kv[0]);
            File.WriteAllText(chemin, pj);
        }
        CPH.SendMessage("🏆 Récompense de base pour tous les participants : +" + baseXp + " XP, +" + baseRam + " RAM.");
    }

    private string ReinitEtatBoss(string etat)
    {
        etat = ModifierValeur(etat, "bossActif", "false", false);
        etat = ModifierValeurString(etat, "bossPhase", "");
        etat = ModifierValeurString(etat, "bossNom", "");
        etat = ModifierValeur(etat, "bossPVMax", "0", false);
        etat = ModifierValeur(etat, "bossPVActuels", "0", false);
        etat = ModifierValeur(etat, "areneFin", "0", false);
        etat = ModifierValeurString(etat, "ordre", "");
        etat = ModifierValeur(etat, "tourIndex", "0", false);
        etat = ModifierValeur(etat, "tourDeadline", "0", false);
        etat = ModifierValeurString(etat, "participants", "");
        etat = ModifierValeur(etat, "buffCaTours", "0", false);
        etat = ModifierValeur(etat, "buffAtkTours", "0", false);
        etat = ModifierValeur(etat, "buffDesTours", "0", false);
        etat = ModifierValeur(etat, "bossCaMalusTours", "0", false);
        etat = ModifierValeur(etat, "bossAtkMalusTours", "0", false);
        etat = ModifierValeurString(etat, "defenseurs", "");
        etat = ModifierValeurString(etat, "provocateur", "");
        etat = ModifierValeurString(etat, "bribeSafe", "");
        etat = ModifierValeurString(etat, "bribeCible", "");
        return etat;
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
        if (pvBonus > 0) { json = AjouterValeur(json, "pvMax", pvBonus); json = AjouterValeur(json, "pvActuels", pvBonus); }
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

    private int Clamp(int v, int min, int max)
    {
        return v < min ? min : (v > max ? max : v);
    }

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
}
