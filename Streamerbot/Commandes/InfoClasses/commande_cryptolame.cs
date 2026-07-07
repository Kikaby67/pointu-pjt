// sb-action: !cryptolame
// sb-subaction-id: 6195b3d0-dc31-4846-b23c-43dd68c9ef6c
using System;
using System.IO;

public class CPHInline
{
    public bool Execute()
    {
        CPH.SendMessage("🗡️ CRYPTOLAME — Assassin chiffré | DPS / Fuite");
        CPH.SendMessage("Stats : PV 16+1d6 | CA 13+1d4 | Mana 5 | Charisme 11 | Agilité 14 ⭐ | Arme : Double-Dagues | Bonus Atq : 0+1d4");
        CPH.SendMessage("Actions (rencontre) : !combat · !discuter · !fuir (agilité ⭐) — hors rencontre : !soin (1d4, 5 mana)");
        CPH.SendMessage("Sous-classes niv.5 — Byte-Fantôme : 3 attaques 1d6 · Pointeur-Null : Arc-Binaire, 1d10 (1 attaque)");
        return true;
    }
}
