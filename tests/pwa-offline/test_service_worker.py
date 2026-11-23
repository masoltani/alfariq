"""Test du service worker publié et du module alfariq.js dans un vrai Chromium (Playwright).

Vérifie : ouverture hors ligne après la première visite, routes de l'appli servies hors ligne, détection d'une
nouvelle version SANS rechargement automatique, activation à la demande, purge de l'ancien cache.

Usage : python3 tests/pwa-offline/test_service_worker.py   (dépendance : pip install playwright ; chromium installé)
Il publie un mini-site temporaire avec les VRAIS fichiers service-worker.published.js et alfariq.js du dépôt.
"""
import base64, hashlib, http.server, json, pathlib, shutil, socketserver, sys, tempfile, threading
from playwright.sync_api import sync_playwright

REPO = pathlib.Path(__file__).resolve().parents[2]
SITE = pathlib.Path(sys.argv[1]) if len(sys.argv) > 1 else pathlib.Path(tempfile.mkdtemp(prefix="alfariq-sw-"))
SITE.mkdir(parents=True, exist_ok=True)

def sha(path):
    return "sha256-" + base64.b64encode(hashlib.sha256(path.read_bytes()).digest()).decode()

def publish(version, label):
    (SITE / "index.html").write_text(f"""<!doctype html><html><head><meta charset="utf-8"><title>t</title></head>
<body><div id="v">{label}</div>
<script>
  window.__calls = [];
  if ("serviceWorker" in navigator) {{
    window.alfariqSwReady = navigator.serviceWorker.register("service-worker.js", {{ updateViaCache: "none" }});
  }}
</script>
<script type="module">
  import * as m from "./alfariq.js";
  window.__m = m;
  m.initUpdates({{ invokeMethodAsync: (n) => {{ window.__calls.push(n); }} }});
</script></body></html>""", encoding="utf-8")
    shutil.copy(REPO / "src/AlFariq.UI/wwwroot/js/alfariq.js", SITE / "alfariq.js")
    shutil.copy(REPO / "src/AlFariq.Web/wwwroot/service-worker.published.js", SITE / "service-worker.js")
    assets = [{"hash": sha(SITE / n), "url": n} for n in ("index.html", "alfariq.js")]
    (SITE / "service-worker-assets.js").write_text(
        "self.assetsManifest = " + json.dumps({"version": version, "assets": assets}) + ";", encoding="utf-8")

publish("v1", "VERSION-1")

class H(http.server.SimpleHTTPRequestHandler):
    def __init__(self, *a, **k): super().__init__(*a, directory=str(SITE), **k)
    def log_message(self, *a): pass
    def end_headers(self):
        self.send_header("Cache-Control", "no-cache"); super().end_headers()

socketserver.TCPServer.allow_reuse_address = True
srv = socketserver.TCPServer(("127.0.0.1", 0), H)
port = srv.server_address[1]
threading.Thread(target=srv.serve_forever, daemon=True).start()
url = f"http://127.0.0.1:{port}/index.html"

ok = True
def check(name, cond, extra=""):
    global ok
    ok &= bool(cond)
    print(("PASS " if cond else "FAIL ") + name + (f"  [{extra}]" if extra else ""))

with sync_playwright() as p:
    b = p.chromium.launch()
    ctx = b.new_context()
    page = ctx.new_page()
    errors = []
    page.on("pageerror", lambda e: errors.append(str(e)))

    # 1. Première visite en ligne : le service worker s'installe et met en cache
    page.goto(url)
    page.evaluate("navigator.serviceWorker.ready.then(() => true)")
    page.wait_for_function("navigator.serviceWorker.controller !== null || true")
    page.reload()
    page.wait_for_function("navigator.serviceWorker.controller !== null", timeout=10000)
    check("service worker actif après la première visite", True)
    caches = page.evaluate("caches.keys()")
    check("cache hors ligne créé pour la version v1", "offline-cache-v1" in caches, str(caches))

    # 2. Hors ligne : l'appli s'ouvre (AC « s'ouvre en mode avion »)
    ctx.set_offline(True)
    page.reload()
    check("ouverture en mode avion (index servi depuis le cache)", page.inner_text("#v") == "VERSION-1")
    page.goto(f"http://127.0.0.1:{port}/matchs")   # route de l'appli : doit servir index.html
    check("navigation vers une route de l'appli hors ligne", page.inner_text("#v") == "VERSION-1")
    ctx.set_offline(False)

    # 3. Déploiement d'une nouvelle version : détection sans rechargement automatique
    page.goto(url)
    page.wait_for_load_state("networkidle")
    page.wait_for_timeout(2000)   # laisse finir la vérification de mise à jour lancée par la navigation elle-même
    publish("v2", "VERSION-2")
    page.evaluate("window.alfariqSwReady.then(r => r.update())")
    page.wait_for_function("window.__calls.includes('OnUpdateAvailable')", timeout=15000)
    check("nouvelle version détectée -> OnUpdateAvailable", True)
    check("pas de rechargement automatique (l'utilisateur voit encore v1)", page.inner_text("#v") == "VERSION-1")

    # 4. L'utilisateur accepte : activation puis rechargement sur la v2
    with page.expect_navigation(timeout=15000):
        page.evaluate("window.__m.applyUpdate()")
    check("après « Recharger » : version 2 affichée", page.inner_text("#v") == "VERSION-2")
    caches = page.evaluate("caches.keys()")
    check("ancien cache v1 supprimé, v2 présent", caches == ["offline-cache-v2"], str(caches))

    # 5. Et la v2 fonctionne hors ligne
    ctx.set_offline(True)
    page.reload()
    check("v2 ouverte en mode avion", page.inner_text("#v") == "VERSION-2")
    check("aucune erreur JavaScript", not errors, "; ".join(errors))
    b.close()

srv.shutdown()
sys.exit(0 if ok else 1)
