# -*- coding: utf-8 -*-
"""
Genere le catalogue de la boutique de Faine, pret a coller dans #boutique sur Discord.

La source de verite reste config_items.json + config_global.boutique_catalogue :
si tu reequilibres un prix, relance ce script et recolle le message epingle.

    python Tools/catalogue_boutique.py            -> affiche
    python Tools/catalogue_boutique.py --fichier  -> ecrit Tools/catalogue_boutique.txt
"""
import io, json, os, sys, collections

# La console Windows est en cp1252 : sans ca, les emoji font planter l'affichage.
try:
    sys.stdout.reconfigure(encoding='utf-8')
except Exception:
    pass

RACINE = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
def charger(nom):
    return json.load(io.open(os.path.join(RACINE, 'Donnees', nom), encoding='utf-8'))

I = charger('config_items.json')
G = charger('config_global.json')
Q = charger('config_quetes.json')
C = charger('config_classes.json')

catalogue = [x.strip() for x in Q[G['boutique_catalogue']].split(',') if x.strip()]

def stats(nom):
    """Bonus lisibles d'un item, dans un ordre stable."""
    libelles = [('attaqueBonus', 'atq'), ('caBonus', 'CA'),
                ('manaBonus', 'mana'), ('charismeBonus', 'cha')]
    out = ['+%d %s' % (I[nom + '_' + cle], lib) for cle, lib in libelles if I.get(nom + '_' + cle)]
    poids = I.get(nom + '_poids', 0)
    if poids:
        out.append('poids %d' % poids)
    return ' · '.join(out) if out else 'aucun bonus'

def ligne(nom):
    achat  = I.get(nom + '_prixAchat', 0)
    vente  = I.get(nom + '_prixVente', 0)
    return '`%-22s` %-34s **%s RAM**  *(revente %s)*' % (nom, stats(nom), '{:,}'.format(achat).replace(',', ' '),
                                                          '{:,}'.format(vente).replace(',', ' '))

# les armes sont par SOUS-classe, les armures/accessoires par classe
sousclasse_de = {}
for cl in ['Hexadécimeur', 'Cryptolame', 'Hackmancien', 'Firewaller', 'Algorythmancien']:
    for sc in C.get(cl + '_sousClasses', '').split(','):
        sousclasse_de[sc.strip()] = cl

armes  = [n for n in catalogue if I.get(n + '_slot') == 'arme']
defense = [n for n in catalogue if I.get(n + '_slot') in ('armure', 'accessoire')]

blocs = []

b = ["# 🐿️ La boutique de Faîne l'Écureuil-Archive",
     "*Désert · objets légendaires · introuvables ailleurs*", "",
     "**Comment acheter** — il te faut **deux choses** :",
     "> 🐪 un **jeton de caravane** (récompense de points de chaîne *Caravane du Désert*)",
     "> 💾 la **RAM** correspondante",
     "",
     "Puis dans le chat Twitch : `!acheter [nom exact]`",
     "*Les accents et la casse n'ont pas d'importance.*",
     "",
     "⚠️ Un jeton = **un** achat. Sac plein ou RAM insuffisante : le jeton **n'est pas** consommé.",
     "", "---", "",
     "## 🛡️ Armures & accessoires — par classe"]
par_classe = collections.OrderedDict()
for n in defense:
    par_classe.setdefault(I.get(n + '_classeTheme', '?'), []).append(n)
for cl, items in par_classe.items():
    b.append("")
    b.append("**%s**" % cl)
    for n in sorted(items, key=lambda x: I.get(x + '_slot')):
        b.append(ligne(n))
blocs.append('\n'.join(b))

b = ["## ⚔️ Armes — une par sous-classe", "",
     "*Réservées à ceux qui ont choisi leur voie au niveau 5.*"]
par_cl = collections.OrderedDict()
for n in armes:
    sc = I.get(n + '_sousClasseTheme', '?')
    par_cl.setdefault(sousclasse_de.get(sc, '?'), []).append((sc, n))
for cl, paires in par_cl.items():
    b.append("")
    b.append("**%s**" % cl)
    for sc, n in paires:
        b.append('*%s* — %s' % (sc, ligne(n)))
b += ["", "---", "",
      "*Tu peux revendre n'importe quel objet avec `!vendre [nom]` — y compris ce que tu portes.*",
      "*Faîne ne rachète pas au prix qu'elle vend. « Rien ne se perd chez moi. Rien ne se donne non plus. »*"]
blocs.append('\n'.join(b))

sortie = ('\n\n' + '=' * 70 + '\n=== MESSAGE SUIVANT (limite Discord : 2000 caracteres) ===\n'
          + '=' * 70 + '\n\n').join(blocs)

if '--fichier' in sys.argv:
    dest = os.path.join(RACINE, 'Tools', 'catalogue_boutique.txt')
    io.open(dest, 'w', encoding='utf-8', newline='\n').write(sortie)
    print('Ecrit dans %s' % dest)
    for i, bl in enumerate(blocs, 1):
        print('  message %d : %d caracteres%s' % (i, len(bl), '  ⚠ >2000' if len(bl) > 2000 else ''))
else:
    print(sortie)
    print()
    for i, bl in enumerate(blocs, 1):
        print('--- message %d : %d caracteres%s' % (i, len(bl), '  ⚠ DEPASSE 2000' if len(bl) > 2000 else ' (OK)'))
