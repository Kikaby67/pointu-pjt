using System;
using System.IO;

public class CPHInline
{
    private const string DOSSIER_JOUEURS = @"C:\Users\Florian\pjt\Pointu-PJT\Donnees\joueurs";
    private const string CONFIG_LEVEL    = @"C:\Users\Florian\pjt\Pointu-PJT\Donnees\config_level.json";
    private const string CONFIG_ALLIES   = @"C:\Users\Florian\pjt\Pointu-PJT\Donnees\config_allies.json";
    private const string CONFIG_ITEMS    = @"C:\Users\Florian\pjt\Pointu-PJT\Donnees\config_items.json";

    public bool Execute()
    {
        string nomJoueur     = args["user"].ToString();
        string cheminFichier = Path.Combine(DOSSIER_JOUEURS, nomJoueur.ToLower() + ".json");

        if (!File.Exists(cheminFichier))
        {
            CPH.SendMessage(nomJoueur + ", tape !rejoindre pour t'inscrire dans l'Antre de Pointu !");
            return true;
        }

        string json       = File.ReadAllText(cheminFichier);
        long   maintenant = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        bool   enRencontre = LireValeur(json, "enRencontre") == "true";
        string typeR       = LireValeur(json, "rencontreType");

        // Uniquement pour les rencontres alliées (pas les combats)
        if (!enRencontre || typeR == "combat" || typeR == "")
        {
            CPH.SendMessage(nomJoueur + ", tu n'as aucune offre alliée en attente !");
            return true;
        }

        long expire = long.Parse(LireValeur(json, "rencontreExpire"));
        if (expire > 0 && maintenant > expire)
        {
            CPH.SendMessage(nomJoueur + ", cette offre a déjà expiré — le timer va remettre ta quête en route !");
            return true;
        }

        bool enQuete = LireValeur(json, "enQuete") == "true";
        int  valeur  = int.Parse(LireValeur(json, "offreValeur"));
        string msgFinal = "";

        // === Handlers par type ===

        if (typeR == "marchand_potion")
        {
            int    prixPotion = valeur; // prix stocké à la création de l'offre (= ce qui a été affiché)
            int    ramJoueur  = int.Parse(LireValeur(json, "ram"));
            string inv        = LireValeurString(json, "inventaire");
            int    nbItems    = inv == "" ? 0 : inv.Split(',').Length;
            int    maxSac     = 8;

            if (ramJoueur < prixPotion)
            {
                CPH.SendMessage(nomJoueur + ", tu n'as pas assez de RAM (" + ramJoueur + "/" + prixPotion + ") pour acheter la Potion. Tape !refuser pour décliner.");
                return true;
            }
            if (nbItems >= maxSac)
            {
                CPH.SendMessage(nomJoueur + ", ton sac est plein ! Tape !refuser pour décliner.");
                return true;
            }
            json     = AjouterValeur(json, "ram", -prixPotion);
            string nouvInv = inv == "" ? "Potion" : inv + ",Potion";
            json     = ModifierValeurString(json, "inventaire", nouvInv);
            msgFinal = nomJoueur + ", tu achètes une Potion pour " + prixPotion + " RAM — elle est dans ton sac !";
        }
        else if (typeR == "vieux_sage")
        {
            string cfgA       = File.ReadAllText(CONFIG_ALLIES);
            int    chanceXP   = int.Parse(LireValeur(cfgA, "vieux_sage_chance_xp"));
            int    chanceItem = int.Parse(LireValeur(cfgA, "vieux_sage_chance_perte_item"));
            Random rng        = new Random();
            bool   gagne      = rng.Next(100) < chanceXP;
            bool   perd       = rng.Next(100) < chanceItem;

            if (gagne)
            {
                json     = AjouterValeur(json, "experience", valeur);
                json     = VerifierMonteeNiveau(json, nomJoueur);
                msgFinal  = nomJoueur + ", tu acceptes le marché du Vieux Sage — il te transmet son savoir ! +" + valeur + " XP !";
            }
            if (perd)
            {
                string   inv   = LireValeurString(json, "inventaire");
                string[] items = inv == "" ? new string[0] : inv.Split(',');
                if (items.Length > 0)
                {
                    int    idx      = rng.Next(items.Length);
                    string perdu    = items[idx].Trim();
                    string nouvInv  = "";
                    bool   retire   = false;
                    foreach (string it in items)
                    {
                        if (!retire && it.Trim() == perdu) { retire = true; continue; }
                        if (nouvInv != "") nouvInv += ",";
                        nouvInv += it.Trim();
                    }
                    json     = ModifierValeurString(json, "inventaire", nouvInv);
                    msgFinal += (msgFinal != "" ? " Mais" : nomJoueur + ", tu acceptes le marché —")
                              + " le Vieux Sage s'empare de " + perdu + " en guise de paiement...";
                }
            }
            if (!gagne && !perd)
                msgFinal = nomJoueur + ", tu acceptes le marché — le Vieux Sage sourit et s'éclipse sans rien donner ni prendre...";
        }
        else if (typeR == "bonus_ram")
        {
            json     = AjouterValeur(json, "ram", valeur);
            msgFinal = nomJoueur + ", tu ramasses la bourse : +" + valeur + " RAM !";
        }
        else if (typeR == "alcove_chene")
        {
            int pvActuels   = int.Parse(LireValeur(json, "pvActuels"));
            int pvMax       = int.Parse(LireValeur(json, "pvMax"));
            int manaActuels = int.Parse(LireValeur(json, "manaActuels"));
            int manaMax     = int.Parse(LireValeur(json, "manaMax"));
            int pvSoin      = pvMax - pvActuels;
            int manaSoin    = manaMax - manaActuels;
            json     = ModifierValeur(json, "pvActuels",   pvMax.ToString(),   false);
            if (manaMax > 0)
                json = ModifierValeur(json, "manaActuels", manaMax.ToString(), false);
            string partMana = manaMax > 0 ? " | Mana : " + manaActuels + " → " + manaMax : "";
            msgFinal = nomJoueur + ", tu te reposes dans l'alcôve du chêne-serveur. PV : " + pvActuels + " → " + pvMax + partMana + " — Entièrement restauré !";
        }
        else if (typeR == "marchand_classe")
        {
            // Rappel des classes disponibles — la rencontre reste active, le joueur tape !choisirclasse
            string classeActuelle = LireValeur(json, "classe");
            CPH.SendMessage(nomJoueur + ", le Marchand de Classe te propose de changer ! Ta classe actuelle : " + classeActuelle + ". Tape !choisirclasse [nom] pour changer | !refuser pour décliner. Classes : Hexadécimeur · Cryptolame · Hackmancien · Firewaller · Algorythmancien");
            return true; // rencontre maintenue — elle sera résolue par !choisirclasse ou !refuser
        }
        else
        {
            msgFinal = nomJoueur + ", offre acceptée.";
        }

        // === Résoudre la rencontre et reprendre la quête ===
        long pauseDebut = long.Parse(LireValeur(json, "quetePauseDebut"));
        long totalPause = long.Parse(LireValeur(json, "queteTotalPause"));
        if (pauseDebut > 0)
            json = ModifierValeur(json, "queteTotalPause", (totalPause + (maintenant - pauseDebut)).ToString(), false);
        json = ModifierValeur(json, "enRencontre",     "false", false);
        json = ModifierValeur(json, "rencontreType",   "",      true);
        json = ModifierValeur(json, "rencontreExpire", "0",     false);
        json = ModifierValeur(json, "quetePauseDebut", "0",     false);

        File.WriteAllText(cheminFichier, json);
        CPH.SendMessage(msgFinal + (enQuete ? " Ta quête reprend !" : ""));
        return true;
    }

    private string VerifierMonteeNiveau(string json, string nomJoueur)
    {
        int niveauActuel  = int.Parse(LireValeur(json, "niveau"));
        int xp            = int.Parse(LireValeur(json, "experience"));
        int nouveauNiveau = CalculerNiveau(xp);
        if (nouveauNiveau > niveauActuel)
        {
            json = ModifierValeur(json, "niveau", nouveauNiveau.ToString(), false);
            json = AppliquerBonusNiveau(json, nouveauNiveau);
            CPH.SendMessage(MessageNiveau(nomJoueur, nouveauNiveau));
        }
        return json;
    }

    private int CalculerNiveau(int xp)
    {
        string cfg    = File.ReadAllText(CONFIG_LEVEL);
        int niveauMax = int.Parse(LireValeur(cfg, "niveauMax"));
        for (int i = niveauMax; i >= 2; i--)
            if (xp >= int.Parse(LireValeur(cfg, "niveau_" + i + "_xp"))) return i;
        return 1;
    }

    private string AppliquerBonusNiveau(string json, int niveau)
    {
        string cfg        = File.ReadAllText(CONFIG_LEVEL);
        int pvBonus       = int.Parse(LireValeur(cfg, "niveau_" + niveau + "_pvBonus"));
        int caBonus       = int.Parse(LireValeur(cfg, "niveau_" + niveau + "_caBonus"));
        int ramBonus      = int.Parse(LireValeur(cfg, "niveau_" + niveau + "_ramBonus"));
        int charismeBonus = int.Parse(LireValeur(cfg, "niveau_" + niveau + "_charismeBonus"));
        if (pvBonus > 0) { json = AjouterValeur(json, "pvMax", pvBonus); json = AjouterValeur(json, "pvActuels", pvBonus); }
        if (caBonus       > 0) json = AjouterValeur(json, "classeArmure", caBonus);
        if (ramBonus      > 0) json = AjouterValeur(json, "ram",          ramBonus);
        if (charismeBonus > 0) json = AjouterValeur(json, "charisme",     charismeBonus);
        return json;
    }

    private string MessageNiveau(string nomJoueur, int niveau)
    {
        string cfg   = File.ReadAllText(CONFIG_LEVEL);
        string bonus = LireValeur(cfg, "niveau_" + niveau + "_message");
        return "🎉 " + nomJoueur + " passe au niveau " + niveau + " ! " + bonus;
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

    private string AjouterValeur(string json, string cle, int montant)
    {
        string marqueur = "\"" + cle + "\": ";
        int posDebut    = json.IndexOf(marqueur);
        if (posDebut == -1) return json;
        posDebut       += marqueur.Length;
        int posFin      = json.IndexOfAny(new char[] { ',', '\n', '}' }, posDebut);
        string ancienneStr = json.Substring(posDebut, posFin - posDebut).Trim().Trim('"');
        int ancienne = int.TryParse(ancienneStr, out int v) ? v : 0;
        return json.Substring(0, posDebut) + (ancienne + montant).ToString() + json.Substring(posDebut + (posFin - posDebut));
    }
}
