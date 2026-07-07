// sb-action: !repos
// sb-subaction-id: f840b5da-6911-4752-844a-a19aad38c8df
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

        if (LireValeur(json, "enCombat") == "true")
        {
            CPH.SendMessage(nomJoueur + ", tu ne peux pas te reposer en plein combat !");
            return true;
        }

        if (LireValeur(json, "enQuete") == "true")
        {
            CPH.SendMessage(nomJoueur + ", tu es en pleine quête — pas le temps de se reposer !");
            return true;
        }

        // Note : le repos fonctionne même à 0 PV — c'est ainsi qu'on se remet d'un effondrement (à terre).

        long maintenant  = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        long cooldownFin = long.Parse(LireValeur(json, "reposCooldownFin"));

        if (maintenant < cooldownFin)
        {
            long restant  = cooldownFin - maintenant;
            int  minutes  = (int)(restant / 60);
            int  secondes = (int)(restant % 60);
            CPH.SendMessage(nomJoueur + ", l'Antre de Pointu se recharge... encore " + minutes + "m " + secondes + "s avant de pouvoir te reposer.");
            return true;
        }

        int pvActuels   = int.Parse(LireValeur(json, "pvActuels"));
        int pvMax       = int.Parse(LireValeur(json, "pvMax"));
        int manaActuels = int.Parse(LireValeur(json, "manaActuels"));
        int manaMax     = int.Parse(LireValeur(json, "manaMax"));

        int pvRestores   = pvMax   - pvActuels;
        int manaRestores = manaMax - manaActuels;

        json = EnsureChamp(json, "reposCooldownFin", "0", false);
        json = ModifierValeur(json, "pvActuels",        pvMax.ToString(),                false);
        json = ModifierValeur(json, "manaActuels",      manaMax.ToString(),              false);
        int reposCooldown = int.Parse(LireValeur(File.ReadAllText(CONFIG_GLOBAL), "repos_cooldown_secondes"));
        json = ModifierValeur(json, "reposCooldownFin", (maintenant + reposCooldown).ToString(), false);

        File.WriteAllText(cheminFichier, json);

        string msgPV   = pvRestores   > 0 ? "+" + pvRestores   + " PV"   : "PV déjà au max";
        string msgMana = manaRestores > 0 ? " · +" + manaRestores + " mana" : "";
        int reposMins = reposCooldown / 60;
        CPH.SendMessage(nomJoueur + " se repose dans l'Antre de Pointu. La carapace ancienne pulse doucement... " + msgPV + msgMana + " ! (Prochain repos dans " + reposMins + " min)");

        return true;
    }

    // Insère un champ s'il est absent du JSON (pour les anciens profils)
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
