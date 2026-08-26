// sb-action: !acheter
// sb-subaction-id: 63d0c115-412b-4b6e-b54a-4f9cd557cca8
using System;
using System.IO;

public class CPHInline
{
    private const string DOSSIER_JOUEURS = @"C:\Users\Florian\pjt\Pointu-PJT\Donnees\joueurs";
    private const string CONFIG_GLOBAL   = @"C:\Users\Florian\pjt\Pointu-PJT\Donnees\config_global.json";
    private const string CONFIG_QUETES   = @"C:\Users\Florian\pjt\Pointu-PJT\Donnees\config_quetes.json";
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
        string rawInput = args.ContainsKey("rawInput") && args["rawInput"] != null
                        ? args["rawInput"].ToString().Trim() : "";

        // DEUX CONTEXTES, arbitrés par la présence d'un argument :
        //   !acheter            → le marchand ambulant de la rencontre (Potion)
        //   !acheter [item]     → la boutique de Faîne (Désert), payée en RAM + 1 jeton de caravane
        // Sans cette règle, un joueur au marchand ne pourrait pas atteindre la boutique.
        if (rawInput == "") return AchatMarchand(nomJoueur, cheminFichier, json);
        return AchatBoutique(nomJoueur, cheminFichier, json, rawInput);
    }


    // ---------------------------------------------------------------- MARCHAND AMBULANT

    private bool AchatMarchand(string nomJoueur, string cheminFichier, string json)
    {
        long   maintenant = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        string typeR      = LireValeur(json, "rencontreType");

        if (typeR != "marchand_potion")
        {
            CPH.SendMessage(nomJoueur + ", aucun marchand n'est là. Chez Faîne, précise ce que tu veux : !acheter [objet]");
            return true;
        }

        long expire = long.Parse(LireValeur(json, "rencontreExpire"));
        if (expire > 0 && maintenant > expire)
        {
            CPH.SendMessage(nomJoueur + ", le marchand a déjà repris la route...");
            return true;
        }

        int prixPotion = int.Parse(LireValeur(json, "offreValeur"));
        int maxSac     = int.Parse(LireValeur(File.ReadAllText(CONFIG_GLOBAL), "max_sac"));
        int ram        = int.Parse(LireValeur(json, "ram"));
        string inv     = LireValeurString(json, "inventaire");
        int nbItems    = inv == "" ? 0 : inv.Split(',').Length;

        if (nbItems >= maxSac)
        {
            CPH.SendMessage(nomJoueur + ", ton sac est plein (" + nbItems + "/" + maxSac + ") — impossible d'acheter la Potion.");
            return true;
        }
        if (ram < prixPotion)
        {
            CPH.SendMessage(nomJoueur + ", il te faut " + prixPotion + " RAM pour la Potion (tu en as " + ram + ").");
            return true;
        }

        json = AjouterValeur(json, "ram", -prixPotion);
        json = ModifierValeurString(json, "inventaire", inv == "" ? "Potion" : inv + ",Potion");

        // Résoudre la rencontre et reprendre la quête
        long pauseDebut = long.Parse(LireValeur(json, "quetePauseDebut"));
        long totalPause = long.Parse(LireValeur(json, "queteTotalPause"));
        if (pauseDebut > 0)
            json = ModifierValeur(json, "queteTotalPause", (totalPause + (maintenant - pauseDebut)).ToString(), false);
        json = ModifierValeur(json, "enRencontre",     "false", false);
        json = ModifierValeur(json, "rencontreType",   "",      true);
        json = ModifierValeur(json, "rencontreExpire", "0",     false);
        json = ModifierValeur(json, "quetePauseDebut", "0",     false);

        File.WriteAllText(cheminFichier, json);
        bool enQuete = LireValeur(json, "enQuete") == "true";
        CPH.SendMessage(nomJoueur + " achète une Potion au marchand pour " + prixPotion + " RAM (reste "
            + (ram - prixPotion) + " RAM). Tape !utiliser Potion pour t'en servir."
            + (enQuete ? " Ta quête reprend !" : ""));
        return true;
    }


    // ---------------------------------------------------------------- BOUTIQUE DE FAÎNE

    private bool AchatBoutique(string nomJoueur, string cheminFichier, string json, string rawInput)
    {
        if (LireValeur(json, "enCombat") == "true")
        {
            CPH.SendMessage(nomJoueur + ", on ne fait pas ses courses en plein combat.");
            return true;
        }

        // Le jeton vient du reward Caravane du Désert : il donne le DROIT d'acheter.
        // La RAM paie l'objet. Les deux monnaies comptent.
        json = EnsureChamp(json, "caravaneAchats", "0", false);
        int jetons = int.Parse(LireValeur(json, "caravaneAchats"));
        if (jetons <= 0)
        {
            CPH.SendMessage("🐿️ " + nomJoueur + ", Faîne ne déballe rien sans caravane. "
                + "Échange la récompense 🐪 Caravane du Désert pour ouvrir ses réserves.");
            return true;
        }

        string cfgG      = File.ReadAllText(CONFIG_GLOBAL);
        string cfgItems  = File.ReadAllText(CONFIG_ITEMS);
        string poolNom   = LireValeurString(cfgG, "boutique_catalogue");
        string catalogue = LireValeurString(File.ReadAllText(CONFIG_QUETES), poolNom);

        if (catalogue == "")
        {
            CPH.SendMessage(nomJoueur + ", les réserves de Faîne sont vides — préviens le streamer.");
            CPH.LogWarn("!acheter — catalogue '" + poolNom + "' introuvable dans config_quetes.");
            return true;
        }

        // Recherche tolérante : ni la casse ni les accents ne bloquent l'achat.
        string trouve = "";
        foreach (string it in catalogue.Split(','))
            if (NormaliserNom(it) == NormaliserNom(rawInput)) { trouve = it.Trim(); break; }

        if (trouve == "")
        {
            CPH.SendMessage("🐿️ " + nomJoueur + ", Faîne ne vend pas ça. Le catalogue est épinglé dans #boutique sur Discord.");
            return true;
        }

        int prixBase = int.Parse(LireValeur(cfgItems, trouve + "_prixAchat"));
        int prix     = prixBase;

        // Reduction accordee au pionnier qui a ouvert le Desert (voir caravane_desert.cs).
        // Elle s'applique a UN achat, puis disparait.
        json = EnsureChamp(json, "reductionBoutique", "0", false);
        int reduc = 0; int.TryParse(LireValeur(json, "reductionBoutique"), out reduc);
        if (reduc > 0) prix = prixBase - (prixBase * reduc / 100);

        if (prixBase <= 0)
        {
            CPH.SendMessage(nomJoueur + ", " + trouve + " n'a pas de prix — préviens le streamer.");
            CPH.LogWarn("!acheter — " + trouve + " est au catalogue mais sans _prixAchat.");
            return true;
        }

        int    ram     = int.Parse(LireValeur(json, "ram"));
        int    maxSac  = int.Parse(LireValeur(cfgG, "max_sac"));
        string inv     = LireValeurString(json, "inventaire");
        int    nbItems = inv == "" ? 0 : inv.Split(',').Length;

        if (nbItems >= maxSac)
        {
            CPH.SendMessage("🐿️ " + nomJoueur + ", ton sac est plein (" + nbItems + "/" + maxSac
                + "). Faîne ne garde rien de côté — ton jeton reste valable.");
            return true;
        }
        if (ram < prix)
        {
            CPH.SendMessage("🐿️ Faîne repose " + trouve + " sur l'étagère. « " + prix + " RAM"
                + (reduc > 0 ? " (déjà -" + reduc + "%)" : "") + ". Tu en as " + ram
                + ". » Ton jeton reste valable.");
            return true;
        }

        // Tout est validé : on débite, on consomme le jeton, on livre.
        json = AjouterValeur(json, "ram", -prix);
        json = AjouterValeur(json, "caravaneAchats", -1);
        if (reduc > 0) json = ModifierValeur(json, "reductionBoutique", "0", false);   // usage unique
        json = ModifierValeurString(json, "inventaire", inv == "" ? trouve : inv + "," + trouve);
        File.WriteAllText(cheminFichier, json);

        int restants = jetons - 1;
        CPH.SendMessage("🐿️ Faîne compte, recompte, puis pousse " + trouve + " vers " + nomJoueur
            + " — " + prix + " RAM" + (reduc > 0 ? " au lieu de " + prixBase + " (-" + reduc + "%)" : "")
            + " (reste " + (ram - prix) + "). "
            + (restants > 0 ? restants + " achat(s) encore ouvert(s)." : "La caravane repart.")
            + " Tape !equiper " + trouve + " pour t'en servir.");
        return true;
    }


    // ---------------------------------------------------------------- utilitaires

    private string NormaliserNom(string s)
    {
        if (s == null) return "";
        string d = s.Trim().Normalize(System.Text.NormalizationForm.FormD);
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        foreach (char c in d)
            if (System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c)
                != System.Globalization.UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        return sb.ToString().Normalize(System.Text.NormalizationForm.FormC).ToLowerInvariant();
    }

    private string EnsureChamp(string json, string cle, string valeurDefaut, bool estTexte)
    {
        if (json.Contains("\"" + cle + "\"")) return json;
        int    pos = json.LastIndexOf('}');
        string val = estTexte ? "\"" + valeurDefaut + "\"" : valeurDefaut;
        return json.Substring(0, pos) + ",\n  \"" + cle + "\": " + val + "\n}";
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
