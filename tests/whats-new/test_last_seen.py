"""Test des fonctions « dernière version vue » de alfariq.js dans un vrai Chromium (Playwright).

Vérifie : valeur absente au premier lancement (null), écriture puis relecture, et stockage indisponible
(navigation privée, données bloquées) : aucune exception, l'appli continue de fonctionner.

Usage : python3 tests/whats-new/test_last_seen.py   (dépendance : playwright ; chromium installé)
Il charge le VRAI fichier src/AlFariq.UI/wwwroot/js/alfariq.js du dépôt.
"""
import http.server, pathlib, socketserver, sys, threading
from playwright.sync_api import sync_playwright

REPO = pathlib.Path(__file__).resolve().parents[2]
JS = REPO / "src/AlFariq.UI/wwwroot/js/alfariq.js"


class Handler(http.server.BaseHTTPRequestHandler):
    def do_GET(self):
        if self.path.split("?")[0] == "/alfariq.js":
            body, kind = JS.read_bytes(), "text/javascript"
        else:
            body, kind = b"<!doctype html><title>t</title>", "text/html"
        self.send_response(200)
        self.send_header("Content-Type", kind)
        self.end_headers()
        self.wfile.write(body)

    def log_message(self, *args):
        pass


failures = []


def check(name, condition, detail=""):
    print(("OK   " if condition else "FAIL ") + name + (f"  [{detail}]" if detail and not condition else ""))
    if not condition:
        failures.append(name)


with socketserver.TCPServer(("127.0.0.1", 0), Handler) as server:
    threading.Thread(target=server.serve_forever, daemon=True).start()
    url = f"http://127.0.0.1:{server.server_address[1]}/"

    with sync_playwright() as p:
        browser = p.chromium.launch()
        page = browser.new_page()
        errors = []
        page.on("pageerror", lambda e: errors.append(str(e)))
        page.goto(url)

        r = page.evaluate("""async () => {
            const m = await import('/alfariq.js?a');
            const first = m.getLastSeenVersion();
            m.setLastSeenVersion('0.2.0');
            const second = m.getLastSeenVersion();
            m.setLastSeenVersion('0.3.0');
            return { first, second, third: m.getLastSeenVersion(), raw: localStorage.getItem('alfariq.lastSeenVersion') };
        }""")
        check("premier lancement : aucune version vue (null)", r["first"] is None, r)
        check("la version écrite est relue", r["second"] == "0.2.0", r)
        check("la version est remplacée par la suivante", r["third"] == "0.3.0" and r["raw"] == "0.3.0", r)

        r = page.evaluate("""async () => {
            const m = await import('/alfariq.js?b');
            const original = Object.getOwnPropertyDescriptor(window, 'localStorage');
            Object.defineProperty(window, 'localStorage', { get() { throw new DOMException('blocked', 'SecurityError'); }, configurable: true });
            try {
              m.setLastSeenVersion('9.9.9');
              return { read: m.getLastSeenVersion(), threw: false };
            } catch (e) {
              return { threw: true, message: String(e) };
            } finally {
              Object.defineProperty(window, 'localStorage', original);
            }
        }""")
        check("stockage bloqué : aucune exception à l'écriture ni à la lecture", r.get("threw") is False, r)
        check("stockage bloqué : la lecture renvoie null", r.get("read") is None, r)

        check("aucune erreur JavaScript non gérée", not errors, errors)
        browser.close()

print("\n" + ("TOUS LES CONTRÔLES PASSENT" if not failures else f"{len(failures)} ÉCHEC(S) : {failures}"))
sys.exit(1 if failures else 0)
