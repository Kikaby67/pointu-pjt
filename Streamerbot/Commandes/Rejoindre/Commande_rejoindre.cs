// sb-action: !rejoindre
// sb-subaction-id: c978a3ff-400e-4d32-b95d-a0d75c3d8410
using System;
using System.IO;

public class CPHInline
{

    // CONFIGURATION — le seul endroit où tu touches les chemins

    private const string DOSSIER_JOUEURS = @"C:\Users\Florian\pjt\Pointu-PJT\Donnees\joueurs";

    public bool Execute()
    {
    
        // Nom du Viewer
    
        string nomJoueur = args["user"].ToString();
        string cheminFichier = Path.Combine(DOSSIER_JOUEURS, nomJoueur.ToLower() + ".json");

    
        // Déjà inscrit
    
        if (File.Exists(cheminFichier))
        {
            CPH.SendMessage(nomJoueur + ", tu es déjà inscrit dans l'Antre, Aventurier !");
            return true;
        }

      
        // Création du JSON pour le profil du joueur

       string contenuJson =
            // Identité
            "{\n" +
            "  \"nomJoueur\": \""       + nomJoueur      + "\",\n" +

            // Progression
            "  \"ram\": 10,\n"        +
            "  \"niveau\": 1,\n"      +
            "  \"experience\": 0,\n"  +

            // Classe
            "  \"classeChoisie\": false,\n"    +
            "  \"classe\": \"\",\n"            +
            "  \"sousClasseChoisie\": false,\n"+
            "  \"sousClasse\": \"\",\n"        +
            "  \"typeArme\": \"\",\n"          +

            // Stats de combat — initialisées à 0, Remplies par !choisirclasse
            "  \"pvMax\": 0,\n"         +
            "  \"pvActuels\": 0,\n"     +
            "  \"classeArmure\": 0,\n"  +
            "  \"bonusAttaque\": 0,\n"  +
            "  \"manaMax\": 0,\n"       +
            "  \"manaActuels\": 0,\n"   +
            "  \"charisme\": 0,\n"      +
            "  \"agilite\": 0,\n"      +

            "  \"enCombat\": false,\n" +
            "  \"enQuete\": false,\n"  +
            "  \"queteId\": \"\",\n"   +
            "  \"queteTicksRestants\": 0,\n"    +
            "  \"queteDernierTick\": 0,\n"      +
            "  \"queteTotalPause\": 0,\n"       +
            "  \"quetePauseDebut\": 0,\n"       +
            "  \"queteCooldownFin\": 0,\n"      +
            "  \"enRencontre\": false,\n"       +
            "  \"rencontreType\": \"\",\n"      +
            "  \"rencontreExpire\": 0,\n"       +
            "  \"compagnonActif\": \"\",\n"     +
            "  \"dernierCheckRencontre\": 0,\n" +
            "  \"reposCooldownFin\": 0,\n"      +
            "  \"queteEventsUsed\": 0,\n"       +
            "  \"offreValeur\": 0,\n"           +
            "  \"derniereActivite\": 0,\n"      +

            // État du combat en cours
            // Mis à jour à chaque tour de combat
            "  \"combatActuel\": {\n"                   +
            "    \"ennemiNom\": \"\",\n"                +
            "    \"ennemiPVActuels\": 0,\n"             +
            "    \"buffActif\": false,\n"               +
            "    \"protectionActive\": false,\n"        +
            "    \"tourCombat\": 0\n"                   +
            "  },\n"                                    +

            // Inventaire
            "  \"inventaire\": \"\",\n"    +
            "  \"armeEquipee\": \"\",\n"    +
            "  \"armureEquipee\": \"\",\n"  +
            "  \"accessoireEquipe\": \"\",\n"    +
            

            // Statistiques globales
            "  \"statistiques\": {\n"             +
            "    \"combatsGagnes\": 0,\n"          +
            "    \"combatsPerdus\": 0,\n"          +
            "    \"quetesTerminees\": 0\n"         +
            "  }\n"                                +
            "}";
     
        Directory.CreateDirectory(DOSSIER_JOUEURS);
        File.WriteAllText(cheminFichier, contenuJson);

        // Message de bienvenue dans le chat Twitch
      
        CPH.SendMessage(nomJoueur + ", parfait ! Tu es maintenant un Aventurier de l'Antre ! " +
            "Ton fragment de Carapace est prêt. Choisis ta voie : " +
            "!choisirclasse (Hexadécimeur / Cryptolame / Hackmancien / Firewaller / Algorythmancien). " +
            "Tape ![nom de la classe] pour en savoir plus sur chaque classe.");

        return true;
    }
}