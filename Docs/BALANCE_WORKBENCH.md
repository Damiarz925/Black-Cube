# Black-Cube Balance Workbench

Open **Black-Cube → Balance Workbench** in the Unity Editor. The Workbench is an inspection, simulation, and authoring frontend over production systems; it is not a second balance implementation.

## Architecture and parity

- `SeededSimulationRandomSource` implements `ILootRandomSource` for reproducible experiments. Live rewards still come from `ProductionLootRandomSource`, backed by system entropy.
- `ProductionBalanceAdapters` calls the real `LootManager` rarity/weapon rules, `ModManager` affix/tier/value rolls, `Gear` local-stat calculations, `EnemyAI` candidate generation, `EnemyBuildOptimizer`, `EnemyScalingMath`, `EnemyLootProfile`, and `CurrencyLootTable`.
- Enemy samples use an isolated production enemy, run the actual optimizer, capture the result, and destroy the actor. Samples do not load or render a gameplay scene.
- Loot coefficients and currency entries live in the authoritative `Assets/Resources/LootBalanceProfile.asset`. Runtime drops and the Workbench read this same asset.
- Experiment metadata records timestamp, seed, sample count, level/scenario, Git revision, and a lightweight production-data fingerprint.
- Percentiles use the R-7 convention (linear interpolation at `(n - 1) × p`, equivalent to Excel `PERCENTILE.INC`).

## Presets, results, and charts

Use **Experiments → Save Current Preset** to write version-controlled JSON under `Assets/Balance/Presets`. Load that file to restore the three lab request forms and rerun the same seed. Results export to `ReviewCaptures/BalanceWorkbench` as CSV or JSON. Curve Explorer exports PNG charts there as well.

Charts support multiple series, series visibility toggles, middle-mouse pan, mouse-wheel zoom, reset, and PNG export. Every generated curve also has a table view.

Long runs display cancellable Unity progress. Unity object access remains on the main thread; simple item and drop loops reuse data/components and never create one GameObject per item. Enemy samples currently use one isolated production actor at a time because exact `EnemyAI`/optimizer parity takes priority over unsafe worker-thread access.

## Weapon / Item Lab

To answer “What does a normal level-50 Rare Dagger look like?” choose **Weapon**, **Dagger**, level **50**, rarity **Rare**, choose a sample count and seed, then Generate. Representative buttons open exact rolled affixes, tiers, values, base range, APS, crit, DPS, and GearScore.

To answer “What is P90 Sword DPS at level 80?” choose Sword, level 80, run the desired sample count, and read P90 in Weapon DPS. Export CSV for precise analysis. Natural Rarity uses the production level-weighted rarity table.

## Enemy Gear Lab

The Enemy Gear Lab does not invent average equipment. Every sample runs production candidate generation, the level-dependent candidate count, affix rolling, `EnemyBuildOptimizer`, equipment selection, intrinsic scaling, and final combat-stat capture.

**Production damage type** leaves generation unchanged. **Force Primary Damage** is a simulation-only input that supplies Physical, Fire, Cold, Lightning, or Void to production weapon candidate generation; the same optimizer then evaluates those candidates. It never edits the enemy definition asset.

Use representative Weakest/P50/P90/Strongest buttons to inspect equipped slots, candidate count, optimizer GearScore, final Life, hit, APS, DPS, and defenses. The archetype selector reads the production world catalog.

## Drop Simulator

Choose level, rarity, boss role, gear-quality mode, kill count, and seed. **Generate Actual Enemy Gear** invokes the Enemy Gear Lab production path and samples its realized GearQualityFactor distribution. **Fixed Gear Quality** provides a controlled preview value. Both then call the real production `LevelFactor`, rarity multiplier, LootPower budgets, stochastic rounding, weighted currency table, level/rarity/rebirth gates, quality bias, and stack ranges.

The per-currency table reports stable ID, production weight, chance per kill, average amount, amounts per 100/1,000 kills, and expected kills per drop. Ancient currencies appear only when their real gates permit them.

**Fixed Gear Quality** is a simulation override and never changes production. In **Settings / Validation**, edits are pending until **Apply to Production** is confirmed. The operation records Unity Undo, dirties the authoritative profile, and saves it. Reload/revert can use Unity Undo or reselect the asset; no Workbench-only copy exists.

## Validation

**Validate Balance Tooling** checks the production ModDatabase, all six weapon profiles, enemy catalog, optimizer/scaling/loot data, deterministic RNG, entropy-backed production RNG, and finite chart data. Tooling tests cover deterministic repetition, changed-seed divergence, production adapter usage, level candidate rules, drop-profile source-of-truth behavior, curve safety, and PNG export.

The legacy giant BalanceLab is intentionally separate and is not run by this Workbench or its validation.
