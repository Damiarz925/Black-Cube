# Current Black-Cube passive tree reference

Snapshot: 2026-09-27. Extracted from the production Passive Tree V3 class/weapon Branch ScriptableObjects. Values rounded to four decimals to suppress serialization noise.

Each node costs one point. Allocate a tier spine before its choices. Each side permits at most one choice; the two sides are independent. Completing the native ten-node class spine opens other class routes. Completing a class spine opens its signature weapon route. Subclass choices require the selected, unlocked native subclass. Weapon effects only apply while that weapon type is equipped.

Within each generic-choice cell, A / B / C are alternatives, not a package. Subclass effects appear as an additional alternative on BOTH sides. Only the selected subclass variant is available. Class routes have 10 tiers; weapon routes have 5. Fully investing a class route costs 30 points; a weapon route costs 15.

Regeneration, on-hit/on-kill recovery, attributes, and projectile amount are flat values; percentages are explicitly marked. Increased Critical Chance scales base critical chance rather than adding base-crit percentage points. Fractional additional projectile amounts are pooled and floored by the projectile-count calculation.

Authored nodes labeled Magic / Sceptre Magic grant Aura Effect. Authored Defense nodes grant increased Armour, not direct physical damage reduction. Current cooldown nodes grant Cooldown Reduction, not Cast Speed.

## Barbarian

Source: D:\Unity\Projects\Black-Cube\Assets\GameData\PassiveTree\Branches\Class\SO_Barbarian_Branch.asset

| Tier | Mainline/spine | Left A / B / C | Right A / B / C | Subclass A option (either side) | Subclass B option (either side) |
|---|---|---|---|---|---|
| 1 | Strength +5 | Maximum Life +9% / Increased Armour +9% / Life Regeneration +3.6 | Physical Damage +9% / Critical Multiplier +14.4% / Reduced Rage Decay +9% | Titan Barbarian: Critical Multiplier +14.4% | Fire Barbarian: Ignite Chance +9% |
| 2 | Maximum Life +5.125% | Rage Generation +9.2% / Rage Effect +9.2% / Bleed Chance +9.2% | Fire Damage +9.2% / Increased Critical Chance +9.2% / Life On Hit +3.68 | Titan Barbarian: Maximum Life +9.2% | Fire Barbarian: Rage Effect +9.2% |
| 3 | Physical Damage +5.25% | Maximum Life +9.4% / Increased Armour +9.4% / Life Regeneration +3.76 | Physical Damage +9.4% / Critical Multiplier +15.04% / Reduced Rage Decay +9.4% | Titan Barbarian: Physical Damage +9.4% | Fire Barbarian: Fire Damage +9.4% |
| 4 | Rage Generation +5.375% | Rage Generation +9.6% / Rage Effect +9.6% / Bleed Chance +9.6% | Fire Damage +9.6% / Increased Critical Chance +9.6% / Life On Hit +3.84 | Titan Barbarian: Critical Multiplier +15.36% | Fire Barbarian: Ignite Chance +9.6% |
| 5 | Critical Multiplier +8.8%; More Damage +5% | Maximum Life +9.8% / Increased Armour +9.8% / Life Regeneration +3.92 | Physical Damage +9.8% / Critical Multiplier +15.68% / Reduced Rage Decay +9.8% | Titan Barbarian: Maximum Life +9.8% | Fire Barbarian: Rage Effect +9.8% |
| 6 | Life Regeneration +2.25 | Rage Generation +10% / Rage Effect +10% / Bleed Chance +10% | Fire Damage +10% / Increased Critical Chance +10% / Life On Hit +4 | Titan Barbarian: Physical Damage +10% | Fire Barbarian: Fire Damage +10% |
| 7 | Bleed Chance +5.75%; Bleed Dmg +5% | Maximum Life +10.2% / Increased Armour +10.2% / Life Regeneration +4.08 | Physical Damage +10.2% / Critical Multiplier +16.32% / Reduced Rage Decay +10.2% | Titan Barbarian: Critical Multiplier +16.32% | Fire Barbarian: Ignite Chance +10.2% |
| 8 | Fire Damage +5.875%; Ignite Dmg +5% | Rage Generation +10.4% / Rage Effect +10.4% / Bleed Chance +10.4% | Fire Damage +10.4% / Increased Critical Chance +10.4% / Life On Hit +4.16 | Titan Barbarian: Maximum Life +10.4% | Fire Barbarian: Rage Effect +10.4% |
| 9 | Rage Effect +6% | Maximum Life +10.6% / Increased Armour +10.6% / Life Regeneration +4.24 | Physical Damage +10.6% / Critical Multiplier +16.96% / Reduced Rage Decay +10.6% | Titan Barbarian: Physical Damage +10.6% | Fire Barbarian: Fire Damage +10.6% |
| 10 | Increased Armour +6.125% | Rage Generation +10.8% / Rage Effect +10.8% / Bleed Chance +10.8% | Fire Damage +10.8% / Increased Critical Chance +10.8% / Life On Hit +4.32 | Titan Barbarian: Critical Multiplier +17.28% | Fire Barbarian: Ignite Chance +10.8% |

## Mage

Source: D:\Unity\Projects\Black-Cube\Assets\GameData\PassiveTree\Branches\Class\SO_Mage_Branch.asset

| Tier | Mainline/spine | Left A / B / C | Right A / B / C | Subclass A option (either side) | Subclass B option (either side) |
|---|---|---|---|---|---|
| 1 | Intelligence +5 | Maximum Mana +9% / Mana Regeneration +3.6 / Mana On Hit +3.6 | Fire Damage +9% / Cold Damage +9% / Lightning Damage +9% | Chrono Mage: Mana Regeneration +3.6 | Storm Mage: Lightning Damage +9% |
| 2 | Maximum Mana +5.125% | Shock Chance +9.2% / Fire Damage +9.2% / Increased Critical Chance +9.2% | Cooldown Reduction +9.2% / Critical Multiplier +14.72% / Increased Armour +9.2% | Chrono Mage: Mana On Hit +3.68 | Storm Mage: Increased Critical Chance +9.2% |
| 3 | Cooldown Reduction +5.25% | Maximum Mana +9.4% / Mana Regeneration +3.76 / Mana On Hit +3.76 | Fire Damage +9.4% / Cold Damage +9.4% / Lightning Damage +9.4% | Chrono Mage: Cooldown Reduction +9.4% | Storm Mage: Shock Chance +9.4% |
| 4 | Mana Regeneration +2.15 | Shock Chance +9.6% / Fire Damage +9.6% / Increased Critical Chance +9.6% | Cooldown Reduction +9.6% / Critical Multiplier +15.36% / Increased Armour +9.6% | Chrono Mage: Mana Regeneration +3.84 | Storm Mage: Lightning Damage +9.6% |
| 5 | Aura Effect +5.5% | Maximum Mana +9.8% / Mana Regeneration +3.92 / Mana On Hit +3.92 | Fire Damage +9.8% / Cold Damage +9.8% / Lightning Damage +9.8% | Chrono Mage: Mana On Hit +3.92 | Storm Mage: Increased Critical Chance +9.8% |
| 6 | Shock Chance +5.625%; Shock Effect +5% | Shock Chance +10% / Fire Damage +10% / Increased Critical Chance +10% | Cooldown Reduction +10% / Critical Multiplier +16% / Increased Armour +10% | Chrono Mage: Cooldown Reduction +10% | Storm Mage: Shock Chance +10% |
| 7 | Chill Chance +5.75%; Chill Effect +5% | Maximum Mana +10.2% / Mana Regeneration +4.08 / Mana On Hit +4.08 | Fire Damage +10.2% / Cold Damage +10.2% / Lightning Damage +10.2% | Chrono Mage: Mana Regeneration +4.08 | Storm Mage: Lightning Damage +10.2% |
| 8 | Increased Critical Chance +5.875% | Shock Chance +10.4% / Fire Damage +10.4% / Increased Critical Chance +10.4% | Cooldown Reduction +10.4% / Critical Multiplier +16.64% / Increased Armour +10.4% | Chrono Mage: Mana On Hit +4.16 | Storm Mage: Increased Critical Chance +10.4% |
| 9 | Void Damage +6% | Maximum Mana +10.6% / Mana Regeneration +4.24 / Mana On Hit +4.24 | Fire Damage +10.6% / Cold Damage +10.6% / Lightning Damage +10.6% | Chrono Mage: Cooldown Reduction +10.6% | Storm Mage: Shock Chance +10.6% |
| 10 | Cooldown Reduction +6.125% | Shock Chance +10.8% / Fire Damage +10.8% / Increased Critical Chance +10.8% | Cooldown Reduction +10.8% / Critical Multiplier +17.28% / Increased Armour +10.8% | Chrono Mage: Mana Regeneration +4.32 | Storm Mage: Lightning Damage +10.8% |

## Priest

Source: D:\Unity\Projects\Black-Cube\Assets\GameData\PassiveTree\Branches\Class\SO_Priest_Branch.asset

| Tier | Mainline/spine | Left A / B / C | Right A / B / C | Subclass A option (either side) | Subclass B option (either side) |
|---|---|---|---|---|---|
| 1 | Strength +5; Intelligence +4 | Maximum Life +9% / Life Regeneration +3.6 / Life On Hit +3.6 | Maximum Mana +9% / Mana Regeneration +3.6 / Mana On Hit +3.6 | Dark Priest: Poison Chance +9% | Light Priest: Life On Hit +3.6 |
| 2 | Maximum Life +5.125% | Increased Armour +9.2% / Cold Damage +9.2% / Chill Chance +9.2% | Aura Effect +9.2% / Cooldown Reduction +9.2% / Lightning Damage +9.2% | Dark Priest: Increased Critical Chance +9.2% | Light Priest: Increased Armour +9.2% |
| 3 | Maximum Mana +5.25% | Maximum Life +9.4% / Life Regeneration +3.76 / Life On Hit +3.76 | Maximum Mana +9.4% / Mana Regeneration +3.76 / Mana On Hit +3.76 | Dark Priest: Void Damage +9.4% | Light Priest: Aura Effect +9.4% |
| 4 | Life Regeneration +2.15 | Increased Armour +9.6% / Cold Damage +9.6% / Chill Chance +9.6% | Aura Effect +9.6% / Cooldown Reduction +9.6% / Lightning Damage +9.6% | Dark Priest: Poison Chance +9.6% | Light Priest: Life On Hit +3.84 |
| 5 | Mana Regeneration +2.2 | Maximum Life +9.8% / Life Regeneration +3.92 / Life On Hit +3.92 | Maximum Mana +9.8% / Mana Regeneration +3.92 / Mana On Hit +3.92 | Dark Priest: Increased Critical Chance +9.8% | Light Priest: Increased Armour +9.8% |
| 6 | Aura Effect +5.625%; All Elemental Resistance +5% | Increased Armour +10% / Cold Damage +10% / Chill Chance +10% | Aura Effect +10% / Cooldown Reduction +10% / Lightning Damage +10% | Dark Priest: Void Damage +10% | Light Priest: Aura Effect +10% |
| 7 | Increased Armour +5.75% | Maximum Life +10.2% / Life Regeneration +4.08 / Life On Hit +4.08 | Maximum Mana +10.2% / Mana Regeneration +4.08 / Mana On Hit +4.08 | Dark Priest: Poison Chance +10.2% | Light Priest: Life On Hit +4.08 |
| 8 | Chill Chance +5.875%; Chill Effect +5% | Increased Armour +10.4% / Cold Damage +10.4% / Chill Chance +10.4% | Aura Effect +10.4% / Cooldown Reduction +10.4% / Lightning Damage +10.4% | Dark Priest: Increased Critical Chance +10.4% | Light Priest: Increased Armour +10.4% |
| 9 | Void Damage +6% | Maximum Life +10.6% / Life Regeneration +4.24 / Life On Hit +4.24 | Maximum Mana +10.6% / Mana Regeneration +4.24 / Mana On Hit +4.24 | Dark Priest: Void Damage +10.6% | Light Priest: Aura Effect +10.6% |
| 10 | Aura Effect +6.125% | Increased Armour +10.8% / Cold Damage +10.8% / Chill Chance +10.8% | Aura Effect +10.8% / Cooldown Reduction +10.8% / Lightning Damage +10.8% | Dark Priest: Poison Chance +10.8% | Light Priest: Life On Hit +4.32 |

## Ranger

Source: D:\Unity\Projects\Black-Cube\Assets\GameData\PassiveTree\Branches\Class\SO_Ranger_Branch.asset

| Tier | Mainline/spine | Left A / B / C | Right A / B / C | Subclass A option (either side) | Subclass B option (either side) |
|---|---|---|---|---|---|
| 1 | Dexterity +5 | Attack Speed +9% / Increased Critical Chance +9% / Precision Chance +10.8% | Projectile Damage +9% / Projectile Speed +9% / Additional Projectile Amount +1.8 | Venom Ranger: Void Damage +9% | Projectile Ranger: Precision Chance +10.8% |
| 2 | Attack Speed +5.125% | Lightning Damage +9.2% / Poison Chance +9.2% / Maximum Life +9.2% | Precision Damage +14.72% / Cooldown Reduction +9.2% / Hit Twice Chance +5.52% | Venom Ranger: Attack Speed +9.2% | Projectile Ranger: Projectile Speed +9.2% |
| 3 | Projectile Damage +5.25% | Attack Speed +9.4% / Increased Critical Chance +9.4% / Precision Chance +11.28% | Projectile Damage +9.4% / Projectile Speed +9.4% / Additional Projectile Amount +1.88 | Venom Ranger: Poison Chance +9.4% | Projectile Ranger: Additional Projectile Amount +1.88 |
| 4 | Projectile Speed +5.375% | Lightning Damage +9.6% / Poison Chance +9.6% / Maximum Life +9.6% | Precision Damage +15.36% / Cooldown Reduction +9.6% / Hit Twice Chance +5.76% | Venom Ranger: Void Damage +9.6% | Projectile Ranger: Precision Chance +11.52% |
| 5 | Precision Chance +6.6% | Attack Speed +9.8% / Increased Critical Chance +9.8% / Precision Chance +11.76% | Projectile Damage +9.8% / Projectile Speed +9.8% / Additional Projectile Amount +1.96 | Venom Ranger: Attack Speed +9.8% | Projectile Ranger: Projectile Speed +9.8% |
| 6 | Increased Critical Chance +5.625% | Lightning Damage +10% / Poison Chance +10% / Maximum Life +10% | Precision Damage +16% / Cooldown Reduction +10% / Hit Twice Chance +6% | Venom Ranger: Poison Chance +10% | Projectile Ranger: Additional Projectile Amount +2 |
| 7 | Shock Chance +5.75%; Shock Effect +5% | Attack Speed +10.2% / Increased Critical Chance +10.2% / Precision Chance +12.24% | Projectile Damage +10.2% / Projectile Speed +10.2% / Additional Projectile Amount +2.04 | Venom Ranger: Void Damage +10.2% | Projectile Ranger: Precision Chance +12.24% |
| 8 | Poison Chance +5.875%; Poison Dmg +5% | Lightning Damage +10.4% / Poison Chance +10.4% / Maximum Life +10.4% | Precision Damage +16.64% / Cooldown Reduction +10.4% / Hit Twice Chance +6.24% | Venom Ranger: Attack Speed +10.4% | Projectile Ranger: Projectile Speed +10.4% |
| 9 | Mana On Hit +2.4 | Attack Speed +10.6% / Increased Critical Chance +10.6% / Precision Chance +12.72% | Projectile Damage +10.6% / Projectile Speed +10.6% / Additional Projectile Amount +2.12 | Venom Ranger: Poison Chance +10.6% | Projectile Ranger: Additional Projectile Amount +2.12 |
| 10 | Projectile Damage +6.125% | Lightning Damage +10.8% / Poison Chance +10.8% / Maximum Life +10.8% | Precision Damage +17.28% / Cooldown Reduction +10.8% / Hit Twice Chance +6.48% | Venom Ranger: Void Damage +10.8% | Projectile Ranger: Precision Chance +12.96% |

## Thief

Source: D:\Unity\Projects\Black-Cube\Assets\GameData\PassiveTree\Branches\Class\SO_Thief_Branch.asset

| Tier | Mainline/spine | Left A / B / C | Right A / B / C | Subclass A option (either side) | Subclass B option (either side) |
|---|---|---|---|---|---|
| 1 | Dexterity +5; Intelligence +4 | Increased Critical Chance +9% / Critical Multiplier +14.4% / Attack Speed +9% | Mana On Hit +3.6 / Cooldown Reduction +9% / Hit Twice Chance +5.4% | Assassin: Critical Multiplier +14.4% | Ailment Assassin: Poison Chance +9% |
| 2 | Increased Critical Chance +5.125% | Poison Chance +9.2% / Void Damage +9.2% / Life On Hit +3.68 | Increased Armour +9.2% / Mana Regeneration +3.68 / Mana On Kill +3.68 | Assassin: Attack Speed +9.2% | Ailment Assassin: Bleed Chance +9.2% |
| 3 | Critical Multiplier +8.4% | Increased Critical Chance +9.4% / Critical Multiplier +15.04% / Attack Speed +9.4% | Mana On Hit +3.76 / Cooldown Reduction +9.4% / Hit Twice Chance +5.64% | Assassin: Increased Critical Chance +9.4% | Ailment Assassin: Critical Multiplier +15.04% |
| 4 | Attack Speed +5.375% | Poison Chance +9.6% / Void Damage +9.6% / Life On Hit +3.84 | Increased Armour +9.6% / Mana Regeneration +3.84 / Mana On Kill +3.84 | Assassin: Critical Multiplier +15.36% | Ailment Assassin: Poison Chance +9.6% |
| 5 | Cooldown Reduction +5.5% | Increased Critical Chance +9.8% / Critical Multiplier +15.68% / Attack Speed +9.8% | Mana On Hit +3.92 / Cooldown Reduction +9.8% / Hit Twice Chance +5.88% | Assassin: Attack Speed +9.8% | Ailment Assassin: Bleed Chance +9.8% |
| 6 | Poison Chance +5.625%; Poison Dmg +5% | Poison Chance +10% / Void Damage +10% / Life On Hit +4 | Increased Armour +10% / Mana Regeneration +4 / Mana On Kill +4 | Assassin: Increased Critical Chance +10% | Ailment Assassin: Critical Multiplier +16% |
| 7 | Void Damage +5.75%; Poison Speed +5% | Increased Critical Chance +10.2% / Critical Multiplier +16.32% / Attack Speed +10.2% | Mana On Hit +4.08 / Cooldown Reduction +10.2% / Hit Twice Chance +6.12% | Assassin: Critical Multiplier +16.32% | Ailment Assassin: Poison Chance +10.2% |
| 8 | Hit Twice Chance +3.525% | Poison Chance +10.4% / Void Damage +10.4% / Life On Hit +4.16 | Increased Armour +10.4% / Mana Regeneration +4.16 / Mana On Kill +4.16 | Assassin: Attack Speed +10.4% | Ailment Assassin: Bleed Chance +10.4% |
| 9 | Life On Hit +2.4 | Increased Critical Chance +10.6% / Critical Multiplier +16.96% / Attack Speed +10.6% | Mana On Hit +4.24 / Cooldown Reduction +10.6% / Hit Twice Chance +6.36% | Assassin: Increased Critical Chance +10.6% | Ailment Assassin: Critical Multiplier +16.96% |
| 10 | Critical Multiplier +9.8% | Poison Chance +10.8% / Void Damage +10.8% / Life On Hit +4.32 | Increased Armour +10.8% / Mana Regeneration +4.32 / Mana On Kill +4.32 | Assassin: Critical Multiplier +17.28% | Ailment Assassin: Poison Chance +10.8% |

## Warrior

Source: D:\Unity\Projects\Black-Cube\Assets\GameData\PassiveTree\Branches\Class\SO_Warrior_Branch.asset

| Tier | Mainline/spine | Left A / B / C | Right A / B / C | Subclass A option (either side) | Subclass B option (either side) |
|---|---|---|---|---|---|
| 1 | Strength +5; Dexterity +4 | Maximum Life +9% / Increased Armour +9% / Physical Damage +9% | Increased Critical Chance +9% / Attack Speed +9% / Hit Twice Chance +5.4% | Bleed Warrior: Physical Damage +9% | Momentum Warrior: Hit Twice Chance +5.4% |
| 2 | Maximum Life +5.125% | Life Regeneration +3.68 / Life On Hit +3.68 / Bleed Chance +9.2% | Maximum Mana +9.2% / Mana On Hit +3.68 / Cooldown Reduction +9.2% | Bleed Warrior: Life On Hit +3.68 | Momentum Warrior: Critical Multiplier +14.72% |
| 3 | Increased Armour +5.25% | Maximum Life +9.4% / Increased Armour +9.4% / Physical Damage +9.4% | Increased Critical Chance +9.4% / Attack Speed +9.4% / Hit Twice Chance +5.64% | Bleed Warrior: Bleed Chance +9.4% | Momentum Warrior: Attack Speed +9.4% |
| 4 | Physical Damage +5.375% | Life Regeneration +3.84 / Life On Hit +3.84 / Bleed Chance +9.6% | Maximum Mana +9.6% / Mana On Hit +3.84 / Cooldown Reduction +9.6% | Bleed Warrior: Physical Damage +9.6% | Momentum Warrior: Hit Twice Chance +5.76% |
| 5 | Hit Twice Chance +3.3% | Maximum Life +9.8% / Increased Armour +9.8% / Physical Damage +9.8% | Increased Critical Chance +9.8% / Attack Speed +9.8% / Hit Twice Chance +5.88% | Bleed Warrior: Life On Hit +3.92 | Momentum Warrior: Critical Multiplier +15.68% |
| 6 | Life On Hit +2.25 | Life Regeneration +4 / Life On Hit +4 / Bleed Chance +10% | Maximum Mana +10% / Mana On Hit +4 / Cooldown Reduction +10% | Bleed Warrior: Bleed Chance +10% | Momentum Warrior: Attack Speed +10% |
| 7 | Bleed Chance +5.75%; Bleed Dmg +5% | Maximum Life +10.2% / Increased Armour +10.2% / Physical Damage +10.2% | Increased Critical Chance +10.2% / Attack Speed +10.2% / Hit Twice Chance +6.12% | Bleed Warrior: Physical Damage +10.2% | Momentum Warrior: Hit Twice Chance +6.12% |
| 8 | Increased Critical Chance +5.875% | Life Regeneration +4.16 / Life On Hit +4.16 / Bleed Chance +10.4% | Maximum Mana +10.4% / Mana On Hit +4.16 / Cooldown Reduction +10.4% | Bleed Warrior: Life On Hit +4.16 | Momentum Warrior: Critical Multiplier +16.64% |
| 9 | Attack Speed +6% | Maximum Life +10.6% / Increased Armour +10.6% / Physical Damage +10.6% | Increased Critical Chance +10.6% / Attack Speed +10.6% / Hit Twice Chance +6.36% | Bleed Warrior: Bleed Chance +10.6% | Momentum Warrior: Attack Speed +10.6% |
| 10 | Physical Damage +6.125%; Phys Penetration +5% | Life Regeneration +4.32 / Life On Hit +4.32 / Bleed Chance +10.8% | Maximum Mana +10.8% / Mana On Hit +4.32 / Cooldown Reduction +10.8% | Bleed Warrior: Physical Damage +10.8% | Momentum Warrior: Hit Twice Chance +6.48% |

## Bow

Source: D:\Unity\Projects\Black-Cube\Assets\GameData\PassiveTree\Branches\Weapon\SO_Bow_Branch.asset

| Tier | Mainline/spine | Left A / B / C | Right A / B / C |
|---|---|---|---|
| 1 | Precision Chance +17.28% | Precision Chance +17.28% / Precision Damage +23.04% / Projectile Speed +14.4% | Projectile Damage +14.4% / Attack Speed +14.4% / Additional Projectile Amount +2.88 |
| 2 | Poison Chance +14.72% | Increased Critical Chance +14.72% / Poison Chance +14.72% / Lightning Damage +14.72% | Mana On Hit +5.888 / Shock Chance +14.72% / Critical Multiplier +23.552% |
| 3 | Projectile Speed +15.04% | Precision Chance +18.048% / Precision Damage +24.064% / Projectile Speed +15.04% | Projectile Damage +15.04% / Attack Speed +15.04% / Additional Projectile Amount +3.008 |
| 4 | Increased Critical Chance +15.36% | Increased Critical Chance +15.36% / Poison Chance +15.36% / Lightning Damage +15.36% | Mana On Hit +6.144 / Shock Chance +15.36% / Critical Multiplier +24.576% |
| 5 | Precision Damage +25.088% | Precision Chance +18.816% / Precision Damage +25.088% / Projectile Speed +15.68% | Projectile Damage +15.68% / Attack Speed +15.68% / Additional Projectile Amount +3.136 |

## Dagger

Source: D:\Unity\Projects\Black-Cube\Assets\GameData\PassiveTree\Branches\Weapon\SO_Dagger_Branch.asset

| Tier | Mainline/spine | Left A / B / C | Right A / B / C |
|---|---|---|---|
| 1 | Increased Critical Chance +14.4% | Increased Critical Chance +14.4% / Critical Multiplier +23.04% / Attack Speed +14.4% | Life On Hit +5.76 / Mana On Hit +5.76 / Cooldown Reduction +14.4% |
| 2 | Bleed Chance +14.72% | Poison Chance +14.72% / Bleed Chance +14.72% / Void Damage +14.72% | Hit Twice Chance +8.832% / Poison Chance +14.72% / Attack Speed +14.72% |
| 3 | Attack Speed +15.04% | Increased Critical Chance +15.04% / Critical Multiplier +24.064% / Attack Speed +15.04% | Life On Hit +6.016 / Mana On Hit +6.016 / Cooldown Reduction +15.04% |
| 4 | Poison Chance +15.36% | Poison Chance +15.36% / Bleed Chance +15.36% / Void Damage +15.36% | Hit Twice Chance +9.216% / Poison Chance +15.36% / Attack Speed +15.36% |
| 5 | Critical Multiplier +25.088% | Increased Critical Chance +15.68% / Critical Multiplier +25.088% / Attack Speed +15.68% | Life On Hit +6.272 / Mana On Hit +6.272 / Cooldown Reduction +15.68% |

## Sceptre

Source: D:\Unity\Projects\Black-Cube\Assets\GameData\PassiveTree\Branches\Weapon\SO_Sceptre_Branch.asset

| Tier | Mainline/spine | Left A / B / C | Right A / B / C |
|---|---|---|---|
| 1 | Aura Effect +14.4% | Aura Effect +14.4% / Cooldown Reduction +14.4% / Mana Regeneration +5.76 | Life On Hit +5.76 / Mana On Hit +5.76 / Life Regeneration +5.76 |
| 2 | Chill Chance +14.72% | Cold Damage +14.72% / Chill Chance +14.72% / Increased Critical Chance +14.72% | Increased Armour +14.72% / Maximum Life +14.72% / Lightning Damage +14.72% |
| 3 | Mana Regeneration +6.016 | Aura Effect +15.04% / Cooldown Reduction +15.04% / Mana Regeneration +6.016 | Life On Hit +6.016 / Mana On Hit +6.016 / Life Regeneration +6.016 |
| 4 | Cold Damage +15.36% | Cold Damage +15.36% / Chill Chance +15.36% / Increased Critical Chance +15.36% | Increased Armour +15.36% / Maximum Life +15.36% / Lightning Damage +15.36% |
| 5 | Cooldown Reduction +15.68% | Aura Effect +15.68% / Cooldown Reduction +15.68% / Mana Regeneration +6.272 | Life On Hit +6.272 / Mana On Hit +6.272 / Life Regeneration +6.272 |

## Staff

Source: D:\Unity\Projects\Black-Cube\Assets\GameData\PassiveTree\Branches\Weapon\SO_Staff_Branch.asset

| Tier | Mainline/spine | Left A / B / C | Right A / B / C |
|---|---|---|---|
| 1 | Cooldown Reduction +14.4% | Cooldown Reduction +14.4% / Maximum Mana +14.4% / Mana Regeneration +5.76 | Fire Damage +14.4% / Lightning Damage +14.4% / Cold Damage +14.4% |
| 2 | Fire Damage +14.72% | Increased Critical Chance +14.72% / Fire Damage +14.72% / Lightning Damage +14.72% | Mana On Hit +5.888 / Critical Multiplier +23.552% / Cooldown Reduction +14.72% |
| 3 | Mana Regeneration +6.016 | Cooldown Reduction +15.04% / Maximum Mana +15.04% / Mana Regeneration +6.016 | Fire Damage +15.04% / Lightning Damage +15.04% / Cold Damage +15.04% |
| 4 | Increased Critical Chance +15.36% | Increased Critical Chance +15.36% / Fire Damage +15.36% / Lightning Damage +15.36% | Mana On Hit +6.144 / Critical Multiplier +24.576% / Cooldown Reduction +15.36% |
| 5 | Maximum Mana +15.68% | Cooldown Reduction +15.68% / Maximum Mana +15.68% / Mana Regeneration +6.272 | Fire Damage +15.68% / Lightning Damage +15.68% / Cold Damage +15.68% |

## Sword

Source: D:\Unity\Projects\Black-Cube\Assets\GameData\PassiveTree\Branches\Weapon\SO_Sword_Branch.asset

| Tier | Mainline/spine | Left A / B / C | Right A / B / C |
|---|---|---|---|
| 1 | Physical Damage +14.4% | Physical Damage +14.4% / Attack Speed +14.4% / Increased Critical Chance +14.4% | Life On Hit +5.76 / Hit Twice Chance +8.64% / Increased Armour +14.4% |
| 2 | Bleed Chance +14.72% | Critical Multiplier +23.552% / Bleed Chance +14.72% / Hit Twice Chance +8.832% | Maximum Life +14.72% / Mana On Hit +5.888 / Cooldown Reduction +14.72% |
| 3 | Increased Critical Chance +15.04% | Physical Damage +15.04% / Attack Speed +15.04% / Increased Critical Chance +15.04% | Life On Hit +6.016 / Hit Twice Chance +9.024% / Increased Armour +15.04% |
| 4 | Critical Multiplier +24.576% | Critical Multiplier +24.576% / Bleed Chance +15.36% / Hit Twice Chance +9.216% | Maximum Life +15.36% / Mana On Hit +6.144 / Cooldown Reduction +15.36% |
| 5 | Attack Speed +15.68% | Physical Damage +15.68% / Attack Speed +15.68% / Increased Critical Chance +15.68% | Life On Hit +6.272 / Hit Twice Chance +9.408% / Increased Armour +15.68% |

## TwoHandedAxe

Source: D:\Unity\Projects\Black-Cube\Assets\GameData\PassiveTree\Branches\Weapon\SO_TwoHandedAxe_Branch.asset

| Tier | Mainline/spine | Left A / B / C | Right A / B / C |
|---|---|---|---|
| 1 | Physical Damage +14.4% | Physical Damage +14.4% / Critical Multiplier +23.04% / Rage Generation +14.4% | Maximum Life +14.4% / Increased Armour +14.4% / Life Regeneration +5.76 |
| 2 | Bleed Chance +14.72% | Rage Effect +14.72% / Bleed Chance +14.72% / Fire Damage +14.72% | Bleed Chance +14.72% / Ignite Chance +14.72% / Life On Hit +5.888 |
| 3 | Rage Generation +15.04% | Physical Damage +15.04% / Critical Multiplier +24.064% / Rage Generation +15.04% | Maximum Life +15.04% / Increased Armour +15.04% / Life Regeneration +6.016 |
| 4 | Rage Effect +15.36% | Rage Effect +15.36% / Bleed Chance +15.36% / Fire Damage +15.36% | Bleed Chance +15.36% / Ignite Chance +15.36% / Life On Hit +6.144 |
| 5 | Critical Multiplier +25.088% | Physical Damage +15.68% / Critical Multiplier +25.088% / Rage Generation +15.68% | Maximum Life +15.68% / Increased Armour +15.68% / Life Regeneration +6.272; Rage Finisher: at maximum Rage, arm an empowered Axe attack |

