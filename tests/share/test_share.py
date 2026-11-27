"""Test des modules share.js et lineup-image.js dans un vrai Chromium (Playwright).

Vérifie : image PNG de la composition (listes et terrain, grande composition, noms très longs), partage natif
(simulé : réussite, annulation, échec, absence), partage d'un fichier, copie du texte et son repli.

Usage : python3 tests/share/test_share.py [dossier_pour_les_images]
(dépendance : playwright ; chromium installé). Il charge les VRAIS fichiers JavaScript et la feuille de style du dépôt.
"""
import base64, http.server, pathlib, socketserver, sys, threading
from playwright.sync_api import sync_playwright

REPO = pathlib.Path(__file__).resolve().parents[2]
CLIENT_JS = REPO / "src/AlFariq.Client.Core/wwwroot/js"
FILES = {
    "/share.js": (CLIENT_JS / "share.js", "text/javascript"),
    "/lineup-image.js": (CLIENT_JS / "lineup-image.js", "text/javascript"),
    "/app.css": (REPO / "src/AlFariq.UI/wwwroot/css/app.css", "text/css"),
}


class Handler(http.server.BaseHTTPRequestHandler):
    def do_GET(self):
        path = self.path.split("?")[0]
        if path in FILES:
            file, kind = FILES[path]
            body = file.read_bytes()
        else:
            kind = "text/html; charset=utf-8"
            body = b'<!doctype html><title>t</title><link rel="stylesheet" href="/app.css"><button id="b">b</button>'
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


def player(name, position=None):
    return {"name": name, "position": position}


def team(name, color, players, lines=(), formation=None, others=(), others_title=None):
    return {"name": name, "colorCode": color, "formation": formation, "lines": list(lines),
            "players": players, "others": list(others), "othersTitle": others_title}


def card(teams, unassigned=(), location="Stade municipal"):
    return {"sportCode": "football", "title": "Match du mercredi 2 décembre 2026", "details": "20:00 · 11 contre 11",
            "location": location, "teams": teams, "unassigned": list(unassigned),
            "unassignedTitle": "Non assignés :", "emptyTeam": "Aucun joueur dans cette équipe."}


NAMES = ["Sami", "Alexandre Dubois", "Mohamed-Amine", "Zoé", "Karim", "Jean-Christophe", "Léa", "Nicolas",
         "Yassine Benabdallah", "محمد الأمين", "Abderrahmane"]


def pitch(layout, free=()):
    lines, index = [], 0
    for codes in layout:
        line = []
        for code in codes:
            line.append({"position": code, "name": None if index in free else NAMES[index % len(NAMES)]})
            index += 1
        lines.append(line)
    return lines


LISTS = card([
    team("Les Verts", "green", [player(n, "Milieu défensif" if i % 2 else None) for i, n in enumerate(NAMES[:5])]),
    team("Équipe B", "white", []),
], unassigned=["Tom", "Nadia"])

PITCHES = card([
    team("Les Invincibles du jeudi soir", "yellow", [player(n) for n in NAMES],
         pitch([["G"], ["DC", "DC", "DC"], ["MG", "MC", "MDF", "MC", "MD"], ["BU", "BU"]]), "3-5-2",
         others=[player("Tom"), player("Nadia", "Attaquant")], others_title="Remplaçants :"),
    team("Équipe B", "black", [player(n) for n in NAMES[:9]],
         pitch([["G"], ["DG", "DC", "DC", "DD"], ["MDF", "MDF"], ["MG", "MOF", "MD"], ["BU"]], free=(6, 10)),
         "4-2-3-1"),
])

EXTREME = card([
    team("W" * 30, "blue", [player("M" * 60, "Réceptionneur-attaquant")] * 30),
    team("Équipe B", "red", [player(f"Joueur {i}") for i in range(30)]),
], unassigned=[f"Remplaçant {i}" for i in range(40)], location="L" * 100)

DRAW = """async ([card]) => {
  const m = await import('/share.js');
  const image = await m.createImage(card, 'alfariq-2026-12-02.png');
  const info = image.info();
  const blob = await (await fetch(info.url)).blob();
  const bitmap = await createImageBitmap(blob);
  const data = await new Promise(done => { const r = new FileReader(); r.onload = () => done(r.result); r.readAsDataURL(blob); });
  image.dispose();
  let released = false;
  try { await fetch(info.url); } catch { released = true; }
  return { type: blob.type, size: blob.size, width: bitmap.width, height: bitmap.height, data, released };
}"""

SHARE = """async ([card, behaviour]) => {
  const calls = [];
  const fail = name => () => Promise.reject(new DOMException('x', name));
  const share = { ok: () => Promise.resolve(), abort: fail('AbortError'), error: fail('NotAllowedError') }[behaviour];
  Object.defineProperty(navigator, 'share', { configurable: true, value: share && (data => { calls.push(data); return share(); }) });
  Object.defineProperty(navigator, 'canShare', { configurable: true, value: share && (() => true) });
  const m = await import('/share.js');
  const image = await m.createImage(card, 'match.png');
  const result = { available: m.canShare(), shareable: image.info().canShare };
  if (share) {
    const pending = image.share();
    result.immediate = calls.length === 1;   // navigator.share part sans attente
    result.image = await pending;
    result.file = calls[0].files[0].name + ' ' + calls[0].files[0].type;
    result.text = await m.shareText('Bonjour');
    result.sent = calls[1].text;
  }
  image.dispose();
  return result;
}"""

with socketserver.TCPServer(("127.0.0.1", 0), Handler) as server:
    threading.Thread(target=server.serve_forever, daemon=True).start()
    url = f"http://127.0.0.1:{server.server_address[1]}/"
    output = pathlib.Path(sys.argv[1]) if len(sys.argv) > 1 else None

    with sync_playwright() as p:
        browser = p.chromium.launch()
        context = browser.new_context()
        context.grant_permissions(["clipboard-read", "clipboard-write"], origin=url.rstrip("/"))
        page = context.new_page()
        errors = []
        page.on("pageerror", lambda e: errors.append(str(e)))
        page.goto(url)

        # --- Image ---
        for name, content in (("listes", LISTS), ("terrain", PITCHES), ("extreme", EXTREME)):
            r = page.evaluate(DRAW, [content])
            check(f"image « {name} » : PNG de 1080 px de large", r["type"] == "image/png" and r["width"] == 1080 and r["size"] > 10000,
                  (r["type"], r["width"], r["size"]))
            check(f"image « {name} » : hauteur raisonnable", 400 < r["height"] < 8000, r["height"])
            check(f"image « {name} » : mémoire libérée", r["released"])
            if output:
                output.mkdir(parents=True, exist_ok=True)
                (output / f"{name}.png").write_bytes(base64.b64decode(r["data"].split(",", 1)[1]))

        colors = page.evaluate("""async ([card]) => {
          const { renderLineup } = await import('/lineup-image.js');
          const bitmap = await createImageBitmap(await renderLineup(card));
          const canvas = new OffscreenCanvas(bitmap.width, bitmap.height);
          const context = canvas.getContext('2d');
          context.drawImage(bitmap, 0, 0);
          const pixels = context.getImageData(0, 0, bitmap.width, bitmap.height).data;
          const found = new Set();
          for (let i = 0; i < pixels.length; i += 4) found.add(`${pixels[i]},${pixels[i + 1]},${pixels[i + 2]}`);
          return { green: found.has('27,122,52'), spare: document.querySelectorAll('span').length };
        }""", [LISTS])
        check("la couleur d'équipe vient de la feuille de style", colors["green"])
        check("aucun élément de mesure ne reste dans la page", colors["spare"] == 0, colors["spare"])

        wrapped = page.evaluate("""async () => {
          const { wrap } = await import('/lineup-image.js');
          const context = document.createElement('canvas').getContext('2d');
          context.font = '28px sans-serif';
          const lines = wrap(context, 'Abcdefghij'.repeat(8) + ' ⚽⚽⚽', 150, 2);
          return { count: lines.length, widest: Math.max(...lines.map(l => context.measureText(l).width)), last: lines.at(-1) };
        }""")
        check("un nom très long tient en deux lignes avec « … »",
              wrapped["count"] == 2 and wrapped["widest"] <= 150 and wrapped["last"].endswith("…"), wrapped)

        hyphen = page.evaluate("""async () => {
          const { wrap } = await import('/lineup-image.js');
          const context = document.createElement('canvas').getContext('2d');
          context.font = '28px sans-serif';
          const width = context.measureText('Christophe').width + 2;
          return [wrap(context, 'Jean-Christophe', width), wrap(context, 'Jean-Christophe', 600), wrap(context, 'A - B', 600)];
        }""")
        check("un nom composé revient à la ligne après le trait d'union",
              hyphen == [["Jean-", "Christophe"], ["Jean-Christophe"], ["A - B"]], hyphen)

        # --- Partage natif (simulé) ---
        for behaviour, expected in (("ok", "shared"), ("abort", "cancelled"), ("error", "failed")):
            page.reload()
            r = page.evaluate(SHARE, [LISTS, behaviour])
            check(f"partage ({behaviour}) : résultat « {expected} »", r["image"] == expected and r["text"] == expected, r)
            check(f"partage ({behaviour}) : fichier PNG et texte transmis sans attente",
                  r["immediate"] and r["file"] == "match.png image/png" and r["sent"] == "Bonjour", r)

        page.reload()
        r = page.evaluate(SHARE, [LISTS, "absent"])
        check("sans partage natif : ni texte ni image à partager", r == {"available": False, "shareable": False}, r)

        # --- Copie ---
        page.reload()
        text = "⚽ *Match*\n• Zoé"
        r = page.evaluate("""async ([text]) => {
          const m = await import('/share.js');
          const ok = await m.copyText(text);
          return { ok, read: await navigator.clipboard.readText() };
        }""", [text])
        check("copie du texte (emoji et retours à la ligne)", r["ok"] and r["read"].replace("\r\n", "\n") == text, r)

        page.evaluate("""() => {
          window.copied = null;
          document.addEventListener('copy', () => { window.copied = document.activeElement.value; });
        }""")
        page.evaluate("""() => {
          Object.defineProperty(navigator, 'clipboard', { configurable: true, value: undefined });
          document.getElementById('b').onclick = async () => { window.result = await (await import('/share.js')).copyText('repli'); };
        }""")
        page.click("#b")
        page.wait_for_function("window.result !== undefined")
        r = page.evaluate("() => ({ ok: window.result, copied: window.copied, left: document.querySelectorAll('textarea').length, focus: document.activeElement.id })")
        check("sans presse-papiers moderne : repli, puis retour du focus",
              r == {"ok": True, "copied": "repli", "left": 0, "focus": "b"}, r)

        check("aucune erreur JavaScript", not errors, errors)
        browser.close()

    server.shutdown()

print(f"\n{len(failures)} échec(s)")
sys.exit(1 if failures else 0)
