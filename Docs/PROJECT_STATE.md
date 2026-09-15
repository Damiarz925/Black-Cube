# Black-Cube project state

> This document answers: **What does the current repository actually do?** It records verified implementation reality, not intended design. Codex may update it after verified implementation milestones, but must not use it to invent design. Update the baseline commit, verification totals, and material behavior changes together.

For intended behavior, read [GAME_DESIGN_CONTRACT.md](GAME_DESIGN_CONTRACT.md). For detailed ownership and persistence rules, read [RUNTIME_LIFECYCLE.md](RUNTIME_LIFECYCLE.md) and [SAVE_STATE_CONTRACT.md](SAVE_STATE_CONTRACT.md).

Current-reality claims use this evidence order: (1) verified source code, (2) scenes/prefabs/assets, (3) tests and verification tooling, (4) `RUNTIME_LIFECYCLE.md`, (5) `SAVE_STATE_CONTRACT.md`, then (6) `CODE_MAP.md` and `DEVELOPER_HANDOFF.md`. Lower-ranked summaries never override verified runtime evidence.

## 1. Verified baseline

- Repository branch: `codex/repository-cleanup-baseline`
- Verified implementation commit: `76c5687f8fcee69d089eb9b3efc0c9df5549ea6a` (`Implement versioned save and load system`)
- Unity: `6000.6.0f1` (`f7f8ed4d1e24`)
- Enabled build scenes, in order: `Assets/Scenes/Main Menu.unity`, `Assets/Scenes/SampleScene.unity`
- Source inventory at this milestone: 94 runtime C# files, 23 Editor C# files, 11 EditMode test files, and 45 sources under `Tools` (`.cs`, `.py`, `.ps1`)
- EditMode suite: 136/136 passing
- Real-scene rich save → Main Menu → load/New Game check: passing
- Six-entry lifecycle and Pause Menu soak: passing
- Missing-reference validation: passing with zero failures
- Windows x64 build: succeeding with zero errors; final standalone smoke launch remained alive through its observation window

Known baseline limits include one logical save slot; clean encounter-boundary rather than exact-frame restoration; functional/basic runtime-built menu overlays; two active enemy archetypes; six repeating forest backgrounds; no final campaign/content count; and the incomplete mechanics listed below.

## 2. Current product state

Black-Cube is presently a playable single-player paper-styled auto-battle prototype. The player can start or load a run, watch automatic gauge-driven combat, inspect and modify a build, cast one selected active skill manually, gain XP and passive points, collect/equip/dismantle/craft gear, use relic meta progression through Rebirth, die and restart the same combat level, and save to the Main Menu or quit.

The game has a coherent vertical slice rather than shipping-scale content. Combat, buildcraft, UI navigation, persistence, and reset semantics are operational. Balance, campaign scope, enemy variety, several advertised stats, achievements, and final presentation remain unfinished.

## 3. Runtime architecture

- **Lifecycle:** the cold Main Menu has no gameplay service set. The first gameplay scene creates persistent singleton authorities, which detach to roots and survive scene changes. Scene-owned actors, combat, zone, and UI are destroyed and rebound on each gameplay entry. See [RUNTIME_LIFECYCLE.md](RUNTIME_LIFECYCLE.md).
- **GameManager:** owns combat level, normal-kill count, boss position, death/restart flow, rewards, scene rebinding, New Game/load bootstrap, and immediate progression checkpoints.
- **BattleManager:** owns player/enemy gauges, global-turn ordering, attacks, active-skill resolution, status ticking, current enemy, deterministic encounter spawning, and encounter-start resource capture.
- **Player:** `PlayerController` builds basic and converted damage contexts from equipped weapon/stats. `HealthComponent`, `ManaComponent`, `StatsComponent`, `StatusController`, `PlayerSkillController`, and `PassiveKeystoneState` own their respective runtime domains.
- **Enemies:** `EnemyAI` rolls rarity and generated equipment, asks `EnemyBuildOptimizer` to choose a bounded build, then attacks automatically. `EnemyStatSetup` initializes stat buckets but does not scale base stats by level; maximum life comes from the enemy prefab's `HealthComponent`.
- **Combat/stats:** attacks use separate physical/elemental components, critical rolls, armour, elemental/ailment resistance, penetration, scoped damage, on-hit recovery, and DOT/status application. Stats are raw buckets with explicit percentage conversion rules.
- **Inventory/equipment:** `Inventory` owns unequipped run gear and pickup filtering; `EquipmentManager` owns eight equipped slots and projects item modifiers onto the current player. Stable gear IDs support persistence.
- **Crafting:** `CurrencyInventory` owns ordinary/Ancient stacks and transient armed intent. `EquipmentCrafting` and `AncientRelicCrafting` validate, mutate, consume, and checkpoint successful outcomes.
- **Progression:** `PlayerProgression` owns level, XP, passive points and 290 binary allocations. It rebuilds stats and keystone projections when allocations change.
- **Skills:** seven catalog skills can be selected one at a time and cast from the Skills panel when mana and a living encounter are available. Auto-attacks continue independently.
- **Statuses:** Poison, Bleed, and Ignite deal turn-based damage with stack policies, duration/tick rate, resistance and penetration. Shock and Chill have application/lifetime/HUD infrastructure but incomplete effects.
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
- Poison, Bleed and Ignite damage-over-time stacking/ticking
- Player Hit Twice, life/mana regeneration, life/mana on hit, damage conversion, scoped Magic/Projectile/Minion damage inputs, and implemented passive keystone projections
- Enemy rarity, generated equipment candidates, and bounded build optimization
- Exactly-once enemy death rewards, equipment drops and world currency pickups
- Eight-slot equipment, inventory grid, item tooltips, local/global weapon modifiers, stable item identity, pickup filters and auto-dismantling
- Six ordinary equipment-crafting operations and six corresponding Ancient relic-crafting operations
- Level/XP progression to 100, passive allocation/refund connectivity, 290-node passive tree and 20 keystones
- Seven active skills, one selected skill, mana costs and projectile presentation for Fireball
- Relic inventory, four active slots, current-cycle crafting and confirmed Rebirth
- Main Menu, HUD, gameplay panels, Pause/Options, Death Menu, restart, New Game confirmation and Load Game
- Schema-2 transactional persistence, backup recovery, V1 migration, autosaves and clean deterministic encounter restoration
- Layered paper player/enemy animation, swappable player weapon presentation, status/damage visuals, and repeating forest progression

## 6. Partially implemented or dead mechanics

| Mechanic | Current repository reality |
|---|---|
| Accuracy / evasion | Stats, affix pools and display mappings exist. Combat performs no accuracy/evasion hit test; attacks do not miss. |
| Block | `ChanceToBlock` can exist and display, but damage resolution has no block roll or block mitigation. |
| Maximum resistance | Max-resistance stats exist in data/UI, but resistance code uses a fixed ±90% reduction clamp and never reads them. |
| Chill | Cold hits can create Chill status records and UI badges. `ApplyChill` is empty; target attack speed is unchanged. |
| Shock | Shock statuses can be applied/tracked, but they do not accumulate into a general threshold-triggered Lightning effect. Lightning Strike separately converts Shock Chance applications directly into extra hits. |
| Enemy Hit Twice | The player path consumes `ChanceToHitTwice`; the enemy attack path does not. Enemy builds can therefore receive no combat value from it. |
| Projectile Amount | Passives and Bullet Hell expose projectile-count values and damage tradeoffs, but `SkillProjectile` launches one projectile and does not consume the count. |
| Cooldown Recovery | Rollable/displayable percentage stat; there is no active-skill cooldown state or timer. |
| Attributes/scaling | Strength, Dexterity, Intelligence and their scaling affixes exist in enums/pools/UI but are not projected into combat/resources. |
| Skill-level modifiers | `Plus1*` affixes roll and display but do not change skill behavior or levels. |
| Kill-resource modifiers | Life/Mana on Kill exist in item data/UI but enemy reward resolution does not apply them. On-hit recovery does work. |
| Minions | Minion damage scope/stat calculation exists, but there are no minion entities, commands or attacks. |
| Void | `Element.Void` and masks exist. Player loot excludes unfinished Void weapon bases and no final Void mechanic is implemented. |
| Status callbacks | Virtual apply/tick/expire/outgoing-damage hooks exist on `StatusEffects` but current ticking does not dispatch them. |
| Enemy base scaling | Enemy item level, item count and build candidates grow with combat level. Base life/damage scaling remains an unused extension point; prefab life is constant per archetype. |
| Prestige | Zone level 10+ reaches an obsolete placeholder that logs and auto-continues. Reward methods are empty; Rebirth is the functional reset system. |
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

- NUnit EditMode suite under `Assets/Tests/Editor` (136 tests at this baseline)
- `GamePersistenceTests` for schema, corruption, migration, backup, deterministic seed, transactional failure and debounce behavior
- `MenuLoadPlayChecks` for rich real-scene save/load, transient clearing, preferences and New Game replacement
- `RuntimeLifecyclePlayChecks` for six entries, five returns, singleton/rebinding and Pause Menu behavior
- `BaselineVerificationRunner.ValidateReferences` for scenes, prefabs, dependencies and missing references
- `BaselineVerificationRunner.RunSynchronousPlayChecks` for equipment, player stats, statuses and encounter progression
- `BaselineVerificationRunner.BuildWindows` for strict Windows x64 builds
- Focused Editor menu fixtures under `Assets/Editor` and file-only combat, cadence, forest and art verification tools under `Tools/Art`

Generated reports are written to `Logs` or `ReviewCaptures`; check timestamps before treating a report as current.

## 11. Known risks and technical debt

- Rollable but unconsumed affixes create dead player/enemy item outcomes and misleading displayed power.
- Shock and Chill presentation can imply mechanics that are not actually applied.
- Enemy base survivability/damage does not scale with level independently of generated equipment.
- Obsolete Prestige code remains in the post-boss route, although it currently only auto-continues.
- The active content pool is one normal enemy, one boss and one repeating environment family.
- Much of the active UI is constructed in code; it is testable but harder to art-direct than final authored prefabs.
- `PaperBattle.prefab` retains substantial legacy 3D content and dormant systems, increasing import and maintenance cost.
- Save recovery exposes diagnostic status through logs/API rather than a polished player-facing recovery dialog.
- There is no automated cloud conflict policy, multiple-slot UI or achievement integration.

## 12. Roadmap position

Steps 1–6 are complete. Step 7 locks current-state and intended-design documentation without changing runtime behavior.

Known later phases are: Step 8 obsolete Prestige cleanup; Steps 9–10 incomplete-mechanics decisions and implementation; Step 11 enemy base scaling; Step 13 balance; and Step 14 final v1 zone/enemy/boss/content scope. The current Step 7 brief does not define Step 12, so this document does not invent it.

## Maintenance rule

Update this file only from verified code, assets, tests and runtime evidence. Record what ships now, including defects and placeholders. Never promote an implementation accident into intended design; put intent in [GAME_DESIGN_CONTRACT.md](GAME_DESIGN_CONTRACT.md) and register conflicts there.
