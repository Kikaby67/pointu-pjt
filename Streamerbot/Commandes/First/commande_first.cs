// sb-action: !first
// sb-subaction-id: 532add11-7012-440e-87f4-7a86bd1dc357
// sb-group: Joueurs
// sb-trigger: command !first
using System;
using System.IO;

// !first — le premier viewer du stream est... le premier.
// Hors jeu RPG : aucun profil joueur lu ni écrit.
//
// Deux déclencheurs sur la MÊME action :
//   - Command Triggered → !first
//   - Channel Point Reward → 🥇 First (coût 1, à attacher à la main dans SB)
// args["user"] existe dans les deux cas.
//
// Le premier est retenu dans Donnees/first.txt ("timestamp|pseudo", git-ignoré : pseudo de tiers).
// Le titre expire après first_fenetre_secondes (config_global, défaut 12 h) → le prochain stream repart à zéro.

public class CPHInline
{
    private const string CONFIG_GLOBAL = @"C:\Users\Florian\pjt\Pointu-PJT\Donnees\config_global.json";
    private const string FICHIER_FIRST = @"C:\Users\Florian\pjt\Pointu-PJT\Donnees\first.txt";

    // Deux !first tapés à la même seconde ne doivent pas faire deux "premiers".
    private static readonly object VERROU = new object();

    public bool Execute()
    {
        string pseudo = args.ContainsKey("user") && args["user"] != null ? args["user"].ToString() : "";
        if (pseudo == "") return true;

        long maintenant = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        int  fenetre    = LireEntierConfig("first_fenetre_secondes", 43200);

        lock (VERROU)
        {
            string premier = "";
            long   quand   = 0;

            try
            {
                if (File.Exists(FICHIER_FIRST))
                {
                    string[] morceaux = File.ReadAllText(FICHIER_FIRST).Trim().Split('|');
                    if (morceaux.Length == 2 && long.TryParse(morceaux[0], out quand))
                        premier = morceaux[1];
                }
            }
            catch (Exception e) { CPH.LogWarn("[first] lecture impossible : " + e.Message); }

            bool titreEnCours = premier != "" && maintenant - quand < fenetre;

            if (!titreEnCours)
            {
                try { File.WriteAllText(FICHIER_FIRST, maintenant + "|" + pseudo); }
                catch (Exception e) { CPH.LogWarn("[first] écriture impossible : " + e.Message); }

                CPH.SendMessage("🐢 " + pseudo + " est premier·e ! Pointu, qui arrive toujours dernier, "
                              + "applaudit lentement… très lentement… il n'a toujours pas fini.");
            }
            else if (premier.ToLower() == pseudo.ToLower())
            {
                CPH.SendMessage("🐢 Oui oui " + pseudo + ", tu es premier·e. Pointu le sait, "
                              + "il applaudit encore.");
            }
            else
            {
                CPH.SendMessage("🐢 Trop tard, " + pseudo + "… même Pointu est arrivé avant toi. "
                              + "Enfin presque. " + premier + " est premier·e, et ça ne bouge plus.");
            }
        }
        return true;
    }

    private int LireEntierConfig(string cle, int defaut)
    {
        try
        {
            int valeur;
            if (int.TryParse(LireValeur(File.ReadAllText(CONFIG_GLOBAL), cle), out valeur) && valeur > 0)
                return valeur;
        }
        catch (Exception e) { CPH.LogWarn("[first] config illisible : " + e.Message); }
        return defaut;
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
}
