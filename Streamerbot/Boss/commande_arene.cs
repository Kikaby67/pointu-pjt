// sb-action: Combat de boss
// sb-subaction-id: a355a4c3-3d46-4e73-af6f-901f9ea73f28
using System;
using System.IO;

// !arene — rejoindre la bataille de boss pendant la phase de recrutement.
public class CPHInline
{
    private const string DOSSIER_JOUEURS = @"C:\Users\Florian\pjt\Pointu-PJT\Donnees\joueurs";
    private const string ETAT_GLOBAL     = @"C:\Users\Florian\pjt\Pointu-PJT\Donnees\etat_global.json";

    public bool Execute()
    {
        string nomJoueur     = args["user"].ToString();
        string cheminFichier = Path.Combine(DOSSIER_JOUEURS, nomJoueur.ToLower() + ".json");

        if (!File.Exists(cheminFichier))
        {
            CPH.SendMessage(nomJoueur + ", tu n'es pas encore inscrit ! Tape !rejoindre.");
            return true;
        }

        string etat = File.ReadAllText(ETAT_GLOBAL);
        if (LireValeur(etat, "bossActif") != "true")
        {
            CPH.SendMessage(nomJoueur + ", aucun boss n'est annoncé pour l'instant.");
            return true;
        }
        if (LireValeurString(etat, "bossPhase") != "recrutement")
        {
            CPH.SendMessage(nomJoueur + ", le combat a déjà commencé, trop tard pour rejoindre ! Attends le prochain boss.");
            return true;
        }

        string json = File.ReadAllText(cheminFichier);
        if (LireValeur(json, "classeChoisie") != "true")
        {
            CPH.SendMessage(nomJoueur + ", choisis d'abord une classe (!choisirclasse) avant de rejoindre l'arène !");
            return true;
        }
        if (int.Parse(LireValeur(json, "pvActuels")) <= 0)
        {
            CPH.SendMessage(nomJoueur + ", tu es à terre ! Va te soigner (!repos) avant de rejoindre le combat.");
            return true;
        }

        string pseudoKey = nomJoueur.ToLower();
        string ordre = LireValeurString(etat, "ordre");

        if (DansListe(ordre, pseudoKey))
        {
            CPH.SendMessage(nomJoueur + ", tu es déjà dans l'arène ! Prépare-toi au combat.");
            return true;
        }

        ordre = ordre == "" ? pseudoKey : ordre + "," + pseudoKey;
        etat  = ModifierValeurString(etat, "ordre", ordre);
        File.WriteAllText(ETAT_GLOBAL, etat);

        int nb = ordre.Split(',').Length;
        CPH.SendMessage("⚔️ " + nomJoueur + " rejoint la bataille contre " + LireValeurString(etat, "bossNom") + " ! (" + nb + " combattant" + (nb > 1 ? "s" : "") + ")");
        return true;
    }

    private bool DansListe(string csv, string pseudo)
    {
        if (csv == "") return false;
        foreach (string p in csv.Split(','))
            if (p.Trim() == pseudo) return true;
        return false;
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
