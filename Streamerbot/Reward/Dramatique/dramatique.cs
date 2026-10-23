// sb-action: Moment Dramatique
// sb-subaction-id: a4ad8c85-1d36-42bb-ac1d-5bacb2e55691
// sb-group: Reward
using System;
using System.IO;

// Reward « 🎭 Moment dramatique » — hors jeu RPG, aucun profil lu ni écrit.
// La cam passe en noir et blanc pendant qu'une musique triste joue, puis
// retrouve ses couleurs à la fin de la musique.
// L'effet a SON PROPRE filtre OBS (créé au 1er passage s'il manque) : les
// filtres déjà réglés sur la cam ne sont jamais modifiés.
public class CPHInline
{
    private const string CONFIG_GLOBAL = @"C:\Users\Florian\pjt\Pointu-PJT\Donnees\config_global.json";
    private const string FICHIER_SON   = @"C:\Users\Florian\Desktop\Stream\Animation\Dramatique\triste.mp3";
    private const string NOM_FILTRE    = "Dramatique N&B";

    // Deux redemptions rapprochées : la seconde attend la fin de la première,
    // sinon la première éteindrait le filtre en plein milieu de la seconde.
    private static readonly object VERROU = new object();

    public bool Execute()
    {
        string user = args.ContainsKey("user") && args["user"] != null ? args["user"].ToString() : "Quelqu'un";

        string cfg    = File.Exists(CONFIG_GLOBAL) ? File.ReadAllText(CONFIG_GLOBAL) : "";
        string source = LireValeurString(cfg, "dramatique_source");
        if (source == "") source = "Cam 4k";
        int dureeSec  = LireConfigInt(cfg, "dramatique_duree_secondes", 20);
        int volumePct = LireConfigInt(cfg, "dramatique_volume_pct", 70);

        bool obsOk = CPH.ObsIsConnected(0);
        if (!obsOk) CPH.LogWarn("[Dramatique] OBS non connecté à Streamer.bot — musique seule.");

        lock (VERROU)
        {
            CPH.SendMessage("🎭 " + user + " plonge le stream dans le drame...");

            if (obsOk) PreparerFiltre(source);
            if (obsOk) ActiverFiltre(source, true);
            try
            {
                // Avec musique : l'effet dure exactement le morceau (lecture bloquante).
                // Sans musique : repli sur la durée de config.
                if (File.Exists(FICHIER_SON))
                    CPH.PlaySound(FICHIER_SON, volumePct / 100f, true);
                else
                {
                    CPH.LogWarn("[Dramatique] Musique introuvable : " + FICHIER_SON);
                    CPH.Wait(dureeSec * 1000);
                }
            }
            finally
            {
                // Même en cas d'erreur, la cam retrouve ses couleurs.
                if (obsOk) ActiverFiltre(source, false);
            }
        }
        return true;
    }

    // Crée le filtre (désactivé) s'il n'existe pas encore. S'il existe, OBS refuse la
    // création et on garde les réglages éventuellement retouchés à la main dans OBS.
    private void PreparerFiltre(string source)
    {
        CPH.ObsSendRaw("CreateSourceFilter",
            "{\"sourceName\":\"" + Echapper(source) + "\",\"filterName\":\"" + Echapper(NOM_FILTRE)
            + "\",\"filterKind\":\"color_filter_v2\""
            + ",\"filterSettings\":{\"saturation\":-1.0,\"contrast\":0.25,\"brightness\":-0.05}}", 0);
    }

    private void ActiverFiltre(string source, bool actif)
    {
        CPH.ObsSendRaw("SetSourceFilterEnabled",
            "{\"sourceName\":\"" + Echapper(source) + "\",\"filterName\":\"" + Echapper(NOM_FILTRE)
            + "\",\"filterEnabled\":" + (actif ? "true" : "false") + "}", 0);
    }

    // ---------------------------------------------------------------- helpers

    private int LireConfigInt(string cfg, string cle, int defaut)
    {
        int valeur;
        if (int.TryParse(LireValeur(cfg, cle), out valeur) && valeur > 0) return valeur;
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

    private string Echapper(string texte)
    {
        return texte.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }
}
