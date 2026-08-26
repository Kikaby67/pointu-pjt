// sb-action: !utiliser
// sb-subaction-id: 83c59e87-bea5-42b3-b02d-ffbf8cbc9b4f
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

        string rawInput = args["rawInput"].ToString().Trim();
        if (rawInput == "")
        {
            CPH.SendMessage(nomJoueur + ", précise l'item à utiliser : !utiliser [nom]");
            return true;
        }

        // Chercher l'item dans le sac
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

        // Vérifier que c'est bien un consommable
        string cfgItems = File.ReadAllText(CONFIG_ITEMS);
        string slot     = LireValeur(cfgItems, itemTrouve + "_slot");

        if (slot != "consommable")
        {
            CPH.SendMessage(nomJoueur + ", " + itemTrouve + " ne s'utilise pas — équipe-le avec !equiper ou vends-le avec !vendre.");
            return true;
        }

        // Lire les effets
        int pvSoin   = int.Parse(LireValeur(cfgItems, itemTrouve + "_pvSoin"));
        int manaSoin = int.Parse(LireValeur(cfgItems, itemTrouve + "_manaSoin"));

        int pvActuels   = int.Parse(LireValeur(json, "pvActuels"));
        int pvMax       = int.Parse(LireValeur(json, "pvMax"));
        int manaActuels = int.Parse(LireValeur(json, "manaActuels"));
        int manaMax     = int.Parse(LireValeur(json, "manaMax"));

        int pvRestores   = Math.Min(pvSoin,   pvMax   - pvActuels);
        int manaRestores = Math.Min(manaSoin, manaMax - manaActuels);

        // Appliquer les soins
        if (pvRestores > 0)   json = ModifierValeur(json, "pvActuels",   (pvActuels   + pvRestores).ToString(),   false);
        if (manaRestores > 0) json = ModifierValeur(json, "manaActuels", (manaActuels + manaRestores).ToString(), false);

        // Retirer l'item du sac (un seul exemplaire)
        string nouvInventaire = "";
        bool   dejaRetire     = false;
        foreach (string item in items)
        {
            if (!dejaRetire && item.Trim() == itemTrouve) { dejaRetire = true; continue; }
            if (nouvInventaire != "") nouvInventaire += ",";
            nouvInventaire += item.Trim();
        }
        json = ModifierValeurString(json, "inventaire", nouvInventaire);

        File.WriteAllText(cheminFichier, json);

        // Message de résultat
        string msgPV   = pvRestores   > 0 ? "+" + pvRestores   + " PV"             : "PV déjà au max";
        string msgMana = manaRestores > 0 ? " · +" + manaRestores + " mana" : (manaMax > 0 ? " · mana déjà au max" : "");
        CPH.SendMessage(nomJoueur + " utilise " + itemTrouve + " — " + msgPV + msgMana + " !");

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
