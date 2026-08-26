// sb-action: !bouledefeu
// sb-subaction-id: e8e42c97-73b5-4e0b-8831-be3524621efe
// sb-group: Boss
// sb-trigger: command !bouledefeu
// Capacité Hackmancien — Boule de feu : gros dégâts fixes (nature + techno). Config: boss_bouledefeu_*. Coûte du mana.
// La victoire (boss à 0 PV) est finalisée par ArenaCheck.
using System;
using System.IO;

public class CPHInline
{
    private const string DOSSIER_JOUEURS = @"C:\Users\Florian\pjt\Pointu-PJT\Donnees\joueurs";
    private const string CONFIG_GLOBAL   = @"C:\Users\Florian\pjt\Pointu-PJT\Donnees\config_global.json";
    private const string ETAT_GLOBAL     = @"C:\Users\Florian\pjt\Pointu-PJT\Donnees\etat_global.json";
    private const string CONFIG_CLASSES  = @"C:\Users\Florian\pjt\Pointu-PJT\Donnees\config_classes.json";
    private const string CLASSE_REQUISE  = "Hackmancien";

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
        if (LireValeur(json, "classe") != CLASSE_REQUISE) { CPH.SendMessage(nomJoueur + ", la Boule de feu est réservée aux " + CLASSE_REQUISE + "s."); return true; }

        string cfgG = File.ReadAllText(CONFIG_GLOBAL);
        int cout  = int.Parse(LireValeur(cfgG, "boss_bouledefeu_mana"));
        int mana  = int.Parse(LireValeur(json, "manaActuels"));
        if (mana < cout) { CPH.SendMessage(nomJoueur + ", pas assez de mana pour la Boule de feu (" + mana + "/" + cout + ")."); return true; }

        int niveau  = int.Parse(LireValeur(json, "niveau"));
        int degats  = int.Parse(LireValeur(cfgG, "boss_bouledefeu_base")) + niveau
                    + BonusSousClasse(LireValeur(json, "sousClasse"), "bouledefeuBonus");
        if (int.Parse(LireValeur(etat, "buffDesTours")) > 0)
            degats += int.Parse(LireValeur(cfgG, "boss_danse_bonus"));
        if (int.Parse(LireValeur(etat, "bossCaMalusTours")) > 0)
            degats += int.Parse(LireValeur(cfgG, "boss_anatheme_degats"));
        if (degats < 1) degats = 1;

        json = ModifierValeur(json, "manaActuels", (mana - cout).ToString(), false);
        File.WriteAllText(cheminFichier, json);

        int pvMaxBoss = int.Parse(LireValeur(etat, "bossPVMax"));
        int pvBoss    = int.Parse(LireValeur(etat, "bossPVActuels")) - degats;
        string bossNom = LireValeurString(etat, "bossNom");

        string participants = LireValeurString(etat, "participants");
        participants = SetParticipantDmg(participants, pseudoKey, LireParticipantDmg(participants, pseudoKey) + degats);
        etat = ModifierValeurString(etat, "participants", participants);

        if (pvBoss <= 0)
        {
            etat = ModifierValeur(etat, "bossPVActuels", "0", false);
            File.WriteAllText(ETAT_GLOBAL, etat);
            CPH.SendMessage("🔥 " + nomJoueur + " embrase " + bossNom + " d'une Boule de feu (" + degats + " dégâts)... COUP FATAL ! 🏆 Les récompenses arrivent...");
            return true;
        }

        etat = ModifierValeur(etat, "bossPVActuels", pvBoss.ToString(), false);
        int pct = pvMaxBoss > 0 ? (int)(100.0 * pvBoss / pvMaxBoss) : 0;

        long maintenant = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        AvancerTour(etat, ordreArr, tourIndex, cfgG, maintenant, "🔥 " + nomJoueur + " lance une Boule de feu (nature + techno) pour " + degats + " dégâts ! " + bossNom + " : " + pvBoss + "/" + pvMaxBoss + " PV (" + pct + "%).");
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

    // Bonus d'arene accorde par la sous-classe (config_classes). 0 si absent.
    private int BonusSousClasse(string sousClasse, string cle)
    {
        if (sousClasse == "" || sousClasse == "0") return 0;
        int v;
        return int.TryParse(LireValeur(File.ReadAllText(CONFIG_CLASSES), sousClasse + "_" + cle), out v) ? v : 0;
    }
}
