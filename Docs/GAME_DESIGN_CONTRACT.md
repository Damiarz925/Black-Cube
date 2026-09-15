# Black-Cube game design contract

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
- Active skills layered over automatic attacks
- Rebirth and relic-based meta progression
- A stylized layered paper/cut-paper/storybook presentation

Basic attacks continue automatically without active input. Active skills supplement that loop rather than replace it. A more directly controlled top-down or bullet-hell boss mode has been discussed, but it is not locked for the current core or v1 scope.

## 3. Core gameplay loop

Start or resume one run, automatically fight repeated encounters, earn XP/items/currency, refine equipment and passive/skill choices, defeat bosses to advance combat levels, and eventually Rebirth to exchange run progress for persistent-within-save relic progression. Death restarts the current combat level without erasing the build. Save/load returns to a clean deterministic encounter boundary.

Moment-to-moment input should focus on build decisions, inspection, crafting, active-skill timing and navigation—not manually issuing every basic attack.

## 4. Combat model

- Combat is primarily automatic and gauge/attack-speed driven.
- The current cadence is nine normal encounters followed by a boss as stage 10.
- Boss defeat advances the combat level and begins the next level's normal sequence.
- Player and enemy damage use the same broad stat/damage-context language, while their exact sources may differ.
- Active skills may convert damage, add hits/projectiles, specialize ailments, and consume mana.
- The total campaign length and shipping counts of zones, enemies and bosses are deliberately not locked here.

## 5. Damage types

The current primary ecosystem is Physical, Fire, Cold, Lightning, Poison, Bleed and Ignite/Burn. Physical and elemental hits, DOT ailments, critical scaling, armour, resistances and penetration are established concepts.

Void exists in implementation data but has no approved final gameplay identity. Do not infer its damage, defense, ailment, conversion or thematic rules.

## 6. Ailments

- **Poison:** damage-over-time ailment sourced from eligible Physical/Poison damage.
- **Bleed:** damage-over-time ailment sourced from Physical damage.
- **Ignite/Burn:** damage-over-time ailment sourced from Fire damage.
- **Shock intended identity:** Shock accumulates toward a threshold/stack requirement. Reaching it triggers an additional Lightning hit or effect. It is not a generic increased-damage-taken debuff. Preserve any already-approved threshold details; where none are explicit, leave threshold, duration interaction, damage basis and stack consumption unresolved.
- **Chill intended identity:** Chill reduces target attack speed. Reduction scales with the size/strength of the Cold hit that caused it. Exact curve, cap, stacking and duration interaction remain unresolved until approved.

The current Shock/Chill implementation gaps are registered below; do not redefine intent to match the placeholders.

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

## 8. Mana and resources

Active skills consume mana; unavailable mana prevents casting. Mana regenerates and may be modified by implemented passive/resource rules. Basic auto-attacks do not require skill activation. A general cooldown system is not approved merely because `CooldownRecovery` exists.

Life and mana are run resources. Death/restart and encounter-boundary persistence follow the lifecycle/save contracts rather than creating a new run.

## 9. Player progression

- Player level cap is 100.
- Leveling grants passive progression through the current point system.
- Rebirth becomes available at level 50 or above.
- **New Game:** completely clears gameplay and relic/rebirth meta progression, including Ancient currency, while preserving independent preferences.
- **Rebirth:** resets the established run layer, preserves relic history, advances the relic cycle and creates/manages the new-cycle relic.
- **Restart after death:** restarts the current combat level and preserves the build/meta state defined in [RUNTIME_LIFECYCLE.md](RUNTIME_LIFECYCLE.md).

New Game, Rebirth and Restart are three distinct operations and must never share ambiguous UI wording or reset behavior.

## 10. Passive tree

[PASSIVE_TREE.md](PASSIVE_TREE.md) is the detailed authority. The current approved structure is one central player root, ten primary stat spokes, ten connecting/specialized bridges, a forty-node outer travel ring, ten inner keystones and ten outer keystones: 290 binary allocatable nodes and 20 keystones total.

Stable IDs, adjacency, refund safety, stat bindings, keystone effects and art rules belong in that specification rather than being duplicated here. Currently documented placeholders such as Bullet Hell projectile spawning and Deep Freeze's unbalanced maximum-effect increase remain discrepancies/unresolved work, not permission to invent values.

## 11. Items and affixes

Eight equipment slots are authoritative: Weapon, Helmet, Body Armour, Gloves, Boots, Amulet, Ring and Belt.

Rarities are Normal, Magic, Rare and Legendary. Current approved modifier counts are Normal 1, Magic 2, Rare 3–4, and Legendary 5–6. Guaranteed weapon base damage/attack-speed/critical rolls are intrinsic and do not count against random crafting-affix limits. One original non-intrinsic modifier is locked; crafting may mutate only unlocked modifiers.

Modifier tiers are item-level gated. Weapon-element matching damage rolls may be local to the weapon; other equipment rolls project globally. Duplicate stat/group exclusions and weighted definitions are part of the current affix model.

[STAT_AFFIX_AUDIT.md](STAT_AFFIX_AUDIT.md) is the Step 9 implementation inventory and proposed v1 triage: 114 stable IDs, 97 actually generatable definitions, eleven pool-listed zero-tier records, and six internal/non-rollable stats. Its recommendations do not approve unresolved mechanics; the user-decision table must be resolved before Step 10 changes roll pools or formulas.

Prefix/suffix separation is not implemented or approved as a current rule. It remains a possible future itemization layer.

## 12. Crafting

The six ordinary equipment operations are:

1. Normal → Magic
2. Magic → Rare
3. Reroll Magic modifier
4. Add Rare/Legendary modifier
5. Reroll Rare/Legendary modifier
6. Remove Rare/Legendary modifier

The six Ancient relic counterparts are Ancient Normal → Magic, Ancient Magic → Rare, Ancient Rare → Legendary, Ancient Reroll, Ancient Add Modifier and Ancient Remove Modifier. Ancient operations apply only to the eligible current-cycle relic. Successful random outcomes and currency consumption form one immediate save transaction.

## 13. Pickup filters and dismantling

Items rejected by the modifier pickup filter intentionally auto-dismantle. This is approved behavior and must not be redesigned as an accidental discard bug.

Current full-orb dismantle rewards are implementation reality, not the approved future fragment model. The future mechanic is:

- Normal dismantles yield no fragments.
- Magic dismantles yield one fragment toward Normal → Magic currency.
- Ten matching fragments combine into one full orb.
- Rare dismantles yield the analogous fragment toward Magic → Rare currency.

Fragment integration applies to manual and filter-driven dismantling when eventually implemented. It is not part of Step 7.

## 14. Rebirth and relics

Rebirth is the intended meta-progression reset system and becomes available at level 50+. It resets run progression according to the established contract, retains permanent-within-save relic history, advances the cycle, creates one new current-cycle relic, restricts Ancient crafting to current-cycle authority, and supports four active relic slots.

New Game clears relic history, active slots, cycle/rebirth history and Ancient currency. Rebirth is the only run-reset/meta-progression system; the superseded Prestige runtime path was removed in Step 8 and must not be reintroduced as a competing design.

## 15. Save, load and restart semantics

[SAVE_STATE_CONTRACT.md](SAVE_STATE_CONTRACT.md) and [RUNTIME_LIFECYCLE.md](RUNTIME_LIFECYCLE.md) are authoritative. Locked concepts are:

- One logical current-run save with primary plus backup
- Schema 2 and validated atomic writes
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

## 17. Environment and art direction

The paper/cut-paper identity is locked. Modular layered environment construction is approved. Corruption/progression may transform environment art while preserving that identity.

This document does not lock how many zones/backgrounds ship. The six current forest states are implementation content, not a promise that the final campaign contains exactly six environments.

## 18. Enemy build philosophy

Enemies may use generated equipment and builds. The existing bounded enemy build optimizer is intentional: it should evaluate real implemented offense/defense and retain archetype diversity rather than choosing arbitrary rolls.

Current intrinsic/base enemy scaling is not final design. Step 11 owns base scaling; Step 13 owns balance. Dead mechanics must not receive invented optimizer value merely to make their affixes look useful.

## 19. Approved future mechanics

- Dismantle fragments and ten-fragment orb combination as specified above
- More shipping environments/enemies/bosses after a dedicated content contract
- Continued modular paper-layer environment production and corruption progression
- Potential prefix/suffix itemization, only after explicit approval
- A possible active top-down/bullet-hell boss mode, discussed but not currently locked

An item in this section is not authorization to implement it during unrelated work.

## 20. Explicitly unresolved mechanics

Do not guess rules for:

- Accuracy/evasion formula, caps and interaction
- Block chance, mitigation and ordering
- Maximum-resistance cap behavior
- Exact general Shock threshold, trigger hit, stack consumption, duration and scaling
- Exact Chill hit-strength curve, stacking and cap
- Enemy Hit Twice
- General Projectile Amount behavior and projectile-spawn semantics
- Cooldown system and Cooldown Recovery
- Attributes and attribute-derived scaling
- Skill-level affix behavior
- Life/Mana on Kill behavior and ordering
- Minion entities/behavior
- Void identity
- Status callback lifecycle/extensibility
- Deep Freeze's maximum-Chill-effect increase

These belong to Steps 9–10 unless a more specific approved roadmap document assigns them elsewhere.

## 21. Content scope intentionally deferred

Do not lock total v1 combat levels, zones, backgrounds, normal enemies, bosses, encounter variations, campaign ending, final boss-mode structure or post-campaign loop here. Step 14 owns the v1 content contract. Balance values beyond explicitly recorded current skill/item rules belong to Step 13.

## 22. Design discrepancy register

| System | Intended design | Current implementation | Status | Roadmap step |
|---|---|---|---|---|
| Shock | Build stacks toward a threshold that triggers an additional Lightning hit/effect; not generic increased damage taken. | Generic Shock is stored/displayed but has no triggered effect. Lightning Strike directly converts Shock Chance applications into immediate extra hits, bypassing a persisted target threshold. | Discrepancy; exact threshold details unresolved. | 9–10 |
| Chill | Reduce target attack speed in proportion to causing Cold-hit strength. | Chill can apply, stack, expire and display; `ApplyChill` is empty and attack speed is unchanged. | Missing mechanic. | 9–10 |
| Accuracy/evasion | Deliberate hit/miss system, if approved. | Rollable/displayable stats exist; combat always proceeds without an accuracy/evasion roll. | Unresolved/dead affixes. | 9–10 |
| Block | Deliberate defensive avoidance/mitigation rule, if approved. | `ChanceToBlock` exists but is never consumed by damage resolution. | Unresolved/dead affix. | 9–10 |
| Maximum resistance | Approved maximum-cap rules should control achievable resistance bounds. | Max-resistance stats are unconsumed; calculator clamps final reduction to ±90%. | Unresolved/dead affixes. | 9–10 |
| Projectile Amount / Bullet Hell | Additional projectile count should affect projectile-producing skills once exact behavior is approved. | Passive/stat/keystone values exist; projectile launcher creates one projectile. | Partial projection, missing consumer. | 9–10 |
| Enemy Hit Twice | Enemy behavior requires an explicit decision. | Player attacks consume Hit Twice; enemy turns do not. | Unresolved asymmetry/dead enemy roll. | 9–10 |
| Cooldowns | No final cooldown model is yet approved. | Cooldown Recovery is pool-listed/displayable but has zero tiers and cannot generate; skills have no cooldown timer. | Unresolved/dead pool exposure. | 9–10 |
| Attributes, skill levels, kill resources | Require explicit formulas and ordering. | Attribute and kill-resource affixes generate but have no consumers. Skill-level entries are pool-listed/displayable but zero-tier and cannot generate. | Dead/unreachable affix families. | 9–10 |
| Minions | Require entity, ownership and combat design. | Only a Minion damage scope/stat calculation exists; there are no minions. | Infrastructure only. | 9–10/content TBD |
| Void | Final identity must be explicitly approved. | Enum/mask/tooltip support exists; normal player weapon generation excludes Void and no gameplay rule is defined. | Unresolved. | 9–10 |
| Status callbacks | Extensible status lifecycle should have an approved dispatch contract if retained. | Empty virtual hooks exist and are not called by current status ticking. | Dead extension surface. | 9–10 |
| Enemy scaling | Enemies should scale intentionally across progression; exact balance is later. | Gear level/count grows, but prefab base life/damage has no level multiplier. | Partial system. | 11, then 13 |
| Prestige (resolved) | Rebirth is the sole intended meta reset. | Step 8 removed the level-10 offer branch, placeholder continuation, public reset method and empty reward hook. Boss clears advance directly at every combat level. | Resolved; retain this historical row to prevent regression. | Completed in 8 |
| Dismantle rewards | Future rarity fragments: Normal none, Magic one N→M fragment, Rare analogous M→R fragment. | Dismantling currently grants 1/2/3/5 random full ordinary currencies by rarity. | Approved future replacement, not a Step 7 bug fix. | Future crafting step |
