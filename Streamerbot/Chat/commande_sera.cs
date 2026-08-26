// sb-action: StSera
// sb-subaction-id: 4265c2c3-2dfc-47d5-86e2-4e6649405e98
// sb-group: Command chat
// sb-trigger: command !sera
using System;

public class CPHInline
{
    // Répliques de Sera. Ajouter une ligne ici suffit — le tirage s'adapte tout seul.
    private static readonly string[] REPLIQUES =
    {
        "T'es mauvais Othnos !",
        "Othnos ? Voyez vous, dans son domaine il est premier en bas de sa liste mais c'est ainsi qu'on l'aime",
        "Othnos enlève tes dents de mon paff c'est pas ouf",
        "Kika ris donc j'ai raison",
        "Othnos t'as mal ? Bah arrête"
    };

    // Random en static : deux !sera lancés dans la même seconde tireraient la même
    // réplique avec un "new Random()" local (même graine d'horloge).
    private static readonly Random RNG = new Random();

    public bool Execute()
    {
        CPH.SendMessage(REPLIQUES[RNG.Next(REPLIQUES.Length)]);
        return true;
    }
}
