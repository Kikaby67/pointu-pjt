// sb-action: attaquer boss
// sb-subaction-id: aa9edde6-c023-4690-9467-5bcee670f942
using System;
using System.IO;

// !attaquer — frapper le boss quand c'est ton tour (phase combat).
// La victoire (boss à 0 PV) est finalisée par le timer ArenaCheck.
public class CPHInline
{
    private const string DOSSIER_JOUEURS = @"C:\Users\Florian\pjt\Pointu-PJT\Donnees\joueurs";
    private const string CONFIG_GLOBAL   = @"C:\Users\Florian\pjt\Pointu-PJT\Donnees\config_global.json";
    private const string CONFIG_ITEMS    = @"C:\Users\Florian\pjt\Pointu-PJT\Donnees\config_items.json";
    private const string CONFIG_CLASSES  = @"C:\Users\Florian\pjt\Pointu-PJT\Donnees\config_classes.json";
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
            CPH.SendMessage(nomJoueur + ", tout le monde a agi — " + LireValeurString(etat, "bossNom") + " prépare sa riposte !");
            return true;
        }
        if (ordreArr[tourIndex] != pseudoKey)
        {
            CPH.SendMessage(nomJoueur + ", ce n'est pas ton tour ! C'est à " + ordreArr[tourIndex] + " d'agir.");
            return true;
        }

        // === Dégâts du joueur sur le boss (buffs de groupe pris en compte) ===
        string json = File.ReadAllText(cheminFichier);
        Random rng = new Random();
        string cfgG = File.ReadAllText(CONFIG_GLOBAL);

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
        int degats  = degBase + (atkEff + niveau) * nbAtq + rng.Next(0, degAlea + 1);
        string frappesTxt = nbAtq > 1 ? " en " + nbAtq + " frappes" : "";
        if (int.Parse(LireValeur(etat, "buffDesTours")) > 0)
            degats += int.Parse(LireValeur(cfgG, "boss_danse_bonus"));
        if (int.Parse(LireValeur(etat, "bossCaMalusTours")) > 0)
            degats += int.Parse(LireValeur(cfgG, "boss_anatheme_degats"));
        if (degats < 1) degats = 1;

        int pvMaxBoss = int.Parse(LireValeur(etat, "bossPVMax"));
        int pvBoss    = int.Parse(LireValeur(etat, "bossPVActuels")) - degats;

        // Cumul des dégâts (pour la récompense top dégâts)
        string participants = LireValeurString(etat, "participants");
        participants = SetParticipantDmg(participants, pseudoKey, LireParticipantDmg(participants, pseudoKey) + degats);
        etat = ModifierValeurString(etat, "participants", participants);

        string bossNom = LireValeurString(etat, "bossNom");

        // === BOSS VAINCU (récompenses distribuées par ArenaCheck au prochain tick) ===
        if (pvBoss <= 0)
        {
            etat = ModifierValeur(etat, "bossPVActuels", "0", false);
            File.WriteAllText(ETAT_GLOBAL, etat);
            CPH.SendMessage("💥 " + nomJoueur + " inflige " + degats + " dégâts" + frappesTxt + "... et porte le COUP FATAL à " + bossNom + " ! 🏆 Les récompenses arrivent...");
            return true;
        }

        etat = ModifierValeur(etat, "bossPVActuels", pvBoss.ToString(), false);

        // Avancer le tour
        int suivant = tourIndex + 1;
        long maintenant = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        int pct = pvMaxBoss > 0 ? (int)(100.0 * pvBoss / pvMaxBoss) : 0;

        if (suivant >= ordreArr.Length)
        {
            etat = ModifierValeur(etat, "tourIndex", suivant.ToString(), false);
            etat = ModifierValeur(etat, "tourDeadline", maintenant.ToString(), false);
            File.WriteAllText(ETAT_GLOBAL, etat);
            CPH.SendMessage("💥 " + nomJoueur + " inflige " + degats + " dégâts" + frappesTxt + " ! " + bossNom + " : " + pvBoss + "/" + pvMaxBoss + " PV (" + pct + "%). ⏳ Tous ont agi — " + bossNom + " prépare une attaque dévastatrice !");
            return true;
        }

        int timeout = int.Parse(LireValeur(cfgG, "arene_tour_timeout_secondes"));
        etat = ModifierValeur(etat, "tourIndex", suivant.ToString(), false);
        etat = ModifierValeur(etat, "tourDeadline", (maintenant + timeout).ToString(), false);
        File.WriteAllText(ETAT_GLOBAL, etat);
        CPH.SendMessage("💥 " + nomJoueur + " inflige " + degats + " dégâts" + frappesTxt + " ! " + bossNom + " : " + pvBoss + "/" + pvMaxBoss + " PV (" + pct + "%). ➡️ Au tour de " + ordreArr[suivant] + " ! (!attaquer, !defense, !soin, !discuter, !fuir ou ta capacité)");
        return true;
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
