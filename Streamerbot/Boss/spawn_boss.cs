// sb-action: !spawnboss
// sb-subaction-id: b6bcdf59-ce9e-453e-aa3a-e0f9bd50ce7a
using System;
using System.IO;

// !spawnboss [nom]  — réservé au streamer. Ouvre une phase de recrutement (!arene).
public class CPHInline
{
    private const string CONFIG_GLOBAL  = @"C:\Users\Florian\pjt\Pointu-PJT\Donnees\config_global.json";
    private const string CONFIG_ENNEMIS = @"C:\Users\Florian\pjt\Pointu-PJT\Donnees\config_ennemis.json";
    private const string ETAT_GLOBAL    = @"C:\Users\Florian\pjt\Pointu-PJT\Donnees\etat_global.json";

    public bool Execute()
    {
        string nomJoueur   = args["user"].ToString();
        string cfgG        = File.ReadAllText(CONFIG_GLOBAL);
        string broadcaster = LireValeurString(cfgG, "broadcaster");

        if (nomJoueur.ToLower() != broadcaster.ToLower())
        {
            CPH.SendMessage(nomJoueur + ", tu ne peux pas invoquer un boss.");
            return true;
        }

        string etat = File.ReadAllText(ETAT_GLOBAL);
        if (LireValeur(etat, "bossActif") == "true")
        {
            CPH.SendMessage("⚔️ " + LireValeurString(etat, "bossNom") + " est déjà là (" + LireValeurString(etat, "bossPhase") + ") !");
            return true;
        }

        // Choix du boss : argument optionnel (!spawnboss <nom>), sinon aléatoire
        string[] boss = LireValeurString(cfgG, "rencontre_boss").Split(',');
        string demande = "";
        try { demande = args["rawInput"].ToString().Trim(); } catch { demande = ""; }

        string bossNom = "";
        if (demande != "")
        {
            foreach (string b in boss)
                if (b.Trim().ToLower() == demande.ToLower()) { bossNom = b.Trim(); break; }
            if (bossNom == "")
            {
                CPH.SendMessage("Boss inconnu : " + demande + ". Boss disponibles : " + LireValeurString(cfgG, "rencontre_boss"));
                return true;
            }
        }
        else
        {
            Random rng = new Random();
            bossNom = boss[rng.Next(boss.Length)].Trim();
        }

        long maintenant = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        int recrut = int.Parse(LireValeur(cfgG, "arene_recrutement_secondes"));

        // Phase recrutement : les PV sont calculés au début du combat (selon le nb de joueurs)
        etat = ModifierValeur(etat, "bossActif", "true", false);
        etat = ModifierValeurString(etat, "bossPhase", "recrutement");
        etat = ModifierValeurString(etat, "bossNom", bossNom);
        etat = ModifierValeur(etat, "bossPVMax", "0", false);
        etat = ModifierValeur(etat, "bossPVActuels", "0", false);
        etat = ModifierValeur(etat, "areneFin", (maintenant + recrut).ToString(), false);
        etat = ModifierValeurString(etat, "ordre", "");
        etat = ModifierValeur(etat, "tourIndex", "0", false);
        etat = ModifierValeur(etat, "tourDeadline", "0", false);
        etat = ModifierValeurString(etat, "participants", "");
        File.WriteAllText(ETAT_GLOBAL, etat);

        CPH.EnableTimer("ArenaCheck");
        CPH.SendMessage("🚨 BOSS À L'HORIZON ! " + bossNom + " approche d'Arbonet ! Tapez !arene pour rejoindre la bataille — vous avez " + (recrut / 60) + " minutes pour vous rassembler !");
        return true;
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

    private string ModifierValeur(string json, string cle, string val, bool estTexte)
    {
        string marqueur = "\"" + cle + "\": ";
        int posDebut = json.IndexOf(marqueur);
        if (posDebut == -1) return json;
        posDebut += marqueur.Length;
        int posFin = json.IndexOfAny(new char[] { ',', '\n', '}' }, posDebut);
        string ancienne = json.Substring(posDebut, posFin - posDebut);
        string nouvelle = estTexte ? "\"" + val + "\"" : val;
        return json.Substring(0, posDebut) + nouvelle + json.Substring(posDebut + ancienne.Length);
    }

    private string ModifierValeurString(string json, string cle, string val)
    {
        string marqueur = "\"" + cle + "\": \"";
        int posDebut    = json.IndexOf(marqueur);
        if (posDebut == -1) return json;
        posDebut       += marqueur.Length;
        int posFin      = json.IndexOf("\"", posDebut);
        if (posFin == -1) return json;
        return json.Substring(0, posDebut) + val + json.Substring(posFin);
    }
}
