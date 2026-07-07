// sb-action: !abandon
// sb-subaction-id: 39e2f7b0-7a9a-46e0-94ab-462cebf90a24
using System;
using System.IO;

public class CPHInline
{
    private const string DOSSIER_JOUEURS = @"C:\Users\Florian\pjt\Pointu-PJT\Donnees\joueurs";
    private const string CONFIG_GLOBAL   = @"C:\Users\Florian\pjt\Pointu-PJT\Donnees\config_global.json";

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

        if (LireValeur(json, "enQuete") != "true")
        {
            CPH.SendMessage(nomJoueur + ", tu n'es pas en quête !");
            return true;
        }

        if (LireValeur(json, "enCombat") == "true")
        {
            CPH.SendMessage(nomJoueur + ", impossible d'abandonner en pleine rencontre — résous-la d'abord (!combat, !discuter ou !fuir) !");
            return true;
        }

        string queteId = LireValeur(json, "queteId");

        int    cooldown  = int.Parse(LireValeur(File.ReadAllText(CONFIG_GLOBAL), "quete_cooldown_abandon_secondes"));
        long   maintenant = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        // Réinitialiser tous les champs de quête
        json = ModifierValeur(json, "enQuete",               "false", false);
        json = ModifierValeur(json, "queteId",               "",      true);
        json = ModifierValeur(json, "queteTicksRestants",    "0",     false);
        json = ModifierValeur(json, "queteDernierTick",      "0",     false);
        json = ModifierValeur(json, "enRencontre",           "false", false);
        json = ModifierValeur(json, "rencontreType",         "",      true);
        json = ModifierValeur(json, "rencontreExpire",       "0",     false);
        json = ModifierValeur(json, "compagnonActif",        "",      true);
        json = ModifierValeur(json, "quetePauseDebut",       "0",     false);
        json = ModifierValeur(json, "queteTotalPause",       "0",     false);
        json = ModifierValeur(json, "dernierCheckRencontre", "0",     false);
        json = ModifierValeur(json, "queteEventsUsed",       "0",     false);
        json = ModifierValeur(json, "offreEnAttente",        "",      true);
        json = ModifierValeur(json, "offreValeur",           "0",     false);
        json = ModifierValeur(json, "offreExpire",           "0",     false);
        json = ModifierValeur(json, "queteCooldownFin",      (maintenant + cooldown).ToString(), false);

        File.WriteAllText(cheminFichier, json);

        int cooldownMin = cooldown / 60;
        CPH.SendMessage(nomJoueur + " abandonne la quête " + queteId + ". Lâche ! Arbonet te punit : " + cooldownMin + " min de cooldown avant de repartir.");
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
