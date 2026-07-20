// sb-action: Chat Logger
// sb-subaction-id: 71d2e6b2-4393-4166-ad62-ff4b56d412f6
// sb-group: Moderation
// sb-trigger: chat
using System;
using System.IO;
using System.Text;

public class CPHInline
{
    // HORS REPO (volontairement) : contient des messages nominatifs de viewers.
    // Ne jamais versionner ni diffuser — usage strictement personnel de moderation.
    private const string DOSSIER_MODERATION = @"C:\Users\Florian\Desktop\Stream\Moderation";

    public bool Execute()
    {
        string message = Lire("message");
        if (message.Trim() == "") return true;   // rien a logger (evenement sans texte)

        string user   = Lire("user");
        if (user == "") user = Lire("userName");
        string userId = Lire("userId");

        // Fichier par mois : chat_log_2026-07.jsonl
        string fichier = Path.Combine(DOSSIER_MODERATION,
            "chat_log_" + DateTime.Now.ToString("yyyy-MM") + ".jsonl");

        StringBuilder ligne = new StringBuilder();
        ligne.Append("{\"ts\":\"").Append(DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"))
             .Append("\",\"user\":\"").Append(Echapper(user))
             .Append("\",\"userId\":\"").Append(Echapper(userId))
             .Append("\",\"role\":\"").Append(Role())
             .Append("\",\"sub\":").Append(Booleen("isSubscribed") ? "true" : "false")
             .Append(",\"msg\":\"").Append(Echapper(message))
             .Append("\"}");

        // UTF8Encoding(false) : SANS BOM. Encoding.UTF8 en poserait un a la creation
        // du fichier, ce qui rendrait la 1re ligne du mois illisible pour un parseur JSON.
        Directory.CreateDirectory(DOSSIER_MODERATION);
        File.AppendAllText(fichier, ligne.ToString() + Environment.NewLine, new UTF8Encoding(false));

        return true;   // action silencieuse : aucun message dans le chat
    }

    // ---------------------------------------------------------------- helpers

    private string Lire(string cle)
    {
        return args.ContainsKey(cle) && args[cle] != null ? args[cle].ToString() : "";
    }

    private bool Booleen(string cle)
    {
        string v = Lire(cle).ToLower();
        return v == "true" || v == "1";
    }

    // broadcaster > moderator > vip > subscriber > viewer
    private string Role()
    {
        if (Booleen("isBroadcaster")) return "broadcaster";
        if (Booleen("isModerator"))   return "mod";
        if (Booleen("isVip"))         return "vip";
        if (Booleen("isSubscribed"))  return "sub";

        // Repli : certaines versions de SB n'exposent qu'un entier "role".
        string r = Lire("role");
        if (r == "4") return "broadcaster";
        if (r == "3") return "mod";
        if (r == "2") return "vip";
        return "viewer";
    }

    // Echappement JSON manuel (pas de Newtonsoft dans Streamer.bot).
    // Sans ca, un viewer qui tape un guillemet ou un backslash casse le .jsonl.
    private string Echapper(string texte)
    {
        if (texte == "") return "";
        StringBuilder sb = new StringBuilder(texte.Length + 8);
        foreach (char c in texte)
        {
            switch (c)
            {
                case '"':  sb.Append("\\\""); break;
                case '\\': sb.Append("\\\\"); break;
                case '\b': sb.Append("\\b");  break;
                case '\f': sb.Append("\\f");  break;
                case '\n': sb.Append("\\n");  break;
                case '\r': sb.Append("\\r");  break;
                case '\t': sb.Append("\\t");  break;
                default:
                    // Caracteres de controle -> \u00XX (le reste passe tel quel, UTF-8)
                    if (c < ' ') sb.Append("\\u").Append(((int)c).ToString("x4"));
                    else         sb.Append(c);
                    break;
            }
        }
        return sb.ToString();
    }
}
