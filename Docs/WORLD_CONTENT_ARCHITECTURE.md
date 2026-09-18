# World content architecture

Step 15 establishes the authoring and runtime contract for the V1 main progression. It creates the structure, resolver, validation, and reference migration; it does not claim that the placeholder world is production content.

## Authoritative mapping

Let `L = clamp(requested combat level, 1, 360)`, `z = L - 1`, and use corruption percentages `[0, 20, 40, 60, 80, 100]`.

- Biome index: `floor(z / 60)`; displayed as biome 1–6.
- Position inside the biome: `z % 60`.
- Corruption index: `floor((z % 60) / 10)`.
- Base-location index: `(z % 60) % 10`; displayed as location 1–10.
- Encounter stages 1–9 resolve a weighted normal-enemy pool.
- Encounter stage 10 resolves the location table's boss.

This deliberately orders each biome as ten locations at 0%, the same ten at 20%, and so on through 100%. Thus level 60 is biome 1/location 10/100%; level 61 is biome 2/location 1/0%; and level 360 is biome 6/location 10/100%.

Combat levels above 360 retain their actual numerical level for scaling and saves but resolve the final authored world position and table as an explicitly labelled endless fallback. The final post-360 experience remains a future design decision.

## Definitions and stable IDs

`WorldContentDatabase` owns lists of `BiomeDefinition`, `CorruptionTierDefinition`, `EnemyArchetypeDefinition`, `BossDefinition`, `EncounterTableDefinition`, and progression-independent `ChallengeEncounterDefinition` records. A biome owns exactly ten `LocationDefinition` records. A location references its encounter table by stable ID and may provide a base background, six corruption-specific background/overlay presentations, an optional environment-set ID, and an enemy-spawn presentation ID.

IDs are content identity and must not be derived from scene-object names. Reference IDs use namespaces such as `biome-01`, `location.biome-01.01`, `encounter.biome-01.placeholder`, `enemy.goblin`, and `boss.hobgoblin`. Future saves, Codex pages, achievements, unlocks, challenge encounters, and special-affix pools should use these IDs.

An encounter table owns weighted normal-enemy archetype references, one boss reference, and reserved lists for future enemy skills, encounter modifiers, and reward/content hooks. Selection uses a stable local seed from combat level, stage, and table ID; it does not alter Unity's global random state.

Boss definitions reserve presentation, Codex, mechanic, skill, reward, and story hooks. Challenge definitions are not inserted into the main 9+1 loop: they carry their own content ID, unlock level/requirements, entry resource and amount, boss, reward resource, special-affix pool ID, and repeatability flag for a future progression-independent launcher.

## Current reference content

The runtime reference catalog maps all 360 positions so the architecture is executable and testable. Only `enemy.goblin`, `boss.hobgoblin`, and the existing six paper-forest corruption images are current production references. All six biome records and all sixty location records are marked placeholders and reuse that art and encounter content. Biomes 2–6 are explicitly named `Placeholder`; this is not the V1 content set and Step 16 has not begun.

The scene may later assign an authored `WorldContentDatabase` asset. Until then, `WorldContentCatalog.Reference` is the authoritative fallback. `BattleManager` resolves definitions first and uses its existing serialized Goblin/Hobgoblin prefab references when the reference definitions intentionally have no prefab asset. `ZoneManager` resolves location/corruption presentation first and uses its six current forest sprites as the matching reference fallback.

## Validation and authoring workflow

Run **Black Cube → Validation → Validate World Content** or `Step15TestRunner.RunWorldValidation`. Validation rejects duplicate/missing stable IDs, noncanonical corruption tiers, missing or overlapping biome ranges, a biome without ten locations, incomplete corruption presentations, invalid enemy/boss/table references, invalid challenge data, any missing level mapping from 1–360, a normal stage without a valid enemy, or stage 10 without a valid boss.

Before adding production content, create definitions rather than branching arithmetic in `GameManager`, `ZoneManager`, HUD code, or save DTOs. Combat level remains the persisted authority; biome, location, corruption, and encounter table are derived and therefore require no schema-8 duplication.
