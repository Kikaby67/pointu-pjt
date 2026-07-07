// sb-action: attaquer boss
// sb-subaction-id: aa9edde6-c023-4690-9467-5bcee670f942
using System;
using System.IO;

// !attaquer — frapper le boss quand c'est ton tour (phase combat).
public class CPHInline
{
    private const string DOSSIER_JOUEURS = @"C:\Users\Florian\pjt\Pointu-PJT\Donnees\joueurs";
    private const string CONFIG_GLOBAL   = @"C:\Users\Florian\pjt\Pointu-PJT\Donnees\config_global.json";
    private const string CONFIG_ITEMS    = @"C:\Users\Florian\pjt\Pointu-PJT\Donnees\config_items.json";
    private const string CONFIG_CLASSES  = @"C:\Users\Florian\pjt\Pointu-PJT\Donnees\config_classes.json";
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

        string etat = File.ReadAllText(ETAT_GLOBAL);
        if (LireValeur(etat, "bossActif") != "true")
        {
            CPH.SendMessage(nomJoueur + ", aucun combat de boss en cours.");
            return true;
        }
        if (LireValeurString(etat, "bossPhase") != "combat")
        {
            CPH.SendMessage(nomJoueur + ", le combat n'a pas encore commencé ! Tape !arene pour rejoindre la bataille.");
            return true;
        }

        string pseudoKey = nomJoueur.ToLower();
        string ordre = LireValeurString(etat, "ordre");
        string[] ordreArr = ordre == "" ? new string[0] : ordre.Split(',');

        if (!DansListe(ordreArr, pseudoKey))
        {
            CPH.SendMessage(nomJoueur + ", tu ne participes pas à ce combat. Trop tard pour rejoindre !");
            return true;
        }

        int tourIndex = int.Parse(LireValeur(etat, "tourIndex"));
        if (tourIndex >= ordreArr.Length)
        {
            CPH.SendMessage(nomJoueur + ", tout le monde a frappé — " + LireValeurString(etat, "bossNom") + " prépare sa riposte !");
            return true;
        }
        if (ordreArr[tourIndex] != pseudoKey)
        {
            CPH.SendMessage(nomJoueur + ", ce n'est pas ton tour ! C'est à " + ordreArr[tourIndex] + " de frapper.");
            return true;
        }

        // === Dégâts du joueur sur le boss ===
        string json = File.ReadAllText(cheminFichier);
        Random rng = new Random();
        string cfgG = File.ReadAllText(CONFIG_GLOBAL);

        int atkEff = int.Parse(LireValeur(json, "bonusAttaque")) + GetBonusItems(json, "attaqueBonus");
        int niveau = int.Parse(LireValeur(json, "niveau"));
        string classe     = LireValeur(json, "classe");
        string sousClasse = LireValeur(json, "sousClasse");
        string cfgCls = File.ReadAllText(CONFIG_CLASSES);
        int nbAtq = (sousClasse != "" && sousClasse != "0") ? int.Parse(LireValeur(cfgCls, sousClasse + "_nbAttaques")) : 0;
        if (nbAtq == 0) nbAtq = int.Parse(LireValeur(cfgCls, classe + "_nbAttaques"));
        if (nbAtq == 0) nbAtq = 1;

        int degBase = int.Parse(LireValeur(cfgG, "boss_degats_base"));
        int degAlea = int.Parse(LireValeur(cfgG, "boss_degats_alea"));
        int degats  = degBase + (atkEff + niveau) * nbAtq + rng.Next(0, degAlea + 1);
        if (degats < 1) degats = 1;

        int pvMaxBoss = int.Parse(LireValeur(etat, "bossPVMax"));
        int pvBoss    = int.Parse(LireValeur(etat, "bossPVActuels")) - degats;

        // Cumul des dégâts (pour la récompense top dégâts)
        string participants = LireValeurString(etat, "participants");
        participants = SetParticipantDmg(participants, pseudoKey, LireParticipantDmg(participants, pseudoKey) + degats);
        etat = ModifierValeurString(etat, "participants", participants);

        string bossNom = LireValeurString(etat, "bossNom");

        // === BOSS VAINCU ===
        if (pvBoss <= 0)
        {
            CPH.SendMessage("💥 " + nomJoueur + " inflige " + degats + " dégâts... et porte le COUP FATAL à " + bossNom + " !");
            DistribuerRecompenses(participants, bossNom, cfgG);
            etat = ReinitEtat(etat);
            File.WriteAllText(ETAT_GLOBAL, etat);
            CPH.DisableTimer("ArenaCheck");
            return true;
        }

        etat = ModifierValeur(etat, "bossPVActuels", pvBoss.ToString(), false);

        // Avancer le tour
        int suivant = tourIndex + 1;
        long maintenant = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        int pct = pvMaxBoss > 0 ? (int)(100.0 * pvBoss / pvMaxBoss) : 0;

        if (suivant >= ordreArr.Length)
        {
            // Tous ont frappé → riposte du boss (résolue par le timer ArenaCheck)
            etat = ModifierValeur(etat, "tourIndex", suivant.ToString(), false);
            etat = ModifierValeur(etat, "tourDeadline", maintenant.ToString(), false);
            File.WriteAllText(ETAT_GLOBAL, etat);
            CPH.SendMessage("💥 " + nomJoueur + " inflige " + degats + " dégâts ! " + bossNom + " : " + pvBoss + "/" + pvMaxBoss + " PV (" + pct + "%). ⏳ Tous ont frappé — " + bossNom + " prépare une attaque dévastatrice !");
            return true;
        }

        int timeout = int.Parse(LireValeur(cfgG, "arene_tour_timeout_secondes"));
        etat = ModifierValeur(etat, "tourIndex", suivant.ToString(), false);
        etat = ModifierValeur(etat, "tourDeadline", (maintenant + timeout).ToString(), false);
        File.WriteAllText(ETAT_GLOBAL, etat);
        CPH.SendMessage("💥 " + nomJoueur + " inflige " + degats + " dégâts ! " + bossNom + " : " + pvBoss + "/" + pvMaxBoss + " PV (" + pct + "%). ➡️ Au tour de " + ordreArr[suivant] + " ! (!attaquer)");
        return true;
    }

    private string ReinitEtat(string etat)
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
        return etat;
    }

    // Récompenses : base à tous les participants, bonus + loot légendaire au meilleur dégâteur
    private void DistribuerRecompenses(string participants, string bossNom, string cfgG)
    {
        if (participants == "") return;
        string[] parts = participants.Split(',');

        string topPseudo = "";
        int topDmg = -1;
        foreach (string p in parts)
        {
            string[] kv = p.Split(':');
            if (kv.Length != 2) continue;
            int d = int.TryParse(kv[1], out int v) ? v : 0;
            if (d > topDmg) { topDmg = d; topPseudo = kv[0]; }
        }

        int baseXp   = int.Parse(LireValeur(cfgG, "boss_recompense_base_xp"));
        int baseRam  = int.Parse(LireValeur(cfgG, "boss_recompense_base_ram"));
        int bonusXp  = int.Parse(LireValeur(cfgG, "boss_top_bonus_xp"));
        int bonusRam = int.Parse(LireValeur(cfgG, "boss_top_bonus_ram"));
        int maxSac   = int.Parse(LireValeur(cfgG, "max_sac"));

        string loot = "";
        string poolName = LireValeurString(cfgG, "boss_loot_pool");
        string lootRaw  = LireValeurString(File.ReadAllText(CONFIG_QUETES), poolName);
        if (lootRaw != "")
        {
            string[] pool = lootRaw.Split(',');
            loot = pool[new Random().Next(pool.Length)].Trim();
        }

        int nbParticipants = 0;
        foreach (string p in parts)
        {
            string[] kv = p.Split(':');
            if (kv.Length != 2) continue;
            string pseudo = kv[0];
            string chemin = Path.Combine(DOSSIER_JOUEURS, pseudo + ".json");
            if (!File.Exists(chemin)) continue;
            nbParticipants++;

            string pj = File.ReadAllText(chemin);
            pj = AjouterValeur(pj, "experience", baseXp);
            pj = AjouterValeur(pj, "ram", baseRam);

            if (pseudo == topPseudo)
            {
                pj = AjouterValeur(pj, "experience", bonusXp);
                pj = AjouterValeur(pj, "ram", bonusRam);
                if (loot != "")
                {
                    string inv = LireValeurString(pj, "inventaire");
                    int nb = inv == "" ? 0 : inv.Split(',').Length;
                    if (nb < maxSac)
                    {
                        string nouvInv = inv == "" ? loot : inv + "," + loot;
                        pj = ModifierValeurString(pj, "inventaire", nouvInv);
                    }
                    else loot = "";
                }
            }
            pj = VerifierMonteeNiveau(pj, pseudo);
            File.WriteAllText(chemin, pj);
        }

        CPH.SendMessage("🏆 " + bossNom + " est VAINCU par la communauté (" + nbParticipants + " combattants) ! Chacun gagne " + baseXp + " XP et " + baseRam + " RAM.");
        if (topPseudo != "")
            CPH.SendMessage("👑 Meilleur combattant : " + topPseudo + " (" + topDmg + " dégâts) → +" + bonusXp + " XP, +" + bonusRam + " RAM" + (loot != "" ? " et " + loot + " (légendaire) !" : " !"));
    }

    // ===== Participants CSV ("pseudo:degats,...") =====
    private int LireParticipantDmg(string csv, string pseudo)
    {
        if (csv == "") return 0;
        foreach (string p in csv.Split(','))
        {
            string[] kv = p.Split(':');
            if (kv.Length == 2 && kv[0] == pseudo)
                return int.TryParse(kv[1], out int v) ? v : 0;
        }
        return 0;
    }

    private string SetParticipantDmg(string csv, string pseudo, int dmg)
    {
        string result = "";
        bool found = false;
        if (csv != "")
        {
            foreach (string p in csv.Split(','))
            {
                string[] kv = p.Split(':');
                if (kv.Length != 2) continue;
                if (kv[0] == pseudo) { result = Append(result, pseudo + ":" + dmg); found = true; }
                else                 { result = Append(result, kv[0] + ":" + kv[1]); }
            }
        }
        if (!found) result = Append(result, pseudo + ":" + dmg);
        return result;
    }

    private string Append(string csv, string entry) { return csv == "" ? entry : csv + "," + entry; }

    private bool DansListe(string[] arr, string pseudo)
    {
        foreach (string p in arr) if (p == pseudo) return true;
        return false;
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

    private string VerifierMonteeNiveau(string json, string nomJoueur)
    {
        int niveauActuel  = int.Parse(LireValeur(json, "niveau"));
        int nouvelXP      = int.Parse(LireValeur(json, "experience"));
        int nouveauNiveau = CalculerNiveau(nouvelXP);
        if (nouveauNiveau > niveauActuel)
        {
            json = ModifierValeur(json, "niveau", nouveauNiveau.ToString(), false);
            json = AppliquerBonusNiveau(json, nouveauNiveau);
            string cfg   = File.ReadAllText(CONFIG_LEVEL);
            string bonus = LireValeur(cfg, "niveau_" + nouveauNiveau + "_message");
            CPH.SendMessage("🎉 " + nomJoueur + " passe au niveau " + nouveauNiveau + " ! " + bonus);
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

    // ===== Helpers JSON =====
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
