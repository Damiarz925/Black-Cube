# Class keystone survey — final production-policy fights

PROVISIONAL WEAPON TREE VALUES — NOT FINAL BALANCE

42 builds plus 18 weapon ablations; 30 fights each, **1800 fights and zero errors**. No auto-tuning. Optimization provenance: 9179FFE9B641FA61BF326F1E90315CB1D8B27C512DB606CEA841735D503CDC35. Final combat/actor provenance: BC03B735D7B623A37A7B292075C15417296DB0A72ADF424A9BCAE616ECB5666B.

Original bounded-search artifacts remain in Logs/ClassKeystones/Survey\9179FFE9B641FA61. Revalidated per-build JSON and ACTOR_INPUTS.json are in Logs/ClassKeystones/Survey\9179FFE9B641FA61\Validated\BC03B735D7B623A3. The new fights use the exact saved allocations, gear and seeds; no crafting/passive searches were repeated. Automatic-only skill bindings always use both skills, matching live Staff behavior; the analytical manual-skill policy cannot disable them. Secondary damage/recovery parity was regression-tested before this replay.

Search: straight progression/no farming, signature weapons, no subclasses, identical checkpoint history/ground-loot seeds, independent cloned inventories, Serious crafting capped at 12 actions, passive beam 12, gear shortlist 20/beam 48, soft current defensive targets. These are bounded heuristic builds, not proven global optima. Level 30 and 31 have different checkpoint inventories; do not read their DPS difference as an isolated keystone multiplier. Analytical ailment DPS is uncapped search potential, not encounter DPS; short fights, mitigation, stack timing and overkill materially change the final Lab result.

## Level 30

| Class | Keystone | Weapon | Combat level | Native/off-class/weapon points | Lab DPS | Analytical DPS | Without weapon DPS | Mean seconds | Win rate | Life | Armour | Combined PDR | F/C/L/V resist | Actual skill policy | Warnings |
|---|---|---|---:|---|---:|---:|---:|---:|---:|---:|---:|---:|---|---|---|
| class.barbarian | Pre-keystone | weapon.two_handed_axe | 26 | 30/0/0 | 243.42 | 315.26 | — | 5.48 | 100% | 2448.39 | 542.5 | 32.46% | 41.27%/51.78%/41.72%/32.88% | Skill 1 Only | — |
| class.mage | Pre-keystone | weapon.staff | 26 | 30/0/0 | 229.06 | 410.64 | — | 6.09 | 100% | 1435.01 | 477.47 | 29.72% | 39.52%/23.11%/33.63%/38.14% | Both skills — automatic cooldown | — |
| class.priest | Pre-keystone | weapon.sceptre | 26 | 30/0/0 | 112.71 | 187.02 | — | 11.93 | 100% | 2543.94 | 659.69 | 36.88% | 47.7%/37.17%/50.71%/64.68% | No Skills | — |
| class.ranger | Pre-keystone | weapon.bow | 26 | 30/0/0 | 141.96 | 2068.2 | — | 9.41 | 100% | 1940.36 | 403.48 | 26.33% | 50.75%/40.37%/40.37%/38.62% | Skill 1 Only | — |
| class.thief | Pre-keystone | weapon.dagger | 26 | 30/0/0 | 542.35 | 513.99 | — | 2.8 | 100% | 1540.5 | 605.72 | 34.92% | 45.97%/51.87%/35.94%/47.83% | Skill 1 Only | — |
| class.warrior | Pre-keystone | weapon.sword | 26 | 30/0/0 | 714.89 | 858.66 | — | 1.91 | 100% | 1916.8 | 1135.86 | 50.15% | 43.29%/40.65%/41.91%/40.85% | Skill 1 Only | Short fight |

## Level 31

| Class | Keystone | Weapon | Combat level | Native/off-class/weapon points | Lab DPS | Analytical DPS | Without weapon DPS | Mean seconds | Win rate | Life | Armour | Combined PDR | F/C/L/V resist | Actual skill policy | Warnings |
|---|---|---|---:|---|---:|---:|---:|---:|---:|---:|---:|---:|---|---|---|
| class.barbarian | Molten Edge | weapon.two_handed_axe | 27 | 31/0/0 | 84.75 | 98.86 | — | 16.48 | 100% | 2468.78 | 499.36 | 30.55% | 37.21%/39.75%/40.33%/36.38% | Skill 1 Only | No Physical to convert |
| class.barbarian | Crown of Fury | weapon.two_handed_axe | 27 | 31/0/0 | 76.88 | 98.86 | — | 18.08 | 100% | 2468.78 | 499.36 | 30.55% | 37.21%/39.75%/40.33%/36.38% | Skill 1 Only | No cap uptime |
| class.barbarian | Blood Engine | weapon.two_handed_axe | 27 | 31/0/0 | 100.25 | 118.63 | — | 14 | 100% | 2468.78 | 499.36 | 30.55% | 37.21%/39.75%/40.33%/36.38% | Skill 1 Only | — |
| class.mage | Fire Commitment | weapon.staff | 27 | 31/0/0 | 255.3 | 628.55 | — | 5.72 | 100% | 1461.4 | 827.34 | 42.16% | 36.29%/50.86%/36.29%/39.74% | Both skills — automatic cooldown | — |
| class.mage | Storm's Price | weapon.staff | 27 | 31/0/0 | 292.67 | 606.02 | — | 4.87 | 100% | 1445.34 | 799.61 | 41.33% | 42.99%/42.99%/60.62%/39.32% | Both skills — automatic cooldown | — |
| class.mage | Stormglass | weapon.staff | 27 | 31/0/0 | 263.81 | 606.02 | — | 5.6 | 100% | 1445.34 | 799.61 | 41.33% | 42.99%/42.99%/60.62%/39.32% | Both skills — automatic cooldown | No Shatter |
| class.priest | Prismatic Discipline | weapon.sceptre | 27 | 31/0/0 | 81.42 | 144.58 | — | 17.09 | 100% | 2634.3 | 567.8 | 33.34% | 40.18%/39.67%/48.19%/52.67% | No Skills | No active auras |
| class.priest | Deep Fracture | weapon.sceptre | 27 | 31/0/0 | 81.42 | 144.58 | — | 17.09 | 100% | 2634.3 | 567.8 | 33.34% | 40.18%/39.67%/48.19%/52.67% | No Skills | No Fracture |
| class.priest | Ailment Sacrifice | weapon.sceptre | 27 | 31/0/0 | 87.77 | 144.58 | — | 15.93 | 100% | 2634.3 | 567.8 | 33.34% | 40.18%/39.67%/48.19%/52.67% | No Skills | — |
| class.ranger | Endless Venom | weapon.bow | 27 | 31/0/0 | 69.12 | 636.8 | — | 20.09 | 100% | 1899.82 | 691.05 | 37.84% | 47.09%/44.58%/51.9%/45.69% | Skill 1 Only | — |
| class.ranger | Risky Precision | weapon.bow | 27 | 31/0/0 | 90.84 | 856.61 | — | 15.35 | 100% | 1899.82 | 691.05 | 37.84% | 47.09%/44.58%/51.9%/45.69% | Skill 1 Only | — |
| class.ranger | Threefold Flight | weapon.bow | 27 | 31/0/0 | 279.11 | 847.47 | — | 4.97 | 100% | 1899.82 | 691.05 | 37.84% | 39.63%/37.11%/59.43%/45.69% | Skill 2 Only | — |
| class.thief | Marked Weakness | weapon.dagger | 27 | 31/0/0 | 371.36 | 327.63 | — | 4.05 | 100% | 1709.6 | 2337.52 | 67.31% | 48.84%/61.1%/69.75%/44.14% | Skill 1 Only | No ailment Crit gained |
| class.thief | First Blood | weapon.dagger | 27 | 31/0/0 | 332.66 | 229.34 | — | 4.68 | 100% | 1709.6 | 2337.52 | 67.31% | 48.84%/61.1%/69.75%/44.14% | Skill 1 Only | — |
| class.thief | Vanishing Blade | weapon.dagger | 27 | 31/0/0 | 423.1 | 376.78 | — | 3.55 | 100% | 1709.6 | 2337.52 | 67.31% | 48.84%/61.1%/69.75%/44.14% | Skill 1 Only | Next-enemy consumption not sampled |
| class.warrior | Blood Reservoir | weapon.sword | 27 | 31/0/0 | 236.47 | 293.13 | — | 5.93 | 100% | 2122.35 | 2428.37 | 68.15% | 37.93%/50.19%/45.76%/45.99% | Skill 1 Only | — |
| class.warrior | One Decisive Strike | weapon.sword | 27 | 31/0/0 | 236.26 | 293.33 | — | 5.93 | 100% | 2122.35 | 2428.37 | 68.15% | 37.93%/50.19%/45.76%/45.99% | Skill 1 Only | — |
| class.warrior | Unbroken Tempo | weapon.sword | 27 | 31/0/0 | 282.52 | 330.53 | — | 4.98 | 100% | 2122.35 | 2428.37 | 68.15% | 37.93%/50.19%/45.76%/45.99% | Skill 1 Only | — |

## Level 70

| Class | Keystone | Weapon | Combat level | Native/off-class/weapon points | Lab DPS | Analytical DPS | Without weapon DPS | Mean seconds | Win rate | Life | Armour | Combined PDR | F/C/L/V resist | Actual skill policy | Warnings |
|---|---|---|---:|---|---:|---:|---:|---:|---:|---:|---:|---:|---|---|---|
| class.barbarian | Molten Edge | weapon.two_handed_axe | 72 | 31/29/10 | 1811.96 | 12103.25 | 1370.76 | 4.54 | 100% | 5063.86 | 1231.3 | 29.9% | 75%/75%/75%/75% | Skill 1 Only | No Physical to convert |
| class.barbarian | Crown of Fury | weapon.two_handed_axe | 72 | 31/29/10 | 1801.41 | 12103.25 | 1337.4 | 4.55 | 100% | 5063.86 | 1231.3 | 29.9% | 75%/75%/75%/75% | Skill 1 Only | No cap uptime |
| class.barbarian | Blood Engine | weapon.two_handed_axe | 72 | 31/29/10 | 2588.52 | 14523.9 | 1772.65 | 3.13 | 100% | 5063.86 | 1231.3 | 29.9% | 75%/75%/75%/75% | Skill 1 Only | — |
| class.mage | Fire Commitment | weapon.staff | 72 | 31/30/9 | 1890.76 | 5472.89 | 1860.35 | 4.62 | 100% | 3086.82 | 10893.12 | 63.45% | 75%/75%/75%/75% | Both skills — automatic cooldown | — |
| class.mage | Storm's Price | weapon.staff | 72 | 31/39/0 | 4999.8 | 239615.43 | 4999.8 | 1.98 | 100% | 3096.9 | 4832.18 | 43.51% | 75%/75%/75%/64.29% | Both skills — automatic cooldown | Short fight |
| class.mage | Stormglass | weapon.staff | 72 | 31/39/0 | 4999.8 | 239615.43 | 4999.8 | 1.98 | 100% | 3096.9 | 4832.18 | 43.51% | 75%/75%/75%/64.29% | Both skills — automatic cooldown | No Shatter; Short fight |
| class.priest | Prismatic Discipline | weapon.sceptre | 72 | 31/28/11 | 1399.73 | 4111.13 | 562.83 | 6 | 100% | 5643.06 | 5062.72 | 44.66% | 75%/71.13%/75%/75% | No Skills | No active auras |
| class.priest | Deep Fracture | weapon.sceptre | 72 | 31/28/11 | 1827.14 | 4111.13 | 785.86 | 4.52 | 100% | 5643.06 | 5062.72 | 44.66% | 75%/71.13%/75%/75% | No Skills | No Fracture |
| class.priest | Ailment Sacrifice | weapon.sceptre | 72 | 31/28/11 | 1929.9 | 4111.13 | 942.98 | 4.26 | 100% | 5643.06 | 5062.72 | 44.66% | 75%/71.13%/75%/75% | No Skills | — |
| class.ranger | Endless Venom | weapon.bow | 72 | 31/25/14 | 4990.24 | 17077.54 | 3087.54 | 1.63 | 100% | 4066.41 | 5970.52 | 48.76% | 75%/75%/75%/75% | Skill 2 Only | No Poison; Short fight |
| class.ranger | Risky Precision | weapon.bow | 72 | 31/26/13 | 5514.81 | 23948.36 | 3923.75 | 1.47 | 100% | 4066.41 | 5970.52 | 48.76% | 75%/75%/75%/75% | Skill 2 Only | Short fight |
| class.ranger | Threefold Flight | weapon.bow | 72 | 31/25/14 | 4221.14 | 51895.14 | 2946.03 | 1.92 | 100% | 4066.41 | 5970.52 | 48.76% | 75%/75%/75%/75% | Skill 2 Only | Short fight |
| class.thief | Marked Weakness | weapon.dagger | 72 | 31/25/14 | 11407.22 | 139928.95 | 10444.46 | 0.77 | 100% | 2954.53 | 3980.35 | 38.81% | 75%/75%/75%/75% | Skill 1 Only | Short fight |
| class.thief | First Blood | weapon.dagger | 72 | 31/25/14 | 11139.65 | 97950.26 | 10085.01 | 0.87 | 100% | 2954.53 | 3980.35 | 38.81% | 75%/75%/75%/75% | Skill 1 Only | Short fight |
| class.thief | Vanishing Blade | weapon.dagger | 72 | 31/25/14 | 11160.77 | 160918.28 | 10220.65 | 0.85 | 100% | 2954.53 | 3980.35 | 38.81% | 75%/75%/75%/75% | Skill 1 Only | Next-enemy consumption not sampled; Short fight |
| class.warrior | Blood Reservoir | weapon.sword | 72 | 31/27/12 | 9513 | 85968.97 | 3273.22 | 0.85 | 100% | 4273.78 | 5970.52 | 48.76% | 75%/70.9%/75%/75% | Skill 1 Only | Short fight |
| class.warrior | One Decisive Strike | weapon.sword | 72 | 31/27/12 | 9354.45 | 87282.66 | 3337.33 | 0.88 | 100% | 4273.78 | 5970.52 | 48.76% | 75%/70.9%/75%/75% | Skill 1 Only | Short fight |
| class.warrior | Unbroken Tempo | weapon.sword | 72 | 31/27/12 | 10939.95 | 98877.11 | 3729.33 | 0.74 | 100% | 4273.78 | 5970.52 | 48.76% | 75%/70.9%/75%/75% | Skill 1 Only | Short fight |

## Resource/attack telemetry

Counts and amounts are totals across each build's 30 fights; percentage inputs are configured actor values, not uptime claims. Full snapshots include every ailment coefficient, cooldown, Mana cost, penetration, resistance cap, Rage-retention and recovery input.

| Level | Class | Keystone | APS | Configured Crit | Crit/Multistrike/Precision events | Projectiles launched/impacted | Would-be regen / Blood Engine recovery | Revenge / Aura Effect | Mana spent/regen/on-hit; zero-Mana seconds | Rage generated/spent/decayed |
|---:|---|---|---:|---:|---|---|---|---|---|---|
| 30 | class.barbarian | Pre-keystone | 0.36 | 5.54% | 8/0/0 | 0/0 | 0.1%/0% | 10%/0% | 1770/710.4/369.05; 0 | 2124/0/0 |
| 30 | class.mage | Pre-keystone | 0.51 | 19.5% | 27/0/3 | 29/28 | 0.05%/0% | 0%/0% | 3190/1735.99/444.11; 0 | 0/0/0 |
| 30 | class.priest | Pre-keystone | 0.63 | 12.17% | 23/5/0 | 0/0 | 0.05%/0% | 0%/40% | 0/0/0; 0 | 0/0/0 |
| 30 | class.ranger | Pre-keystone | 0.91 | 8.46% | 26/0/34 | 240/213 | 0.05%/0% | 0%/0% | 6300/2715.97/1460.55; 0 | 0/0/0 |
| 30 | class.thief | Pre-keystone | 1.13 | 39.67% | 38/13/0 | 0/0 | 0.05%/0% | 0%/0% | 2325/402.66/0; 0 | 0/0/0 |
| 30 | class.warrior | Pre-keystone | 1.14 | 18.68% | 29/32/0 | 0/0 | 0.05%/0% | 0%/0% | 0/0/0; 0 | 0/0/0 |
| 31 | class.barbarian | Molten Edge | 0.33 | 7.62% | 16/0/0 | 0/0 | 0.22%/0% | 20%/0% | 4830/2817.24/0; 0 | 3081.02/0/81.02 |
| 31 | class.barbarian | Crown of Fury | 0.33 | 7.62% | 17/0/0 | 0/0 | 0.22%/0% | 20%/0% | 5310/3153.99/0; 0 | 5604/0/4280.42 |
| 31 | class.barbarian | Blood Engine | 0.33 | 7.62% | 16/0/0 | 0/0 | 0.22%/0.11% | 20%/0% | 4110/2295.76/0; 0 | 3066.18/0/66.18 |
| 31 | class.mage | Fire Commitment | 0.58 | 24.31% | 40/0/7 | 33/29 | 0%/0% | 0%/0% | 3630/1902.19/0; 0 | 0/0/0 |
| 31 | class.mage | Storm's Price | 0.52 | 25.77% | 27/0/2 | 30/20 | 0%/0% | 0%/0% | 3300/1091.64/0; 0 | 0/0/0 |
| 31 | class.mage | Stormglass | 0.52 | 25.77% | 32/0/2 | 38/24 | 0%/0% | 0%/0% | 4180/1659.49/0; 0 | 0/0/0 |
| 31 | class.priest | Prismatic Discipline | 0.34 | 7.69% | 16/0/0 | 0/0 | 0%/0% | 0%/10% | 0/0/0; 0 | 0/0/0 |
| 31 | class.priest | Deep Fracture | 0.34 | 7.69% | 16/0/0 | 0/0 | 0%/0% | 0%/10% | 0/0/0; 0 | 0/0/0 |
| 31 | class.priest | Ailment Sacrifice | 0.34 | 7.69% | 15/0/0 | 0/0 | 0%/0% | 0%/10% | 0/0/0; 0 | 0/0/0 |
| 31 | class.ranger | Endless Venom | 0.75 | 7.14% | 34/0/77 | 436/416 | 0%/0% | 0%/0% | 10080/3938.43/3442.43; 0 | 0/0/0 |
| 31 | class.ranger | Risky Precision | 0.75 | 7.14% | 24/0/258 | 329/307 | 0%/0% | 0%/0% | 7840/2943.6/1946.75; 0 | 0/0/0 |
| 31 | class.ranger | Threefold Flight | 0.75 | 7.14% | 82/0/174 | 1620/971 | 0%/0% | 0%/0% | 3600/320.82/3279.18; 0 | 0/0/0 |
| 31 | class.thief | Marked Weakness | 1.32 | 46.03% | 71/10/0 | 0/0 | 0%/0% | 0%/0% | 4160/692.49/896.19; 0 | 0/0/0 |
| 31 | class.thief | First Blood | 1.32 | 46.03% | 84/12/0 | 0/0 | 0%/0% | 0%/0% | 4732/824.64/1037.7; 0 | 0/0/0 |
| 31 | class.thief | Vanishing Blade | 1.32 | 46.03% | 64/6/0 | 0/0 | 0%/0% | 0%/0% | 3640/586.76/770.41; 0 | 0/0/0 |
| 31 | class.warrior | Blood Reservoir | 0.7 | 28.36% | 131/59/0 | 0/0 | 0%/0% | 0%/0% | 0/0/0; 0 | 0/0/0 |
| 31 | class.warrior | One Decisive Strike | 0.7 | 28.36% | 125/0/0 | 0/0 | 0%/0% | 0%/0% | 0/0/0; 0 | 0/0/0 |
| 31 | class.warrior | Unbroken Tempo | 0.81 | 28.36% | 134/74/0 | 0/0 | 0%/0% | 0%/0% | 0/0/0; 0 | 0/0/0 |
| 70 | class.barbarian | Molten Edge | 0.64 | 5.7% | 15/2/0 | 0/0 | 0.19%/0% | 87.5%/0% | 2610/624.06/1633.6; 0 | 2977.68/0/0 |
| 70 | class.barbarian | Crown of Fury | 0.64 | 5.7% | 15/2/0 | 0/0 | 0.19%/0% | 87.5%/0% | 2610/627.11/1633.6; 0 | 3453.27/0/851.08 |
| 70 | class.barbarian | Blood Engine | 0.64 | 5.7% | 4/0/0 | 0/0 | 0.19%/0.1% | 87.5%/0% | 1800/328.45/1110; 0 | 2330.28/0/0 |
| 70 | class.mage | Fire Commitment | 0.77 | 41.32% | 63/0/2 | 19/19 | 0%/0% | 0%/0% | 2090/870.05/190.69; 0 | 0/0/0 |
| 70 | class.mage | Storm's Price | 0.82 | 53% | 30/0/0 | 2/0 | 0%/0% | 0%/0% | 170/0/26; 0 | 0/0/0 |
| 70 | class.mage | Stormglass | 0.82 | 53% | 31/0/0 | 2/0 | 0%/0% | 0%/0% | 220/0/17.34; 0 | 0/0/0 |
| 70 | class.priest | Prismatic Discipline | 1.09 | 7.58% | 24/38/0 | 0/0 | 0%/0% | 0%/40% | 0/0/0; 0 | 0/0/0 |
| 70 | class.priest | Deep Fracture | 1.09 | 7.58% | 29/21/0 | 0/0 | 0%/0% | 0%/40% | 0/0/0; 0 | 0/0/0 |
| 70 | class.priest | Ailment Sacrifice | 1.09 | 7.58% | 28/18/0 | 0/0 | 0%/0% | 0%/40% | 0/0/0; 0 | 0/0/0 |
| 70 | class.ranger | Endless Venom | 2.07 | 12.99% | 14/0/77 | 360/109 | 0%/0% | 0%/0% | 3600/240.79/2016.5; 0 | 0/0/0 |
| 70 | class.ranger | Risky Precision | 2.1 | 15.1% | 7/0/54 | 344/68 | 0%/0% | 0%/0% | 3440/209.74/999; 0 | 0/0/0 |
| 70 | class.ranger | Threefold Flight | 1.99 | 14.13% | 34/0/156 | 3348/218 | 0%/0% | 0%/0% | 3720/288.17/3368.48; 0 | 0/0/0 |
| 70 | class.thief | Marked Weakness | 1.57 | 74.38% | 30/1/0 | 0/0 | 0%/0% | 0%/0% | 900/22.36/672.5; 0 | 0/0/0 |
| 70 | class.thief | First Blood | 1.57 | 74.38% | 30/2/0 | 0/0 | 0%/0% | 0%/0% | 1025/40.25/777.58; 0 | 0/0/0 |
| 70 | class.thief | Vanishing Blade | 1.57 | 74.38% | 29/2/0 | 0/0 | 0%/0% | 0%/0% | 1000/40.25/759.08; 0 | 0/0/0 |
| 70 | class.warrior | Blood Reservoir | 1.17 | 16.46% | 19/51/0 | 0/0 | 0%/0% | 6.25%/0% | 0/0/0; 0 | 0/0/0 |
| 70 | class.warrior | One Decisive Strike | 1.17 | 16.46% | 15/0/0 | 0/0 | 0%/0% | 6.25%/0% | 0/0/0; 0 | 0/0/0 |
| 70 | class.warrior | Unbroken Tempo | 1.35 | 16.46% | 19/51/0 | 0/0 | 0%/0% | 6.25%/0% | 0/0/0; 0 | 0/0/0 |

## Damage, ailments, healing and keystone activity

Ailment entries show damage per simulated second and maximum/average stacks (stack-time divided by all fight-time). Keystone entries show summed counter values and observation counts. No entry means no observed activity, not proof the mechanic is unavailable.

| Level | Class | Keystone | Player damage by type | Ailment DPS, max/avg stacks | Healing effective / overheal | Keystone counters: total [count] |
|---:|---|---|---|---|---|---|
| 30 | class.barbarian | Pre-keystone | Fire: 36632.08 | Ignite: 20.41, 1/0.2 | Life Regeneration: 245.54/136.82 |  |
| 30 | class.mage | Pre-keystone | Lightning: 29358.63; Fire: 10628.88 |  | Life Regeneration: 84.45/40.14 |  |
| 30 | class.priest | Pre-keystone | Physical: 34063.25; Lightning: 2605.72 | Bleed: 9.28, 5/1.68 | Life Regeneration: 361.67/71.16 |  |
| 30 | class.ranger | Pre-keystone | Void: 11080.24 | Poison: 102.41, 31/11.84 | Life Regeneration: 206.2/54.28; Poison Leech: 1156.29/0 |  |
| 30 | class.thief | Pre-keystone | Lightning: 39987.51 |  | Life Regeneration: 19.89/41.7 |  |
| 30 | class.warrior | Pre-keystone | Fire: 1999.57; Lightning: 37987.94 |  | Life Regeneration: 3.1/49.07 |  |
| 31 | class.barbarian | Molten Edge | Void: 41125.19 | Poison: 0.93, 2/0.19 | Life Regeneration: 2482.47/206.67 | Converted Physical: 0 [161] |
| 31 | class.barbarian | Crown of Fury | Void: 41059.71 | Poison: 0.97, 2/0.2 | Life Regeneration: 2744.12/206.67 |  |
| 31 | class.barbarian | Blood Engine | Void: 41156.31 | Poison: 1.03, 2/0.18 | Damage-based Recovery: 45.81/0 |  |
| 31 | class.mage | Fire Commitment | Fire: 36172.52 | Ignite: 31.55, 1/0.59 |  | Discarded non-Fire damage: 6545.41 [148] |
| 31 | class.mage | Storm's Price | Fire: 7844.1; Void: 18027.63; Lightning: 15644.06 | Ignite: 0.06, 1/0.29; Poison: 0.43, 3/0.26 |  | Enemy bolt damage: 7376.83 [60]; Incoming self-hit damage: 2914.54 [60] |
| 31 | class.mage | Stormglass | Fire: 10898.97; Void: 20301.98; Lightning: 9944.64 | Ignite: 1.91, 1/0.35; Poison: 0.72, 3/0.4 |  |  |
| 31 | class.priest | Prismatic Discipline | Void: 40289.63 | Poison: 2.53, 3/0.63 |  | Active aura count × seconds: 0 [707]; Aura Effect from keystone × seconds: 0 [707] |
| 31 | class.priest | Deep Fracture | Void: 40289.63 | Poison: 2.53, 3/0.63 |  |  |
| 31 | class.priest | Ailment Sacrifice | Void: 38525.82 |  |  | Distinct ailment conversions: 54 [54]; Sacrifice Max-Life damage: 3061.19 [54] |
| 31 | class.ranger | Endless Venom | Void: 11570.82 | Poison: 49.8, 58/24.68 | Poison Leech: 1200.65/0 | Endless Poison duration modifier: 262.08 [416] |
| 31 | class.ranger | Risky Precision | Void: 9799.35 | Poison: 69.01, 32/13.65 | Poison Leech: 1271.51/0 | Projectile misses: 49 [49] |
| 31 | class.ranger | Threefold Flight | Cold: 9844.32; Void: 31127.28 | Poison: 4.13, 111/26.19 | Poison Leech: 24.62/0 | Projectiles before split: 540 [90]; Projectiles after split: 1620 [90] |
| 31 | class.thief | Marked Weakness | Lightning: 41587.01 |  |  | Ailment base Crit gained: 0 [161] |
| 31 | class.thief | First Blood | Lightning: 41587.01 |  |  | Full-health hits: 30 [30]; Injured-target hits: 168 [168] |
| 31 | class.thief | Vanishing Blade | Lightning: 41587.01 |  |  | Stealth activations: 30 [30] |
| 31 | class.warrior | Blood Reservoir | Fire: 40083.09 | Ignite: 8.45, 1/0.7 |  |  |
| 31 | class.warrior | One Decisive Strike | Fire: 39641.2 | Ignite: 10.94, 1/0.7 |  | Multistrikes converted: 53 [53]; Consolidated hit multiplier: 108.65 [53] |
| 31 | class.warrior | Unbroken Tempo | Fire: 40526.29 | Ignite: 7.11, 1/0.69 |  |  |
| 70 | class.barbarian | Molten Edge | Void: 232298.45 | Poison: 78.03, 12/3.55 | Life Regeneration: 842.03/467.18 | Converted Physical: 0 [89] |
| 70 | class.barbarian | Crown of Fury | Void: 232063.55 | Poison: 79.5, 12/3.56 | Life Regeneration: 846.22/467.18 |  |
| 70 | class.barbarian | Blood Engine | Void: 242916.61 | Poison: 0, 8/1.87 | Damage-based Recovery: 133.29/97.48 |  |
| 70 | class.mage | Fire Commitment | Fire: 234865.59 | Ignite: 58.11, 1/0.27; Shock: 0, 0/0.02 |  | Discarded non-Fire damage: 30313.63 [128] |
| 70 | class.mage | Storm's Price | Physical: 214252.18; Fire: 1686.34; Cold: 10713.8; Lightning: 9346.29 | Bleed: 116.09, 5/1.52; Ignite: 0.1, 1/0.13; Shock: 0, 0/0.11 |  | Enemy bolt damage: 2860.35 [3]; Incoming self-hit damage: 966.21 [3]; Player Shocks from self-bolt: 1 [1] |
| 70 | class.mage | Stormglass | Physical: 214252.18; Fire: 1686.34; Cold: 10713.8; Lightning: 9346.29 | Bleed: 116.09, 5/1.52; Ignite: 0.1, 1/0.13; Shock: 0, 0/0.11 |  |  |
| 70 | class.priest | Prismatic Discipline | Cold: 14715.41; Lightning: 6512.95; Fire: 193792.02 | Ignite: 155.06, 1/0.65 |  | Active aura count × seconds: 0 [339]; Aura Effect from keystone × seconds: 0 [339]; Discarded hit damage types: 233 [233] |
| 70 | class.priest | Deep Fracture | Fire: 205344; Cold: 16058.72; Lightning: 6752.98 | Ignite: 108.88, 1/0.64 |  |  |
| 70 | class.priest | Ailment Sacrifice | Fire: 194956.7; Cold: 15246.39; Lightning: 6411.38 |  |  | Distinct ailment conversions: 86 [86]; Sacrifice Max-Life damage: 26302.15 [86] |
| 70 | class.ranger | Endless Venom | Fire: 237527.25; Lightning: 5389.37 | Shock: 0, 0/0.06 |  |  |
| 70 | class.ranger | Risky Precision | Fire: 179455.85; Cold: 60348.32; Lightning: 3112.44 | Shock: 0, 0/0.02 |  | Projectile misses: 14 [14] |
| 70 | class.ranger | Threefold Flight | Fire: 237539.08; Lightning: 5377.53 | Shock: 0, 0/0.15 |  | Projectiles before split: 1116 [93]; Projectiles after split: 3348 [93] |
| 70 | class.thief | Marked Weakness | Physical: 163349.25; Fire: 633.68; Cold: 26596.76; Lightning: 52336.91 | Bleed: 0, 2/0.11; Shock: 0, 0/0.06 |  | Ailment base Crit gained: 0.3 [36] |
| 70 | class.thief | First Blood | Physical: 163207.51; Fire: 634.55; Cold: 26633.12; Lightning: 52408.45 | Bleed: 1.26, 5/0.34; Shock: 0, 0/0.15 |  | Full-health hits: 30 [30]; Injured-target hits: 13 [13] |
| 70 | class.thief | Vanishing Blade | Physical: 163223.33; Fire: 634.69; Cold: 26638.86; Lightning: 52419.74 | Bleed: 0, 4/0.25; Shock: 0, 0/0.13 |  | Stealth activations: 30 [30] |
| 70 | class.warrior | Blood Reservoir | Physical: 230536.55; Fire: 7290.88; Cold: 2457.39; Lightning: 2631.78 | Bleed: 0, 10/0; Shock: 0, 0/0 |  |  |
| 70 | class.warrior | One Decisive Strike | Physical: 230652.88; Fire: 7222.37; Cold: 2434.3; Lightning: 2607.05 | Bleed: 0, 5/0.16; Shock: 0, 0/0 |  | Multistrikes converted: 61 [61]; Consolidated hit multiplier: 125.05 [61] |
| 70 | class.warrior | Unbroken Tempo | Physical: 230536.55; Fire: 7290.88; Cold: 2457.39; Lightning: 2631.78 | Bleed: 0, 5/0; Shock: 0, 0/0 |  |  |

## Interpretation limits

- Weapon ablation keeps final gear, class allocations and actual skill policy; points are refunded and never reallocated. It estimates these **provisional** packages, not final weapon balance.
- Soft defenses can miss targets. Wins against one sampled rare archetype, especially short fights, do not establish boss survival or balanced endgame difficulty.
- Threshold/conditional keystones can be inactive in the selected build. The warning/counter columns distinguish that from focused mechanic regression tests.
- Independent one-enemy fights cannot verify Vanishing Blade's next-enemy first-attack consumption. Native manual encounter-chain smoke remains separate.
- Automatic Staff casting increases Mana pressure and self-bolt opportunities. Neither Staff auto-cast was disabled in these final fights.

