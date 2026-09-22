# Enemy Behavior Authoring

Production behavior lives in reusable profiles inside `Assets/Resources/GameData/WorldContentDatabase.asset`. Profiles reference reusable skill IDs. Runtime `EnemyAI` and Behavior Preview both call `EnemyBehaviorResolver`, so the tool has no separate selection model.

## Resolution semantics

Migrated profiles keep **Preserve Legacy Rotating Cadence** enabled. This preserves Step 19 exactly: scan from the rotating loadout index, choose the first skill whose cadence matches, otherwise use the rotating fallback. No migrated rule, skill, or balance value was redesigned.

For authored rule resolution, the resolver gathers condition-valid rules, rejects cooldown/use-limit failures, keeps the highest priority, then performs deterministic weighted selection among equal-priority candidates. Weight is relative only within that priority. Cooldowns and minimum-attacks gates are action based. Once Per Encounter and Maximum Uses are recorded in encounter state. If no normal rule resolves, an eligible fallback is selected; validation warns when none exists. Weighted choices use injected encounter RNG, never loot RNG.

Supported generic conditions are Always, First Action, Every/After N Attacks, self/player Life above or below a threshold, player/self ailment state, player missing an ailment, cooldown ready, boss phase, corruption minimum, hits taken/dealt, once per encounter, and weighted random. These are authoring capabilities only; production profiles retain their migrated legacy behavior until explicitly changed.

## Editing and diagnosis

Rules can be added, duplicated, reordered, copied/pasted, or deleted in staging. Each rule exposes skill, stable rule ID, condition inputs, priority, weight, cooldown, minimum attacks, once/max uses, and fallback eligibility. **Preview Diff Unsaved** means production has not changed. Applying validates first, warns how many enemies/bosses share the profile, asks for confirmation, records Undo, saves the authoritative asset, and invalidates cached results.

Behavior Preview controls enemy/player Life, ailments, hits, phase, corruption, seed, and 1–50 actions. Every timeline entry records the selected skill/rule, reason, eligible and rejected rules, condition outcomes, priority, weight, and RNG value. Click an action for **Why This Action?**; export the complete deterministic timeline to CSV.

Validation reports missing skills, missing/duplicate IDs, invalid Life ranges, negative cooldowns, invalid weights/cadence/hit counts/corruption, missing phase references, no fallback, and incompatible or unsupported state. It warns rather than silently repairing data.

## Shared profiles, phases, and challenge bosses

**Used By** identifies all archetypes and bosses affected by a profile. **Duplicate Profile For Selected Enemy** creates a unique copy and reassigns only that enemy. Existing profiles are the behavior-template library: duplicate a current production pattern, then stage edits.

Boss / Phase Authoring edits the actual phase list: ordered Life threshold, loadout, mechanic ID, and damage multiplier. It validates a 100% opening phase, unique valid thresholds, loadouts, and positive multipliers. Challenge bosses reuse these phase assets and the 0/20/40/60/80/100 corruption modifiers; the resolved panel displays cadence, mechanics, and apex data without creating six enemy copies.
