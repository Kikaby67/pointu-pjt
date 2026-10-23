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
public class CPHInline
{
    private const string CONFIG_GLOBAL = @"C:\Users\Florian\pjt\Pointu-PJT\Donnees\config_global.json";
    private const string FICHIER_SON   = @"C:\Users\Florian\Desktop\Stream\Animation\Wizz\wizz.mp3";

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

        CPH.SendMessage("📳 " + user + " a envoyé un Wizz !");

        if (File.Exists(FICHIER_SON))
            CPH.PlaySound(FICHIER_SON, volumePct / 100f, false);   // false : le son joue pendant le tremblement
        else
            CPH.LogWarn("[Wizz] Son introuvable : " + FICHIER_SON);

        if (!CPH.ObsIsConnected(0))
        {
            CPH.LogWarn("[Wizz] OBS non connecté à Streamer.bot — pas de tremblement.");
            return true;
        }

        lock (VERROU)
        {
            if (scene == "") scene = CPH.ObsGetCurrentScene(0);
            Secouer(scene, source, dureeMs, intervalle, amplitude);
        }
        return true;
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
