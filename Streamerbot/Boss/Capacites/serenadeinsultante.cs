// sb-action: !serenadeinsultante
// sb-subaction-id: c7188377-4504-48e6-82db-04106890888c
// sb-group: Boss
// sb-trigger: command !serenadeinsultante
// Capacité Algorythmancien — Sérénade insultante : insulte la "daronne" du boss, réduit sa puissance d'attaque. Config: boss_serenade_*.
using System;
using System.IO;

public class CPHInline
{
    private const string DOSSIER_JOUEURS = @"C:\Users\Florian\pjt\Pointu-PJT\Donnees\joueurs";
    private const string CONFIG_GLOBAL   = @"C:\Users\Florian\pjt\Pointu-PJT\Donnees\config_global.json";
    private const string CONFIG_INSULTES = @"C:\Users\Florian\pjt\Pointu-PJT\Donnees\config_insultes.json";
    private const string ETAT_GLOBAL     = @"C:\Users\Florian\pjt\Pointu-PJT\Donnees\etat_global.json";
    private const string CLASSE_REQUISE  = "Algorythmancien";

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
        if (LireValeur(json, "classe") != CLASSE_REQUISE) { CPH.SendMessage(nomJoueur + ", la Sérénade insultante est réservée aux " + CLASSE_REQUISE + "s."); return true; }

        string cfgG = File.ReadAllText(CONFIG_GLOBAL);
        int cout  = int.Parse(LireValeur(cfgG, "boss_serenade_mana"));
        int mana  = int.Parse(LireValeur(json, "manaActuels"));
        if (mana < cout) { CPH.SendMessage(nomJoueur + ", pas assez de mana pour la Sérénade insultante (" + mana + "/" + cout + ")."); return true; }

        int    reduction = int.Parse(LireValeur(cfgG, "boss_serenade_reduction_pct"));
        int    tours     = int.Parse(LireValeur(cfgG, "boss_serenade_tours"));
        // Répliques dans un fichier à part (git-ignoré) — absent = repli neutre, la capacité marche quand même
        string insultes  = File.Exists(CONFIG_INSULTES)
                         ? LireValeurString(File.ReadAllText(CONFIG_INSULTES), "boss_serenade_insultes")
                         : "";
        string[] pool    = insultes == "" ? new string[] { "Ta carte mère tourne encore à la manivelle !" } : insultes.Split('|');
        string insulte   = pool[new Random().Next(pool.Length)].Trim();

        json = ModifierValeur(json, "manaActuels", (mana - cout).ToString(), false);
        File.WriteAllText(cheminFichier, json);

        etat = ModifierValeur(etat, "bossAtkMalusTours", tours.ToString(), false);

        long maintenant = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        AvancerTour(etat, ordreArr, tourIndex, cfgG, maintenant, "🎤 " + nomJoueur + " balance : « " + insulte + " » — " + LireValeurString(etat, "bossNom") + ", vexé, frappe " + reduction + "% moins fort pendant " + tours + " tours !");
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
}
