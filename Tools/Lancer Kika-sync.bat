@echo off
rem Lance l'interface graphique Kika-sync sans fenetre console.
cd /d "%~dp0"
where pythonw >nul 2>nul && (
    start "" pythonw "kika_sync_gui.py"
) || (
    start "" python "kika_sync_gui.py"
)
