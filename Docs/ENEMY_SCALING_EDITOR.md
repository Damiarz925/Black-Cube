# Enemy Scaling Editor

Open **Black-Cube → Balance Workbench → Enemy Scaling**. The intrinsic source is `Assets/Resources/EnemyScalingProfile.asset`; rarity and corruption modifiers live in `Assets/Resources/GameData/WorldContentDatabase.asset`. Runtime and Workbench both call `EnemyScalingMath` and the exact enemy generation path.

## Intrinsic production values

The scaling asset owns the actual formula inputs: curve break level; pre/post-break Life growth; pre/post-break damage growth; Armour per level; resistance points per level; and resistance cap. Intrinsic charts cover levels 1–360 for Life factor, damage factor, Armour, and resistance. The cursor table inspects a selected level.

Fields edit an isolated staging snapshot labeled **Preview Only — Production Unchanged**. Graphs refresh from staging without dirtying an asset. **Reset Preview / Reload Production Values** discards staging. **Apply To Production** shows the complete old/new diff, requires confirmation, records Unity Undo, applies through `EnemyScalingProfile`, marks and saves only that asset, clears affected caches, and regenerates. Unity Undo restores the prior values.

## Gear-realized curves and overlays

Generated curves run actual candidate generation, affix rolls, optimizer selection, rarity, corruption, and final-stat capture at every level. Select enemy or aggregate, rarity, corruption, metric, range, increment, samples, seed, and optional forced damage. Available metrics are Life, damage per hit, DPS, Armour, GearScore, primary resistance, and regeneration. Every run returns P10, P50, mean, P90, and P99 and supports CSV/PNG export.

Intrinsic overlays show production rarity and corruption modifiers. Gear-realized buttons generate P50 overlays for all four rarity profiles or all 0/20/40/60/80/100 corruption profiles. The stale-data warning compares the saved result fingerprint against current enemy/scaling/skill/rarity/corruption/phase data.

Corruption and rarity have separate staging editors. Corruption exposes only implemented damage, speed, recovery, and mechanic identity. Rarity exposes spawn weight, generated item rarity, and runtime Life/Damage/Speed multipliers. Preview, curves, and exact enemy generation consume matching staged overrides; Apply is explicit, confirmed, Undoable, saved, and cache-invalidating.

## Player comparison and guide lines

The selected Player Build supplies analytical TTK/TTD against the selected exact enemy preview. A loaded Tooling 2 player sweep remains available in Player vs Enemy for normalized first-point-to-100 growth charts, avoiding a misleading raw Player DPS vs enemy Life axis. Desired ordinary/boss TTK values are preview-only visual references and never drive production changes.

Enemy Preview is the seed-exact inspection surface; Batch Analyzer is the biome/all-enemy distribution surface. Neither auto-balances data. Scaling, rarity, and corruption changes occur only through their explicit Apply controls.
