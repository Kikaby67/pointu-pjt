// sb-action: Suggestion de Donation Goal
// sb-subaction-id: 3f6f6bb5-19dc-43c0-8e47-740b7a4f584f
using System;
using System.IO;

public class CPHInline
{
    private const string FICHIER_DONATIONGOAL = @"C:\Users\Florian\Desktop\Stream\Donation goal\DonationGoal.txt";

    public bool Execute()
    {
        string user = args.ContainsKey("user") && args["user"] != null
            ? args["user"].ToString() : "inconnu";

        // Texte saisi par le viewer lors de l'échange de la récompense.
        // Selon la version de Streamer.bot, il arrive dans rawInput ou message.
        string suggestion = "";
        if (args.ContainsKey("rawInput") && args["rawInput"] != null)
            suggestion = args["rawInput"].ToString();
        if (suggestion.Trim() == "" && args.ContainsKey("message") && args["message"] != null)
            suggestion = args["message"].ToString();

        // Une suggestion = une ligne : on aplatit les retours à la ligne éventuels.
        suggestion = suggestion.Replace("\r", " ").Replace("\n", " ").Trim();
        if (suggestion == "") return true; // aucun texte → rien à écrire

        // Append : chaque nouvelle suggestion s'ajoute à la suite (dossier + fichier créés s'ils manquent).
        Directory.CreateDirectory(Path.GetDirectoryName(FICHIER_DONATIONGOAL));
        string ligne = DateTime.Now.ToString("yyyy-MM-dd HH:mm") + " | " + user + " : " + suggestion + Environment.NewLine;
        File.AppendAllText(FICHIER_DONATIONGOAL, ligne);

        return true; // récompense silencieuse : aucun message dans le chat
    }
}
