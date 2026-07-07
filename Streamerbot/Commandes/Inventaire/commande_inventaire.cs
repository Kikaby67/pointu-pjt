// sb-action: !inventaire
// sb-subaction-id: 1e7832e9-27c4-4490-8199-14a018e2c557
using System;
using System.IO;

public class CPHInline
{
    private const string DOSSIER_JOUEURS = @"C:\Users\Florian\pjt\Pointu-PJT\Donnees\joueurs";

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

        string inventaire       = LireValeurString(json, "inventaire");
        string armeEquipee      = LireValeur(json, "armeEquipee");
        string armureEquipee    = LireValeur(json, "armureEquipee");
        string accessoireEquipe = LireValeur(json, "accessoireEquipe");

        // Sac
        int    nbItems      = 0;
        string affichageSac = "vide";
        if (inventaire != "")
        {
            string[] items = inventaire.Split(',');
            nbItems        = items.Length;
            affichageSac   = string.Join(" · ", items);
        }
        CPH.SendMessage(nomJoueur + " — Sac (" + nbItems + ") : " + affichageSac);

        // Équipement
        string arme       = armeEquipee      != "" && armeEquipee      != "0" ? armeEquipee      : "—";
        string armure     = armureEquipee    != "" && armureEquipee    != "0" ? armureEquipee    : "—";
        string accessoire = accessoireEquipe != "" && accessoireEquipe != "0" ? accessoireEquipe : "—";
        CPH.SendMessage(nomJoueur + " — Équipé : Arme " + arme + " · Armure " + armure + " · Accessoire " + accessoire);

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
}
