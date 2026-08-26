// sb-action: !bonjour
// sb-subaction-id: 24be61f7-405a-4e73-81d9-d2e28357a2bf
using System;
using System.IO;
using System.Text;

public class CPHInline
{
    private const string FICHIER_OFFRE = @"C:\Users\Florian\pjt\Pointu-PJT\Donnees\offre_fragment.txt";

    public bool Execute()
    {
        // Je récupère le nom du viewer qui a déclenché la commande !bonjour
        string nomViewer = args["user"].ToString();

        // Pointu propose le fragment de sa carapace. C'est LE choix d'entrée dans Arbonet :
        // l'accepter (!rejoindre) ou le refuser (!nonmerci). Voir Lore/ZONES_ALLIES_ENNEMIS.md.
        string message = "Bonjour " + nomViewer + " ! Je suis Pointu, Gardien de l'Antre. "
                       + "Bienvenu(e) à Arbonet, un monde où la nature et la technologie coexistent en équilibre fragile. "
                       + "Tu ne comprends pas encore la langue des miens — ce morceau de ma carapace te le permettra, "
                       + "et t'aidera à te défendre. Tape !rejoindre pour l'accepter, ou !nonmerci pour le refuser.";

        // J'envoie le message de bienvenue dans le chat
        CPH.SendMessage(message);

        // On note que l'offre est ouverte pour ce viewer. !nonmerci ne traite un refus
        // QUE si cette trace est fraîche — sinon un !no lancé pour rire (alias partagé avec
        // la commande son) déclencherait un MP à quelqu'un qui n'a rien demandé.
        EnregistrerOffre(nomViewer);

        return true;
    }

    // Upsert : une ligne par pseudo, "pseudo|timestamp". Les lignes périmées sont purgées
    // au passage pour que le fichier ne gonfle pas indéfiniment.
    private void EnregistrerOffre(string nomViewer)
    {
        try
        {
            long maintenant = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            string cle      = nomViewer.ToLower();
            var    gardees  = new System.Text.StringBuilder();

            if (File.Exists(FICHIER_OFFRE))
            {
                foreach (string ligne in File.ReadAllLines(FICHIER_OFFRE))
                {
                    string[] p = ligne.Split('|');
                    if (p.Length != 2) continue;
                    if (p[0] == cle) continue;                        // remplacée plus bas
                    long ts;
                    if (!long.TryParse(p[1], out ts)) continue;
                    if (maintenant - ts > 86400) continue;            // purge au-delà de 24 h
                    gardees.AppendLine(ligne);
                }
            }
            gardees.AppendLine(cle + "|" + maintenant);

            File.WriteAllText(FICHIER_OFFRE, gardees.ToString(), new UTF8Encoding(false));
        }
        catch (Exception ex)
        {
            // Une offre non enregistrée ne doit jamais empêcher l'accueil de s'afficher.
            CPH.LogWarn("!bonjour — enregistrement de l'offre impossible : " + ex.Message);
        }
    }
}
