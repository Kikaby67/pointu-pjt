// sb-action: !algorythmancien
// sb-subaction-id: 8ebcbd6b-ca94-4bbd-8bcc-4406629e5d7c
using System;
using System.IO;

public class CPHInline
{
    public bool Execute()
    {
        CPH.SendMessage("🎵 ALGORYTHMANCIEN — Barde du code | Buff collectif / Soin");
        CPH.SendMessage("Stats : PV 16+1d6 | CA 11+1d4 | Mana 20 | Charisme 16 ⭐ | Agilité 12 | Arme : Luth-Code | Bonus Atq : 0+1d4");
        CPH.SendMessage("Actions (rencontre) : !combat · !discuter (charisme ⭐) · !fuir — hors rencontre : !soin (1d6, 5 mana)");
        CPH.SendMessage("Sous-classes niv.5 — Barde-Binaire : 1d10 + buff TOUS alliés · Patch-Mélodique : soin 1d8+3");
        return true;
    }
}
