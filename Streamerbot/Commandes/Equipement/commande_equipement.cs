// sb-action: !equipement
// sb-subaction-id: 4507f385-9ea8-4b6c-a29b-fd56da795acd
using System;
using System.IO;
public class CPHInline
{
    private const string DOSSIER_JOUEURS = @"C:\Users\Florian\pjt\Pointu-PJT\Donnees\joueurs";
    private const string CONFIG_ITEMS    = @"C:\Users\Florian\pjt\Pointu-PJT\Donnees\config_items.json";
    private const string CONFIG_GLOBAL   = @"C:\Users\Florian\pjt\Pointu-PJT\Donnees\config_global.json";

    public bool Execute()
    {
        string nomJoueur    = args["user"].ToString();
        string cheminFichier = Path.Combine(DOSSIER_JOUEURS, nomJoueur.ToLower() + ".json");

        if (!File.Exists(cheminFichier))
        {
            CPH.SendMessage(nomJoueur + ", tape !rejoindre pour t'inscrire dans l'Antre de Pointu !");
            return true;
        }

        string json = File.ReadAllText(cheminFichier);
        string cfg  = File.ReadAllText(CONFIG_ITEMS);

        string[] slots  = { "armeEquipee", "armureEquipee", "accessoireEquipe" };
        string[] labels = { "Arme", "Armure", "Accessoire" };

        int totalAtq = 0, totalCA = 0, totalMana = 0, totalCha = 0, totalPoids = 0;
        string[] parties = new string[3];

        for (int i = 0; i < 3; i++)
        {
            string item = LireValeur(json, slots[i]);
            if (item == "" || item == "0")
            {
                parties[i] = labels[i] + ": Rien";
                continue;
            }

            int atq = int.Parse(LireValeur(cfg, item + "_attaqueBonus"));
            int ca  = int.Parse(LireValeur(cfg, item + "_caBonus"));
            int man = int.Parse(LireValeur(cfg, item + "_manaBonus"));
            int cha = int.Parse(LireValeur(cfg, item + "_charismeBonus"));
            int pds = int.Parse(LireValeur(cfg, item + "_poids"));

            totalAtq   += atq;
            totalCA    += ca;
            totalMana  += man;
            totalCha   += cha;
            totalPoids += pds;

            string bonus = "";
            if (atq != 0) bonus += (atq > 0 ? "+" : "") + atq + "atq ";
            if (ca  != 0) bonus += (ca  > 0 ? "+" : "") + ca  + "CA ";
            if (man != 0) bonus += (man > 0 ? "+" : "") + man + "mana ";
            if (cha != 0) bonus += (cha > 0 ? "+" : "") + cha + "cha ";
            bonus = bonus.Trim();
            if (pds > 0) bonus += (bonus != "" ? " · " : "") + "poids " + pds;
            if (bonus == "") bonus = "déco";

            parties[i] = labels[i] + ": " + item + " [" + bonus + "]";
        }

        CPH.SendMessage(nomJoueur + " — Équipement : " + string.Join(" · ", parties));

        string total = "";
        if (totalAtq  > 0) total += " +" + totalAtq  + " atq";
        if (totalCA   > 0) total += " +" + totalCA   + " CA";
        if (totalMana > 0) total += " +" + totalMana + " mana";
        if (totalCha  > 0) total += " +" + totalCha  + " charisme";

        // Le poids freine !fuir : on montre le malus réel, lu dans config_global.
        if (totalPoids > 0)
        {
            int pctParPoids = File.Exists(CONFIG_GLOBAL)
                ? int.Parse(LireValeur(File.ReadAllText(CONFIG_GLOBAL), "fuite_poids_pct")) : 0;
            total += " · poids " + totalPoids
                   + (pctParPoids > 0 ? " (−" + (totalPoids * pctParPoids) + "% à !fuir)" : "");
        }

        string msgTotal = total == ""
            ? nomJoueur + " — Aucun bonus d'équipement actif."
            : nomJoueur + " — Bonus total :" + total;
        CPH.SendMessage(msgTotal);

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
}
