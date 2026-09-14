# Installed temporary forest background cycle

The six user-supplied PNGs from `D:/Documents/BlackCubeAssets/Level Art/Forest/Forest 1/` are copied byte-for-byte unchanged to `Assets/Art/PaperBattle/ForestCycle/` in the main checkout.

| Forest | Supplied image | Combat levels |
| --- | --- | --- |
| 1 | 0_Percent.png | 1–10 |
| 2 | 20_Percent.png | 11–20 |
| 3 | 40_Percent.png | 21–30 |
| 4 | 60_Percent.png | 31–40 |
| 5 | 80_Percent.png | 41–50 |
| 6 | 100_Percent.png | 51–60 |

Level 61 starts Forest 1 again; the sequence repeats every 60 combat levels. This is a fixed image schedule, not a corruption gameplay mechanic.

`ZoneManager.GenerateZone` updates the existing paper background renderer before its disabled-3D-generation early return. The current `GameManager.StartZone` path already calls it whenever the combat level changes. The paper prefab has all six ordered references and starts on 0%; the scene builder reproduces the setup. The HUD uses labels such as `FOREST 2 · LEVEL 11`. Unconfigured legacy zones retain their original labels and generation behavior.

All images are 1672×941. Single-sprite, centered-pivot imports use 94.1 PPU (10 world units tall), bilinear filtering, clamp wrapping, no mipmaps or compression, and no NPOT resizing. No player idle/attack asset or combat timing changes were made.

Verification: 729 assertions compiling the actual ZoneManager with minimal Unity substitutes passed for all levels 1–180, boundaries, looping, direct loads, reset, invalid and large levels, renderer updates with 3D generation disabled, labels and legacy fallback. Source hashes, six ordered prefab references, initial renderer sprite, import settings and scoped diff checks passed. See `asset-checks.json` and `installation-checks.json`.

Visual overview: `forest-cycle-contact.png`. Unity import/rendering and in-game progression were not exercised; no Unity launch, Play controls, batch mode or desktop interaction was performed. No commit or push was requested.
