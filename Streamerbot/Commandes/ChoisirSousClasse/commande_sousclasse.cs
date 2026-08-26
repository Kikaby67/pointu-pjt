// sb-action: !sousclasse
// sb-subaction-id: ab0204c3-2061-4230-9c23-7783feb48367
using System;
using System.IO;

public class CPHInline
{
    private const string DOSSIER_JOUEURS = @"C:\Users\Florian\pjt\Pointu-PJT\Donnees\joueurs";
    private const string CONFIG_CLASSES  = @"C:\Users\Florian\pjt\Pointu-PJT\Donnees\config_classes.json";
    private const string CONFIG_LEVEL    = @"C:\Users\Florian\pjt\Pointu-PJT\Donnees\config_level.json";

    public bool Execute()
    {
        string nomJoueur     = args["user"].ToString();
        string cheminFichier = Path.Combine(DOSSIER_JOUEURS, nomJoueur.ToLower() + ".json");

        if (!File.Exists(cheminFichier))
        {
            CPH.SendMessage(nomJoueur + ", tu n'es pas encore inscrit ! Tape !rejoindre.");
            return true;
        }

        string json = File.ReadAllText(cheminFichier);
        string cfg  = File.ReadAllText(CONFIG_CLASSES);

        if (LireValeur(json, "classeChoisie") != "true")
        {
            CPH.SendMessage(nomJoueur + ", tu n'as pas encore choisi de classe ! Tape !choisirclasse.");
            return true;
        }

        string classe = LireValeur(json, "classe");


        // Déjà spécialisé : on RAPPELLE l'effet actif. Avant, le joueur n'avait
        // aucun moyen de savoir ce que sa sous-classe lui apportait réellement.

        if (LireValeur(json, "sousClasseChoisie") == "true")
        {
            string actuelle = LireValeur(json, "sousClasse");
            CPH.SendMessage(nomJoueur + " — " + actuelle + " : " + Libelle(cfg, actuelle)
                          + " (choix définitif)");
            return true;
        }


        // Palier de déblocage lu dans config_level, jamais en dur

        int niveau    = int.Parse(LireValeur(json, "niveau"));
        int niveauMin = NiveauSousClasse(cfg);
        if (niveau < niveauMin)
        {
            CPH.SendMessage(nomJoueur + ", il te faut le niveau " + niveauMin
                          + " pour choisir ta voie. (Tu es niveau " + niveau + ")");
            return true;
        }


        // Les deux options viennent du config : "<Classe>_sousClasses"

        string duo = LireValeurString(cfg, classe + "_sousClasses");
        if (duo == "")
        {
            CPH.SendMessage(nomJoueur + ", aucune voie n'est prévue pour la classe " + classe + ".");
            return true;
        }
        string[] options = duo.Split(',');

        string rawInput = args.ContainsKey("rawInput") && args["rawInput"] != null
                        ? args["rawInput"].ToString().Trim() : "";

        if (rawInput == "")
        {
            CPH.SendMessage("🐢 " + nomJoueur + ", Pointu attend que tu nommes ta voie : " + OptionsLisibles(cfg, options)
                          + " → !sousclasse [nom]");
            return true;
        }


        // Recherche tolérante : casse et accents ignorés (personne ne tape "Faille-Zéro" avec l'accent)

        string sousClasse = "";
        foreach (string o in options)
            if (NormaliserNom(o) == NormaliserNom(rawInput)) { sousClasse = o.Trim(); break; }

        if (sousClasse == "")
        {
            CPH.SendMessage(nomJoueur + ", voie inconnue pour un " + classe + ". Options : " + OptionsLisibles(cfg, options));
            return true;
        }


        // Application + relevé AVANT/APRÈS : le joueur doit VOIR ce qui a changé

        int    pvAvant   = int.Parse(LireValeur(json, "pvMax"));
        int    caAvant   = int.Parse(LireValeur(json, "classeArmure"));
        string armeAvant = LireValeur(json, "typeArme");

        int    pvBonus = int.Parse(LireValeur(cfg, sousClasse + "_pvMaxBonus"));
        int    caModif = int.Parse(LireValeur(cfg, sousClasse + "_caModif"));
        string arme    = LireValeurString(cfg, sousClasse + "_typeArme");

        if (pvBonus != 0)
        {
            json = AjouterValeur(json, "pvMax",     pvBonus);
            json = AjouterValeur(json, "pvActuels", pvBonus);
        }
        if (caModif != 0) json = AjouterValeur(json, "classeArmure", caModif);
        if (arme != "")   json = ModifierValeur(json, "typeArme", arme, true);

        json = ModifierValeur(json, "sousClasse",        sousClasse, true);
        json = ModifierValeur(json, "sousClasseChoisie", "true",     false);
        File.WriteAllText(cheminFichier, json);

        string changements = "";
        if (pvBonus != 0) changements += " PV max " + pvAvant + "→" + (pvAvant + pvBonus) + " ·";
        if (caModif != 0) changements += " CA " + caAvant + "→" + (caAvant + caModif) + " ·";
        if (arme != "" && arme != armeAvant) changements += " Arme " + armeAvant + "→" + arme + " ·";
        changements = changements.TrimEnd('·', ' ');

        CPH.SendMessage("🐢 " + nomJoueur + " emprunte la voie du " + sousClasse + " ! "
                      + Libelle(cfg, sousClasse)
                      + (changements == "" ? "" : " |" + changements));

        string lore = LireValeurString(cfg, sousClasse + "_lore");
        if (lore != "") CPH.SendMessage("📜 " + lore);

        return true;
    }

    private string Libelle(string cfg, string sousClasse)
    {
        string l = LireValeurString(cfg, sousClasse + "_libelle");
        return l == "" ? "effet non documenté" : l;
    }

    private string OptionsLisibles(string cfg, string[] options)
    {
        string s = "";
        foreach (string o in options)
        {
            string n = o.Trim();
            if (s != "") s += " · ";
            s += n + " (" + Libelle(cfg, n) + ")";
        }
        return s;
    }

    // Le palier de sous-classe est le niveau dont le message annonce la voie.
    // Repli sur 5 si config_level est illisible — on ne bloque jamais le joueur.
    private int NiveauSousClasse(string cfgClasses)
    {
        try
        {
            string cfgL = File.ReadAllText(CONFIG_LEVEL);
            for (int n = 2; n <= 20; n++)
                if (LireValeurString(cfgL, "niveau_" + n + "_message").ToLower().Contains("sous-classe"))
                    return n;
        }
        catch (Exception ex) { CPH.LogWarn("!sousclasse — lecture du palier impossible : " + ex.Message); }
        return 5;
    }

    private string NormaliserNom(string s)
    {
        if (s == null) return "";
        string d = s.Trim().Normalize(System.Text.NormalizationForm.FormD);
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        foreach (char c in d)
            if (System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c)
                != System.Globalization.UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        return sb.ToString().Normalize(System.Text.NormalizationForm.FormC).ToLowerInvariant();
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

    private string AjouterValeur(string json, string cle, int montant)
    {
        int val = int.Parse(LireValeur(json, cle));
        return ModifierValeur(json, cle, (val + montant).ToString(), false);
    }
}
