// sb-action: !quete
// sb-subaction-id: bb53602e-8479-4cfb-bf6e-b729662a41fe
using System;
using System.IO;
public class CPHInline
{
    private const string DOSSIER_JOUEURS = @"C:\Users\Florian\pjt\Pointu-PJT\Donnees\joueurs";
    private const string CONFIG_QUETES   = @"C:\Users\Florian\pjt\Pointu-PJT\Donnees\config_quetes.json";
    private const string CONFIG_LORE     = @"C:\Users\Florian\pjt\Pointu-PJT\Donnees\config_lore_textes.json";
    private const string CONFIG_GLOBAL   = @"C:\Users\Florian\pjt\Pointu-PJT\Donnees\config_global.json";
    private const string CONFIG_LEVEL    = @"C:\Users\Florian\pjt\Pointu-PJT\Donnees\config_level.json";
    private const string ETAT_GLOBAL     = @"C:\Users\Florian\pjt\Pointu-PJT\Donnees\etat_global.json";

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

            string cfgQr = File.ReadAllText(CONFIG_QUETES);
            string keyR  = QueteKeyDeId(cfgQr, queteEnCours);
            json = EnsureChamp(json, "quetesFaites", "", true);

            if (succes)
            {
                int xp = int.Parse(data[2]);
                int ram = int.Parse(data[3]);
                json = AjouterValeur(json, "experience", xp);
                json = AjouterValeur(json, "ram", ram);

                // Une secondaire paie en equipement : 1 item du pool de sa zone.
                string lootMsg = RecompenseItem(ref json, cfgQr, keyR, rng);

                // Artefacts et secondaires ne se refont pas ; les services, si.
                if (LireValeurString(cfgQr, keyR + "_type") != "service")
                    json = MarquerFaite(json, queteEnCours);

                CPH.SendMessage(TexteResolution(cfgQr, keyR, "succes", data[6], rng) + " " + nomJoueur
                              + " +" + xp + " XP · +" + ram + " RAM." + lootMsg);
                json = VerifierMonteeNiveau(json, nomJoueur);
                File.WriteAllText(cheminFichier, json);
            }
            else
            {
                File.WriteAllText(cheminFichier, json);
                CPH.SendMessage(TexteResolution(cfgQr, keyR, "echec", data[6], rng) + " " + nomJoueur + " repart bredouille.");
            }
            return true;
        }

        // Lancer une nouvelle quête — tirage filtré (zone débloquée, quête pas déjà faite,
        // artefact débloqué) puis pondéré vers la zone la plus avancée du joueur.
        string cfgQ  = File.ReadAllText(CONFIG_QUETES);
        string cfgG2 = File.ReadAllText(CONFIG_GLOBAL);
        json = EnsureChamp(json, "quetesFaites", "", true);

        string zoneCourante = ZoneCourante(json, cfgG2);
        string[] courante = new string[99]; int nbC = 0;
        string[] autres   = new string[99]; int nbA = 0;

        for (int i = 1; i <= 99; i++)
        {
            string key = QueteKey(i);
            string qid = LireValeurString(cfgQ, key + "_id");
            if (qid == "") break;
            if (!QueteEligible(json, cfgQ, cfgG2, key, qid)) continue;
            if (LireValeurString(cfgQ, key + "_zone") == zoneCourante) courante[nbC++] = qid;
            else                                                      autres[nbA++]   = qid;
        }

        if (nbC + nbA == 0)
        {
            CPH.SendMessage(nomJoueur + ", tu as fait tout ce que l'Antre avait a te confier pour l'instant. "
                          + "Monte en niveau pour ouvrir de nouvelles zones.");
            return true;
        }

        // La zone courante domine, les precedentes restent jouables (quete_poids_zone_courante)
        int poidsC = int.Parse(LireValeur(cfgG2, "quete_poids_zone_courante"));
        bool prendreCourante = nbC > 0 && (nbA == 0 || rng.Next(100) < poidsC);
        string queteId = prendreCourante ? courante[rng.Next(nbC)] : autres[rng.Next(nbA)];
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
        // Départ de quête : c'est une réponse à une commande tapée, donc on peut se permettre
        // du texte — c'est ici que le lore entre en jeu sans coûter un message de plus.
        string accroche = LireValeurString(cfgQ, QueteKeyDeId(cfgQ, queteId) + "_description");
        string msgDepart = "🧭 " + nomJoueur + " — " + questData[4] + " te confie « " + questData[0] + " »";
        if (accroche != "") msgDepart += " : " + accroche;
        msgDepart += " (" + dureeMin + " min · " + questData[2] + " XP · " + questData[3] + " RAM)";
        CPH.SendMessage(msgDepart);
        CPH.EnableTimer("QuestCheck");
        return true;
    }

    // Zone la plus avancee que le niveau du joueur debloque. Le Desert est a part :
    // il n'entre dans la progression que si la communaute l'a ouvert (reward Caravane).
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

    private string ZoneCourante(string json, string cfgG)
    {
        int niveau = int.Parse(LireValeur(json, "niveau"));
        string[] zones = LireValeurString(cfgG, "zone_ordre").Split(',');
        string speciale = LireValeurString(cfgG, "quete_zone_speciale");
        string courante = zones.Length > 0 ? zones[0].Trim() : "";
        foreach (string z0 in zones)
        {
            string z = z0.Trim();
            if (z == speciale) continue;
            if (niveau >= NiveauZone(cfgG, z)) courante = z;
        }
        return courante;
    }

    private int NiveauZone(string cfgG, string zone)
    {
        int v;
        return int.TryParse(LireValeur(cfgG, "zone_" + SansAccent(zone).ToLowerInvariant() + "_niveau_min"), out v) ? v : 1;
    }

    // Tirable si : zone ouverte, pas deja reussie (hors services), et pour un artefact
    // toutes ses conditions remplies (niveau, nb de quetes, 3 slots, artefact precedent).
    private bool QueteEligible(string json, string cfgQ, string cfgG, string key, string qid)
    {
        string zone = LireValeurString(cfgQ, key + "_zone");
        string type = LireValeurString(cfgQ, key + "_type");
        int niveau  = int.Parse(LireValeur(json, "niveau"));

        if (niveau < NiveauZone(cfgG, zone)) return false;

        // Le Desert n'existe pour personne tant que la caravane n'est pas passee.
        if (zone == LireValeurString(cfgG, "quete_zone_speciale"))
        {
            try
            {
                if (LireValeur(File.ReadAllText(ETAT_GLOBAL), "desertDecouvert") != "true") return false;
            }
            catch (Exception) { return false; }
        }

        if (type != "service" && EstFaite(json, qid)) return false;

        if (type == "artefact")
        {
            int nMin; int.TryParse(LireValeur(cfgQ, key + "_niveauMin"), out nMin);
            if (niveau < nMin) return false;

            int qMin; int.TryParse(LireValeur(cfgQ, key + "_quetesMin"), out qMin);
            if (int.Parse(LireValeur(json, "quetesTerminees")) < qMin) return false;

            if (LireValeurString(cfgQ, key + "_equipementComplet") == "true")
            {
                if (SlotVide(json, "armeEquipee") || SlotVide(json, "armureEquipee") || SlotVide(json, "accessoireEquipe"))
                    return false;
            }

            string requis = LireValeurString(cfgQ, key + "_requiert");
            if (requis != "" && !EstFaite(json, requis)) return false;
        }
        return true;
    }

    private bool SlotVide(string json, string slot)
    {
        string v = LireValeur(json, slot);
        return v == "" || v == "0";
    }

    private bool EstFaite(string json, string qid)
    {
        string faites = LireValeurString(json, "quetesFaites");
        if (faites == "" || qid == "") return false;
        foreach (string f in faites.Split(','))
            if (f.Trim() == qid) return true;
        return false;
    }

    private string MarquerFaite(string json, string qid)
    {
        if (EstFaite(json, qid)) return json;
        string faites = LireValeurString(json, "quetesFaites");
        return ModifierValeurString(json, "quetesFaites", faites == "" ? qid : faites + "," + qid);
    }

    // Texte de resolution PROPRE a la quete ; a defaut, la replique du PNJ.
    private string TexteResolution(string cfgQ, string key, string genre, string demandeurCle, Random rng)
    {
        string t = LireValeurString(cfgQ, key + "_texte" + (genre == "succes" ? "Victoire" : "Echec"));
        if (t != "") return t;
        return TexteLore(demandeurCle, genre, rng);
    }

    // Recompense en equipement des secondaires : 1 item du pool de la zone.
    private string RecompenseItem(ref string json, string cfgQ, string key, Random rng)
    {
        string pool = LireValeurString(cfgQ, key + "_recompensePool");
        if (pool == "") return "";                       // le Desert paie en RAM, pas en objet

        string brut = LireValeurString(cfgQ, pool);
        if (brut == "") return "";

        int maxSac = int.Parse(LireValeur(File.ReadAllText(CONFIG_GLOBAL), "max_sac"));
        string inv = LireValeurString(json, "inventaire");
        int nb = inv == "" ? 0 : inv.Split(',').Length;
        if (nb >= maxSac) return " (sac plein, la recompense est perdue !)";

        string[] items = brut.Split(',');
        string item = items[rng.Next(items.Length)].Trim();
        json = ModifierValeurString(json, "inventaire", inv == "" ? item : inv + "," + item);
        return " \U0001F381 " + item + " !";
    }

    private string SansAccent(string s)
    {
        if (s == null) return "";
        string d = s.Trim().Normalize(System.Text.NormalizationForm.FormD);
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        foreach (char c in d)
            if (System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c)
                != System.Globalization.UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        return sb.ToString().Normalize(System.Text.NormalizationForm.FormC);
    }

    private string EnsureChamp(string json, string cle, string valeurDefaut, bool estTexte)
    {
        if (json.Contains("\"" + cle + "\"")) return json;
        int    pos = json.LastIndexOf('}');
        string val = estTexte ? "\"" + valeurDefaut + "\"" : valeurDefaut;
        return json.Substring(0, pos) + ",\n  \"" + cle + "\": " + val + "\n}";
    }

    // Retrouve la clé quete0NN correspondant à un _id (pour lire les champs annexes).
    private string QueteKeyDeId(string cfg, string id)
    {
        for (int i = 1; i <= 99; i++)
        {
            string key = QueteKey(i);
            string qid = LireValeurString(cfg, key + "_id");
            if (qid == "") break;
            if (qid == id) return key;
        }
        return "";
    }

    // [0]=nom [1]=ticks [2]=xp [3]=ram [4]=demandeur [5]=type [6]=demandeurCle (textes du lore)
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
                LireValeurString(cfg, key + "_type"),
                LireValeurString(cfg, key + "_demandeurCle")
            };
        }
        return new string[] { "Quête inconnue", "1", "0", "0", "Arbonet", "service", "" };
    }

    // Tire une variante narrative dans config_lore_textes.json (voir quest_timer.cs).
    // LireValeurString OBLIGATOIRE : ces textes contiennent des virgules.
    private string TexteLore(string prefixe, string genre, Random rng)
    {
        try
        {
            if (!File.Exists(CONFIG_LORE)) return "";
            string cfg    = File.ReadAllText(CONFIG_LORE);
            string trouve = TirerVariante(cfg, prefixe, genre, rng);
            if (trouve == "") trouve = TirerVariante(cfg, "defaut", genre, rng);
            return trouve;
        }
        catch (Exception ex)
        {
            CPH.LogWarn("Lore : " + ex.Message);
            return "";
        }
    }

    private string TirerVariante(string cfg, string prefixe, string genre, Random rng)
    {
        if (prefixe == "") return "";
        int nb = 0;
        for (int i = 1; i <= 99; i++)
        {
            if (LireValeurString(cfg, prefixe + "_" + genre + "_" + Deux(i)) == "") break;
            nb++;
        }
        if (nb == 0) return "";
        return LireValeurString(cfg, prefixe + "_" + genre + "_" + Deux(rng.Next(nb) + 1));
    }

    private string Deux(int i) { return i < 10 ? "0" + i : "" + i; }

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