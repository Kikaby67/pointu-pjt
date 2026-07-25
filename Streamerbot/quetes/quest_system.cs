// sb-action: !quete
// sb-subaction-id: bb53602e-8479-4cfb-bf6e-b729662a41fe
using System;
using System.IO;
public class CPHInline
{
    private const string DOSSIER_JOUEURS = @"C:\Users\Florian\pjt\Pointu-PJT\Donnees\joueurs";
    private const string CONFIG_QUETES   = @"C:\Users\Florian\pjt\Pointu-PJT\Donnees\config_quetes.json";
    private const string CONFIG_GLOBAL   = @"C:\Users\Florian\pjt\Pointu-PJT\Donnees\config_global.json";
    private const string CONFIG_LEVEL    = @"C:\Users\Florian\pjt\Pointu-PJT\Donnees\config_level.json";

    public bool Execute()
    {
        try { return ExecuteInner(); }
        catch (Exception e)
        {
            CPH.LogWarn("QUETE ERREUR: " + e.GetType().Name + " — " + e.Message);
            CPH.SendMessage("⚠ Erreur !quete : " + e.GetType().Name + " — " + e.Message);
            return true;
        }
    }

    private bool ExecuteInner()
    {
        CPH.LogWarn("QUETE: début exec pour " + (args.ContainsKey("user") ? args["user"].ToString() : "???"));
        string nomJoueur = args["user"].ToString();
        string cheminFichier = Path.Combine(DOSSIER_JOUEURS, nomJoueur.ToLower() + ".json");

        if (!File.Exists(cheminFichier))
        {
            CPH.SendMessage(nomJoueur + ", tape !rejoindre pour commencer ton parcours d'aventurier dans l'Antre de Pointu. Tu pourras ensuite choisir ta classe et commencer à faire des quêtes !");
            return true;
        }

        string json = File.ReadAllText(cheminFichier);
        long maintenant = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        long cooldownFin = long.Parse(LireValeur(json, "queteCooldownFin"));
        if (cooldownFin > 0 && maintenant < cooldownFin)
        {
            int minutesRestantes = (int)Math.Ceiling((cooldownFin - maintenant) / 60.0);
            CPH.SendMessage(nomJoueur + ", tu récupères dans l'Antre après ta défaite. Encore " + minutesRestantes + " minute(s) avant de pouvoir repartir en quête !");
            return true;
        }

        if (LireValeur(json, "classeChoisie") != "true")
        {
            CPH.SendMessage(nomJoueur + ", choisis ta classe avant de commencer une quête. Tape !choisirclasse suivi du nom de la classe (ex: !choisirclasse hexadécimeur).");
            return true;
        }

        if (LireValeur(json, "enCombat") == "true")
        {
            string ennemiEnCours = LireValeur(json, "ennemiNom");
            CPH.SendMessage(nomJoueur + ", tu es face à " + ennemiEnCours + " ! Tape !combat pour te battre, !discuter pour parlementer ou !fuir pour t'échapper.");
            return true;
        }

        if (int.Parse(LireValeur(json, "pvActuels")) <= 0)
        {
            CPH.SendMessage(nomJoueur + ", tu es à terre (0 PV). Repose-toi dans l'Antre (!repos), soigne-toi (!soin) ou bois une !utiliser Potion avant de repartir en quête.");
            return true;
        }

        int seed = ((int)(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() % int.MaxValue) ^ nomJoueur.GetHashCode()) & int.MaxValue;
        Random rng = new Random(seed);

        if (LireValeur(json, "enQuete") == "true")
        {
            if (LireValeur(json, "enRencontre") == "true")
            {
                string typeR = LireValeur(json, "rencontreType");
                if (typeR == "combat")
                {
                    string ennemNom = LireValeur(json, "ennemiNom");
                    CPH.SendMessage(nomJoueur + ", tu es face à " + ennemNom + " ! Tape !combat pour te battre, !discuter pour parlementer ou !fuir pour t'échapper.");
                }
                else
                {
                    CPH.SendMessage(nomJoueur + ", tu as une rencontre en attente ! Tape !accepter ou !refuser pour y répondre.");
                }
                return true;
            }

            string queteEnCours = LireValeur(json, "queteId");
            int ticksRequis = int.Parse(LireValeur(json, "queteTicksRestants"));
            long debutTimestamp = long.Parse(LireValeur(json, "queteDernierTick"));
            long totalPause = long.Parse(LireValeur(json, "queteTotalPause"));
            long secondesEcoulees = (maintenant - debutTimestamp) - totalPause;
            long secondesRequises = ticksRequis * 5 * 60L;

            if (secondesEcoulees < secondesRequises)
            {
                int minutesRestantes = (int)Math.Ceiling((secondesRequises - secondesEcoulees) / 60.0);
                CPH.SendMessage(nomJoueur + ", ta quête est en cours (" + queteEnCours + "). Il te reste environ " + minutesRestantes + " minute(s).");
                return true;
            }

            // Le temps est écoulé : résoudre la quête
            string[] data = GetQueteData(queteEnCours);
            int tauxEchec = int.Parse(LireValeur(File.ReadAllText(CONFIG_GLOBAL), "quete_taux_echec"));
            bool succes = rng.Next(100) >= tauxEchec;

            json = ModifierValeur(json, "enQuete", "false", false);
            json = ModifierValeur(json, "queteTicksRestants", "0", false);
            json = ModifierValeur(json, "compagnonActif", "", true);

            if (succes)
            {
                int xp = int.Parse(data[2]);
                int ram = int.Parse(data[3]);
                json = AjouterValeur(json, "experience", xp);
                json = AjouterValeur(json, "ram", ram);
                CPH.SendMessage(nomJoueur + ", ta quête est terminée ! Succès ! Tu gagnes " + xp + " XP et " + ram + " RAM. Bien joué aventurier !");
                json = VerifierMonteeNiveau(json, nomJoueur);   // annonce + bonus si seuil franchi
                File.WriteAllText(cheminFichier, json);
            }
            else
            {
                File.WriteAllText(cheminFichier, json);
                CPH.SendMessage(nomJoueur + ", ta quête est terminée... Échec. Le destin ne t'a pas souri cette fois. Retente ta chance bientôt !");
            }
            return true;
        }

        // Lancer une nouvelle quête — liste chargée dynamiquement depuis config_quetes.json
        string cfgQ    = File.ReadAllText(CONFIG_QUETES);
        string[] allIds = new string[99];
        int nbQ = 0;
        for (int i = 1; i <= 99; i++)
        {
            string qid = LireValeurString(cfgQ, QueteKey(i) + "_id");
            if (qid == "") break;
            allIds[nbQ++] = qid;
        }
        if (nbQ == 0) { CPH.SendMessage(nomJoueur + ", aucune quête disponible !"); return true; }
        string queteId   = allIds[rng.Next(nbQ)];
        string[] questData = GetQueteData(queteId);
        int ticks = int.Parse(questData[1]);

        json = ModifierValeur(json, "enQuete", "true", false);
        json = ModifierValeur(json, "queteId", queteId, true);
        json = ModifierValeur(json, "queteTicksRestants", questData[1], false);
        json = ModifierValeur(json, "queteDernierTick", maintenant.ToString(), false);
        json = ModifierValeur(json, "enRencontre", "false", false);
        json = ModifierValeur(json, "rencontreType", "", true);
        json = ModifierValeur(json, "quetePauseDebut", "0", false);
        json = ModifierValeur(json, "queteTotalPause", "0", false);
        json = ModifierValeur(json, "queteCooldownFin", "0", false);
        json = ModifierValeur(json, "dernierCheckRencontre", maintenant.ToString(), false);
        json = ModifierValeur(json, "queteEventsUsed", "0", false);

        File.WriteAllText(cheminFichier, json);
        int dureeMin = ticks * 5;
        CPH.SendMessage(nomJoueur + ", " + questData[4] + " t'envoie en mission : " + questData[0] + " (" + dureeMin + " min) !");
        CPH.EnableTimer("QuestCheck");
        return true;
    }

    // [0]=nom [1]=ticks [2]=xp [3]=ram [4]=demandeur [5]=type
    private string[] GetQueteData(string id)
    {
        string cfg = File.ReadAllText(CONFIG_QUETES);
        for (int i = 1; i <= 99; i++)
        {
            string key = QueteKey(i);
            string qid = LireValeurString(cfg, key + "_id");
            if (qid == "") break;
            if (qid != id) continue;
            return new string[] {
                LireValeurString(cfg, key + "_nom"),
                LireValeur(cfg,       key + "_ticks"),
                LireValeur(cfg,       key + "_xp"),
                LireValeur(cfg,       key + "_ram"),
                LireValeurString(cfg, key + "_demandeur"),
                LireValeurString(cfg, key + "_type")
            };
        }
        return new string[] { "Quête inconnue", "1", "0", "0", "Arbonet", "service" };
    }

    // ===== Montée de niveau =====
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

    private string QueteKey(int i)
    {
        if (i < 10)  return "quete00" + i;
        if (i < 100) return "quete0"  + i;
        return "quete" + i;
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

}