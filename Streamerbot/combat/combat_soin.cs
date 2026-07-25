// sb-action: !soin
// sb-subaction-id: 41ac9bf9-5566-4aa4-96e1-42ab1ab75290
using System;
using System.IO;

public class CPHInline
{
    private const string DOSSIER_JOUEURS = @"C:\Users\Florian\pjt\Pointu-PJT\Donnees\joueurs";
    private const string CONFIG_CLASSES  = @"C:\Users\Florian\pjt\Pointu-PJT\Donnees\config_classes.json";
    private const string CONFIG_GLOBAL   = @"C:\Users\Florian\pjt\Pointu-PJT\Donnees\config_global.json";
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

        if (LireValeur(json, "classeChoisie") != "true")
        {
            CPH.SendMessage(nomJoueur + ", choisis d'abord une classe avec !choisirclasse !");
            return true;
        }

        // === Contexte COMBAT DE BOSS (arène) — le soin consomme le tour ===
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

                string cfgGb = File.ReadAllText(CONFIG_GLOBAL);
                int coutB  = int.Parse(LireValeur(cfgGb, "boss_soin_mana"));
                int manaB  = int.Parse(LireValeur(json, "manaActuels"));
                int pvB    = int.Parse(LireValeur(json, "pvActuels"));
                int pvMaxB = int.Parse(LireValeur(json, "pvMax"));
                if (pvB >= pvMaxB) { CPH.SendMessage(nomJoueur + ", tu as déjà tous tes PV (" + pvB + "/" + pvMaxB + ") ! Choisis une autre action."); return true; }
                if (manaB < coutB) { CPH.SendMessage(nomJoueur + ", pas assez de mana pour te soigner (" + manaB + "/" + coutB + ")."); return true; }

                Random rngB = new Random();
                int soin = RollSoin(LireValeur(json, "classe"), LireValeur(json, "sousClasse"), rngB);
                if (int.Parse(LireValeur(etatB, "buffDesTours")) > 0) soin += int.Parse(LireValeur(cfgGb, "boss_danse_bonus"));
                int nvPvB = Math.Min(pvB + soin, pvMaxB);
                json = ModifierValeur(json, "pvActuels",   nvPvB.ToString(),          false);
                json = ModifierValeur(json, "manaActuels", (manaB - coutB).ToString(), false);
                File.WriteAllText(cheminFichier, json);

                long maintB = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                AvancerTour(etatB, ordreArr, ti, cfgGb, maintB, "💚 " + nomJoueur + " se soigne +" + (nvPvB - pvB) + " PV → " + nvPvB + "/" + pvMaxB + " PV !");
                return true;
            }
        }

        // Soin désormais HORS combat uniquement
        if (LireValeur(json, "enCombat") == "true")
        {
            CPH.SendMessage(nomJoueur + ", impossible de te soigner en pleine rencontre ! Choisis !combat, !discuter ou !fuir.");
            return true;
        }

        string classe     = LireValeur(json, "classe");
        string sousClasse  = LireValeur(json, "sousClasse");
        int joueurPV       = int.Parse(LireValeur(json, "pvActuels"));
        int joueurPVMax    = int.Parse(LireValeur(json, "pvMax"));
        int mana           = int.Parse(LireValeur(json, "manaActuels"));
        int manaCout       = int.Parse(LireValeur(File.ReadAllText(CONFIG_GLOBAL), "combat_mana_cout_soin"));

        if (joueurPV >= joueurPVMax)
        {
            CPH.SendMessage(nomJoueur + ", tu as déjà tous tes PV (" + joueurPV + "/" + joueurPVMax + ") !");
            return true;
        }

        if (mana < manaCout)
        {
            CPH.SendMessage(nomJoueur + ", pas assez de mana (" + mana + "/" + manaCout + " requis) pour te soigner.");
            return true;
        }

        Random rng         = new Random();
        int soinRoll       = RollSoin(classe, sousClasse, rng);
        int nouveauPV      = Math.Min(joueurPV + soinRoll, joueurPVMax);
        int soinsEffectifs = nouveauPV - joueurPV;

        json = ModifierValeur(json, "pvActuels",   nouveauPV.ToString(),         false);
        json = ModifierValeur(json, "manaActuels", (mana - manaCout).ToString(), false);
        File.WriteAllText(cheminFichier, json);

        CPH.SendMessage(nomJoueur + " canalise l'énergie d'Arbonet et se soigne +" + soinsEffectifs + " PV → "
            + nouveauPV + "/" + joueurPVMax + " PV (" + (mana - manaCout) + " mana restant).");
        return true;
    }

    private int RollSoin(string classe, string sousClasse, Random rng)
    {
        string cfg  = File.ReadAllText(CONFIG_CLASSES);
        string cfgG = File.ReadAllText(CONFIG_GLOBAL);
        string key    = (sousClasse != "" && LireValeur(cfg, sousClasse + "_soinMax") != "0") ? sousClasse : classe;
        int soinMax   = int.Parse(LireValeur(cfg, key + "_soinMax"));
        int soinBonus = int.Parse(LireValeur(cfg, key + "_soinBonus"));
        if (soinMax == 0) soinMax = int.Parse(LireValeur(cfgG, "soin_max_defaut"));
        return rng.Next(1, soinMax + 1) + soinBonus;
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
