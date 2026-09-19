# Save-state contract

## 1. Goals

This is the authoritative design and shipped behavior established in Step 6 and extended in Steps 12.5/12.5G. It separates run state, meta progression, preferences, transient session state, and derived values; defines atomic checkpoint behavior; and prevents a partially restored game.

The production goals are: one canonical capture/apply path, resumable encounter-boundary checkpoints, explicit failure reporting, forward migration, validation before mutation, and no serialization of live-frame combat presentation.

## 2. Current save implementation

`GamePersistence` stores schema version 8 as UTF-8 JSON files under `Application.persistentDataPath`: `current-save.json`, `current-save.json.bak`, and the transactional `current-save.json.tmp`. The envelope carries schema version, stable run ID, deterministic run seed, UTC timestamp, and the complete payload. Class, subclass, weapon, gear, relic, skill, and passive ownership uses stable domain IDs rather than Unity object identity or list positions. Gear serializes weapon type/ranges, affix endpoints, OriginRarity, current/maximum Potential, and Empowered/boss-special provenance; currencies include fragment remainders and Empowerment Catalysts; relics retain generated level and tier metadata.

Capture creates a detached DTO without rewards, rolls, consumption, spawning, or progression changes. It validates before serialization, verifies the durable temporary file by parsing and validating it, atomically replaces the primary while rotating its previous version to backup, and reports success only afterward. Confirmed New Game additionally replaces the backup with the new run. Load validates the complete primary before mutation, tries backup on failure, and applies under a restoration guard; both invalid files remain untouched.

Historical `BlackCube.Save.V1` remains a read-only migration source. When neither current-format file exists, a valid V1 snapshot is parsed/validated, expanded with clean defaults for fields it never stored, restored, and only then committed as schema 8. Existing schema-2 JSON migrates sequentially through schemas 3, 4, 5, 6, 7 and 8 in memory before validation/application. Schema 8 adds `baseClassId`, subclass unlock/selection, and `weaponTypeId`; schema-7 saves default to Warrior and historical weapons default to Sword while all existing item values remain exact. Loaded fragment counts of ten or more normalize into full currencies plus remainders. After successful load, the migrated checkpoint is atomically committed. Newly generated schema-8 items/relics must satisfy current validation. The V1 key is preserved and cannot be repeatedly imported once a new-format file exists.

The persistence-related `PlayerPrefs` inventory is:

- `BlackCube.Save.V1`: historical gameplay snapshot retained only as a nondestructive migration source.
- `BlackCube.InventoryFilters.V1`: independent pickup and modifier-filter preferences.
- `BlackCube.Options.PausePassiveTree`: independent user preference.
- `BlackCube.PlayerDisplayName`: independent player-facing preference/profile value.
- `BlackCube.ModFilter.SkipModeWarning`: independent acknowledgement preference.

## 3. Pause-menu explicit save behavior

`GamePersistence.TrySave` is the only explicit gameplay-save entry point and executes the complete schema-8 capture/validation/file transaction. Existing `Save()` callers delegate to it.

`SAVE & MAIN MENU` and `SAVE & QUIT` continue only after `TrySave` returns true. A failure leaves the player in the paused gameplay scene.

## 4. Shipped save model

Step 6 uses **one current checkpoint slot plus one automatic backup of that same slot**. Explicit saves and autosaves update the same logical run; they are not separate progress branches.

| Model | UI/implementation cost | Loss/confusion risk | Steam use | Recommendation |
|---|---|---|---|---|
| One autosave/current slot | Lowest; current menu already assumes one Load action | Low with atomic write plus backup; New Game needs overwrite confirmation | Fits an idle game and Steam Cloud cleanly | **Recommended for v1** |
| Manual/current slot plus autosave | Requires choosing which snapshot Load uses and communicating two timelines | Can protect a manual point, but invites stale-slot confusion and save-scumming | More files/conflict cases | Defer unless players need rollback |
| Multiple slots | Slot browser, naming, timestamps, delete/overwrite UI, per-slot backups/migrations | Best intentional run separation, highest accidental wrong-slot complexity | More Cloud conflicts and support burden | Future feature only |

## 5. Complete state classification

Classifications: A = save-persistent run state, B = meta-progression, C = user preference, D = transient session state, E = derived/reconstructed state.

| State | Class | Authority | Reason and Step 6 representation |
|---|---:|---|---|
| Save/schema version, save ID, written-at time | A metadata | Persistence layer | Required for migration, diagnostics, and atomic replacement; timestamp is informational, not gameplay authority |
| Run ID and run seed | A | New-run/bootstrap service | Stable identity and deterministic encounter/reward derivation; create on confirmed New Game |
| Combat level | A | `GameManager` | Core resumable run position |
| Completed normal encounters / current stage / boss position | A | `GameManager` | Save completed-normal count and boss-stage flag or a validated equivalent; never infer solely from UI text |
| Encounter deterministic seed/ordinal | A | Encounter coordinator | Reconstruct the same current encounter without serializing a live enemy or global Unity RNG state |
| Player level, XP, available passive points | A | `PlayerProgression` | Authoritative progression values; validate bounds and point accounting |
| Passive allocations/ranks | A | `PlayerProgression` | Save stable node IDs/ranks; rebuild all stat/keystone modifiers after restore |
| Selected active skill | A | `PlayerSkillController` | Save a stable skill ID, not an asset instance/reference |
| Encounter-start player health/mana checkpoint | A | `GamePersistence` / `BattleManager` | Restores the clean start of the current encounter; do not capture arbitrary mid-frame values |
| Inventory gear | A | `Inventory` | Full stable gear identity, weapon min/max, paired affix endpoints, permanent implicit marker and legacy-affix compatibility marker |
| Equipped gear | A | `EquipmentManager` | Full gear identity plus slot; validate unique slots and prevent duplicate item ownership |
| Ordinary crafting currency | A | `CurrencyInventory` | Run economy; nonnegative bounded counts |
| Armed currency/cursor state | D | `CurrencyInventory`/UI | In-progress UI intent; cancel on save/load/menu transitions rather than resume it |
| Pickup filter enable flags, level/rarity thresholds | C | `Inventory` today | Player choice, not run power; move to preferences independent of slot/New Game |
| Mod-filter mode, simple/advanced selections, required-match count | C | `InventoryModFilter` today | Player UI preference; serialize stable enum IDs and validate removed stats |
| Relic history and modifier rolls | B | `RelicInventory` | Permanent rebirth progression; save full stable identity and locked-roll state |
| Current relic cycle / rebirth count | B | `RelicInventory` | `currentCycle` is currently the effective rebirth counter; do not add a duplicate counter unless design later distinguishes them |
| Active relic slots | B | `RelicInventory` | Save relic IDs rather than list indices in the production schema so reorder/migration is safe |
| Current-cycle relic and crafting eligibility | B/E | `RelicInventory` | Derive current relic from cycle + relic IDs where possible; persist eligibility only if it cannot be derived safely from cycle ownership |
| Ancient currency | A | `CurrencyInventory` | Run crafting currency even though it operates on relics; restore on Load, replace during Rebirth, and clear completely on New Game |
| Pending rebirth confirmation | D | `RebirthManager` | Modal intent; always clear on save/load/scene entry |
| Passive-tree pause option | C | `GameplayOptions` | Already independent in `PlayerPrefs`; never reset with gameplay |
| Display name | C | `PlayerDisplayNameProvider` | Already independent in `PlayerPrefs`; preserve across New Game |
| Skip mod-filter warning acknowledgement | C | `InventoryModHighlightUI` | UI acknowledgement; preserve across New Game |
| Pause/play state and open panels | D | `PaperBattleHUD`/UI | Load into running gameplay with overlays closed; pause again only when the user opens the menu |
| Player/enemy status effects | D | `StatusController` | Excluded by clean encounter checkpoint; begin restored encounter with none |
| Exact enemy object, current health/mana, rarity object, equipment objects | D/E | `BattleManager` and enemy generation | Recreate a full-health enemy from saved run/encounter seed and position |
| Attack gauges, queued turns, projectiles, animation frames, popup state | D | Combat/presentation | Live-frame state; reset to encounter-start defaults |
| Death state and death menu | D | `GameManager`/`DeathMenuUI` | Do not save a dead/modal checkpoint; explicit save exists only during living paused gameplay |
| Zone/biome/background | E unless future branching makes it independent | `ZoneManager` | Currently derived from combat level; add a stable zone ID only if future routing is not level-derived |
| Calculated stats, item/passive/relic modifiers, keystone projection | E | Stats/equipment/progression systems | Rebuild in dependency order; never serialize caches or totals |
| HUD/tooltips/scroll positions/optimizer results | D/E | UI | Presentation-only and safely recreated |
| Unlocks/tutorial/achievements | Future B or C | Not implemented | Use stable IDs and separate profile/meta storage when designed; achievements must not be part of New Game run deletion |
| Global RNG engine state | D | Unity | Do not serialize; use stable run/encounter/outcome seeds and checkpoint random outcomes transactionally |

## 6. Quit/Load, Restart, Rebirth, and New Game matrix

“Quit/Load” means Save & Quit or Save & Main Menu followed by Load of the clean checkpoint.

| State | Quit/Load | Restart after death | Rebirth | New Game |
|---|---|---|---|---|
| Combat level and encounter position | Restore same level/stage at clean encounter start | Keep level; reset to normal encounter 0 | Reset to level 1/encounter 0 | Reset to level 1/encounter 0 |
| Player level/XP/passives/points | Restore | Preserve | Reset | Reset |
| Selected active skill | Restore | Preserve | Preserve current behavior | Reset |
| Encounter-start health/mana | Restore checkpoint | Full restore | Full restore | Full defaults |
| Statuses/gauges/projectiles | Clear/rebuild defaults | Clear/rebuild defaults | Clear | Clear |
| Enemy | Recreate same encounter deterministically at full resources | Spawn first normal enemy | Spawn first normal enemy | Spawn first normal enemy |
| Inventory/equipment | Restore | Preserve | Clear and starter weapon | Clear and starter weapon |
| Ordinary currency | Restore | Preserve | Clear | Clear |
| Armed currency/UI cursor | Clear | Clear | Clear | Clear |
| Relic history/cycle/active slots | Restore | Preserve | Preserve history/slots; advance cycle and create current relic | Clear completely; reset cycle/history and active slots |
| Ancient currency | Restore | Preserve | Replace old stacks with one fresh unit of each Ancient operation | Clear as run currency |
| Filters/preferences | Preserve independently | Preserve | Preserve | Preserve |
| Existing disk checkpoint | Updated by explicit save | Unchanged until an autosave trigger | Atomically replace after successful rebirth transaction | Confirm, then atomically replace with the initialized new-run checkpoint |

## 7. New Game contract

After any required overwrite confirmation, New Game creates a completely blank gameplay profile: clear stale Load intent; clear relic history, active relic slots, the current relic cycle, and all rebirth history; clear Ancient and ordinary currency; reset combat/progression/items/equipment/skill/resources; close transient UI; cancel armed currency/rebirth confirmation; create a new run ID/seed; and start level 1 encounter 0 with starter equipment. Preferences and future platform achievements always survive.

If a checkpoint exists, show an explicit “Start New Game and overwrite current run?” confirmation. After confirmation, initialize and validate the complete new-run state, then atomically replace the current slot with its first checkpoint; do not delete or invalidate the only valid old file before the replacement is durable. Cancelling the confirmation leaves both the active state and checkpoint unchanged.

## 8. Restart contract

Preserve Step 4: Restart means restart the current combat level. It retains the combat level, XP, passive allocations/points, selected skill, inventory, equipment, all currencies, relics, cycle, and preferences. It clears death state, statuses, gauges, projectiles, enemy state, and encounter kills; restores player resources; then spawns normal encounter 0. Restart itself should checkpoint after the replacement encounter is established if death/restart persistence is desired.

## 9. Rebirth contract

Preserve the current atomic transaction: require combat zone 60 plus explicit confirmation; clear equipment, inventory, ordinary and prior Ancient currency; reset player/run progression and combat to level 1; clear combat transients; lock prior relics; increment the cycle; create one craftable current-cycle relic with one locked tiered modifier; grant one of each Ancient operation; retain relic history and active slots; provision exactly one starter after active relic effects resolve; then save once after the full transaction succeeds. Pending confirmation is never persisted.

## 10. Save & Quit contract

The shipped path captures the canonical **start-of-current-encounter checkpoint**, validates and atomically commits run + meta state, verifies the committed file, and only then requests `Application.Quit`.

Load resumes the same combat level and encounter position with the same deterministically generated enemy at full encounter-start resources. Player health/mana restore to values captured when that encounter began. Statuses, attack gauges, queued attacks, projectiles, animation frames, popups, pause state, and open panels reset. This avoids fragile live-frame serialization and prevents save timing from changing the generated enemy/reward identity.

## 11. Save & Main Menu contract

Use exactly the same canonical checkpoint capture/validation/commit routine as Save & Quit. After success, unpause the global clock and load `Main Menu`, allowing normal lifecycle teardown. After failure, remain in the paused gameplay scene. Returning to Main Menu by any route must not silently imply saving unless that button explicitly says Save.

## 12. Autosave triggers

Use transaction-aware immediate checkpoints plus a short debounce for ordinary progression. Never save from every event independently while a larger transaction is incomplete.

| Trigger | Policy |
|---|---|
| Save & Quit / Save & Main Menu | Immediate explicit checkpoint; surface failure and block exit |
| Rebirth | Immediate once after the complete rebirth transaction |
| Random crafting or relic crafting | Immediate once after currency consumption and outcome both commit |
| Enemy reward/loot | Immediate after XP, currency/item reward, and encounter advancement are all committed; prevents reward rerolls/duplication |
| Boss kill/combat-level advancement | Immediate after reward and next encounter checkpoint exist |
| Passive allocate/refund, skill selection, equip/unequip | Debounced (recommended 1–3 seconds), coalescing rapid UI changes |
| Ordinary currency/item changes outside a reward transaction | Debounced and coalesced |
| Level-up | Included in the enclosing reward transaction; otherwise debounced |
| Application pause / application quit callback | Flush any pending debounce and attempt immediate checkpoint |
| Periodic timer | Not recommended initially; add only if long idle intervals can mutate meaningful state without listed events |
| Merely opening/closing UI or returning without a Save-labelled action | No gameplay save |
| Preferences | Store independently and immediately/debounced in the preference store, not the run checkpoint |

## 13. Deterministic restoration sequence

1. Read primary bytes without mutating runtime state; if unreadable, read backup.
2. Parse a version envelope, migrate DTOs in memory to the current schema, and validate the complete candidate graph.
3. Enter a restoration guard that suppresses autosaves, rewards, spawning, and change-event side effects.
4. Perform normal Step 4 gameplay lifecycle creation and bind authoritative persistent services to the new scene.
5. Reset run-owned containers to a known empty baseline.
6. Restore relic records by stable ID, cycle, and active relic IDs; restore Ancient currency with the other run currencies. New Game clears all of these instead of entering this restore path.
7. Recreate inventory Gear identities and establish a save-ID-to-object map.
8. Restore equipment by item ID/slot, ensuring each item has one owner.
9. Restore player level, XP, available points, and passive node IDs; then rebuild passive/keystone projections.
10. Restore selected skill by stable catalog ID.
11. Restore ordinary currency and clear armed currency.
12. Restore combat level, completed-normal count/boss position, run/encounter seed, and encounter-start player resources.
13. Rebind equipment/progression to the new player and rebuild derived stats exactly once.
14. Generate zone presentation and the saved encounter deterministically; initialize enemy at full resources.
15. Apply checkpoint player health/mana through validated restore APIs; clear statuses/gauges/projectiles/modal UI and set `Time.timeScale = 1`.
16. Release the restoration guard, publish one coherent refresh, and mark load successful. On any failure before release, discard the candidate/runtime attempt and return to a safe fresh-run or Main Menu path without autosaving over the recoverable file.

## 14. Version and migration policy

The shipped storage envelope is schema 9 and is independent from the historical `BlackCube.Save.V1` key. Schema 4 distinguished strict new implicit/explicit legality from schema-3 historical side-cap exceptions; schema 5 adds fragments; schema 6 adds relic level/tier; schema 7 adds OriginRarity, Potential, Empowerment, and boss-special provenance; schema 8 adds base-class, subclass milestone/selection, and weapon-type identity; schema 9 clears V1 passive allocations and refunds exactly one earned point per level for V2. Keep migrations sequential and DTO-only before validation.

Schema-6 gear migrates without rerolling or changing any affix/value: its current rarity becomes legacy OriginRarity, it receives that rarity's full 6/8/10/14 Potential, and its preexisting modifiers remain ordinary/unempowered/non-special. Queued active-skill state is combat-transient and is never serialized; a restored encounter starts with no queued skill.

Keep the original primary/backup untouched until migrated data validates and a new atomic file commits. Missing optional fields receive documented defaults. A version newer than the build is unsupported and must not be overwritten. A version older than the oldest supported migration should offer recovery/new game while retaining the files for support. Migration failure falls through to backup, then reports a recoverable load failure.

## 15. Corruption and backup policy

Validate before apply: required IDs, finite numeric values, sensible ranges, known enums, unique gear IDs/slot ownership, valid modifier IDs/tiers, nonnegative currencies, passive topology/point accounting, relic ID/slot integrity, and legal combat/encounter positions. Unknown optional future fields may be ignored; unknown required gameplay enums/items invalidate that candidate or are removed only by an explicit documented migration.

Write serialized bytes to a temporary file in the same directory, flush/close, parse and validate the temp file, rotate the last valid primary to `.bak`, then atomically replace/rename temp to primary. Keep one last-known-good backup. On load: primary validates first, backup second. Never autosave after a failed or partial restore. If both fail, leave the files intact, report the error, and offer New Game/retry rather than entering half-restored gameplay.

## 16. Storage implementation

Gameplay snapshots are UTF-8 JSON files under `Application.persistentDataPath`, using temp + atomic replacement and primary + backup. Small user preferences remain in `PlayerPrefs`, including the inventory/mod-filter preference record.

Each character slot owns `slot-01.json` through `slot-06.json`, plus its own `.bak` and `.tmp`. The active slot is the only capture/load target. The prior `current-save.json`/`.bak` pair is read as a legacy single character and copied non-destructively into Slot 1 once; the historical source is retained. Do not expose absolute paths in gameplay UI; log them for diagnostics.

## 17. Derived versus serialized state

Serialize authoritative identities and inputs: run/encounter IDs, progress counters, allocation IDs, gear/relic rolls, currency counts, skill ID, and checkpoint resources. Reconstruct calculated stats, equipment/passive/relic modifiers, keystone state, zone image, enemy GameObjects, HUD state, item tooltips, filter highlight visuals, optimizer results, and all combat presentation.

Never serialize Unity object references, asset instance IDs, delegates, event subscriptions, cached totals, or `UnityEngine.Random.state` as the primary determinism mechanism.

## 18. Randomness and future considerations

Accepting rerolls on reload is not recommended because item/relic crafting and boss rewards materially affect progression. Commit random outcomes immediately as one transaction. For not-yet-resolved encounters, derive generation from a persisted run seed plus stable combat-level/encounter ordinal. Persist the resolved result itself once the outcome is awarded. This is simpler and more migration-friendly than restoring Unity’s global RNG stream.

Future tutorial flags, unlocks, and achievements need stable IDs and explicit profile-vs-run ownership. Steam Cloud conflict handling should prefer the newest valid timestamp only when save ancestry/run ID is compatible; otherwise ask the player rather than silently merging item graphs.

## 19. Approved user decisions

| Decision | Approved contract | Shipped Step 6 behavior |
|---|---|---|
| Relic history on New Game | Clear completely | Removes all relic records, rolls, current-cycle state and active slots before committing the new checkpoint. |
| Rebirth cycle/history on New Game | Reset | Returns `currentCycle` to zero; this is the current effective rebirth-history authority. |
| Ancient currency on New Game | Clear as run currency | Clears every Ancient stack while retaining the established Rebirth replacement behavior. |
| Existing save on New Game | Confirm and automatically replace | Cancel leaves files untouched; Confirm atomically installs a new run and copies it to backup so old progress cannot recover. A failed initial commit returns to Main Menu. |
| Mid-combat Save/Load | Restore a clean start of the current encounter | Persists run/encounter identity and encounter-start resources; recreates the encounter deterministically with no live-frame transients. |

All five approved decisions are implemented; none remains unresolved.
# Schema 10

Schema 9 migrates directly to schema 10 without reinterpretation. Schema 10 persists selected/unlocked subclass state, stable transformed node IDs, and the Ranger Volley/Focused setting. Freeze, combo, aura contribution, enemy-acted state, Shock/Poison instances, queued skills, and partial cooldowns remain transient under clean-encounter restore.
