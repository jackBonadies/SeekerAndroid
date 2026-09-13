#!/usr/bin/env python3
"""Regenerate the launcher icon bitmaps in Seeker/Resources/mipmap-* from the 512px
rasters in Seeker/Assets.

Sources
    seeker_logo_raster.png        -> ic_launcher{,_round,_foreground}
    seeker_logo_source_mono.png   -> ic_launcher_mono_foreground
"""

from __future__ import annotations

import argparse
import os
import sys

try:
    from PIL import Image, ImageDraw, ImageFilter
except ImportError:
    sys.exit("Pillow is required:  pip install Pillow")

REPO = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
ASSETS = os.path.join(REPO, "Seeker", "Assets")
RES = os.path.join(REPO, "Seeker", "Resources")

COLOUR_PNG = os.path.join(ASSETS, "seeker_logo_raster.png")
MONO_PNG = os.path.join(ASSETS, "seeker_logo_source_mono.png")

DENSITIES = {"mdpi": 1.0, "hdpi": 1.5, "xhdpi": 2.0, "xxhdpi": 3.0, "xxxhdpi": 4.0}

ADAPTIVE_DP = 108
LEGACY_DP = 48

FOREGROUND_SCALE = 0.68          # white plate / adaptive canvas
LEGACY_SQUARE_SCALE = 38 / 48    # rounded square / legacy canvas
LEGACY_ROUND_SCALE = 44 / 48     # circle / legacy canvas
SQUARE_CORNER = 10 / 152         # corner radius / square side

# Drop shadow, as fractions of the legacy canvas.
SHADOW_BLUR = 2.5 / 192
SHADOW_DY = 3.0 / 192
SHADOW_ALPHA = 0.35

SS = 4                           # mask supersampling; ImageDraw has no antialiasing

STALE_TOLERANCE_SEC = 300


def load(png):
    svg = os.path.splitext(png)[0].replace("_raster", "_source") + ".svg"
    if os.path.exists(svg):
        behind = os.path.getmtime(svg) - os.path.getmtime(png)
        if behind > STALE_TOLERANCE_SEC:
            print("WARNING: %s is %d min newer than %s -- re-export it first."
                  % (os.path.basename(svg), behind // 60, os.path.basename(png)))
    with Image.open(png) as img:
        return img.convert("RGBA")


def scaled(master, px):
    return master.resize((px, px), Image.LANCZOS)


def on_white(art):
    plate = Image.new("RGBA", art.size, (255, 255, 255, 255))
    plate.alpha_composite(art)
    return plate


def rounded_square_mask(size, radius):
    mask = Image.new("L", (size * SS, size * SS), 0)
    ImageDraw.Draw(mask).rounded_rectangle(
        [0, 0, size * SS - 1, size * SS - 1], radius=radius * SS, fill=255
    )
    return mask.resize((size, size), Image.LANCZOS)


def circle_mask(size):
    mask = Image.new("L", (size * SS, size * SS), 0)
    ImageDraw.Draw(mask).ellipse([0, 0, size * SS - 1, size * SS - 1], fill=255)
    return mask.resize((size, size), Image.LANCZOS)


def origin(canvas, size):
    """Top-left of a centred `size` square.  Half-pixels round *up*, which is what
    Image Asset Studio did -- floor here shifts mdpi and xhdpi one pixel off."""
    return (canvas - size + 1) // 2


def centred(shape, canvas):
    out = Image.new("RGBA", (canvas, canvas), (0, 0, 0, 0))
    top_left = origin(canvas, shape.width)
    out.alpha_composite(shape, (top_left, top_left))
    return out


def with_shadow(shape, canvas):
    """Centre `shape` on a canvas-sized transparent square, over a soft drop shadow."""
    offset = origin(canvas, shape.width)

    shadow = Image.new("RGBA", (canvas, canvas), (0, 0, 0, 0))
    tint = Image.new("RGBA", shape.size, (0, 0, 0, int(255 * SHADOW_ALPHA)))
    shadow.paste(tint, (offset, offset + max(1, round(SHADOW_DY * canvas))), shape.getchannel("A"))
    shadow = shadow.filter(ImageFilter.GaussianBlur(max(1.0, SHADOW_BLUR * canvas)))

    shadow.alpha_composite(shape, (offset, offset))
    return shadow


def blacken(art):
    glyph = Image.new("RGBA", art.size, (0, 0, 0, 0))
    glyph.paste((0, 0, 0, 255), mask=art.getchannel("A"))
    return glyph


def main():
    parser = argparse.ArgumentParser(
        description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter
    )
    parser.add_argument(
        "--out-dir", default=RES, help="resources root to write into (default: Seeker/Resources)"
    )
    parser.add_argument("--densities", default=",".join(DENSITIES), help="comma-separated subset")
    args = parser.parse_args()

    colour = load(COLOUR_PNG)
    mono = load(MONO_PNG)

    for density in [d.strip() for d in args.densities.split(",") if d.strip()]:
        if density not in DENSITIES:
            sys.exit("unknown density %r" % density)
        factor = DENSITIES[density]
        adaptive = round(ADAPTIVE_DP * factor)
        legacy = round(LEGACY_DP * factor)
        plate_px = round(FOREGROUND_SCALE * adaptive)
        square_px = round(LEGACY_SQUARE_SCALE * legacy)
        round_px = round(LEGACY_ROUND_SCALE * legacy)

        out_dir = os.path.join(args.out_dir, "mipmap-%s" % density)
        os.makedirs(out_dir, exist_ok=True)

        # Legacy layers: self-contained shape plus shadow, for pre-26 launchers.
        square = on_white(scaled(colour, square_px))
        square.putalpha(rounded_square_mask(square_px, round(SQUARE_CORNER * square_px)))
        circle = on_white(scaled(colour, round_px))
        circle.putalpha(circle_mask(round_px))

        for name, image in (
            # Adaptive layers: no mask and no shadow, the launcher applies its own.
            ("ic_launcher_foreground", centred(on_white(scaled(colour, plate_px)), adaptive)),
            ("ic_launcher_mono_foreground", centred(blacken(scaled(mono, plate_px)), adaptive)),
            ("ic_launcher", with_shadow(square, legacy)),
            ("ic_launcher_round", with_shadow(circle, legacy)),
        ):
            path = os.path.join(out_dir, "%s.png" % name)
            image.save(path, optimize=True)
            print("  %s  %dx%d" % (path, image.width, image.height))

    print("\nDone. Optional: oxipng -o4 --strip safe Seeker/Resources/mipmap-*/ic_launcher*.png")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
