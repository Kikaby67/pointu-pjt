#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
sb_sync_gui.py — Interface graphique pour sb_sync.py.
Pilote la synchro .cs -> Streamer.bot sans passer par PowerShell.

Lancement : double-clic sur "Lancer interface.bat"
         ou : python sb_sync_gui.py
"""

import os
import queue
import subprocess
import sys
import threading
import tkinter as tk
from tkinter import filedialog, ttk

HERE       = os.path.dirname(os.path.abspath(__file__))
SB_SYNC    = os.path.join(HERE, "sb_sync.py")
LOG_FILE   = os.path.join(HERE, "sb_sync.log")

# python.exe meme si l'appli est lancee via pythonw.exe
PY = sys.executable
if PY.lower().endswith("pythonw.exe"):
    cand = PY[:-len("pythonw.exe")] + "python.exe"
    if os.path.exists(cand):
        PY = cand

CREATE_NO_WINDOW = 0x08000000 if sys.platform.startswith("win") else 0


class App:
    def __init__(self, root):
        self.root = root
        self.proc = None
        self.watching = False
        self.q = queue.Queue()

        root.title("Streamer.bot — Sync C#")
        root.geometry("860x560")
        root.minsize(700, 460)

        # ---- Bandeau options ----
        top = ttk.Frame(root, padding=(12, 10))
        top.pack(fill="x")

        ttk.Label(top, text="Reload :").pack(side="left")
        self.mode = tk.StringVar(value="A")
        ttk.Radiobutton(top, text="B — patch seul (relance SB toi-même)",
                        variable=self.mode, value="B",
                        command=self._sync_lock).pack(side="left", padx=(4, 10))
        ttk.Radiobutton(top, text="A — kill + relance auto de SB",
                        variable=self.mode, value="A",
                        command=self._sync_lock).pack(side="left", padx=(0, 14))

        self.lock = tk.BooleanVar(value=False)
        self.chk_lock = ttk.Checkbutton(
            top, text="verrouiller actions.json après patch (anti-écrasement)",
            variable=self.lock)
        self.chk_lock.pack(side="left")

        # ---- Boutons d'action ----
        bar = ttk.Frame(root, padding=(12, 0))
        bar.pack(fill="x")

        self.btn_discover = ttk.Button(bar, text="🔍 Découvrir (aperçu)",
                                       command=lambda: self.run(["--discover"]))
        self.btn_discover.pack(side="left", padx=(0, 6), pady=6)

        self.btn_stamp = ttk.Button(bar, text="🏷️ Écrire les en-têtes (auto-map)",
                                    command=lambda: self.run(["--discover", "--write"]))
        self.btn_stamp.pack(side="left", padx=6, pady=6)

        self.btn_once = ttk.Button(bar, text="⬆️ Patch complet",
                                   command=lambda: self.run(["--once"] + self._reload_args()))
        self.btn_once.pack(side="left", padx=6, pady=6)

        self.btn_file = ttk.Button(bar, text="📄 Patch un fichier…",
                                   command=self.patch_file)
        self.btn_file.pack(side="left", padx=6, pady=6)

        self.btn_watch = ttk.Button(bar, text="▶ Démarrer la surveillance",
                                    command=self.toggle_watch)
        self.btn_watch.pack(side="left", padx=(16, 6), pady=6)

        self.btn_stop = ttk.Button(bar, text="■ Stop", command=self.stop,
                                   state="disabled")
        self.btn_stop.pack(side="left", padx=6, pady=6)

        # ---- Journal ----
        logframe = ttk.Frame(root, padding=(12, 6))
        logframe.pack(fill="both", expand=True)
        self.txt = tk.Text(logframe, wrap="word", height=18,
                           background="#111418", foreground="#d6dae0",
                           insertbackground="#d6dae0", relief="flat",
                           font=("Consolas", 9))
        self.txt.pack(side="left", fill="both", expand=True)
        sb = ttk.Scrollbar(logframe, command=self.txt.yview)
        sb.pack(side="right", fill="y")
        self.txt.configure(yscrollcommand=sb.set, state="disabled")
        self.txt.tag_config("warn",  foreground="#e8b84b")
        self.txt.tag_config("err",   foreground="#ef6b6b")
        self.txt.tag_config("ok",    foreground="#66c489")
        self.txt.tag_config("dim",   foreground="#7d8894")

        # ---- Barre d'état ----
        bottom = ttk.Frame(root, padding=(12, 6))
        bottom.pack(fill="x")
        self.status = tk.StringVar(value="Prêt.")
        ttk.Label(bottom, textvariable=self.status).pack(side="left")
        ttk.Button(bottom, text="Vider le journal",
                   command=self.clear_log).pack(side="right")
        ttk.Button(bottom, text="Ouvrir le fichier log",
                   command=self.open_log).pack(side="right", padx=6)

        self._log(f"Interface prête. Script : {SB_SYNC}\n"
                  f"actions.json et chemins : configurés en tête de sb_sync.py.\n", "dim")
        self._sync_lock()
        self.root.after(80, self._poll)
        self.root.protocol("WM_DELETE_WINDOW", self._on_close)

    # ------------------ helpers UI ------------------
    def _reload_args(self):
        a = ["--reload", self.mode.get()]
        if self.lock.get() and self.mode.get() == "B":
            a.append("--lock")
        return a

    def _sync_lock(self):
        self.chk_lock.configure(
            state="normal" if self.mode.get() == "B" else "disabled")

    def _action_buttons(self):
        return [self.btn_discover, self.btn_stamp, self.btn_once,
                self.btn_file, self.btn_watch]

    def _set_busy(self, busy):
        for b in self._action_buttons():
            b.configure(state="disabled" if busy else "normal")
        self.btn_stop.configure(state="normal" if busy else "disabled")

    def _log(self, text, tag=None):
        self.txt.configure(state="normal")
        self.txt.insert("end", text, tag or ())
        self.txt.see("end")
        self.txt.configure(state="disabled")

    def _log_line(self, line):
        tag = None
        if "[ERROR]" in line:
            tag = "err"
        elif "[WARN]" in line:
            tag = "warn"
        elif " OK  " in line or "MATCH" in line or "patché" in line or "relancé" in line:
            tag = "ok"
        self._log(line, tag)

    def clear_log(self):
        self.txt.configure(state="normal")
        self.txt.delete("1.0", "end")
        self.txt.configure(state="disabled")

    def open_log(self):
        if os.path.exists(LOG_FILE):
            os.startfile(LOG_FILE)  # noqa (Windows)
        else:
            self.status.set("Pas encore de fichier log.")

    # ------------------ exécution ------------------
    def run(self, args, watch=False):
        if self.proc is not None:
            self.status.set("Une opération est déjà en cours.")
            return
        if not os.path.exists(SB_SYNC):
            self._log(f"[ERROR] Introuvable : {SB_SYNC}\n", "err")
            return

        cmd = [PY, "-u", SB_SYNC] + args
        env = dict(os.environ, PYTHONIOENCODING="utf-8")
        self._log(f"\n$ {' '.join(args)}\n", "dim")
        try:
            self.proc = subprocess.Popen(
                cmd, stdout=subprocess.PIPE, stderr=subprocess.STDOUT,
                text=True, encoding="utf-8", errors="replace",
                env=env, creationflags=CREATE_NO_WINDOW, cwd=HERE)
        except OSError as e:
            self._log(f"[ERROR] Lancement impossible : {e}\n", "err")
            self.proc = None
            return

        self.watching = watch
        self._set_busy(True)
        self.status.set("Surveillance en cours…" if watch else "Traitement…")
        threading.Thread(target=self._reader, args=(self.proc,),
                         daemon=True).start()

    def _reader(self, proc):
        try:
            for line in iter(proc.stdout.readline, ""):
                self.q.put(line)
        finally:
            try:
                proc.stdout.close()
            except OSError:
                pass
            self.q.put(("__END__", proc.returncode))

    def _poll(self):
        try:
            while True:
                item = self.q.get_nowait()
                if isinstance(item, tuple) and item and item[0] == "__END__":
                    self._finished()
                else:
                    self._log_line(item)
        except queue.Empty:
            pass
        self.root.after(80, self._poll)

    def _finished(self):
        self.proc = None
        was_watch = self.watching
        self.watching = False
        self._set_busy(False)
        self.btn_watch.configure(text="▶ Démarrer la surveillance")
        self.status.set("Surveillance arrêtée." if was_watch else "Terminé.")

    # ------------------ boutons ------------------
    def patch_file(self):
        path = filedialog.askopenfilename(
            title="Choisir un fichier .cs",
            initialdir=os.path.join(os.path.dirname(HERE), "Streamerbot"),
            filetypes=[("Fichiers C#", "*.cs"), ("Tous", "*.*")])
        if path:
            self.run(["--file", path] + self._reload_args())

    def toggle_watch(self):
        if self.proc is None:
            self.btn_watch.configure(text="⏳ Surveillance…")
            self.run(self._reload_args(), watch=True)

    def stop(self):
        if self.proc is not None:
            self.status.set("Arrêt…")
            try:
                self.proc.terminate()
            except OSError:
                pass

    def _on_close(self):
        if self.proc is not None:
            try:
                self.proc.terminate()
            except OSError:
                pass
        self.root.destroy()


def main():
    root = tk.Tk()
    try:
        ttk.Style().theme_use("vista")
    except tk.TclError:
        pass
    App(root)
    root.mainloop()


if __name__ == "__main__":
    main()
