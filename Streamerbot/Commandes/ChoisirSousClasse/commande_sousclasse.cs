// sb-action: !sousclasse
// sb-subaction-id: ab0204c3-2061-4230-9c23-7783feb48367
using System;
using System.IO;

public class CPHInline
{
    private const string DOSSIER_JOUEURS = @"C:\Users\Florian\pjt\Pointu-PJT\Donnees\joueurs";
    private const string CONFIG_CLASSES  = @"C:\Users\Florian\pjt\Pointu-PJT\Donnees\config_classes.json";

    public bool Execute()
    {
        string nomJoueur = args["user"].ToString();
        string cheminFichier = Path.Combine(DOSSIER_JOUEURS, nomJoueur.ToLower() + ".json");

        if (!File.Exists(cheminFichier))
        {
            CPH.SendMessage(nomJoueur + ", tu n'es pas encore inscrit ! Tape !rejoindre.");
            return true;
        }

        string json = File.ReadAllText(cheminFichier);

        if (LireValeur(json, "classeChoisie") != "true")
        {
            CPH.SendMessage(nomJoueur + ", tu n'as pas encore choisi de classe ! Tape !choisirclasse.");
            return true;
        }

        if (LireValeur(json, "sousClasseChoisie") == "true")
        {
            string sousClasseActuelle = LireValeur(json, "sousClasse");
            CPH.SendMessage(nomJoueur + ", tu as déjà choisi ta sous-classe : " + sousClasseActuelle + " !");
            return true;
        }

        int niveau = int.Parse(LireValeur(json, "niveau"));
        if (niveau < 5)
        {
            CPH.SendMessage(nomJoueur + ", il te faut atteindre le niveau 5 pour choisir une sous-classe ! (Tu es niveau " + niveau + ")");
            return true;
        }

        string classe = LireValeur(json, "classe");
        string rawInput = args.ContainsKey("rawInput") ? args["rawInput"].ToString().Trim() : "";

        if (string.IsNullOrEmpty(rawInput))
        {
            CPH.SendMessage(nomJoueur + ", choisis ta sous-classe : " + GetOptionsSousClasse(classe) + " → Tape !choisirSousClasse [nom]");
            return true;
        }

        string sousClasse = NormaliserSousClasse(rawInput);

        if (string.IsNullOrEmpty(sousClasse))
        {
            CPH.SendMessage(nomJoueur + ", sous-classe inconnue. Options pour " + classe + " : " + GetOptionsSousClasse(classe));
            return true;
        }

        if (!SousClasseValide(classe, sousClasse))
        {
            CPH.SendMessage(nomJoueur + ", cette sous-classe n'est pas disponible pour la classe " + classe + ". Options : " + GetOptionsSousClasse(classe));
            return true;
        }

        string msgBonus = AppliquerBonusSousClasse(ref json, sousClasse);

        json = ModifierValeur(json, "sousClasse", sousClasse, true);
        json = ModifierValeur(json, "sousClasseChoisie", "true", false);

        File.WriteAllText(cheminFichier, json);
        CPH.SendMessage(nomJoueur + " choisit la sous-classe " + sousClasse + " ! " + msgBonus);
        return true;
    }

    private string AppliquerBonusSousClasse(ref string json, string sousClasse)
    {
        string cfg   = File.ReadAllText(CONFIG_CLASSES);
        int pvBonus  = int.Parse(LireValeur(cfg, sousClasse + "_pvMaxBonus"));
        int caModif  = int.Parse(LireValeur(cfg, sousClasse + "_caModif"));
        string arme  = LireValeur(cfg, sousClasse + "_typeArme");

        if (pvBonus != 0)
        {
            json = AjouterValeur(json, "pvMax", pvBonus);
            json = AjouterValeur(json, "pvActuels", pvBonus);
        }
        if (caModif != 0)
            json = AjouterValeur(json, "classeArmure", caModif);
        if (arme != "0" && arme != "")
            json = ModifierValeur(json, "typeArme", arme, true);

        switch (sousClasse)
        {
            case "Bloc-Hex":        return "+" + pvBonus + " PV max — tu es un monolithe de métal !";
            case "Surcharge":       return caModif + " CA, 2 attaques par tour — attaque sans retenue !";
            case "Byte-Fantôme":    return "3 attaques par tour — frappe comme une ombre !";
            case "Pointeur-Null":   return "Arme → " + arme + ", 1d10 dégâts — vise juste !";
            case "Faille-Zéro":     return "2d8 dégâts — tu exploites la faille ultime du système !";
            case "Compilateur":     return "Buff un allié +2 attaque — assemble la force collective !";
            case "Protocole-Sacré": return "Aura protectrice pour tes alliés — sois leur rempart !";
            case "Serment-Binaire": return "Smite +1d8 en combat — jure sur le code !";
            case "Barde-Binaire":   return "1d10 dégâts et buff TOUS les alliés — la musique du réseau !";
            case "Patch-Mélodique": return "Soin 1d8+3 — ta fréquence corrige toutes les blessures !";
            default:                return "";
        }
    }

    private bool SousClasseValide(string classe, string sousClasse)
    {
        switch (classe)
        {
            case "Hexadécimeur":    return sousClasse == "Bloc-Hex"         || sousClasse == "Surcharge";
            case "Cryptolame":      return sousClasse == "Byte-Fantôme"     || sousClasse == "Pointeur-Null";
            case "Hackmancien":     return sousClasse == "Faille-Zéro"      || sousClasse == "Compilateur";
            case "Firewaller":      return sousClasse == "Protocole-Sacré"  || sousClasse == "Serment-Binaire";
            case "Algorythmancien": return sousClasse == "Barde-Binaire"    || sousClasse == "Patch-Mélodique";
            default:                return false;
        }
    }

    private string GetOptionsSousClasse(string classe)
    {
        switch (classe)
        {
            case "Hexadécimeur":    return "Bloc-Hex (+8 PV) · Surcharge (2 attaques/-2 CA)";
            case "Cryptolame":      return "Byte-Fantôme (3 attaques) · Pointeur-Null (1d10/Arc)";
            case "Hackmancien":     return "Faille-Zéro (1d12) · Compilateur (buff allié)";
            case "Firewaller":      return "Protocole-Sacré (aura) · Serment-Binaire (Smite +1d8)";
            case "Algorythmancien": return "Barde-Binaire (1d10/buff tous) · Patch-Mélodique (soin 1d8+3)";
            default:                return "aucune option disponible";
        }
    }

    private string NormaliserSousClasse(string input)
    {
        switch (input.ToLower().Trim())
        {
            case "bloc-hex":
            case "blochex":              return "Bloc-Hex";
            case "surcharge":            return "Surcharge";
            case "byte-fantôme":
            case "byte-fantome":         return "Byte-Fantôme";
            case "pointeur-null":        return "Pointeur-Null";
            case "faille-zéro":
            case "faille-zero":          return "Faille-Zéro";
            case "compilateur":          return "Compilateur";
            case "protocole-sacré":
            case "protocole-sacre":      return "Protocole-Sacré";
            case "serment-binaire":      return "Serment-Binaire";
            case "barde-binaire":        return "Barde-Binaire";
            case "patch-mélodique":
            case "patch-melodique":      return "Patch-Mélodique";
            default:                     return "";
        }
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

    private string ModifierValeur(string json, string cle, string val, bool estTexte)
    {
        string marqueur = "\"" + cle + "\": ";
        int posDebut = json.IndexOf(marqueur);
        if (posDebut == -1) return json;
        posDebut += marqueur.Length;
        int posFin = json.IndexOfAny(new char[] { ',', '\n', '}' }, posDebut);
        string ancienne = json.Substring(posDebut, posFin - posDebut);
        string nouvelle = estTexte ? "\"" + val + "\"" : val;
        return json.Substring(0, posDebut) + nouvelle + json.Substring(posDebut + ancienne.Length);
    }

    private string AjouterValeur(string json, string cle, int montant)
    {
        string marqueur = "\"" + cle + "\": ";
        int posDebut = json.IndexOf(marqueur);
        if (posDebut == -1) return json;
        posDebut += marqueur.Length;
        int posFin = json.IndexOfAny(new char[] { ',', '\n', '}' }, posDebut);
        string ancienneStr = json.Substring(posDebut, posFin - posDebut).Trim().Trim('"');
        int ancienne = int.TryParse(ancienneStr, out int v) ? v : 0;
        return json.Substring(0, posDebut) + (ancienne + montant).ToString() + json.Substring(posDebut + (posFin - posDebut));
    }
}
