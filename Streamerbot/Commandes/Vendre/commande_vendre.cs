// sb-action: !vendre
// sb-subaction-id: b32dfe65-766f-41ee-86d4-91e0a6d3c87d
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
            CPH.SendMessage(nomJoueur + ", impossible de vendre en plein combat !");
            return true;
        }

        string rawInput = args["rawInput"].ToString().Trim();
        if (rawInput == "")
        {
            CPH.SendMessage(nomJoueur + ", précise l'item à vendre : !vendre [nom]");
            return true;
        }

        // Chercher dans le sac
        string   inventaire = LireValeurString(json, "inventaire");
        string[] items      = inventaire == "" ? new string[0] : inventaire.Split(',');

        string itemTrouve  = "";
        bool   dansLeSac   = false;
        foreach (string item in items)
        {
            if (string.Equals(item.Trim(), rawInput, StringComparison.OrdinalIgnoreCase))
            {
                itemTrouve = item.Trim();
                dansLeSac  = true;
                break;
            }
        }

        // Chercher dans les slots équipés
        string   champSlotTrouve = "";
        string[] slots           = { "armeEquipee", "armureEquipee", "accessoireEquipe" };
        if (!dansLeSac)
        {
            foreach (string slot in slots)
            {
                string equipe = LireValeur(json, slot);
                if (string.Equals(equipe, rawInput, StringComparison.OrdinalIgnoreCase))
                {
                    itemTrouve     = equipe;
                    champSlotTrouve = slot;
                    break;
                }
            }
        }

        if (itemTrouve == "")
        {
            CPH.SendMessage(nomJoueur + ", \"" + rawInput + "\" n'est ni dans ton sac ni équipé !");
            return true;
        }

        // Lire le prix dans la config
        string cfgItems  = File.ReadAllText(CONFIG_ITEMS);
        int    prixVente = int.Parse(LireValeur(cfgItems, itemTrouve + "_prixVente"));
        if (prixVente == 0) prixVente = 5; // valeur de secours

        // Retirer l'item
        if (dansLeSac)
        {
            string nouvInventaire = "";
            bool   dejaRetire     = false;
            foreach (string item in items)
            {
                if (!dejaRetire && item.Trim() == itemTrouve) { dejaRetire = true; continue; }
                if (nouvInventaire != "") nouvInventaire += ",";
                nouvInventaire += item.Trim();
            }
            json = ModifierValeurString(json, "inventaire", nouvInventaire);
        }
        else
        {
            json = ModifierValeur(json, champSlotTrouve, "", true);
        }

        // Ajouter les RAM
        json = AjouterValeur(json, "ram", prixVente);
        File.WriteAllText(cheminFichier, json);

        int ramActuels = int.Parse(LireValeur(json, "ram"));
        CPH.SendMessage(nomJoueur + " vend " + itemTrouve + " pour " + prixVente + " RAM. Total : " + ramActuels + " RAM.");

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

    private string AjouterValeur(string json, string cle, int montant)
    {
        int val = int.Parse(LireValeur(json, cle));
        return ModifierValeur(json, cle, (val + montant).ToString(), false);
    }
}
