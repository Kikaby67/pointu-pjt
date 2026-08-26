// sb-action: Caravane du Desert
// sb-subaction-id: 3196cff0-e302-4855-ad15-1e0803e66ab5
// sb-group: Reward
using System;
using System.IO;

public class CPHInline
{

    // CONFIGURATION — le seul endroit où tu touches les chemins

    private const string DOSSIER_JOUEURS = @"C:\Users\Florian\pjt\Pointu-PJT\Donnees\joueurs";
    private const string CONFIG_GLOBAL   = @"C:\Users\Florian\pjt\Pointu-PJT\Donnees\config_global.json";
    private const string ETAT_GLOBAL     = @"C:\Users\Florian\pjt\Pointu-PJT\Donnees\etat_global.json";

    public bool Execute()
    {
        string nomJoueur     = args["user"].ToString();
        string cheminFichier = Path.Combine(DOSSIER_JOUEURS, nomJoueur.ToLower() + ".json");


        // Pas de profil = pas d'achat possible. On rembourse les points plutôt que
        // de laisser le viewer payer pour rien — sinon c'est un remboursement à la main, en live.

        if (!File.Exists(cheminFichier))
        {
            CPH.SendMessage(nomJoueur + ", tape !bonjour puis !rejoindre avant d'appeler la caravane — "
                          + "tes points te sont rendus.");
            RembourserPoints(nomJoueur);
            return true;
        }

        string json = File.ReadAllText(cheminFichier);

        if (LireValeur(json, "classeChoisie") != "true")
        {
            CPH.SendMessage(nomJoueur + ", choisis d'abord ta classe avec !choisirclasse — tes points te sont rendus.");
            RembourserPoints(nomJoueur);
            return true;
        }


        // 1) Le jeton d'achat — PERSONNEL. Seul celui qui a payé peut acheter.

        string cfgG    = File.ReadAllText(CONFIG_GLOBAL);
        int    parOuv  = 1;
        int    tmp;
        if (int.TryParse(LireValeur(cfgG, "caravane_achats_par_ouverture"), out tmp) && tmp > 0) parOuv = tmp;

        json = EnsureChamp(json, "caravaneAchats", "0", false);
        json = AjouterValeur(json, "caravaneAchats", parOuv);
        int total = int.Parse(LireValeur(json, "caravaneAchats"));


        // 2) La découverte du Désert — GLOBALE et définitive. Le premier qui paie
        //    ouvre la route pour tout le chat : les missives de Faîne apparaissent
        //    dans l'Antre pour les aventuriers qui ont le niveau.

        bool premiere = false;
        try
        {
            string etat = File.ReadAllText(ETAT_GLOBAL);
            etat = EnsureChamp(etat, "desertDecouvert", "false", false);
            if (LireValeur(etat, "desertDecouvert") != "true")
            {
                etat = ModifierValeur(etat, "desertDecouvert", "true", false);
                premiere = true;
            }
            File.WriteAllText(ETAT_GLOBAL, etat);
        }
        catch (Exception ex)
        {
            // Le jeton est déjà crédité : on ne bloque pas l'achat pour un souci d'état partagé.
            CPH.LogWarn("Caravane — ouverture du Désert impossible : " + ex.Message);
        }

        // Prime de pionnier : celui qui ouvre la route pour TOUT le chat repart avec
        // une reduction. Sans ca, le premier acheteur paie 1000 points pour offrir la
        // zone aux autres et ne garde qu'un jeton qu'il ne peut souvent pas encore utiliser.
        int reduc = 0;
        if (premiere)
        {
            int.TryParse(LireValeur(cfgG, "caravane_reduction_pionnier_pct"), out reduc);
            if (reduc > 0)
            {
                json = EnsureChamp(json, "reductionBoutique", "0", false);
                json = ModifierValeur(json, "reductionBoutique", reduc.ToString(), false);
            }
        }
        File.WriteAllText(cheminFichier, json);

        if (premiere)
            CPH.SendMessage("🐪 Une caravane franchit la dernière dune — " + nomJoueur
                          + " vient d'ouvrir la route du Désert pour tout Arbonet ! "
                          + "Les missives de Faîne arrivent dans l'Antre pour les aventuriers de niveau 6 et plus."
                          + (reduc > 0 ? " 🎖️ Faîne n'oublie pas qui a ouvert la route : -" + reduc + "% sur son prochain achat." : ""));

        CPH.SendMessage("🐿️ Faîne déballe ses réserves pour " + nomJoueur + " — "
                      + total + " achat(s) en attente. Catalogue épinglé dans #boutique sur Discord.");

        return true;
    }

    // Annule la redemption Twitch, ce qui rend les points au viewer.
    // Ne marche que si le reward est configuré en "skip request queue = false" côté Twitch.
    private void RembourserPoints(string nomJoueur)
    {
        try
        {
            string rewardId     = args.ContainsKey("rewardId")     && args["rewardId"]     != null ? args["rewardId"].ToString()     : "";
            string redemptionId = args.ContainsKey("redemptionId") && args["redemptionId"] != null ? args["redemptionId"].ToString() : "";
            if (rewardId == "" || redemptionId == "")
            {
                CPH.LogWarn("Caravane — remboursement impossible pour " + nomJoueur + " : identifiants de redemption absents.");
                return;
            }
            CPH.TwitchRedemptionCancel(rewardId, redemptionId);
        }
        catch (Exception ex)
        {
            CPH.LogWarn("Caravane — remboursement impossible pour " + nomJoueur + " : " + ex.Message);
        }
    }

    private string EnsureChamp(string json, string cle, string valeurDefaut, bool estTexte)
    {
        if (json.Contains("\"" + cle + "\"")) return json;
        int    pos = json.LastIndexOf('}');
        string val = estTexte ? "\"" + valeurDefaut + "\"" : valeurDefaut;
        return json.Substring(0, pos) + ",\n  \"" + cle + "\": " + val + "\n}";
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

    private string ModifierValeur(string json, string cle, string val, bool estTexte)
    {
        string marqueur = "\"" + cle + "\": ";
        int posDebut    = json.IndexOf(marqueur);
        if (posDebut == -1) return json;
        posDebut       += marqueur.Length;
        int posFin      = json.IndexOfAny(new char[] { ',', '\n', '}' }, posDebut);
        string ancienne = json.Substring(posDebut, posFin - posDebut);
        string nouvelle = estTexte ? "\"" + val + "\"" : val;
        return json.Substring(0, posDebut) + nouvelle + json.Substring(posDebut + ancienne.Length);
    }

    private string AjouterValeur(string json, string cle, int montant)
    {
        int val = int.Parse(LireValeur(json, cle));
        return ModifierValeur(json, cle, (val + montant).ToString(), false);
    }
}
