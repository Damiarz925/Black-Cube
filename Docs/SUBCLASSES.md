# Production Subclasses (Step 18)

All numerical values are **first-pass placeholders**, not final balance. The production catalog contains exactly two subclasses per base class. Unlock is the persistent `story.main.complete` Subclass Sigil entitlement. Class is permanent; subclass changes are free outside active combat, require confirmation, and clear transformations while preserving ordinary allocations.

| Class | Stable ID | Core identity |
|---|---|---|
| Warrior | `subclass.warrior.bleed` | +20pp Bleed chance, +40% Bleed damage, 15% Rupture of remaining active Bleeds. |
| Warrior | `subclass.warrior.multihit` | +10% Attack Speed, +10pp Hit Twice; 5% more per prior hit before a successful enemy attack, cap 10. |
| Barbarian | `subclass.barbarian.big_hit` | 25% less Attack Speed, 60% more Physical hit damage, 75% full-Life target and 35% injured bonuses. |
| Barbarian | `subclass.barbarian.fire` | Adds Fire equal to 40% base Physical; 20% chance for a non-recursive 75% Fire Eruption. |
| Ranger | `subclass.ranger.poison` | All damage can Poison; +20pp chance, +40% damage, +25% duration and speed. |
| Ranger | `subclass.ranger.projectile` | +1 projectile, +20% speed, +15pp Precision; Volley/Focused toggle, with 75% more per sacrificed projectile. |
| Mage | `subclass.mage.cooldown` | +20% CDR; 20% cooldown bypass or queued repeat, with same-frame recursion protection. |
| Mage | `subclass.mage.storm` | All damage can Shock, +50% effectiveness, up to three multiplicatively combined Shock instances. |
| Priest | `subclass.priest.dark` | Non-regeneration healing becomes triggerless Void damage; Void can apply all ailments; 0.20% more Void per corruption point. |
| Priest | `subclass.priest.light` | Cannot deal Void; direct damage heals 10%; enemy regeneration halved; four damage-built auras. |
| Thief | `subclass.thief.assassin` | First Strike, 50% opener damage, +10pp Crit, +50% Crit multiplier, 10% non-boss execution. |
| Thief | `subclass.thief.ailment_crit` | Excess Crit multiplier additionally scales damaging ailments; Poison/Bleed/Ignite roll Critical Ailment once on creation. |

## Passive transformations

The cap is 10. Only allocated travel/small/notable nodes are eligible. The first may be any eligible node; later nodes must touch the transformed group. Start nodes and Keystones are invalid. A transformation replaces the original effects; it never stacks with them. Notables receive a 2.5× transformed numerical budget. Removing transformations is free; subclass change clears all transformations; Refund All is safe. Schema 10 persists stable transformed node IDs and the Ranger projectile mode, but never transient combat counters.

`SubclassTransformationProfile` maps passive themes to replacement effects without a 366 × 12 table. `SubclassEffectCatalog` gives every core bonus a stable, source-agnostic ID, while `SubclassCombatState.GrantExternalEffect` / `RevokeExternalEffect` prove that an effect can be sourced independently of the selected subclass. This is the extension point for a future item that grants one foreign subclass bonus. Step 18 deliberately does **not** implement that currency, affix, or roll table.

## Light Priest auras

Physical, Fire, Cold, and Lightning contribution is tracked separately against the current enemy. `intensity = clamp01((typedDamage / enemyMaxLife) / 0.10)`. At full intensity every aura grants +20% global damage. Secondary values are Physical +10% Attack Speed, Fire +25% Ignite duration, Cold +20% Chill effectiveness, and Lightning +20% Shock effectiveness. Both components scale by `1 + AuraEffect`. There is no Void aura, and contribution resets with the target.
