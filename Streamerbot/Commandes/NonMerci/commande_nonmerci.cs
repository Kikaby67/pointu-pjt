// sb-action: !nonmerci
// sb-group: Joueurs
// sb-subaction-id: a02be90f-4406-4cb6-8e85-0f64199042b7
// sb-trigger: command !nonmerci
using System;
using System.IO;
using System.Text;

public class CPHInline
{

    // CONFIGURATION — le seul endroit où tu touches les chemins

    private const string DOSSIER_JOUEURS = @"C:\Users\Florian\pjt\Pointu-PJT\Donnees\joueurs";
    private const string CONFIG_GLOBAL   = @"C:\Users\Florian\pjt\Pointu-PJT\Donnees\config_global.json";
    private const string FICHIER_OFFRE   = @"C:\Users\Florian\pjt\Pointu-PJT\Donnees\offre_fragment.txt";
    private const string FICHIER_REFUS   = @"C:\Users\Florian\pjt\Pointu-PJT\Donnees\refus_fragment.txt";

    public bool Execute()
    {
        // Nom du Viewer

        string nomJoueur     = args["user"].ToString();
        string cheminFichier = Path.Combine(DOSSIER_JOUEURS, nomJoueur.ToLower() + ".json");

        // Cette action partage ses mots-clés avec la commande son !no (!no/!non/!noo/!nooo).
        // On distingue donc le refus VOLONTAIRE (!nonmerci tapé exprès) du simple gag.
        string motTape = "";
        if (args.ContainsKey("command") && args["command"] != null)
            motTape = args["command"].ToString().ToLower();
        bool refusExplicite = motTape == "!nonmerci" || motTape == "";


        // Déjà inscrit — le fragment est définitif, on ne le rend pas (cf. ZONES_ALLIES_ENNEMIS.md)

        if (File.Exists(cheminFichier))
        {
            if (refusExplicite)
                CPH.SendMessage(nomJoueur + ", tu portes déjà un morceau de ma carapace. "
                              + "Ce lien-là ne se défait pas, Aventurier.");
            return true;   // sur un !no lancé pour rire : silence, le son suffit
        }


        // L'offre doit être OUVERTE : Pointu doit avoir proposé le fragment récemment (!bonjour).
        // Sans ce garde-fou, chaque !no du chat enverrait un MP à un inconnu.

        if (!OffreOuverte(nomJoueur))
        {
            if (refusExplicite)
                CPH.SendMessage(nomJoueur + ", personne ne t'a encore rien proposé. Tape !bonjour pour rencontrer Pointu.");
            return true;   // alias son hors contexte : on ne dit rien
        }


        // Le refus : expulsion d'Arbonet, en une ligne, publique

        CPH.SendMessage("🐢 Pointu hoche la tête sans insister. " + nomJoueur
                      + " refuse le fragment — Arbonet se referme, et le silence revient. "
                      + "(Quelque chose, ailleurs, a remarqué ce refus...)");

        ConsommerOffre(nomJoueur);   // un refus par offre, pas de MP en rafale


        // Bascule hors-fiction : proposition de devenir un boss, en message privé.
        // Le whisper Twitch peut échouer silencieusement (compte expéditeur non vérifié par
        // téléphone, ou destinataire qui n'a jamais écrit au compte) — d'où la trace fichier
        // ci-dessous, qui reste la source fiable pour recontacter le viewer à la main.

        string messagePrive = "Hector-Pierre Castor t'a vu refuser la carapace de Pointu. "
                            + "Il propose autre chose : rejoindre sa cause et devenir l'un des boss du jeu, "
                            + "avec ton propre personnage et ton propre lore. "
                            + "Si ça t'intéresse, réponds directement à ce message.";

        string statutWhisper = "ok";
        try
        {
            // bot: false → le MP part du compte BROADCASTER, pas du compte bot.
            // C'est voulu : le viewer doit pouvoir répondre à Florian directement.
            CPH.TwitchSendWhisper(nomJoueur, messagePrive, false);
        }
        catch (Exception ex)
        {
            statutWhisper = "echec";
            CPH.LogWarn("!nonmerci — whisper impossible vers " + nomJoueur + " : " + ex.Message);
        }


        // Journal des refus — c'est cette liste que tu relis pour envoyer les MP à la main.
        // UTF8Encoding(false) : SANS BOM (cf. chat_logger.cs).

        try
        {
            string ligne = DateTime.Now.ToString("yyyy-MM-dd HH:mm") + " | " + nomJoueur
                         + " | whisper:" + statutWhisper;
            File.AppendAllText(FICHIER_REFUS, ligne + Environment.NewLine, new UTF8Encoding(false));
        }
        catch (Exception ex)
        {
            CPH.LogWarn("!nonmerci — écriture du journal de refus impossible : " + ex.Message);
        }

        return true;
    }

    // L'offre est ouverte si !bonjour a laissé une trace de moins de
    // fragment_offre_expire_secondes (config_global.json).
    private bool OffreOuverte(string nomJoueur)
    {
        try
        {
            if (!File.Exists(FICHIER_OFFRE)) return false;

            int delai = 600;
            if (File.Exists(CONFIG_GLOBAL))
            {
                string v = LireValeur(File.ReadAllText(CONFIG_GLOBAL), "fragment_offre_expire_secondes");
                int tmp;
                if (int.TryParse(v, out tmp) && tmp > 0) delai = tmp;
            }

            string cle      = nomJoueur.ToLower();
            long maintenant = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            foreach (string ligne in File.ReadAllLines(FICHIER_OFFRE))
            {
                string[] p = ligne.Split('|');
                if (p.Length != 2 || p[0] != cle) continue;
                long ts;
                if (!long.TryParse(p[1], out ts)) continue;
                return (maintenant - ts) <= delai;
            }
        }
        catch (Exception ex)
        {
            CPH.LogWarn("!nonmerci — lecture de l'offre impossible : " + ex.Message);
        }
        return false;
    }

    // Retire la ligne du viewer : l'offre est consommée, un seul refus par !bonjour.
    private void ConsommerOffre(string nomJoueur)
    {
        try
        {
            if (!File.Exists(FICHIER_OFFRE)) return;
            string cle     = nomJoueur.ToLower();
            var    gardees = new StringBuilder();
            foreach (string ligne in File.ReadAllLines(FICHIER_OFFRE))
            {
                string[] p = ligne.Split('|');
                if (p.Length == 2 && p[0] == cle) continue;
                if (ligne.Trim() != "") gardees.AppendLine(ligne);
            }
            File.WriteAllText(FICHIER_OFFRE, gardees.ToString(), new UTF8Encoding(false));
        }
        catch (Exception ex)
        {
            CPH.LogWarn("!nonmerci — consommation de l'offre impossible : " + ex.Message);
        }
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
