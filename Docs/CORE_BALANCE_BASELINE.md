# Black-Cube Step 13 integrated core balance baseline

This is the first *playtest* baseline for the implemented core loop, not a claim of final launch balance. It freezes the Step 13 production numbers, the deterministic reference methodology, and the limitations that a human playtest must resolve. The synthetic Step 11/12 reference player remains useful for enemy-curve shape diagnostics only; it is **not** the player used here.

## Source and measurement method

`CoreBalanceReferenceRunner` constructs actor stats from the authored player prefab model, `PlayerProgression`, legal allocations on the real passive graph, production `ModManager` equipment rolls, permanent random Implicits, Prefix/Suffix legality, actual item-level tier access, real weapon ranges, attributes, equipped skills and active relic modifiers. `ItemizationValidator` rejects illegal constructed gear. Its eight paths are auto/basic, Heavy Strike Physical, Ice Strike Cold/Chill, Lightning Strike Shock, Fireball Fire/Projectile, Envenom Poison/Void, Shiv Bleed and Immolate Ignite. Combat checkpoints are 1/10/25/50/75/100, then player-capped level 100 against combat 110/125/150/200/300.

Each archetype/checkpoint rolls three gear tracks. P50 natural picks among 1–3 candidates per slot, P75 engaged among 3–9 with occasional legal Normal→Magic crafting, and P90 strong among 6–18 with additional occasional Magic→Rare crafting. The scorer values relevant weapon/build stats and, for P75/P90, first fills core resistance gaps; it no longer treats an almost-capped resistance as unimportant. This is an **availability model**, not a complete chronology of dropped gear and spent orbs. Every selected item's Implicit is a production roll; no slot is awarded a hand-picked BIS Implicit. The resulting passive allocations are path-connected and fit the available level budget. Heavy Strike appears once as a skill archetype and once as an auto-only control; only the skill row enters seven-skill comparisons.

The relic tracks are 0 first-run, 1 randomly generated average/current-cycle relic, and 4 reasonable randomly generated/crafted active relics. They do not assume perfect type/value rolls. A random relic can be an XP or resource relic, so the one-relic *combat* track must not be misread as the uplift of a selected combat-specific relic. XP relic effects are modeled in progression math separately from duel TTK.

For each reference row, the report records an expected-hit normal TTK/TTD, 10-second burst and 30-second sustained skill throughput, effective core resistances (including All Resistance), Mana/cost/regeneration, basic/skill ailment contribution, Prefix/Suffix and resistance-affix investment, Implicit count/points, tier-1 explicit count, gear pieces and enemy role. Three seeded event-timed idle and engaged duels per row include automatic attacks, independent weapon damage rolls, crit, Hit Twice, active inputs spaced at least 1.5 seconds, Mana recovery, DOT turn ownership, Shock and Chill. Event wins, victory/death time, actual DOT share, Mana remaining and cast count are retained in local JSON under `Logs/Balance/`.

The duels are **not** exact-frame scene play: projectile travel, ranged skill-hit weapon variance, status expiration, enemy/player regeneration, pickup time, actual input decisions and encounter transitions remain approximations. Expected-hit TTD omits DOT pressure. Three duels per row and five production builds per archetype/level/seed give directional medians and distributions, not precise population percentiles. Every numerical conclusion below distinguishes these diagnostics from verified runtime semantics.

### Tuning discipline

The five tuning seeds are 13001–13005. The untouched holdout seeds are 13901–13903. The primary final-source run uses five builds/archetype/checkpoint/track per seed, so tuning combines 25 independent builds per cell and holdout combines 15; each build has three paired idle/engaged duels against a normal and boss. The old 250-build Step 11/12 lab remains an enemy-distribution diagnostic where practical, not a real-player holdout. No tuning is made after examining holdout outcomes.

The four major diagnostic stages were: (1) production-player references exposed pathological high-tier enemy equipment compounding; (2) a flattened intrinsic-curve trial was explicitly **rejected** because it violated the approved growth ranges; (3) enemy-only item-tier compression with the approved intrinsic curve restored credible late-game survival; (4) integrated resistance access, real level-owned Life/XP pacing and boss-role pressure were tuned together. A subsequent resistance-aware **reference selection** correction changed no generated item legality or player gameplay value. Reports from rejected trials remain local diagnostics, never shipped configuration.

## Frozen production configuration

The player retains authored 1,000 Life at level one. `PlayerProgression` adds level-owned flat Life before equipment/passive percentage multipliers: 1.012 growth for the first 49 level gains, then 1.025 for the remaining 50 through the level-100 cap. This is runtime progression, not simulation scaling. Boss XP is five normal-enemy equivalents. Normal-equivalent kills required per level interpolate through 8/10/16/22/32/45 at levels 1/10/25/50/75/99; the requirement-XP anchors and passive-point budget stay intact. Level-up restores resources after applying the new stat layer. Rebirth is optional at combat zone 60+; New Game wipes it.

The enemy `EnemyScalingProfile` is unchanged: Life ×1.04 and outgoing damage ×1.03 per level to 100, then ×1.02/×1.015 afterward, +5 Armour per level and +0.15 points/level ordinary Fire/Cold/Lightning/Void resistance (20-point cap). Authored level-one normal/boss Life remains 250/500. Enemy-only equipment item level is `min(15, 1 + combatLevel/10)`; player loot still uses its own level and rarity bands. Enemy rarity remains separately weighted 40/20/10/1 for Normal/Magic/Rare/Legendary; modifier counts remain 1/2/3–4/5–6 and gear-piece counts rise independently to eight at level 75+. The optimizer remains 75% offense/25% defense. Boss role multiplies outgoing damage by 1.8 *after* intrinsic scaling; it does not alter the profile or the authored Life seed.

Normal maximum resistance remains 75% with the existing 90% hard ceiling. The four ordinary core resistance family-selection weights are now 80, and All Resistance is 200. All Resistance on Body Armour, Ring and Belt uses the Black-Cube-specific item-level ladder 1/12/24/36/48/60/85 with rolls 5–7/7–10/10–13/13–16/16–19/19–22/22–25 points; Helmet retains its authored fallback. Weapon core resistance is still ineligible. Slot-relative tier counts, T1 as strongest, lower-tier eligibility, affix side/group rules, permanent Implicits and the single Ring remain unchanged. [POEDB_AFFIX_BASELINE.md](POEDB_AFFIX_BASELINE.md) preserves the researched ordinary-PoE snapshot and explicitly records this Black-Cube deviation.

Natural player loot rarity is smoothly interpolated: at level 1 Normal/Magic/Rare/Legendary = 60/31/8/1%; at level 20 = 42/39/17/2%; at level 50 = 28/41/28/3%; from 75 = 19/35/42/4%. The historical 1/71 Legendary construction baseline was healthy and was not treated as a bug. Normal enemies roll equipment at 50%; bosses guarantee at least one equipment item, never a guaranteed Legendary. Independent ordinary currency rates per normal kill are 7% Normal→Magic, 7% Magic reroll, 5% Magic→Rare, 4% Rare/Legendary reroll, 3% Rare/Legendary Add and 2% Magic+ Remove. A boss adds one guaranteed random ordinary orb to those independent rolls. All current equipment dismantle pays fragments rather than random full orbs: Normal 0, Magic +1 N→M, Rare +1 M→R, Legendary +2 M→R; ten matching fragments convert automatically to a full orb with remainder. Manual and filter auto-dismantle share the same guarded path. Schema 6 persists fragment remainders plus relic level/tier metadata; schema-2 JSON migrates sequentially 2→3→4→5→6 and the historical V1 key follows the same later migrations. New Game/Rebirth clear fragments, Restart preserves and Load restores them. Canonical Ancient art/semantics and permanent Implicits are untouched.

Deep Freeze's production Chill cap is 40% (+10 percentage points), with the existing 25% less Chill effectiveness downside; ordinary Chill remains capped at 30%. Lightning Strike defaults to 20 Mana; common skills' natural ~20–30 Mana depletion/recast at 7 Mana/second corresponds to roughly 3–4 seconds, while 100-Mana Fireball corresponds to ~14 seconds before Mana-on-hit/gear bonuses. Skill levels remain 5% offensive output and 2% cost per level, cap 20. Ignite/Bleed/Poison turn and stack rules, functional crit base 1.5×, and independently sampled weapon ranges were not redesigned.

## Combat and resistance findings

The final source generated 39,600 tuning rows and 23,760 untouched holdout rows. The table uses P50/no-relic expected-hit TTK and the event-duel win rate in parentheses. TTD is Life divided by expected enemy basic-hit DPS, not a literal death time when recovery/statuses are active. Event victory time is retained in the JSON only among wins; losses are never folded into a deceptively short TTK.

| Level | Tuning normal TTK (win) | Tuning boss TTK (win) | Normal / boss TTD | Holdout normal TTK (win) | Holdout boss TTK (win) |
|---:|---:|---:|---:|---:|---:|
| 1 | 2.05s (100%) | 4.11s (100%) | 40.95s / 22.59s | 2.06s (100%) | 4.13s (100%) |
| 10 | 5.31s (100%) | 10.60s (94%) | 36.91s / 20.27s | 5.19s (100%) | 10.30s (93%) |
| 25 | 3.69s (100%) | 7.41s (93%) | 25.78s / 14.85s | 3.57s (100%) | 7.24s (93%) |
| 50 | 4.83s (100%) | 9.92s (86%) | 34.88s / 18.59s | 5.14s (99%) | 9.85s (84%) |
| 75 | 3.93s (98%) | 8.07s (82%) | 25.48s / 14.10s | 3.91s (99%) | 8.39s (72%) |
| 100 | 5.44s (91%) | 10.95s (65%) | 19.62s / 11.86s | 5.81s (90%) | 11.69s (54%) |

Normal TTK is in or very near the requested bands except level 10 (+0.31s) and level 75 (-0.07s). Boss TTK is short at levels 1/25/50/75/100; boss pressure instead appears as declining win rate and level-100 TTD. This baseline deliberately records that mismatch rather than adding an unapproved hidden boss-Life curve after holdout. The most important manual check is whether short successful boss fights plus meaningful loss chance feels better or worse than longer, lower-pressure fights.

For P90 gear against ordinary noncritical hits, median damage is only 2.3–7.0% of maximum Life across the six checkpoints; the 90th percentile is 4.7–9.5%. A base 1.5x crit is therefore far below the 55% preference. Independent min/max weapon rolls, crit and Hit Twice are sampled by event duels and covered by focused tests; no routine one-shot was found. This does not bound future authored enemy abilities.

### Resistance access and affix pressure

Values below are mean effective Fire/Cold/Lightning/Void percentages before the 75% ordinary combat clamp. `Caps` is the mean number of four at or above 75; `All 4` is the share of generated characters with all four at or above 75.

| Level | Track | Fire / Cold / Lightning / Void | Caps | All 4 | Resistance Implicits / explicits |
|---:|---|---|---:|---:|---:|
| 25 | P50 | 26 / 23 / 25 / 23 | 0.01 | 0.0% | 1.63 / 3.65 |
| 50 | P50 | 39 / 41 / 38 / 34 | 0.29 | 0.0% | 1.51 / 4.66 |
| 75 | P50 | 58 / 56 / 58 / 60 | 1.19 | 1.1% | 1.58 / 5.48 |
| 100 | P50 | 64 / 62 / 64 / 61 | 1.34 | 1.7% | 1.47 / 5.39 |
| 50 | P75 | 58 / 60 / 65 / 59 | 1.11 | 0.6% | 1.90 / 6.90 |
| 75 | P75 | 82 / 82 / 83 / 86 | 2.34 | 9.1% | 1.91 / 7.49 |
| 100 | P75 | 90 / 92 / 87 / 88 | 2.53 | 20.0% | 1.90 / 7.14 |
| 50 | P90 | 82 / 81 / 79 / 77 | 2.19 | 10.9% | 2.61 / 8.51 |
| 75 | P90 | 98 / 95 / 96 / 104 | 3.07 | 36.6% | 2.26 / 8.63 |
| 100 | P90 | 103 / 103 / 105 / 107 | 3.19 | 40.6% | 1.99 / 8.37 |

Holdout results were close: P50 level-100 means were 67/56/67/65, P75 averaged 2.54 caps with 14.3% all-four, and P90 averaged 3.16 caps with 38.1% all-four. P50 is inside the target ranges and P75 can cap all four without perfect gear, but all-four P90 capping is not yet a majority outcome. P75/P90 spend roughly seven to nine explicit resistance affixes plus about two random resistance Implicits; defense therefore retains real suffix opportunity cost. Across level-100 P50/P75/P90 reference items, rolled Implicits account for about 26.7/28.4/28.9% of the runner's total item-selection score. This score is a heterogeneous selection heuristic, not an exact DPS multiplier, but it shows Implicits are useful without replacing the explicit budget. Each eight-item set has exactly eight production-rolled Implicits; the model never assumes all eight are resistance rolls.

Prefix and Suffix occupancy stays symmetric by construction: at level 100 it averages 11.7/11.7 on P50, 13.9/13.9 on P75 and 15.5/15.5 on P90. P50/P75/P90 average 4.2/6.0/7.2 top-tier explicits at level 100. The reference craft model averages below one crafted item at level 100 for P75/P90 because crafting is probabilistic and deliberately does not hand-build BIS gear. Longitudinal useful-upgrade frequency, dead/redundant drop rate and 30–70-action spend remain manual economy measurements; the deterministic runner proves legal availability and pressure, not a complete pickup history.

### Skills, active play, Mana and ailments

The following cells are median 30-second P50/no-relic throughput relative to the seven-skill median, followed by expected DOT share where applicable. Gear is independently legal and archetype-scored, so ratios include the basic-attack quality of that archetype's selected weapon—not only the active-skill coefficient.

| Skill | L25 | L50 | L75 | L100 |
|---|---:|---:|---:|---:|
| Heavy Strike | 2.03 / 2% | 2.80 / 3% | 1.47 / 8% | 1.42 / 8% |
| Ice Strike | 1.00 / 0% | 1.00 / 0% | 1.00 / 3% | 0.64 / 0% |
| Lightning Strike | 0.66 / 2% | 0.68 / 0% | 0.49 / 2% | 0.53 / 0% |
| Fireball | 0.99 / 0% | 1.21 / 0% | 1.25 / 4% | 1.26 / 8% |
| Envenom | 0.72 / 44% | 0.89 / 50% | 0.92 / 44% | 1.00 / 42% |
| Shiv | 1.23 / 38% | 1.15 / 44% | 1.15 / 23% | 1.18 / 26% |
| Immolate | 1.01 / 57% | 0.99 / 58% | 0.61 / 58% | 0.55 / 47% |

The final corrective pass reduced Shiv from 50% direct/20x Bleed basis to 20%/6x. It removed the previous 2.6–4.0x runaway while preserving a guaranteed Bleed and near-median total throughput. Holdouts reproduced the correction: Shiv ratios were 1.00/1.12/1.00/1.22 at levels 25/50/75/100. Late-game Shiv expected DOT share (23–26%) remains below the 40–70% aspiration; event-timed share is lower because short fights do not realize all remaining Bleed damage. Envenom reaches 42–50% expected DOT, just below its 50–80% lower bound in several cells; Immolate generally preserves its Ignite identity but falls behind total throughput late. Heavy Strike is the clear remaining high outlier, especially at levels 25/50, because its archetype-scored Physical gear also creates an unusually strong automatic attack. Lightning Strike and late Immolate/Ice are low outliers. These are explicit human-playtest/re-reference priorities; further coefficient-only tuning would hide the gear-model effect.

Median expected engaged uplift over the same character's automatic attack is 20/60/80/79/76/54% at levels 1/10/25/50/75/100, above the desired 15–40% through most progression. Idle remains viable in the event model (83% level-100 normal win rate versus 91% engaged in tuning), but active throughput variance is not yet final. Fireball retains the intended short-burst premium and high cost. With authored 7 Mana/second before gear, common 20–30 Mana skills naturally recover in roughly 3–4 seconds and 100-Mana Fireball in about 14 seconds; event rows preserve actual casts/Mana remaining, while high-level Mana gear frequently removes practical depletion. Manual play must determine whether that late-game abundance is desirable.

Deep Freeze is finalized at a 40% Chill cap with its existing 25% less-effect downside. Shock threshold/stack rules, crit, weapon range, Hit Twice, Poison, Bleed and Ignite mechanics were not redesigned. Focused regression verifies those semantics; the reference event model adds real turn ownership and replacement/stacking behavior but remains an approximation of scene timing.

## Economy and progression interpretation

Using the normal-only XP-equivalent curve and final five-seed P50 engaged TTK interpolated between checkpoints, estimated **combat-only** first-run elapsed time is about 5/19/53/101/177 minutes to levels 10/25/50/75/100. That falls within the requested 5–10/15–30/45–75/90–135/150–210 minute windows. Magic/Rare enemy XP bonuses and five-equivalent bosses reduce required actual kills; boss combat duration, equipment decisions and encounter transitions increase wall-clock time. These estimates are not an instrumented complete run and must be tested manually.

At 75+, the independent ordinary-currency rules produce 0.28 orbs per normal kill on average before bosses. If *every* equipment drop were dismantled, 50% equipment chance and 35/42/4% Magic/Rare/Legendary mix would add about 0.0175 Normal→Magic and 0.025 Magic→Rare full-orb equivalents per kill through fragments; the actual auto/manual dismantle fraction is lower. Currency availability is intentionally above craft-action targets; useful item/side/legal-target opportunities, not mandatory hundreds of rerolls, should bound spent actions. The runner's occasional Normal→Magic and Magic→Rare reference crafts do **not** constitute a full longitudinal 10–25 action-by-50 or 30–70 action-by-100 economy simulation. That remains a manual-playtest/economy risk, especially with under-filled crafted items, one-mod Add/Remove/rerolls and pickup filters.

## Rebirth, relic and post-100 findings

Rebirth stays optional at combat zone 60 and retains the established reset transaction. Random/current-cycle relic sampling produces a median combat-throughput uplift of only 2–3% for one relic and 15–17% for four at levels 25–100 (means 4–5% and 20–22%). That historical Step-13 sample predates relic levels/tiers and starter-weapon relic families; it is retained only as a past baseline, not current proof. A later longitudinal selected-relic playtest is still required.

For a P50 level-100 character against normal enemies, event win rates at combat 100/110/125/150/200 are 91/87/74/41/2% with no relic, 90/88/76/42/2% with one random relic, and 94/92/81/47/2% with four random relics. The first-run wall therefore begins around 140–160 as requested; random mixed relics improve the approach but do not create the broad +30–60 combat-level push intended for four deliberately useful combat relics. Selected combat relic strength and first-Rebirth acceleration remain high-priority manual gates.

## Enemy equipment and build diversity

Enemy equipment remains independent of player loot. At high levels the 1,500-build audit observed rarity 53.7% Normal, 30.1% Magic, 14.7% Rare and 1.4% Legendary, matching the separately normalized 40/20/10/1 weights. Gear-piece count reaches eight; total explicit modifier count across the equipped set averaged 0/8.0/20.9/36.6 by rarity. The optimizer remains 75% offense/25% defense and uses real aggregated combat/stat formulas. The Step 13 enemy item-level cap prevents high-level equipment from compounding at player-tier values while leaving rarity/mod-count logic intact.

| Combat level | Physical | Fire | Cold | Lightning | Void | Crit affix | Hit Twice | Poison / Bleed / Ignite |
|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| 100 | 54.2% | 13.4% | 13.0% | 11.2% | 8.2% | 25.8% | 35.6% | 48.6 / 43.6 / 23.2% |
| 150 | 56.8% | 13.0% | 11.0% | 9.8% | 9.4% | 29.6% | 27.0% | 44.4 / 42.0 / 19.8% |
| 300 | 58.2% | 13.2% | 11.6% | 7.6% | 9.4% | 28.8% | 28.6% | 44.6 / 45.0 / 23.6% |

Shock/Chill capability affixes appeared on roughly 0.8–2.2% of builds. Physical exceeds the 55% preference at levels 150/300, but no single non-Physical family dominates; this slight late Physical bias follows authored weapon baselines and optimizer valuation. It is recorded for playtesting rather than changed after holdout.

## Validation and playtest gates

Final-source EditMode regression passed **248/248** with zero failures/skips (`Logs/Step13-final-correction-editmode.xml`). The suite covers fragments, crafting, schema migration/roundtrip, life/XP, enemy tier/role, crit/ailments/skills, lifecycle, menu/load, CODEX and Pause behavior. The final itemization audit passed **1,312** legal player/enemy/crafting cases with zero errors (`Logs/Step13-itemization-validation.txt`). Missing-reference and scene/prefab wiring validation passed with zero failures (`Logs/BaselineReferenceValidation.txt`). Fresh synchronous scene checks passed PaperBattle, player stats, status and progression; the six-entry lifecycle/Pause soak and Main Menu/New Game/Load/overwrite fixture passed (`Logs/BaselineSynchronousPlayChecks.txt`, `Logs/RuntimeLifecyclePlayChecks.txt`, `Logs/MenuLoadPlayChecks.txt`).

Five final tuning seeds and three untouched holdouts completed on the locked source under `Logs/Balance/`. The separate enemy-diversity audit produced 1,500 rows at seed 13001. A strict Windows x64 build succeeded with zero errors (523 existing warnings), output size 645,431,844 bytes, at `Builds/Step13Windows/BlackCube.exe` (`Logs/Step13WindowsBuild.txt`). That exact executable reached `MainMenuUI` and remained alive/responsive for a 30-second headless smoke (`Logs/Step13-standalone-smoke.log`); the only application message was the known unassigned placeholder Achievements button.

Human playtest priorities are boss TTK/pressure at levels 1/25/75/100, level-100 boss loss rate, Heavy Strike's physical-gear advantage, Lightning Strike and late Ice/Immolate output, late Shiv/Envenom DOT realization, projectile travel and actual Mana decisions, ailment ramp/stack replacement, P90 all-four resistance consistency and suffix opportunity cost, useful-upgrade/dead-drop frequency, craft expenditure and auto-dismantle acceptance, selected combat relic versus random XP/utility relic uplift, the first-Rebirth acceleration and the practical post-100 wall. Step 13 adds no zone, species, encounter table, achievement, minion, final art or Step 14 content.
