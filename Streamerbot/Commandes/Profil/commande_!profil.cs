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

        string equipe = "";
        string armeEq   = LireValeur(json, "armeEquipee");
        string armureEq = LireValeur(json, "armureEquipee");
        string accEq    = LireValeur(json, "accessoireEquipe");
        if (armeEq   != "" && armeEq   != "0") equipe += armeEq;
        if (armureEq != "" && armureEq != "0") equipe += (equipe != "" ? ", " : "") + armureEq;
        if (accEq    != "" && accEq    != "0") equipe += (equipe != "" ? ", " : "") + accEq;
        if (equipe   != "") msg += " | " + equipe;

        CPH.SendMessage(msg);
        return true;
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
