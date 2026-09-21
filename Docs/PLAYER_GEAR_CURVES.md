# Player Gear Curves and Scenario Sweeps

The three assets in `Assets/Balance/Profiles` are editable simulation models:

- `SO_PlayerGearProfile_Low`
- `SO_PlayerGearProfile_Mid`
- `SO_PlayerGearProfile_Optimized`

They do not change production drop rates, rarity weights, affix weights, or live loot. Edit candidates per slot, strategy, target percentile, retained candidates, beam width, and rarity policy in the Inspector. Crafting and endgame-only modifier access default off. Increment `version` when intentionally changing a profile; results store the asset GUID, version, seed, and production fingerprint.

`Random Legal` takes the first deterministic legal candidate. `Percentile Target` selects the configured objective percentile. `Best Per Slot` performs a fast greedy search. `Full Gearset Beam Search` retains several partial whole sets, so cross-slot interactions can beat independently attractive items.

## Worked Warrior example

1. Open **Gear Curves** or **Scenario**.
2. Choose Warrior, Sword, Primary **Physical Hit DPS**, Secondary **Attacks per Second**.
3. Set player levels 10–100, step 10, and choose the three profiles.
4. In Scenario, enable **Optimize Passives**. Independent mode solves each level from scratch. Progressive mode carries the prior build forward; disable free respec to preserve its passive choices.
5. Run the profile comparison. Each graph point retains exact gear, passives, metrics, optimizer inputs, fingerprint, and seed. Click a row to reopen it in Player Build.
6. Export the sweep CSV, result JSON, or current curve PNG.

To compare against enemies, first generate the desired archetype/rarity dataset in Enemy Gear Lab, then open **Player vs Enemy**. P50/P90 TTK uses enemy Life divided by compatible player basic DPS; TTD uses player Life divided by enemy expected DPS. They are explicitly neutral analytical estimates, not simulated combat. Normalized mode indexes the first point to 100 to compare scaling shape without mixing raw DPS and Life on one axis.

