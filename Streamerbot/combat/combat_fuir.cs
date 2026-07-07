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
