@echo off
REM ---------------------------------------------------------------------------
REM Touch Portal -> Streamer.bot : arme un timeout manuel sur un viewer.
REM
REM Usage   : tp_timeout.bat <pseudo>
REM Effet   : ecrit le pseudo dans timeout_cible.txt (sans retour a la ligne).
REM           Le File/Folder Watcher de Streamer.bot detecte l'ecriture et
REM           declenche l'action "Timeout Manuel", qui lit puis vide le fichier.
REM
REM Le fichier n'est ecrit qu'au CLIC du bouton : taper dans le champ texte
REM de Touch Portal ne declenche rien.
REM ---------------------------------------------------------------------------
setlocal

set "CIBLE=C:\Users\Florian\Desktop\Stream\Moderation\timeout_cible.txt"

set "PSEUDO=%~1"
if "%PSEUDO%"=="" (
    echo [tp_timeout] Aucun pseudo fourni. Usage : tp_timeout.bat ^<pseudo^>
    exit /b 1
)

if not exist "C:\Users\Florian\Desktop\Stream\Moderation" (
    mkdir "C:\Users\Florian\Desktop\Stream\Moderation"
)

REM "set /p" sans retour a la ligne : le .cs fait un Trim() de toute facon,
REM mais un fichier propre evite les surprises cote watcher.
> "%CIBLE%" <nul set /p "=%PSEUDO%"

endlocal
exit /b 0
