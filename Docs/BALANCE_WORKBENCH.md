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

## Tooling 2: player builds and optimization

The same window now includes **Player Build**, **Gear Curves**, **Passive Opt**, **Heatmap**, **Scenario**, and **Player vs Enemy**. A `PlayerBuildSnapshot` is pure serializable data: player/combat level, stable class/subclass/weapon IDs, projectile mode, exact item rolls, and exact passive stable IDs. It deliberately excludes transient encounter state. Generated and manual snapshots work outside Play Mode; curve points retain the exact snapshot and seed and can be opened back in Player Build.

`PlayerBuildEvaluator` materializes one hidden isolated actor, applies `PlayerStatSetup`, production `Gear` local/global routing, `PlayerProgression.LevelLifeBonus`, authored passive effects, the shared `SubclassStatPackage`, `DerivedStatCalculator`, and the normal `PlayerController` hit/crit/attack-speed pipeline. Cooldowns use the `PlayerSkillController` production formula and skills come from `PlayerSkills.asset`. The actor is destroyed immediately after capture. Ailment DPS is explicitly labeled as a neutral, zero-resistance single-application analytical basis; it is not presented as rotation or encounter DPS.

### Objective score

The metric catalog supplies readable raw and derived objectives. Weighted mode does not add unlike raw units. For each metric it calculates signed log-relative improvement:

`log((max(0, candidate) + floor) / (max(0, baseline) + floor))`

where `floor = max(0.000001, abs(baseline) × 0.05)`. Primary and secondary normalized values are combined by the displayed weights. Lexicographic mode orders Primary first and uses Secondary only as the second priority. This score means “fit for this selected scenario objective”; it is not a universal Black-Cube power rating.

### Gear profiles and gear search

Editable simulation-only profiles live in `Assets/Balance/Profiles`:

- `SO_PlayerGearProfile_Low`
- `SO_PlayerGearProfile_Mid`
- `SO_PlayerGearProfile_Optimized`

Their defaults are assumptions, not production balance. They control legal candidates per slot, retention, natural/allowed rarity, selection strategy, and gearset beam width. Crafting, Empowered modifiers, boss-special modifiers, and deep-endgame repairs default off. Item level resolves independently from player/combat progression and remains capped by production at 100.

Every candidate is a real `ModManager.RollEquipmentModsForItem` result with production rarity, element, weapon profile, implicit, affix, tier, and value rules. Fast mode retains one greedy state. Thorough mode expands slot-by-slot, evaluates full gearsets, deduplicates deterministic signatures, and retains the configured beam. Item contribution is controlled ablation: full score minus the score with that slot removed, so interacting contributions need not sum exactly.

### Passive search, marginal values, and heatmap

Both greedy and beam search submit every candidate allocation to `PlayerProgression.ValidateAllocationState`; there is no editor copy of spine, cross-class, weapon, subclass, or choice-exclusivity rules. Greedy records immediate deltas and does not claim global optimality. Beam search deduplicates allocation signatures and first preserves the best state in each topology group (deepest tier in every class and weapon route), then fills remaining width by objective score. This keeps promising travel states alive without an unbounded exhaustive level-100 search.

The marginal analyzer evaluates legal next nodes. Minimum-path analysis constructs the class/weapon spine package required by the target and accepts it only if the production validator approves the final package. Path-adjusted value is total objective gain divided by added point count.

The Heatmap is an editor-only overlay over `PassiveTreeDefinition.LayoutPosition` and `PassiveTreeDefinition.Edges`, the exact data used by the production tree view. It does not modify sprites or Branch assets. Selecting a result can ping its owning `PassiveClassBranchSO` or `PassiveWeaponBranchSO` for immediate authoring.

### Sweeps and enemy comparison

Gear Curves generates full legal sets for Low, Mid, and Optimized profiles. Scenario Sweep can also optimize passives, run independently at each level, or carry a progressive character forward with optional free respec. Each point stores the exact build, profile/version, seed, fingerprint, metrics, and objective score.

Player vs Enemy reuses the exact Enemy Gear Lab sample set. It reports neutral analytical `enemy Life / player DPS` TTK and `player Life / enemy DPS` TTD ratios for percentile enemies. These are not combat simulations. Normalized mode indexes each curve’s first point to 100 so growth shapes can be compared without plotting incompatible units on one raw axis.

The production-data fingerprint includes affix, enemy, loot, skill, passive database/Branch, and player gear-profile assets. Editing a passive Branch or gear profile therefore changes the fingerprint and invalidates the assumptions behind an older result.
# Tooling 3 enemy authoring

The same Balance Workbench now includes Enemy Authoring, Enemy Scaling, Enemy Preview, Behavior Preview, and Boss / Phase Authoring. These surfaces read the authoritative Assets/Resources/GameData/WorldContentDatabase.asset, production EnemyScalingProfile, EnemyAI, and EnemyBuildOptimizer.

Enemy, behavior, phase, scaling, rarity, and corruption edits are staged. They do not write production data until the corresponding validated Apply button is confirmed. Production applications use Unity Undo, dirty and save only the authoritative asset, reload shared data, and invalidate cached results and the production-data fingerprint.

Generated enemy curves use real candidate generation and `EnemyBuildOptimizer` at every sampled level and expose P10/P50/mean/P90/P99 for Life, hit damage, DPS, Armour, GearScore, primary resistance, and regeneration. Enemy Preview supports exact seed reproduction, forced-damage experiments, gear/behavior inspection, and four-way comparison. Behavior Preview uses the same deterministic resolver as runtime and records full **Why This Action?** traces. Boss/phase, rarity, and 0–100% corruption editing are views over the production catalog rather than Workbench-only copies.

See ENEMY_AUTHORING_TOOLS.md, ENEMY_BEHAVIOR_AUTHORING.md, and ENEMY_SCALING_EDITOR.md.

## Tooling 4: headless combat laboratory

The Workbench now includes Combat Lab, Combat Timeline, Batch Matchups, Boss Lab, and Combat Curves. It uses an event-driven pure combat state with deterministic fight seeds, actual attack/skill/projectile/ailment/resource sequencing, shared production mitigation/Rage/behavior/phase services, exact Tooling 2 player snapshots, and exact Tooling 3 enemy snapshots. Single fights retain inspectable traces; batches retain aggregates and outlier seeds without retaining 10,000 full timelines.

Results cover Win/Loss/Timeout/Error, complete duration percentiles, remaining Life, DPS, damage source/type, healing/overheal, Mana starvation, Rage, ailments, skills, projectiles, and boss phase time. Action and Rage Finisher policies are saved simulation-only assumptions. Matchup matrices, level/corruption/rarity/gear-profile sweeps, policy comparison, trace replay, CSV/JSON/PNG exports, stale fingerprint warnings, and safety repro artifacts are integrated into the same window.

See `COMBAT_LAB.md`, `COMBAT_SIMULATION_ARCHITECTURE.md`, and `PLAYER_COMBAT_POLICIES.md`.
