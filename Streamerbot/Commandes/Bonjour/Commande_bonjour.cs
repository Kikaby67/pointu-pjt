// sb-action: !bonjour
// sb-subaction-id: 24be61f7-405a-4e73-81d9-d2e28357a2bf
using System;

public class CPHInline
{
    public bool Execute()
    {   
        // Je récupère le nom du viewer qui a déclenché la commande !bonjour
        string nomViewer = args["user"].ToString();
        // Je crée un message de bienvenue personnalisé pour le viewer
		string message = "Bonjour " + nomViewer + " ! Je suis Pointu, Gardien de l'Antre. Bienvenu(e) à Arbonet,Un monde où la nature et la technologie coexistent en équilibre fragile. Si tu accepte ce fragment de ma carapace, tu pourras m'aider à réaliser des quêtes. Tape !rejoindre pour commencer ton Aventure.";
		// J'envoie le message de bienvenue dans le chat
		CPH.SendMessage (message);

        return true;
    }
}