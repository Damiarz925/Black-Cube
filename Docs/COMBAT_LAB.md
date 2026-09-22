# Combat Lab

Open **Black-Cube → Balance Workbench** and use **Combat Lab**, **Combat Timeline**, **Batch Matchups**, **Boss Lab**, or **Combat Curves**. Combat Lab never edits production balance. Its player policies are explicit simulation assumptions.

## One fight and replay

Select the current Tooling 2 player snapshot, a production enemy or boss, level, rarity, corruption, seed, and policy, then choose **Run One Fight**. The Timeline retains ordered attacks, skill readiness, Mana, projectiles, damage, ailments, Rage, enemy behavior, boss phases, and deaths. Use the flags filter and click an event for its structured reason, rule, roll, resource transition, projectile index, and stack state.

**Rerun Same Seed** reconstructs the enemy and combat RNG from the saved inputs. **Export Trace JSON** stores the complete result and event list; **Load Trace** opens it without rerunning. A loss includes the last ten damaging events as a death summary.

## Batch fights

Set 10, 100, 1,000, 10,000, or a custom fight count and choose **Run Batch**. Batches retain numeric samples and aggregate metrics, not every event trace. Results include Win/Loss/Timeout/Error rates, full duration percentiles, remaining Life, DPS, Mana starvation, damage source/type, effective healing/overheal, ailments, player/enemy skills, and boss phase time.

Representative mode reuses one exact generated enemy snapshot while varying combat seeds. Population mode generates a production-valid gear snapshot from the derived seed for every fight, then runs the actual combat simulation on pure data. Population preparation is slower because exact Tooling 3 generation intentionally uses the production enemy/optimizer path.

Outlier buttons rerun fastest, slowest, closest, and selected loss seeds with a full trace. A simulation safety failure writes a JSON repro under `Logs/CombatSimulationErrors` with request, player/enemy snapshots, fingerprint, seed, result, error, and retained events.

## Policies

Player Action Policy controls queued skills and Dagger ImmediateCooldown activation. Staff AutoCooldown skills remain automatic. Rage Finisher Policy independently controls whether a ready finisher is never used, used immediately, held for the next/selected skill, or held for a target-Life threshold. **Compare Built-In Policies** runs the same matchup and seed family under each action policy.

Save a `PlayerCombatPolicySO` for reusable assumptions. Save/load experiment JSON includes the player snapshot, enemy configuration, policy, fight count, seed, duration, population mode, corruption, and rarity.

## Matchups, bosses, and curves

- **Batch Matchups** runs the current Tooling 2 build against selected production archetypes and displays clickable Win Rate heatmap rows.
- **Boss Lab** uses the authored boss prefab snapshot, behavior, loadout, phases, corruption, and challenge-boss flag. Timeline phase markers and aggregate phase time reveal phase walls.
- **Combat Curves** runs player/enemy level sweeps with Win Rate, P50/P90 duration, remaining Life, Mana starvation, and ailment share.
- **Rarity Sweep** compares Normal/Magic/Rare/Legendary population gear.
- **Low / Mid / Optimized** generates the three Tooling 2 gear profiles and fights them against the same matchup.
- **Corruption Sweep** evaluates 0/20/40/60/80/100.

Gear Optimizer and Passive Optimizer results expose **Evaluate … With Combat Lab** after their analytical search. Combat simulation is deliberately not placed inside either optimizer's inner loop.

## Exports and stale results

Timeline, batch, matrix, and curve CSV; result/trace JSON; and curve PNG use the normal Workbench export folder and conventions. Results retain the production-data fingerprint. A mismatch is shown as **STALE — PRODUCTION DATA CHANGED**.

## Quick answers

- Build win rate versus a Rare level-80 enemy: **Combat Lab → Batch**.
- Why a fight lost: replay a loss in **Combat Timeline → Death Summary**.
- Staff Mana starvation: **Resource/Skill metrics → Mana delays and ready-starved time**.
- Rage Finisher frequency: **Rage metrics** or **Policy Comparison**.
- Poison share: **Damage Contribution / Ailments**.
- Freeze impact: **Ailment metrics and skipped attacks**.
- Boss phase wall: **Boss Lab → Phase Time and phase markers**.
- Scaling across levels: **Combat Curves**.
- Reproduce an abnormal result: select an outlier and replay its exact seed.

