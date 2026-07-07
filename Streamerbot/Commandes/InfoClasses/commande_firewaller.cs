// sb-action: !firewaller
// sb-subaction-id: b4ac1413-a766-432e-addc-e599473617c9
using System;
using System.IO;

public class CPHInline
{
    public bool Execute()
    {
        CPH.SendMessage("🛡️ FIREWALLER — Paladin du réseau | Tank / Soin");
        CPH.SendMessage("Stats : PV 22+1d6 | CA 15+1d4 | Mana 25 | Charisme 13 | Agilité 8 | Arme : Marteau-Rune | Bonus Atq : 0+1d4");
        CPH.SendMessage("Actions (rencontre) : !combat · !discuter · !fuir — hors rencontre : !soin (1d8+3 ⭐, 5 mana)");
        CPH.SendMessage("Sous-classes niv.5 — Protocole-Sacré : aura -1 dégât pour alliés · Serment-Binaire : Smite +1d8 par attaque");
        return true;
    }
}
