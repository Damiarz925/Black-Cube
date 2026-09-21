# Black-Cube game design contract

## Step 19 production world contract

V1 world progression is six biomes × ten locations × six corruption tiers, with nine normal stages and one boss stage per combat level. The mechanical catalog contains 48 non-boss enemies, 60 main bosses, six repeatable challenge bosses, deterministic skill cadence, reusable boss phases, and central location/corruption profiles. The level-100 Burning Crown boss owns `story.main.complete` and unlocks subclass choice on defeat. Final presentation assets and final numerical balance are not claimed by this content pass.

## 1. Document authority and change policy

> **This document defines intended game behavior.** Current code does not override it. Unresolved items must not be guessed. Design changes require explicit user approval; Codex may propose changes but must not silently rewrite this contract. When implementation differs, report the difference in the discrepancy register and update [PROJECT_STATE.md](PROJECT_STATE.md) with code reality.

Detailed approved specifications remain authoritative within their scopes, especially [PASSIVE_TREE.md](PASSIVE_TREE.md), [SAVE_STATE_CONTRACT.md](SAVE_STATE_CONTRACT.md), and [RUNTIME_LIFECYCLE.md](RUNTIME_LIFECYCLE.md). A newer explicitly approved first-party decision may supersede an older one and should record that fact.

Intended-design claims use this authority order: (1) explicit user-approved rules, (2) dedicated specifications such as `PASSIVE_TREE.md`, (3) existing first-party design documentation, then (4) clearly intentional implemented behavior. Current code alone is not evidence that a behavior is approved design.

Maintenance follows the same boundary: never alter intended behavior here solely because implementation changes. An approved design change must say what it supersedes. If code conflicts, preserve intent here, update current reality in `PROJECT_STATE.md`, revise the discrepancy register, and report the conflict.

## 2. Product identity and pillars

Black-Cube is an idle/auto-battle action RPG centered on:

- Buildcraft and meaningful character specialization
- Itemization and gear crafting
- A large passive tree
- Repeated encounter and combat-level progression
- Active skills queued to replace scheduled automatic attacks
- Rebirth and relic-based meta progression
- A stylized layered paper/cut-paper/storybook presentation

Basic attacks continue automatically without active input. Activating the equipped skill queues it to replace the next scheduled basic attack; it is not an additional attack. A more directly controlled top-down or bullet-hell boss mode has been discussed, but it is not locked for the current core or v1 scope.

## 3. Core gameplay loop

Start or resume one run, automatically fight repeated encounters, earn XP/items/currency, refine equipment and passive/skill choices, defeat bosses to advance combat levels, and eventually Rebirth to exchange run progress for persistent-within-save relic progression. Death restarts the current combat level without erasing the build. Save/load returns to a clean deterministic encounter boundary.

Moment-to-moment input should focus on build decisions, inspection, crafting, active-skill timing and navigation—not manually issuing every basic attack.

## 4. Combat model

- Combat is primarily automatic and gauge/attack-speed driven.
- The current cadence is nine normal encounters followed by a boss as stage 10.
- Boss defeat advances the combat level and begins the next level's normal sequence.
- Player and enemy damage use the same broad stat/damage-context language, while their exact sources may differ.
- Active skills may convert damage, add hits/projectiles, specialize ailments, and consume mana.
- V1 main progression is 360 combat levels. Production enemy and boss roster counts remain unresolved.

## 5. Damage types

The core direct-hit elements are Physical, Fire, Cold, Lightning and **Void**. Void is a first-class non-Physical element with flat, increased, more, resistance, penetration and maximum-resistance stats. The generic, elemental/Magic and relevant global scopes include Void. Poison, Bleed and Ignite/Burn are ailments, not additional direct-hit elements. Preserve the serialized legacy `Element.Poison = 4`; canonical direct contexts normalize that old value to Void rather than reordering the enum.

## 6. Ailments

- **Poison:** an ailment sourced from an eligible damaging hit, including Envenom's authored conversion. Its ticks deal **Void damage over time**. Poison resistance/penetration controls application/effect; Void resistance, Void penetration and the Void maximum-resistance cap mitigate its ticks. Poison-specific and Void offensive scaling each apply once; an already-Void-scaled source does not double-dip.
- **Bleed:** damage-over-time ailment sourced from Physical damage.
- **Ignite/Burn:** damage-over-time ailment sourced from Fire damage.
- **Step 12.5 damaging-stack rules:** each application keeps its own snapshotted strength, remaining duration and next tick; status UI aggregates them without merging actual damage. Poison has no global stack cap, lasts eight **global combat turns**, and ticks every second global turn (four ticks). Bleed holds at most five independent stacks, lasts ten **afflicted-actor turns**, ticks every second such turn (five ticks), and a stronger incoming stack replaces the weakest remaining-total-damage stack at capacity. Ignite holds one stack, lasts four afflicted-actor turns, ticks every second (two ticks), and only a stronger application replaces it. An afflicted-actor turn is that actor's resolved turn, not the attacker's turn. Direct-hit ailment basis is snapshotted after damage range, outgoing increases/More and critical multiplier, but before target armour/resistance; the ailment then applies its own offense and target mitigation exactly once. Repeated hits/applications each roll independently.
- **Shock:** a nonzero eligible Lightning hit generates `floor(effective chance / 100%)` guaranteed stacks plus a fractional-remainder roll. At five stacks, consume exactly five and trigger one already-mitigated secondary Lightning hit per threshold; keep overflow. The secondary basis is actual triggering Lightning damage dealt, multiplied by `min(100%, 50% × (1 + Shock Effect))`. It cannot recursively Shock, Hit Twice or retarget a replacement. Base lifetime is five global turns, refreshed on contribution. Lightning Strike's authored extra-hit interaction remains, and its real hits can add stacks.
- **Chill:** an eligible nonzero Cold hit slows attack speed. `RawSlow = clamp(5% + ColdDamageDealt / TargetMaxLife, 5%, 30%)`; `FinalSlow = min(30%, RawSlow × (1 + ChillEffect))`. This dynamically multiplies real player/enemy gauge speed by `(1 - FinalSlow)`. Base lifetime is four global turns, minimum one after duration adjustments. Stronger replaces and refreshes, equal refreshes, weaker does not overwrite. Ice Strike guarantees its authored Chill application without converting it to a chance roll; resistance may reduce magnitude/duration.

Deep Freeze's authored extra application chance is retained; the numerical increase to its maximum Chill effect remains unset (serialized zero default), so the v1 30% cap remains authoritative until separately tuned.

## 7. Active skills

The initial locked skill set and currently implemented numeric data are:

| Skill | Mana | Current exact data and intended role |
|---|---:|---|
| Heavy Strike | 30 | 1.3× Physical hit; heavy/chunky attack. |
| Ice Strike | 20 | 1.2× hit, converts 50% of non-Cold damage to Cold, and guarantees one additional Chill application. |
| Lightning Strike | 10 | Each hit is 0.55×, converts 50% of non-Lightning damage to Lightning, and is intended to interact with Shock through extra hits. General threshold behavior still needs reconciliation. |
| Fireball | 100 | 2.0× hit, converts 50% of non-Fire damage to Fire, and uses a projectile presentation. |
| Envenom | 20 | Suppresses direct hit damage and uses the normal attack basis to produce Poison. |
| Shiv | 30 | 0.7× direct hit with a 2.0× Bleed basis. |
| Immolate | 60 | 0.1× direct hit, 50% non-Fire conversion to Fire, and Ignite with an absolute 2.5× source coefficient. |

One active skill may be equipped at a time. Skills use the established damage-context conversion model and interact with compatible gear/passive/keystone scopes. If future tuning changes these numbers, require approval and update both the catalog and this table together.

Each of the seven skills starts at level 1, gains matching `+Skill Level` affixes, and caps at 20. Its complete authored offensive package receives exactly one linear factor `1 + 0.05 × (level - 1)`; mana cost receives `1 + 0.02 × (level - 1)` and is rounded by the existing skill-cost convention. Levels alone do not alter projectile/hit count, conversion, chance, duration, Shock threshold or Chill cap. The player-only +1 affixes roll at item level 40 or above as single-tier value-1, weight-5 amulet affixes.

## 8. Mana and resources

Active skills consume mana; unavailable mana prevents casting. Mana regenerates and may be modified by implemented passive/resource rules. Basic auto-attacks do not require skill activation. A general cooldown system is not approved merely because `CooldownRecovery` exists.

Life and mana are run resources. Death/restart and encounter-boundary persistence follow the lifecycle/save contracts rather than creating a new run.

Damage per Maximum Mana and Damage per Current Mana add global *increased* damage: `affix percentage points × (maximum mana / 100)` and `affix percentage points × (snapshotted current mana / 100)` respectively. Basic attacks snapshot at attack build; skills pay cost first and snapshot remaining mana, including every projectile. Ailment strength is snapshotted on creation rather than changing with later mana. A credited enemy death grants flat Life/Mana on Kill once, clamped to resource maxima.

## 9. Player progression

- Player level cap is 100.
- Leveling grants passive progression through the current point system.
- Rebirth becomes available at authoritative combat zone 60 or above.
- **New Game:** completely clears gameplay and relic/rebirth meta progression, including Ancient currency, while preserving independent preferences.
- **Rebirth:** resets the established run layer, preserves relic history, advances the relic cycle and creates/manages the new-cycle relic.
- **Restart after death:** restarts the current combat level and preserves the build/meta state defined in [RUNTIME_LIFECYCLE.md](RUNTIME_LIFECYCLE.md).

New Game, Rebirth and Restart are three distinct operations and must never share ambiguous UI wording or reset behavior.

## 10. Passive tree

[PASSIVE_TREE.md](PASSIVE_TREE.md) is the detailed authority. The current approved structure is one central player root, ten primary stat spokes, ten connecting/specialized bridges, a forty-node outer travel ring, ten inner keystones and ten outer keystones: 290 binary allocatable nodes and 20 keystones total.

Stable IDs, adjacency, refund safety, stat bindings, keystone effects and art rules belong in that specification rather than being duplicated here. Currently documented placeholders such as Bullet Hell projectile spawning and Deep Freeze's unbalanced maximum-effect increase remain discrepancies/unresolved work, not permission to invent values.

## 11. Items and affixes

Eight equipment slots are authoritative: Weapon, Helmet, Body Armour, Gloves, Boots, Amulet, Ring and Belt.

Rarities are Normal, Magic, Rare and Legendary. Every equipment item has exactly one permanent implicit modifier, rolled from any family legal for its item type and level. It survives upgrades and cannot be rerolled, added to, removed or replaced by ordinary crafting. Its source family's Prefix/Suffix classification is informational: it consumes neither side nor total explicit capacity, and an explicit may legally repeat the implicit's family. Explicit-to-explicit family/group exclusions remain. Guaranteed weapon base damage/attack-speed/critical rolls are separate intrinsic base values outside these counts.

Modifier tiers are item-level gated and may contain any authored number of tiers, including paired minimum/maximum damage rolls. T1 is the strongest tier eligible for that item's level and slot, not an assumed fifth row. Weapon-element matching damage rolls may be local to the weapon; other equipment rolls project globally. Duplicate stat/group exclusions and weighted definitions are part of the current affix model.

Step 12.5G explicit capacities are Normal 0; Magic 2 (1 Prefix/1 Suffix); Rare 4 (2/2); Legendary 6 (3/3). Natural full items generate those exact counts plus the implicit, displaying 1/3/5/7 modifiers respectively. Crafting may leave under-filled items without auto-refill. Add/reroll/remove/upgrades may not bypass an explicit side cap or mutate the implicit. Ordinary PoE-style tier gates and rolls for the direct-mapping families, together with Black-Cube slot deviations, are recorded in [POEDB_AFFIX_BASELINE.md](POEDB_AFFIX_BASELINE.md); nonmatching game-specific families retain authored data pending Step 13.

Weapon base damage is a min–max range. The starter weapon is 64–96 (average 80), has one permanent legal implicit, and keeps its authored 1.2 attacks/second and 5% base crit instead of applying randomized weapon-base rolls. Every resolved hit, including Hit Twice and real multi-hit interactions, samples its own value within the range; noncritical previews show the average. The base critical damage multiplier is 1.5×; critical-multiplier stats add their classified fractional value. Actual hit damage/life loss and critical presentation must show the final resolved result rather than a scalar estimate.

[STAT_AFFIX_AUDIT.md](STAT_AFFIX_AUDIT.md) records the historical Step 9 inventory and the Step 10 delta: 120 stable IDs (0–119), 107 pooled definitions including three guaranteed weapon bases, and 104 random v1 affixes. Removed families retain numeric IDs for schema-2 legacy gear but do not newly roll.

Final attributes clamp nonnegative after `(base + flat) × (1 + attribute-% increase)`. Per ten final Strength: +1% increased maximum Life and Physical damage; per ten Dexterity: +1% increased attack speed and projectile damage; per ten Intelligence: +1% increased maximum Mana and Fire/Cold/Lightning/Void damage. Fractional groups count. Explicit `Damage per Strength` and `Damage per Lowest Attribute` add their stored percentage points per ten final attributes; named flat/resource/speed/DOT scalers use their named attribute basis. Derived values are queried, not permanently written into raw buckets.

Elemental maximum resistance starts at 75%, matching individual and Maximum All modifiers add percentage points, and effective caps cannot exceed 90%. Fire, Cold, Lightning and Void use the same rule; penetration lowers effective resistance after cap, never the maximum stat itself.

The ordinary Poison-named damage passive branch now grants Void direct damage. Poison-chance and ailment-specialized passives remain Poison-specific; stable passive IDs and tree geometry are unchanged.

## 12. Crafting

The six ordinary equipment operations are:

1. Normal → Magic
2. Magic → Rare
3. Reroll Magic modifier
4. Add Rare/Legendary modifier
5. Reroll Rare/Legendary modifier
6. Remove Magic/Rare/Legendary modifier

Temporary Normal → Magic behavior preserves the implicit and adds exactly one Prefix and one Suffix. Magic → Rare preserves existing explicits and adds exactly two legal explicit modifiers, even to under-filled Magic gear; it does not refill all empty capacity. Magic and Rare/Legendary rerolls each replace exactly one randomly selected existing explicit on the same side. Rare/Legendary Add adds one explicit; Magic/Rare/Legendary Remove removes one explicit and may leave zero. Full Add and empty Remove fail without consuming currency. A future Add Magic Modifier currency is not part of this pass.

The six Ancient relic counterparts are Ancient Normal → Magic, Ancient Magic → Rare, Ancient Rare → Legendary, Ancient Reroll, Ancient Add Modifier and Ancient Remove Modifier. Ancient operations apply only to the eligible current-cycle relic. Successful random outcomes and currency consumption form one immediate save transaction.

Ancient currency inventory tiles and armed cursors use the supplied canonical artwork alone, without generated rune overlays or purple-square fallbacks; manual GEAR/RELICS tab selection cancels any armed crafting intent, while a valid Ancient target may switch to the Relics tab automatically without losing the intent. Existing relic crafting semantics stay separate from equipment implicits. The supplied art labels do not provide a dedicated Ancient Rare → Legendary image, so its current gold nearest-design mapping needs eventual user visual approval.

## 13. Pickup filters and dismantling

Items rejected by the modifier pickup filter intentionally auto-dismantle. This is approved behavior and must not be redesigned as an accidental discard bug.

The approved fragment model is now implementation reality for manual and filter-driven dismantling:

- Normal dismantles yield no fragments.
- Magic dismantles yield one fragment toward Normal → Magic currency.
- Ten matching fragments combine into one full orb.
- Rare dismantles yield the analogous fragment toward Magic → Rare currency.
- Legendary dismantles yield two Magic → Rare fragments.

Both dismantle modes use the same single-claim path. Current equipment no longer awards random full ordinary currencies; historical stacked scrap objects retain their legacy conversion on load. Fragments clear with ordinary currency on New Game/Rebirth and persist on Restart/Load.

## 14. Rebirth and relics

Rebirth is the intended meta-progression reset system and becomes available at combat zone 60+. It resets run progression according to the established contract, retains permanent-within-save relic history, advances the cycle, creates one levelled current-cycle relic, provisions a new starter after active relic effects are resolved, restricts Ancient crafting to current-cycle authority, and supports four active relic slots.

New Game clears relic history, active slots, cycle/rebirth history and Ancient currency. Rebirth is the only run-reset/meta-progression system; the superseded Prestige runtime path was removed in Step 8 and must not be reintroduced as a competing design.

## 15. Save, load and restart semantics

[SAVE_STATE_CONTRACT.md](SAVE_STATE_CONTRACT.md) and [RUNTIME_LIFECYCLE.md](RUNTIME_LIFECYCLE.md) are authoritative. Locked concepts are:

- One logical current-run save with primary plus backup
- Schema 8 and validated atomic writes, with sequential schema-2/3/4/5/6/7 migration, exact historical-affix preservation, fragment normalization, origin/Potential/Empowerment persistence, stable base-class/subclass state, and stable weapon types

## Step 16 class/weapon contract

V1 has six base classes and six signature weapon mappings, but any class may equip any weapon. Classes add no innate stats. Each weapon owns exactly two skill slots and the player has one replaceable next-attack queue. Final skill-to-weapon mappings and final subclass identities are not yet approved and remain unassigned production content. Story completion unlocks one class-valid subclass selection through `story.main.complete`; Rebirth preserves class and subclass state. Ranged weapon metadata ships without Accuracy, whose hit-chance design remains unresolved. See [CLASS_WEAPON_ARCHITECTURE.md](CLASS_WEAPON_ARCHITECTURE.md).
- Explicit New Game overwrite confirmation
- Full gameplay/meta wipe on New Game with preferences preserved
- One canonical system for Save & Main Menu and Save & Quit
- Deterministic clean encounter-boundary restoration
- Restore encounter-start HP/mana, not exact live-frame combat
- Rebirth distinct from New Game
- Restart as current-combat-level restart, not disk load or fresh run

Do not add multiple slots or exact-frame serialization without a later approved design change.

## 16. UI and presentation direction

The visual identity is stylized layered paper/cut-paper/storybook. Paper Battle is intentional, and menus/HUD should feel native to that world.

Functional implementation and final visual ownership are distinct. Codex may create functional controls, sensible basic placement and component wiring. The user retains final custom art, animated hover/click/pressed states, precision alignment, bespoke sprite transitions and visual polish. Runtime-built placeholders do not lock final artwork or geometry.

Step 12.5 allows Inventory to coexist with either Stats or Enemy Inspection; Stats and Enemy Inspection are mutually exclusive. Gear tooltips show identity/base values, then an accented implicit line with a lock icon, then a divider and compact Prefix/Suffix lines with actual rolls, tier ranges, T numbers and local designation. They do not print literal LOCKED/UNLOCKED labels. The Pause Menu CODEX → MOD LIST browser reads live affix definitions, per-slot tiers and item-level gates from the same runtime catalog as item generation and crafting; its side filter describes normal explicit classification. Opening and backing through CODEX remain paused until Resume.

## 17. Environment and art direction

The paper/cut-paper identity is locked. Modular layered environment construction is approved. Corruption/progression may transform environment art while preserving that identity.

This document does not lock how many zones/backgrounds ship. The six current forest states are implementation content, not a promise that the final campaign contains exactly six environments.

## 18. Enemy build philosophy

Enemies may use generated equipment and builds. The existing bounded enemy build optimizer is intentional: it should evaluate real implemented offense/defense and retain archetype diversity rather than choosing arbitrary rolls.

Step 11 supplies centralized intrinsic scaling from each enemy prefab's level-1 Life seed: 4% Life and 3% outgoing damage growth through combat level 100, then 2%/1.5%; +5 flat Armour per level and +0.15 ordinary Fire/Cold/Lightning/Void resistance percentage points per level capped at +20 points. Step 13 retains this curve, separately caps enemy equipment tier access and applies an explicit 1.8× boss-role outgoing-damage multiplier; it does not alter authored level-one Life seeds or add a hidden boss Life curve. Step 12's synthetic reference-player curve remains Editor diagnostics only; Step 13 adds real progression-owned player Life and production-gear reference characters. These are playtest baselines, not final launch balance. Dead mechanics must not receive invented optimizer value merely to make their affixes look useful.

## 19. Approved future mechanics

- More shipping environments/enemies/bosses after a dedicated content contract
- Continued modular paper-layer environment production and corruption progression
- A possible active top-down/bullet-hell boss mode, discussed but not currently locked

An item in this section is not authorization to implement it during unrelated work.

## 20. Explicitly unresolved mechanics

Do not guess rules for:

- Accuracy/evasion formula, caps and interaction
- Block chance, mitigation and ordering
- Cooldown system and Cooldown Recovery
- Minion entities/behavior
- Status callback lifecycle/extensibility
- Deep Freeze's maximum-Chill-effect increase

The resolved Step 10 rules above supersede the removed items from this unresolved list. Remaining items belong to later design/content steps unless a specific authority assigns them otherwise.

## 21. V1 world structure

The V1 main progression is locked at six biomes × ten base locations per biome × six corruption states (0/20/40/60/80/100%) = 360 combat levels. Within a biome, levels are ordered in six ten-location corruption bands. Each combat level retains the nine-normal-plus-stage-10-boss loop. [WORLD_CONTENT_ARCHITECTURE.md](WORLD_CONTENT_ARCHITECTURE.md) is the authoritative mapping and authoring contract. Current all-world Goblin/Hobgoblin/forest reuse is explicitly placeholder content, not completion of the V1 roster or art set.

Combat levels above 360 use the final authored table as a safe endless fallback while retaining their numerical level; exact post-360 gameplay is unresolved. Enemy/boss rosters, whether seven skills are the final set, and active boss-mode classification remain explicit decisions.

## 21A. Step 14 progression rules

- Rebirth unlocks at authoritative combat zone 60. It preserves permanent relic state, resets the run, provisions exactly one starter through the New Game authority, then begins combat.
- Relic level is stored at creation as `clamp(1 + floor((zone - 60) * 99 / 300), 1, 100)`. Numerical tiers unlock T5/T4/T3/T2/T1 at relic levels 1/20/40/60/80.
- Higher level biases eligible item and relic tiers toward stronger results through one shared policy; lower tiers remain possible and T1 is not guaranteed.
- Baseline starters sit below the weakest reasonable natural level-one weapon. Active relics may intentionally override that early upgrade expectation with element, base-damage, item-level and one-time Legendary transformations.
- Starter element conflicts use strongest tier then active-slot order. Starter item-level and Legendary chance sources add and cap at 100; base-damage percentages add.

## 22. Design discrepancy register

## 21B. Step 14.5 attack and endgame-item contract

- Activating the equipped skill queues one replacement for the next scheduled player basic attack. Queuing never attacks, spends Mana, or changes the attack gauge. Resolution rechecks and spends Mana; if Mana became insufficient, the queue clears and that opportunity resolves as a basic attack. The current valid enemy is targeted at resolution, and clean encounter restore never restores a queued skill.
- Because a skill now consumes a basic-attack opportunity, final balance must make every skill materially stronger than a basic attack or equivalently useful. Shiv is a known review target. Step 14.5 intentionally preserves existing multi-hit, projectile, ailment, Shock, and Hit Twice semantics and changes no skill numbers.
- Every equipment item stores `OriginRarity`, current rarity, and current/maximum Crafting Potential. Natural Normal/Magic/Rare/Legendary maximums are 6/8/10/14; upgrades never raise the origin-derived maximum. Successful ordinary upgrades and rerolls cost 1, while Add and Remove cost 2. Invalid or failed work spends neither Potential nor currency, and ordinary crafting never changes an implicit.
- Equipment item level remains capped at 100. At combat levels 120/160/210/260/310/360, an item may hold 1/2/3/4/5/6 player-applied Empowered explicit modifiers. Only an ordinary, numeric, authored-empowerable T1 explicit is eligible. The temporary default range is 125% of both T1 endpoints, unless the family supplies a custom range. Empowered modifiers cannot be ordinarily rerolled or removed and Empowerment consumes no ordinary Potential.
- The Empowerment Catalyst is implemented as a persisted logical currency with a temporary first-party icon, but has no production drop source. Its intended source is optional post-100 challenge content.
- Boss-special affix pools have stable pool/content IDs, side, slot, range, progression, weight, and description data. The replacement service targets Legendary gear, replaces one non-Empowered explicit on the same side, preserves explicit count and implicit, and costs 3 Potential only on success. No production pool, catalyst, boss, or drop was added.
- Deep endgame must eventually offer an extremely rare, bounded way to repair or replace an implicit. Ordinary crafting must remain unable to do so; exact source and operation semantics are deliberately deferred.
- The levels 1–100 item journey is finding better bases/implicits/natural explicits and finitely repairing promising drops. Post-100 is refining ilvl-100 gear through Empowerment, optional challenge-boss specialization, and eventually rare implicit repair. A zone-360 aspiration is a player-built Legendary with six strong explicits, several/all Empowered, and legal boss-special affixes—not one astronomically perfect natural drop.
- Inventory overload remains a V1 UX requirement. Later work should extend highlighting, pickup filters, auto-dismantle, and Codex data with desired-modifier profiles, match scoring, visual ranking, and stronger criteria; Step 14.5 adds no replacement loot-filter UI.

## 21C. Step 15 encounter/content contract

- Combat level is the sole persisted main-world coordinate. Biome, base location, corruption tier, encounter table, and label are derived through `WorldProgression`; do not duplicate its arithmetic or add redundant save fields.
- Stable content IDs, never scene-object names, identify biomes, locations, encounter tables, enemy archetypes, bosses, and challenge content.
- Main progression tables provide weighted normal pools for stages 1–9 and one boss reference for stage 10. Definitions reserve future skill, modifier, reward, story, Codex, environment, and presentation hooks without implementing those systems prematurely.
- Optional challenge encounters are progression-independent records with unlock, entry resource, boss, reward resource, special-affix pool, and repeatability data. They do not enter the main 9+1 loop.
- Ten base visuals per biome may combine with six corruption presentations; 360 unique background files are neither required nor intended.

| System | Intended design | Current implementation | Status | Roadmap step |
|---|---|---|---|---|
| Shock | Five-stack threshold and 50%-basis secondary hit, with effect cap, overflow and five-turn lifetime. | Step 10 implemented and the fresh 172-test/synchronous regression passes. | Reconciled for v1; later balance is separate. | 10 |
| Chill | Cold-hit/max-Life slow curve capped at 30%, four-turn lifetime and replace-if-stronger. | Step 10 dynamically queries gauge slow, strength and replacement; fresh regression passes. | Reconciled for v1; Deep Freeze's cap tuning remains separate. | 10 |
| Accuracy/evasion | Deliberate hit/miss system only if later approved. | Legacy stats deserialize/display but leave new v1 pools/filter; combat still has no hit/miss roll. | Intentionally deprecated for v1. | 10 |
| Block | Deliberate defensive avoidance/mitigation only if later approved. | Legacy ID deserializes but leaves new v1 pools/filter; combat still has no block roll. | Intentionally deprecated for v1. | 10 |
| Maximum resistance | 75% baseline, matching/all additions and 90% hard cap before penetration. | Step 10 shared calculator includes Void and both actors; fresh regression passes. | Reconciled for v1. | 10 |
| Projectile Amount / Bullet Hell | Fireball gains one independent target-snapshotted projectile per whole passive addition. | Step 10 launcher consumes the count and keystone contribution; fresh regression passes. | Reconciled for v1. | 10 |
| Enemy Hit Twice | One extra legitimate, nonrecursive hit against the same living actor. | Step 10 symmetric enemy path and optimizer valuation; fresh regression passes. | Reconciled for v1. | 10 |
| Cooldowns | Attack Speed and Cooldown Reduction remain distinct. | Cooldown Reduction governs eligible independent Staff AutoCooldown skills; deprecated Cast Speed remains serialized only. | Implemented first pass; final balance remains TBD. | 18/V3 |

| Attributes, skill levels, kill resources | Use the locked Step 10 formulas and exactly-once death claim. | Step 10 derived projection, seven skill levels and kill recovery wired; fresh regression passes. | Reconciled for v1. | 10 |
| Minions | Require entity, ownership and combat design. | Only a Minion damage scope/stat calculation exists; there are no minions. | Infrastructure only. | 9–10/content TBD |
| Void / Poison | Void is a core element; Poison is Void DOT ailment with legacy enum compatibility. | Step 10 append-only stats 114–119 and direct/tick/gear/UI/optimizer mappings; fresh regression passes. | Reconciled for v1; final balance later. | 10 |
| Status callbacks | Extensible status lifecycle should have an approved dispatch contract if retained. | Empty virtual hooks exist and are not called by current status ticking. | Dead extension surface. | 9–10 |
| Enemy scaling | Enemies should scale intentionally across progression; exact balance is later. | Central profile applies authored-seed Life/outgoing-damage, Armour and ordinary elemental/Void resistance before independently generated equipment. | Structural Step 11 baseline; Step 13 balance remains. | 11, then 13 |
| Prestige (resolved) | Rebirth is the sole intended meta reset. | Step 8 removed the level-10 offer branch, placeholder continuation, public reset method and empty reward hook. Boss clears advance directly at every combat level. | Resolved; retain this historical row to prevent regression. | Completed in 8 |
| Dismantle rewards | Normal none; Magic one N→M fragment; Rare one M→R fragment; Legendary two M→R fragments; ten matching fragments make one orb. | Step 13 implements one authoritative manual/auto path, persistence and UI counts; schema 6 retains the fragment fields. | Implemented; historical stacked scrap conversion remains for old saves only. | Completed in 13 |

## Step 17 contract additions

Passive Tree V3 has six radial ten-tier class routes and six outward five-tier weapon routes. Every tier has two optional three-choice groups; the native class adds one selected-subclass fourth sibling per group. Choices are mutually exclusive, cross-class and weapon access require completed class spines, and refunds are dependency-safe. Weapon effects use a 1.60 specialization premium. Level 100 owns 100 points and respecs are free.

Six character slots are character-owned save containers; class, progression, inventory, relics, and passives do not cross slots. Ordinary preferences remain global. Schema 12 grants migrated characters a full Passive Tree V3 respec without changing non-passive progression.
# Step 18 locked contracts

- Every weapon binds exactly two production skills; Staff uses AutoCooldown and Dagger Quick Strike uses ImmediateCooldown.
- Cooldown Reduction (ID 126) replaces production Cast Speed; Cast Speed ID 120 remains deprecated, never reinterpreted.
- Freeze skips one enemy attack and stores Chill strength for Shatter.
- Each base class owns exactly two subclasses, but subclasses do not restrict weapon choice.
- The Subclass Sigil unlocks at `story.main.complete`; subclass respec is free outside combat and refunds allocated subclass fourth-choice nodes while preserving generic nodes.
- Transformations replace up to 10 connected allocated non-start/non-Keystone nodes.
- All Step 18 numbers are first-pass placeholders.

## Step 18.5 gameplay cleanup contract

- Ailment chance cannot bypass typed eligibility. Default bases are Physical Bleed, Fire Ignite, Lightning Shock, Cold Chill, and Physical/Void Poison. Only documented skill/subclass effects expand them.
- Every enemy grants one gear item. Further gear and currency quantity uses the centralized bounded LootPower formula documented in [LOOT_SYSTEM.md](LOOT_SYSTEM.md); these coefficients are first-pass placeholders.
- Enemy rarity is distinct from item rarity. Ancient currency is naturally obtainable only after combat level 60 and remains substantially rarer than ordinary currency. Empowerment Catalyst remains challenge-only.
- All six natural weapon types have equal selection weight and preserve distinct level-one damage, speed, and critical profiles. Starters remain separately weaker.

## Step 18.6 loot and player-damage contract

- World/enemy selection and enemy-owned builds may be deterministic, but every claimed enemy death rolls its entire reward event from fresh production entropy. Stage, encounter, enemy, run, and save identity never seed future loot.
- Equipped weapon type determines attribute coefficients independently of class. Sword uses Strength/Dexterity; Axe Strength; Bow Dexterity; Staff Intelligence; Dagger Dexterity/Intelligence; Sceptre Strength/Intelligence.
- Root weapon/skill hits gain 0.25% additive increased damage per player level above one, capped by the level-100 player cap. Attribute and level contributions do not change local item DPS or reapply to downstream derived damage.

## Step 20 endgame itemization contract

- Item level remains capped at 100. Post-100 progression improves selected T1 explicits, replaces ordinary explicits with compatible APEX affixes, and rarely repairs permanent implicits; it never introduces ilvl 101+.
- Challenge resources are character-slot owned and survive Rebirth. A key is spent only after a challenge encounter exists; failure never refunds it. Victory grants explicit Essence/Catalyst rewards outside ordinary currency selection.
- Boss Infusion requires Legendary gear, a selected ordinary non-Empowered explicit, one matching Essence, and 3 Potential. It preserves side/capacity. APEX modifiers cannot enter ordinary crafting or Empowerment.
- Implicit Reforge requires ilvl100 Rare/Legendary gear, costs one rare Reforger and zero Potential, and changes only the permanent implicit.
- All values are FIRST-PASS PLACEHOLDER VALUES. Reward/craft RNG uses fresh entropy, never deterministic world/stage seeds.
