// sb-action: !hackmancien
// sb-subaction-id: 55b4ec62-c7b7-40a4-9ec9-d4d30c7f2dc9
using System;
using System.IO;

public class CPHInline
{
    public bool Execute()
    {
        CPH.SendMessage("💻 HACKMANCIEN — Mage-hacker | DPS magie / Buff ciblé");
        CPH.SendMessage("Stats : PV 14+1d6 | CA 10+1d4 | Mana 30 | Charisme 10 | Agilité 10 | Arme : Bâton-Magique | Bonus Atq : 0+1d4");
        CPH.SendMessage("Actions (rencontre) : !combat · !discuter · !fuir — hors rencontre : !soin (1d6, 5 mana)");
        CPH.SendMessage("Sous-classes niv.5 — Faille-Zéro : 2d8 dégâts, l'exploit ultime · Compilateur : buff un allié +2 attaque");
        return true;
    }
}
