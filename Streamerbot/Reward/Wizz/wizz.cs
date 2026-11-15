// sb-action: Wizz
// sb-subaction-id: 843b0a54-9d1f-4917-b777-a7d481682673
// sb-group: Reward
using System;
using System.IO;
using System.Collections.Generic;
using System.Globalization;

// Reward « 📳 Wizz » (300 points) — hors jeu RPG, aucun profil lu ni écrit.
// Comme le wizz de MSN : un son + l'écran du stream qui tremble ~1 seconde.
// Le tremblement décale la position de chaque source de la scène OBS via
// obs-websocket (SetSceneItemTransform), puis remet TOUT à sa place d'origine.
// Le son est joué PAR OBS (source média cachée), sinon le stream ne l'entend pas.
public class CPHInline
{
    private const string CONFIG_GLOBAL = @"C:\Users\Florian\pjt\Pointu-PJT\Donnees\config_global.json";
    private const string FICHIER_SON   = @"C:\Users\Florian\Desktop\Stream\Animation\Wizz\wizz.mp3";
    private const string NOM_SOURCE_SON = "Wizz_son";   // source média OBS, dans la scène des sons

    // Deux wizz simultanés liraient comme « position d'origine » une position déjà
    // décalée par le premier, et la scène resterait de travers. On les sérialise.
    private static readonly object VERROU = new object();

    public bool Execute()
    {
        string user = args.ContainsKey("user") && args["user"] != null ? args["user"].ToString() : "Quelqu'un";

        string cfg     = File.Exists(CONFIG_GLOBAL) ? File.ReadAllText(CONFIG_GLOBAL) : "";
        int dureeMs    = LireConfigInt(cfg, "wizz_duree_ms", 900);
        int intervalle = LireConfigInt(cfg, "wizz_intervalle_ms", 30);
        int amplitude  = LireConfigInt(cfg, "wizz_amplitude_px", 18);
        int volumePct  = LireConfigInt(cfg, "wizz_volume_pct", 80);
        string scene   = LireValeurString(cfg, "wizz_scene");    // vide = scène en direct
        string source  = LireValeurString(cfg, "wizz_source");   // vide = toutes les sources

        string sceneSon  = LireValeurString(cfg, "son_obs_scene");
        if (sceneSon == "") sceneSon = "Reward";
        int attenteMaxMs = LireConfigInt(cfg, "son_obs_attente_max_secondes", 300) * 1000;

        CPH.SendMessage("📳 " + user + " a envoyé un Wizz !");

        if (!File.Exists(FICHIER_SON)) CPH.LogWarn("[Wizz] Son introuvable : " + FICHIER_SON);

        if (!CPH.ObsIsConnected(0))
        {
            CPH.LogWarn("[Wizz] OBS non connecté à Streamer.bot — pas de tremblement, son local seulement.");
            if (File.Exists(FICHIER_SON)) CPH.PlaySound(FICHIER_SON, volumePct / 100f, false);
            return true;
        }

        lock (VERROU)
        {
            // Le son démarre AVANT le tremblement, pour qu'ils arrivent ensemble à l'écran
            int idSon = File.Exists(FICHIER_SON) ? DemarrerSonObs(sceneSon, NOM_SOURCE_SON, FICHIER_SON, volumePct) : -1;
            if (idSon < 0 && File.Exists(FICHIER_SON))
            {
                CPH.LogWarn("[Wizz] OBS n'a pas joué le son (la scène « " + sceneSon + " » est-elle dans la scène en direct ?) — son local seulement.");
                CPH.PlaySound(FICHIER_SON, volumePct / 100f, false);
            }
            try
            {
                if (scene == "") scene = CPH.ObsGetCurrentScene(0);
                Secouer(scene, source, dureeMs, intervalle, amplitude);
            }
            finally
            {
                if (idSon >= 0) FinirSonObs(sceneSon, idSon, NOM_SOURCE_SON, attenteMaxMs);
            }
        }
        return true;
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

    private void Secouer(string scene, string source, int dureeMs, int intervalle, int amplitude)
    {
        string sceneJson = "\"sceneName\":\"" + Echapper(scene) + "\"";

        // 1. Les sources à secouer
        List<int> ids = new List<int>();
        if (source != "")
        {
            string rep = CPH.ObsSendRaw("GetSceneItemId",
                "{" + sceneJson + ",\"sourceName\":\"" + Echapper(source) + "\"}", 0);
            double id;
            if (LireNombre(rep, "sceneItemId", 0, out id)) ids.Add((int)id);
        }
        else
        {
            string rep = CPH.ObsSendRaw("GetSceneItemList", "{" + sceneJson + "}", 0) ?? "";
            int pos = 0;
            double id;
            while ((pos = rep.IndexOf("\"sceneItemId\"", pos)) != -1)
            {
                if (LireNombre(rep, "sceneItemId", pos, out id)) ids.Add((int)id);
                pos += 13;
            }
        }
        if (ids.Count == 0)
        {
            CPH.LogWarn("[Wizz] Aucune source trouvée dans la scène « " + scene + " »"
                        + (source != "" ? " (source « " + source + " »)" : "") + ".");
            return;
        }

        // 2. Les positions d'origine — on n'écrira jamais rien qu'on n'a pas su relire
        List<int>    items = new List<int>();
        List<double> origX = new List<double>();
        List<double> origY = new List<double>();
        foreach (int id in ids)
        {
            string t = CPH.ObsSendRaw("GetSceneItemTransform", "{" + sceneJson + ",\"sceneItemId\":" + id + "}", 0);
            double x, y;
            if (LireNombre(t, "positionX", 0, out x) && LireNombre(t, "positionY", 0, out y))
            {
                items.Add(id); origX.Add(x); origY.Add(y);
            }
        }

        // 3. Le tremblement : décalage aléatoire qui s'amortit, puis retour à l'origine
        Random rng = new Random();
        int nbFrames = Math.Max(1, dureeMs / Math.Max(1, intervalle));
        try
        {
            for (int f = 0; f < nbFrames; f++)
            {
                double amorti = 1.0 - (double)f / nbFrames;
                double dx = (rng.NextDouble() * 2 - 1) * amplitude * amorti;
                double dy = (rng.NextDouble() * 2 - 1) * amplitude * amorti;
                for (int i = 0; i < items.Count; i++)
                    Placer(sceneJson, items[i], origX[i] + dx, origY[i] + dy);
                CPH.Wait(intervalle);
            }
        }
        finally
        {
            // Même en cas d'erreur au milieu, la scène retrouve sa disposition.
            for (int i = 0; i < items.Count; i++)
                Placer(sceneJson, items[i], origX[i], origY[i]);
        }
    }

    private void Placer(string sceneJson, int id, double x, double y)
    {
        CPH.ObsSendRaw("SetSceneItemTransform", "{" + sceneJson + ",\"sceneItemId\":" + id
            + ",\"sceneItemTransform\":{\"positionX\":" + x.ToString(CultureInfo.InvariantCulture)
            + ",\"positionY\":" + y.ToString(CultureInfo.InvariantCulture) + "}}", 0);
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
