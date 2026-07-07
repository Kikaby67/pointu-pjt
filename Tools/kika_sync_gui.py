#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Kika-sync — Interface graphique.
Pilote la synchro .cs -> Streamer.bot (kika_sync.py) sans passer par PowerShell.

Lancement : double-clic sur "Lancer Kika-sync.bat"
         ou : python kika_sync_gui.py
"""

import os
import queue
import subprocess
import sys
import threading
import tkinter as tk
from tkinter import ttk

HERE      = os.path.dirname(os.path.abspath(__file__))
KIKA_SYNC = os.path.join(HERE, "kika_sync.py")
LOG_FILE  = os.path.join(HERE, "kika_sync.log")

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

        root.title("Kika-sync — Streamer.bot")
        root.geometry("900x600")
        root.minsize(720, 500)

        style = ttk.Style()
        try:
            style.theme_use("vista")
        except tk.TclError:
            pass
        style.configure("Big.TButton", font=("Segoe UI", 11, "bold"), padding=(10, 12))
        style.configure("Kika.Header.TLabel", font=("Segoe UI", 15, "bold"))

        # ---- En-tête + options ----
        head = ttk.Frame(root, padding=(14, 12, 14, 4))
        head.pack(fill="x")
        ttk.Label(head, text="⚡ Kika-sync", style="Kika.Header.TLabel").pack(side="left")

        opt = ttk.Frame(head)
        opt.pack(side="right")
        ttk.Label(opt, text="Reload :").pack(side="left")
        self.mode = tk.StringVar(value="A")
        ttk.Radiobutton(opt, text="A (auto)", variable=self.mode, value="A",
                        command=self._sync_lock).pack(side="left", padx=(4, 2))
        ttk.Radiobutton(opt, text="B (SB fermé)", variable=self.mode, value="B",
                        command=self._sync_lock).pack(side="left", padx=(0, 8))
        self.lock = tk.BooleanVar(value=False)
        self.chk_lock = ttk.Checkbutton(opt, text="verrouiller", variable=self.lock)
        self.chk_lock.pack(side="left")

        ttk.Label(root,
                  text="Mode A = ferme et relance Streamer.bot tout seul (recommandé).  "
                       "Mode B = patch seul, SB doit être fermé.",
                  foreground="#7d8894").pack(fill="x", padx=16)

        # ---- 3 actions principales ----
        prim = ttk.Frame(root, padding=(14, 10))
        prim.pack(fill="x")
        for i in range(3):
            prim.columnconfigure(i, weight=1, uniform="prim")

        self.btn_all = ttk.Button(prim, text="🗂️  Synchroniser\ntout le dossier",
                                  style="Big.TButton",
                                  command=lambda: self.run(["--once"] + self._reload_args()))
        self.btn_all.grid(row=0, column=0, sticky="ew", padx=(0, 6))

        self.btn_last = ttk.Button(prim, text="💾  Synchroniser\nla dernière save",
                                   style="Big.TButton",
                                   command=lambda: self.run(["--last"] + self._reload_args()))
        self.btn_last.grid(row=0, column=1, sticky="ew", padx=6)

        self.btn_dup = ttk.Button(prim, text="🔍  Scanner\nles doublons",
                                  style="Big.TButton",
                                  command=lambda: self.run(["--check-duplicates"]))
        self.btn_dup.grid(row=0, column=2, sticky="ew", padx=(6, 0))

        # ---- Actions secondaires ----
        sec = ttk.Frame(root, padding=(14, 0))
        sec.pack(fill="x")
        self.btn_stamp = ttk.Button(sec, text="🏷️ Écrire les en-têtes (auto-map)",
                                    command=lambda: self.run(["--discover", "--write"]))
        self.btn_stamp.pack(side="left", padx=(0, 6))
        self.btn_watch = ttk.Button(sec, text="▶ Surveillance auto",
                                    command=self.toggle_watch)
        self.btn_watch.pack(side="left", padx=6)
        self.btn_stop = ttk.Button(sec, text="■ Stop", command=self.stop, state="disabled")
        self.btn_stop.pack(side="left", padx=6)

        # ---- Journal ----
        logframe = ttk.Frame(root, padding=(14, 8))
        logframe.pack(fill="both", expand=True)
        self.txt = tk.Text(logframe, wrap="word", height=15,
                           background="#111418", foreground="#d6dae0",
                           insertbackground="#d6dae0", relief="flat",
                           font=("Consolas", 9))
        self.txt.pack(side="left", fill="both", expand=True)
        sb = ttk.Scrollbar(logframe, command=self.txt.yview)
        sb.pack(side="right", fill="y")
        self.txt.configure(yscrollcommand=sb.set, state="disabled")
        self.txt.tag_config("warn", foreground="#e8b84b")
        self.txt.tag_config("err",  foreground="#ef6b6b")
        self.txt.tag_config("ok",   foreground="#66c489")
        self.txt.tag_config("dim",  foreground="#7d8894")

        # ---- Barre d'état ----
        bottom = ttk.Frame(root, padding=(14, 6))
        bottom.pack(fill="x")
        self.status = tk.StringVar(value="Prêt.")
        ttk.Label(bottom, textvariable=self.status).pack(side="left")
        ttk.Button(bottom, text="Vider le journal", command=self.clear_log).pack(side="right")
        ttk.Button(bottom, text="Ouvrir le log", command=self.open_log).pack(side="right", padx=6)

        self._log("Kika-sync prêt. Chemins : configurés dans kika_sync.local.json.\n", "dim")
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
        self.chk_lock.configure(state="normal" if self.mode.get() == "B" else "disabled")

    def _action_buttons(self):
        return [self.btn_all, self.btn_last, self.btn_dup,
                self.btn_stamp, self.btn_watch]

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
        elif "[WARN]" in line or "DOUBLON" in line or "ATTENTION" in line:
            tag = "warn"
        elif " OK  " in line or "MATCH" in line or "patché" in line \
                or "relancé" in line or "Aucun doublon" in line or "✅" in line:
            tag = "ok"
        self._log(line, tag)

    def clear_log(self):
        self.txt.configure(state="normal")
        self.txt.delete("1.0", "end")
        self.txt.configure(state="disabled")

    def open_log(self):
        if os.path.exists(LOG_FILE):
            try:
                os.startfile(LOG_FILE)  # noqa (Windows)
            except (AttributeError, OSError):
                self.status.set("Impossible d'ouvrir le log ici.")
        else:
            self.status.set("Pas encore de fichier log.")

    # ------------------ exécution ------------------
    def run(self, args, watch=False):
        if self.proc is not None:
            self.status.set("Une opération est déjà en cours.")
            return
        if not os.path.exists(KIKA_SYNC):
            self._log(f"[ERROR] Introuvable : {KIKA_SYNC}\n", "err")
            return

        cmd = [PY, "-u", KIKA_SYNC] + args
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
        threading.Thread(target=self._reader, args=(self.proc,), daemon=True).start()

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
        self.btn_watch.configure(text="▶ Surveillance auto")
        self.status.set("Surveillance arrêtée." if was_watch else "Terminé.")

    # ------------------ boutons ------------------
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
    App(root)
    root.mainloop()


if __name__ == "__main__":
    main()
