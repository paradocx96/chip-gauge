# ChipGauge brand assets

Vector masters live here; raster outputs are generated into `generated/`.

## Masters

| File | Purpose |
|------|---------|
| `icon.svg` | Full-color app icon (chip tile + gauge). Source for `.ico`/`.icns` and PNGs. |
| `icon-mono.svg` | Single-color glyph for tray/status icons. `{COLOR}` is substituted at render time. |
| `logo.svg` | Horizontal lockup: glyph + "ChipGauge" wordmark (transparent background). |
| `banner.svg` | Self-contained dark README banner with tagline and feature chips. |

## Regenerating rasters

```bash
pip install cairosvg Pillow
python3 build/generate-icons.py
```

This writes PNGs (16–512 px), a multi-resolution `favicon.ico` / `icon.ico`,
light/dark tray PNGs, and `logo.png` / `banner.png` into `generated/`.

## Palette

| Token | Hex | Use |
|-------|-----|-----|
| Violet | `#7C3AED` | Primary gradient start, brand accent |
| Indigo | `#4F46E5` | Primary gradient mid, hub |
| Sky | `#0EA5E9` | Primary gradient end |
| Cyan | `#22D3EE` | Gauge value / live-data highlight |
| Slate 900 | `#0B1020` | Dark surfaces / banner background |
| Slate 50 | `#F8FAFC` | Light text / needle |

The same gradient (violet → indigo → sky, with a cyan "live value" accent) is the
signature used across the icon, charts, and the app's accent color.
