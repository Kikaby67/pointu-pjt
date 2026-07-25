// sb-action: !zeroday
// sb-subaction-id: 8fcd18f9-9923-4999-91ff-89b0f6d27e12
// sb-group: Boss
// sb-trigger: command !zeroday
// Capacité Cryptolame — Lame du Zero-Day : attaque à crit croissant selon les PV manquants du boss (config: boss_zeroday_crit_facteur).
// Plus le boss est bas, plus le crit est fort (×1 à plein → ×3 à zéro avec facteur 2). La victoire est finalisée par ArenaCheck.
using System;
using System.IO;

public class CPHInline
{
    private const string DOSSIER_JOUEURS = @"C:\Users\Florian\pjt\Pointu-PJT\Donnees\joueurs";
    private const string CONFIG_GLOBAL   = @"C:\Users\Florian\pjt\Pointu-PJT\Donnees\config_global.json";
    private const string CONFIG_ITEMS    = @"C:\Users\Florian\pjt\Pointu-PJT\Donnees\config_items.json";
    private const string CONFIG_CLASSES  = @"C:\Users\Florian\pjt\Pointu-PJT\Donnees\config_classes.json";
    private const string ETAT_GLOBAL     = @"C:\Users\Florian\pjt\Pointu-PJT\Donnees\etat_global.json";
    private const string CLASSE_REQUISE  = "Cryptolame";

    public bool Execute()
    {
        string nomJoueur     = args["user"].ToString();
        string cheminFichier = Path.Combine(DOSSIER_JOUEURS, nomJoueur.ToLower() + ".json");
        if (!File.Exists(cheminFichier)) { CPH.SendMessage(nomJoueur + ", tu n'es pas encore inscrit ! Tape !rejoindre."); return true; }

        string etat = File.ReadAllText(ETAT_GLOBAL);
        if (LireValeur(etat, "bossActif") != "true") { CPH.SendMessage(nomJoueur + ", aucun combat de boss en cours."); return true; }
        if (LireValeurString(etat, "bossPhase") != "combat") { CPH.SendMessage(nomJoueur + ", le combat n'a pas encore commencé ! Tape !arene."); return true; }

        string pseudoKey = nomJoueur.ToLower();
        string ordre = LireValeurString(etat, "ordre");
        string[] ordreArr = ordre == "" ? new string[0] : ordre.Split(',');
        if (!DansListe(ordreArr, pseudoKey)) { CPH.SendMessage(nomJoueur + ", tu ne participes pas à ce combat."); return true; }

        int tourIndex = int.Parse(LireValeur(etat, "tourIndex"));
        if (tourIndex >= ordreArr.Length) { CPH.SendMessage(nomJoueur + ", tout le monde a agi — riposte imminente !"); return true; }
        if (ordreArr[tourIndex] != pseudoKey) { CPH.SendMessage(nomJoueur + ", ce n'est pas ton tour ! C'est à " + ordreArr[tourIndex] + " d'agir."); return true; }

        string json = File.ReadAllText(cheminFichier);
        if (LireValeur(json, "classe") != CLASSE_REQUISE) { CPH.SendMessage(nomJoueur + ", la Lame du Zero-Day est réservée aux " + CLASSE_REQUISE + "s."); return true; }

        string cfgG = File.ReadAllText(CONFIG_GLOBAL);
        Random rng  = new Random();

        // Dégâts de base (avec buffs de groupe), puis crit selon les PV manquants du boss
        int atkEff = int.Parse(LireValeur(json, "bonusAttaque")) + GetBonusItems(json, "attaqueBonus");
        if (int.Parse(LireValeur(etat, "buffAtkTours")) > 0)
            atkEff += int.Parse(LireValeur(cfgG, "boss_surcharge_atk"));
        int niveau = int.Parse(LireValeur(json, "niveau"));
        string classe     = LireValeur(json, "classe");
        string sousClasse = LireValeur(json, "sousClasse");
        string cfgCls = File.ReadAllText(CONFIG_CLASSES);
        int nbAtq = (sousClasse != "" && sousClasse != "0") ? int.Parse(LireValeur(cfgCls, sousClasse + "_nbAttaques")) : 0;
        if (nbAtq == 0) nbAtq = int.Parse(LireValeur(cfgCls, classe + "_nbAttaques"));
        if (nbAtq == 0) nbAtq = 1;

        int degBase = int.Parse(LireValeur(cfgG, "boss_degats_base"));
        int degAlea = int.Parse(LireValeur(cfgG, "boss_degats_alea"));
        int baseDeg = degBase + (atkEff + niveau) * nbAtq + rng.Next(0, degAlea + 1);
        if (int.Parse(LireValeur(etat, "buffDesTours")) > 0)
            baseDeg += int.Parse(LireValeur(cfgG, "boss_danse_bonus"));
        if (int.Parse(LireValeur(etat, "bossCaMalusTours")) > 0)
            baseDeg += int.Parse(LireValeur(cfgG, "boss_anatheme_degats"));

        int pvMaxBoss = int.Parse(LireValeur(etat, "bossPVMax"));
        int pvActuBoss = int.Parse(LireValeur(etat, "bossPVActuels"));
        double ratioManquant = pvMaxBoss > 0 ? (1.0 - (double)pvActuBoss / pvMaxBoss) : 0;
        int    facteur   = int.Parse(LireValeur(cfgG, "boss_zeroday_crit_facteur"));
        double critMult  = 1.0 + ratioManquant * facteur;
        int    degats    = (int)Math.Round(baseDeg * critMult);
        if (degats < 1) degats = 1;

        int cm10 = (int)Math.Round(critMult * 10);
        string critAff = (cm10 / 10) + "." + (cm10 % 10);

        int pvBoss = pvActuBoss - degats;
        string bossNom = LireValeurString(etat, "bossNom");

        // Cumul des dégâts
        string participants = LireValeurString(etat, "participants");
        participants = SetParticipantDmg(participants, pseudoKey, LireParticipantDmg(participants, pseudoKey) + degats);
        etat = ModifierValeurString(etat, "participants", participants);

        if (pvBoss <= 0)
        {
            etat = ModifierValeur(etat, "bossPVActuels", "0", false);
            File.WriteAllText(ETAT_GLOBAL, etat);
            CPH.SendMessage("🗡️ " + nomJoueur + " exploite la Lame du Zero-Day (×" + critAff + ") pour " + degats + " dégâts... COUP FATAL à " + bossNom + " ! 🏆 Les récompenses arrivent...");
            return true;
        }

        etat = ModifierValeur(etat, "bossPVActuels", pvBoss.ToString(), false);
        int pct = pvMaxBoss > 0 ? (int)(100.0 * pvBoss / pvMaxBoss) : 0;

        long maintenant = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        AvancerTour(etat, ordreArr, tourIndex, cfgG, maintenant, "🗡️ " + nomJoueur + " frappe avec la Lame du Zero-Day (crit ×" + critAff + ") pour " + degats + " dégâts ! " + bossNom + " : " + pvBoss + "/" + pvMaxBoss + " PV (" + pct + "%).");
        return true;
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
