// sb-action: !fuir
// sb-subaction-id: 6b1a27ce-1fe8-4af2-b206-9f318f85bf90
using System;
using System.IO;

public class CPHInline
{
    private const string DOSSIER_JOUEURS = @"C:\Users\Florian\pjt\Pointu-PJT\Donnees\joueurs";
    private const string CONFIG_CLASSES  = @"C:\Users\Florian\pjt\Pointu-PJT\Donnees\config_classes.json";
    private const string CONFIG_ITEMS    = @"C:\Users\Florian\pjt\Pointu-PJT\Donnees\config_items.json";
    private const string CONFIG_GLOBAL   = @"C:\Users\Florian\pjt\Pointu-PJT\Donnees\config_global.json";
    private const string CONFIG_ENNEMIS  = @"C:\Users\Florian\pjt\Pointu-PJT\Donnees\config_ennemis.json";
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

        // === Contexte COMBAT DE BOSS (arène) : fuir = quitter + perte de PV + aucune récompense ===
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
                int perte = int.Parse(LireValeur(File.ReadAllText(CONFIG_ENNEMIS), bossNom + "_degatsMax"));
                if (perte <= 0) perte = int.Parse(LireValeur(cfgGb, "ennemi_degats_defaut"));

                int pvB   = int.Parse(LireValeur(json, "pvActuels"));
                int nvPvB = Math.Max(0, pvB - perte);
                json = ModifierValeur(json, "pvActuels", nvPvB.ToString(), false);
                File.WriteAllText(cheminFichier, json);

                // Retire le fuyard de l'arène (ordre, participants, effets ponctuels)
                string nouvOrdre = "";
                foreach (string p in ordreArr) if (p != pseudoKey) nouvOrdre = nouvOrdre == "" ? p : nouvOrdre + "," + p;
                int nb = nouvOrdre == "" ? 0 : nouvOrdre.Split(',').Length;
                etatB = ModifierValeurString(etatB, "ordre", nouvOrdre);
                etatB = ModifierValeurString(etatB, "participants", RetirerParticipant(LireValeurString(etatB, "participants"), pseudoKey));
                etatB = ModifierValeurString(etatB, "defenseurs", RetirerCsv(LireValeurString(etatB, "defenseurs"), pseudoKey));
                if (LireValeurString(etatB, "provocateur") == pseudoKey) etatB = ModifierValeurString(etatB, "provocateur", "");
                if (LireValeurString(etatB, "bribeSafe")   == pseudoKey) etatB = ModifierValeurString(etatB, "bribeSafe",   "");
                if (LireValeurString(etatB, "bribeCible")  == pseudoKey) etatB = ModifierValeurString(etatB, "bribeCible",  "");

                long maintB = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

                if (nb == 0)
                {
                    etatB = ReinitEtatBoss(etatB);
                    File.WriteAllText(ETAT_GLOBAL, etatB);
                    CPH.DisableTimer("ArenaCheck");
                    CPH.SendMessage("🏃 " + nomJoueur + " fuit " + bossNom + " (-" + perte + " PV) et l'arène se vide... " + bossNom + " triomphe sans combattre.");
                    return true;
                }

                string prefixF = "🏃 " + nomJoueur + " fuit " + bossNom + " (-" + perte + " PV dans le dos, aucune récompense) !";
                if (ti >= nb)
                {
                    etatB = ModifierValeur(etatB, "tourIndex", nb.ToString(), false);
                    etatB = ModifierValeur(etatB, "tourDeadline", maintB.ToString(), false);
                    File.WriteAllText(ETAT_GLOBAL, etatB);
                    CPH.SendMessage(prefixF + " ⏳ Tous ont agi — " + bossNom + " prépare sa riposte !");
                }
                else
                {
                    int timeout = int.Parse(LireValeur(cfgGb, "arene_tour_timeout_secondes"));
                    etatB = ModifierValeur(etatB, "tourIndex", ti.ToString(), false);
                    etatB = ModifierValeur(etatB, "tourDeadline", (maintB + timeout).ToString(), false);
                    File.WriteAllText(ETAT_GLOBAL, etatB);
                    CPH.SendMessage(prefixF + " ➡️ Au tour de " + nouvOrdre.Split(',')[ti] + " ! (!attaquer, !defense, !soin, !discuter, !fuir ou ta capacité)");
                }
                return true;
            }
        }

        if (LireValeur(json, "enCombat") != "true" || LireValeur(json, "enRencontre") != "true")
        {
            CPH.SendMessage(nomJoueur + ", tu n'as aucune rencontre à fuir pour l'instant.");
            return true;
        }

        string ennemNom = LireValeur(json, "ennemiNom");
        string classe   = LireValeur(json, "classe");
        string cfgG     = File.ReadAllText(CONFIG_GLOBAL);
        long maintenant = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        Random rng      = new Random();

        // Agilité : profil, fallback sur la classe (anciens profils), puis défaut global
        int agilite = int.Parse(LireValeur(json, "agilite"));
        if (agilite == 0)
        {
            agilite = int.Parse(LireValeur(File.ReadAllText(CONFIG_CLASSES), classe + "_agilite"));
            if (agilite == 0) agilite = int.Parse(LireValeur(cfgG, "agilite_defaut"));
        }
        int poids = GetBonusItems(json, "poids");

        int fuite = int.Parse(LireValeur(cfgG, "fuite_base_pct"))
                  + agilite * int.Parse(LireValeur(cfgG, "fuite_agilite_pct"))
                  - poids   * int.Parse(LireValeur(cfgG, "fuite_poids_pct"));
        fuite = Clamp(fuite, int.Parse(LireValeur(cfgG, "fuite_min")), int.Parse(LireValeur(cfgG, "fuite_max")));

        if (rng.Next(100) < fuite)
        {
            // FUITE RÉUSSIE : on quitte la rencontre, la quête reprend
            long pauseDebut = long.Parse(LireValeur(json, "quetePauseDebut"));
            long totalPause = long.Parse(LireValeur(json, "queteTotalPause"));
            if (pauseDebut > 0) totalPause += maintenant - pauseDebut;

            json = ModifierValeur(json, "enCombat", "false", false);
            json = ModifierValeur(json, "enRencontre", "false", false);
            json = ModifierValeur(json, "rencontreType", "", true);
            json = ModifierValeur(json, "rencontreExpire", "0", false);
            json = ModifierValeur(json, "quetePauseDebut", "0", false);
            json = ModifierValeur(json, "queteTotalPause", totalPause.ToString(), false);
            File.WriteAllText(cheminFichier, json);
            CPH.SendMessage(nomJoueur + " sème " + ennemNom + " → FUITE RÉUSSIE ! Ta quête reprend.");
        }
        else
        {
            // FUITE ÉCHOUÉE : la rencontre reste, pas de riposte (plus de tour par tour)
            CPH.SendMessage(nomJoueur + " tente de fuir " + ennemNom + " → ÉCHEC ! "
                + ennemNom + " te barre la route. Tape !combat ou !discuter.");
        }
        return true;
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

    private int Clamp(int v, int min, int max)
    {
        return v < min ? min : (v > max ? max : v);
    }

    private bool DansListe(string[] arr, string pseudo)
    {
        foreach (string p in arr) if (p == pseudo) return true;
        return false;
    }

    private string RetirerParticipant(string csv, string pseudo)
    {
        if (csv == "") return "";
        string res = "";
        foreach (string p in csv.Split(','))
        {
            string[] kv = p.Split(':');
            if (kv.Length == 2 && kv[0] == pseudo) continue;
            res = res == "" ? p : res + "," + p;
        }
        return res;
    }

    private string RetirerCsv(string csv, string pseudo)
    {
        if (csv == "") return "";
        string res = "";
        foreach (string p in csv.Split(','))
        {
            if (p == pseudo) continue;
            res = res == "" ? p : res + "," + p;
        }
        return res;
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

    private string LireValeur(string json, string cle)
    {
        string marqueur = "\"" + cle + "\": ";
        int posDebut    = json.IndexOf(marqueur);
        if (posDebut == -1) return "0";
        posDebut       += marqueur.Length;
        int posFin      = json.IndexOfAny(new char[] { ',', '\n', '}' }, posDebut);
        return json.Substring(posDebut, posFin - posDebut).Trim().Trim('"');
    }

    private string ModifierValeur(string json, string cle, string val, bool estTexte)
    {
        string marqueur = "\"" + cle + "\": ";
        int posDebut    = json.IndexOf(marqueur);
        if (posDebut == -1) return json;
        posDebut       += marqueur.Length;
        int posFin      = json.IndexOfAny(new char[] { ',', '\n', '}' }, posDebut);
        string ancienne = json.Substring(posDebut, posFin - posDebut);
        string nouvelle = estTexte ? "\"" + val + "\"" : val;
        return json.Substring(0, posDebut) + nouvelle + json.Substring(posDebut + ancienne.Length);
    }
}
