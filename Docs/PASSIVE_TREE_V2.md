# Passive Tree V2

## Production contract

The shared V2 tree contains 366 deterministic nodes: six 44-node class sectors, six 10-node bridge regions, and a 42-node center. The selected class supplies the only free allocation origin. Warrior is upper, Ranger upper-right, Thief lower-right, Mage lower, Priest lower-left, and Barbarian upper-left. Other class anchors remain visible landmarks but are not alternate free roots.

Level progression owns exactly one point per player level, including one point at level 1 and 100 total at level 100. Class anchors cost no points. Refunds cost no currency, may not strand an allocated node, and Refund All returns every spent point without changing class, subclass, gear, level, or other progression.

## Geography and authoring

Main travel nodes are primarily +5 Strength/Dexterity/Intelligence or +3/+3 hybrid attributes. Each sector also exposes modest local life, mana, critical chance, attack speed, recovery, armour, and Hit Twice access. Compact branches use three small nodes followed by a notable; notables use stronger multi-stat packages. Sector and bridge density—not class locks—creates the intended themes and ailment geography.

Weapon districts use the centralized `1.60` narrowness multiplier. Sword emphasizes tempo/critical/sustain, Axe physical Rage/retention/Bleed/recovery, Bow Precision/projectile speed/additional projectiles, Dagger speed/Poison/Void/on-hit, Staff Cooldown Reduction/mana/elemental sustain, and Sceptre hybrid recovery/elemental defense. Weapon-restricted effects are projected only while their weapon type is equipped.

Six gameplay-changing keystones ship in the first pass: Titanic Blows, Living Current, Venomous Transmutation, Infernal Conversion, Mana Shield, and Rage Finisher. Retired V1 enum identities remain source-compatible but do not appear in V2 data.

`PassiveTreeDefinition` is the authority for stable IDs, explicit Cartesian positions, effects, regions, edges, weapon restrictions, and extension metadata. Every node carries a stable transformation slot and section identity; Step 18's `SubclassTransformationProfile` supplies production replacement effects for eligible connected allocations. Structured generation is deterministic, while the node DTO remains individually overrideable.

Schema 9 performs the one-time V1-to-V2 respec: it preserves class, player level, equipment, relics, currencies, and non-passive progression; clears old allocations; and refunds the full level-owned point total. New IDs use `tree.v2.*` namespaces and old numerical node identities are not repurposed.

## Review and validation

Run `Step17TestRunner.RunValidation` or **Black Cube → Validation → Write Passive Tree V2 Report**. The report is [PassiveTreeV2LayoutReport.md](../ReviewCaptures/PassiveTreeV2LayoutReport.md), and the generated overview is [PassiveTreeV2Overview.png](../ReviewCaptures/PassiveTreeV2Overview.png). Validation reports sector radii/counts, starts, districts/clusters, keystones, IDs, invalid/duplicate/one-way edges, disconnected/unreachable nodes, and overlaps.
# Step 18 targeted patch

The graph remains 366 nodes. Former Cast Speed content now emits Cooldown Reduction. Shock/Chill clusters include effectiveness, all six starts receive small Aura Effect secondaries, and Priest/Sceptre nodes carry the highest Aura Effect density. `SubclassTransformationProfile` classifies existing nodes and supplies replacement effects; ordinary macro geometry and stable node IDs are unchanged.
