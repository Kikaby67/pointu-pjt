// sb-action: !accepter
// sb-subaction-id: 65fa667b-b4ef-47dc-a78a-f916b311c8fe
using System;
using System.IO;

public class CPHInline
{
    private const string DOSSIER_JOUEURS = @"C:\Users\Florian\pjt\Pointu-PJT\Donnees\joueurs";
    private const string CONFIG_LEVEL    = @"C:\Users\Florian\pjt\Pointu-PJT\Donnees\config_level.json";
    private const string CONFIG_ALLIES   = @"C:\Users\Florian\pjt\Pointu-PJT\Donnees\config_allies.json";
    private const string CONFIG_ITEMS    = @"C:\Users\Florian\pjt\Pointu-PJT\Donnees\config_items.json";
    private const string CONFIG_GLOBAL   = @"C:\Users\Florian\pjt\Pointu-PJT\Donnees\config_global.json";
    private const string CONFIG_CLASSES  = @"C:\Users\Florian\pjt\Pointu-PJT\Donnees\config_classes.json";

    public bool Execute()
    {
        string nomJoueur     = args["user"].ToString();
        string cheminFichier = Path.Combine(DOSSIER_JOUEURS, nomJoueur.ToLower() + ".json");

        if (!File.Exists(cheminFichier))
        {
            CPH.SendMessage(nomJoueur + ", tape !rejoindre pour t'inscrire dans l'Antre de Pointu !");
            return true;
        }

        string json       = File.ReadAllText(cheminFichier);
        long   maintenant = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        // === Duel entre joueurs (hors quête) — prioritaire sur les rencontres alliées ===
        json = EnsureChamp(json, "duelDe",     "", true);
        json = EnsureChamp(json, "duelExpire", "0", false);
        string duelDe = LireValeur(json, "duelDe");
        if (duelDe != "" && duelDe != "0")
        {
            long dExp = long.Parse(LireValeur(json, "duelExpire"));
            if (maintenant > dExp)
            {
                json = ModifierValeur(json, "duelDe",     "",  true);
                json = ModifierValeur(json, "duelExpire", "0", false);
                File.WriteAllText(cheminFichier, json);
                CPH.SendMessage(nomJoueur + ", le défi en duel de " + duelDe + " a déjà expiré.");
                return true;
            }
            return ResoudreDuel(cheminFichier, json, nomJoueur, duelDe, maintenant);
        }

        bool   enRencontre = LireValeur(json, "enRencontre") == "true";
        string typeR       = LireValeur(json, "rencontreType");

        // Uniquement pour les rencontres alliées (pas les combats)
        if (!enRencontre || typeR == "combat" || typeR == "")
        {
            CPH.SendMessage(nomJoueur + ", tu n'as aucune offre alliée en attente !");
            return true;
        }

        long expire = long.Parse(LireValeur(json, "rencontreExpire"));
        if (expire > 0 && maintenant > expire)
        {
            CPH.SendMessage(nomJoueur + ", cette offre a déjà expiré — le timer va remettre ta quête en route !");
            return true;
        }

        bool enQuete = LireValeur(json, "enQuete") == "true";
        int  valeur  = int.Parse(LireValeur(json, "offreValeur"));
        string msgFinal = "";

        // === Handlers par type ===

        if (typeR == "marchand_potion")
        {
            int    prixPotion = valeur; // prix stocké à la création de l'offre (= ce qui a été affiché)
            int    ramJoueur  = int.Parse(LireValeur(json, "ram"));
            string inv        = LireValeurString(json, "inventaire");
            int    nbItems    = inv == "" ? 0 : inv.Split(',').Length;
            int    maxSac     = 8;

            if (ramJoueur < prixPotion)
            {
                CPH.SendMessage(nomJoueur + ", tu n'as pas assez de RAM (" + ramJoueur + "/" + prixPotion + ") pour acheter la Potion. Tape !refuser pour décliner.");
                return true;
            }
            if (nbItems >= maxSac)
            {
                CPH.SendMessage(nomJoueur + ", ton sac est plein ! Tape !refuser pour décliner.");
                return true;
            }
            json     = AjouterValeur(json, "ram", -prixPotion);
            string nouvInv = inv == "" ? "Potion" : inv + ",Potion";
            json     = ModifierValeurString(json, "inventaire", nouvInv);
            msgFinal = nomJoueur + ", tu achètes une Potion pour " + prixPotion + " RAM — elle est dans ton sac !";
        }
        else if (typeR == "vieux_sage")
        {
            string cfgA       = File.ReadAllText(CONFIG_ALLIES);
            int    chanceXP   = int.Parse(LireValeur(cfgA, "vieux_sage_chance_xp"));
            int    chanceItem = int.Parse(LireValeur(cfgA, "vieux_sage_chance_perte_item"));
            Random rng        = new Random();
            bool   gagne      = rng.Next(100) < chanceXP;
            bool   perd       = rng.Next(100) < chanceItem;

            if (gagne)
            {
                json     = AjouterValeur(json, "experience", valeur);
                json     = VerifierMonteeNiveau(json, nomJoueur);
                msgFinal  = nomJoueur + ", tu acceptes le marché du Vieux Sage — il te transmet son savoir ! +" + valeur + " XP !";
            }
            if (perd)
            {
                // Pool combiné : sac (chaque exemplaire) + équipement (3 slots) — le vol touche les deux
                string   inv   = LireValeurString(json, "inventaire");
                string[] sac   = inv == "" ? new string[0] : inv.Split(',');
                string[] slots = { "armeEquipee", "armureEquipee", "accessoireEquipe" };

                System.Collections.Generic.List<string> noms    = new System.Collections.Generic.List<string>();
                System.Collections.Generic.List<string> sources = new System.Collections.Generic.List<string>(); // "sac" ou nom du slot
                foreach (string it in sac)
                {
                    string t = it.Trim();
                    if (t != "") { noms.Add(t); sources.Add("sac"); }
                }
                foreach (string slot in slots)
                {
                    string eq = LireValeur(json, slot);
                    if (eq != "" && eq != "0") { noms.Add(eq); sources.Add(slot); }
                }

                if (noms.Count > 0)
                {
                    int    idx    = rng.Next(noms.Count);
                    string perdu  = noms[idx];
                    string source = sources[idx];

                    if (source == "sac")
                    {
                        string nouvInv = "";
                        bool   retire  = false;
                        foreach (string it in sac)
                        {
                            if (!retire && it.Trim() == perdu) { retire = true; continue; }
                            if (nouvInv != "") nouvInv += ",";
                            nouvInv += it.Trim();
                        }
                        json = ModifierValeurString(json, "inventaire", nouvInv);
                    }
                    else
                    {
                        json = ModifierValeur(json, source, "", true); // vide le slot équipé
                    }

                    msgFinal += (msgFinal != "" ? " Mais" : nomJoueur + ", tu acceptes le marché —")
                              + " le Vieux Sage s'empare de " + perdu + (source != "sac" ? " (équipé)" : "") + " en guise de paiement...";
                }
            }
            if (!gagne && !perd)
                msgFinal = nomJoueur + ", tu acceptes le marché — le Vieux Sage sourit et s'éclipse sans rien donner ni prendre...";
        }
        else if (typeR == "bonus_ram")
        {
            json     = AjouterValeur(json, "ram", valeur);
            msgFinal = nomJoueur + ", tu ramasses la bourse : +" + valeur + " RAM !";
        }
        else if (typeR == "alcove_chene")
        {
            int pvActuels   = int.Parse(LireValeur(json, "pvActuels"));
            int pvMax       = int.Parse(LireValeur(json, "pvMax"));
            int manaActuels = int.Parse(LireValeur(json, "manaActuels"));
            int manaMax     = int.Parse(LireValeur(json, "manaMax"));
            int pvSoin      = pvMax - pvActuels;
            int manaSoin    = manaMax - manaActuels;
            json     = ModifierValeur(json, "pvActuels",   pvMax.ToString(),   false);
            if (manaMax > 0)
                json = ModifierValeur(json, "manaActuels", manaMax.ToString(), false);
            string partMana = manaMax > 0 ? " | Mana : " + manaActuels + " → " + manaMax : "";
            msgFinal = nomJoueur + ", tu te reposes dans l'alcôve du chêne-serveur. PV : " + pvActuels + " → " + pvMax + partMana + " — Entièrement restauré !";
        }
        else if (typeR == "marchand_classe")
        {
            // Rappel des classes disponibles — la rencontre reste active, le joueur tape !choisirclasse
            string classeActuelle = LireValeur(json, "classe");
            CPH.SendMessage(nomJoueur + ", le Marchand de Classe te propose de changer ! Ta classe actuelle : " + classeActuelle + ". Tape !choisirclasse [nom] pour changer | !refuser pour décliner. Classes : Hexadécimeur · Cryptolame · Hackmancien · Firewaller · Algorythmancien");
            return true; // rencontre maintenue — elle sera résolue par !choisirclasse ou !refuser
        }
        else
        {
            msgFinal = nomJoueur + ", offre acceptée.";
        }

        // === Résoudre la rencontre et reprendre la quête ===
        long pauseDebut = long.Parse(LireValeur(json, "quetePauseDebut"));
        long totalPause = long.Parse(LireValeur(json, "queteTotalPause"));
        if (pauseDebut > 0)
            json = ModifierValeur(json, "queteTotalPause", (totalPause + (maintenant - pauseDebut)).ToString(), false);
        json = ModifierValeur(json, "enRencontre",     "false", false);
        json = ModifierValeur(json, "rencontreType",   "",      true);
        json = ModifierValeur(json, "rencontreExpire", "0",     false);
        json = ModifierValeur(json, "quetePauseDebut", "0",     false);

        File.WriteAllText(cheminFichier, json);
        CPH.SendMessage(msgFinal + (enQuete ? " Ta quête reprend !" : ""));
        return true;
    }

    // Résout un duel amical accepté : compare la puissance des deux joueurs, applique XP + compteurs.
    // nomJoueur = celui qui accepte (défié) · challenger = celui qui a lancé !duel
    private bool ResoudreDuel(string cheminB, string jsonB, string cible, string challenger, long maintenant)
    {
        string cheminA = Path.Combine(DOSSIER_JOUEURS, challenger.ToLower() + ".json");
        if (!File.Exists(cheminA))
        {
            jsonB = ModifierValeur(jsonB, "duelDe",     "",  true);
            jsonB = ModifierValeur(jsonB, "duelExpire", "0", false);
            File.WriteAllText(cheminB, jsonB);
            CPH.SendMessage(cible + ", ton adversaire " + challenger + " est introuvable — duel annulé.");
            return true;
        }
        string jsonA = File.ReadAllText(cheminA);

        // Re-validation « dans l'Antre » des deux joueurs
        // Le duel reste valable en quête, mais pas en plein combat, en repos ou à terre
        string invalide = "";
        if (LireValeur(jsonA, "enCombat") == "true" || long.Parse(LireValeur(jsonA, "reposCooldownFin")) > maintenant || int.Parse(LireValeur(jsonA, "pvActuels")) <= 0)
            invalide = challenger + " n'est plus en état de se battre";
        else if (LireValeur(jsonB, "enCombat") == "true" || long.Parse(LireValeur(jsonB, "reposCooldownFin")) > maintenant || int.Parse(LireValeur(jsonB, "pvActuels")) <= 0)
            invalide = cible + " n'est plus en état de se battre";
        if (invalide != "")
        {
            jsonA = EnsureChamp(jsonA, "duelVers", "", true);
            jsonA = ModifierValeur(jsonA, "duelVers",   "",  true);
            jsonB = ModifierValeur(jsonB, "duelDe",     "",  true);
            jsonB = ModifierValeur(jsonB, "duelExpire", "0", false);
            File.WriteAllText(cheminA, jsonA);
            File.WriteAllText(cheminB, jsonB);
            CPH.SendMessage("Duel annulé : " + invalide + ".");
            return true;
        }

        string cfgG     = File.ReadAllText(CONFIG_GLOBAL);
        string cfgCls   = File.ReadAllText(CONFIG_CLASSES);
        string cfgItems = File.ReadAllText(CONFIG_ITEMS);

        int scoreA = ScorePuissance(jsonA, cfgG, cfgCls, cfgItems);
        int scoreB = ScorePuissance(jsonB, cfgG, cfgCls, cfgItems);

        Random rng   = new Random();
        int    probaA = scoreA * 100 / (scoreA + scoreB);
        bool   challengerGagne = rng.Next(100) < probaA;

        int xpG = int.Parse(LireValeur(cfgG, "duel_recompense_xp_gagnant"));
        int xpP = int.Parse(LireValeur(cfgG, "duel_recompense_xp_perdant"));

        jsonA = EnsureChamp(jsonA, "duelsGagnes",     "0", false);
        jsonA = EnsureChamp(jsonA, "duelsPerdus",     "0", false);
        jsonA = EnsureChamp(jsonA, "duelVers",        "",  true);
        jsonA = EnsureChamp(jsonA, "duelCooldownFin", "0", false);
        jsonB = EnsureChamp(jsonB, "duelsGagnes",     "0", false);
        jsonB = EnsureChamp(jsonB, "duelsPerdus",     "0", false);

        string gagnant, perdant;
        if (challengerGagne)
        {
            gagnant = challenger; perdant = cible;
            jsonA = AjouterValeur(jsonA, "experience", xpG); jsonA = AjouterValeur(jsonA, "duelsGagnes", 1);
            jsonB = AjouterValeur(jsonB, "experience", xpP); jsonB = AjouterValeur(jsonB, "duelsPerdus", 1);
        }
        else
        {
            gagnant = cible; perdant = challenger;
            jsonB = AjouterValeur(jsonB, "experience", xpG); jsonB = AjouterValeur(jsonB, "duelsGagnes", 1);
            jsonA = AjouterValeur(jsonA, "experience", xpP); jsonA = AjouterValeur(jsonA, "duelsPerdus", 1);
        }

        // Cooldown validé uniquement maintenant que le duel a été accepté (sur le challenger)
        long cdFin = maintenant + long.Parse(LireValeur(cfgG, "duel_cooldown_secondes"));
        jsonA = ModifierValeur(jsonA, "duelCooldownFin", cdFin.ToString(), false);

        // Nettoyage des marqueurs de duel
        jsonA = ModifierValeur(jsonA, "duelVers",   "",  true);
        jsonB = ModifierValeur(jsonB, "duelDe",     "",  true);
        jsonB = ModifierValeur(jsonB, "duelExpire", "0", false);

        // Montée de niveau après gain d'XP
        jsonA = VerifierMonteeNiveau(jsonA, challenger);
        jsonB = VerifierMonteeNiveau(jsonB, cible);

        File.WriteAllText(cheminA, jsonA);
        File.WriteAllText(cheminB, jsonB);

        string jsonGagnant  = challengerGagne ? jsonA : jsonB;
        int    scoreGagnant = challengerGagne ? scoreA : scoreB;
        int    scorePerdant = challengerGagne ? scoreB : scoreA;
        string commentaire  = CommentaireDuel(jsonGagnant, cfgItems, gagnant, perdant, scoreGagnant, scorePerdant);

        CPH.SendMessage("⚔️ DUEL — " + challenger + " (puissance " + scoreA + ") vs " + cible + " (puissance " + scoreB
            + ") → 🏆 " + gagnant + " l'emporte ! +" + xpG + " XP pour " + gagnant + ", +" + xpP + " XP pour " + perdant + ".");
        CPH.SendMessage("📜 " + commentaire);
        return true;
    }

    // Commentaire de saveur expliquant l'issue du duel (le « pourquoi du comment » affiché au chat).
    // Ex. marge nette côté attaque → « ses coups d'une puissance brutale ont fini par briser la garde de X. »
    private string CommentaireDuel(string jsonGagnant, string cfgItems, string gagnant, string perdant, int scoreGagnant, int scorePerdant)
    {
        int marge = scoreGagnant - scorePerdant;
        if (marge < 0)
            return "Contre toute attente, " + gagnant + " renverse le pronostic face à " + perdant + " — la fortune sourit aux audacieux !";
        if (marge <= 20)   // puissances ~100..350 : 20 points, c'est serre

            return "Duel d'une intensité rare, décidé sur un ultime échange : " + gagnant + " l'emporte d'un cheveu sur " + perdant + ".";

        // Marge nette → on met en avant la stat dominante du vainqueur
        int atk = int.Parse(LireValeur(jsonGagnant, "bonusAttaque")) + GetBonusItems(jsonGagnant, cfgItems, "attaqueBonus");
        int ca  = int.Parse(LireValeur(jsonGagnant, "classeArmure")) + GetBonusItems(jsonGagnant, cfgItems, "caBonus");
        int agi = int.Parse(LireValeur(jsonGagnant, "agilite"));
        int cha = int.Parse(LireValeur(jsonGagnant, "charisme"))     + GetBonusItems(jsonGagnant, cfgItems, "charismeBonus");

        string raison;
        if (atk >= ca && atk >= agi && atk >= cha)
            raison = "ses coups d'une puissance brutale ont fini par briser la garde de " + perdant + ".";
        else if (ca >= atk && ca >= agi && ca >= cha)
            raison = "sa défense impénétrable a épuisé " + perdant + ", incapable d'y trouver la faille.";
        else if (agi >= atk && agi >= ca && agi >= cha)
            raison = "sa vivacité insaisissable a laissé " + perdant + " frapper dans le vide.";
        else
            raison = "son aplomb et son verbe tranchant ont déstabilisé " + perdant + " avant même le premier coup.";
        return gagnant + " s'impose nettement : " + raison;
    }

    // Puissance de combat d'un joueur (formule !combat, sans palier ennemi)
    // === PUISSANCE (duel) === identique a !combat : une echelle lineaire, sans plafond.
    // Le duel compare deja deux puissances en ratio — le clamp 20..80 d'avant ecrasait
    // les ecarts entre duellistes, il disparait.
    private int ScorePuissance(string json, string cfgG, string cfgCls, string cfgItems)
    {
        int pv   = int.Parse(LireValeur(json, "pvMax"));
        int ca   = int.Parse(LireValeur(json, "classeArmure")) + GetBonusItems(json, cfgItems, "caBonus");
        int atk  = int.Parse(LireValeur(json, "bonusAttaque")) + GetBonusItems(json, cfgItems, "attaqueBonus");
        int mana = int.Parse(LireValeur(json, "manaMax"))      + GetBonusItems(json, cfgItems, "manaBonus");
        int cha  = int.Parse(LireValeur(json, "charisme"))     + GetBonusItems(json, cfgItems, "charismeBonus");
        int agi  = int.Parse(LireValeur(json, "agilite"));
        int niv  = int.Parse(LireValeur(json, "niveau"));

        string classe     = LireValeur(json, "classe");
        string sousClasse = LireValeur(json, "sousClasse");
        int nbAtq = (sousClasse != "" && sousClasse != "0") ? int.Parse(LireValeur(cfgCls, sousClasse + "_nbAttaques")) : 0;
        if (nbAtq == 0) nbAtq = int.Parse(LireValeur(cfgCls, classe + "_nbAttaques"));
        if (nbAtq == 0) nbAtq = 1;

        int p = pv   * int.Parse(LireValeur(cfgG, "combat_poids_pv"))
              + ca   * int.Parse(LireValeur(cfgG, "combat_poids_ca"))
              + atk  * int.Parse(LireValeur(cfgG, "combat_poids_atk"))
              + niv  * int.Parse(LireValeur(cfgG, "combat_poids_niveau"))
              + mana / 10 * int.Parse(LireValeur(cfgG, "combat_poids_mana"))
              + cha  * int.Parse(LireValeur(cfgG, "combat_poids_charisme"))
              + agi  * int.Parse(LireValeur(cfgG, "combat_poids_agilite"))
              + (nbAtq - 1) * int.Parse(LireValeur(cfgG, "combat_puissance_par_attaque"));

        int modifCl; int.TryParse(LireValeur(cfgCls, classe + "_puissanceModif"), out modifCl);
        p += modifCl;
        p += BonusSousClasse(cfgCls, sousClasse, "bonusPuissance");

        // Compagnon : il combat aux cotes de son maitre, y compris en duel amical.
        if (LireValeurString(json, "compagnonActif") != "")
            p += int.Parse(LireValeur(cfgG, "combat_puissance_compagnon"));

        return p < 1 ? 1 : p;
    }

    // Bonus plat accorde par la sous-classe (config_classes). 0 si la cle n'existe pas :
    // toutes les sous-classes n'agissent pas sur tous les leviers.
    private int BonusSousClasse(string cfgCls, string sousClasse, string cle)
    {
        if (sousClasse == "" || sousClasse == "0") return 0;
        int v;
        return int.TryParse(LireValeur(cfgCls, sousClasse + "_" + cle), out v) ? v : 0;
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

    private int Clamp(int v, int min, int max)
    {
        return v < min ? min : (v > max ? max : v);
    }

    // Insère un champ s'il est absent (anciens profils)
    private string EnsureChamp(string json, string cle, string valeurDefaut, bool estTexte)
    {
        if (json.Contains("\"" + cle + "\"")) return json;
        int    pos = json.LastIndexOf('}');
        string val = estTexte ? "\"" + valeurDefaut + "\"" : valeurDefaut;
        return json.Substring(0, pos) + ",\n  \"" + cle + "\": " + val + "\n}";
    }

    private string VerifierMonteeNiveau(string json, string nomJoueur)
    {
        int niveauActuel  = int.Parse(LireValeur(json, "niveau"));
        int xp            = int.Parse(LireValeur(json, "experience"));
        int nouveauNiveau = CalculerNiveau(xp);
        if (nouveauNiveau > niveauActuel)
        {
            json = ModifierValeur(json, "niveau", nouveauNiveau.ToString(), false);
            json = AppliquerBonusNiveau(json, nouveauNiveau);
            CPH.SendMessage(MessageNiveau(nomJoueur, nouveauNiveau));
        }
        return json;
    }

    private int CalculerNiveau(int xp)
    {
        string cfg    = File.ReadAllText(CONFIG_LEVEL);
        int niveauMax = int.Parse(LireValeur(cfg, "niveauMax"));
        for (int i = niveauMax; i >= 2; i--)
            if (xp >= int.Parse(LireValeur(cfg, "niveau_" + i + "_xp"))) return i;
        return 1;
    }

    private string AppliquerBonusNiveau(string json, int niveau)
    {
        string cfg        = File.ReadAllText(CONFIG_LEVEL);
        int pvBonus       = int.Parse(LireValeur(cfg, "niveau_" + niveau + "_pvBonus"));
        int caBonus       = int.Parse(LireValeur(cfg, "niveau_" + niveau + "_caBonus"));
        int ramBonus      = int.Parse(LireValeur(cfg, "niveau_" + niveau + "_ramBonus"));
        int charismeBonus = int.Parse(LireValeur(cfg, "niveau_" + niveau + "_charismeBonus"));
        if (pvBonus > 0) { json = AjouterValeur(json, "pvMax", pvBonus); json = AjouterValeur(json, "pvActuels", pvBonus); }
        if (caBonus       > 0) json = AjouterValeur(json, "classeArmure", caBonus);
        if (ramBonus      > 0) json = AjouterValeur(json, "ram",          ramBonus);
        if (charismeBonus > 0) json = AjouterValeur(json, "charisme",     charismeBonus);
        return json;
    }

    // Message unique de montée de niveau — identique dans tous les fichiers qui donnent de l'XP
    private string MessageNiveau(string nomJoueur, int niveau)
    {
        string cfg   = File.ReadAllText(CONFIG_LEVEL);
        string bonus = LireValeur(cfg, "niveau_" + niveau + "_message");
        if (bonus == "0") bonus = "";

        int stats = int.Parse(LireValeur(cfg, "niveau_" + niveau + "_pvBonus"))
                  + int.Parse(LireValeur(cfg, "niveau_" + niveau + "_caBonus"))
                  + int.Parse(LireValeur(cfg, "niveau_" + niveau + "_ramBonus"))
                  + int.Parse(LireValeur(cfg, "niveau_" + niveau + "_charismeBonus"));

        string msg = "🎉 " + nomJoueur + " gagne 1 niveau (niveau " + niveau + ")";
        msg += stats > 0 && bonus != ""
             ? ", augmente sa stat de " + bonus + " et progresse vers le sommet !"
             : " et progresse vers le sommet !" + (bonus != "" ? " " + bonus : "");

        if (niveau >= int.Parse(LireValeur(cfg, "niveauMax"))) msg += " 👑 Niveau maximum atteint !";
        return msg;
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

    private string AjouterValeur(string json, string cle, int montant)
    {
        string marqueur = "\"" + cle + "\": ";
        int posDebut    = json.IndexOf(marqueur);
        if (posDebut == -1) return json;
        posDebut       += marqueur.Length;
        int posFin      = json.IndexOfAny(new char[] { ',', '\n', '}' }, posDebut);
        string ancienneStr = json.Substring(posDebut, posFin - posDebut).Trim().Trim('"');
        int ancienne = int.TryParse(ancienneStr, out int v) ? v : 0;
        return json.Substring(0, posDebut) + (ancienne + montant).ToString() + json.Substring(posDebut + (posFin - posDebut));
    }
}
