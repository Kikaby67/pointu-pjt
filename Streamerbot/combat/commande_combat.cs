// sb-action: !combat
// sb-subaction-id: ca3cebb3-d828-40cd-bb92-eb65832a8fe0
using System;
using System.IO;

public class CPHInline
{
    private const string DOSSIER_JOUEURS = @"C:\Users\Florian\pjt\Pointu-PJT\Donnees\joueurs";
    private const string CONFIG_ENNEMIS  = @"C:\Users\Florian\pjt\Pointu-PJT\Donnees\config_ennemis.json";
    private const string CONFIG_ITEMS    = @"C:\Users\Florian\pjt\Pointu-PJT\Donnees\config_items.json";
    private const string CONFIG_LEVEL    = @"C:\Users\Florian\pjt\Pointu-PJT\Donnees\config_level.json";
    private const string CONFIG_GLOBAL   = @"C:\Users\Florian\pjt\Pointu-PJT\Donnees\config_global.json";
    private const string CONFIG_CLASSES  = @"C:\Users\Florian\pjt\Pointu-PJT\Donnees\config_classes.json";
    private const string CONFIG_QUETES   = @"C:\Users\Florian\pjt\Pointu-PJT\Donnees\config_quetes.json";

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

        if (LireValeur(json, "enCombat") != "true" || LireValeur(json, "enRencontre") != "true")
        {
            CPH.SendMessage(nomJoueur + ", tu n'as aucune rencontre à affronter pour l'instant.");
            return true;
        }

        string ennemNom = LireValeur(json, "ennemiNom");
        string tier     = GetEnnemiTier(ennemNom);
        long maintenant = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        Random rng      = new Random();

        string cfgG = File.ReadAllText(CONFIG_GLOBAL);

        // === CALCUL DE LA CHANCE DE RÉUSSITE (toutes les stats + niveau + attaques) ===
        int pvMax   = int.Parse(LireValeur(json, "pvMax"));
        int caEff   = int.Parse(LireValeur(json, "classeArmure")) + GetBonusItems(json, "caBonus");
        int atkEff  = int.Parse(LireValeur(json, "bonusAttaque"))  + GetBonusItems(json, "attaqueBonus");
        int manaEff = int.Parse(LireValeur(json, "manaMax"))   + GetBonusItems(json, "manaBonus");
        int chaEff  = int.Parse(LireValeur(json, "charisme"))  + GetBonusItems(json, "charismeBonus");
        int agi     = int.Parse(LireValeur(json, "agilite"));
        int niveau  = int.Parse(LireValeur(json, "niveau"));

        // Nombre d'attaques (sous-classe prioritaire, puis classe, défaut 1) → réintègre les sous-classes
        string classe     = LireValeur(json, "classe");
        string sousClasse = LireValeur(json, "sousClasse");
        string cfgCls = File.ReadAllText(CONFIG_CLASSES);
        int nbAtq = (sousClasse != "" && sousClasse != "0") ? int.Parse(LireValeur(cfgCls, sousClasse + "_nbAttaques")) : 0;
        if (nbAtq == 0) nbAtq = int.Parse(LireValeur(cfgCls, classe + "_nbAttaques"));
        if (nbAtq == 0) nbAtq = 1;

        int score = int.Parse(LireValeur(cfgG, "combat_base_pct"))
            + Tranche(cfgG, pvMax,   "pv")
            + Tranche(cfgG, caEff,   "ca")
            + Tranche(cfgG, atkEff,  "atk")
            + Tranche(cfgG, manaEff, "mana")
            + Tranche(cfgG, chaEff,  "cha")
            + Tranche(cfgG, agi,     "agi")
            + Tranche(cfgG, niveau,  "niveau")
            + (nbAtq - 1) * int.Parse(LireValeur(cfgG, "combat_attaques_pct"));

        score = Clamp(score, int.Parse(LireValeur(cfgG, "combat_plancher_joueur")), int.Parse(LireValeur(cfgG, "combat_plafond_joueur")));

        string compagnon = LireValeurString(json, "compagnonActif");
        if (compagnon != "")
            score += int.Parse(LireValeur(cfgG, "compagnon_combat_bonus"));

        int tierMod = TierMod(cfgG, tier);
        int finalPct = Clamp(score + tierMod, int.Parse(LireValeur(cfgG, "combat_min")), int.Parse(LireValeur(cfgG, "combat_max")));

        // === JET ===
        bool reussite = rng.Next(100) < finalPct;
        int diviseur     = int.Parse(LireValeur(cfgG, "combat_pv_perte_diviseur"));
        int facteurEchec = int.Parse(LireValeur(cfgG, "combat_pv_perte_echec_facteur"));
        int alea         = int.Parse(LireValeur(cfgG, "combat_pv_perte_alea"));
        int baseToll     = (int)Math.Ceiling((100.0 - finalPct) / diviseur) + rng.Next(0, alea + 1);

        int pvActuels = int.Parse(LireValeur(json, "pvActuels"));
        int cooldown  = int.Parse(LireValeur(cfgG, "quete_cooldown_defaite_secondes"));
        string compTxt = compagnon != "" ? " (compagnon " + compagnon + ")" : "";

        if (reussite)
        {
            // VICTOIRE : toll de PV + récompenses
            int perte    = Math.Min(baseToll, pvActuels);
            int nvPV      = pvActuels - perte;
            int[] recomp  = GetRecompensesEnnemi(ennemNom);

            json = ModifierValeur(json, "pvActuels", nvPV.ToString(), false);
            json = AjouterValeur(json, "experience", recomp[0]);
            json = AjouterValeur(json, "ram", recomp[1]);
            json = AjouterValeur(json, "combatsGagnes", 1);
            json = VerifierMonteeNiveau(json, nomJoueur);

            // Mini-boss : loot garanti (rareté supérieure), si le sac a de la place
            string lootMsg = "";
            if (tier == "miniboss")
            {
                string inventaire = LireValeurString(json, "inventaire");
                int nbItems = inventaire == "" ? 0 : inventaire.Split(',').Length;
                int maxSac  = int.Parse(LireValeur(cfgG, "max_sac"));
                if (nbItems < maxSac)
                {
                    string poolName = LireValeurString(cfgG, "mini_boss_loot_pool");
                    string cfgLoot  = File.ReadAllText(CONFIG_QUETES);
                    string lootRaw  = LireValeurString(cfgLoot, poolName);
                    if (lootRaw == "") lootRaw = LireValeurString(cfgLoot, "loot_commun");
                    string[] lootPool = lootRaw != "" ? lootRaw.Split(',') : new string[] { "Potion" };
                    string loot = lootPool[rng.Next(lootPool.Length)].Trim();
                    string nouvInv = inventaire == "" ? loot : inventaire + "," + loot;
                    json = ModifierValeurString(json, "inventaire", nouvInv);
                    lootMsg = " 🎁 Butin de mini-boss : " + loot + " !";
                }
                else
                {
                    lootMsg = " (sac plein, butin de mini-boss perdu !)";
                }
            }

            string baseMsg = nomJoueur + " affronte " + ennemNom + compTxt
                           + " → VICTOIRE ! -" + perte + " PV (" + nvPV + "/" + pvMax + "), +" + recomp[0] + " XP, +" + recomp[1] + " RAM." + lootMsg;

            if (nvPV <= 0)
            {
                json = TerminerQuete(json, maintenant, 0);   // effondré mais vainqueur : pas de cooldown
                File.WriteAllText(cheminFichier, json);
                CPH.SendMessage(baseMsg + " Mais tu t'effondres, vidé de tes forces ! Va te soigner dans l'Antre (!repos) avant de repartir.");
            }
            else
            {
                json = ReprendreQuete(json, maintenant);
                File.WriteAllText(cheminFichier, json);
                CPH.SendMessage(baseMsg + " Ta quête reprend !");
            }
            return true;
        }

        // === ÉCHEC ===
        json = AjouterValeur(json, "combatsPerdus", 1);

        if (tier == "fort" || tier == "miniboss")
        {
            // KO + quête échouée + cooldown
            json = ModifierValeur(json, "pvActuels", "0", false);
            json = TerminerQuete(json, maintenant, cooldown);
            File.WriteAllText(cheminFichier, json);
            CPH.SendMessage(nomJoueur + " affronte " + ennemNom + compTxt
                + " → DÉFAITE ! Le " + ennemNom + " t'envoie au tapis. Quête échouée, tu récupères dans l'Antre ("
                + (cooldown / 60) + " min).");
            return true;
        }

        // Échec vs faible/moyen : grosse perte de PV mais survie possible
        int perteEchec = baseToll * facteurEchec;
        int pvApres    = Math.Max(0, pvActuels - perteEchec);
        int perteReelle = pvActuels - pvApres;
        json = ModifierValeur(json, "pvActuels", pvApres.ToString(), false);

        if (pvApres <= 0)
        {
            json = TerminerQuete(json, maintenant, cooldown);
            File.WriteAllText(cheminFichier, json);
            CPH.SendMessage(nomJoueur + " affronte " + ennemNom + compTxt
                + " → DÉFAITE ! -" + perteReelle + " PV, tu t'effondres. Quête échouée, repos dans l'Antre ("
                + (cooldown / 60) + " min).");
        }
        else
        {
            json = ReprendreQuete(json, maintenant);
            File.WriteAllText(cheminFichier, json);
            CPH.SendMessage(nomJoueur + " affronte " + ennemNom + compTxt
                + " → ÉCHEC ! Tu encaisses durement -" + perteReelle + " PV (" + pvApres + "/" + pvMax
                + ") mais tu t'en sors. Ta quête reprend.");
        }
        return true;
    }

    // Reprend la quête : ferme la rencontre et comptabilise la pause
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

    // Termine la quête (effondrement / défaite). cooldownSecondes = 0 → pas de cooldown.
    private string TerminerQuete(string json, long maintenant, int cooldownSecondes)
    {
        json = ModifierValeur(json, "enCombat", "false", false);
        json = ModifierValeur(json, "enRencontre", "false", false);
        json = ModifierValeur(json, "rencontreType", "", true);
        json = ModifierValeur(json, "rencontreExpire", "0", false);
        json = ModifierValeur(json, "enQuete", "false", false);
        json = ModifierValeur(json, "queteTicksRestants", "0", false);
        json = ModifierValeur(json, "quetePauseDebut", "0", false);
        json = EnsureChamp(json, "compagnonActif", "", true);
        json = ModifierValeur(json, "compagnonActif", "", true);
        if (cooldownSecondes > 0)
            json = ModifierValeur(json, "queteCooldownFin", (maintenant + cooldownSecondes).ToString(), false);
        return json;
    }

    private int TierMod(string cfgG, string tier)
    {
        if (tier == "faible")   return int.Parse(LireValeur(cfgG, "combat_tier_faible_mod"));
        if (tier == "fort")     return int.Parse(LireValeur(cfgG, "combat_tier_fort_mod"));
        if (tier == "miniboss") return int.Parse(LireValeur(cfgG, "combat_tier_miniboss_mod"));
        return int.Parse(LireValeur(cfgG, "combat_tier_moyen_mod"));
    }

    private string GetEnnemiTier(string nom)
    {
        string t = LireValeurString(File.ReadAllText(CONFIG_ENNEMIS), nom + "_tier");
        return t == "" ? "moyen" : t;
    }

    private int[] GetRecompensesEnnemi(string nom)
    {
        string cfg  = File.ReadAllText(CONFIG_ENNEMIS);
        string cfgG = File.ReadAllText(CONFIG_GLOBAL);
        int xp  = int.Parse(LireValeur(cfg, nom + "_xp"));
        int ram = int.Parse(LireValeur(cfg, nom + "_ram"));
        return new int[] {
            xp  != 0 ? xp  : int.Parse(LireValeur(cfgG, "ennemi_xp_defaut")),
            ram != 0 ? ram : int.Parse(LireValeur(cfgG, "ennemi_ram_defaut"))
        };
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

    // Contribution d'une stat au score : ((valeur - ref) / tranche) * pct (clés combat_<prefixe>_*)
    private int Tranche(string cfgG, int valeur, string prefixe)
    {
        int refv = int.Parse(LireValeur(cfgG, "combat_" + prefixe + "_ref"));
        int tr   = int.Parse(LireValeur(cfgG, "combat_" + prefixe + "_tranche"));
        int pct  = int.Parse(LireValeur(cfgG, "combat_" + prefixe + "_pct"));
        if (tr == 0) tr = 1;
        return ((valeur - refv) / tr) * pct;
    }

    private int Clamp(int v, int min, int max)
    {
        return v < min ? min : (v > max ? max : v);
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

    private string MessageNiveau(string nomJoueur, int niveau)
    {
        string cfg   = File.ReadAllText(CONFIG_LEVEL);
        string bonus = LireValeur(cfg, "niveau_" + niveau + "_message");
        return "🎉 " + nomJoueur + " passe au niveau " + niveau + " ! " + bonus;
    }

    // Insère un champ s'il est absent (anciens profils)
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
