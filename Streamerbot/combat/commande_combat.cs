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
    private const string CONFIG_LORE     = @"C:\Users\Florian\pjt\Pointu-PJT\Donnees\config_lore_textes.json";

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

        string compagnon = LireValeurString(json, "compagnonActif");
        int puissance = Puissance(json, cfgG, cfgCls, nbAtq, compagnon != "");
        int finalMille = ChanceMille(puissance, cfgG, TierMille(cfgG, tier));
        int finalPct   = finalMille / 10;      // sert encore au calcul de la perte de PV

        // === JET === (sur 1000 : chaque point de stat compte)
        bool reussite = rng.Next(1000) < finalMille;
        int diviseur     = int.Parse(LireValeur(cfgG, "combat_pv_perte_diviseur"));
        int facteurEchec = int.Parse(LireValeur(cfgG, "combat_pv_perte_echec_facteur"));
        int alea         = int.Parse(LireValeur(cfgG, "combat_pv_perte_alea"));
        int baseToll     = (int)Math.Ceiling((100.0 - finalPct) / diviseur) + rng.Next(0, alea + 1);

        int pvActuels = int.Parse(LireValeur(json, "pvActuels"));
        int cooldown  = int.Parse(LireValeur(cfgG, "quete_cooldown_defaite_secondes"));
        string compTxt    = compagnon != "" ? " (compagnon " + compagnon + ")" : "";
        string frappesTxt = TexteFrappes(nbAtq, reussite, rng);

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

            // La résolution est une réponse à une commande tapée : c'est ici que le lore
            // s'exprime sans coûter un message de plus au chat.
            string baseMsg = "⚔️ " + TexteLore(ennemNom, "victoire", rng) + " " + nomJoueur + compTxt + frappesTxt
                           + " -" + perte + " PV (" + nvPV + "/" + pvMax + ") · +" + recomp[0] + " XP · +" + recomp[1] + " RAM." + lootMsg;

            if (nvPV <= 0)
            {
                json = TerminerQuete(json, maintenant, 0);   // effondré mais vainqueur : pas de cooldown
                File.WriteAllText(cheminFichier, json);
                CPH.SendMessage(baseMsg + " Mais il s'effondre, vidé de ses forces — !repos avant de repartir.");
            }
            else
            {
                json = ReprendreQuete(json, maintenant);
                File.WriteAllText(cheminFichier, json);
                CPH.SendMessage(baseMsg + " Sa quête reprend.");
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
            CPH.SendMessage("💥 " + TexteLore(ennemNom, "defaite", rng) + " " + nomJoueur + compTxt + frappesTxt
                + " est au tapis — quête échouée, repos dans l'Antre (" + (cooldown / 60) + " min).");
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
            CPH.SendMessage("💥 " + TexteLore(ennemNom, "defaite", rng) + " " + nomJoueur + compTxt + frappesTxt
                + " -" + perteReelle + " PV, il s'effondre. Quête échouée, repos dans l'Antre ("
                + (cooldown / 60) + " min).");
        }
        else
        {
            json = ReprendreQuete(json, maintenant);
            File.WriteAllText(cheminFichier, json);
            CPH.SendMessage("💥 " + TexteLore(ennemNom, "defaite", rng) + " " + nomJoueur + compTxt + frappesTxt
                + " encaisse -" + perteReelle + " PV (" + pvApres + "/" + pvMax
                + ") mais s'en sort. Sa quête reprend.");
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

    // Palier de l'ennemi, en milliemes (config_global : combat_tier_<tier>_mille)
    private int TierMille(string cfgG, string tier)
    {
        int v;
        if (int.TryParse(LireValeur(cfgG, "combat_tier_" + tier + "_mille"), out v)) return v;
        return 0;
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
    // Bonus plat accorde par la sous-classe (config_classes). 0 si la cle n'existe pas :
    // toutes les sous-classes n'agissent pas sur tous les leviers.
    private int BonusSousClasse(string cfgCls, string sousClasse, string cle)
    {
        if (sousClasse == "" || sousClasse == "0") return 0;
        int v;
        return int.TryParse(LireValeur(cfgCls, sousClasse + "_" + cle), out v) ? v : 0;
    }

    // Rend VISIBLE le multi-attaque des sous-classes. Sans ca, "3 attaques par tour"
    // n'est qu'un +12% invisible dans la formule : le joueur ne voit jamais ses frappes.
    // Purement narratif — la resolution reste le jet unique, l'equilibrage est intact.
    private string TexteFrappes(int nbAtq, bool reussite, Random rng)
    {
        if (nbAtq <= 1) return "";                       // classe mono-attaque : pas de bruit
        if (reussite)   return " (" + nbAtq + " frappes portées)";
        int portees = rng.Next(0, nbAtq);                // 0..nbAtq-1 : un echec ne porte jamais tout
        if (portees == 0) return " (aucune frappe ne porte)";
        return " (" + portees + " frappe" + (portees > 1 ? "s" : "") + " sur " + nbAtq + ")";
    }

    // === PUISSANCE ===
    // Une seule echelle lineaire, au lieu des 7 tranches a division entiere qui
    // saturaient des le 2e palier d'equipement (Rare a Legendaire donnaient le meme
    // resultat que Commun). Tous les poids sont dans config_global.
    private int Puissance(string json, string cfgG, string cfgCls, int nbAtq, bool compagnon)
    {
        int pv   = int.Parse(LireValeur(json, "pvMax"));
        int ca   = int.Parse(LireValeur(json, "classeArmure")) + GetBonusItems(json, "caBonus");
        int atk  = int.Parse(LireValeur(json, "bonusAttaque"))  + GetBonusItems(json, "attaqueBonus");
        int mana = int.Parse(LireValeur(json, "manaMax"))       + GetBonusItems(json, "manaBonus");
        int cha  = int.Parse(LireValeur(json, "charisme"))      + GetBonusItems(json, "charismeBonus");
        int agi  = int.Parse(LireValeur(json, "agilite"));
        int niv  = int.Parse(LireValeur(json, "niveau"));

        int p = pv   * int.Parse(LireValeur(cfgG, "combat_poids_pv"))
              + ca   * int.Parse(LireValeur(cfgG, "combat_poids_ca"))
              + atk  * int.Parse(LireValeur(cfgG, "combat_poids_atk"))
              + niv  * int.Parse(LireValeur(cfgG, "combat_poids_niveau"))
              + mana / 10 * int.Parse(LireValeur(cfgG, "combat_poids_mana"))
              + cha  * int.Parse(LireValeur(cfgG, "combat_poids_charisme"))
              + agi  * int.Parse(LireValeur(cfgG, "combat_poids_agilite"))
              + (nbAtq - 1) * int.Parse(LireValeur(cfgG, "combat_puissance_par_attaque"));

        // L'Algorythmancien se bat mal SEUL : son malus est exactement compense
        // par un compagnon. Il ne frappe pas, il fait frapper.
        string classe     = LireValeur(json, "classe");
        string sousClasse = LireValeur(json, "sousClasse");
        int modifCl; int.TryParse(LireValeur(cfgCls, classe + "_puissanceModif"), out modifCl);
        p += modifCl;
        p += BonusSousClasse(cfgCls, sousClasse, "bonusPuissance");
        if (compagnon) p += int.Parse(LireValeur(cfgG, "combat_puissance_compagnon"));
        return p;
    }

    // Puissance -> chance de reussite, en MILLIEMES (le tirage se fait sur 1000,
    // sinon un bonus de +1 attaque disparaitrait dans l'arrondi au pourcent).
    private int ChanceMille(int puissance, string cfgG, int tierMille)
    {
        int m = int.Parse(LireValeur(cfgG, "combat_base_mille"))
              + (puissance - int.Parse(LireValeur(cfgG, "combat_socle")))
                * int.Parse(LireValeur(cfgG, "combat_pente_num"))
                / int.Parse(LireValeur(cfgG, "combat_pente_den"))
              + tierMille;
        return Clamp(m, int.Parse(LireValeur(cfgG, "combat_mille_min")),
                        int.Parse(LireValeur(cfgG, "combat_mille_max")));
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

    // Tire une variante narrative dans config_lore_textes.json.
    // Énumère <ennemi>_<genre>_01, _02... jusqu'à clé absente, puis replie sur defaut_<genre>_XX.
    // LireValeurString OBLIGATOIRE : ces textes contiennent des virgules, LireValeur couperait
    // la phrase à la première. Fichier absent ou nom d'ennemi inconnu -> chaîne vide, le jeu tourne.
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
