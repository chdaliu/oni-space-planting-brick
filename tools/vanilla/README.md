# Vanilla source art

`hydroponic_farm_atlas.png` is the game's `farmtilehydroponicrotating_0`
texture atlas, extracted from `sharedassets0.assets` so the generator can
recolor the vanilla Hydroponic Farm instead of painting a new tile.

The generator uses the vanilla UV rectangles, bounds, and animation-frame
offsets, so the recoloured brick keeps the Hydroponic Farm's exact size and
placement.

| Symbol | UV rectangle | Bounds (center x, center y, w, h) | Frame offset |
| --- | --- | --- | --- |
| `base` (world tile) | `(0.0, 0.484375, 0.484375, 0.96875)` | `(-10.75, 3.85, 248, 248)` | `(12.75, -103.15)` |
| `place` (placement blueprint) | `(0.0, 0.0, 0.484375, 0.484375)` | `(0.75, 0, 248, 248)` | `(2.0, -99.3)` |
| `ui` (build icon) | `(0.484375, 0.484375, 0.9609375, 0.9296875)` | `(1.55, -96.175, 244, 228)` | `(0.9, -1.85)` |

To regenerate this file from a local ONI install, run the following with
UnityPy installed (the same extraction was used to create the checked-in
copy):

```python
import UnityPy

data = "~/Library/Application Support/Steam/steamapps/common/OxygenNotIncluded/"
data += "OxygenNotIncluded.app/Contents/Resources/Data/sharedassets0.assets"

env = UnityPy.load(data)
for obj in env.objects:
    if obj.type.name == "Texture2D" and obj.peek_name() == "farmtilehydroponicrotating_0":
        obj.read().image.save("tools/vanilla/hydroponic_farm_atlas.png")
        break
```

Then run:

```bash
python3 tools/generate_anim.py
```

The checked-in atlas is only a development input. The mod ships the recolored
derivative in `anim/assets/space_planting_brick/`.
