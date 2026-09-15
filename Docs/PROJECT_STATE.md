# Black-Cube project state

> This document answers: **What does the current repository actually do?** It records verified implementation reality, not intended design. Codex may update it after verified implementation milestones, but must not use it to invent design. Update the baseline commit, verification totals, and material behavior changes together.

For intended behavior, read [GAME_DESIGN_CONTRACT.md](GAME_DESIGN_CONTRACT.md). For the exhaustive current stat/affix inventory and Step 10 triage, read [STAT_AFFIX_AUDIT.md](STAT_AFFIX_AUDIT.md). For detailed ownership and persistence rules, read [RUNTIME_LIFECYCLE.md](RUNTIME_LIFECYCLE.md) and [SAVE_STATE_CONTRACT.md](SAVE_STATE_CONTRACT.md).

Current-reality claims use this evidence order: (1) verified source code, (2) scenes/prefabs/assets, (3) tests and verification tooling, (4) `RUNTIME_LIFECYCLE.md`, (5) `SAVE_STATE_CONTRACT.md`, then (6) `CODE_MAP.md` and `DEVELOPER_HANDOFF.md`. Lower-ranked summaries never override verified runtime evidence.

## 1. Verified baseline

- Repository branch: `codex/repository-cleanup-baseline`
- Verified Step 6 implementation commit: `76c5687f8fcee69d089eb9b3efc0c9df5549ea6a` (`Implement versioned save and load system`)
- Step 7 authority baseline: `a039bb1e116c919ee63a32913793d50da62d4ad5` (`Lock project state and game design contracts`)
- Step 8 cleanup baseline: `00bbc93f5153108e34cb133c81dccb3879168782` (`Remove obsolete Prestige runtime path`)
- Unity: `6000.6.0f1` (`f7f8ed4d1e24`)
- Enabled build scenes, in order: `Assets/Scenes/Main Menu.unity`, `Assets/Scenes/SampleScene.unity`
- Historical Step 6 source inventory: 94 runtime C# files, 23 Editor C# files, 12 EditMode test files, and 45 sources under `Tools` (`.cs`, `.py`, `.ps1`); Step 10 adds `DerivedStatCalculator` and `Step10MechanicsTests`.
- Historical Step 6 EditMode suite: 143/143 passing. **Fresh Step 10 Unity EditMode suite: 172/172 passing**, including 29 Step 10 fixture cases; local runtime and editor/test assembly compiles also succeed with 0 errors.
- Fresh Step 10 four synchronous real-scene equipment/combat, player-stat, status and progression checks: passing
- Fresh Step 10 rich save → Main Menu → load/New Game check: passing
- Fresh Step 10 six-entry lifecycle and Pause Menu soak: passing
- Fresh Step 10 missing-reference validation: passing with zero failures
- Fresh Step 10 Windows x64 strict build: succeeded, zero errors, 522 warnings; standalone headless smoke stayed responsive for 30 seconds through Main Menu startup (the unassigned placeholder Achievements button logged its known warning)
- **Fresh Steps 11 + 12 Unity EditMode suite: 184/184 passing**, including five EnemyScaling and seven BalanceSimulation cases. The four synchronous real-scene equipment/stat/status/progression checks, rich schema-2 menu/load/New Game fixture, six-entry lifecycle/Pause soak and scaling-profile-aware reference scan passed. The finalized 250-build baseline generated 4,500 sampled rows at seed 11012 in 329.0 seconds (25-sample quick: 33.8 seconds). A fresh strict Windows x64 build succeeded with zero errors and 522 existing warnings; its standalone headless process remained responsive for more than 30 seconds through Main Menu startup, with only the known unassigned placeholder Achievements button message.

Known baseline limits include one logical save slot; clean encounter-boundary rather than exact-frame restoration; functional/basic runtime-built menu overlays; two active enemy archetypes; six repeating forest backgrounds; no final campaign/content count; and the incomplete mechanics listed below.

## 2. Current product state

Black-Cube is presently a playable single-player paper-styled auto-battle prototype. The player can start or load a run, watch automatic gauge-driven combat, inspect and modify a build, cast one selected active skill manually, gain XP and passive points, collect/equip/dismantle/craft gear, use relic meta progression through Rebirth, die and restart the same combat level, and save to the Main Menu or quit.

The game has a coherent vertical slice rather than shipping-scale content. Combat, buildcraft, UI navigation, persistence, and reset semantics are operational. Balance, campaign scope, enemy variety, several advertised stats, achievements, and final presentation remain unfinished.

## 3. Runtime architecture

- **Lifecycle:** the cold Main Menu has no gameplay service set. The first gameplay scene creates persistent singleton authorities, which detach to roots and survive scene changes. Scene-owned actors, combat, zone, and UI are destroyed and rebound on each gameplay entry. See [RUNTIME_LIFECYCLE.md](RUNTIME_LIFECYCLE.md).
- **GameManager:** owns combat level, normal-kill count, boss position, death/restart flow, rewards, scene rebinding, New Game/load bootstrap, and immediate progression checkpoints.
- **BattleManager:** owns player/enemy gauges, global-turn ordering, attacks, active-skill resolution, status ticking, current enemy, deterministic encounter spawning, and encounter-start resource capture.
- **Player:** `PlayerController` builds basic and converted damage contexts from equipped weapon/stats. `HealthComponent`, `ManaComponent`, `StatsComponent`, `StatusController`, `PlayerSkillController`, and `PassiveKeystoneState` own their respective runtime domains.
- **Enemies:** `EnemyStatSetup` applies central combat-level intrinsic Life, flat Armour and ordinary Fire/Cold/Lightning/Void resistance from each prefab's authored level-1 seed. `EnemyAI` rolls independent rarity/equipment, asks `EnemyBuildOptimizer` to score against that scaled pre-gear baseline/outgoing factor, then applies the factor once to the completed pre-crit attack. Gear Life/Life%, attributes and other legitimate modifiers feed final `HealthComponent` maximum Life normally; boss role receives no hidden multiplier.
- **Combat/stats:** attacks use separate physical/elemental components, critical rolls, armour, elemental/ailment resistance, penetration, scoped damage, on-hit recovery, and DOT/status application. Stats are raw buckets with explicit percentage conversion rules.
- **Inventory/equipment:** `Inventory` owns unequipped run gear and pickup filtering; `EquipmentManager` owns eight equipped slots and projects item modifiers onto the current player. Stable gear IDs support persistence.
- **Crafting:** `CurrencyInventory` owns ordinary/Ancient stacks and transient armed intent. `EquipmentCrafting` and `AncientRelicCrafting` validate, mutate, consume, and checkpoint successful outcomes.
- **Progression:** `PlayerProgression` owns level, XP, passive points and 290 binary allocations. It rebuilds stats and keystone projections when allocations change.
- **Skills:** seven catalog skills can be selected one at a time and cast from the Skills panel when mana and a living encounter are available. Auto-attacks continue independently.
- **Statuses:** Poison ticks are mitigated as Void DOT while retaining Poison application and visual identity. Bleed/Ignite retain DOT behavior. Shock has five-stack Lightning triggers; Chill dynamically slows either actor's real gauge from actual Cold-hit strength.
- **Relics/Rebirth:** `RelicInventory` owns permanent-within-save relic history, four active slots and current-cycle crafting authority. `RebirthManager` performs the level-50 reset transaction and immediately saves it. New Game clears this entire layer.
- **Persistence:** `GamePersistence` owns schema-2 DTO capture/validation, stable run identity/seed, atomic files, backup recovery, V1 migration, deterministic encounter restore and debounced autosaves. See [SAVE_STATE_CONTRACT.md](SAVE_STATE_CONTRACT.md).
- **UI:** `PaperBattleHUD` coordinates gameplay panels and time controls. Most feature panels are runtime-built over the authored paper battle prefab.

Use [CODE_MAP.md](CODE_MAP.md) for file ownership and [DEVELOPER_HANDOFF.md](DEVELOPER_HANDOFF.md) for working entry points; this document deliberately does not repeat their line-by-line map.

## 4. Current core loop

1. **Main Menu:** New Game, Load Game, Options, and a placeholder Achievements button are visible. Load is disabled without a recoverable save. Existing progress causes New Game to show Cancel/overwrite confirmation.
2. **New or Load:** confirmed New Game creates and atomically commits a fresh run. Load validates primary, then backup, then eligible legacy V1 data, and reconstructs gameplay transactionally.
3. **Gameplay:** level 1 begins with a starter weapon. Separate player/enemy gauges produce automatic turns; the player may inspect panels, change the build, or cast the selected active skill.
4. **Encounter rewards:** an enemy death is claimed once, awards XP, independently rolls a 50% equipment drop and 10% chances for each ordinary currency, then advances and checkpoints.
5. **Boss cadence:** nine normal enemies are followed by the boss as encounter stage 10. Boss death advances the combat level and starts a new normal encounter.
6. **Presentation progression:** six forest images each cover ten combat levels and repeat after level 60; combat levels themselves continue.
7. **Death/Restart:** Death Menu shows killer details. Restart resets the current combat level to normal encounter 1 and full player resources while retaining progression, build, currencies, skill and relic meta.
8. **Rebirth:** at player level 50+, confirmation clears run inventory/equipment/currencies/progression, creates the next-cycle relic, grants one of each Ancient operation, returns to level 1 with starter gear, and saves. Existing relic history/slots remain.
9. **Save/exit:** Pause Menu Save & Main Menu and Save & Quit use the same canonical transactional checkpoint and do not leave gameplay when saving fails.

## 5. Implemented systems

- Gauge-driven automatic player/enemy attacks and manual active-skill casting
- Physical/elemental hit calculation, critical strikes, armour, resistances and penetration
- Poison-as-Void DOT, Bleed and Ignite stacking/ticking; first-class direct Void damage, resistance, penetration and max resistance
- Symmetric Hit Twice; player life/mana regeneration, on-hit and credited on-kill recovery; Fireball passive-only Projectile Amount, conversion and scoped Magic/Projectile damage inputs
- Enemy rarity, generated equipment candidates, and bounded build optimization
- Provisional enemy intrinsic Life/damage/Armour/elemental-Void resistance progression through combat level 100 and slower post-100 continuation, separate from unchanged generated gear
- Editor-only seeded balance lab with real generated-build/optimizer distributions, typed mitigation, synthetic-reference TTK/TTD and snapshot duels (not final gameplay balance)
- Exactly-once enemy death rewards, equipment drops and world currency pickups
- Eight-slot equipment, inventory grid, item tooltips, local/global weapon modifiers, stable item identity, pickup filters and auto-dismantling
- Six ordinary equipment-crafting operations and six corresponding Ancient relic-crafting operations
- Level/XP progression to 100, passive allocation/refund connectivity, 290-node passive tree and 20 keystones
- Seven levelable active skills, one selected skill, level-adjusted mana costs and independent Fireball projectiles
- Relic inventory, four active slots, current-cycle crafting and confirmed Rebirth
- Main Menu, HUD, gameplay panels, Pause/Options, Death Menu, restart, New Game confirmation and Load Game
- Schema-2 transactional persistence, backup recovery, V1 migration, autosaves and clean deterministic encounter restoration
- Layered paper player/enemy animation, swappable player weapon presentation, status/damage visuals, and repeating forest progression

## 6. Partially implemented or dead mechanics

| Mechanic | Current repository reality |
|---|---|
| Accuracy / evasion / block | Existing IDs remain loadable/displayable, but new v1 pools exclude these unapproved miss/avoidance families. No combat hit test/block mitigation exists. |
| Cooldown Recovery / Mana Cost affix | Existing IDs remain loadable, but neither newly generates; there is no cooldown loop, and skill mana cost is computed from skill level instead. |
| Deep Freeze maximum-Chill increase | Extra application remains projected; authored maximum-effect-increase field has a zero default pending a separate tuning decision. Current Chill cap remains 30%. |
| Minions | Minion damage scope/stat calculation exists, but there are no minion entities, commands or attacks. |
| Status callbacks | Virtual apply/tick/expire/outgoing-damage hooks exist on `StatusEffects` but current ticking does not dispatch them. |
| Integrated balance | Intrinsic enemy scaling now exists, but its profile and the lab's synthetic reference player are provisional; final TTK/TTD, enemy/gear/skill/passive/relic/XP/reward tuning belongs to Step 13. |
| Achievements | Main Menu button only logs a placeholder message. No achievement system exists. |

## 7. Current content

- **Active enemy:** Goblin normal prefab with authored idle/attack/hit presentation.
- **Active boss:** Hobgoblin boss prefab with authored idle/attack presentation.
- **Legacy inactive prefabs:** Ghoul2D and GhoulBoss2D remain in the repository but are not wired into the active PaperBattle slots.
- **Environment:** one forest thematic family represented by six corruption/progression images: 0%, 20%, 40%, 60%, 80%, 100%. Each lasts ten combat levels, then the sequence repeats.
- **Skills:** Heavy Strike, Ice Strike, Lightning Strike, Fireball, Envenom, Shiv and Immolate.
- **Passive tree:** 290 allocatable binary nodes—ten spokes, ten bridges, forty statless ring nodes, ten inner keystones and ten outer keystones. See [PASSIVE_TREE.md](PASSIVE_TREE.md).
- **Art pipeline:** the active player is the authored chibi body/weapon pipeline; active enemies use registered authored forest-enemy frames; backgrounds use installed paper forest PNGs. Deterministic preparation/install/verification scripts live under `Tools/Art`. Several older player and 3D-theme pipelines remain as archived/optional material.
- **Planned, not current content:** additional zones, enemies, bosses, final campaign length, bullet-hell boss play, fragments, minions and achievements are not implemented or locked here.

## 8. Current persistence

Schema 2 writes `current-save.json`, `current-save.json.bak`, and `current-save.json.tmp` under `Application.persistentDataPath`. It has one current-run slot, validated atomic writes, primary→backup recovery, nondestructive PlayerPrefs V1 migration, stable item/relic/passive/skill IDs, and a two-second mutation debounce.

New Game confirms replacement, clears all gameplay and relic/rebirth meta, preserves independent preferences, and makes primary/backup belong to the new run. Rebirth preserves relic history while resetting its established run state. Restart is an in-memory current-level reset, not disk load. Mid-combat load reconstructs the same deterministic encounter from its beginning and restores recorded encounter-start HP/mana rather than live-frame state. See [SAVE_STATE_CONTRACT.md](SAVE_STATE_CONTRACT.md) for the field and failure contract.

## 9. Current UI

- **Main Menu:** authored shell with functional New Game, Load Game and placeholder Achievements; Options and overwrite confirmation are functional runtime-built overlays.
- **HUD:** paper-styled top buttons for Skills, Passives, Enemy, Inventory, Stats, Pause and Play; run/level/encounter/resource summaries and status strips.
- **Inventory/equipment:** responsive grid, eight equipment slots, ordinary/Ancient currency and relic tabs, filter controls, modifier highlighting, crafting targeting and item tooltips.
- **Stats:** categorized player-stat panel with damage previews and equipment-derived refresh.
- **Enemy Inspection:** current enemy identity, rarity, level, stats/build and equipment inspection.
- **Skills:** seven skill entries, single-selection/forfeit behavior and Cast Active Skill control.
- **Passive Tree:** pan/zoom 7,000-pixel graph with allocation/refund, details, points and optional pause-while-open preference.
- **Pause/Options:** Resume, Options, Save & Main Menu, Save & Quit, and passive-tree pause preference.
- **Death Menu:** killer summary, restart current level and return-to-menu path.

These controls are functionally wired. Runtime-built modal/panel geometry and generic button styling are not final-art approval; final sprites, animated states, precision placement and bespoke transitions remain user-owned polish.

## 10. Verification infrastructure

- NUnit EditMode suite under `Assets/Tests/Editor` (fresh Steps 11 + 12 **184/184**; Step 10 historical baseline 172/172)
- `EnemyScalingTests` for authored level-1 seeds, canonical curves/safety/idempotence, Poison/Void one-pass output, generated-actor and optimizer parity; `BalanceSimulationTests` for deterministic seeds, live-singleton isolation, production-math parity and snapshot status/mitigation behavior
- `BalanceSimulationRunner` for ignored CSV/JSON/Markdown 25/250/1000-build sampled reports at representative levels; `BalanceSimulationWindow` for local configuration; see [ENEMY_SCALING_BASELINE.md](ENEMY_SCALING_BASELINE.md)
- `GamePersistenceTests` for schema, corruption, migration, backup, deterministic seed, transactional failure and debounce behavior
- `MenuLoadPlayChecks` for rich real-scene save/load, transient clearing, preferences and New Game replacement
- `RuntimeLifecyclePlayChecks` for six entries, five returns, singleton/rebinding and Pause Menu behavior
- `BaselineVerificationRunner.ValidateReferences` for scenes, prefabs, dependencies and missing references
- `BaselineVerificationRunner.RunSynchronousPlayChecks` for equipment, player stats, statuses and encounter progression
- `BaselineVerificationRunner.BuildWindows` for strict Windows x64 builds
- Focused Editor menu fixtures under `Assets/Editor` and file-only combat, cadence, forest and art verification tools under `Tools/Art`

Generated reports are written to `Logs` or `ReviewCaptures`; check timestamps before treating a report as current.

## 11. Known risks and technical debt

- Structural Step 11/12 coverage and representative seeded reports do not establish final combat balance, authored art/UI polish or every item permutation. The synthetic reference's higher-level duel losses are a Step 13 question, not an approved runtime player curve.
- The active content pool is one normal enemy, one boss and one repeating environment family.
- Much of the active UI is constructed in code; it is testable but harder to art-direct than final authored prefabs.
- `PaperBattle.prefab` retains substantial legacy 3D content and dormant systems, increasing import and maintenance cost.
- The file-only `verify_boss_cadence.ps1` harness predates the current scene-lifecycle dependencies and no longer compiles its reduced Unity stubs; Unity's `ProgressionChecks` remains the passing cadence regression.
- Save recovery exposes diagnostic status through logs/API rather than a polished player-facing recovery dialog.
- There is no automated cloud conflict policy, multiple-slot UI or achievement integration.

## 12. Roadmap position

Steps 1–12 have completed their requested source and fresh regression/build gates. Steps 11 + 12 add central placeholder enemy intrinsic scaling and a deterministic Editor-only measurement lab, documented in [ENEMY_SCALING_BASELINE.md](ENEMY_SCALING_BASELINE.md). They do not change the 120 stable stat IDs or 107 pooled definitions (3 guaranteed weapon bases plus 104 random affixes), and they do not establish final combat balance.

Known later phases are Step 13 integrated balance and Step 14 final v1 zone/enemy/boss/content scope. Step 13 should compare real player progression against the lab's deliberately synthetic reference before changing the provisional enemy profile.

## Maintenance rule

Update this file only from verified code, assets, tests and runtime evidence. Record what ships now, including defects and placeholders. Never promote an implementation accident into intended design; put intent in [GAME_DESIGN_CONTRACT.md](GAME_DESIGN_CONTRACT.md) and register conflicts there.
