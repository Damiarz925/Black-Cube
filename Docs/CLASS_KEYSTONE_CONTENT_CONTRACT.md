# Class keystone and weapon-tree content contract

PROVISIONAL WEAPON TREE VALUES — NOT FINAL BALANCE

Save schema 13. One level-owned passive budget; 100 points at level 100. Native 30 nodes plus one keystone unlock explicit additional-class and single-weapon selection. Off-class travel requires ten spines; its keystone additionally requires twenty side choices. Every weapon tree costs at most 21 shared points.

## Eighteen class keystones

| Class | Keystone | Exact effect |
|---|---|---|
| class.warrior | Blood Reservoir | 35% less Bleed Damage; double maximum Bleed stack cap, not applications. |
| class.warrior | Unbroken Tempo | 15% MORE Attack Speed; multiplicative after increased Attack Speed. |
| class.warrior | One Decisive Strike | Consolidate rolled melee Multistrikes into the original hit at 105% extra-strike damage each. No repeated triggers. |
| class.barbarian | Crown of Fury | Continuous Rage decay; no offensive benefit below full Rage. Full-Rage damage is 40% more than ordinary capped Rage. |
| class.barbarian | Molten Edge | Resolve scaled Physical hit damage as Fire; +1 maximum Ignite stack. Eruption remains separate. |
| class.barbarian | Blood Engine | No Life Regeneration. Recover half the would-be max-Life/sec regeneration percentage from actual damage dealt; 20% MORE Damage. |
| class.ranger | Threefold Flight | Each projectile is replaced once by three independent projectiles dealing 33% each; 15% MORE Projectile Damage. |
| class.ranger | Endless Venom | 40% less Poison Damage; Poison lasts until target death. Each duration-derived additional tick grants 5% MORE Poison Damage. |
| class.ranger | Risky Precision | Projectiles always Precision hit if they land; independent 15% miss chance; 15% MORE Precision Hit Damage. |
| class.mage | Fire Commitment | Discard non-Fire outgoing damage at resolution; 50% MORE Fire Damage. |
| class.mage | Storm's Price | Once per spell cast: enemy and incoming self Lightning bolts at 35% of the primary single-hit basis. No Crit or recursion; self-hit grants no offensive on-hit effects. |
| class.mage | Stormglass | Freeze at 75% Chill Effect. Lightning against Frozen targets deals 1.5x Lightning damage, consumes Freeze and Shatters for 10% target max Life as Cold. |
| class.priest | Ailment Sacrifice | Cannot apply Poison, Bleed or Ignite. Each distinct type a hit successfully would apply loses 5% target maximum Life; at most 15% per hit. Counts for damage-based healing. |
| class.priest | Prismatic Discipline | 20% Aura Effect per active aura. Hits with more than two damage types randomly retain only two types, independently each hit. |
| class.priest | Deep Fracture | Freeze only at 200% Chill Effect for one attack opportunity, then permanently Fracture: 25% more damage taken, no further Freeze or Shatter. |
| class.thief | First Blood | 40% MORE Damage against full-health enemies; 30% LESS against any injured enemy. |
| class.thief | Marked Weakness | +5 percentage points base Crit per unique Poison, Bleed, Ignite, Shock or Chill on the target, before increased Crit scaling. |
| class.thief | Vanishing Blade | 15% MORE generic damage. Kill grants non-stacking Stealth: next enemy's first attack has 30% miss chance, consumed whether it hits or misses. |

These are first-pass values. High thresholds, target conditions, resource pressure and damage-type restrictions can produce weak or inactive results in the controlled survey; that is a diagnostic, not permission to tune them automatically.

## weapon.sword — PROVISIONAL

Offense: Increased Physical Damage, Attack Speed, Multistrike Chance, Increased Critical Chance.

Defense: Increased Maximum Life, Increased Armour, Life On Hit, Increased Life Recovery Effect.

Utility: Mana On Hit, Increased Revenge Effect, Strength, Dexterity.

Spine: increased matching-weapon damage at 4 / 6 / 8 / 10 / 12 / 15 / 20%. Side depth weights: 0.6 / 0.8 / 1 / 1.25 / 1.5 / 3 / 6. Tier-5 premium packages are approximately one-quarter of final-tier stat impact, Tier-6 one-half, Tier-7 full impact; final-tier offensive MORE additions are multiplicative.

| Tier | Left A | Left B | Left C | Right A | Right B | Right C |
|---:|---|---|---|---|---|---|
| 1 | Increased Maximum Life +4.8% | Increased Armour +7.2% | Life On Hit +4.8; Strength +2.25 | Increased Physical Damage +12% | Attack Speed +3.6% | Multistrike Chance +3.6%; Strength +1.5 |
| 2 | Increased Armour +9.6% | Life On Hit +6.4 | Increased Life Recovery Effect +6.4%; Dexterity +3 | Attack Speed +4.8% | Multistrike Chance +4.8% | Increased Critical Chance +8%; Dexterity +2 |
| 3 | Life On Hit +8 | Increased Life Recovery Effect +8% | Increased Maximum Life +8%; Mana On Hit +2.25 | Multistrike Chance +6% | Increased Critical Chance +10% | Increased Physical Damage +20%; Mana On Hit +1.5 |
| 4 | Increased Life Recovery Effect +10% | Increased Maximum Life +10% | Increased Armour +15%; Increased Revenge Effect +9.375% | Increased Critical Chance +12.5% | Increased Physical Damage +25% | Attack Speed +7.5%; Increased Revenge Effect +6.25% |
| 5 | Increased Maximum Life +12% | Increased Armour +18% | Life On Hit +12; Strength +5.625 | Increased Physical Damage +30% | Attack Speed +9% | Multistrike Chance +9%; Strength +3.75 |
| 6 | Increased Armour +36% | Life On Hit +24 | Increased Life Recovery Effect +24%; Dexterity +11.25 | Attack Speed +18% | Multistrike Chance +18% | Increased Critical Chance +30%; Dexterity +7.5 |
| 7 | Life On Hit +48; Strength +22.5 | Increased Life Recovery Effect +48%; Dexterity +22.5 | Increased Maximum Life +48%; Mana On Hit +13.5 | Multistrike Chance +36%; More Damage +30%; Increased Critical Chance +30% | Increased Critical Chance +60%; More Damage +20%; Increased Physical Damage +60% | Increased Physical Damage +120%; More Damage +15%; Attack Speed +18% |

Only one left and one right choice per tier. Tier 7 contains three distinct defensive/utility packages and three distinct offensive packages. All packages are matching-weapon restricted. Bow's fractional additional-projectile investments accumulate and follow the existing integer projectile-count rules.

## weapon.bow — PROVISIONAL

Offense: Increased Projectile-tagged Damage, Attack Speed, Projectile Precision Damage, Increased Critical Chance.

Defense: Increased Maximum Life, Increased Armour, Cold Res, Lightning Res.

Utility: Increased Projectile Speed, Additional Projectile Amount, Projectile Precision Chance, Mana On Hit.

Spine: increased matching-weapon damage at 4 / 6 / 8 / 10 / 12 / 15 / 20%. Side depth weights: 0.6 / 0.8 / 1 / 1.25 / 1.5 / 3 / 6. Tier-5 premium packages are approximately one-quarter of final-tier stat impact, Tier-6 one-half, Tier-7 full impact; final-tier offensive MORE additions are multiplicative.

| Tier | Left A | Left B | Left C | Right A | Right B | Right C |
|---:|---|---|---|---|---|---|
| 1 | Increased Maximum Life +4.8% | Increased Armour +7.2% | Cold Res +3.6%; Projectile Precision Chance +2.7% | Increased Projectile-tagged Damage +12% | Attack Speed +3.6% | Projectile Precision Damage +9%; Projectile Precision Chance +1.8% |
| 2 | Increased Armour +9.6% | Cold Res +4.8% | Lightning Res +4.8%; Mana On Hit +1.8 | Attack Speed +4.8% | Projectile Precision Damage +12% | Increased Critical Chance +8%; Mana On Hit +1.2 |
| 3 | Cold Res +6% | Lightning Res +6% | Increased Maximum Life +8%; Increased Projectile Speed +9% | Projectile Precision Damage +15% | Increased Critical Chance +10% | Increased Projectile-tagged Damage +20%; Increased Projectile Speed +6% |
| 4 | Lightning Res +7.5% | Increased Maximum Life +10% | Increased Armour +15%; Additional Projectile Amount +0.141 | Increased Critical Chance +12.5% | Increased Projectile-tagged Damage +25% | Attack Speed +7.5%; Additional Projectile Amount +0.094 |
| 5 | Increased Maximum Life +12% | Increased Armour +18% | Cold Res +9%; Projectile Precision Chance +6.75% | Increased Projectile-tagged Damage +30% | Attack Speed +9% | Projectile Precision Damage +22.5%; Projectile Precision Chance +4.5% |
| 6 | Increased Armour +36% | Cold Res +18% | Lightning Res +18%; Mana On Hit +6.75 | Attack Speed +18% | Projectile Precision Damage +45% | Increased Critical Chance +30%; Mana On Hit +4.5 |
| 7 | Cold Res +36%; Projectile Precision Chance +27% | Lightning Res +36%; Mana On Hit +13.5 | Increased Maximum Life +48%; Increased Projectile Speed +54% | Projectile Precision Damage +90%; More Damage +30%; Increased Critical Chance +30% | Increased Critical Chance +60%; More Damage +20%; Increased Projectile-tagged Damage +60% | Increased Projectile-tagged Damage +120%; More Damage +15%; Attack Speed +18% |

Only one left and one right choice per tier. Tier 7 contains three distinct defensive/utility packages and three distinct offensive packages. All packages are matching-weapon restricted. Bow's fractional additional-projectile investments accumulate and follow the existing integer projectile-count rules.

## weapon.dagger — PROVISIONAL

Offense: Increased Critical Chance, Critical Damage Multiplier, Attack Speed, Void Damage.

Defense: Increased Maximum Life, Increased Armour, Life On Hit, Cold Res.

Utility: Poison Chance, Poison Speed, Mana On Hit, Intelligence.

Spine: increased matching-weapon damage at 4 / 6 / 8 / 10 / 12 / 15 / 20%. Side depth weights: 0.6 / 0.8 / 1 / 1.25 / 1.5 / 3 / 6. Tier-5 premium packages are approximately one-quarter of final-tier stat impact, Tier-6 one-half, Tier-7 full impact; final-tier offensive MORE additions are multiplicative.

| Tier | Left A | Left B | Left C | Right A | Right B | Right C |
|---:|---|---|---|---|---|---|
| 1 | Increased Maximum Life +4.8% | Increased Armour +7.2% | Life On Hit +4.8; Mana On Hit +1.35 | Increased Critical Chance +6% | Critical Damage Multiplier +12% | Attack Speed +3.6%; Mana On Hit +0.9 |
| 2 | Increased Armour +9.6% | Life On Hit +6.4 | Cold Res +4.8%; Intelligence +3 | Critical Damage Multiplier +16% | Attack Speed +4.8% | Void Damage +16%; Intelligence +2 |
| 3 | Life On Hit +8 | Cold Res +6% | Increased Maximum Life +8%; Poison Chance +7.5% | Attack Speed +6% | Void Damage +20% | Increased Critical Chance +10%; Poison Chance +5% |
| 4 | Cold Res +7.5% | Increased Maximum Life +10% | Increased Armour +15%; Poison Speed +9.375% | Void Damage +25% | Increased Critical Chance +12.5% | Critical Damage Multiplier +25%; Poison Speed +6.25% |
| 5 | Increased Maximum Life +12% | Increased Armour +18% | Life On Hit +12; Mana On Hit +3.375 | Increased Critical Chance +15% | Critical Damage Multiplier +30% | Attack Speed +9%; Mana On Hit +2.25 |
| 6 | Increased Armour +36% | Life On Hit +24 | Cold Res +18%; Intelligence +11.25 | Critical Damage Multiplier +60% | Attack Speed +18% | Void Damage +60%; Intelligence +7.5 |
| 7 | Life On Hit +48; Mana On Hit +13.5 | Cold Res +36%; Intelligence +22.5 | Increased Maximum Life +48%; Poison Chance +45% | Attack Speed +36%; More Damage +30%; Void Damage +60% | Void Damage +120%; More Damage +20%; Increased Critical Chance +30% | Increased Critical Chance +60%; More Damage +15%; Critical Damage Multiplier +60% |

Only one left and one right choice per tier. Tier 7 contains three distinct defensive/utility packages and three distinct offensive packages. All packages are matching-weapon restricted. Bow's fractional additional-projectile investments accumulate and follow the existing integer projectile-count rules.

## weapon.staff — PROVISIONAL

Offense: Fire Damage, Cold Damage, Lightning Damage, Void Damage.

Defense: Increased Maximum Life, All Elemental Resist, Increased Armour, Increased Life Recovery Effect.

Utility: Cooldown Reduction, Mana Percent, Mana Regeneration, Mana On Hit.

Spine: increased matching-weapon damage at 4 / 6 / 8 / 10 / 12 / 15 / 20%. Side depth weights: 0.6 / 0.8 / 1 / 1.25 / 1.5 / 3 / 6. Tier-5 premium packages are approximately one-quarter of final-tier stat impact, Tier-6 one-half, Tier-7 full impact; final-tier offensive MORE additions are multiplicative.

| Tier | Left A | Left B | Left C | Right A | Right B | Right C |
|---:|---|---|---|---|---|---|
| 1 | Increased Maximum Life +4.8% | All Elemental Resist +3.6% | Increased Armour +7.2%; Mana Regeneration +1.35 | Fire Damage +12% | Cold Damage +12% | Lightning Damage +12%; Mana Regeneration +0.9 |
| 2 | All Elemental Resist +4.8% | Increased Armour +9.6% | Increased Life Recovery Effect +6.4%; Mana On Hit +1.8 | Cold Damage +16% | Lightning Damage +16% | Void Damage +16%; Mana On Hit +1.2 |
| 3 | Increased Armour +12% | Increased Life Recovery Effect +8% | Increased Maximum Life +8%; Cooldown Reduction +7.5% | Lightning Damage +20% | Void Damage +20% | Fire Damage +20%; Cooldown Reduction +5% |
| 4 | Increased Life Recovery Effect +10% | Increased Maximum Life +10% | All Elemental Resist +7.5%; Mana Percent +9.375% | Void Damage +25% | Fire Damage +25% | Cold Damage +25%; Mana Percent +6.25% |
| 5 | Increased Maximum Life +12% | All Elemental Resist +9% | Increased Armour +18%; Mana Regeneration +3.375 | Fire Damage +30% | Cold Damage +30% | Lightning Damage +30%; Mana Regeneration +2.25 |
| 6 | All Elemental Resist +18% | Increased Armour +36% | Increased Life Recovery Effect +24%; Mana On Hit +6.75 | Cold Damage +60% | Lightning Damage +60% | Void Damage +60%; Mana On Hit +4.5 |
| 7 | Increased Armour +72%; Mana Regeneration +13.5 | Increased Life Recovery Effect +48%; Mana On Hit +13.5 | Increased Maximum Life +48%; Cooldown Reduction +45% | Lightning Damage +120%; More Damage +30%; Void Damage +60% | Void Damage +120%; More Damage +20%; Fire Damage +60% | Fire Damage +120%; More Damage +15%; Cold Damage +60% |

Only one left and one right choice per tier. Tier 7 contains three distinct defensive/utility packages and three distinct offensive packages. All packages are matching-weapon restricted. Bow's fractional additional-projectile investments accumulate and follow the existing integer projectile-count rules.

## weapon.sceptre — PROVISIONAL

Offense: Increased Magic-tagged Damage, Cold Damage, Fire Damage, Void Damage.

Defense: Maximum Life Regenerated per Second, Increased Maximum Life, All Elemental Resist, Increased Life Recovery Effect.

Utility: Mana Regeneration, Aura Effect, Mana On Hit, Life On Hit.

Spine: increased matching-weapon damage at 4 / 6 / 8 / 10 / 12 / 15 / 20%. Side depth weights: 0.6 / 0.8 / 1 / 1.25 / 1.5 / 3 / 6. Tier-5 premium packages are approximately one-quarter of final-tier stat impact, Tier-6 one-half, Tier-7 full impact; final-tier offensive MORE additions are multiplicative.

| Tier | Left A | Left B | Left C | Right A | Right B | Right C |
|---:|---|---|---|---|---|---|
| 1 | Maximum Life Regenerated per Second +0.3% | Increased Maximum Life +4.8% | All Elemental Resist +3.6%; Mana On Hit +1.35 | Increased Magic-tagged Damage +12% | Cold Damage +12% | Fire Damage +12%; Mana On Hit +0.9 |
| 2 | Increased Maximum Life +6.4% | All Elemental Resist +4.8% | Increased Life Recovery Effect +6.4%; Life On Hit +4.8 | Cold Damage +16% | Fire Damage +16% | Void Damage +16%; Life On Hit +3.2 |
| 3 | All Elemental Resist +6% | Increased Life Recovery Effect +8% | Maximum Life Regenerated per Second +0.5%; Mana Regeneration +2.25 | Fire Damage +20% | Void Damage +20% | Increased Magic-tagged Damage +20%; Mana Regeneration +1.5 |
| 4 | Increased Life Recovery Effect +10% | Maximum Life Regenerated per Second +0.625% | Increased Maximum Life +10%; Aura Effect +9.375% | Void Damage +25% | Increased Magic-tagged Damage +25% | Cold Damage +25%; Aura Effect +6.25% |
| 5 | Maximum Life Regenerated per Second +0.75% | Increased Maximum Life +12% | All Elemental Resist +9%; Mana On Hit +3.375 | Increased Magic-tagged Damage +30% | Cold Damage +30% | Fire Damage +30%; Mana On Hit +2.25 |
| 6 | Increased Maximum Life +24% | All Elemental Resist +18% | Increased Life Recovery Effect +24%; Life On Hit +18 | Cold Damage +60% | Fire Damage +60% | Void Damage +60%; Life On Hit +12 |
| 7 | All Elemental Resist +36%; Mana On Hit +13.5 | Increased Life Recovery Effect +48%; Life On Hit +36 | Maximum Life Regenerated per Second +3%; Mana Regeneration +13.5 | Fire Damage +120%; More Damage +30%; Void Damage +60% | Void Damage +120%; More Damage +20%; Increased Magic-tagged Damage +60% | Increased Magic-tagged Damage +120%; More Damage +15%; Cold Damage +60% |

Only one left and one right choice per tier. Tier 7 contains three distinct defensive/utility packages and three distinct offensive packages. All packages are matching-weapon restricted. Bow's fractional additional-projectile investments accumulate and follow the existing integer projectile-count rules.

## weapon.two_handed_axe — PROVISIONAL

Offense: Increased Physical Damage, Bleed Damage, Bleed Chance, Increased Rage Effect.

Defense: Increased Maximum Life, Maximum Life Regenerated per Second, Life On Hit, Physical Damage Reduction.

Utility: Increased Rage Generation, Reduced Rage Decay, Strength, Increased Revenge Effect.

Spine: increased matching-weapon damage at 4 / 6 / 8 / 10 / 12 / 15 / 20%. Side depth weights: 0.6 / 0.8 / 1 / 1.25 / 1.5 / 3 / 6. Tier-5 premium packages are approximately one-quarter of final-tier stat impact, Tier-6 one-half, Tier-7 full impact; final-tier offensive MORE additions are multiplicative.

| Tier | Left A | Left B | Left C | Right A | Right B | Right C |
|---:|---|---|---|---|---|---|
| 1 | Increased Maximum Life +4.8% | Maximum Life Regenerated per Second +0.3% | Life On Hit +4.8; Strength +2.25 | Increased Physical Damage +12% | Bleed Damage +12% | Bleed Chance +6%; Strength +1.5 |
| 2 | Maximum Life Regenerated per Second +0.4% | Life On Hit +6.4 | Physical Damage Reduction +1.2%; Increased Revenge Effect +6% | Bleed Damage +16% | Bleed Chance +8% | Increased Rage Effect +8%; Increased Revenge Effect +4% |
| 3 | Life On Hit +8 | Physical Damage Reduction +1.5% | Increased Maximum Life +8%; Increased Rage Generation +7.5% | Bleed Chance +10% | Increased Rage Effect +10% | Increased Physical Damage +20%; Increased Rage Generation +5% |
| 4 | Physical Damage Reduction +1.875% | Increased Maximum Life +10% | Maximum Life Regenerated per Second +0.625%; Reduced Rage Decay +9.375% | Increased Rage Effect +12.5% | Increased Physical Damage +25% | Bleed Damage +25%; Reduced Rage Decay +6.25% |
| 5 | Increased Maximum Life +12% | Maximum Life Regenerated per Second +0.75% | Life On Hit +12; Strength +5.625 | Increased Physical Damage +30% | Bleed Damage +30% | Bleed Chance +15%; Strength +3.75 |
| 6 | Maximum Life Regenerated per Second +1.5% | Life On Hit +24 | Physical Damage Reduction +4.5%; Increased Revenge Effect +22.5% | Bleed Damage +60% | Bleed Chance +30% | Increased Rage Effect +30%; Increased Revenge Effect +15% |
| 7 | Life On Hit +48; Strength +22.5 | Physical Damage Reduction +9%; Increased Revenge Effect +45% | Increased Maximum Life +48%; Increased Rage Generation +45% | Bleed Chance +60%; More Damage +30%; Increased Rage Effect +30% | Increased Rage Effect +60%; More Damage +20%; Increased Physical Damage +60% | Increased Physical Damage +120%; More Damage +15%; Bleed Damage +60%; at full Rage arm one 2× attack event, consuming Rage after resolution |

Only one left and one right choice per tier. Tier 7 contains three distinct defensive/utility packages and three distinct offensive packages. All packages are matching-weapon restricted. Bow's fractional additional-projectile investments accumulate and follow the existing integer projectile-count rules.

## Weapon-exclusive suffix ladders

Among weapons only: Sword Multistrike, Bow Projectiles, Staff Cooldown Reduction, Sceptre Aura Effect, Axe local Physical MORE plus global Rage Generation, Dagger Culling. Nonweapon and passive sources are retained.

| Weapon | Family | Tier | Minimum item level | Primary range | Secondary range |
|---|---|---:|---:|---|---|
| weapon.sword | Multistrike Chance | T5 | 50 | 4–5% | — |
| weapon.sword | Multistrike Chance | T4 | 60 | 6.4–8% | — |
| weapon.sword | Multistrike Chance | T3 | 70 | 8.8–11% | — |
| weapon.sword | Multistrike Chance | T2 | 80 | 11.2–14% | — |
| weapon.sword | Multistrike Chance | T1 | 90 | 14.4–18% | — |
| weapon.bow | Additional Projectile Amount | T5 | 50 | 1–1 projectiles | — |
| weapon.bow | Additional Projectile Amount | T4 | 60 | 1–1 projectiles | — |
| weapon.bow | Additional Projectile Amount | T3 | 70 | 1–1 projectiles | — |
| weapon.bow | Additional Projectile Amount | T2 | 80 | 1–1 projectiles | — |
| weapon.bow | Additional Projectile Amount | T1 | 90 | 2–2 projectiles | — |
| weapon.staff | Cooldown Reduction | T5 | 50 | 6.4–8% | — |
| weapon.staff | Cooldown Reduction | T4 | 60 | 9.6–12% | — |
| weapon.staff | Cooldown Reduction | T3 | 70 | 12.8–16% | — |
| weapon.staff | Cooldown Reduction | T2 | 80 | 16–20% | — |
| weapon.staff | Cooldown Reduction | T1 | 90 | 20–25% | — |
| weapon.sceptre | Aura Effect | T5 | 50 | 8–10% | — |
| weapon.sceptre | Aura Effect | T4 | 60 | 16–20% | — |
| weapon.sceptre | Aura Effect | T3 | 70 | 24–30% | — |
| weapon.sceptre | Aura Effect | T2 | 80 | 32–40% | — |
| weapon.sceptre | Aura Effect | T1 | 90 | 40–50% | — |
| weapon.two_handed_axe | Local More Physical Damage / Rage Generation | T5 | 50 | 4–5% | 8–10% Rage Generation |
| weapon.two_handed_axe | Local More Physical Damage / Rage Generation | T4 | 60 | 6–7.5% | 12–15% Rage Generation |
| weapon.two_handed_axe | Local More Physical Damage / Rage Generation | T3 | 70 | 8–10% | 16–20% Rage Generation |
| weapon.two_handed_axe | Local More Physical Damage / Rage Generation | T2 | 80 | 10–12.5% | 20–25% Rage Generation |
| weapon.two_handed_axe | Local More Physical Damage / Rage Generation | T1 | 90 | 12–15% | 24–30% Rage Generation |
| weapon.dagger | Culling Strike Life Threshold | T5 | 50 | 4–5% | — |
| weapon.dagger | Culling Strike Life Threshold | T4 | 60 | 6–7.5% | — |
| weapon.dagger | Culling Strike Life Threshold | T3 | 70 | 8–10% | — |
| weapon.dagger | Culling Strike Life Threshold | T2 | 80 | 10–12.5% | — |
| weapon.dagger | Culling Strike Life Threshold | T1 | 90 | 12–15% | — |

Axe Physical MORE scales local Physical before global scaling; it does not scale local Fire/Cold/Lightning/Void. Dagger Culling checks current Life against maximum Life and includes bosses. All exclusive tiers begin at item level 50, then 60/70/80/90, with T1 strongest. Each tier has weight 100. Five rare Amulet aura-access prefixes remain unchanged.

## Units and compatibility

Life Regeneration is percentage maximum Life/sec, Mana Regeneration flat Mana/sec, Physical Damage Reduction fractional (0.02 = 2%). Schema-12 migration refunds passive allocations, clears selected specialization/routes and explicitly converts historical regeneration rolls. Other run/class/subclass/equipment/rebirth progression is preserved.

See CLASS_KEYSTONE_SYSTEM_GUIDE.md for UI navigation, authoring sockets, staged save, combat rules and survey methodology. Validation and results are reported separately; this content export alone does not certify completion.
