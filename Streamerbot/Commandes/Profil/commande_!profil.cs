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

        // Message 1 : identité
        int niveau = int.Parse(LireValeur(json, "niveau"));
        int xp     = int.Parse(LireValeur(json, "experience"));
        int ram    = int.Parse(LireValeur(json, "ram"));
        CPH.SendMessage(nomJoueur + " | Niv " + niveau + " | XP : " + xp + " | RAM : " + ram);

        // Message 2 : classe
        if (LireValeur(json, "classeChoisie") != "true")
        {
            CPH.SendMessage(nomJoueur + " — Classe : aucune (tape !choisirclasse)");
        }
        else
        {
            string classe     = LireValeur(json, "classe");
            string sousClasse = LireValeur(json, "sousClasse");
            string typeArme   = LireValeur(json, "typeArme");
            string msg2 = nomJoueur + " — Classe : " + classe;
            if (sousClasse != "" && sousClasse != "0") msg2 += " / " + sousClasse;
            msg2 += " | Arme : " + (typeArme != "" && typeArme != "0" ? typeArme : "aucune");
            CPH.SendMessage(msg2);
        }

        // Message 3 : stats combat (effectives = base + bonus items)
        int pvActuels   = int.Parse(LireValeur(json, "pvActuels"));
        int pvMax       = int.Parse(LireValeur(json, "pvMax"));
        int ca          = int.Parse(LireValeur(json, "classeArmure"))  + GetBonusItems(json, "caBonus");
        int atq         = int.Parse(LireValeur(json, "bonusAttaque"))  + GetBonusItems(json, "attaqueBonus");
        int manaActuels = int.Parse(LireValeur(json, "manaActuels"));
        int manaMax     = int.Parse(LireValeur(json, "manaMax"))       + GetBonusItems(json, "manaBonus");
        int charisme    = int.Parse(LireValeur(json, "charisme"))      + GetBonusItems(json, "charismeBonus");
        int agilite     = int.Parse(LireValeur(json, "agilite"));

        string msg3 = nomJoueur + " — PV : " + pvActuels + "/" + pvMax + " | CA : " + ca + " | Atq : +" + atq;
        if (manaMax > 0) msg3 += " | Mana : " + manaActuels + "/" + manaMax;
        if (charisme > 0) msg3 += " | Charisme : " + charisme;
        if (agilite > 0) msg3 += " | Agilité : " + agilite;
        CPH.SendMessage(msg3);

        // Message 4 : stats + items équipés
        int combatsGagnes   = int.Parse(LireValeur(json, "combatsGagnes"));
        int combatsPerdus   = int.Parse(LireValeur(json, "combatsPerdus"));
        int quetesTerminees = int.Parse(LireValeur(json, "quetesTerminees"));
        bool enCombat = LireValeur(json, "enCombat") == "true";
        bool enQuete  = LireValeur(json, "enQuete")  == "true";
        string compagnon = LireValeur(json, "compagnonActif");

        string msg4 = nomJoueur + " — Combats : " + combatsGagnes + "V/" + combatsPerdus + "D | Quêtes : " + quetesTerminees;
        if (enCombat) msg4 += " | EN RENCONTRE";
        else if (enQuete) msg4 += " | EN QUETE";
        if (compagnon != "" && compagnon != "0") msg4 += " | Compagnon : " + compagnon;

        string armeEq   = LireValeur(json, "armeEquipee");
        string armureEq = LireValeur(json, "armureEquipee");
        string accEq    = LireValeur(json, "accessoireEquipe");
        string equipe   = "";
        if (armeEq   != "" && armeEq   != "0") equipe += " Arme:" + armeEq;
        if (armureEq != "" && armureEq != "0") equipe += " Armure:" + armureEq;
        if (accEq    != "" && accEq    != "0") equipe += " Acc:" + accEq;
        if (equipe   != "") msg4 += " |" + equipe;

        CPH.SendMessage(msg4);
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
