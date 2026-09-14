> Historical results: the additive-more rule in this milestone was superseded on 2026-09-08. See MORE-DAMAGE-MILESTONE.md for the current independent-roll formula and validation.

# Compact inventory, pickup filter, and damage milestone

2026-09-07, Unity 6000.6.0f1. Final state: outside Play; no scene/prefab fixture saved. Existing unrelated changes preserved. No commit.

## Results and reproduction

- PASS: compilation after the final changes (Assembly-CSharp-Editor rebuilt at 01:24; no C# errors).
- PASS: from SampleScene, outside Play, choose **Black Cube > Play Checks > Verify Inventory Grid Filter and Gear Damage**. This enters fresh Play, runs disposable fixtures, writes `ReviewCaptures/inventory-grid-check.txt`, and leaves a paused visual fixture. Latest report has 185 PASS assertions and no FAIL.
- PASS: real ModDatabase tier-1 midpoint GenericDmg/GenericMult rolls on both rings and helmets, individual equip/removal, combined equip and swaps. Preview, real attack context, neutral combat calculation, and immediate stats-panel damage agree. Increased25+20 gives145; more20+30 gives150 (not156); increased50 with more20 gives180. Matching generic/elemental more bonuses sum. Physical/Fire/Cold/Light/Poison/Void each give225 for increased25+25 and more20+30 on base100.
- PASS: pickup-only filter defaults both OFF; inclusive level and each rarity threshold; OR behavior; disabled threshold ignored; enabling is not retroactive; equipment returns bypass filtering; equipped gear and Scrap protected; duplicate pickup/dismantling produces no second reward. Normal/Magic/Rare/Legendary yield1/2/3/5 in one stack.
- PASS: earlier live UI pass in this milestone, using the diagnostic's populated fixture: left-click the first +10.5% increased Magic ring; damage100->110.5 and ring slot updates. Right-click the next +5% more Magic ring; cell disappears, Scrap18->20, equipped ring/damage unchanged. Scroll to bottom; hover LV63 Legendary glove, move into tooltip, click Scrap+5; scroll back up to see Scrap25. Filter controls toggled independently, level10->11 and rarity Legendary->Rare, with inclusive labels and OR explanation. Cells show pictograms/rarity and LV only; scrolling reaches LV67.
- PASS: final marker polish visually inspected using **Black Cube > Play Checks > Preview Compact Inventory** from outside Play. Six weapon markers render white/orange/cyan/yellow/green/purple diamonds separately from rarity-colored glyphs; Scrap is a gray pictogram with x2 instead of LV. No missing-font marker boxes.
- PASS (follow-up resolves paused-fixture uncertainty): enter fresh NORMAL Play using the toolbar, not a diagnostic menu. Click INVENTORY, then FILTER: panel opens on the first native click, showing OFF / Level10 and below and OFF / Magic and below. Click level toggle (ON), plus (Level11), rarity toggle (ON), right arrow (Rare). Click level toggle again (OFF while rarity stays ON), then rarity toggle (both OFF). Every action and refreshed label was visually verified during active natural combat. Scrap accumulated x5->x9->x14->x22->x27 with filtering enabled. No UI code change was needed; the earlier nonresponse was limited to timeScale0 diagnostic sessions using injected input, not reproduced during normal Play. Exited Play afterward without saving session state.

## DOT verification and corrections

Already correct: source uses the outgoing, scaled hit; bleed Physical, poison Physical+Poison, ignite Fire; unrelated Void component is excluded. Generic DOT more plus matching ailment more already shared a summed bucket. Existing production coefficients remain bleed0.5, poison0.1, ignite0.8, with base two ticks and intervals1/4/2 respectively.

Corrected demonstrated inconsistencies:

- Percent accessors already returned fractions, but AilmentCalculator divided increased/more by100 again. It now applies `(1 + increased sum) * (1 + more sum)` to the eligible scaled hit times the configured coefficient.
- Extra duration previously divided the same total across additional ticks. Tick strength now divides by the configured BASE tick count, so duration adds equally strong ticks without changing baseline balance.
- Gear's legacy Multiplicative operations now add percentage values for damage-more stats, including DOT/ailment more stats; generic and matching elemental hit-more bonuses also sum before applying once. This supersedes the earlier compounded-more interpretation.

PASS for all three production damaging ailments: baseline, hit scaling alone (+50 increased/+20 more), ailment scaling alone (+20/+30 increased, +20 ailment more/+30 generic DOT more), both layers, and +2 duration independently. Tests apply each status through StatusController, advance its real tick schedule, verify actual health loss, and verify expiration with no remaining summary. Examples:

| Ailment | Baseline tick / total | Both layers tick / total | +2 duration tick / total |
| --- | --- | --- | --- |
| Bleed, eligible base100 | 25 / 50 | 101.25 / 202.5 | 101.25 / 405 |
| Poison, eligible base150 | 7.5 / 15 | 30.375 / 60.75 | 30.375 / 121.5 |
| Ignite, eligible base100 | 40 / 80 | 162 / 324 | 162 / 648 |

No arbitrary coefficient buffs or chill/shock gameplay added. Existing local weapon-base calculation and mixed-flat behavior preserved. Existing Poison/Void hit-stat mappings still fall back to Physical; the six-type check verifies consistent bucket arithmetic under those existing mappings, not new dedicated Poison/Void hit stats. GenericDot has no dedicated production asset in this check.

## Cleanup and limits

Exited both final Play fixtures with the toolbar. No scene changes saved, no C# edited during Play. Test health/damage/items/statuses disappeared with Play exit. Focused log segment contained no new gameplay exception/FAIL; Unity AI NoSubscription and licensing404 remain unrelated existing editor errors. Paused diagnostic damage popups were fixture artifacts and discarded on exit.

The older AttackStatsChecks expected arithmetic was updated for summed more, but that older diagnostic was NOT RUN again; its historical raw report is not final-formula evidence. Previously completed progression/XP/skills suites were not repeated here. No long soak, build/platform coverage, or save persistence claimed. Filter settings are deliberately session-only.
