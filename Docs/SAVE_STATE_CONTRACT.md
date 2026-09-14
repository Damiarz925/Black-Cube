# Save-state contract

## 1. Goals

This is the authoritative design input for Step 6. It separates run state, meta progression, preferences, transient session state, and derived values; defines atomic checkpoint behavior; and prevents a partially restored game. Step 5 does **not** add fields to `GameSaveData` or migrate storage.

The production goals are: one canonical capture/apply path, resumable encounter-boundary checkpoints, explicit failure reporting, forward migration, validation before mutation, and no serialization of live-frame combat presentation.

## 2. Current save implementation

`GamePersistence` stores one version-1 JSON string under `BlackCube.Save.V1` in `PlayerPrefs`. V1 captures inventory gear, equipped gear, all currency stacks, relic history, relic cycle, and four active-relic indices. Gear identity includes type, rarity, item level, element, weapon base values, and every rolled modifier including tier/value/locked-original state.

V1 does not capture combat level/stage, player progression, passive allocations, selected skill, resources, encounter checkpoint, filters, or preferences. Loading accepts only `version == 1` and applies directly to live services; it does not validate the whole graph first, migrate, back up, or roll back a partial apply. The one-shot `loadRequested` flag is transition intent, not save data.

The complete `PlayerPrefs` inventory is:

- `BlackCube.Save.V1`: current V1 gameplay snapshot.
- `BlackCube.Options.PausePassiveTree`: independent user preference.
- `BlackCube.PlayerDisplayName`: independent player-facing preference/profile value.
- `BlackCube.ModFilter.SkipModeWarning`: independent acknowledgement preference.

## 3. Pause-menu explicit save behavior

Step 5 adds `GamePersistence.TrySave`, which calls the same existing V1 capture route, catches/report failures, flushes `PlayerPrefs`, and verifies that the written JSON can be read back unchanged. Existing `Save()` callers delegate to it. No V1 fields changed.

`SAVE & MAIN MENU` and `SAVE & QUIT` continue only after `TrySave` returns true. A failure leaves the player in the paused gameplay scene. Once Step 6 replaces the canonical save internals, both buttons automatically receive the full contract.

## 4. Recommended save model

Use **one current checkpoint slot plus one automatic backup of that same slot** for the first production version. Explicit saves and autosaves update the same logical run; they are not separate progress branches.

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
| Encounter-start player health/mana checkpoint | A | Future checkpoint snapshot owned by combat/run coordinator | Restores the clean start of the current encounter; do not capture arbitrary mid-frame values |
| Inventory gear | A | `Inventory` | Full gear identity/rolls already supported by V1 |
| Equipped gear | A | `EquipmentManager` | Full gear identity plus slot; validate unique slots and prevent duplicate item ownership |
| Ordinary crafting currency | A | `CurrencyInventory` | Run economy; nonnegative bounded counts |
| Armed currency/cursor state | D | `CurrencyInventory`/UI | In-progress UI intent; cancel on save/load/menu transitions rather than resume it |
| Pickup filter enable flags, level/rarity thresholds | C | `Inventory` today | Player choice, not run power; move to preferences independent of slot/New Game |
| Mod-filter mode, simple/advanced selections, required-match count | C | `InventoryModFilter` today | Player UI preference; serialize stable enum IDs and validate removed stats |
| Relic history and modifier rolls | B | `RelicInventory` | Permanent rebirth progression; save full stable identity and locked-roll state |
| Current relic cycle / rebirth count | B | `RelicInventory` | `currentCycle` is currently the effective rebirth counter; do not add a duplicate counter unless design later distinguishes them |
| Active relic slots | B | `RelicInventory` | Save relic IDs rather than list indices in the production schema so reorder/migration is safe |
| Current-cycle relic and crafting eligibility | B/E | `RelicInventory` | Derive current relic from cycle + relic IDs where possible; persist eligibility only if it cannot be derived safely from cycle ownership |
| Ancient currency | B, pending decision | `CurrencyInventory` | Mechanically tied to permanent relic crafting; recommended meta state, but New Game behavior requires user approval |
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
| Relic history/cycle/active slots | Restore | Preserve | Preserve history/slots; advance cycle and create current relic | **User decision**; currently preserve |
| Ancient currency | Restore | Preserve | Replace old stacks with one fresh unit of each Ancient operation | **User decision**; currently preserve |
| Filters/preferences | Preserve independently | Preserve | Preserve | Preserve |
| Existing disk checkpoint | Updated by explicit save | Unchanged until an autosave trigger | Atomically replace after successful rebirth transaction | **User decision**; recommend confirmation then replace with new-run checkpoint |

## 7. New Game contract

Confirmed behavior independent of unresolved meta choices: clear stale Load intent, create a new run ID/seed, reset combat/progression/items/ordinary currency/skill/resources, close transient UI, cancel armed currency/rebirth confirmation, and start level 1 encounter 0. Preferences and future platform achievements always survive.

If a checkpoint exists, recommendation is an explicit “Start New Game and overwrite current run?” confirmation. After confirmation, initialize the new run and atomically write its first checkpoint; do not delete the only valid old file before the replacement is durable. Relic/rebirth/Ancient behavior remains a user decision below.

## 8. Restart contract

Preserve Step 4: Restart means restart the current combat level. It retains the combat level, XP, passive allocations/points, selected skill, inventory, equipment, all currencies, relics, cycle, and preferences. It clears death state, statuses, gauges, projectiles, enemy state, and encounter kills; restores player resources; then spawns normal encounter 0. Restart itself should checkpoint after the replacement encounter is established if death/restart persistence is desired.

## 9. Rebirth contract

Preserve the current atomic transaction: require level 50 plus explicit confirmation; clear equipment, inventory, ordinary and prior Ancient currency; reset run progression and combat to level 1; clear combat transients; equip a starter weapon; lock prior relics; increment the cycle; create one craftable current-cycle relic with one locked modifier; grant one of each Ancient operation; retain relic history and active slots; then save once after the full transaction succeeds. Pending confirmation is never persisted.

## 10. Save & Quit contract

Step 6 should capture the canonical **start-of-current-encounter checkpoint**, validate and atomically commit run + meta state, verify the committed primary or fallback backup, and only then request `Application.Quit`.

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
| Application pause/focus loss | Flush any pending debounce and attempt immediate checkpoint |
| Periodic timer | Not recommended initially; add only if long idle intervals can mutate meaningful state without listed events |
| Merely opening/closing UI or returning without a Save-labelled action | No gameplay save |
| Preferences | Store independently and immediately/debounced in the preference store, not the run checkpoint |

## 13. Deterministic restoration sequence

1. Read primary bytes without mutating runtime state; if unreadable, read backup.
2. Parse a version envelope, migrate DTOs in memory to the current schema, and validate the complete candidate graph.
3. Enter a restoration guard that suppresses autosaves, rewards, spawning, and change-event side effects.
4. Perform normal Step 4 gameplay lifecycle creation and bind authoritative persistent services to the new scene.
5. Reset run-owned containers to a known empty baseline.
6. Restore meta first: relic records by stable ID, cycle, active relic IDs, and Ancient currency per approved policy.
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

Introduce a storage-envelope version independent from the historical `BlackCube.Save.V1` key. Increment the schema version for any serialized meaning/shape change. Support sequential pure migrations (`V1 -> V2 -> ... -> current`) on DTOs before validation; never migrate by partially applying old data to live objects.

Keep the original primary/backup untouched until migrated data validates and a new atomic file commits. Missing optional fields receive documented defaults. A version newer than the build is unsupported and must not be overwritten. A version older than the oldest supported migration should offer recovery/new game while retaining the files for support. Migration failure falls through to backup, then reports a recoverable load failure.

## 15. Corruption and backup policy

Validate before apply: required IDs, finite numeric values, sensible ranges, known enums, unique gear IDs/slot ownership, valid modifier IDs/tiers, nonnegative currencies, passive topology/point accounting, relic ID/slot integrity, and legal combat/encounter positions. Unknown optional future fields may be ignored; unknown required gameplay enums/items invalidate that candidate or are removed only by an explicit documented migration.

Write serialized bytes to a temporary file in the same directory, flush/close, parse and validate the temp file, rotate the last valid primary to `.bak`, then atomically replace/rename temp to primary. Keep one last-known-good backup. On load: primary validates first, backup second. Never autosave after a failed or partial restore. If both fail, leave the files intact, report the error, and offer New Game/retry rather than entering half-restored gameplay.

## 16. Storage recommendation

Move gameplay snapshots in Step 6 from `PlayerPrefs` to UTF-8 JSON files under `Application.persistentDataPath`, using temp + atomic replacement and primary + backup. File storage supports size growth, inspection, backup recovery, Steam Cloud synchronization, and explicit I/O failure handling. Keep small user preferences in `PlayerPrefs` unless a later settings/profile file is introduced.

Suggested logical files are `current-save.json`, `current-save.json.bak`, and a same-directory temporary file. Do not expose absolute paths in gameplay UI; log them for diagnostics.

## 17. Derived versus serialized state

Serialize authoritative identities and inputs: run/encounter IDs, progress counters, allocation IDs, gear/relic rolls, currency counts, skill ID, and checkpoint resources. Reconstruct calculated stats, equipment/passive/relic modifiers, keystone state, zone image, enemy GameObjects, HUD state, item tooltips, filter highlight visuals, optimizer results, and all combat presentation.

Never serialize Unity object references, asset instance IDs, delegates, event subscriptions, cached totals, or `UnityEngine.Random.state` as the primary determinism mechanism.

## 18. Randomness and future considerations

Accepting rerolls on reload is not recommended because item/relic crafting and boss rewards materially affect progression. Commit random outcomes immediately as one transaction. For not-yet-resolved encounters, derive generation from a persisted run seed plus stable combat-level/encounter ordinal. Persist the resolved result itself once the outcome is awarded. This is simpler and more migration-friendly than restoring Unity’s global RNG stream.

Future tutorial flags, unlocks, and achievements need stable IDs and explicit profile-vs-run ownership. Steam Cloud conflict handling should prefer the newest valid timestamp only when save ancestry/run ID is compatible; otherwise ask the player rather than silently merging item graphs.

## 19. Explicit unresolved user decisions

| Decision | Current behavior | Option A | Option B | Recommendation | Why |
|---|---|---|---|---|---|
| Relic history when starting New Game | Preserved in memory; disk save untouched | Preserve relic history/cycle/active slots as account meta | Clear all relic/rebirth state for a completely blank profile | **Preserve** | Relics are described and implemented as permanent rebirth rewards; clearing them makes New Game function like profile deletion |
| Rebirth cycle/history when starting New Game | Preserved as `RelicInventory.currentCycle` | Preserve with relic meta | Reset cycle/history | **Preserve** | The cycle is the provenance and crafting authority for permanent relics; splitting it from retained relics creates invalid history |
| Ancient currency when starting New Game | Preserved temporarily; Rebirth replaces it | Preserve as meta crafting currency | Clear as run economy | **Preserve** | Ancient operations target permanent/current-cycle relics and are awarded by rebirth, unlike ordinary equipment currency |
| Existing checkpoint when New Game is pressed | Left untouched until some later save | Confirm, then atomically replace the single current slot with the initialized new run | Keep old slot alongside an unsaved new run | **Confirm and replace** | One-slot v1 stays understandable and cannot accidentally load a run different from the active one; atomic replacement protects against loss |

These four recommendations are design proposals, not Step 5 behavior changes. The user must approve them before Step 6 encodes them into reset, schema, and migration rules.
