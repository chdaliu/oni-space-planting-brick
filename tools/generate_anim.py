#!/usr/bin/env python3
"""Generate the Space Planting Brick KAnim art from the vanilla Hydroponic Farm.

The source texture is the game's ``farmtilehydroponicrotating_0`` atlas. The
symbols we need are cropped straight out of that atlas with the UV rectangles
from the vanilla build file:

* ``base`` - the solid 1x1 farm tile used in the world.
* ``place`` - the vanilla placement-blueprint line art.
* ``ui``   - the build-menu icon.

Rather than painting a new tile from scratch, the script keeps the vanilla
shape, shading, scratches, and metal frame, then recolors it into a cold
deep-space palette: gunmetal blue-grey steel, a cyan nutrient bed, and dark
indigo growing medium.

The world symbol keeps the vanilla sprite size, build bounds, and animation
offset, and the build icon keeps the vanilla UI bounds. Nothing is stretched
or given a custom anchor.
"""
from __future__ import annotations

import colorsys
import struct
from pathlib import Path

from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
ASSETS = ROOT / "anim" / "assets" / "space_planting_brick"
SOURCE = ROOT / "tools" / "vanilla" / "hydroponic_farm_atlas.png"
NAME = "space_planting_brick"

TILE_HASH = None  # filled in main()
PLACE_HASH = None
UI_HASH = None

# Bounds are (center_x, center_y, width, height). These are copied verbatim
# from the vanilla farmtilehydroponicrotating build file, along with the
# animation-frame offsets below, so the brick uses the same placement as the
# vanilla Hydroponic Farm.
WORLD_BOUNDS = (-10.75, 3.85, 248.0, 248.0)
PLACE_BOUNDS = (0.75, 0.0, 248.0, 248.0)
UI_BOUNDS = (1.55, -96.175, 244.0, 228.0)

# Vanilla animation frame bounds and element translations. The element
# translation is what places the sprite; omitting it makes the art look
# shifted relative to the vanilla building.
WORLD_FRAME_BOUNDS = (2.0, -99.3, 220.8, 217.5)
WORLD_OFFSET = (12.75, -103.15)
PLACE_FRAME_BOUNDS = (2.75, -99.3, 220.2, 217.3)
PLACE_OFFSET = (2.0, -99.3)
UI_FRAME_BOUNDS = (-3.9, -59.9, 355.6, 379.0)
UI_OFFSET = (0.9, -1.85)

# UV rectangles from the vanilla farmtilehydroponicrotating build file. The
# atlas is stored top-left first, so these map directly onto PIL crop boxes.
BASE_UV = (0.0, 0.484375, 0.484375, 0.96875)
PLACE_UV = (0.0, 0.0, 0.484375, 0.484375)
UI_UV = (0.484375, 0.484375, 0.9609375, 0.9296875)

ANIMS = ("idle", "off", "on", "place", "ui")


def sdbm_lower(text: str) -> int:
    h = 0
    for ch in text.lower():
        h = (ord(ch) + (h << 6) + (h << 16) - h) & 0xFFFFFFFF
    return (h ^ 0x80000000) - 0x80000000


def kstring(text: str) -> bytes:
    data = text.encode("utf-8")
    return struct.pack("<i", len(data)) + data


# ---------------------------------------------------------------------------
# vanilla art / recolouring
# ---------------------------------------------------------------------------

def crop_uv(image: Image.Image, uv: tuple[float, float, float, float]) -> Image.Image:
    """Crop a UV rectangle from the source atlas."""
    w, h = image.size
    x0 = round(uv[0] * w)
    y0 = round(uv[1] * h)
    x1 = round(uv[2] * w)
    y1 = round(uv[3] * h)
    return image.crop((x0, y0, x1, y1))


def lerp(a: float, b: float, t: float) -> float:
    return a + (b - a) * t


def recolor(image: Image.Image) -> Image.Image:
    """Shift the vanilla palette into cold space colours.

    The vanilla tile is mostly low-saturation steel, a warm ochre grow bed,
    and dark olive soil. We classify each pixel by hue/saturation so the
    original shading, scratches, and outlines survive the palette change.
    """
    out = image.copy()
    px = out.load()
    for y in range(out.height):
        for x in range(out.width):
            r, g, b, a = px[x, y]
            if a == 0:
                continue

            hue, sat, val = colorsys.rgb_to_hsv(r / 255.0, g / 255.0, b / 255.0)

            if sat < 0.18:
                # Steel and dark outlines: cool gunmetal instead of neutral grey.
                nr = r * 0.78
                ng = g * 0.86
                nb = min(255.0, b * 1.04 + 8.0)
                px[x, y] = (int(nr), int(ng), int(nb), a)
            elif hue < 0.14 and sat > 0.35:
                # Warm ochre grow bed -> teal/cyan. The original warm-to-dark
                # gradient is preserved by mapping its hue range across cyan.
                nh = lerp(0.45, 0.52, min(1.0, max(0.0, (hue - 0.04) / 0.10)))
                nr, ng, nb = colorsys.hsv_to_rgb(
                    nh, min(1.0, sat * 1.02), min(1.0, val * 1.06))
                px[x, y] = (int(nr * 255), int(ng * 255), int(nb * 255), a)
            elif 0.08 < hue < 0.35 and sat > 0.18:
                # Dark olive growing medium -> deep indigo.
                nh = lerp(0.62, 0.70, min(1.0, max(0.0, (hue - 0.08) / 0.22)))
                nr, ng, nb = colorsys.hsv_to_rgb(
                    nh, min(1.0, sat * 1.05), val * 0.80)
                px[x, y] = (int(nr * 255), int(ng * 255), int(nb * 255), a)
            # Remaining hues (the little red warning light) stay as-is so the
            # vanilla indicator detail still reads.
    return out


def make_world(source: Image.Image) -> Image.Image:
    """Return the recoloured vanilla base sprite at its native size/bounds."""
    return recolor(crop_uv(source, BASE_UV))


def make_place(source: Image.Image) -> Image.Image:
    """Return the vanilla placement-blueprint sprite."""
    return crop_uv(source, PLACE_UV)


def make_ui(source: Image.Image) -> Image.Image:
    """Return the recoloured vanilla UI icon at its native size/bounds."""
    return recolor(crop_uv(source, UI_UV))


# ---------------------------------------------------------------------------
# kanim writer
# ---------------------------------------------------------------------------

def pad_edge(img: Image.Image, pad: int) -> Image.Image:
    """Extend the outer pixels so bilinear sampling cannot bleed in."""
    w, h = img.size
    out = Image.new("RGBA", (w + pad * 2, h + pad * 2), (0, 0, 0, 0))
    out.paste(img, (pad, pad))
    out.paste(img.crop((0, 0, w, 1)).resize((w, pad)), (pad, 0))
    out.paste(img.crop((0, h - 1, w, h)).resize((w, pad)), (pad, pad + h))
    out.paste(img.crop((0, 0, 1, h)).resize((pad, h)), (0, pad))
    out.paste(img.crop((w - 1, 0, w, h)).resize((pad, h)), (pad + w, pad))
    for sx, sy in ((0, 0), (w - 1, 0), (0, h - 1), (w - 1, h - 1)):
        colour = img.getpixel((sx, sy))
        x = 0 if sx == 0 else pad + w
        y = 0 if sy == 0 else pad + h
        out.paste(Image.new("RGBA", (pad, pad), colour), (x, y))
    return out


def write_kanim(world: Image.Image, place: Image.Image,
                ui: Image.Image) -> None:
    ASSETS.mkdir(parents=True, exist_ok=True)

    # Padding around each symbol prevents bilinear filtering from sampling the
    # neighbouring symbol. The world tile is edge-extended so its border never
    # turns into a transparent seam.
    pad = 4
    world_pad = pad_edge(world, pad)

    def pad_transparent(img: Image.Image) -> Image.Image:
        out = Image.new("RGBA",
                        (img.width + pad * 2, img.height + pad * 2),
                        (0, 0, 0, 0))
        out.paste(img, (pad, pad))
        return out

    place_pad = pad_transparent(place)
    ui_pad = pad_transparent(ui)

    def next_pow2(value):
        result = 1
        while result < value:
            result <<= 1
        return result

    # Symbol order matches the vanilla build: base, place, ui.
    symbols = (
        ("tile", TILE_HASH, WORLD_BOUNDS, world_pad, world),
        ("place", PLACE_HASH, PLACE_BOUNDS, place_pad, place),
        ("ui", UI_HASH, UI_BOUNDS, ui_pad, ui),
    )

    atlas = Image.new(
        "RGBA",
        (next_pow2(sum(item[3].width for item in symbols)),
         next_pow2(max(item[3].height for item in symbols))),
        (0, 0, 0, 0))

    uvs = {}
    x = 0
    for name, _, _, padded, img in symbols:
        atlas.paste(padded, (x, 0))
        uvs[name] = (
            (x + pad) / atlas.width,
            pad / atlas.height,
            (x + pad + img.width) / atlas.width,
            (pad + img.height) / atlas.height,
        )
        x += padded.width
    atlas.save(ASSETS / f"{NAME}_0.png")

    build = bytearray()
    build += b"BILD"
    build += struct.pack("<i", 10)
    build += struct.pack("<i", len(symbols))  # symbols
    build += struct.pack("<i", len(symbols))  # total frames
    build += kstring(NAME)
    for name, symbol_hash, bounds, _, _ in symbols:
        build += struct.pack("<i", symbol_hash)
        build += struct.pack("<i", symbol_hash)
        build += struct.pack("<i", 0)  # channels
        build += struct.pack("<i", 0)  # flags
        build += struct.pack("<i", 1)  # frames
        build += struct.pack("<i", 0)  # sequence
        build += struct.pack("<i", 1)  # duration
        build += struct.pack("<i", 0)  # texture image
        build += struct.pack("<4f", *bounds)
        build += struct.pack("<4f", *uvs[name])
    build += struct.pack("<i", len(symbols))  # hash table entries
    for name, symbol_hash, _, _, _ in symbols:
        build += struct.pack("<i", symbol_hash)
        build += kstring(name)
    (ASSETS / f"{NAME}_build.bytes").write_bytes(build)

    anim = bytearray()
    anim += b"ANIM"
    anim += struct.pack("<i", 5)
    anim += struct.pack("<i", len(ANIMS))  # total frames
    anim += struct.pack("<i", len(ANIMS))  # total elements
    anim += struct.pack("<i", len(ANIMS))  # banks
    for anim_name in ANIMS:
        if anim_name == "ui":
            symbol_hash = UI_HASH
            frame_bounds = UI_FRAME_BOUNDS
            offset = UI_OFFSET
        elif anim_name == "place":
            symbol_hash = PLACE_HASH
            frame_bounds = PLACE_FRAME_BOUNDS
            offset = PLACE_OFFSET
        else:
            symbol_hash = TILE_HASH
            frame_bounds = WORLD_FRAME_BOUNDS
            offset = WORLD_OFFSET

        anim += kstring(anim_name)
        anim += struct.pack("<i", symbol_hash)
        anim += struct.pack("<f", 1.0)
        anim += struct.pack("<i", 1)
        anim += struct.pack("<4f", *frame_bounds)
        anim += struct.pack("<i", 1)
        anim += struct.pack("<i", symbol_hash)
        anim += struct.pack("<i", 0)
        anim += struct.pack("<i", symbol_hash)
        anim += struct.pack("<i", 0)
        anim += struct.pack("<4f", 1.0, 1.0, 1.0, 1.0)
        anim += struct.pack("<6f", 1.0, 0.0, 0.0, 1.0, offset[0], offset[1])
        anim += struct.pack("<f", 0.0)
    anim += struct.pack("<i", 0)
    anim += struct.pack("<i", len(symbols))
    for name, symbol_hash, _, _, _ in symbols:
        anim += struct.pack("<i", symbol_hash)
        anim += kstring(name)
    (ASSETS / f"{NAME}_anim.bytes").write_bytes(anim)


def main() -> None:
    global TILE_HASH, PLACE_HASH, UI_HASH
    TILE_HASH = sdbm_lower("tile")
    PLACE_HASH = sdbm_lower("place")
    UI_HASH = sdbm_lower("ui")

    if not SOURCE.exists():
        raise SystemExit(
            f"Missing vanilla source texture: {SOURCE}\n"
            "See tools/vanilla/README.md for how to extract it from the game.")

    source = Image.open(SOURCE).convert("RGBA")
    world = make_world(source)
    place = make_place(source)
    ui = make_ui(source)
    write_kanim(world, place, ui)
    preview = ui.resize((ui.width * 4, ui.height * 4), Image.LANCZOS)
    preview.save(ROOT / "tools" / "space_planting_brick_preview.png")
    print(f"wrote {ASSETS}")


if __name__ == "__main__":
    main()
