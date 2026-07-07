// sb-action: !choisirclasse
// sb-subaction-id: a5149da3-43f8-4701-a962-61414b3a3082
using System;
using System.IO;

public class CPHInline
{
    private const string DOSSIER_JOUEURS = @"C:\Users\Florian\pjt\Pointu-PJT\Donnees\joueurs";
    private const string CONFIG_CLASSES  = @"C:\Users\Florian\pjt\Pointu-PJT\Donnees\config_classes.json";
    private const string CONFIG_GLOBAL   = @"C:\Users\Florian\pjt\Pointu-PJT\Donnees\config_global.json";
    private const string CONFIG_LEVEL    = @"C:\Users\Florian\pjt\Pointu-PJT\Donnees\config_level.json";

    public bool Execute()
    {
        string nomJoueur     = args["user"].ToString();
        string rawInput      = args["rawInput"].ToString().Trim().ToLower();
        string cheminFichier = Path.Combine(DOSSIER_JOUEURS, nomJoueur.ToLower() + ".json");

        if (!File.Exists(cheminFichier))
        {
            CPH.SendMessage(nomJoueur + ", tape !rejoindre d'abord !");
            return true;
        }

        string json            = File.ReadAllText(cheminFichier);
        bool classeDejaChoisie = LireValeur(json, "classeChoisie") == "true";
        bool viaMarchand       = LireValeur(json, "rencontreType") == "marchand_classe";

        // Bloquer si classe déjà choisie, SAUF si c'est via le Marchand de Classe
        if (classeDejaChoisie && !viaMarchand)
        {
            CPH.SendMessage(nomJoueur + ", tu as déjà une classe ! Rencontre un Marchand de Classe en quête pour en changer.");
            return true;
        }

        // Normalisation du nom de classe
        string classeNom;
        switch (rawInput)
        {
            case "hexadécimeur":
            case "hexadecimeur":    classeNom = "Hexadécimeur";    break;
            case "cryptolame":      classeNom = "Cryptolame";      break;
            case "hackmancien":     classeNom = "Hackmancien";     break;
            case "firewaller":      classeNom = "Firewaller";      break;
            case "algorythmancien": classeNom = "Algorythmancien"; break;
            default:
                CPH.SendMessage(nomJoueur + ", classe inconnue ! Choisis parmi : Hexadécimeur · Cryptolame · Hackmancien · Firewaller · Algorythmancien");
                return true;
        }

        // Via marchand : ne peut pas choisir la même classe
        if (viaMarchand && LireValeur(json, "classe") == classeNom)
        {
            CPH.SendMessage(nomJoueur + ", tu as déjà cette classe ! Choisis une autre : Hexadécimeur · Cryptolame · Hackmancien · Firewaller · Algorythmancien");
            return true;
        }

        // Chargement des stats de la nouvelle classe
        string cfg       = File.ReadAllText(CONFIG_CLASSES);
        int pvBase       = int.Parse(LireValeur(cfg, classeNom + "_pvBase"));
        int caBase       = int.Parse(LireValeur(cfg, classeNom + "_caBase"));
        int manaBase     = int.Parse(LireValeur(cfg, classeNom + "_manaBase"));
        int charismeBase = int.Parse(LireValeur(cfg, classeNom + "_charisme"));
        int agiliteBase  = int.Parse(LireValeur(cfg, classeNom + "_agilite"));
        string typeArme  = LireValeur(cfg, classeNom + "_typeArme");

        // Jets de dés (faces depuis config_global)
        string cfgG      = File.ReadAllText(CONFIG_GLOBAL);
        Random rng       = new Random();
        int jetPV        = rng.Next(1, int.Parse(LireValeur(cfgG, "creation_pv_de"))  + 1);
        int jetCA        = rng.Next(1, int.Parse(LireValeur(cfgG, "creation_ca_de"))  + 1);
        int bonusAttaque = rng.Next(1, int.Parse(LireValeur(cfgG, "creation_atq_de")) + 1);

        int pvFinal  = pvBase + jetPV;
        int caFinale = caBase + jetCA;

        // Si changement de classe via marchand : ajouter les bonus de niveaux déjà gagnés
        if (viaMarchand)
        {
            string cfgLvl = File.ReadAllText(CONFIG_LEVEL);
            int niveau    = int.Parse(LireValeur(json, "niveau"));
            for (int lv = 2; lv <= niveau; lv++)
            {
                pvFinal  += int.Parse(LireValeur(cfgLvl, "niveau_" + lv + "_pvBonus"));
                caFinale += int.Parse(LireValeur(cfgLvl, "niveau_" + lv + "_caBonus"));
            }
        }

        // Sauvegarde des champs de base
        json = EnsureChamp(json, "agilite",        "0", false);
        json = EnsureChamp(json, "compagnonActif", "",  true);
        json = EnsureChamp(json, "rencontreExpire", "0", false);

        // Si l'arme équipée n'est pas l'arme de classe actuelle (= arme custom), la remettre dans le sac
        if (viaMarchand)
        {
            string ancienneClasse     = LireValeur(json, "classe");
            string ancienneArmeClasse = LireValeur(cfg, ancienneClasse + "_typeArme");
            string armeActuelle       = LireValeur(json, "armeEquipee");
            if (armeActuelle != "" && armeActuelle != "0" && armeActuelle != ancienneArmeClasse)
            {
                string inv    = LireValeurString(json, "inventaire");
                int nbItems   = inv == "" ? 0 : inv.Split(',').Length;
                int maxSac    = int.Parse(LireValeur(cfgG, "max_sac"));
                if (nbItems < maxSac)
                {
                    string nouvInv = inv == "" ? armeActuelle : inv + "," + armeActuelle;
                    json = ModifierValeurString(json, "inventaire", nouvInv);
                }
                // sac plein → arme perdue (même comportement que le reste du jeu)
            }

            // Reset sous-classe (liée à l'ancienne classe)
            json = ModifierValeur(json, "sousClasse",       "",      true);
            json = ModifierValeur(json, "sousClasseChoisie", "false", false);
        }

        // Stats de la nouvelle classe
        json = ModifierValeur(json, "classeChoisie",  "true",                  false);
        json = ModifierValeur(json, "classe",          classeNom,              true);
        json = ModifierValeur(json, "typeArme",        typeArme,               true);
        json = ModifierValeur(json, "armeEquipee",     typeArme,               true);
        int pvActuelsNouveau = viaMarchand
            ? Math.Min(int.Parse(LireValeur(json, "pvActuels")), pvFinal) // changement : garde les PV actuels, plafonnés
            : pvFinal;                                                      // première création : PV pleins

        json = ModifierValeur(json, "pvMax",     pvFinal.ToString(),          false);
        json = ModifierValeur(json, "pvActuels", pvActuelsNouveau.ToString(), false);
        json = ModifierValeur(json, "classeArmure",    caFinale.ToString(),    false);
        json = ModifierValeur(json, "bonusAttaque",    bonusAttaque.ToString(), false);
        json = ModifierValeur(json, "manaMax",         manaBase.ToString(),    false);
        json = ModifierValeur(json, "manaActuels",     manaBase.ToString(),    false);
        json = ModifierValeur(json, "charisme",        charismeBase.ToString(), false);
        json = ModifierValeur(json, "agilite",         agiliteBase.ToString(), false);

        // Si via marchand : reprendre la quête
        if (viaMarchand)
        {
            long maintenant = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            long pauseDebut = long.Parse(LireValeur(json, "quetePauseDebut"));
            long totalPause = long.Parse(LireValeur(json, "queteTotalPause"));
            if (pauseDebut > 0)
                json = ModifierValeur(json, "queteTotalPause", (totalPause + (maintenant - pauseDebut)).ToString(), false);
            json = ModifierValeur(json, "enRencontre",     "false", false);
            json = ModifierValeur(json, "rencontreType",   "",      true);
            json = ModifierValeur(json, "rencontreExpire", "0",     false);
            json = ModifierValeur(json, "quetePauseDebut", "0",     false);
        }

        File.WriteAllText(cheminFichier, json);

        string signCA  = jetCA        >= 0 ? "+" + jetCA        : jetCA.ToString();
        string signAtq = bonusAttaque >= 0 ? "+" + bonusAttaque : bonusAttaque.ToString();

        if (viaMarchand)
            CPH.SendMessage(nomJoueur + " change de classe pour " + classeNom + " ! PV : " + pvBase + "+" + jetPV + "=" + pvFinal + " | CA : " + caBase + signCA + "=" + caFinale + " | Atq : " + signAtq + " | Arme : " + typeArme + " Ta quête reprend !");
        else
            CPH.SendMessage(nomJoueur + " entre dans l'Antre en tant que " + classeNom + " ! PV : " + pvBase + "+" + jetPV + "=" + pvFinal + " | CA : " + caBase + signCA + "=" + caFinale + " | Atq : " + signAtq + " | Arme : " + typeArme);

        return true;
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
