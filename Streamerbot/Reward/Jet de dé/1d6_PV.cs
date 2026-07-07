// sb-action: Jet de dé - PV
// sb-subaction-id: ac722527-c374-4c18-887a-610b8583fc3b
using System;
using System.IO;

public class CPHInline
{
	private const string DOSSIER_JOUEURS = @"C:\Users\Florian\pjt\Pointu-PJT\Donnees\joueurs";
	private const string CONFIG_CLASSES  = @"C:\Users\Florian\pjt\Pointu-PJT\Donnees\config_classes.json";
	public bool Execute()
	{
		// Reward point Stat PV
		string nomJoueur	= args["user"].ToString();
		string cheminFichier = Path.Combine(DOSSIER_JOUEURS, nomJoueur.ToLower() + ".json");

		if (!File.Exists(cheminFichier))
		{
			CPH.SendMessage(nomJoueur + ", tape !rejoindre pour créer ton profil !");
			return true;
		}

		string json = File.ReadAllText(cheminFichier);

		if (LireValeur(json, "classeChoisie") != "true")
		{
			CPH.SendMessage(nomJoueur + ", choisis d'abord ta classe avec !choisirclasse !");
			return true;
		}
		
		string classe = LireValeur(json, "classe");
		int[] baseClasse = GetClasseBase(classe);
		int pvBase = baseClasse[0];
		int resultat = new Random().Next(1, 7); //1D6
		int nouveauPvMax = pvBase + resultat;

		json = ModifierValeur(json, "pvMax",     nouveauPvMax.ToString(),    false);
		string nomStat = "PV";

		File.WriteAllText(cheminFichier, json);
		CPH.SendMessage(nomJoueur + " À fait un Jet sur " + nomStat + " : +" + resultat + " !");
		return true;
	}

	private int[] GetClasseBase(string classe)
    {
        string cfg = File.ReadAllText(CONFIG_CLASSES);
        int pvBase = int.Parse(LireValeur(cfg, classe + "_pvBase"));
        int caBase = int.Parse(LireValeur(cfg, classe + "_caBase"));
        return pvBase != 0 ? new int[] { pvBase, caBase, 0, 0 } : new int[] { 10, 10, 0, 0 };
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