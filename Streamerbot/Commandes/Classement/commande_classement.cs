// sb-action: !classement
// sb-subaction-id: 2b99f514-6fc0-4be7-a657-967918f8bb85
using System;
using System.IO;

public class CPHInline
{
    private const string DOSSIER_JOUEURS = @"C:\Users\Florian\pjt\Pointu-PJT\Donnees\joueurs";
    private const string CONFIG_GLOBAL   = @"C:\Users\Florian\pjt\Pointu-PJT\Donnees\config_global.json";
    private const int    TOP_N           = 5;

    public bool Execute()
    {
        string cfgG        = File.ReadAllText(CONFIG_GLOBAL);
        string broadcaster = LireValeur(cfgG, "broadcaster");

        if (args["user"].ToString().ToLower() != broadcaster)
            return true;

        string[] fichiers = Directory.GetFiles(DOSSIER_JOUEURS, "*.json");

        // Tableaux de collecte (taille max = nombre de fichiers)
        string[] noms      = new string[fichiers.Length];
        int[]    xps       = new int[fichiers.Length];
        int[]    niveaux   = new int[fichiers.Length];
        int[]    victoires = new int[fichiers.Length];
        int[]    quetes    = new int[fichiers.Length];
        int      nb        = 0;

        foreach (string chemin in fichiers)
        {
            string json = File.ReadAllText(chemin);
            if (LireValeur(json, "classeChoisie") != "true") continue;

            noms[nb]      = LireValeur(json, "nomJoueur");
            xps[nb]       = int.Parse(LireValeur(json, "experience"));
            niveaux[nb]   = int.Parse(LireValeur(json, "niveau"));
            victoires[nb] = int.Parse(LireValeur(json, "combatsGagnes"));
            quetes[nb]    = int.Parse(LireValeur(json, "quetesTerminees"));
            nb++;
        }

        if (nb == 0)
        {
            CPH.SendMessage("Aucun aventurier actif dans Arbonet !");
            return true;
        }

        // Tri décroissant par XP (tri à bulles)
        for (int i = 0; i < nb - 1; i++)
        {
            for (int j = 0; j < nb - i - 1; j++)
            {
                if (xps[j] < xps[j + 1])
                {
                    int    ti = xps[j];       xps[j]       = xps[j + 1];       xps[j + 1]       = ti;
                           ti = niveaux[j];   niveaux[j]   = niveaux[j + 1];   niveaux[j + 1]   = ti;
                           ti = victoires[j]; victoires[j] = victoires[j + 1]; victoires[j + 1] = ti;
                           ti = quetes[j];    quetes[j]    = quetes[j + 1];    quetes[j + 1]    = ti;
                    string ts = noms[j];      noms[j]      = noms[j + 1];      noms[j + 1]      = ts;
                }
            }
        }

        int top = Math.Min(nb, TOP_N);

        string[] medailles = { "🥇", "🥈", "🥉", "4.", "5." };

        CPH.SendMessage("🏆 ══ CLASSEMENT ARBONET ══ 🏆  (Top " + top + " sur " + nb + " aventuriers)");

        for (int i = 0; i < top; i++)
        {
            CPH.SendMessage(
                medailles[i] + " " + noms[i] +
                " (Nv" + niveaux[i] + ")" +
                " — " + xps[i] + " XP" +
                " · " + victoires[i] + " victoire" + (victoires[i] > 1 ? "s" : "") +
                " · " + quetes[i] + " quête" + (quetes[i] > 1 ? "s" : "")
            );
        }

        return true;
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
}
