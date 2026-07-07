// sb-action: !hexadecimeur
// sb-subaction-id: 0a9d867b-af44-4e63-9fd7-2b469006ff47
using System;
using System.IO;

public class CPHInline
{
    public bool Execute()
    {
        CPH.SendMessage("⚔️ HEXADÉCIMEUR — Guerrier du réseau | DPS corps à corps / Tank");
        CPH.SendMessage("Stats : PV 25+1d6 | CA 14+1d4 | Mana 5 | Charisme 8 | Agilité 8 | Arme : Épée | Bonus Atq : 0+1d4");
        CPH.SendMessage("Actions (rencontre) : !combat · !discuter · !fuir — hors rencontre : !soin (1d4, 5 mana)");
        CPH.SendMessage("Sous-classes niv.5 — Bloc-Hex : +8 PV, frappe immuable · Surcharge : 2 attaques 1d8 / -2 CA permanente");
        return true;
    }
}
