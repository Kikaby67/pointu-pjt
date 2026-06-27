using System;
using System.IO;
using System.Collections.Generic;

// Timer ArenaCheck (toutes les ~10s) : transition recrutement→combat (initiative),
// sauts de tour AFK (2 min) et riposte du boss.
public class CPHInline
{
    private const string DOSSIER_JOUEURS = @"C:\Users\Florian\pjt\Pointu-PJT\Donnees\joueurs";
    private const string CONFIG_GLOBAL   = @"C:\Users\Florian\pjt\Pointu-PJT\Donnees\config_global.json";
    private const string CONFIG_ENNEMIS  = @"C:\Users\Florian\pjt\Pointu-PJT\Donnees\config_ennemis.json";
    private const string CONFIG_ITEMS    = @"C:\Users\Florian\pjt\Pointu-PJT\Donnees\config_items.json";
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
            // Ne garder que les inscrits valides et vivants
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
            File.WriteAllText(ETAT_GLOBAL, etat);

            CPH.SendMessage("⚔️ LE COMBAT COMMENCE ! " + bossNom + " : " + pvMax + " PV. Ordre d'initiative : " + ordre.Replace(",", " → ") + ".");
            CPH.SendMessage("➡️ À " + vivants[0] + " de frapper en premier ! Tape !attaquer (2 min sinon ton tour saute).");
            return true;
        }

        // === PHASE COMBAT ===
        if (phase != "combat") return true;

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

        // --- Riposte du boss : tout le monde a frappé ---
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
                CPH.SendMessage("➡️ Au tour de " + ordreArr[suivant] + " ! (!attaquer)");
            }
        }
        return true;
    }

    // Le boss frappe tous les joueurs : X = degatsMax × nb joueurs, réparti INVERSEMENT à la CA.
    private void RiposteBoss(string etat, string[] ordreArr, string cfgG, long maintenant)
    {
        string bossNom = LireValeurString(etat, "bossNom");
        string cfgE = File.ReadAllText(CONFIG_ENNEMIS);
        int degatsMax = int.Parse(LireValeur(cfgE, bossNom + "_degatsMax"));
        if (degatsMax <= 0) degatsMax = int.Parse(LireValeur(cfgG, "ennemi_degats_defaut"));

        int n = ordreArr.Length;
        int total = degatsMax * n;

        // CA effective de chaque joueur + poids inverse
        string cfgItems = File.ReadAllText(CONFIG_ITEMS);
        double[] poids = new double[n];
        int[] caEff = new int[n];
        double sommePoids = 0;
        for (int i = 0; i < n; i++)
        {
            string pj = File.ReadAllText(Path.Combine(DOSSIER_JOUEURS, ordreArr[i] + ".json"));
            int ca = int.Parse(LireValeur(pj, "classeArmure")) + GetBonusItems(pj, "caBonus", cfgItems);
            if (ca < 1) ca = 1;
            caEff[i] = ca;
            poids[i] = 1.0 / ca;       // CA haute → poids faible → moins de dégâts
            sommePoids += poids[i];
        }

        List<string> survivants = new List<string>();
        string resume = "";
        string morts = "";
        for (int i = 0; i < n; i++)
        {
            int part = (int)Math.Round(total * poids[i] / sommePoids);
            if (part < 1) part = 1;

            string chemin = Path.Combine(DOSSIER_JOUEURS, ordreArr[i] + ".json");
            string pj = File.ReadAllText(chemin);
            int pv = int.Parse(LireValeur(pj, "pvActuels"));
            int nvPv = Math.Max(0, pv - part);
            pj = ModifierValeur(pj, "pvActuels", nvPv.ToString(), false);
            File.WriteAllText(chemin, pj);

            resume = resume == "" ? ordreArr[i] + " -" + part + "PV" : resume + " · " + ordreArr[i] + " -" + part + "PV";
            if (nvPv <= 0) morts = morts == "" ? ordreArr[i] : morts + ", " + ordreArr[i];
            else survivants.Add(ordreArr[i]);
        }

        CPH.SendMessage("💢 " + bossNom + " déchaîne sa fureur sur le groupe (" + total + " dégâts répartis) ! " + resume);
        if (morts != "") CPH.SendMessage("☠️ Tombé(s) au combat : " + morts + " (repos nécessaire).");

        // Défaite : plus aucun survivant
        if (survivants.Count == 0)
        {
            etat = ReinitEtat(etat);
            File.WriteAllText(ETAT_GLOBAL, etat);
            CPH.DisableTimer("ArenaCheck");
            CPH.SendMessage("🏴 " + bossNom + " a terrassé tous les aventuriers... Arbonet pleure ses héros. La prochaine fois, peut-être.");
            return;
        }

        // Nouveau round avec les survivants
        int timeout = int.Parse(LireValeur(cfgG, "arene_tour_timeout_secondes"));
        string ordre = string.Join(",", survivants.ToArray());
        etat = ModifierValeurString(etat, "ordre", ordre);
        etat = ModifierValeur(etat, "tourIndex", "0", false);
        etat = ModifierValeur(etat, "tourDeadline", (maintenant + timeout).ToString(), false);
        File.WriteAllText(ETAT_GLOBAL, etat);

        int pvBoss = int.Parse(LireValeur(etat, "bossPVActuels"));
        int pvMax  = int.Parse(LireValeur(etat, "bossPVMax"));
        CPH.SendMessage("🔁 Nouveau tour ! " + bossNom + " : " + pvBoss + "/" + pvMax + " PV. ➡️ À " + survivants[0] + " de frapper ! (!attaquer)");
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
        return etat;
    }

    private int GetBonusItems(string json, string stat, string cfgItems)
    {
        string[] slots    = { "armeEquipee", "armureEquipee", "accessoireEquipe" };
        int total = 0;
        foreach (string slot in slots)
        {
            string item = LireValeur(json, slot);
            if (item != "" && item != "0")
                total += int.Parse(LireValeur(cfgItems, item + "_" + stat));
        }
        return total;
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
