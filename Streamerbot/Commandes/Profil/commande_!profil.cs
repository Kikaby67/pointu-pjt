// sb-action: !profil
// sb-subaction-id: 5bee0ddc-b940-444d-806c-9e8e5fea2235
using System;
using System.IO;

public class CPHInline
{
    private const string DOSSIER_JOUEURS = @"C:\Users\Florian\pjt\Pointu-PJT\Donnees\joueurs";
    private const string CONFIG_ITEMS    = @"C:\Users\Florian\pjt\Pointu-PJT\Donnees\config_items.json";

    public bool Execute()
    {
        string nomJoueur     = args["user"].ToString();
        string cheminFichier = Path.Combine(DOSSIER_JOUEURS, nomJoueur.ToLower() + ".json");

        if (!File.Exists(cheminFichier))
        {
            CPH.SendMessage(nomJoueur + ", tape !rejoindre pour créer ton profil !");
            return true;
        }

        string json = File.ReadAllText(cheminFichier);

        int niveau = int.Parse(LireValeur(json, "niveau"));
        int xp     = int.Parse(LireValeur(json, "experience"));
        int ram    = int.Parse(LireValeur(json, "ram"));

        bool   aClasse    = LireValeur(json, "classeChoisie") == "true";
        string classe     = LireValeur(json, "classe");
        string sousClasse = LireValeur(json, "sousClasse");
        string typeArme   = LireValeur(json, "typeArme");

        int pvActuels   = int.Parse(LireValeur(json, "pvActuels"));
        int pvMax       = int.Parse(LireValeur(json, "pvMax"));
        int ca          = int.Parse(LireValeur(json, "classeArmure")) + GetBonusItems(json, "caBonus");
        int atq         = int.Parse(LireValeur(json, "bonusAttaque")) + GetBonusItems(json, "attaqueBonus");
        int manaActuels = int.Parse(LireValeur(json, "manaActuels"));
        int manaMax     = int.Parse(LireValeur(json, "manaMax"))      + GetBonusItems(json, "manaBonus");
        int charisme    = int.Parse(LireValeur(json, "charisme"))     + GetBonusItems(json, "charismeBonus");

        int combatsGagnes   = int.Parse(LireValeur(json, "combatsGagnes"));
        int combatsPerdus   = int.Parse(LireValeur(json, "combatsPerdus"));
        int quetesTerminees = int.Parse(LireValeur(json, "quetesTerminees"));

        // UN SEUL message : tout le profil tient largement dans les 500 caractères de Twitch.
        string msg = "🐢 " + nomJoueur + " — Niv " + niveau + " · " + xp + " XP · " + ram + " RAM";

        if (!aClasse)
        {
            msg += " | sans classe (!choisirclasse)";
        }
        else
        {
            msg += " | " + classe;
            if (sousClasse != "" && sousClasse != "0") msg += "/" + sousClasse;
            if (typeArme   != "" && typeArme   != "0") msg += " · " + typeArme;
        }

        msg += " | PV " + pvActuels + "/" + pvMax + " · CA " + ca + " · Atq +" + atq;
        if (manaMax  > 0) msg += " · Mana " + manaActuels + "/" + manaMax;
        if (charisme > 0) msg += " · Cha " + charisme;

        msg += " | " + combatsGagnes + "V/" + combatsPerdus + "D · " + quetesTerminees + " quêtes";

        if (LireValeur(json, "enCombat") == "true")     msg += " · ⚔️ en rencontre";
        else if (LireValeur(json, "enQuete") == "true") msg += " · 🧭 en quête";

        // Chaque objet équipé avec ses chiffres : « Lame-de-Pointu [+2atq] »
        string   cfgItems = File.ReadAllText(CONFIG_ITEMS);
        string   equipe   = "";
        string[] slotsEq  = { "armeEquipee", "armureEquipee", "accessoireEquipe" };
        foreach (string slot in slotsEq)
        {
            string item = LireValeur(json, slot);
            if (item == "" || item == "0") continue;
            equipe += (equipe != "" ? ", " : "") + item + " [" + BonusItem(cfgItems, item) + "]";
        }

        // Limite Twitch : 500 caractères. Avec des noms longs, l'équipement part dans un 2e message.
        bool equipeAPart = equipe != "" && msg.Length + 3 + equipe.Length > 500;
        if (equipe != "" && !equipeAPart) msg += " | " + equipe;
        CPH.SendMessage(msg);
        if (equipeAPart) CPH.SendMessage("🎒 " + nomJoueur + " — " + equipe);
        return true;
    }

    // Les caractéristiques d'un objet en version courte : « +2atq +1CA · poids 1 »
    private string BonusItem(string cfgItems, string item)
    {
        int atq   = int.Parse(LireValeur(cfgItems, item + "_attaqueBonus"));
        int ca    = int.Parse(LireValeur(cfgItems, item + "_caBonus"));
        int man   = int.Parse(LireValeur(cfgItems, item + "_manaBonus"));
        int cha   = int.Parse(LireValeur(cfgItems, item + "_charismeBonus"));
        int poids = int.Parse(LireValeur(cfgItems, item + "_poids"));

        string bonus = "";
        if (atq != 0) bonus += (atq > 0 ? "+" : "") + atq + "atq ";
        if (ca  != 0) bonus += (ca  > 0 ? "+" : "") + ca  + "CA ";
        if (man != 0) bonus += (man > 0 ? "+" : "") + man + "mana ";
        if (cha != 0) bonus += (cha > 0 ? "+" : "") + cha + "cha ";
        bonus = bonus.Trim();
        if (poids > 0) bonus += (bonus != "" ? " · " : "") + "poids " + poids;
        return bonus == "" ? "déco" : bonus;
    }

    private int GetBonusItems(string json, string stat)
    {
        string   cfgItems = File.ReadAllText(CONFIG_ITEMS);
        string[] slots    = { "armeEquipee", "armureEquipee", "accessoireEquipe" };
        int total = 0;
        foreach (string slot in slots)
        {
            string item = LireValeur(json, slot);
            if (item != "" && item != "0")
            {
                string valStr = LireValeur(cfgItems, item + "_" + stat);
                if (valStr != "0") total += int.Parse(valStr);
            }
        }
        return total;
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
}
