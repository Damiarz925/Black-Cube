# Enemy Authoring Tools

Open **Black-Cube → Balance Workbench → Enemy Authoring**. The authoritative production catalog is `Assets/Resources/GameData/WorldContentDatabase.asset`; runtime and Editor tooling load the same asset. It currently owns 48 archetypes, 66 bosses, 25 reusable skills, 12 loadouts, 12 behavior profiles, four rarity profiles, six corruption profiles, boss phases, encounters, and challenge data.

## Selecting and editing an enemy

Search by name, stable ID, or referenced skill, then filter by biome, rank, or primary damage type. The editor exposes only runtime-consumed properties: identity, rank, primary damage, loadout, behavior, optimizer build preference, and Life/Damage/Attack Speed multipliers. Biome is derived from encounter membership rather than duplicated on the enemy.

Changing a field edits a staging copy. **Validate** checks stable identity and every referenced production definition. **Apply Validated Enemy To Production** is the only normal write path: it asks for confirmation, records Undo, updates and dirties the database, saves that asset, reloads the catalog, and invalidates enemy results. Closing the Workbench discards unapplied staging.

To change primary damage identity, edit **Primary Damage** and apply. Gear preferences are selected by stable build-preference ID and consumed by `EnemyBuildOptimizer`. **Open Behavior** selects the assigned shared profile; **Run Gear Sample** opens the existing exact-production Enemy Gear Lab; **Preview Enemy** opens the single-enemy generator. **Duplicate As New Enemy** creates a new stable ID explicitly and leaves encounter placement to the designer.

## Shared skills and behavior

The Skill Library displays each production skill's kind, element, damage multiplier, hit count, and cadence. **Used By** searches loadouts, behavior profiles, ordinary enemies, elites, bosses, and challenge bosses. Skills remain reusable references; they are not copied into profiles.

Behavior profiles are shared. The Behavior tab reports all affected enemies and bosses before an apply. **Duplicate Profile For Selected Enemy** is the safe separation workflow: it generates a unique profile/rule identity, assigns the copy to the selected enemy, records Undo, and saves explicitly. Existing profiles also serve as the supported behavior templates; duplication copies a current production pattern without introducing new gameplay.

## Preview, comparison, and batch analysis

Enemy Preview calls `EnemyAI.GenerateIsolatedBuild`, `EnemyScalingMath`, production rarity/corruption profiles, real affix rolling, and `EnemyBuildOptimizer`. Same enemy, inputs, seed, and data fingerprint reproduce the same equipment and final stats. Force Primary Damage is a preview-only optimizer input. Reroll advances the seed; Previous Seed returns to the last result; locks preserve archetype or seed/gear. Up to four generated enemies can be compared with relative Life/DPS/Armour deltas and exported to CSV.

The Batch Analyzer runs all archetypes or one biome using the production generator. It reports mean/P90 Life and DPS, Armour, primary resistance, GearScore, skill/profile identity, sortable columns, and non-authoritative warnings for missing content, high spread, DPS/Life outliers, and unusually high resistance. Warnings never alter production.
