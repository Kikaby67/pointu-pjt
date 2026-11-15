// sb-action: Moment Dramatique
// sb-subaction-id: a4ad8c85-1d36-42bb-ac1d-5bacb2e55691
// sb-group: Reward
using System;
using System.IO;
using System.Globalization;

// Reward « 🎭 Moment dramatique » — hors jeu RPG, aucun profil lu ni écrit.
// La cam passe en noir et blanc pendant qu'une musique triste joue, puis
// retrouve ses couleurs à la fin de la musique.
// L'effet a SON PROPRE filtre OBS (créé au 1er passage s'il manque) : les
// filtres déjà réglés sur la cam ne sont jamais modifiés.
// La musique est jouée PAR OBS (source média cachée), sinon le stream ne l'entend pas.
public class CPHInline
{
    private const string CONFIG_GLOBAL = @"C:\Users\Florian\pjt\Pointu-PJT\Donnees\config_global.json";
    private const string FICHIER_SON   = @"C:\Users\Florian\Desktop\Stream\Animation\Dramatique\triste.mp3";
    private const string NOM_FILTRE    = "Dramatique N&B";
    private const string NOM_SOURCE_SON = "Dramatique_son";   // source média OBS, dans la scène des sons

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
        string sceneSon  = LireValeurString(cfg, "son_obs_scene");
        if (sceneSon == "") sceneSon = "Reward";
        int attenteMaxMs = LireConfigInt(cfg, "son_obs_attente_max_secondes", 300) * 1000;

        bool obsOk = CPH.ObsIsConnected(0);
        if (!obsOk) CPH.LogWarn("[Dramatique] OBS non connecté à Streamer.bot — musique seule.");

        lock (VERROU)
        {
            CPH.SendMessage("🎭 " + user + " plonge le stream dans le drame...");

            if (obsOk) PreparerFiltre(source);
            if (obsOk) ActiverFiltre(source, true);
            int idSon = -1;
            try
            {
                // Avec musique : l'effet dure exactement le morceau. Elle est jouée par OBS
                // pour que le stream l'entende ; à défaut, lecture locale par Streamer.bot.
                // Sans musique : repli sur la durée de config.
                if (File.Exists(FICHIER_SON))
                {
                    if (obsOk) idSon = DemarrerSonObs(sceneSon, NOM_SOURCE_SON, FICHIER_SON, volumePct);
                    if (idSon >= 0)
                        FinirSonObs(sceneSon, idSon, NOM_SOURCE_SON, attenteMaxMs);
                    else
                    {
                        if (obsOk) CPH.LogWarn("[Dramatique] OBS n'a pas joué la musique (la scène « " + sceneSon + " » est-elle dans la scène en direct ?) — son local seulement.");
                        CPH.PlaySound(FICHIER_SON, volumePct / 100f, true);
                    }
                }
                else
                {
                    CPH.LogWarn("[Dramatique] Musique introuvable : " + FICHIER_SON);
                    CPH.Wait(dureeSec * 1000);
                }
            }
            finally
            {
                // Même en cas d'erreur, la cam retrouve ses couleurs et la source est recachée.
                if (obsOk) ActiverFiltre(source, false);
                if (idSon >= 0) AfficherSource(sceneSon, idSon, false);
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

    // ---------------------------------------------------------------- son via OBS
    // CPH.PlaySound joue sur la sortie audio de Streamer.bot, que le stream ne capte pas :
    // les viewers n'entendaient rien. Le son passe donc par une source média OBS cachée dans
    // la scène imbriquée « Reward », comme Slap ou NUL : l'afficher la joue, et
    // « Monitoring et sortie » l'envoie au stream ET au casque. Créée au 1er passage si absente.
    // Renvoie l'id de l'élément de scène, ou -1 si OBS n'a rien joué.
    private int DemarrerSonObs(string scene, string nom, string fichier, int volumePct)
    {
        string sceneJson  = "\"sceneName\":\"" + Echapper(scene) + "\"";
        string sourceJson = "\"sourceName\":\"" + Echapper(nom) + "\"";
        string inputJson  = "\"inputName\":\"" + Echapper(nom) + "\"";
        string fichierObs = Echapper(fichier.Replace('\\', '/'));
        double id;

        if (!LireNombre(CPH.ObsSendRaw("GetSceneItemId", "{" + sceneJson + "," + sourceJson + "}", 0), "sceneItemId", 0, out id))
        {
            string rep = CPH.ObsSendRaw("CreateInput", "{" + sceneJson + "," + inputJson
                + ",\"inputKind\":\"ffmpeg_source\",\"inputSettings\":{\"local_file\":\"" + fichierObs + "\"}"
                + ",\"sceneItemEnabled\":false}", 0);
            // Le nom existe déjà ailleurs dans OBS : on l'ajoute simplement à la scène des sons
            if (!LireNombre(rep, "sceneItemId", 0, out id))
                rep = CPH.ObsSendRaw("CreateSceneItem", "{" + sceneJson + "," + sourceJson + ",\"sceneItemEnabled\":false}", 0);
            if (!LireNombre(rep, "sceneItemId", 0, out id)) return -1;
            CPH.ObsSendRaw("SetInputAudioMonitorType", "{" + inputJson + ",\"monitorType\":\"OBS_MONITORING_TYPE_MONITOR_AND_OUTPUT\"}", 0);
        }

        // Fichier et volume réappliqués à chaque passage : changer la config suffit
        CPH.ObsSendRaw("SetInputSettings", "{" + inputJson + ",\"inputSettings\":{\"local_file\":\"" + fichierObs + "\"}}", 0);
        CPH.ObsSendRaw("SetInputVolume", "{" + inputJson + ",\"inputVolumeMul\":"
            + (volumePct / 100.0).ToString(CultureInfo.InvariantCulture) + "}", 0);

        AfficherSource(scene, (int)id, false);   // repart de zéro si un passage précédent est resté affiché
        AfficherSource(scene, (int)id, true);

        // Si « Reward » n'est pas dans la scène en direct, OBS ne lance jamais la lecture
        for (int t = 0; t < 15; t++)
        {
            string etat = EtatMedia(nom);
            if (etat.Contains("PLAYING") || etat.Contains("ENDED")) return (int)id;
            CPH.Wait(100);
        }
        AfficherSource(scene, (int)id, false);
        return -1;
    }

    // Attend la fin du son (plafonné), puis recache la source pour le prochain passage.
    private void FinirSonObs(string scene, int id, string nom, int attenteMaxMs)
    {
        for (int ms = 0; ms < attenteMaxMs; ms += 200)
        {
            string etat = EtatMedia(nom);
            if (!etat.Contains("PLAYING") && !etat.Contains("OPENING") && !etat.Contains("BUFFERING")) break;
            CPH.Wait(200);
        }
        AfficherSource(scene, id, false);
    }

    private string EtatMedia(string nom)
    {
        return CPH.ObsSendRaw("GetMediaInputStatus", "{\"inputName\":\"" + Echapper(nom) + "\"}", 0) ?? "";
    }

    private void AfficherSource(string scene, int id, bool visible)
    {
        CPH.ObsSendRaw("SetSceneItemEnabled", "{\"sceneName\":\"" + Echapper(scene) + "\",\"sceneItemId\":" + id
            + ",\"sceneItemEnabled\":" + (visible ? "true" : "false") + "}", 0);
    }

    // ---------------------------------------------------------------- helpers

    // Lit un nombre dans une réponse obs-websocket, avec ou sans espace après les « : ».
    private bool LireNombre(string json, string cle, int depuis, out double valeur)
    {
        valeur = 0;
        if (string.IsNullOrEmpty(json)) return false;
        int pos = json.IndexOf("\"" + cle + "\"", depuis);
        if (pos == -1) return false;
        pos = json.IndexOf(':', pos);
        if (pos == -1) return false;
        pos++;
        while (pos < json.Length && json[pos] == ' ') pos++;
        int fin = pos;
        while (fin < json.Length && "-+.0123456789eE".IndexOf(json[fin]) != -1) fin++;
        return double.TryParse(json.Substring(pos, fin - pos), NumberStyles.Float,
                               CultureInfo.InvariantCulture, out valeur);
    }


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
