# Deferred progression tests — simpler-model pass

This is an outline, not authorization to start another run automatically. Progression implementation is complete; stop after this milestone. Do not implement XP, enemy scaling, art, or unrelated refactors. Preserve existing changes. Report PASS/FAIL/NOT RUN with exact reproduction steps; fix only separately authorized failures.

Already passed: Unity compilation and the focused Play diagnostic in Assets/Editor/ProgressionChecks.cs;10 normals -> boss; boss-only single advancement; duplicate callbacks produce no extra loot/count/replacement; Forest10 -> Desert1; Desert10 -> Tundra1; player death/restart preserves stage and resets progress; live callbacks rejected. Normal/boss HUD visually inspected. Evidence: progression-check.txt, progression-boss-hud.png. Do not repeat these unless changes justify it.

Remaining checks, one bounded pass each:

| Check | Steps | Expected result |
| --- | --- | --- |
| Sustained combat | Run natural combat across three consecutive stages, using disposable test-only health/damage overrides if needed; restore afterward. Include a DOT lethal hit if practical. | No skipped/double stage, one active enemy, correct10-normal cadence each stage; new enemy does not inherit the killing hit's statuses. Record any existing combat lifecycle issue separately. |
| Boss death/restart | Die during a boss encounter; restart, then kill one normal. | Same zone/local stage and global combat level; count resets0 then1; boss flag clears; player revives through existing health logic. |
| Configuration | In disposable Play state, set two custom zone names and3 stages per zone; inspect global levels1,3,4,6,7. | Local stages1,3,1,3,1; names first,first,second,second,second. Global level unchanged by presentation mapping. Last name repeats after list exhausted. |
| Invalid configuration | In disposable state, empty the zone-name list; try blank entry and nonpositive stage count. | Safe fallback Forest/Zone N; divisor clamped to1; no exception. Restore defaults. |
| UI regression | Open inventory/stats before and after a transition, then exercise death menu. | Correct panels/buttons, no overlaps or stale actor status badges; boss label separate from rarity. Forest art remains intentional for all zones. |
| Cleanup | After sustained run, allow delayed destruction to finish; inspect enemies/effects and new Console errors. | One current enemy; no duplicate rewards or lingering dead-enemy UI; distinguish known rig/DontDestroyOnLoad/deprecated-find warnings from new failures. |

Use fresh Play for isolated fixtures. Do not modify C# while playing (existing nonserialized dictionaries do not survive domain reload). Exit Play after checks and discard fixture-only scene changes. Summarize only newly verified behavior and failures; no exhaustive suite or repeated screenshots.
