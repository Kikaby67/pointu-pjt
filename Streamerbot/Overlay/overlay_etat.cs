// sb-action: Overlay Etat Pointu
// sb-group: Overlay
// sb-subaction-id: <auto>
//
// Agrege l'etat du jeu dans UN seul fichier lu par l'overlay OBS.
// Trigger : un timer Streamer.bot (5 s conseille) — a creer a la main dans SB,
// comme "Timer XP Visionnage". Aucun trigger chat : cette action ne parle jamais.
//
// Pourquoi un agregat : l'overlay ne peut pas lister le dossier joueurs/ (pas de
// listing de repertoire en HTTP statique) et n'a aucune raison de re-parser les
// 50 Ko de config_quetes.json toutes les 5 secondes. Tout est resolu ici, la page
// reste bete : elle affiche ce qu'on lui donne.

using System;
using System.IO;

public class CPHInline
{
    private const string DOSSIER_JOUEURS = @"C:\Users\Florian\pjt\Pointu-PJT\Donnees\joueurs";
    private const string CONFIG_QUETES   = @"C:\Users\Florian\pjt\Pointu-PJT\Donnees\config_quetes.json";
    private const string CONFIG_GLOBAL   = @"C:\Users\Florian\pjt\Pointu-PJT\Donnees\config_global.json";
    private const string CONFIG_LEVEL    = @"C:\Users\Florian\pjt\Pointu-PJT\Donnees\config_level.json";
    private const string ETAT_GLOBAL     = @"C:\Users\Florian\pjt\Pointu-PJT\Donnees\etat_global.json";
    private const string SORTIE          = @"C:\Users\Florian\pjt\overlay\Donnees\Pointu-Etat.json";

    public bool Execute()
    {
        // Une panne d'overlay ne doit jamais remonter dans Streamer.bot.
        try { return ExecuteInner(); }
        catch (Exception e) { CPH.LogWarn("[OverlayEtat] " + e.Message); return true; }
    }

    private bool ExecuteInner()
    {
        string cfgG = File.ReadAllText(CONFIG_GLOBAL);
        string cfgQ = File.ReadAllText(CONFIG_QUETES);
        string cfgL = File.ReadAllText(CONFIG_LEVEL);
        long maintenant = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        // Fenetre d'activite de l'overlay. Par defaut on retombe sur celle du timer
        // d'XP : meme definition de "present dans le stream" partout. La cle dediee
        // permet d'afficher plus serre a l'ecran sans toucher aux gains d'XP.
        long seuilActif = ParseLong(LireValeur(cfgG, "overlay_actif_seuil_secondes"), 0);
        if (seuilActif <= 0) seuilActif = ParseLong(LireValeur(cfgG, "timer_activite_seuil_secondes"), 1800);

        string[] fichiers = Directory.GetFiles(DOSSIER_JOUEURS, "*.json");

        string[] joueurs = new string[fichiers.Length];  // JSON de sortie, un par joueur
        int[]    tris    = new int[fichiers.Length];     // cle de tri : experience
        int      nbJ     = 0;
        string   quetes  = "";
        int      nbQ     = 0;

        foreach (string chemin in fichiers)
        {
            // Un profil corrompu ou en cours d'ecriture ne doit pas vider tout l'overlay.
            try
            {
                string json = File.ReadAllText(chemin);
                string nom  = LireValeur(json, "nomJoueur");
                if (nom == "" || nom == "0") continue;

                int niveau   = ParseInt(LireValeur(json, "niveau"), 1);
                int xp       = ParseInt(LireValeur(json, "experience"), 0);
                int ram      = ParseInt(LireValeur(json, "ram"), 0);
                bool enQuete = LireValeur(json, "enQuete") == "true";

                // Seuls les joueurs presents restent a l'ecran : sinon le classement
                // fige les gros scores de joueurs absents depuis des semaines, et ceux
                // qui jouent ce soir ne se voient jamais. Meme test que Timer_XP_visionnage.
                // Un joueur parti en quete compte comme present : sa carte est deja
                // affichee au-dessus, l'enlever du classement serait incoherent.
                long derniereActivite = ParseLong(LireValeur(json, "derniereActivite"), 0);
                bool actif = derniereActivite > 0 && (maintenant - derniereActivite) <= seuilActif;
                if (!actif && !enQuete) continue;

                string zone  = ZoneCourante(niveau, cfgG);

                joueurs[nbJ] = "{\"nom\":\"" + Esc(nom) + "\""
                             + ",\"niveau\":" + niveau
                             + ",\"classe\":\"" + Esc(LireValeurString(json, "classe")) + "\""
                             + ",\"xpPct\":" + PourcentXp(cfgL, niveau, xp)
                             + ",\"ram\":" + ram
                             + ",\"zone\":\"" + Esc(zone) + "\""
                             + ",\"quetesFaites\":" + ParseInt(LireValeur(json, "quetesTerminees"), 0)
                             + ",\"enQuete\":" + (enQuete ? "true" : "false")
                             + "}";
                tris[nbJ] = xp;
                nbJ++;

                if (!enQuete) continue;

                // === Quete en cours : avancement reel ===
                // queteTicksRestants est le TOTAL requis (pose au depart, jamais decremente) ;
                // l'avancement se deduit du temps ecoule, pauses deduites. 1 tick = 5 min.
                int  ticks      = ParseInt(LireValeur(json, "queteTicksRestants"), 0);
                long debut      = ParseLong(LireValeur(json, "queteDernierTick"), maintenant);
                long totalPause = ParseLong(LireValeur(json, "queteTotalPause"), 0);
                long pauseDebut = ParseLong(LireValeur(json, "quetePauseDebut"), 0);
                if (ticks <= 0) continue;

                // La pause en cours (rencontre, combat, offre) n'est ajoutee au total
                // qu'a sa fermeture : il faut la retrancher ici, sinon la barre avance
                // pendant que le joueur est arrete.
                long ecoulees = (maintenant - debut) - totalPause;
                if (pauseDebut > 0) ecoulees -= (maintenant - pauseDebut);
                long requises = ticks * 5L * 60L;
                if (ecoulees < 0) ecoulees = 0;
                if (ecoulees > requises) ecoulees = requises;

                int pct      = (int)((ecoulees * 100L) / requises);
                int resteMin = (int)((requises - ecoulees + 59) / 60);

                string key = QueteKeyDeId(cfgQ, LireValeur(json, "queteId"));
                if (nbQ > 0) quetes += ",";
                quetes += "{\"joueur\":\"" + Esc(nom) + "\""
                        + ",\"nom\":\"" + Esc(LireValeurString(cfgQ, key + "_nom")) + "\""
                        + ",\"type\":\"" + Esc(LireValeurString(cfgQ, key + "_type")) + "\""
                        + ",\"zone\":\"" + Esc(LireValeurString(cfgQ, key + "_zone")) + "\""
                        + ",\"pct\":" + pct
                        + ",\"resteMin\":" + resteMin
                        + ",\"pause\":" + (pauseDebut > 0 ? "true" : "false")
                        + "}";
                nbQ++;
            }
            catch (Exception) { continue; }
        }

        TrierParXpDecroissant(joueurs, tris, nbJ);

        string listeJ = "";
        for (int i = 0; i < nbJ; i++)
        {
            if (i > 0) listeJ += ",";
            listeJ += joueurs[i];
        }

        bool desert = false;
        try { desert = LireValeur(File.ReadAllText(ETAT_GLOBAL), "desertDecouvert") == "true"; }
        catch (Exception) { }

        string sortie = "{\"ts\":" + maintenant
                      + ",\"desertDecouvert\":" + (desert ? "true" : "false")
                      + ",\"quetes\":[" + quetes + "]"
                      + ",\"joueurs\":[" + listeJ + "]}";

        // Ecriture en deux temps : l'overlay lit toutes les 5 s et tomberait
        // regulierement sur un fichier a moitie ecrit.
        Directory.CreateDirectory(Path.GetDirectoryName(SORTIE));
        string tmp = SORTIE + ".tmp";
        File.WriteAllText(tmp, sortie);
        File.Copy(tmp, SORTIE, true);
        File.Delete(tmp);

        return true;
    }

    // Zone la plus avancee que le niveau debloque. Le Desert est hors progression
    // (il s'ouvre par la caravane) — meme regle que quest_system.cs.
    private string ZoneCourante(int niveau, string cfgG)
    {
        string[] zones  = LireValeurString(cfgG, "zone_ordre").Split(',');
        string speciale = LireValeurString(cfgG, "quete_zone_speciale");
        string courante = zones.Length > 0 ? zones[0].Trim() : "";
        foreach (string z0 in zones)
        {
            string z = z0.Trim();
            if (z == speciale) continue;
            if (niveau >= NiveauZone(cfgG, z)) courante = z;
        }
        return courante;
    }

    private int NiveauZone(string cfgG, string zone)
    {
        int v;
        return int.TryParse(LireValeur(cfgG, "zone_" + SansAccent(zone).ToLowerInvariant() + "_niveau_min"), out v) ? v : 1;
    }

    // Avancement vers le niveau suivant, en %. Au niveau max, la barre est pleine.
    private int PourcentXp(string cfgL, int niveau, int xp)
    {
        int max = ParseInt(LireValeur(cfgL, "niveauMax"), 10);
        if (niveau >= max) return 100;
        int seuilBas  = ParseInt(LireValeur(cfgL, "niveau_" + niveau + "_xp"), 0);
        int seuilHaut = ParseInt(LireValeur(cfgL, "niveau_" + (niveau + 1) + "_xp"), 0);
        if (seuilHaut <= seuilBas) return 0;
        int pct = ((xp - seuilBas) * 100) / (seuilHaut - seuilBas);
        if (pct < 0) return 0;
        if (pct > 100) return 100;
        return pct;
    }

    // Tri par insertion : 17 joueurs, aucune raison de faire mieux.
    private void TrierParXpDecroissant(string[] joueurs, int[] tris, int n)
    {
        for (int i = 1; i < n; i++)
        {
            string j = joueurs[i];
            int    x = tris[i];
            int    k = i - 1;
            while (k >= 0 && tris[k] < x)
            {
                joueurs[k + 1] = joueurs[k];
                tris[k + 1]    = tris[k];
                k--;
            }
            joueurs[k + 1] = j;
            tris[k + 1]    = x;
        }
    }

    private string QueteKeyDeId(string cfg, string id)
    {
        for (int i = 1; i <= 99; i++)
        {
            string key = QueteKey(i);
            string qid = LireValeurString(cfg, key + "_id");
            if (qid == "") break;
            if (qid == id) return key;
        }
        return "";
    }

    private string QueteKey(int i)
    {
        if (i < 10)  return "quete00" + i;
        if (i < 100) return "quete0"  + i;
        return "quete" + i;
    }

    private string SansAccent(string s)
    {
        if (s == null) return "";
        string d = s.Trim().Normalize(System.Text.NormalizationForm.FormD);
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        foreach (char c in d)
            if (System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c)
                != System.Globalization.UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        return sb.ToString().Normalize(System.Text.NormalizationForm.FormC);
    }

    private int ParseInt(string s, int defaut)
    {
        int v;
        return int.TryParse(s, out v) ? v : defaut;
    }

    private long ParseLong(string s, long defaut)
    {
        long v;
        return long.TryParse(s, out v) ? v : defaut;
    }

    private string Esc(string s)
    {
        if (s == null) return "";
        return s.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }

    private string LireValeur(string json, string cle)
    {
        string marqueur = "\"" + cle + "\": ";
        int posDebut = json.IndexOf(marqueur);
        if (posDebut == -1) return "0";
        posDebut += marqueur.Length;
        int posFin = json.IndexOfAny(new char[] { ',', '\n', '}' }, posDebut);
        return json.Substring(posDebut, posFin - posDebut).Trim().Trim('"');
    }

    private string LireValeurString(string json, string cle)
    {
        string marqueur = "\"" + cle + "\": \"";
        int posDebut    = json.IndexOf(marqueur);
        if (posDebut == -1) return "";
        posDebut       += marqueur.Length;
        int posFin      = json.IndexOf("\"", posDebut);
        if (posFin == -1) return "";
        return json.Substring(posDebut, posFin - posDebut);
    }
}
