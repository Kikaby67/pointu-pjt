@echo off
rem Lance l'interface graphique sb_sync sans fenetre console.
cd /d "%~dp0"
where pythonw >nul 2>nul && (
    start "" pythonw "sb_sync_gui.py"
) || (
    start "" python "sb_sync_gui.py"
)
