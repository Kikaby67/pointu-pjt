// sb-action: !ouvrirdesert
// sb-subaction-id: bd3ff837-99b5-48a1-9472-3cac58345623
// sb-group: Broadcaster
// sb-trigger: command !ouvrirdesert
using System;
using System.IO;

public class CPHInline
{

    // CONFIGURATION — le seul endroit où tu touches les chemins

    private const string CONFIG_GLOBAL = @"C:\Users\Florian\pjt\Pointu-PJT\Donnees\config_global.json";
    private const string ETAT_GLOBAL   = @"C:\Users\Florian\pjt\Pointu-PJT\Donnees\etat_global.json";

    public bool Execute()
    {
        string nomJoueur = args["user"].ToString();
        string cfgG      = File.ReadAllText(CONFIG_GLOBAL);


        // Broadcaster uniquement — même garde-fou que !spawnboss

        string broadcaster = LireValeurString(cfgG, "broadcaster");
        if (nomJoueur.ToLower() != broadcaster.ToLower())
            return true;   // silence : la commande n'existe pas pour les autres


        // Filet de sécurité : la découverte du Désert passe normalement par le reward
        // Caravane (points de chaîne). Sans elle, l'artefact du Désert reste hors de
        // portée — et il est dans la chaîne principale. Cette commande la force.

        string etat = File.ReadAllText(ETAT_GLOBAL);
        etat = EnsureChamp(etat, "desertDecouvert", "false", false);

        if (LireValeur(etat, "desertDecouvert") == "true")
        {
            File.WriteAllText(ETAT_GLOBAL, etat);
            CPH.SendMessage("La route du Désert est déjà ouverte.");
            return true;
        }

        etat = ModifierValeur(etat, "desertDecouvert", "true", false);
        File.WriteAllText(ETAT_GLOBAL, etat);

        CPH.SendMessage("🐪 Une caravane franchit la dernière dune — la route du Désert s'ouvre pour tout Arbonet ! "
                      + "Les missives de Faîne arrivent dans l'Antre pour les aventuriers de niveau 6 et plus.");
        return true;
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
}
