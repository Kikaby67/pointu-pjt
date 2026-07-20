' ---------------------------------------------------------------------------
' Touch Portal -> Streamer.bot : arme un timeout manuel sur un viewer.
'
' Deux modes :
'   wscript.exe tp_timeout.vbs <pseudo>   -> silencieux, aucune fenetre a l'ecran
'                                            (Touch Portal fournit deja le pseudo)
'   wscript.exe tp_timeout.vbs            -> affiche une petite boite de saisie
'                                            (repli si TP n'a pas d'action de saisie)
'
' Le File/Folder Watcher de Streamer.bot detecte l'ecriture et declenche
' l'action "Timeout Manuel", qui lit le pseudo puis vide le fichier.
' ---------------------------------------------------------------------------

Const DOSSIER = "C:\Users\Florian\Desktop\Stream\Moderation"
Const CIBLE   = "C:\Users\Florian\Desktop\Stream\Moderation\timeout_cible.txt"

If WScript.Arguments.Count > 0 Then
    pseudo = Trim(WScript.Arguments(0))
Else
    ' Champ volontairement VIDE : pre-remplir avec le dernier viewer du chat
    ' ferait d'un appui sur Entree un timeout instantane sur un innocent.
    pseudo = Trim(InputBox("Pseudo Twitch a timeout (5 min) :", "Moderation — Prison de Sagesse", ""))
End If

' Annulation de la boite ou saisie vide -> on ne touche a rien.
If pseudo = "" Then WScript.Quit 1

Set fso = CreateObject("Scripting.FileSystemObject")
If Not fso.FolderExists(DOSSIER) Then fso.CreateFolder(DOSSIER)

' True = ecrase le contenu precedent (une cible a la fois).
Set f = fso.CreateTextFile(CIBLE, True)
f.Write pseudo
f.Close

WScript.Quit 0
