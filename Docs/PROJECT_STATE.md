# Black-Cube project state

> This document answers: **What does the current repository actually do?** It records verified implementation reality, not intended design. Codex may update it after verified implementation milestones, but must not use it to invent design. Update the baseline commit, verification totals, and material behavior changes together.

For intended behavior, read [GAME_DESIGN_CONTRACT.md](GAME_DESIGN_CONTRACT.md). For the exhaustive current stat/affix inventory and Step 10 triage, read [STAT_AFFIX_AUDIT.md](STAT_AFFIX_AUDIT.md). For detailed ownership and persistence rules, read [RUNTIME_LIFECYCLE.md](RUNTIME_LIFECYCLE.md) and [SAVE_STATE_CONTRACT.md](SAVE_STATE_CONTRACT.md).

Current-reality claims use this evidence order: (1) verified source code, (2) scenes/prefabs/assets, (3) tests and verification tooling, (4) `RUNTIME_LIFECYCLE.md`, (5) `SAVE_STATE_CONTRACT.md`, then (6) `CODE_MAP.md` and `DEVELOPER_HANDOFF.md`. Lower-ranked summaries never override verified runtime evidence.

## 1. Verified baseline

- Repository branch: `codex/repository-cleanup-baseline`
- Step 13 implementation checkpoints: `ceff2ce1` (fragments/Deep Freeze), `a4982ba8` (loot/reference foundation), `ab0d0bcc` (integrated progression/combat validation), `1603cc4c` (reference gearing/skill baseline), and `34c5b3cb` (final Shiv outlier correction). No Step 13 commit has been pushed.
- Step 12.5G implementation checkpoint: `f6ffb5d2` (`Implement Step 12.5G itemization, Ancient art, and affix Codex`); fresh source-equivalent verification is recorded below. The documentation reconciliation checkpoint follows it locally, with no push.
- Step 12.5 checkpoint commits: `83b83ba6` (relic/Ancient UI foundation), `4dc36790` (independent damaging ailments), and `e566a936` (range/crit, lawful variable affixes, tooltips/passive/UI and schema migration). The final validation/balance-lab reconciliation checkpoint follows these milestones on this branch; do not infer final game balance from structural pass gates.
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

**Historical Step 12.5 evidence (pre-12.5G):** Unity EditMode 212/212 passed on 2026-09-15 (`Logs/Step12_5-final-editmode.xml`), including relic, independent ailment, range/crit, PoEDB/Black-Cube affix fallback, itemization validation, schema migration and pair rejection, passive and overlay cases. The seeded itemization audit passed 1,312 player/enemy/crafting cases with zero errors (`Logs/Step12_5-itemization-validation.txt`), and four synchronous real-scene combat/stat/status/progression checks passed after those edits (`Logs/BaselineSynchronousPlayChecks.txt`). Focused AttackStats, InventoryGrid, TooltipCrit and PlayerProgression scenes passed; the six-entry lifecycle/Pause soak, five-transition schema-3 menu/load check and reference scan passed; the balance smoke wrote 450 rows at seed 11012 across nine levels in 35.9 seconds. A strict Windows x64 build succeeded with zero errors and 522 existing warnings (`Logs/BaselineWindowsBuild.txt`); its standalone player reached Main Menu and remained alive/responsive for 48 seconds. These results are not fresh Step 12.5G verification.

**Fresh Step 12.5G evidence (2026-09-15):** the latest-source Unity EditMode suite passed **232/232** with no skips (`Logs/Step12_5G-editmode.xml`). The production itemization audit passed **1,312** natural-player/enemy/crafting cases with zero errors (`Logs/Step12_5G-itemization-validation.txt`); a separate 50,000-roll seeded rarity audit observed 709 Legendary rolls (1.418%, unchanged intended weight 1/71), all 709 legally constructed with no failures, and pickup filters disabled for that audit (`Logs/Step12_5G-legendary-drop.txt`). Reference validation found zero missing assets/scripts/references (`Logs/Step12_5G-reference-validation.txt`). Four synchronous real-scene equipment/stat/status/progression checks, the five-transition current-schema menu/load/New Game fixture, and six-entry/five-return lifecycle soak all passed (`Logs/Step12_5G-synchronous-play-checks.txt`, `Logs/Step12_5G-menu-load-play-checks.txt`, `Logs/Step12_5G-lifecycle-play-checks.txt`). A strict Windows x64 build succeeded with zero errors and 528 warnings (`Logs/Step12_5GWindowsBuild.txt`); its standalone player reached Main Menu and remained alive/responsive for more than 30 seconds, with only the existing unassigned Achievements-button warning (`Logs/Step12_5G-standalone-smoke.log`). Because the original checkout was open in Unity, fresh Unity tests/build ran against a disposable byte-for-byte tracked-file snapshot plus the exact Step 12.5G new assets/scripts; the 205 output build files were copied to `Builds/Step12_5GWindows` and SHA-256 matched. This is source-equivalent verification, not a claim that the locked original Editor performed the batch run.

**Fresh Step 13 evidence (2026-09-16):** final-source EditMode passed **248/248** with no failures/skips (`Logs/Step13-final-correction-editmode.xml`); itemization passed **1,312/1,312** cases; reference validation, four synchronous checks, the six-entry lifecycle/Pause soak and Main Menu/New Game/Load fixture passed. Final production-reference simulation completed five tuning seeds (39,600 rows) and three untouched holdouts (23,760 rows); a separate 1,500-build high-level enemy audit recorded optimizer diversity. The strict Windows x64 build succeeded with zero errors and 523 warnings at `Builds/Step13Windows/BlackCube.exe`; the exact player reached Main Menu and remained responsive through a 30-second headless smoke. Detailed targets, deviations and playtest risks are in [CORE_BALANCE_BASELINE.md](CORE_BALANCE_BASELINE.md).

**Fresh Step 15 evidence (2026-09-18):** focused world/encounter tests passed **16/16** and the complete EditMode suite passed **302/302** with no failures/skips. World-content validation covered all 360 levels, six corruption tiers, ranges, IDs, enemy pools and stage-10 bosses with zero errors. Reference validation passed with zero failures. A real-scene cold Menu → New Game → level 1/61 mapping → save → Menu → Load fixture restored the same derived level-61 world position. The strict Windows x64 build succeeded with zero errors and 523 warnings at `Builds/Step15Windows/BlackCube.exe`; its hidden standalone player remained alive and responsive through the requested ten-second startup smoke. No balance simulations were run.

Known baseline limits include one logical save slot; clean encounter-boundary rather than exact-frame restoration; functional/basic runtime-built menu overlays; only one production normal enemy and boss; six forest corruption images reused by placeholder world definitions; and the incomplete mechanics listed below.

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
- **Skills:** seven catalog skills can be selected one at a time and queued from the Skills panel to replace the next scheduled basic attack.
- **World/encounters:** `WorldProgression` derives the six-biome, ten-location, six-corruption position and stage encounter from combat level. `WorldContentDatabase` supplies stable-ID locations, weighted normal pools, bosses and future challenge records; current non-forest slots are placeholder definitions.
- **Statuses:** Poison ticks are mitigated as Void DOT while retaining Poison application and visual identity. Bleed/Ignite retain DOT behavior. Shock has five-stack Lightning triggers; Chill dynamically slows either actor's real gauge from actual Cold-hit strength.
- **Relics/Rebirth:** `RelicInventory` owns permanent-within-save relic history, four active slots and current-cycle crafting authority. `RebirthManager` performs the combat-zone-60 reset transaction, provisions the relic-modified starter, and immediately saves it. New Game clears this entire layer.
  - **Persistence:** `GamePersistence` owns schema-8 DTO capture/validation, stable run/class/weapon identity, atomic files, backup recovery, sequential V1/V2/V3/V4/V5/V6/V7 migration, historical value preservation, subclass state, deterministic encounter restore and debounced autosaves. See [SAVE_STATE_CONTRACT.md](SAVE_STATE_CONTRACT.md).
- **UI:** `PaperBattleHUD` coordinates gameplay panels and time controls. Most feature panels are runtime-built over the authored paper battle prefab.

Use [CODE_MAP.md](CODE_MAP.md) for file ownership and [DEVELOPER_HANDOFF.md](DEVELOPER_HANDOFF.md) for working entry points; this document deliberately does not repeat their line-by-line map.

## 4. Current core loop

1. **Main Menu:** New Game, Load Game, Options, and a placeholder Achievements button are visible. Load is disabled without a recoverable save. Existing progress causes New Game to show Cancel/overwrite confirmation.
2. **New or Load:** confirmed New Game creates and atomically commits a fresh run. Load validates primary, then backup, then eligible legacy V1 data, and reconstructs gameplay transactionally.
3. **Gameplay:** level 1 begins with the authored 64–96 starter weapon, now carrying one legal permanent implicit without randomized overrides to its base range, speed or crit. Separate player/enemy gauges produce automatic turns; the player may inspect panels, change the build, or cast the selected active skill.
4. **Encounter rewards:** an enemy death is claimed once, awards XP, independently rolls a 50% equipment drop and 10% chances for each ordinary currency, then advances and checkpoints.
5. **Boss cadence:** nine normal enemies are followed by the boss as encounter stage 10. Boss death advances the combat level and starts a new normal encounter.
6. **World progression:** each biome spans six corruption bands of ten locations. The current reference catalog reuses the matching six forest images, Goblin, and Hobgoblin until production definitions replace placeholders.
7. **Death/Restart:** Death Menu shows killer details. Restart resets the current combat level to normal encounter 1 and full player resources while retaining progression, build, currencies, skill and relic meta.
8. **Rebirth:** at combat zone 60+, confirmation clears run inventory/equipment/currencies/progression, creates the next-cycle levelled relic, grants one of each Ancient operation, returns to player level 1 with exactly one relic-modified starter, and saves. Existing relic history/slots remain.
9. **Save/exit:** Pause Menu Save & Main Menu and Save & Quit use the same canonical transactional checkpoint and do not leave gameplay when saving fails.

## 5. Implemented systems

- Gauge-driven automatic player/enemy attacks and manual active-skill casting
- Step 12.5 weapon min–max damage and an independent range roll per actual hit; starter weapon 64–96 (average 80); base critical damage 1.5× plus classified CritMult, final-damage/critical popups (with a compact attached star marker for crits) and range previews
- Physical/elemental hit calculation, critical strikes, armour, resistances and penetration
- Poison-as-Void DOT, Bleed and Ignite stacking/ticking; first-class direct Void damage, resistance, penetration and max resistance
- Independent Poison (uncapped, four ticks over eight global turns), Bleed (five-stack strongest-selection, five ticks over ten afflicted-actor turns) and Ignite (one strongest stack, two ticks over four afflicted-actor turns), each based on post-outgoing/pre-target-mitigation hit snapshots
- Symmetric Hit Twice; player life/mana regeneration, on-hit and credited on-kill recovery; Fireball passive-only Projectile Amount, conversion and scoped Magic/Projectile damage inputs
- Enemy rarity, generated equipment candidates, and bounded build optimization
- Provisional enemy intrinsic Life/damage/Armour/elemental-Void resistance progression through combat level 100 and slower post-100 continuation, separate from unchanged generated gear
- Editor-only seeded balance lab with real generated-build/optimizer distributions, typed mitigation, synthetic-reference TTK/TTD and snapshot duels (not final gameplay balance)
- Exactly-once enemy death rewards, equipment drops and world currency pickups
- Eight-slot equipment, inventory grid, item tooltips, local/global weapon modifiers, stable item identity, pickup filters and auto-dismantling
- One permanent equipment implicit from any legal item-type family, plus full 0/2/4/6 natural explicit sets (Magic 1/1, Rare 2/2, Legendary 3/3); variable authored tiers and explicit legality shared by loot/crafting, with the enemy roll path retained separately. Direct ordinary PoE analogue rows/slot deviations are documented in [POEDB_AFFIX_BASELINE.md](POEDB_AFFIX_BASELINE.md)
- Functional relic cards/inspection and six distinct supplied Ancient crafting sprites/cursors; manual Gear/Relics tab switches cancel armed intent. Existing relic mechanics are retained, so the supplied gold reroll art is the closest available visual assignment for the existing Ancient Rare→Legendary upgrade.
- Six ordinary equipment-crafting operations and six existing Ancient relic-crafting operations

Current Ancient currency-to-art assignments (visual only; relic semantics unchanged):

| Existing Ancient relic currency | Supplied sprite |
|---|---|
| Normal → Magic | `AncientWhiteToBlue.png` |
| Magic → Rare | `AncientBlueToYellow.png` |
| Rare → Legendary | `AncientYellowReroll.png` (closest gold art; icon name implies reroll) |
| Shared relic reroll | `AncientBlueReroll.png` |
| Add modifier | `AncientAddYellow.png` |
| Remove modifier | `AncientRemove.png` |

- Level/XP progression to 100, passive allocation/refund connectivity, 290-node passive tree and 20 keystones
- Seven levelable active skills, one selected skill, level-adjusted mana costs and independent Fireball projectiles
- Relic inventory, four active slots, current-cycle crafting and confirmed Rebirth
- Main Menu, HUD, gameplay panels, Pause/Options/CODEX → MOD LIST, Death Menu, restart, New Game confirmation and Load Game
- Schema-4 transactional persistence, backup recovery, V1/V2/V3 migration with exact historical rolls/paired endpoints retained, legacy-affix preservation, autosaves and clean deterministic encounter restoration
- Layered paper player/enemy animation, swappable player weapon presentation, status/damage visuals, and repeating forest progression

## 6. Partially implemented or dead mechanics

| Mechanic | Current repository reality |
|---|---|
| Accuracy / evasion / block | Existing IDs remain loadable/displayable, but new v1 pools exclude these unapproved miss/avoidance families. No combat hit test/block mitigation exists. |
| Cooldown Recovery / Mana Cost affix | Existing IDs remain loadable, but neither newly generates; there is no cooldown loop, and skill mana cost is computed from skill level instead. |
| Deep Freeze maximum-Chill increase | Implemented: +10 percentage points to the normal 30% maximum Chill slow, capped at 40%, while retaining the keystone's 25% less-effect downside. |
| Minions | Minion damage scope/stat calculation exists, but there are no minion entities, commands or attacks. |
| Status callbacks | Virtual apply/tick/expire/outgoing-damage hooks exist on `StatusEffects` but current ticking does not dispatch them. |
| Integrated balance | Step 13 provides the first production-gear/player-progression baseline, source-locked tuning/holdouts and explicit residual risks. It is a credible playtest baseline, not final launch balance; see `CORE_BALANCE_BASELINE.md`. |
| Achievements | Main Menu button only logs a placeholder message. No achievement system exists. |

## 7. Current content

- **Active enemy:** Goblin normal prefab with authored idle/attack/hit presentation.
- **Active boss:** Hobgoblin boss prefab with authored idle/attack presentation.
- **World definitions:** six biome definitions, sixty base-location definitions, six explicit corruption tiers, six encounter tables, and complete 1–360 mappings. Only the Forest/Goblin/Hobgoblin references are production examples; the rest are marked placeholders.
- **Legacy inactive prefabs:** Ghoul2D and GhoulBoss2D remain in the repository but are not wired into the active PaperBattle slots.
- **Environment:** one forest thematic family represented by six corruption/progression images: 0%, 20%, 40%, 60%, 80%, 100%. Each lasts ten combat levels, then the sequence repeats.
- **Skills:** Heavy Strike, Ice Strike, Lightning Strike, Fireball, Envenom, Shiv and Immolate.
- **Passive tree:** 290 allocatable binary nodes—ten spokes, ten bridges, forty statless ring nodes, ten inner keystones and ten outer keystones. See [PASSIVE_TREE.md](PASSIVE_TREE.md).
- **Art pipeline:** the active player is the authored chibi body/weapon pipeline; active enemies use registered authored forest-enemy frames; backgrounds use installed paper forest PNGs. Deterministic preparation/install/verification scripts live under `Tools/Art`. Several older player and 3D-theme pipelines remain as archived/optional material.
- **Planned, not current content:** additional zones, enemies, bosses, final campaign length, bullet-hell boss play, minions and achievements are not implemented or locked here. Dismantle fragments are current Step 13 functionality.

## 8. Current persistence

Schema 8 writes `current-save.json`, `current-save.json.bak`, and `current-save.json.tmp` under `Application.persistentDataPath`. It adds stable base-class, subclass milestone/selection, and weapon-type identity to the established atomic one-slot/backup model and sequential schema-2/3/4/5/6/7 migration.

New Game confirms replacement, clears all gameplay and relic/rebirth meta, preserves independent preferences, and makes primary/backup belong to the new run. Rebirth preserves relic history while resetting its established run state. Restart is an in-memory current-level reset, not disk load. Mid-combat load reconstructs the same deterministic encounter from its beginning and restores recorded encounter-start HP/mana rather than live-frame state. See [SAVE_STATE_CONTRACT.md](SAVE_STATE_CONTRACT.md) for the field and failure contract.

## 9. Current UI

- **Main Menu:** authored shell with functional New Game, Load Game and placeholder Achievements; Options and overwrite confirmation are functional runtime-built overlays.
- **HUD:** paper-styled top buttons for Skills, Passives, Enemy, Inventory, Stats, Pause and Play; run/level/encounter/resource summaries and status strips.
- **Inventory/equipment:** responsive grid, eight equipment slots, ordinary/canonical Ancient currency art and manual Gear/Relics tabs, relic inspection, filter controls, modifier highlighting, crafting targeting, implicit/Prefix/Suffix exact-tier tooltips, and a paused CODEX → MOD LIST browser bound to live affix/tier data.
- **Stats:** categorized player-stat panel with damage-range/average previews and equipment-derived refresh; Inventory may remain open alongside Stats or Enemy Inspection, but those two inspection panels are mutually exclusive.
- **Enemy Inspection:** current enemy identity, rarity, level, stats/build and equipment inspection.
- **Skills:** seven skill entries, single-selection/forfeit behavior and Cast Active Skill control.
- **Passive Tree:** pan/zoom 7,000-pixel graph with allocation/refund, details, points and optional pause-while-open preference.
- **Pause/Options/CODEX:** Resume, Options, paused CODEX → MOD LIST with item-type/side selectors and a scrollable tier catalog, Save & Main Menu, Save & Quit, and passive-tree pause preference.
- **Death Menu:** killer summary, restart current level and return-to-menu path.

These controls are functionally wired. Runtime-built modal/panel geometry and generic button styling are not final-art approval; final sprites, animated states, precision placement and bespoke transitions remain user-owned polish.

## 10. Verification infrastructure

- NUnit EditMode suite under `Assets/Tests/Editor` (fresh Step 13 **248/248**; Step 12.5G historical baseline 232/232; Step 12.5 historical baseline 212/212)
- `EnemyScalingTests` for authored level-1 seeds, canonical curves/safety/idempotence, Poison/Void one-pass output, generated-actor and optimizer parity; `BalanceSimulationTests` for deterministic seeds, live-singleton isolation, production-math parity and snapshot status/mitigation behavior
- `BalanceSimulationRunner` for ignored CSV/JSON/Markdown 25/250/1000-build sampled reports at representative levels; `BalanceSimulationWindow` for local configuration; see [ENEMY_SCALING_BASELINE.md](ENEMY_SCALING_BASELINE.md)
- `CoreBalanceReferenceRunner` for deterministic legal production-player P50/P75/P90, relic, skill, resistance, checkpoint and post-100 reports; see [CORE_BALANCE_BASELINE.md](CORE_BALANCE_BASELINE.md)
- `GamePersistenceTests` for schema, corruption, migration, backup, deterministic seed, transactional failure and debounce behavior
- `MenuLoadPlayChecks` for rich real-scene save/load, transient clearing, preferences and New Game replacement
- `RuntimeLifecyclePlayChecks` for six entries, five returns, singleton/rebinding and Pause Menu behavior
- `BaselineVerificationRunner.ValidateReferences` for scenes, prefabs, dependencies and missing references
- `BaselineVerificationRunner.RunSynchronousPlayChecks` for equipment, player stats, statuses and encounter progression
- `BaselineVerificationRunner.BuildWindows` for strict Windows x64 builds
- Focused Editor menu fixtures under `Assets/Editor` and file-only combat, cadence, forest and art verification tools under `Tools/Art`

Generated reports are written to `Logs` or `ReviewCaptures`; check timestamps before treating a report as current.

## 11. Known risks and technical debt

- Step 13 is a first integrated baseline, not final combat balance. Boss victories are shorter than requested at several levels, level-100 boss loss rate is high, Heavy/physical gearing remains an offensive outlier, several skills lag late, P90 all-four resistance caps are attainable but not the majority, and selected-combat-relic acceleration/useful-upgrade frequency still require human play.
- The active content pool is one normal enemy, one boss and one repeating environment family.
- Much of the active UI is constructed in code; it is testable but harder to art-direct than final authored prefabs.
- `PaperBattle.prefab` retains substantial legacy 3D content and dormant systems, increasing import and maintenance cost.
- The file-only `verify_boss_cadence.ps1` harness predates the current scene-lifecycle dependencies and no longer compiles its reduced Unity stubs; Unity's `ProgressionChecks` remains the passing cadence regression.
- Save recovery exposes diagnostic status through logs/API rather than a polished player-facing recovery dialog.
- There is no automated cloud conflict policy, multiple-slot UI or achievement integration.
- The initial sandboxed 2026-09-15 Step 12.5 batchmode attempts could not reach the local Unity Licensing Client IPC and left older reports untouched. Running the Unity test/build process with broader host-process permissions connected to the existing Personal license; the final reports above are fresh reruns. No Unity Hub login repair or project-code workaround was required.

## 12. Roadmap position

Steps 1–13 completed their requested gates. Step 14 adds the v1 scope ledger, starter/Rebirth/relic progression, tier quality weighting, and schema 6. Step 14.5 adds queued skill replacement and the schema-7 finite-crafting/Empowerment foundation. Step 15 locks and implements the data-driven six-biome × ten-location × six-corruption mapping while retaining placeholder content. See [V1_CONTENT_CONTRACT.md](V1_CONTENT_CONTRACT.md) and [WORLD_CONTENT_ARCHITECTURE.md](WORLD_CONTENT_ARCHITECTURE.md).

### Step 14 progression delta

- Baseline starter: Physical level 1, 18–27 damage, 0.45 attacks/second, 5% base critical and deterministic minimum T5 Increased Damage implicit. It is about 7.4% below the weakest level-one natural base-power reference after its implicit.
- New Game and Rebirth call one idempotent `EnsureStarterWeapon` path; load restores the persisted weapon and creates none.
- Rebirth eligibility uses combat zone 60, not player level. Generated relic level stores the zone-derived 1–100 result; legacy values remain exact at level 1/tier marker 0.
- Relic inventory cards own their full raycast/click surface. Normal clicks equip/unequip without replacement; Ancient-armed clicks exclusively target crafting.
- Equipment item level remains capped at 100. Late-world gear progression is deliberately unresolved.

## Maintenance rule

Update this file only from verified code, assets, tests and runtime evidence. Record what ships now, including defects and placeholders. Never promote an implementation accident into intended design; put intent in [GAME_DESIGN_CONTRACT.md](GAME_DESIGN_CONTRACT.md) and register conflicts there.
## Step 14.5 current state

The active skill button now queues a single replacement for the next scheduled basic attack. The gauge continues untouched, Mana is paid at resolution, a target is chosen at resolution, insufficient resolution Mana falls back to a basic attack, and the queue is excluded from saves.

Equipment now owns origin rarity and finite Crafting Potential (natural N/M/R/L: 6/8/10/14). Ordinary crafting spends centralized success-only costs, upgrades do not increase the origin maximum, and tooltips expose current/max Potential. Schema 7 persists and validates these fields while schema-6 migration preserves old rolls and grants full current-rarity Potential.

The post-100 foundation keeps item level capped at 100. Empowerment unlocks per-item caps at 120/160/210/260/310/360, converts only eligible T1 numeric explicits to authored or 1.25× ranges, and locks them against ordinary reroll/remove. Empowerment Catalyst exists with placeholder presentation and persistence but no production source. Stable boss-special pool/replacement data and a 3-Potential Legendary service exist; no bosses or production pools were added. Final skill balance, challenge content, rare implicit manipulation, and inventory-overload UX remain later work.

## Step 15 current state

`WorldContentArchitecture` and `ProductionWorldContent` supply the V1 production mechanical catalog: six biomes, 60 locations, 48 non-boss archetypes, 60 main bosses, six challenge bosses, reusable skills/loadouts, phases, corruption/location profiles, deterministic weighted resolution, and the post-360 fallback. `BattleManager` configures the spawned paper actor with resolved production identity and mechanics. Challenge content is structurally separate and has functional entry/reward services, but its final launcher/UI and final presentation art remain outstanding. See `V1_WORLD_CONTENT.md`.

The reference catalog resolves all levels 1–360 and stages 1–10, but deliberately reuses the six forest corruption sprites, `enemy.goblin`, and `boss.hobgoblin` across placeholder locations.

## Step 16 current state

Six stable base classes, six stable weapon types, New Game class selection, signature starter routing, unrestricted cross-class weapon equipping, schema-8 class/weapon/subclass persistence, a two-slot weapon-skill queue, subclass/story-milestone seams, optional weapon-type affix restrictions, and passive extension metadata are implemented. Production skill mappings and subclass identities remain deliberately empty. See [CLASS_WEAPON_ARCHITECTURE.md](CLASS_WEAPON_ARCHITECTURE.md).

## Step 17 current state

Passive Tree V2 ships with 366 nodes, six class starts, six bridge regions, a shared center, explicit generated coordinates, stable `tree.v2.*` IDs, free graph-safe respecs, and a confirmed Refund All action. Level 1 owns one passive point and level 100 owns exactly 100. Weapon effects use a centralized 1.60 specialization premium and activate only for the matching equipped type. See [PASSIVE_TREE_V2.md](PASSIVE_TREE_V2.md).

Cast Speed/Staff AutoCooldown, Bow Precision and projectile latency/barrages, Axe Rage and Rage Finisher, and local Average Weapon DPS are production systems. Final Staff skills and all other final weapon-skill assignments remain unapproved. See [WEAPON_MECHANICS.md](WEAPON_MECHANICS.md).

Persistence is schema 9 with six independent character slots, per-slot primary/backup/temp files, summaries and explicit selected-slot load/save. Legacy `current-save.json` migrates non-destructively to Slot 1 once. Schema 8 clears V1 passive allocations and grants the character's full level-owned V2 point total. Global preferences remain outside character files.
# Step 18 production gameplay systems

Step 18 promotes the weapon-skill and subclass foundations to production data: 12 stable weapon skills, 12 class-specific/weapon-agnostic subclasses, Cooldown Reduction, Freeze/Shatter, modular combat-event tags, Light Priest auras, and connected passive transformations. Save schema is 10. Passive Tree V2 remains 366 nodes. See `PRODUCTION_SKILLS.md` and `SUBCLASSES.md`. Values are first-pass placeholders, not final balance.

## Step 18.5 current state

The bottom action row is owned by one responsive horizontal layout with autosizing/ellipsis and an overlap validator covering 1280×720 through 3440×1440. Ailment eligibility is centralized and defaults to Physical Bleed, Fire Ignite, Lightning Shock, Cold Chill, and Physical/Void Poison with explicit modular overrides. Enemy loot guarantees gear and scales bounded extra gear/currency budgets from level, enemy rarity, and normalized actual equipped-build score. Natural weapons select all six types uniformly, use the locked level-one profiles, and have temporary mapped icons. Save schema remains 10. See `AILMENT_ELIGIBILITY.md` and `LOOT_SYSTEM.md`.

## Step 18.6 current state

Reward generation no longer uses the deterministic encounter seed. Each claimed enemy death owns a fresh cryptographic loot stream while deterministic world/enemy-equipment generation remains unchanged. Weapon-type attribute scaling and capped player-level growth now join the root hit's additive increased bucket exactly once. Save schema remains 10. See `PLAYER_DAMAGE_SCALING.md`.

Fresh Step 18.6 evidence: focused EditMode 31/31 and complete EditMode 370/370 passed; itemization, passive-tree, world-content, and reference validation passed; repeated-stage loot, LootPower, six-weapon attribute, level-damage, ailment, Dagger Quick Strike, Bow travel, Staff auto-cast, and Axe Rage real-scene checks passed. The Windows x64 build succeeded with zero errors at `Builds/Step18_6Windows/BlackCube.exe`, and the hidden standalone player remained alive and responsive through the ten-second startup smoke. BalanceLab was not run.

## Step 20 current state

Schema 11 owns persistent per-character challenge keys/Essences, Catalysts, Reforgers, and final-challenge first-clear state. The production HUD now exposes a six-entry Challenge launcher and one three-tab endgame crafting interface. Six APEX pools contain 36 authored boss-special affixes. Boss infusion, Empowerment, and implicit-only reforge are production-accessible and entropy-backed. See `ENDGAME_ITEMIZATION.md`, `BOSS_SPECIAL_AFFIXES.md`, and `EMPOWERMENT.md`.

Fresh Step 20 evidence: focused EditMode 19/19 and complete EditMode 396/396 passed; world-content, itemization, passive-tree, reference, and endgame-resource/special-affix validators passed; the real-scene launcher → key spend → challenge → rewards → all three crafts → schema-11 save/load smoke passed. Fresh regression smokes also passed repeated-stage loot, all six weapons, Staff/Bow/Axe/Dagger contracts, ailment eligibility, class/load, and Rebirth. The Windows x64 build succeeded with zero errors at `Builds/Step20Windows/BlackCube.exe`, and the hidden standalone player remained alive for the ten-second startup smoke. BalanceLab was not run.
