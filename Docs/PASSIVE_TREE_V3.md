# Passive Tree V3

Passive Tree V3 is the only production passive topology. `PassiveTreeDefinition` deterministically generates six class routes and their six signature-weapon continuations from stable `tree.v3.*` IDs. The central hub is visual only and costs no point.

## Class routes

The radial order is Warrior 90°, Ranger 30°, Thief -30°, Mage -90°, Priest -150°, and Barbarian 150°. Every route has ten sequential spine tiers. A spine requires only the prior spine; the two side groups at each tier are optional and never gate travel. Each side group contains three generic siblings and permits at most one allocation. Thus a class route costs 10 points to traverse and accepts at most 30 points.

The native class renders a fourth, purple subclass sibling in every side group. It remains locked until the story entitlement and subclass selection exist, then derives its effect only from the selected subclass. Other class routes hide this slot and render the symmetric three-choice template. Changing subclass refunds allocated fourth-choice nodes and leaves generic allocations intact.

At creation only native Tier 1 is available. Completing all ten native spine nodes unlocks Tier 1 of every other class. Native-spine refunds are blocked while off-class allocations depend on that unlock.

## Weapon routes

Warrior→Sword, Ranger→Bow, Thief→Dagger, Mage→Staff, Priest→Sceptre, and Barbarian→Two-Handed Axe. Each route continues outward from class Tier 10 with five spine tiers and two optional three-choice groups per tier. It costs 5 points to traverse and accepts at most 15 points. Completing the associated class spine unlocks its weapon route, including off-class routes. Class-spine refunds are blocked while that weapon route contains allocations.

Weapon nodes use the centralized 1.60 specialization premium. They may be allocated without equipping the weapon, but their modifiers are applied only while the matching stable weapon ID is equipped. The existing Axe Rage Finisher remains the final Axe-route specialized option.

## Points, IDs, and migration

One passive point is owned per player level: one at level 1 and exactly 100 at level 100. Allocation and refund cost one point and respecs are free. Schema 12 migrates schema 11 by clearing all V2 ranks and retired transformed-node state and setting available points to the clamped player level. Class, selected subclass, story state, equipment, inventory, currencies, relics, Rebirth state, and endgame state are preserved.

V2 graph, bridge, central-region, transformation-mode, transformation metadata, and transformed-effect behavior are not runtime fallbacks. Old IDs are recognized only by schema migration. `PassiveTreeV3Validation` validates route counts, IDs, groups, restrictions, and edges; focused EditMode tests validate starts, optional/exclusive choices, unlocks, dependency-safe refunds, subclass behavior, points, and migration.

## First-pass content

- Warrior: balanced Strength/Dexterity, Life, Armour, Physical, Hit Twice, Bleed, sustain, Crit, and speed.
- Barbarian: Life, heavy Physical hits, Rage, Bleed, Fire/Ignite, recovery, and slow-hit scaling.
- Ranger: Dexterity, projectiles, Precision, speed, Crit, Poison, Lightning/Shock, and projectile count.
- Thief: Dexterity/Intelligence, Crit, speed, cooldowns, multi-hit, Poison/Bleed, Void, and on-hit recovery.
- Mage: Intelligence, Mana, Cooldown Reduction, elemental/Void damage, ailments, and Crit.
- Priest: Strength/Intelligence, Life/Mana sustain, Aura Effect, defenses, Cold/Chill, and hybrid elemental offense.
- Sword is versatile; Axe emphasizes heavy hits/Rage; Bow emphasizes Precision/projectiles; Dagger emphasizes Crit/ailments; Staff emphasizes cooldown/Mana/elemental cadence; Sceptre emphasizes aura/sustain/defensive elemental play.

Values are centralized first-pass tuning, not final balance.
