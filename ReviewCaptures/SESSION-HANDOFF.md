# Black Cube handoff — 2026-09-05

User intentionally paused work to conserve usage. Status milestone is complete; STOPPED FOR TODAY. Resume queued work only on user instruction. No commit/push. Unity was left outside Play, SampleScene saved without scene dirty marker. Preserve unrelated existing dirty assets, Packages and ProjectSettings.

## Completed and verified

- Forest paper battle art/poses, inventory, equipment/stat panel, rarity glyphs, death menu and damage-number styles. Original 3D sources retained; no corruption work. Popup placement was approved: do not move it.
- Stat model: player base Life 1000, intrinsic/unarmed damage 0, starter weapon damage 80. Derived Life governs initialization/revive/clamping; equipping more Life does not heal. Atomic equipment stat updates prevent transient health clamping. Weapon damage comes from equipped weapon, not an intrinsic player base. Equipment replacement/unequip supported.
- Status icons/counts and hover details on both actors. Actual damaging stacks use averaged final per-stack tick damage, including resistance. Equal intervals use arithmetic mean; mixed intervals use frequency-weighted mean to preserve aggregate ongoing damage per global turn. Raw stack strengths, individual timing and expiry remain; staggered burst totals/lifetime damage may redistribute through averaging. Grouping uses the existing status-effect asset identity. Non-damaging effects show strength/duration, and existing unimplemented gameplay response is explicitly labeled placeholder.
- Death clears status state; HUD rebinds on enemy replacement. Hover remains stable across frames. Reusable procedural flame, poison/bleed drops, frost and shock glyphs; no external art dependency.

Evidence in this directory: stat-model-check.txt, stat-model-baseline.png, status-check.txt, status-player-tooltip.png, status-enemy-tooltip.png, status-chill-tooltip.png. Earlier presentation evidence remains alongside these.

Unity 6000.6.0f1 compiled status changes and actual Play integration passed: 100/50/75 -> three stacks at 75, total 225 damage; expiry -> two at 62.5 and total 125 -> one at 75 -> none. Mixed intervals -> 83.333/tick with 125/global-turn aggregate rate. Enemy damage matches resisted summary. Removal, death, restart and replacement clear state. Visually verified player poison 3 at 75/tick, enemy poison 2 at 15/tick, chill strength 0.2 with no invented damage. Test fixture intentionally freezes Time.timeScale; leaving Play discards it.

Reproduce through Black Cube > Play Checks > Verify Player Stat Model / Verify Status Stacks in fresh Play. Existing rig/DontDestroyOnLoad and deprecated find API warnings are unrelated. Avoid editing C# during Play: existing nonserialized game dictionaries do not survive hot reload. Original verification.md historical Life100/damage20 discrepancy is superseded by these stat-model results.

## Queue 1: stage/boss/zone progression — not started

Forest stages 1–10, then Desert 1–10, then Tundra. Configurable names and ten-stage default, same art including boss allowed. Keep ten normal kills then boss every stage; only boss death advances stage. Guard duplicate death/reward callbacks. Distinguish boss in HUD, independently of rarity. Preserve monotonically increasing enemy level separately from local stage/name. Decide/document behavior beyond final configured zone. Verify normal cadence, stage-10 boss and zone rollover.

Current GameManager.StartZone uses a single increasing currentZoneLevel; OnEnemyKilled currently lacks duplicate guard and generates one loot item. ZoneManager.GetEnemiesToKillBeforeBoss returns 10. BattleManager owns CurrentEnemyAI and replacement. Do not assume progression has been implemented.

## Queue 2: player XP — not started

Cap 100. Configurable Normal/Magic/Rare base XP multiplied by monster level; choose/report sensible rarity defaults. Each kill exactly once, integrate with death guard. Tunable curve in equivalent same-level normal kills: approximately 5*L^p, p=ln(1000)/ln(99), giving about 5 kills for 1->2 and tentative 5000 for 99->100. XP requirement = normalBaseXP * L * kills(L), suitable rounding, double precision, carryover/multiple levels and cap handling. HUD level/XP. Session-only unless an existing save system is found (none identified). No stat bonuses, healing, skill points or enemy rebalance in this phase. Test endpoints, rising equivalent kills, rarity/level awards, multiple levels, cap and duplicate death.

## Queue 3: enemy scaling/gear — not started

- Inspect existing scaling first; avoid double application. Small configurable intrinsic per-level boosts, restrained versus intended player growth. Player level-up bonus magnitude is UNDEFINED; do not invent player stat growth.
- Use the player's eight supported equipment categories, unique slots. Weapon always present; proposed assumption is weapon counts in total, not a ninth slot. Slot count rolled once per enemy and shared across candidate comparisons. Rough targets: level 1 one item; 20 about two; 100 six to eight, approximately linear randomized range.
- Complete candidate loadouts: candidate count roughly level/10, minimum 1 (1/2/10 at levels 1/20/100). Select highest-scoring whole set, not best individual pieces across sets. Prefer fixed anchor weapon across accessory candidates, and document exact approach. Clean up loser gear; it must not become loot.
- Deterministic expected combat score using real combat math without combat RNG/side effects: average normal hit, attack/tick rate, crit, status chance and averaged DOT. Weight weapon synergy strongly, Life/defense less. Normalize relative gains instead of adding raw Life and percentages. Document DOT horizon and defense approximations; no exact global optimum claim. Preserve variety and occasional strong/weak rolls.
- Random monster rarity Normal/Magic/Rare with Rare least common; expose/report initial probabilities. Gear rarity ceiling strict: Normal only Normal; Magic Normal/Magic; Rare any of these three. Increasing level and monster rarity favors highest allowed tier. Level-100 Rare generally fully Rare; level-100 Normal still Normal. Expose/report tier distributions.
- Rare up to 6 explicit mods; Magic up to 4. Normal up to 2 additional mods was FUTURE possibility: preserve current Normal policy now. Keep implicit/inherent modifiers separate, avoid illegal duplicates.
- More loot quantity for Magic/Rare, expose/report defaults. Keep established loot generation rules; do not assume all equipped items drop or candidate gear becomes loot. No new art/corruption.
- Test levels 1/20/100 slot and candidate counts, unique supported slots/weapon, rarity ceilings, seeded statistical tier preference, best-set selection, score sensitivity to synergy, equipped stats/health/actual combat, loser cleanup, affix caps, rarity loot quantity and XP integration. Preserve boss/progression behavior; boss flag distinct from rarity.

## Coordination and files

Origin task: 01a07401-70e2-79b3-b429-fb0e74ec9fee. Send concise milestone/final/block reports using send_message_to_thread when work resumes. No automatic continuation after this pause; do not consume usage resets or purchase credits.

Status implementation: Assets/Scripts/StatusController.cs and StatusController.Display.cs, StatusHUD.cs, StatusBadge.cs, StatusGlyph.cs; PaperBattleHUD adds StatusHUD; HealthComponent clears effects on death. Diagnostic: Assets/Editor/StatusModelChecks.cs. Stat diagnostic: Assets/Editor/PlayerStatModelChecks.cs.

Review copies also belong in C:/Users/david/Documents/Codex/2026-09-05/realtime-voice-chat-2/outputs/black-cube-art/. Workspace is D:/Unity/Projects/Black-Cube. Editor executable D:/Unity/6000.6.0f1/Editor/Unity.exe. Log: workspace Logs/Editor.log.
