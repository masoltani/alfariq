#!/usr/bin/env python3
"""Génère les icônes de la PWA (marque provisoire : terrain vu de dessus sur fond vert).

Usage : python3 tools/generate-icons.py
Dépendance : Pillow (pip install pillow). Les PNG produits sont versionnés ; ce script ne sert qu'à les régénérer
quand la marque change.
"""
from pathlib import Path

from PIL import Image, ImageDraw

GREEN = (11, 110, 79, 255)
WHITE = (255, 255, 255, 255)
OUT = Path(__file__).resolve().parent.parent / "src" / "AlFariq.Web" / "wwwroot" / "icons"
SCALE = 4  # sur-échantillonnage pour des bords lisses


def draw_mark(draw: ImageDraw.ImageDraw, size: int, mark_ratio: float) -> None:
    """Terrain : cercle central, ligne médiane et cadre. Le motif reste dans la zone sûre des icônes « maskable »."""
    mark = size * mark_ratio
    left = (size - mark) / 2
    top = (size - mark) / 2
    right = left + mark
    bottom = top + mark
    line = max(2, round(size * 0.028))

    draw.rounded_rectangle((left, top, right, bottom), radius=mark * 0.08, outline=WHITE, width=line)
    draw.line((left, size / 2, right, size / 2), fill=WHITE, width=line)
    r = mark * 0.2
    draw.ellipse((size / 2 - r, size / 2 - r, size / 2 + r, size / 2 + r), outline=WHITE, width=line)
    d = line * 1.1
    draw.ellipse((size / 2 - d, size / 2 - d, size / 2 + d, size / 2 + d), fill=WHITE)


def make_icon(size: int, *, maskable: bool) -> Image.Image:
    big = size * SCALE
    image = Image.new("RGBA", (big, big), (0, 0, 0, 0))
    draw = ImageDraw.Draw(image)

    if maskable:
        draw.rectangle((0, 0, big, big), fill=GREEN)  # plein cadre : le système applique son propre masque
        draw_mark(draw, big, 0.52)  # reste dans les 80 % centraux recommandés
    else:
        draw.rounded_rectangle((0, 0, big - 1, big - 1), radius=big * 0.22, fill=GREEN)
        draw_mark(draw, big, 0.56)

    return image.resize((size, size), Image.LANCZOS)


def main() -> None:
    OUT.mkdir(parents=True, exist_ok=True)
    targets = [
        ("icon-192.png", 192, False),
        ("icon-512.png", 512, False),
        ("icon-maskable-512.png", 512, True),
        ("apple-touch-icon.png", 180, True),  # iOS applique lui-même les coins arrondis
        ("favicon.png", 64, False),
    ]
    for name, size, maskable in targets:
        make_icon(size, maskable=maskable).save(OUT / name, optimize=True)
        print(f"{name}: {size}x{size}{' (maskable)' if maskable else ''}")


if __name__ == "__main__":
    main()
