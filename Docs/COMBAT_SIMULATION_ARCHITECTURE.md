# Combat Simulation Architecture

## Boundaries

`CombatSimulationCore.cs` is runtime-safe pure state and sequencing. It contains no Editor API, scene lookup, Animator, UI, coroutine, or `Time.deltaTime` dependency. The Editor layer materializes Tooling 2 and Tooling 3 inputs once, captures immutable `CombatantSnapshot` data, and then runs fights without scene or GameObject allocation.

The simulator calls shared production services instead of Editor copies:

- `CombatCalculator` for Armour and resistance mitigation.
- `WeaponMechanicProfile` for projectile cadence/travel conventions, Precision profiles, Rage generation, decay, sustained effects, and Finisher multiplier.
- `EnemyBehaviorResolver` for rule selection and traceable behavior reasons.
- `BossPhaseResolver` for authored Life-threshold phases.
- `BattleManager.CalculateChillSlow` for Chill magnitude.
- `PlayerSkillController`, `PlayerSkillDefinition`, `WeaponSkillBindings`, and `SubclassBalanceProfile` during snapshot construction/mechanic resolution.
- `PlayerBuildEvaluation` for exact Tooling 2 initial player stats.
- Tooling 3 exact production generation and optimizer output for enemy initial state.

`BattleManager.RollOverflowApplications` and runtime `RageState` delegate their deterministic calculations to the same shared rules used by headless combat. Changes to those services, skills, passives, subclasses, enemy data, or snapshots therefore change Combat Lab results and the fingerprint.

Presentation-only runtime behavior—animation, sound, damage-number placement, and rendered projectile transforms—is intentionally outside the headless boundary. Logical projectile launch, stagger, travel, impact, and dead-target fizzle remain scheduled events.

## State and scheduler

Each run constructs isolated player/enemy state: Life, Mana, Rage/decay, combo/opener flags, cooldowns, enemy behavior history, boss phase, DOT instances, Shock instances, Chill/Freeze, and queued events. The stable `(time, insertion order)` scheduler jumps to the next event rather than stepping frames.

Important events are attack-ready, skill-ready, projectile launch/impact, ailment tick/application, behavior decision, Rage, Mana, healing, boss phase, status, death, and safety errors. DOT ticks reschedule themselves. Staff and Dagger cooldowns are independent; a Staff skill that cannot pay Mana remains ready and retries without restarting its cooldown.

The simulator terminates on player death, enemy death, configured duration, or safety cap. Maximum events and maximum same-timestamp events protect the Editor from loops. All state is checked for finite Life/Mana/Rage and legal nonnegative resources. Safety failures become `SimulationError`, never a win or timeout.

## Determinism and batches

`DeterministicCombatRandom` owns each fight's stream. `CombatDeterministicRules.DeriveSeed(baseSeed, fightIndex)` uses a SplitMix-style hash, not `seed + index`. Same snapshots, config, seed, and production fingerprint reproduce the complete logical trace.

Single fights retain the configured trace limit. Batch fights retain aggregates, compact numeric samples, and outlier seeds; an outlier is rerun only when requested. Sequential execution is intentional because snapshot preparation touches Unity objects and ScriptableObjects. The pure fight loop has no GameObject allocation and supports 10,000-fight performance checks.

## Parity verification

Focused Tooling 4 tests cover deterministic replay, gauge ordering, queued skills, Staff starvation, Dagger policy, projectile scheduling, damaging ailments, multiplicative multi-Shock, Freeze skips, Rage policies, enemy behavior, boss phases, timeouts, safety caps, distribution percentiles, and Tooling 2/3 initial-state parity. Shared service calls provide the controlled runtime/headless parity boundary for mitigation, Chill, proc overflow, Rage, behavior, phase, skill, and snapshot rules.

`Tooling4ValidationRunner.RunPerformance` records pure 100/1,000/10,000-fight timings. `RunSmoke` exercises a production enemy, exact replay, 1,000-fight batch, outlier replay, Staff starvation, Rage policy comparison, boss batch, matrix, and exports.

