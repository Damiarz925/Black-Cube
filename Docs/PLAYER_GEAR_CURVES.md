# Player Gear Curves and Scenario Sweeps

The three assets in `Assets/Balance/Profiles` are editable simulation models:

- `SO_PlayerGearProfile_Low`
- `SO_PlayerGearProfile_Mid`
- `SO_PlayerGearProfile_Optimized`

They do not change production drop rates, rarity weights, affix weights, or live loot. Edit candidates per slot, strategy, target percentile, retained candidates, beam width, and rarity policy in the Inspector. Crafting and endgame-only modifier access default off. Increment `version` when intentionally changing a profile; results store the asset GUID, version, seed, and production fingerprint.

Current authored values: Low generates 3 candidates/slot and chooses Random Legal; Mid generates 10, retains up to 5 per slot, and uses Best Per Slot (whole-set beam width 1); Optimized generates 30, retains up to 10 per slot, and uses a whole-gearset beam of 100. All three use natural rarity and disallow Empowered/Boss-special modifiers. Low and Mid turn natural Legendary results into Rare; Optimized permits them. Their serialized min/max rarity fields are bypassed while natural rarity is on. Target percentile is only active in the Percentile Target strategy. `selectionImperfection`, `allowLegalCrafting`, and `allowDeepEndgameImplicitRepair` are serialized profile fields but are not consumed by the current sweep optimizer; do not treat them as active player acquisition/crafting behavior. The Workbench displays this warning beside the profile comparison.

`Random Legal` takes the first deterministic legal candidate. `Percentile Target` selects the configured objective percentile. `Best Per Slot` performs a fast greedy search. `Full Gearset Beam Search` retains several partial whole sets, so cross-slot interactions can beat independently attractive items.

## Worked Warrior example

1. Open **Gear Curves** or **Scenario**.
2. Choose Warrior, Sword, Primary **Physical Hit DPS**, Secondary **Attacks per Second**.
3. Set player levels 10–100, step 10, and choose the three profiles.
4. In Scenario, enable **Optimize Passives**. Independent mode solves each level from scratch. Progressive mode carries the prior build forward; disable free respec to preserve its passive choices.
5. Run the profile comparison. Each graph point retains exact gear, passives, metrics, optimizer inputs, fingerprint, and seed. Click a row to reopen it in Player Build.
6. Export the sweep CSV, result JSON, or current curve PNG.

To compare against enemies, first generate the desired archetype/rarity dataset in Enemy Gear Lab, then open **Player vs Enemy**. P50/P90 TTK uses enemy Life divided by compatible player basic DPS; TTD uses player Life divided by enemy expected DPS. They are explicitly neutral analytical estimates, not simulated combat. Normalized mode indexes the first point to 100 to compare scaling shape without mixing raw DPS and Life on one axis.

The authored Low/Mid/Optimized gear curves remain designer scenarios, not historical-player inventories. For an acquisition-constrained level-50 build, use **Progression History** to model encounters, actual drops, shared currency, optional expected crafting, and final whole-inventory selection. The Defense Calibration resistance audit derives T1 unlocks and earlier-tier reference pressure from the live affix data; it does not rewrite these gear profiles or reserve affixes.

## Exact sweep semantics

The default **Combat Level Policy** is Match Player Level: 10→50 by 10 produces L10/CL10 through L50/CL50. Fixed keeps the selected CL constant. Offset adds the chosen signed offset to every Player Level. Advance from Start starts at the selected CL and advances by each Player Level increment; this is the explicit legacy mode used when loading older presets that lack a policy field. The selected CL control appears only when that policy needs it.

One point represents **one deterministic generated gearset** for one profile at one level and seed; the old “Samples / Level” control did not multiply the sweep and is no longer shown. There is no hidden 100-sample average. Progressive Character starts the next point from the prior build, but re-generates the unlocked gear slots using the next level/profile. It does **not** carry inventory, currency, or acquisition history. With free respec disabled, the prior passive allocation is preserved and optimization may add points. With free respec enabled, the next point may rebuild its passive choices. For inventory/currency history use Progression History.

Open the **Estimated Workload** foldout before an expensive run. The point inspector uses its stored build/objective/metrics and verifies integrity hashes; changing top-level controls afterward warns that the sweep is stale rather than relabeling its points. If the production-data fingerprint changes, rerun the sweep before trusting a drill-down. The CSV exports the objective captured with the sweep, not the current UI selection.
