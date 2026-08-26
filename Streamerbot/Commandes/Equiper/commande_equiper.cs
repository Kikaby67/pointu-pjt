// sb-action: !equiper
// sb-subaction-id: b66a7c17-cb60-40ab-b3ff-ef8cd90f8afd
using System;
using System.IO;

public class CPHInline
{
    private const string DOSSIER_JOUEURS = @"C:\Users\Florian\pjt\Pointu-PJT\Donnees\joueurs";
    private const string CONFIG_ITEMS    = @"C:\Users\Florian\pjt\Pointu-PJT\Donnees\config_items.json";

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

        if (LireValeur(json, "enCombat") == "true")
        {
            CPH.SendMessage(nomJoueur + ", impossible d'équiper en plein combat !");
            return true;
        }

        string rawInput = args["rawInput"].ToString().Trim();
        if (rawInput == "")
        {
            CPH.SendMessage(nomJoueur + ", précise l'item à équiper : !equiper [nom]");
            return true;
        }

        // Chercher l'item dans le sac (insensible à la casse)
        string   inventaire = LireValeurString(json, "inventaire");
        string[] items      = inventaire == "" ? new string[0] : inventaire.Split(',');

        string itemTrouve = "";
        foreach (string item in items)
        {
            if (NormaliserNom(item) == NormaliserNom(rawInput))
            {
                itemTrouve = item.Trim();
                break;
            }
        }

        if (itemTrouve == "")
        {
            CPH.SendMessage(nomJoueur + ", \"" + rawInput + "\" n'est pas dans ton sac !");
            return true;
        }

        // Lire le slot depuis la config
        string cfgItems = File.ReadAllText(CONFIG_ITEMS);
        string slot     = LireValeur(cfgItems, itemTrouve + "_slot");

        if (slot == "consommable")
        {
            CPH.SendMessage(nomJoueur + ", " + itemTrouve + " est un consommable — utilise !utiliser [nom].");
            return true;
        }

        if (slot == "vente")
        {
            CPH.SendMessage(nomJoueur + ", " + itemTrouve + " ne s'équipe pas — vends-le avec !vendre [nom].");
            return true;
        }

        // Déterminer le champ joueur selon le slot
        string champSlot;
        if      (slot == "arme")       champSlot = "armeEquipee";
        else if (slot == "armure")     champSlot = "armureEquipee";
        else if (slot == "accessoire") champSlot = "accessoireEquipe";
        else
        {
            CPH.SendMessage(nomJoueur + ", slot inconnu pour \"" + itemTrouve + "\".");
            return true;
        }

        // Récupérer l'ancien item équipé dans ce slot
        string ancienEquipe = LireValeur(json, champSlot);
        if (ancienEquipe == "0") ancienEquipe = "";

        // Retirer l'item du sac
        string nouvInventaire = "";
        foreach (string item in items)
        {
            if (item.Trim() == itemTrouve) continue;
            if (nouvInventaire != "") nouvInventaire += ",";
            nouvInventaire += item.Trim();
        }

        // L'ancien item équipé retourne dans le sac
        if (ancienEquipe != "")
        {
            if (nouvInventaire != "") nouvInventaire += ",";
            nouvInventaire += ancienEquipe;
        }

        // Mettre à jour le JSON
        json = ModifierValeurString(json, "inventaire", nouvInventaire);
        json = ModifierValeur(json, champSlot, itemTrouve, true);
        File.WriteAllText(cheminFichier, json);

        if (ancienEquipe != "")
            CPH.SendMessage(nomJoueur + " équipe " + itemTrouve + " — " + ancienEquipe + " retourne dans le sac.");
        else
            CPH.SendMessage(nomJoueur + " équipe " + itemTrouve + " !");

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

    // Compare les noms d'item sans tenir compte de la casse NI des accents :
    // sur mobile, personne ne tape "Armure-renforcee" avec son accent.
    // Le nom EXACT du config reste celui qu'on stocke — on ne normalise que la comparaison.
    private string NormaliserNom(string s)
    {
        if (s == null) return "";
        string decompose = s.Trim().Normalize(System.Text.NormalizationForm.FormD);
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        foreach (char c in decompose)
        {
            if (System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c)
                != System.Globalization.UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        }
        return sb.ToString().Normalize(System.Text.NormalizationForm.FormC).ToLowerInvariant();
    }
}
