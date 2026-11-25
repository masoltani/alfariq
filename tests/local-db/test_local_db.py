"""Test du module local-db.js dans un vrai Chromium (Playwright) : IndexedDB, migrations de schéma, persistance.

Vérifie : création des magasins à la première ouverture, écriture/lecture/liste, migration 1 -> 2 qui AJOUTE un
magasin sans perdre les données existantes, ordre des migrations, erreur claire hors schéma, demande de persistance.

Usage : python3 tests/local-db/test_local_db.py   (dépendance : playwright ; chromium installé)
Il charge le VRAI fichier src/AlFariq.Client.Core/wwwroot/js/local-db.js du dépôt.
"""
import http.server, json, pathlib, socketserver, sys, threading
from playwright.sync_api import sync_playwright

REPO = pathlib.Path(__file__).resolve().parents[2]
JS = REPO / "src/AlFariq.Client.Core/wwwroot/js/local-db.js"


class Handler(http.server.BaseHTTPRequestHandler):
    def do_GET(self):
        if self.path.split("?")[0] == "/local-db.js":
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


V1 = [{"version": 1, "createStores": [{"name": "meta"}]}]
V2 = V1 + [{"version": 2, "createStores": [{"name": "players", "indexes": [{"name": "byName", "keyPath": "name"}]}]}]

with socketserver.TCPServer(("127.0.0.1", 0), Handler) as server:
    threading.Thread(target=server.serve_forever, daemon=True).start()
    url = f"http://127.0.0.1:{server.server_address[1]}/"

    with sync_playwright() as p:
        browser = p.chromium.launch()
        page = browser.new_page()
        errors = []
        page.on("pageerror", lambda e: errors.append(str(e)))
        page.goto(url)

        # Chaque « import('/local-db.js?x') » charge un module neuf : c'est comme rouvrir la page.

        # --- Version 1 : création, écriture, lecture ---
        r = page.evaluate("""async ([v1]) => {
            const m = await import('/local-db.js?a');
            await m.open('t1', 1, JSON.stringify(v1));
            await m.put('meta', 'k', JSON.stringify({ id: 'k', value: 42 }));
            return {
              got: JSON.parse(await m.get('meta', 'k')),
              missing: await m.get('meta', 'absent'),
              all: (await m.getAll('meta')).map(JSON.parse),
            };
        }""", [V1])
        check("écriture puis lecture d'un document", r["got"] == {"id": "k", "value": 42}, r)
        check("lecture d'un document absent renvoie null", r["missing"] is None, r)
        check("liste des documents d'un magasin", r["all"] == [{"id": "k", "value": 42}], r)

        # --- Migration 1 -> 2 : ajoute un magasin, conserve les données ---
        r = page.evaluate("""async ([v2]) => {
            const m = await import('/local-db.js?b');   // module neuf : base rouverte
            await m.open('t1', 2, JSON.stringify(v2));
            await m.put('players', 'p1', JSON.stringify({ id: 'p1', name: 'Sami' }));
            const db = await new Promise((res, rej) => { const q = indexedDB.open('t1'); q.onsuccess = () => res(q.result); q.onerror = rej; });
            const info = {
              version: db.version,
              stores: [...db.objectStoreNames].sort(),
              indexes: [...db.transaction('players').objectStore('players').indexNames],
            };
            db.close();
            return {
              info,
              oldData: JSON.parse(await m.get('meta', 'k')),
              player: JSON.parse(await m.get('players', 'p1')),
            };
        }""", [V2])
        check("migration vers la version 2", r["info"]["version"] == 2, r)
        check("le nouveau magasin est créé, l'ancien conservé", r["info"]["stores"] == ["meta", "players"], r)
        check("l'index déclaré est créé", r["info"]["indexes"] == ["byName"], r)
        check("les données de la version 1 survivent à la migration", r["oldData"] == {"id": "k", "value": 42}, r)
        check("le nouveau magasin est utilisable", r["player"]["name"] == "Sami", r)

        # --- Migrations déclarées dans le désordre : appliquées dans l'ordre ---
        r = page.evaluate("""async ([v2]) => {
            const m = await import('/local-db.js?c');
            await m.open('t2', 2, JSON.stringify([...v2].reverse()));
            const db = await new Promise((res, rej) => { const q = indexedDB.open('t2'); q.onsuccess = () => res(q.result); q.onerror = rej; });
            const stores = [...db.objectStoreNames].sort();
            db.close();
            return stores;
        }""", [V2])
        check("une base neuve reçoit toutes les migrations", r == ["meta", "players"], r)

        # --- Magasin inconnu : erreur claire, pas de silence ---
        r = page.evaluate("""async ([v1]) => {
            const m = await import('/local-db.js?d');
            await m.open('t3', 1, JSON.stringify(v1));
            try { await m.put('inconnu', 'x', '{}'); return 'pas d\\'erreur'; } catch (e) { return 'erreur'; }
        }""", [V1])
        check("écrire dans un magasin non déclaré échoue", r == "erreur", r)

        # --- Utilisation avant ouverture ---
        r = page.evaluate("""async () => {
            const m = await import('/local-db.js?e');
            try { await m.get('meta', 'k'); return 'pas d\\'erreur'; } catch (e) { return 'erreur'; }
        }""")
        check("lire avant open() échoue clairement", r == "erreur", r)

        # --- Persistance ---
        r = page.evaluate("""async () => {
            const m = await import('/local-db.js?f');
            return await m.requestPersistence();
        }""")
        check("requestPersistence renvoie une valeur connue", r in ("granted", "denied"), r)

        r = page.evaluate("""async () => {
            const original = navigator.storage.persist;
            Object.defineProperty(navigator.storage, 'persist', { value: undefined, configurable: true });
            const m = await import('/local-db.js?g');
            const result = await m.requestPersistence();
            Object.defineProperty(navigator.storage, 'persist', { value: original, configurable: true });
            return result;
        }""")
        check("navigateur sans persist() : « unsupported »", r == "unsupported", r)

        check("aucune erreur JavaScript non gérée", not errors, errors)
        browser.close()

print("\n" + ("TOUS LES CONTRÔLES PASSENT" if not failures else f"{len(failures)} ÉCHEC(S) : {failures}"))
sys.exit(1 if failures else 0)
