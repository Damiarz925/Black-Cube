# Enemy intrinsic scaling and deterministic balance baseline (Steps 11 + 12)

This is a **pre-balance placeholder baseline**, not final combat tuning. Step 13 owns integrated enemy/player, skill, affix, passive, relic, XP and reward balance. No Step 13 values were changed here.

## Canonical runtime scaling

The tunable first-party configuration is [EnemyScalingProfile.asset](../Assets/Resources/EnemyScalingProfile.asset); [EnemyScalingMath.cs](../Assets/Scripts/EnemyScalingMath.cs) is its single factor/bonus implementation. Combat/enemy level is clamped to at least 1. Every prefab uses its **own authored level-1** `HealthComponent.maxLife`; the boss receives no extra universal multiplier. Through level 100, Life is `seed × 1.04^(L−1)` and outgoing damage factor is `1.03^(L−1)`. Above 100, continue as `1.04^99 × 1.02^(L−100)` and `1.03^99 × 1.015^(L−100)`. Intrinsic flat Armour is `5 × (L−1)`. Ordinary Fire/Cold/Lightning/Void resistance gains `min(20, 0.15 × (L−1))` percentage points. Attack speed, critical stats, max resistance, Poison application resistance and regeneration do not gain intrinsic curves. The damage factor is applied once to the completed pre-crit enemy attack package; Poison/Void DOT derives from that already-scaled source.

`EnemyStatSetup` replaces source-owned Armour/resistance bonuses when reapplied and resets Life base from the prefab seed, not from a gear-modified maximum. The optimizer sees the scaled pre-gear stat snapshot **and** the intrinsic outgoing factor; it still chooses among independently generated `Gear` candidates with unchanged weights, pools, tiers and affix values.

| Level | Life factor | Damage factor | Goblin intrinsic Life | Hobgoblin intrinsic Life | Armour bonus | Resistance points |
| ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 1.000 | 1.000 | 250 | 500 | 0 | 0.00 |
| 10 | 1.423 | 1.305 | 356 | 711 | 45 | 1.35 |
| 25 | 2.563 | 2.033 | 641 | 1,282 | 120 | 3.60 |
| 50 | 6.833 | 4.256 | 1,708 | 3,417 | 245 | 7.35 |
| 75 | 18.217 | 8.912 | 4,554 | 9,109 | 370 | 11.10 |
| 100 | 48.562 | 18.659 | 12,141 | 24,281 | 495 | 14.85 |
| 150 | 130.710 | 39.281 | 32,678 | 65,355 | 745 | 20.00 |
| 200 | 351.818 | 82.697 | 87,954 | 175,909 | 995 | 20.00 |
| 300 | 2,548.795 | 366.517 | 637,199 | 1,274,397 | 1,495 | 20.00 |

These are *intrinsic* values before equipment, Life%, Strength and other legitimate modifiers. Level 1000 was tested for finite/monotonic arithmetic only, not balance.

## Deterministic lab and reference target

The Editor-only lab discovers the active normal/boss prefab references on `PaperBattle.prefab`, presently Goblin2D and Hobgoblin2D. Each sampled build uses the real `ModManager`, `Gear`, `EnemyAI`, `EnemyBuildOptimizer`, `StatsComponent`, `CombatCalculator` and `AilmentCalculator` paths on an isolated actor; it neither edits prefab assets nor touches run saves, progression, currencies or inventory. Legacy `UnityEngine.Random` gear rolls are seeded within a saved/restored state scope, while pure snapshot duels use local `System.Random` seeds. The default scenario samples 250 builds per archetype at levels 1, 10, 25, 50, 75, 100, 150, 200 and 300; the Editor window accepts quick 25, baseline 250 or deep 1000 samples and custom seed/levels/output.

The reference player starts from an isolated real `PlayerStatSetup`/`PlayerController` starter snapshot and is then *synthetically* extended with Life/damage factor `1.035^(L−1)` through 100 and `1.035^99 × 1.018^(L−100)` afterward, Armour `+6 × (L−1)`, and ordinary Fire/Cold/Lightning/Void resistance `min(30, 0.25 × (L−1))` percentage points.

> Reference player values are synthetic placeholder comparison values and are not the game's final player progression model.

CSV/JSON contain per-build intrinsic/final stats, gear/rarity/modifiers, optimizer outputs, typed mitigation including Poison/Void, expected hit/DPS and reference TTK/TTD; the exact synthetic-player warning appears in each format (as a CSV column, JSON field and Markdown statement). Markdown adds min/p10/p50/p90/max/mean distributions, dominant weapon element, build-signature diversity, rarity/slot mix and top affixes. The first 25 sampled builds in each level/archetype also run seeded event-driven snapshot duels with gauges, crit, Hit Twice, production hit/DOT math, Shock and Chill. Analytic TTK/TTD omit ailment ramp and skills, so they are diagnostics rather than shipping combat targets.

## Seeded baseline observations and performance

The finalized report used seed **11012**, **250** generated builds per active archetype/level (4,500 detailed rows), and captured the real level-1 starter at **1,000 Life, 80.00 noncritical hit, 1.20 attacks/s**. The finalized quick 25-build run took **33.8 seconds**; the normal 250-build run took **329.0 seconds** on this machine. Full raw outputs are ignored under `Logs/Balance/`; they are not save-game input. Median (`p50`) and `p90` below are sampled final, post-gear values. TTK/TTD are analytic seconds against the synthetic reference; wins are from the first 25 seeded snapshot duels per group, now using the same synthetic Armour/resistance defense as the analytic TTD calculation.

| Enemy | Level | Final Life p50 / p90 | Expected basic DPS p50 / p90 | TTK p50 | TTD p50 | Reference wins / 25 |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| Goblin | 1 | 250 / 250 | 24.93 / 35.19 | 2.60 | 40.12 | 25 |
| Hobgoblin boss | 1 | 500 / 500 | 24.36 / 33.73 | 5.21 | 41.05 | 25 |
| Goblin | 10 | 356 / 356 | 31.47 / 43.25 | 2.83 | 43.30 | 25 |
| Hobgoblin boss | 10 | 712 / 712 | 30.98 / 43.49 | 5.66 | 43.99 | 25 |
| Goblin | 25 | 641 / 641 | 123.52 / 165.91 | 3.12 | 18.49 | 25 |
| Hobgoblin boss | 25 | 1,282 / 1,282 | 124.58 / 167.69 | 6.23 | 18.33 | 25 |
| Goblin | 50 | 1,764 / 2,570 | 637.73 / 1,003.06 | 3.63 | 8.46 | 24 |
| Hobgoblin boss | 50 | 3,417 / 4,158 | 642.24 / 1,009.85 | 7.03 | 8.40 | 18 |
| Goblin | 75 | 5,124 / 7,242 | 6,633.23 / 16,719.49 | 4.35 | 1.92 | 5 |
| Hobgoblin boss | 75 | 9,327 / 11,187 | 6,834.19 / 15,160.13 | 7.93 | 1.87 | 0 |
| Goblin | 100 | 12,438 / 14,888 | 17,100.07 / 33,395.93 | 4.39 | 1.76 | 3 |
| Hobgoblin boss | 100 | 24,281 / 27,430 | 16,792.74 / 34,748.69 | 8.57 | 1.79 | 0 |

Above-100 safety/endless observations (not final v1 targets): at level **150/200/300**, Goblin final-Life p50 is **32,677 / 87,954 / 637,194** and basic-DPS p50 is **34,307 / 78,666 / 301,892**; Hobgoblin p50 is **65,355 / 175,908 / 1,274,388 Life** and **33,997 / 71,900 / 343,032 DPS**. Analytic TTK remains finite (Goblin p50 **4.69/5.14/6.22 s**, Hobgoblin **9.38/10.28/12.45 s**); the reference mostly loses those sampled duels. Level 1000 arithmetic is finite and monotonic but intentionally not claimed balanced.

No level-1 one-shot, non-monotonic scaling or formula explosion was found, so **no placeholder self-correction** was made. The synthetic reference's median death time compresses sharply from level 50 to 75 despite finite analytic TTK; the first-25 duel wins fall to 5/25 (Goblin) and 0/25 (boss) at 75. This is a measurement concern for Step 13, **not** a reason to tune enemies to the placeholder player. A plausible contributor is that actual enemy generated equipment and candidate counts jump at higher levels while the synthetic player deliberately has no matching gear/skill/passive progression; this is an inference to validate against real player builds.

Current level-1 archetype preservation is explicit: Goblin/Hobgoblin authored maximum Life is **250/500**, authored fallback speed is **0.1/0.1**, and intrinsic Armour, Fire/Cold/Lightning/Void resistance and regeneration are **zero** for both. With the existing generated weapon/gear pipeline at seed 11012, final **pre-crit outgoing-hit p50/p90** is **42.69/53.43** (Goblin) and **40.99/52.76** (boss); final Attack Speed p50/p90 is **0.61/0.72** and **0.59/0.71** attacks/s. These output distributions vary with gear, while the intrinsic level-1 damage factor remains exactly 1.0, so the opening encounter is not rebalanced by Step 11.

Optimizer output is not singular: at level 1, dominant weapon elements are near-even (Goblin Fire/Light/Void/Cold/Physical **54/50/50/49/47**), with **119/250** distinct slot/element/rarity signatures; at level 100, all 250 signatures are distinct but Physical is a plurality (**76/250 Goblin, 92/250 boss**). `GenericDmg`, `GenericMult` and later `ChanceToHitTwice` repeatedly lead affix-frequency counts. A slot/element/rarity signature is only an approximate diversity measure, not proof of distinct damage strategies. Record the Physical tilt and repeated generic-affix preference for Step 13; do not rewrite optimizer weights here.

## Rerun

Unity Editor menu: **Black Cube → Balance → Open Balance Lab**. Configure seed, samples, comma-separated levels and project-relative output, then run the configured/quick/baseline action.

Batchmode baseline (Unity 6000.6.0f1):

```powershell
& 'D:\Unity\6000.6.0f1\Editor\Unity.exe' -batchmode -nographics -quit -projectPath 'D:\Unity\Projects\Black-Cube' -executeMethod BlackCube.BalanceSimulationRunner.RunBaseline -balanceSeed 11012 -balanceSamples 250 -balanceLevels '1,10,25,50,75,100,150,200,300' -balanceOutput 'Logs/Balance' -logFile 'D:\Unity\Projects\Black-Cube\Logs\Step12-baseline.log'
```

Outputs: `Logs/Balance/Step11_12_EnemyScaling.csv`, `Step11_12_EnemyScaling.json`, and `Step11_12_BalanceSummary.md`. The committed concise observations in this document are not automatically overwritten by a run.

Batchmode flags `-balanceSeed`, `-balanceSamples` (1–1,000), `-balanceLevels` (comma-separated), and `-balanceOutput` (project-relative) are optional; absent flags use the visible baseline defaults. Use `BlackCube.BalanceSimulationRunner.RunQuick` for the 25-sample default, or invoke `BalanceSimulationRunner.Run(config)` programmatically for a custom scenario. Output directories are checked to remain inside the project workspace.

## Questions for Step 13

- Compare real player build progression (skills, passives, relics and gear) against the synthetic reference; do not tune runtime enemies solely to this placeholder.
- Check damage-type/affix/build-diversity convergence at high levels, especially optimizer preferences and gear rarity/count growth.
- Decide intended target TTK/TTD, boss challenge, XP/reward pacing and post-100/endless behavior before changing the central profile.
- Validate ailment-ramp, Shock/Chill and active-skill outcomes in longer real-play combat fixtures before finalizing numbers.
