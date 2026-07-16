#!/usr/bin/env python3
"""Regenerate every raster branding asset from the SVG masters in ``assets/``.

Run from the repository root:

    python3 build/generate-icons.py

Requires ``cairosvg`` and ``Pillow`` (``pip install cairosvg Pillow``). The
outputs are committed so a normal build does not depend on this script; re-run it
only when a source SVG changes.
"""
from __future__ import annotations

import io
import os
from pathlib import Path

import cairosvg
from PIL import Image

ROOT = Path(__file__).resolve().parent.parent
ASSETS = ROOT / "assets"
OUT = ASSETS / "generated"

# App-icon raster sizes (also used to assemble the multi-resolution .ico/.icns).
PNG_SIZES = [16, 24, 32, 48, 64, 128, 256, 512]
ICO_SIZES = [16, 24, 32, 48, 64, 128, 256]


def render_png(svg_path: Path, size: int) -> Image.Image:
    """Rasterize an SVG to a square RGBA Pillow image."""
    png_bytes = cairosvg.svg2png(
        url=str(svg_path), output_width=size, output_height=size
    )
    return Image.open(io.BytesIO(png_bytes)).convert("RGBA")


def render_mono(color: str, size: int) -> Image.Image:
    """Rasterize the monochrome glyph substituting ``{COLOR}``."""
    template = (ASSETS / "icon-mono.svg").read_text(encoding="utf-8")
    svg = template.replace("{COLOR}", color)
    png_bytes = cairosvg.svg2png(
        bytestring=svg.encode("utf-8"), output_width=size, output_height=size
    )
    return Image.open(io.BytesIO(png_bytes)).convert("RGBA")


def main() -> None:
    OUT.mkdir(parents=True, exist_ok=True)

    # App icon PNGs.
    icons = {size: render_png(ASSETS / "icon.svg", size) for size in PNG_SIZES}
    for size, image in icons.items():
        image.save(OUT / f"icon-{size}.png")

    # favicon.ico (multi-resolution) + Windows app icon.ico.
    base = icons[256]
    base.save(
        OUT / "favicon.ico",
        format="ICO",
        sizes=[(s, s) for s in ICO_SIZES],
    )
    base.save(OUT / "icon.ico", format="ICO", sizes=[(s, s) for s in ICO_SIZES])

    # Monochrome tray icons for light and dark taskbars.
    for name, color in (("tray-dark", "#F8FAFC"), ("tray-light", "#0F172A")):
        for size in (16, 24, 32, 48):
            render_mono(color, size).save(OUT / f"{name}-{size}.png")

    # Marketing rasters.
    for name, width, height in (("logo", 760, 200), ("banner", 1280, 380)):
        png_bytes = cairosvg.svg2png(
            url=str(ASSETS / f"{name}.svg"), output_width=width, output_height=height
        )
        (OUT / f"{name}.png").write_bytes(png_bytes)

    generated = sorted(p.name for p in OUT.iterdir())
    print(f"Generated {len(generated)} assets in {os.path.relpath(OUT, ROOT)}:")
    for name in generated:
        print(f"  - {name}")


if __name__ == "__main__":
    main()
