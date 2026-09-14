# Inventory milestone — 2026-09-07

Implemented and tested in Unity 6000.6.0f1 / SampleScene. Unity exited Play; no fixture scene or prefab changes saved. Existing XP, skill tree, combat, progression and unrelated edits preserved.

## Behavior

- Direct click on an inventory gear item still equips it.
- Hover opens a stationary, enterable tooltip. A short unscaled grace period bridges the pointer movement from the slot to the card; it remains open while over the card. Stats scroll if needed, with the Scrap action outside the scrolling area.
- Tooltips show effective damage/attack speed/crit, base weapon values, nonzero local modifiers, and every actual global modifier (including status chances). They do not substitute player totals or invent rolled stats. Base element is labeled separately from status effects.
- Existing weapon pictograms now carry a colored Physical/Fire/Cold/Lightning text badge. Rarity remains separately visible. No new art assets.
- Normal/Magic/Rare dismantle for 1/2/3 Scrap. Scrap occupies one inventory slot with an incrementing count. The item is claimed before notification and delayed destruction to reject duplicate rewards.
- Scrap cannot equip or dismantle. Equipped items are protected: swap them back into inventory first. Legendary dismantling is disabled because its yield was not specified. Crafting remains deferred.
- Inventory has no capacity limit; dismantling the first item replaces its slot with Scrap, requiring no spare slot. Subsequent dismantles shrink the inventory while incrementing that same stack.

## Newly verified checks

Start outside Play in SampleScene. Choose **Black Cube > Play Checks > Verify Inventory**. The disposable fixture runs assertions and then freezes combat for manual inspection. Exit Play when finished; do not save fixture state.

| Result | Check / exact reproduction |
| --- | --- |
| PASS | Unity compilation and focused Play assertions. Raw results: `inventory-check.txt`. |
| PASS | Fixture dismantles Normal -> Scrap1, repeats the same callback before delayed destruction -> still1, dismantles Magic ->3, Rare ->6. Exactly one Scrap item; originals removed. |
| PASS | Attempt to dismantle/equip Scrap -> rejected. Equip a Rare weapon with +100 HP -> modifier applied; try dismantling while equipped -> rejected with stats intact. Swap starter back -> modifier removed and Rare returned; dismantle Rare -> Scrap9 with player stats unchanged. |
| PASS | Fixture checks Cold damage115, attacks/sec1.32, Poison chance17%, and HP100 in tooltip text, sourced from its actual fields/modifiers. |
| PASS | Visual fixture shows Scrap9 plus Fire/Cold/Lightning/Physical icon badges. Fire tooltip displays 115 damage, 1.32 attacks/sec, 5% crit, base/local breakdown, Poison17%, HP100, and +3 Scrap action without overlap or clipping at the current QHD Game view. |
| PASS | Enter the Fire tooltip and click its Scrap action -> Fire item disappears and the same slot displays Scrap12. Click the Cold inventory item -> equips directly; starter Physical level1 returns to inventory. Hover Magic, move into its tooltip, wait, click Scrap -> same slot Scrap14. |
| PASS | Hover Scrap14 -> correct material count, no equip/dismantle action. Close inventory with X -> tooltip and inventory both disappear. |
| PASS | No new inventory exceptions observed in the editor log during the fixture. Existing Unity AI subscription/licensing messages are separate. No scene/prefab diff after Play exit. Scoped changed-file whitespace check passes. |
| NOT RUN | A fixed-capacity/full-inventory case: no capacity mechanism exists. First-slot replacement was verified instead. |
| NOT RUN | Standalone build, save/load across application launches, exhaustive resolutions or large-inventory stress. Earlier progression/combat suite not repeated for these inventory-only changes. |

No newly reproduced failures remain in this bounded inventory pass. Session-only Scrap persistence follows the existing inventory; disk saving was not added.
