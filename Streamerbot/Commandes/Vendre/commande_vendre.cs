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
            CPH.SendMessage(nomJoueur + ", précise l'item à vendre : !vendre [nom] [quantité]");
            return true;
        }

        // Quantité optionnelle en dernier mot : "!vendre Potion 2" → item="Potion", qté=2
        int    quantite      = 1;
        string nomItem       = rawInput;
        int    dernierEspace = rawInput.LastIndexOf(' ');
        if (dernierEspace > 0)
        {
            string finTexte = rawInput.Substring(dernierEspace + 1).Trim();
            int    q;
            if (int.TryParse(finTexte, out q))
            {
                quantite = q < 1 ? 1 : q;
                nomItem  = rawInput.Substring(0, dernierEspace).Trim();
            }
        }

        // Chercher dans le sac (et compter les exemplaires)
        string   inventaire = LireValeurString(json, "inventaire");
        string[] items      = inventaire == "" ? new string[0] : inventaire.Split(',');

        string itemTrouve = "";
        int    nbEnStock  = 0;
        foreach (string item in items)
        {
            if (NormaliserNom(item) == NormaliserNom(nomItem))
            {
                if (itemTrouve == "") itemTrouve = item.Trim();
                nbEnStock++;
            }
        }
        bool dansLeSac = itemTrouve != "";

        // Chercher dans les slots équipés
        string   champSlotTrouve = "";
        string[] slots           = { "armeEquipee", "armureEquipee", "accessoireEquipe" };
        if (!dansLeSac)
        {
            foreach (string slot in slots)
            {
                string equipe = LireValeur(json, slot);
                if (NormaliserNom(equipe) == NormaliserNom(nomItem))
                {
                    itemTrouve      = equipe;
                    champSlotTrouve = slot;
                    break;
                }
            }
        }

        if (itemTrouve == "")
        {
            CPH.SendMessage(nomJoueur + ", \"" + nomItem + "\" n'est ni dans ton sac ni équipé !");
            return true;
        }

        // Lire le prix dans la config
        string cfgItems  = File.ReadAllText(CONFIG_ITEMS);
        int    prixVente = int.Parse(LireValeur(cfgItems, itemTrouve + "_prixVente"));
        if (prixVente == 0) prixVente = 5; // valeur de secours

        int    nbVendus;
        string noteManque = "";

        if (dansLeSac)
        {
            nbVendus = Math.Min(quantite, nbEnStock);
            if (quantite > nbEnStock)
                noteManque = " (tu n'en avais que " + nbEnStock + ")";

            // Retirer nbVendus exemplaires du sac
            string nouvInventaire = "";
            int    retires        = 0;
            foreach (string item in items)
            {
                if (retires < nbVendus && string.Equals(item.Trim(), itemTrouve, StringComparison.OrdinalIgnoreCase))
                {
                    retires++;
                    continue;
                }
                if (nouvInventaire != "") nouvInventaire += ",";
                nouvInventaire += item.Trim();
            }
            json = ModifierValeurString(json, "inventaire", nouvInventaire);
        }
        else
        {
            nbVendus = 1; // un slot équipé ne contient qu'un exemplaire
            if (quantite > 1)
                noteManque = " (objet équipé : 1 seul exemplaire vendu)";
            json = ModifierValeur(json, champSlotTrouve, "", true);
        }

        int gain = prixVente * nbVendus;
        json = AjouterValeur(json, "ram", gain);
        File.WriteAllText(cheminFichier, json);

        int    ramActuels = int.Parse(LireValeur(json, "ram"));
        string libelle    = nbVendus > 1 ? nbVendus + "× " + itemTrouve : itemTrouve;
        CPH.SendMessage(nomJoueur + " vend " + libelle + " pour " + gain + " RAM" + noteManque + ". Total : " + ramActuels + " RAM.");

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
