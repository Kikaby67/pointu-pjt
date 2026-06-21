using System;
using System.IO;

public class CPHInline
{
    private const string DOSSIER_JOUEURS = @"C:\Users\Florian\pjt\Pointu-PJT\Donnees\joueurs";
    private const string CONFIG_LEVEL    = @"C:\Users\Florian\pjt\Pointu-PJT\Donnees\config_level.json";
    private const string CONFIG_GLOBAL   = @"C:\Users\Florian\pjt\Pointu-PJT\Donnees\config_global.json";

    public bool Execute()
    {
        string cfgG    = File.ReadAllText(CONFIG_GLOBAL);
        int xpGain     = int.Parse(LireValeur(cfgG, "timer_xp_gain"));
        int regenPV    = int.Parse(LireValeur(cfgG, "timer_regen_pv"));
        int regenMana  = int.Parse(LireValeur(cfgG, "timer_regen_mana"));
        long seuil     = long.Parse(LireValeur(cfgG, "timer_activite_seuil_secondes"));

        long maintenant = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        string[] fichiers = Directory.GetFiles(DOSSIER_JOUEURS, "*.json");

        foreach (string cheminFichier in fichiers)
        {
            string json = File.ReadAllText(cheminFichier);

            if (LireValeur(json, "classeChoisie") != "true") continue;

            // Vérifie que le joueur a été actif dans le stream récemment (tracker_activite.cs)
            json = EnsureChamp(json, "derniereActivite", "0", false);
            long derniereActivite = long.Parse(LireValeur(json, "derniereActivite"));
            if (derniereActivite == 0 || (maintenant - derniereActivite) > seuil) continue;

            string nomJoueur = LireValeur(json, "nomJoueur");

            int xpActuel     = int.Parse(LireValeur(json, "experience"));
            int niveauActuel = int.Parse(LireValeur(json, "niveau"));
            int nouvelXP     = xpActuel + xpGain;

            json = ModifierValeur(json, "experience", nouvelXP.ToString(), false);

            int nouveauNiveau = CalculerNiveau(nouvelXP);

            if (nouveauNiveau > niveauActuel)
            {
                json = ModifierValeur(json, "niveau", nouveauNiveau.ToString(), false);
                json = AppliquerBonusNiveau(json, nouveauNiveau);
                CPH.SendMessage(MessageNiveau(nomJoueur, nouveauNiveau));
            }

            if (LireValeur(json, "enCombat") != "true")
            {
                int pvActuels = int.Parse(LireValeur(json, "pvActuels"));
                int pvMax     = int.Parse(LireValeur(json, "pvMax"));
                if (pvActuels < pvMax)
                    json = ModifierValeur(json, "pvActuels", Math.Min(pvActuels + regenPV, pvMax).ToString(), false);

                int manaActuels = int.Parse(LireValeur(json, "manaActuels"));
                int manaMax     = int.Parse(LireValeur(json, "manaMax"));
                if (manaActuels < manaMax)
                    json = ModifierValeur(json, "manaActuels", Math.Min(manaActuels + regenMana, manaMax).ToString(), false);
            }

            File.WriteAllText(cheminFichier, json);
        }

        return true;
    }

    private int CalculerNiveau(int xp)
    {
        string cfg      = File.ReadAllText(CONFIG_LEVEL);
        int niveauMax   = int.Parse(LireValeur(cfg, "niveauMax"));
        for (int i = niveauMax; i >= 2; i--)
        {
            int seuil = int.Parse(LireValeur(cfg, "niveau_" + i + "_xp"));
            if (xp >= seuil) return i;
        }
        return 1;
    }

    private string AppliquerBonusNiveau(string json, int niveau)
    {
        string cfg        = File.ReadAllText(CONFIG_LEVEL);
        int pvBonus       = int.Parse(LireValeur(cfg, "niveau_" + niveau + "_pvBonus"));
        int caBonus       = int.Parse(LireValeur(cfg, "niveau_" + niveau + "_caBonus"));
        int ramBonus      = int.Parse(LireValeur(cfg, "niveau_" + niveau + "_ramBonus"));
        int charismeBonus = int.Parse(LireValeur(cfg, "niveau_" + niveau + "_charismeBonus"));

        if (pvBonus > 0)
        {
            json = AjouterValeur(json, "pvMax",     pvBonus);
            json = AjouterValeur(json, "pvActuels", pvBonus);
        }
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

    private string EnsureChamp(string json, string cle, string valeurDefaut, bool estTexte)
    {
        if (json.Contains("\"" + cle + "\"")) return json;
        int    pos = json.LastIndexOf('}');
        string val = estTexte ? "\"" + valeurDefaut + "\"" : valeurDefaut;
        return json.Substring(0, pos) + ",\n  \"" + cle + "\": " + val + "\n}";
    }

    private string AjouterValeur(string json, string cle, int montant)
    {
        int valeurActuelle = int.Parse(LireValeur(json, cle));
        return ModifierValeur(json, cle, (valeurActuelle + montant).ToString(), false);
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
}
