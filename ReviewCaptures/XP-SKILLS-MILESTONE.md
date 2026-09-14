# XP and starter skills milestone — 2026-09-07

Implemented and verified in Unity 6000.6.0f1, SampleScene. Unity exited Play; no fixture scene changes saved. Existing project/package/settings changes preserved. No art or enemy scaling redesign.

## Newly verified progression checks

| Check | Result | Reproduction and evidence |
| --- | --- | --- |
| Sustained combat | PASS after scoped fix | Outside Play, open SampleScene, choose **Black Cube > Play Checks > Verify XP Skills and Combat**. Its natural segment runs Forest1 through Forest3 (33 kills) at 12x time, with the actual 1000 HP, 80-damage/1.2 attacks-per-second starter weapon, no health/damage overrides and no allocated skills. Correct 10 normals then boss each stage; global4/count0 at finish. Separate lethal hit and lethal DOT fixtures verify original-target binding and turn termination. |
| Boss death/restart | PASS | **Run Deferred Pass** runs a fresh Forest5 boss fixture: die, restart, verify Forest5/count0/nonboss/full HP, kill one normal -> count1. XP diagnostic additionally covers Forest1 and Forest5, asserting XP, level, available points and purchased skills survive restart. |
| Configuration | PASS | **Run Deferred Pass**, fresh fixture: names Alpha/Beta, 3 stages; StartZone at 1,3,4,6,7 -> Alpha1,Alpha3,Beta1,Beta3,Beta1. Global combat levels unchanged. |
| Invalid configuration | PASS | Same command, separate fresh fixture: empty names with stages0 and -3 -> Forest/stage1 at global7; blank name -> Zone1/stage1. Play exit restores defaults. |
| UI regression | PASS after panel fix | Fresh Play: open Inventory and Stats, observe Forest1 -> Forest2 transition, die and click Restart Level. Original run showed stats/death and enemy-strip/stats overlap. Final follow-up: **Preview Skill Screen**, Return to Battle, open Stats, **UI Death Fixture**, then click Restart Level. Panels/strips hide on death; restart full HP at same stage, retaining level/XP/points. Skill button opens full-screen draggable tree; clicking Force spends one point, increases rank and unlocks Might. Root and all six nodes visible initially. Boss role text remains separate from rarity; existing forest art retained. |
| Cleanup | PASS | XP diagnostic waits 1 second at normal time after sustained run: exactly one EnemyAI, exactly 33 loot additions for 33 kills, no new logged errors. Live UI shows no dead-enemy status badges; damage popups retain their existing lifetime behavior. |

The initial pre-fix sustained fixture found status inheritance on 32 observed replacements. Its loop stopped before observing the final boss replacement, so its printed `32 kills / 33 rewards` was a **test observer off-by-one**, not evidence of an extra reward. The loop and reward assertion were corrected. The post-fix XP diagnostic independently observes all 33 kills and verifies 33 rewards. Raw initial output is retained in deferred-progression.txt for provenance.

## Changes and prototype balance

- PlayerProgression lives with GameManager. Valid enemy deaths award XP inside the existing one-time reward claim. Default XP = 10 × global enemy combat level; bosses multiply this by 2. Rarity currently does not change XP.
- Next-level XP = 30 + 2 × (player level − 1), tunable in PlayerProgression. Level cap100. Carryover and multiple levels supported; invalid/negative XP ignored; cap discards overflow and awards no further points.
- Every level-up adds one skill point and fills a living player's derived maximum HP. It does not resurrect a dead player behind the death menu. Encounter restart uses existing revive logic and preserves XP/skills; StartNewRun resets progression. Session-only; no disk save introduced.
- Six nodes, five ranks each, one point/rank: Vitality +100 HP, Force +15% increased damage, Tempo +5% attack speed; Endurance +150 HP requires Vitality, Might +20% increased damage requires Force, Momentum +8% attack speed requires Tempo. One rank in a parent unlocks its child. Bonuses use existing StatsComponent modifiers, independent of gear. HP nodes increase capacity without healing immediately.
- Full-screen 2400×1400 draggable tree area leaves room for expansion. Combat pauses via BattleManager while the screen is open; existing time scale remains intact. Available points and current XP/level are visible. Maximum allocated points currently30; extra points remain for future tree expansion.
- BattleManager captures the original combat actor, stops after lethal status ticks, and applies on-hit effects only to a surviving original target. This fixes the reproduced successor-status carryover and prevents attacks after DOT death.

Observed normal combat outcome: **33 kills, zero deaths, 16 level-up heals, player level17, minimum observed HP713.4/1000**. Earlier observed enemy hits were roughly 27–60 HP with an occasional ~105 hit; most enemies take a few starter-weapon hits. Frequent early heals leave a generous margin. This is one bounded live run, not a statistical survival guarantee or late-game balance validation. Level100 stops granting heal-on-level-up; balance beyond the tested early stages remains deferred.

## Verification details

**PASS:** Unity compilation; XP duplicate protection; partial XP does not heal; third level1 kill heals/levels; one point per level; prerequisites/invalid nodes/no overspending/rank cap; skill effects reach real damage/attack-speed/maximum-HP stats; multiple levels and exact carryover; NaN/infinity/negative XP; level100 cap; Forest1/5 restart preserves progression; lethal-hit and lethal-DOT lifecycle; natural combat/rewards/cleanup; live skill purchase; skill pause for 2 seconds at Time.timeScale1; final skill layout and death/restart UI.

**NOT RUN:** long-duration/late-game survival, statistical death-rate distribution, save/load across application launches, standalone player build, exhaustive UI resolutions. Return-to-main-menu and quit buttons were visually inspected but not activated during this bounded pass.

The standalone `dotnet build --no-restore` route was unavailable because Unity-generated project.assets.json was absent. Actual Unity compilation and Play tests succeeded. Existing rig/DontDestroyOnLoad/deprecated-find warnings and Unity AI NoSubscription messages are distinct from combat failures; the diagnostic recorded no new gameplay errors. Repository-wide whitespace check flags pre-existing generated solution/settings whitespace; changed gameplay scripts pass it.

Evidence: player-progression-check.txt and deferred-progression.txt. Diagnostics leave disposable state paused where documented: exit Play afterward. No need to rerun the old focused progression suite absent a relevant change.
