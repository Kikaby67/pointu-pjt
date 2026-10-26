// sb-action: !stat
// sb-subaction-id: 887408b9-5275-4c57-817c-425e042a74bb
// sb-trigger: command !stat
// sb-trigger: command !stats
using System;
using System.IO;

// !stat — détail de chaque caractéristique : valeur du personnage + apport des objets.
// Lecture seule, n'écrit jamais dans le profil.
// « Perso » = ce qui est gravé dans le profil (classe, jets de création, niveaux,
// sous-classe, rewards). « obj » = ce qu'apportent les 3 objets équipés.
public class CPHInline
{
    private const string DOSSIER_JOUEURS = @"C:\Users\Florian\pjt\Pointu-PJT\Donnees\joueurs";
    private const string CONFIG_ITEMS    = @"C:\Users\Florian\pjt\Pointu-PJT\Donnees\config_items.json";
    private const string CONFIG_CLASSES  = @"C:\Users\Florian\pjt\Pointu-PJT\Donnees\config_classes.json";
    private const string CONFIG_GLOBAL   = @"C:\Users\Florian\pjt\Pointu-PJT\Donnees\config_global.json";

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

        if (LireValeur(json, "classeChoisie") != "true")
        {
            CPH.SendMessage(nomJoueur + ", choisis d'abord une classe avec !choisirclasse !");
            return true;
        }

        string cfgItems = File.ReadAllText(CONFIG_ITEMS);

        int pvActuels = int.Parse(LireValeur(json, "pvActuels"));
        int pvMax     = int.Parse(LireValeur(json, "pvMax"));
        int ca        = int.Parse(LireValeur(json, "classeArmure"));
        int atq       = int.Parse(LireValeur(json, "bonusAttaque"));
        int manaMax   = int.Parse(LireValeur(json, "manaMax"));
        int charisme  = int.Parse(LireValeur(json, "charisme"));

        // Même repli que !fuir : profil → classe → valeur par défaut
        int agilite = int.Parse(LireValeur(json, "agilite"));
        if (agilite == 0)
        {
            string classe = LireValeur(json, "classe");
            agilite = int.Parse(LireValeur(File.ReadAllText(CONFIG_CLASSES), classe + "_agilite"));
            if (agilite == 0 && File.Exists(CONFIG_GLOBAL))
                agilite = int.Parse(LireValeur(File.ReadAllText(CONFIG_GLOBAL), "agilite_defaut"));
        }

        int objCa    = GetBonusItems(json, cfgItems, "caBonus");
        int objAtq   = GetBonusItems(json, cfgItems, "attaqueBonus");
        int objMana  = GetBonusItems(json, cfgItems, "manaBonus");
        int objCha   = GetBonusItems(json, cfgItems, "charismeBonus");
        int poids    = GetBonusItems(json, cfgItems, "poids");

        string msg = "📊 " + nomJoueur + " — PV " + pvActuels + "/" + pvMax
                   + " · CA "  + Detail(ca, objCa, false)
                   + " · Atq " + Detail(atq, objAtq, true);
        if (manaMax + objMana > 0) msg += " · Mana " + Detail(manaMax, objMana, false);
        msg += " · Cha " + Detail(charisme, objCha, false)
             + " · Agi " + agilite
             + " · Poids " + poids;

        CPH.SendMessage(msg);
        return true;
    }

    // « 21 (17 perso +4 obj) » — ou juste « 17 » si les objets n'apportent rien.
    private string Detail(int perso, int obj, bool signe)
    {
        string total = Signe(perso + obj, signe);
        if (obj == 0) return total;
        return total + " (" + Signe(perso, signe) + " perso " + (obj > 0 ? "+" : "") + obj + " obj)";
    }

    private string Signe(int v, bool signe)
    {
        return signe && v >= 0 ? "+" + v : v.ToString();
    }

    private int GetBonusItems(string json, string cfgItems, string stat)
    {
        string[] slots = { "armeEquipee", "armureEquipee", "accessoireEquipe" };
        int total = 0;
        foreach (string slot in slots)
        {
            string item = LireValeur(json, slot);
            if (item != "" && item != "0")
                total += int.Parse(LireValeur(cfgItems, item + "_" + stat));
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
