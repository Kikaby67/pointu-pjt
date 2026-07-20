// sb-action: Timeout Manuel
// sb-subaction-id: 1f257c41-b0d5-4a90-8eef-f05ee1096c72
// sb-group: Moderation
using System;
using System.IO;
using System.Text;

public class CPHInline
{
    private const string CONFIG_GLOBAL      = @"C:\Users\Florian\pjt\Pointu-PJT\Donnees\config_global.json";
    private const string DOSSIER_MODERATION = @"C:\Users\Florian\Desktop\Stream\Moderation";
    private const string FICHIER_CIBLE      = @"C:\Users\Florian\Desktop\Stream\Moderation\timeout_cible.txt";

    public bool Execute()
    {
        if (!File.Exists(FICHIER_CIBLE)) return true;

        string pseudo = File.ReadAllText(FICHIER_CIBLE).Trim().TrimStart('@').Trim();

        // Fichier vide = c'est notre propre effacement de fin qui a re-declenche le
        // watcher. On sort sans rien faire : c'est ce qui rend l'action idempotente.
        if (pseudo == "") return true;

        Vider();   // avant l'action : un echec ne doit pas laisser la cible armee

        if (!EstPseudoValide(pseudo))
        {
            CPH.LogWarn("[Timeout Manuel] Pseudo invalide, ignore : " + pseudo);
            Auditer(pseudo, 0, "pseudo_invalide");
            return true;
        }

        int duree = LireConfigInt("moderation_timeout_secondes", 300);

        bool ok = CPH.TwitchTimeoutUser(pseudo, duree, "Moderation manuelle", true);

        CPH.LogWarn("[Timeout Manuel] " + pseudo + " — " + duree + "s — "
                    + (ok ? "OK" : "ECHEC (droits mod ou pseudo inexistant ?)"));
        Auditer(pseudo, duree, ok ? "ok" : "echec");

        return true;   // action silencieuse : aucun message dans le chat
    }

    // ---------------------------------------------------------------- helpers

    private void Vider()
    {
        try { File.WriteAllText(FICHIER_CIBLE, ""); }
        catch (Exception ex) { CPH.LogWarn("[Timeout Manuel] Effacement impossible : " + ex.Message); }
    }

    // Le pseudo vient d'un fichier externe et sert d'appel API : on le contraint.
    private bool EstPseudoValide(string p)
    {
        if (p.Length == 0 || p.Length > 25) return false;
        foreach (char c in p)
            if (!char.IsLetterOrDigit(c) && c != '_') return false;
        return true;
    }

    // Journal des sanctions manuelles : c'est la verite terrain qui servira a
    // construire la liste de badwords (recoupement avec chat_log_*.jsonl).
    private void Auditer(string pseudo, int duree, string resultat)
    {
        try
        {
            Directory.CreateDirectory(DOSSIER_MODERATION);
            string ligne = "{\"ts\":\"" + DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
                         + "\",\"user\":\"" + Echapper(pseudo)
                         + "\",\"duree\":" + duree
                         + ",\"resultat\":\"" + resultat + "\",\"source\":\"touchportal\"}";
            // UTF8Encoding(false) : SANS BOM (cf. chat_logger.cs) — sinon la 1re ligne
            // du fichier est illisible pour un parseur JSON strict.
            File.AppendAllText(Path.Combine(DOSSIER_MODERATION,
                "timeouts_" + DateTime.Now.ToString("yyyy-MM") + ".jsonl"),
                ligne + Environment.NewLine, new UTF8Encoding(false));
        }
        catch (Exception ex) { CPH.LogWarn("[Timeout Manuel] Audit impossible : " + ex.Message); }
    }

    private int LireConfigInt(string cle, int defaut)
    {
        try
        {
            int valeur;
            if (int.TryParse(LireValeur(File.ReadAllText(CONFIG_GLOBAL), cle), out valeur) && valeur > 0)
                return valeur;
        }
        catch (Exception) { }
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

    private string Echapper(string texte)
    {
        StringBuilder sb = new StringBuilder(texte.Length + 8);
        foreach (char c in texte)
        {
            if (c == '"')       sb.Append("\\\"");
            else if (c == '\\') sb.Append("\\\\");
            else if (c < ' ')   sb.Append("\\u").Append(((int)c).ToString("x4"));
            else                sb.Append(c);
        }
        return sb.ToString();
    }
}
