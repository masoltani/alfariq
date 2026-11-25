"""Test de la détection hors ligne de alfariq.js dans un vrai Chromium (Playwright).

Vérifie : état initial renvoyé à l'interface, notification à la coupure puis au retour du réseau,
et démarrage alors que l'appareil est déjà hors ligne.

Usage : python3 tests/connectivity/test_connectivity.py   (dépendance : playwright ; chromium installé)
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


# Remplace la référence .NET : enregistre les appels reçus.
INIT = """async (key) => {
    const m = await import('/alfariq.js');
    window[key] = [];
    return m.initConnectivity({ invokeMethodAsync: (method, online) => window[key].push([method, online]) });
}"""

with socketserver.TCPServer(("127.0.0.1", 0), Handler) as server:
    threading.Thread(target=server.serve_forever, daemon=True).start()
    url = f"http://127.0.0.1:{server.server_address[1]}/"

    with sync_playwright() as p:
        browser = p.chromium.launch()
        context = browser.new_context()
        page = context.new_page()
        errors = []
        page.on("pageerror", lambda e: errors.append(str(e)))
        page.goto(url)

        initial = page.evaluate(INIT, "calls")
        check("en ligne au démarrage", initial is True, initial)

        context.set_offline(True)
        page.wait_for_function("window.calls.length >= 1")
        calls = page.evaluate("window.calls")
        check("coupure : l'interface est prévenue (hors ligne)", calls[-1] == ["OnConnectivityChanged", False], calls)

        context.set_offline(False)
        page.wait_for_function("window.calls.length >= 2")
        calls = page.evaluate("window.calls")
        check("retour du réseau : l'interface est prévenue (en ligne)", calls[-1] == ["OnConnectivityChanged", True], calls)

        context.set_offline(True)
        initial = page.evaluate(INIT, "late")
        check("démarrage hors ligne : état initial hors ligne", initial is False, initial)

        check("aucune erreur JavaScript non gérée", not errors, errors)
        browser.close()

print("\n" + ("TOUS LES CONTRÔLES PASSENT" if not failures else f"{len(failures)} ÉCHEC(S) : {failures}"))
sys.exit(1 if failures else 0)
