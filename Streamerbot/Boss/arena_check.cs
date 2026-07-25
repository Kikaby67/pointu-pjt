// sb-action: arene check
// sb-subaction-id: 84f524dc-f4bb-435b-903d-8fc62882c912
using System;
using System.IO;
using System.Collections.Generic;

// Timer ArenaCheck (toutes les ~10s) : transition recrutement→combat (initiative),
// victoire centralisée (boss à 0 PV), sauts de tour AFK et riposte du boss (buffs/débuffs/ciblage).
public class CPHInline
{
    private const string DOSSIER_JOUEURS = @"C:\Users\Florian\pjt\Pointu-PJT\Donnees\joueurs";
    private const string CONFIG_GLOBAL   = @"C:\Users\Florian\pjt\Pointu-PJT\Donnees\config_global.json";
    private const string CONFIG_ENNEMIS  = @"C:\Users\Florian\pjt\Pointu-PJT\Donnees\config_ennemis.json";
    private const string CONFIG_ITEMS    = @"C:\Users\Florian\pjt\Pointu-PJT\Donnees\config_items.json";
    private const string CONFIG_QUETES   = @"C:\Users\Florian\pjt\Pointu-PJT\Donnees\config_quetes.json";
    private const string CONFIG_LEVEL    = @"C:\Users\Florian\pjt\Pointu-PJT\Donnees\config_level.json";
    private const string ETAT_GLOBAL     = @"C:\Users\Florian\pjt\Pointu-PJT\Donnees\etat_global.json";

    public bool Execute()
    {
        string etat = File.ReadAllText(ETAT_GLOBAL);
        if (LireValeur(etat, "bossActif") != "true")
        {
            CPH.DisableTimer("ArenaCheck");
            return true;
        }

        long maintenant = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        string cfgG  = File.ReadAllText(CONFIG_GLOBAL);
        string phase = LireValeurString(etat, "bossPhase");

        // === PHASE RECRUTEMENT : fin des 5 min → démarrage du combat ===
        if (phase == "recrutement")
        {
            long areneFin = long.Parse(LireValeur(etat, "areneFin"));
            if (maintenant < areneFin) return true;

            string ordreBrut = LireValeurString(etat, "ordre");
            List<string> vivants = new List<string>();
            List<int> agis = new List<int>();
            if (ordreBrut != "")
            {
                foreach (string p in ordreBrut.Split(','))
                {
                    string chemin = Path.Combine(DOSSIER_JOUEURS, p + ".json");
                    if (!File.Exists(chemin)) continue;
                    string pj = File.ReadAllText(chemin);
                    if (int.Parse(LireValeur(pj, "pvActuels")) <= 0) continue;
                    vivants.Add(p);
                    agis.Add(int.Parse(LireValeur(pj, "agilite")));
                }
            }

            if (vivants.Count == 0)
            {
                string nomBoss = LireValeurString(etat, "bossNom");
                etat = ReinitEtat(etat);
                File.WriteAllText(ETAT_GLOBAL, etat);
                CPH.DisableTimer("ArenaCheck");
                CPH.SendMessage("🌫️ Personne n'a osé affronter " + nomBoss + "... La menace s'éloigne, pour cette fois.");
                return true;
            }

            // Ordre d'initiative : agilité décroissante, égalité = ordre d'arrivée (tri stable)
            for (int i = 1; i < vivants.Count; i++)
            {
                string p = vivants[i]; int a = agis[i];
                int j = i - 1;
                while (j >= 0 && agis[j] < a)
                {
                    vivants[j + 1] = vivants[j]; agis[j + 1] = agis[j]; j--;
                }
                vivants[j + 1] = p; agis[j + 1] = a;
            }

            string bossNom = LireValeurString(etat, "bossNom");
            string cfgE    = File.ReadAllText(CONFIG_ENNEMIS);
            int pvBase  = int.Parse(LireValeur(cfgE, bossNom + "_pv"));
            if (pvBase <= 0) pvBase = 300;
            int parPart = int.Parse(LireValeur(cfgG, "boss_pv_par_participant"));
            int pvMax   = pvBase + parPart * vivants.Count;
            int timeout = int.Parse(LireValeur(cfgG, "arene_tour_timeout_secondes"));

            string ordre = string.Join(",", vivants.ToArray());
            string participants = "";
            foreach (string p in vivants) participants = participants == "" ? p + ":0" : participants + "," + p + ":0";

            etat = ModifierValeurString(etat, "bossPhase", "combat");
            etat = ModifierValeur(etat, "bossPVMax", pvMax.ToString(), false);
            etat = ModifierValeur(etat, "bossPVActuels", pvMax.ToString(), false);
            etat = ModifierValeurString(etat, "ordre", ordre);
            etat = ModifierValeur(etat, "tourIndex", "0", false);
            etat = ModifierValeur(etat, "tourDeadline", (maintenant + timeout).ToString(), false);
            etat = ModifierValeurString(etat, "participants", participants);
            etat = ResetEffetsRound(etat);
            File.WriteAllText(ETAT_GLOBAL, etat);

            CPH.SendMessage("⚔️ LE COMBAT COMMENCE ! " + bossNom + " : " + pvMax + " PV. Ordre d'initiative : " + ordre.Replace(",", " → ") + ".");
            CPH.SendMessage("➡️ À " + vivants[0] + " d'agir ! !attaquer · !defense · !soin · !discuter · !fuir · ou ta capacité de classe (2 min sinon ton tour saute).");
            return true;
        }

        // === PHASE COMBAT ===
        if (phase != "combat") return true;

        // Victoire : le boss est tombé à 0 PV (coup porté par une commande d'attaque) → récompenses
        if (int.Parse(LireValeur(etat, "bossPVActuels")) <= 0)
        {
            string participants = LireValeurString(etat, "participants");
            string bossNom      = LireValeurString(etat, "bossNom");
            DistribuerRecompenses(participants, bossNom, cfgG);
            etat = ReinitEtat(etat);
            File.WriteAllText(ETAT_GLOBAL, etat);
            CPH.DisableTimer("ArenaCheck");
            return true;
        }

        string ordreC = LireValeurString(etat, "ordre");
        string[] ordreArr = ordreC == "" ? new string[0] : ordreC.Split(',');
        int tourIndex = int.Parse(LireValeur(etat, "tourIndex"));

        if (ordreArr.Length == 0)
        {
            etat = ReinitEtat(etat);
            File.WriteAllText(ETAT_GLOBAL, etat);
            CPH.DisableTimer("ArenaCheck");
            return true;
        }

        // --- Riposte du boss : tout le monde a agi ---
        if (tourIndex >= ordreArr.Length)
        {
            RiposteBoss(etat, ordreArr, cfgG, maintenant);
            return true;
        }

        // --- Saut de tour AFK (> 2 min) ---
        long deadline = long.Parse(LireValeur(etat, "tourDeadline"));
        if (maintenant >= deadline)
        {
            string afk = ordreArr[tourIndex];
            CPH.SendMessage(afk + ", la peur est compréhensible mais je crois en toi ! (tour passé)");

            int suivant = tourIndex + 1;
            if (suivant >= ordreArr.Length)
            {
                etat = ModifierValeur(etat, "tourIndex", suivant.ToString(), false);
                etat = ModifierValeur(etat, "tourDeadline", maintenant.ToString(), false);
                File.WriteAllText(ETAT_GLOBAL, etat);
                CPH.SendMessage("⏳ " + LireValeurString(etat, "bossNom") + " prépare une attaque dévastatrice !");
            }
            else
            {
                int timeout = int.Parse(LireValeur(cfgG, "arene_tour_timeout_secondes"));
                etat = ModifierValeur(etat, "tourIndex", suivant.ToString(), false);
                etat = ModifierValeur(etat, "tourDeadline", (maintenant + timeout).ToString(), false);
                File.WriteAllText(ETAT_GLOBAL, etat);
                CPH.SendMessage("➡️ Au tour de " + ordreArr[suivant] + " ! (!attaquer, !defense, !soin, !discuter, !fuir ou ta capacité)");
            }
        }
        return true;
    }

    // Le boss frappe : total = degatsMax × nb joueurs, modulé par les buffs/débuffs et le ciblage.
    private void RiposteBoss(string etat, string[] ordreArr, string cfgG, long maintenant)
    {
        string bossNom = LireValeurString(etat, "bossNom");
        string cfgE = File.ReadAllText(CONFIG_ENNEMIS);
        int degatsMax = int.Parse(LireValeur(cfgE, bossNom + "_degatsMax"));
        if (degatsMax <= 0) degatsMax = int.Parse(LireValeur(cfgG, "ennemi_degats_defaut"));

        int n = ordreArr.Length;
        int total = degatsMax * n;

        // Débuff d'attaque du boss (sérénade insultante)
        if (int.Parse(LireValeur(etat, "bossAtkMalusTours")) > 0)
        {
            int red = int.Parse(LireValeur(cfgG, "boss_serenade_reduction_pct"));
            total = total * (100 - red) / 100;
        }
        if (total < 1) total = 1;

        string defenseurs  = LireValeurString(etat, "defenseurs");
        string provocateur = LireValeurString(etat, "provocateur");
        string bribeSafe   = LireValeurString(etat, "bribeSafe");
        string bribeCible  = LireValeurString(etat, "bribeCible");
        bool   egideActif  = int.Parse(LireValeur(etat, "buffCaTours")) > 0;
        int    egideCa     = int.Parse(LireValeur(cfgG, "boss_egide_ca_bonus"));
        int    defenseCa   = int.Parse(LireValeur(cfgG, "boss_defense_ca_bonus"));
        string cfgItems    = File.ReadAllText(CONFIG_ITEMS);

        int[]  degParJoueur = new int[n];
        string focusMsg = "";

        int idxProvoc = IndexTab(ordreArr, provocateur);
        int idxCible  = IndexTab(ordreArr, bribeCible);

        if (idxProvoc >= 0)
        {
            int fac = int.Parse(LireValeur(cfgG, "boss_provocation_facteur_pct"));
            degParJoueur[idxProvoc] = Math.Max(1, total * fac / 100);
            focusMsg = " " + ordreArr[idxProvoc] + " encaisse seul pour protéger le groupe !";
        }
        else if (idxCible >= 0)
        {
            degParJoueur[idxCible] = Math.Max(1, total);
            focusMsg = " " + bossNom + " se focalise sur " + ordreArr[idxCible] + " (marché échoué) !";
        }
        else
        {
            double[] poids = new double[n];
            double sommePoids = 0;
            for (int i = 0; i < n; i++)
            {
                if (bribeSafe != "" && ordreArr[i] == bribeSafe) { poids[i] = 0; continue; }
                string cheminPj = Path.Combine(DOSSIER_JOUEURS, ordreArr[i] + ".json");
                if (!File.Exists(cheminPj)) { poids[i] = 0; continue; }   // profil disparu → épargné
                string pj = File.ReadAllText(cheminPj);
                int ca = int.Parse(LireValeur(pj, "classeArmure")) + GetBonusItems(pj, "caBonus", cfgItems);
                if (egideActif) ca += egideCa;
                if (defenseurs != "" && ("," + defenseurs + ",").Contains("," + ordreArr[i] + ",")) ca += defenseCa;
                if (ca < 1) ca = 1;
                poids[i] = 1.0 / ca;
                sommePoids += poids[i];
            }
            if (sommePoids > 0)
                for (int i = 0; i < n; i++)
                    if (poids[i] > 0)
                        degParJoueur[i] = Math.Max(1, (int)Math.Round(total * poids[i] / sommePoids));
            if (bribeSafe != "" && IndexTab(ordreArr, bribeSafe) >= 0)
                focusMsg = " " + bribeSafe + " a été épargné (marché conclu).";
        }

        List<string> survivants = new List<string>();
        string resume = "";
        string morts  = "";
        for (int i = 0; i < n; i++)
        {
            string chemin = Path.Combine(DOSSIER_JOUEURS, ordreArr[i] + ".json");
            if (!File.Exists(chemin)) continue;
            string pj = File.ReadAllText(chemin);
            int part = degParJoueur[i];
            int pv   = int.Parse(LireValeur(pj, "pvActuels"));
            int nvPv = Math.Max(0, pv - part);
            if (part > 0)
            {
                pj = ModifierValeur(pj, "pvActuels", nvPv.ToString(), false);
                File.WriteAllText(chemin, pj);
                resume = resume == "" ? ordreArr[i] + " -" + part + "PV" : resume + " · " + ordreArr[i] + " -" + part + "PV";
            }
            if (nvPv <= 0) morts = morts == "" ? ordreArr[i] : morts + ", " + ordreArr[i];
            else survivants.Add(ordreArr[i]);
        }

        if (resume == "") resume = "le groupe esquive l'assaut !";
        CPH.SendMessage("💢 " + bossNom + " riposte (" + total + " dégâts) !" + focusMsg + " " + resume);
        if (morts != "") CPH.SendMessage("☠️ Tombé(s) au combat : " + morts + " (repos nécessaire).");

        // Fin de round : décrément des durées + reset des effets ponctuels de ce round
        etat = DecrementTour(etat, "buffCaTours");
        etat = DecrementTour(etat, "buffAtkTours");
        etat = DecrementTour(etat, "buffDesTours");
        etat = DecrementTour(etat, "bossCaMalusTours");
        etat = DecrementTour(etat, "bossAtkMalusTours");
        etat = ModifierValeurString(etat, "defenseurs",  "");
        etat = ModifierValeurString(etat, "provocateur", "");
        etat = ModifierValeurString(etat, "bribeSafe",   "");
        etat = ModifierValeurString(etat, "bribeCible",  "");

        if (survivants.Count == 0)
        {
            etat = ReinitEtat(etat);
            File.WriteAllText(ETAT_GLOBAL, etat);
            CPH.DisableTimer("ArenaCheck");
            CPH.SendMessage("🏴 " + bossNom + " a terrassé tous les aventuriers... Arbonet pleure ses héros. La prochaine fois, peut-être.");
            return;
        }

        int timeout = int.Parse(LireValeur(cfgG, "arene_tour_timeout_secondes"));
        string ordre = string.Join(",", survivants.ToArray());
        etat = ModifierValeurString(etat, "ordre", ordre);
        etat = ModifierValeur(etat, "tourIndex", "0", false);
        etat = ModifierValeur(etat, "tourDeadline", (maintenant + timeout).ToString(), false);
        File.WriteAllText(ETAT_GLOBAL, etat);

        int pvBoss = int.Parse(LireValeur(etat, "bossPVActuels"));
        int pvMax  = int.Parse(LireValeur(etat, "bossPVMax"));
        CPH.SendMessage("🔁 Nouveau tour ! " + bossNom + " : " + pvBoss + "/" + pvMax + " PV. ➡️ À " + survivants[0] + " d'agir ! (!attaquer, !defense, !soin, !discuter, !fuir ou ta capacité)");
    }

    // Récompenses : base à tous les participants, bonus + loot légendaire au meilleur dégâteur
    private void DistribuerRecompenses(string participants, string bossNom, string cfgG)
    {
        if (participants == "")
        {
            CPH.SendMessage("🏆 " + bossNom + " est vaincu !");
            return;
        }
        string[] parts = participants.Split(',');

        string topPseudo = "";
        int topDmg = -1;
        foreach (string p in parts)
        {
            string[] kv = p.Split(':');
            if (kv.Length != 2) continue;
            int d = int.TryParse(kv[1], out int v) ? v : 0;
            if (d > topDmg) { topDmg = d; topPseudo = kv[0]; }
        }

        int baseXp   = int.Parse(LireValeur(cfgG, "boss_recompense_base_xp"));
        int baseRam  = int.Parse(LireValeur(cfgG, "boss_recompense_base_ram"));
        int bonusXp  = int.Parse(LireValeur(cfgG, "boss_top_bonus_xp"));
        int bonusRam = int.Parse(LireValeur(cfgG, "boss_top_bonus_ram"));
        int maxSac   = int.Parse(LireValeur(cfgG, "max_sac"));

        string loot = "";
        string poolName = LireValeurString(cfgG, "boss_loot_pool");
        string lootRaw  = LireValeurString(File.ReadAllText(CONFIG_QUETES), poolName);
        if (lootRaw != "")
        {
            string[] pool = lootRaw.Split(',');
            loot = pool[new Random().Next(pool.Length)].Trim();
        }

        int nbParticipants = 0;
        foreach (string p in parts)
        {
            string[] kv = p.Split(':');
            if (kv.Length != 2) continue;
            string pseudo = kv[0];
            string chemin = Path.Combine(DOSSIER_JOUEURS, pseudo + ".json");
            if (!File.Exists(chemin)) continue;
            nbParticipants++;

            string pj = File.ReadAllText(chemin);
            pj = AjouterValeur(pj, "experience", baseXp);
            pj = AjouterValeur(pj, "ram", baseRam);

            if (pseudo == topPseudo)
            {
                pj = AjouterValeur(pj, "experience", bonusXp);
                pj = AjouterValeur(pj, "ram", bonusRam);
                if (loot != "")
                {
                    string inv = LireValeurString(pj, "inventaire");
                    int nb = inv == "" ? 0 : inv.Split(',').Length;
                    if (nb < maxSac)
                    {
                        string nouvInv = inv == "" ? loot : inv + "," + loot;
                        pj = ModifierValeurString(pj, "inventaire", nouvInv);
                    }
                    else loot = "";
                }
            }
            pj = VerifierMonteeNiveau(pj, pseudo);
            File.WriteAllText(chemin, pj);
        }

        CPH.SendMessage("🏆 " + bossNom + " est VAINCU par la communauté (" + nbParticipants + " combattants) ! Chacun gagne " + baseXp + " XP et " + baseRam + " RAM.");
        if (topPseudo != "")
            CPH.SendMessage("👑 Meilleur combattant : " + topPseudo + " (" + topDmg + " dégâts) → +" + bonusXp + " XP, +" + bonusRam + " RAM" + (loot != "" ? " et " + loot + " (légendaire) !" : " !"));
    }

    private int IndexTab(string[] arr, string val)
    {
        if (val == "" || val == "0") return -1;
        for (int i = 0; i < arr.Length; i++) if (arr[i] == val) return i;
        return -1;
    }

    private string DecrementTour(string etat, string cle)
    {
        int v = int.Parse(LireValeur(etat, cle));
        if (v > 0) v--;
        return ModifierValeur(etat, cle, v.ToString(), false);
    }

    private string ResetEffetsRound(string etat)
    {
        etat = ModifierValeur(etat, "buffCaTours",      "0", false);
        etat = ModifierValeur(etat, "buffAtkTours",     "0", false);
        etat = ModifierValeur(etat, "buffDesTours",     "0", false);
        etat = ModifierValeur(etat, "bossCaMalusTours", "0", false);
        etat = ModifierValeur(etat, "bossAtkMalusTours","0", false);
        etat = ModifierValeurString(etat, "defenseurs",  "");
        etat = ModifierValeurString(etat, "provocateur", "");
        etat = ModifierValeurString(etat, "bribeSafe",   "");
        etat = ModifierValeurString(etat, "bribeCible",  "");
        return etat;
    }

    private string ReinitEtat(string etat)
    {
        etat = ModifierValeur(etat, "bossActif", "false", false);
        etat = ModifierValeurString(etat, "bossPhase", "");
        etat = ModifierValeurString(etat, "bossNom", "");
        etat = ModifierValeur(etat, "bossPVMax", "0", false);
        etat = ModifierValeur(etat, "bossPVActuels", "0", false);
        etat = ModifierValeur(etat, "areneFin", "0", false);
        etat = ModifierValeurString(etat, "ordre", "");
        etat = ModifierValeur(etat, "tourIndex", "0", false);
        etat = ModifierValeur(etat, "tourDeadline", "0", false);
        etat = ModifierValeurString(etat, "participants", "");
        etat = ResetEffetsRound(etat);
        return etat;
    }

    private int GetBonusItems(string json, string stat, string cfgItems)
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

    private string VerifierMonteeNiveau(string json, string nomJoueur)
    {
        int niveauActuel  = int.Parse(LireValeur(json, "niveau"));
        int nouvelXP      = int.Parse(LireValeur(json, "experience"));
        int nouveauNiveau = CalculerNiveau(nouvelXP);
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

    // ===== Helpers JSON =====
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
