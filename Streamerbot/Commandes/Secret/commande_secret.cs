// sb-action: !racine
// sb-subaction-id: e3e10f3f-c1db-4faa-9235-414ec3e036ed
using System;
using System.IO;

public class CPHInline
{
    private const string DOSSIER_JOUEURS = @"C:\Users\Florian\pjt\Pointu-PJT\Donnees\joueurs";
    private const string CONFIG_ITEMS    = @"C:\Users\Florian\pjt\Pointu-PJT\Donnees\config_items.json";
    private const string FICHIER_SECRET  = @"C:\Users\Florian\pjt\Pointu-PJT\Donnees\secret_recu.txt";
    private const string CONFIG_GLOBAL   = @"C:\Users\Florian\pjt\Pointu-PJT\Donnees\config_global.json";
    private const string ITEM_SECRET     = "Ecaille-de-Pointu";
    // ── Trigger Streamer.bot : !racine (mot formé par Ecorce-R + A + C + I + N + E) ──

    public bool Execute()
    {
        string nomJoueur     = args["user"].ToString();
        string cheminFichier = Path.Combine(DOSSIER_JOUEURS, nomJoueur.ToLower() + ".json");

        if (!File.Exists(cheminFichier)) return true;

        string json = File.ReadAllText(cheminFichier);
        if (LireValeur(json, "classeChoisie") != "true") return true;

        // Vérifier si déjà reçu
        string[] dejaRecus = File.Exists(FICHIER_SECRET)
            ? File.ReadAllLines(FICHIER_SECRET)
            : new string[0];

        foreach (string nom in dejaRecus)
        {
            if (string.Equals(nom.Trim(), nomJoueur, StringComparison.OrdinalIgnoreCase))
            {
                CPH.SendMessage(nomJoueur + ", Pointu t'observe déjà...");
                return true;
            }
        }

        // Vérifier place dans le sac
        int      maxSac     = int.Parse(LireValeur(File.ReadAllText(CONFIG_GLOBAL), "max_sac"));
        string   inventaire = LireValeurString(json, "inventaire");
        string[] items      = inventaire == "" ? new string[0] : inventaire.Split(',');

        if (items.Length >= maxSac)
        {
            CPH.SendMessage(nomJoueur + ", ton sac est plein ! Libère une place d'abord.");
            return true;
        }

        // Ajouter l'item
        string nouvInventaire = inventaire == "" ? ITEM_SECRET : inventaire + "," + ITEM_SECRET;
        json = ModifierValeurString(json, "inventaire", nouvInventaire);
        File.WriteAllText(cheminFichier, json);

        // Marquer comme reçu
        File.AppendAllText(FICHIER_SECRET, nomJoueur + Environment.NewLine);

        CPH.SendMessage("✦ " + nomJoueur + " — Pointu t'a accordé un fragment de sa carapace millénaire. L'Ecaille-de-Pointu rejoint ton sac. ✦");

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
}
