// sb-action: !refuser
// sb-subaction-id: 89d56aa8-d537-4619-8e08-dffbbdde130e
using System;
using System.IO;

public class CPHInline
{
    private const string DOSSIER_JOUEURS = @"C:\Users\Florian\pjt\Pointu-PJT\Donnees\joueurs";
    private const string CONFIG_ALLIES   = @"C:\Users\Florian\pjt\Pointu-PJT\Donnees\config_allies.json";
    private const string CONFIG_GLOBAL   = @"C:\Users\Florian\pjt\Pointu-PJT\Donnees\config_global.json";

    public bool Execute()
    {
        string nomJoueur     = args["user"].ToString();
        string cheminFichier = Path.Combine(DOSSIER_JOUEURS, nomJoueur.ToLower() + ".json");

        if (!File.Exists(cheminFichier))
        {
            CPH.SendMessage(nomJoueur + ", tape !rejoindre pour t'inscrire dans l'Antre de Pointu !");
            return true;
        }

        string json       = File.ReadAllText(cheminFichier);
        long   maintenant = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        // === Refus d'un duel entre joueurs (aucun cooldown pour le challenger) ===
        json = EnsureChamp(json, "duelDe",     "", true);
        json = EnsureChamp(json, "duelExpire", "0", false);
        string duelDe = LireValeur(json, "duelDe");
        if (duelDe != "" && duelDe != "0")
        {
            long dExp = long.Parse(LireValeur(json, "duelExpire"));
            json = ModifierValeur(json, "duelDe",     "",  true);
            json = ModifierValeur(json, "duelExpire", "0", false);
            File.WriteAllText(cheminFichier, json);

            string cheminA = Path.Combine(DOSSIER_JOUEURS, duelDe.ToLower() + ".json");
            if (File.Exists(cheminA))
            {
                string jsonA = File.ReadAllText(cheminA);
                jsonA = EnsureChamp(jsonA, "duelVers", "", true);
                if (LireValeur(jsonA, "duelVers").ToLower() == nomJoueur.ToLower())
                {
                    jsonA = ModifierValeur(jsonA, "duelVers", "", true);
                    File.WriteAllText(cheminA, jsonA);
                }
            }

            if (maintenant > dExp)
                CPH.SendMessage(nomJoueur + ", le défi de " + duelDe + " avait déjà expiré — c'est annulé.");
            else
                CPH.SendMessage(nomJoueur + " décline le duel de " + duelDe + ". Pas de combat cette fois !");
            return true;
        }

        bool   enRencontre = LireValeur(json, "enRencontre") == "true";
        string typeR       = LireValeur(json, "rencontreType");

        // Uniquement pour les offres alliées
        if (!enRencontre || typeR == "combat" || typeR == "")
        {
            CPH.SendMessage(nomJoueur + ", tu n'as aucune offre à refuser ! Pour un combat, utilise !combat, !discuter ou !fuir.");
            return true;
        }

        long expire = long.Parse(LireValeur(json, "rencontreExpire"));
        if (expire > 0 && maintenant > expire)
        {
            CPH.SendMessage(nomJoueur + ", cette offre a déjà expiré — le timer va remettre ta quête en route !");
            return true;
        }

        bool enQuete = LireValeur(json, "enQuete") == "true";

        // === Vieux Sage : risque de combat ===
        if (typeR == "vieux_sage")
        {
            string cfgA         = File.ReadAllText(CONFIG_ALLIES);
            int    chanceCombat = int.Parse(LireValeur(cfgA, "vieux_sage_chance_combat"));
            Random rng          = new Random();

            if (rng.Next(100) < chanceCombat)
            {
                // Le sage attaque → transformer en rencontre combat (sans changer quetePauseDebut)
                string cfgG     = File.ReadAllText(CONFIG_GLOBAL);
                long   expSecs  = long.Parse(LireValeur(cfgG, "rencontre_expire_secondes"));

                json = ModifierValeur(json, "rencontreType",   "combat",                            true);
                json = ModifierValeur(json, "enCombat",        "true",                              false);
                json = ModifierValeur(json, "ennemiNom",       "Vieux-Sage",                        true);
                json = ModifierValeur(json, "rencontreExpire", (maintenant + expSecs).ToString(),   false);

                File.WriteAllText(cheminFichier, json);
                CPH.SendMessage(nomJoueur + ", tu refuses le marché — le Vieux Sage se lève, furieux ! Tape !combat pour te battre, !discuter afin de t'en sortir ou !fuir pour t'échapper. (" + (expSecs / 60) + " min)");
                return true;
            }
            else
            {
                // Le sage disparaît paisiblement
                ResumeQuete(ref json, maintenant, enQuete, cheminFichier);
                CPH.SendMessage(nomJoueur + ", tu refuses le marché — le Vieux Sage profite de ton inattention pour disparaître sans laisser de trace..." + (enQuete ? " Ta quête reprend." : ""));
                return true;
            }
        }

        // === Autres types alliés : simple refus ===
        string msgRefus = "";
        if (typeR == "marchand_potion")
            msgRefus = nomJoueur + ", tu déclinas la Potion. Le marchand hausse les épaules et repart.";
        else if (typeR == "bonus_ram")
            msgRefus = nomJoueur + ", tu ignores la bourse et continues ta route.";
        else if (typeR == "alcove_chene")
            msgRefus = nomJoueur + ", tu passes devant l'alcôve sans t'arrêter.";
        else if (typeR == "marchand_classe")
            msgRefus = nomJoueur + ", tu déclinas l'offre du marchand.";
        else
            msgRefus = nomJoueur + ", offre refusée.";

        ResumeQuete(ref json, maintenant, enQuete, cheminFichier);
        CPH.SendMessage(msgRefus + (enQuete ? " Ta quête reprend." : ""));
        return true;
    }

    private void ResumeQuete(ref string json, long maintenant, bool enQuete, string chemin)
    {
        long pauseDebut = long.Parse(LireValeur(json, "quetePauseDebut"));
        long totalPause = long.Parse(LireValeur(json, "queteTotalPause"));
        if (pauseDebut > 0)
            json = ModifierValeur(json, "queteTotalPause", (totalPause + (maintenant - pauseDebut)).ToString(), false);
        json = ModifierValeur(json, "enRencontre",     "false", false);
        json = ModifierValeur(json, "rencontreType",   "",      true);
        json = ModifierValeur(json, "rencontreExpire", "0",     false);
        json = ModifierValeur(json, "quetePauseDebut", "0",     false);
        File.WriteAllText(chemin, json);
    }

    // Insère un champ s'il est absent (anciens profils)
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
