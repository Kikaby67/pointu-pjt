// sb-action: !duel
// sb-subaction-id: 7bc1dd09-56c7-4ea4-8bab-27013fcc896d
using System;
using System.IO;

public class CPHInline
{
    private const string DOSSIER_JOUEURS = @"C:\Users\Florian\pjt\Pointu-PJT\Donnees\joueurs";
    private const string CONFIG_GLOBAL   = @"C:\Users\Florian\pjt\Pointu-PJT\Donnees\config_global.json";

    public bool Execute()
    {
        string nomJoueur = args["user"].ToString();
        string cheminA   = Path.Combine(DOSSIER_JOUEURS, nomJoueur.ToLower() + ".json");

        if (!File.Exists(cheminA))
        {
            CPH.SendMessage(nomJoueur + ", tape !rejoindre pour t'inscrire dans l'Antre de Pointu !");
            return true;
        }

        string jsonA      = File.ReadAllText(cheminA);
        long   maintenant = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        if (LireValeur(jsonA, "classeChoisie") != "true")
        {
            CPH.SendMessage(nomJoueur + ", choisis d'abord une classe avec !choisirclasse avant de défier quelqu'un !");
            return true;
        }

        // === Cible ===
        string cibleBrut = args.ContainsKey("rawInput") ? args["rawInput"].ToString() : "";
        cibleBrut = cibleBrut.Trim().TrimStart('@').Trim();
        if (cibleBrut == "")
        {
            CPH.SendMessage(nomJoueur + ", indique qui tu veux défier : !duel @pseudo");
            return true;
        }
        if (cibleBrut.ToLower() == nomJoueur.ToLower())
        {
            CPH.SendMessage(nomJoueur + ", tu ne peux pas te défier toi-même !");
            return true;
        }

        string cheminB = Path.Combine(DOSSIER_JOUEURS, cibleBrut.ToLower() + ".json");
        if (!File.Exists(cheminB))
        {
            CPH.SendMessage(nomJoueur + ", « " + cibleBrut + " » n'a pas encore rejoint l'Antre — impossible de le défier.");
            return true;
        }

        string jsonB  = File.ReadAllText(cheminB);
        string cible  = LireValeurString(jsonB, "nomJoueur");
        if (cible == "") cible = cibleBrut;

        if (LireValeur(jsonB, "classeChoisie") != "true")
        {
            CPH.SendMessage(nomJoueur + ", " + cible + " n'a pas encore choisi de classe — il ne peut pas se battre en duel.");
            return true;
        }

        string cfgG = File.ReadAllText(CONFIG_GLOBAL);

        // === Cooldown du challenger (valide uniquement s'il a mené un duel accepté) ===
        jsonA = EnsureChamp(jsonA, "duelCooldownFin", "0", false);
        long cdFin = long.Parse(LireValeur(jsonA, "duelCooldownFin"));
        if (cdFin > maintenant)
        {
            int minutes = (int)Math.Ceiling((cdFin - maintenant) / 60.0);
            CPH.SendMessage(nomJoueur + ", tu as déjà mené un duel récemment — patiente encore " + minutes + " min avant d'en relancer un.");
            return true;
        }

        // === Défi sortant déjà en cours ? (anti-spam, nettoyé si expiré) ===
        jsonA = EnsureChamp(jsonA, "duelVers", "", true);
        string duelVers = LireValeur(jsonA, "duelVers");
        if (duelVers != "" && duelVers != "0")
        {
            string cheminPrec = Path.Combine(DOSSIER_JOUEURS, duelVers.ToLower() + ".json");
            bool encoreActif = false;
            if (File.Exists(cheminPrec))
            {
                string jsonPrec = File.ReadAllText(cheminPrec);
                long   expPrec  = long.Parse(EnsureChampReadLong(jsonPrec, "duelExpire"));
                if (LireValeur(jsonPrec, "duelDe").ToLower() == nomJoueur.ToLower() && expPrec > maintenant)
                    encoreActif = true;
            }
            if (encoreActif)
            {
                CPH.SendMessage(nomJoueur + ", tu as déjà un défi en attente contre " + duelVers + ". Attends sa réponse !");
                return true;
            }
            jsonA = ModifierValeur(jsonA, "duelVers", "", true); // ancien défi périmé → on nettoie
        }

        // === États « dans l'Antre » (les deux) ===
        if (LireValeur(jsonA, "enQuete") == "true" || LireValeur(jsonA, "enCombat") == "true")
        {
            CPH.SendMessage(nomJoueur + ", tu ne peux lancer un duel que dans l'Antre (hors quête et hors combat).");
            return true;
        }
        if (int.Parse(LireValeur(jsonA, "pvActuels")) <= 0)
        {
            CPH.SendMessage(nomJoueur + ", tu es à terre ! Repose-toi (!repos) avant de défier qui que ce soit.");
            return true;
        }
        if (LireValeur(jsonB, "enQuete") == "true" || LireValeur(jsonB, "enCombat") == "true")
        {
            CPH.SendMessage(nomJoueur + ", " + cible + " est occupé (en quête ou en combat) — impossible de le défier.");
            return true;
        }
        if (int.Parse(LireValeur(jsonB, "pvActuels")) <= 0)
        {
            CPH.SendMessage(nomJoueur + ", " + cible + " est à terre et ne peut pas se battre.");
            return true;
        }

        // === Cible déjà défiée par quelqu'un ? ===
        jsonB = EnsureChamp(jsonB, "duelDe",     "", true);
        jsonB = EnsureChamp(jsonB, "duelExpire", "0", false);
        string dejaDe   = LireValeur(jsonB, "duelDe");
        long   dejaExp  = long.Parse(LireValeur(jsonB, "duelExpire"));
        if (dejaDe != "" && dejaDe != "0" && dejaExp > maintenant)
        {
            CPH.SendMessage(nomJoueur + ", " + cible + " a déjà un duel en attente contre " + dejaDe + ". Réessaie plus tard !");
            return true;
        }

        // === Restriction de niveau : même niveau ou 1 au-dessus ===
        int niveauA = int.Parse(LireValeur(jsonA, "niveau"));
        int niveauB = int.Parse(LireValeur(jsonB, "niveau"));
        if (!(niveauB == niveauA || niveauB == niveauA + 1))
        {
            CPH.SendMessage(nomJoueur + " (niv " + niveauA + "), tu ne peux défier qu'un joueur de ton niveau ou juste au-dessus (niv " + niveauA + " ou " + (niveauA + 1) + "). " + cible + " est niveau " + niveauB + ".");
            return true;
        }

        // === Pose du défi ===
        long expire = maintenant + long.Parse(LireValeur(cfgG, "duel_expire_secondes"));

        jsonB = ModifierValeur(jsonB, "duelDe",     nomJoueur,          true);
        jsonB = ModifierValeur(jsonB, "duelExpire", expire.ToString(),  false);
        File.WriteAllText(cheminB, jsonB);

        jsonA = ModifierValeur(jsonA, "duelVers", cible, true);
        File.WriteAllText(cheminA, jsonA);

        long delai = long.Parse(LireValeur(cfgG, "duel_expire_secondes"));
        CPH.SendMessage("⚔️ " + nomJoueur + " (niv " + niveauA + ") défie " + cible + " (niv " + niveauB + ") en duel amical ! " + cible + ", tape !accepter pour relever le défi ou !refuser pour décliner (" + delai + "s).");
        return true;
    }

    // Lit un long en tolérant l'absence du champ (anciens profils)
    private string EnsureChampReadLong(string json, string cle)
    {
        string v = LireValeur(json, cle);
        return (v == "" ) ? "0" : v;
    }

    // Insère un champ s'il est absent (anciens profils)
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
        int posDebut    = json.IndexOf(marqueur);
        if (posDebut == -1) return json;
        posDebut       += marqueur.Length;
        int posFin      = json.IndexOfAny(new char[] { ',', '\n', '}' }, posDebut);
        string ancienne = json.Substring(posDebut, posFin - posDebut);
        string nouvelle = estTexte ? "\"" + val + "\"" : val;
        return json.Substring(0, posDebut) + nouvelle + json.Substring(posDebut + ancienne.Length);
    }
}
