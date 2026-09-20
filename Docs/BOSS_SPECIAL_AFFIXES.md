# Boss-Special Affixes

All values are **FIRST-PASS PLACEHOLDER VALUES**. Every pool has three Prefixes and three Suffixes. Offensive Prefixes support weapons, with selected hybrid Prefixes also supporting Amulets/Rings. Recovery/defensive Suffixes support armour and accessories. Each modifier has a stable `effect.special.*` identity and recursion/source-tag rules.

## Ashen March — Physical / Bleed / Retaliation

- Ravaging (Prefix, weapon): +35% Physical Damage; +25% Bleed Damage.
- Overwhelming Blow (Prefix, weapon): after 1.5 seconds without an attack, the next attack event deals 40% more hit damage.
- Deep Wounds (Prefix, weapon/Amulet/Ring): +20 percentage points Bleed Chance; hits of at least 5% target maximum Life create Bleeds dealing 25% more damage.
- Reprisal (Suffix): after a direct enemy hit, the next attack within 4 seconds deals 30% more Physical hit damage.
- Blood Return (Suffix): hitting a Bleeding enemy restores 2% missing Life once per attack event, 1-second cooldown.
- Partial Rupture (Suffix): 8% chance against Bleeding enemies to deal 25% remaining Bleed damage without removing stacks; cannot recurse.

## Cinder Wastes — Fire / Ignite / Eruption

- Cinder Power (Prefix): +35% Fire Damage; +25% Ignite Damage.
- Eruption (Prefix): 12% chance on hit for a Fire secondary hit at 35% pre-defense magnitude; normal Fire mitigation/Ignite eligibility; cannot recurse.
- Rapid Burn (Prefix): Ignites deal damage 25% faster with 10% less duration.
- Scorching Penetration (Suffix): +15% Fire Penetration; +20% Ignite Duration.
- Kindled Momentum (Suffix): applying Ignite grants +12% Fire Damage for 4 seconds, maximum three independently expiring stacks.
- First Spark (Suffix): first eligible Fire hit against a non-Ignited enemy gains +50 points Ignite Chance and +30% Ignite magnitude.

## Frostbound Reaches — Cold / Chill / Freeze / Shatter

- Deep Winter (Prefix): +35% Cold Damage; +25% Chill Effectiveness.
- Freezing Edge (Prefix): Cold hits against an enemy with at least 20% Chill have 10% Freeze chance.
- Fracture (Prefix): Shatter deals 50% more damage; does not grant Shatter access.
- Frozen Recovery (Suffix): a Freeze-consumed attack skip restores 3% maximum Mana and 2% maximum Life once.
- Chilled Defense (Suffix): while the enemy is Chilled, +12% Armour and +8% elemental resistances.
- Cryostasis (Suffix): +30% Chill Duration; +15% Chill Effectiveness.

## Tempest Heights — Lightning / Shock / Multi-hit

- Overcharge (Prefix): +35% Lightning Damage; +25% Shock Effectiveness.
- Static Echo (Prefix): every fifth eligible hit against a Shocked target creates a 50% pre-defense Lightning hit; target replacement resets count; proc hits never count.
- Shocked Repetition (Prefix): +12 percentage points Hit Twice Chance against Shocked enemies.
- Conductive Criticals (Suffix): against Shocked enemies, +20% Critical Strike Chance and +25% Critical Strike Multiplier.
- Lingering Charge (Suffix): +30% Shock Duration; +15% Shock Effectiveness.
- Arc Momentum (Suffix): Shocked-target hits grant +2% Attack Speed for 3 seconds, maximum five stacks.

## Voidfen — Void / Poison / Ailments

- Abyssal Venom (Prefix): +35% Void Damage; +30% Poison Damage.
- Corrosive Void (Prefix): Poisons with an eligible Void basis deal 30% more damage; no Void contribution means no bonus.
- Toxic Echo (Prefix): successful Poison application has 15% chance for a 50%-magnitude second Poison; echoed Poison cannot echo.
- Void Exposure (Suffix): +15% Void Penetration against Poisoned enemies.
- Accelerated Decay (Suffix): +25% Poison Speed; +15% Poison Duration.
- Corrupted Sustenance (Suffix): once per attack event against Poisoned enemies, restore 2% maximum Mana and 1% maximum Life.

## Black Citadel — Hybrid / Corruption / Multi-system

- Confluence (Prefix): +8% more damage per distinct direct-damage type dealt in the last 4 seconds, maximum five/+40%.
- Afflicted Dominion (Prefix): +8% more damage per distinct Poison/Bleed/Ignite/Shock/Chill on the enemy, maximum five.
- Prismatic Core (Prefix, weapon): gain 8% of weapon Physical damage as each Fire, Cold, Lightning, and Void; added damage is never recursively reused.
- Corruption Mastery (Suffix): +0.10% more damage per current zone-corruption percentage point, +10% at 100%.
- Balanced Assault (Suffix): attacks with at least three nonzero damage types gain +20% Critical Strike Chance and +20% Critical Strike Multiplier.
- Omniailment Resonance (Suffix): per distinct enemy ailment, +5% Attack Speed and +5% Cooldown Reduction, maximum five.

Tooltips label these modifiers `APEX — <challenge>` and show their full mechanical description. Pause → CODEX → MOD LIST shows source, side, supported item types, and effect text.
