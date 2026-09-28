# Systems redesign: regression checkpoint

This is a partial implementation checkpoint, not completion of the major systems/content brief.

User-authored UI changes are preserved in commit `2cf3efc6`.

## Implemented

- Production Staff Fireball and Shock Barrage now automatically queue an affordable, enabled, ready skill for the normal attack gauge. They do not fire independently. Each control toggles automatic usage; toggles persist, while transient cooldowns reset on load.
- Spell Echo uses a shared runtime/headless chain rule: fractional chance capped at 60%, successive echo Mana costs at 125%, 150%, 175%, etc. Echoes use the skill damage pipeline without restarting its cooldown.
- Mage generic nodes retain their IDs and now total 40% Reduced Shock Effect, 40% Reduced Chill Effect, and 10% Spell Echo Chance. Former generic Cold Damage choices become Echo.
- Stats refreshes pool rows and headers, avoid unchanged text writes, and adapt row height to long values. Repeated header callbacks do not accumulate.
- Advanced Loot has an authored item/weapon dropdown, modifier selection and legal-tier sliders, implicit/explicit selection, selected-state accents, help, and an auto-dismantle toggle. Disabling auto-dismantle retains rejected pickups. Removed legacy menus have an explicit clear-saved-rules control when old rules remain active.
- Inventory grid DPS arrows are hidden; the item tooltip has the comparison instead. T1 modifier lines receive gold emphasis.
- Death recap preserves critical and ailment tags, including critical damaging-ailment ticks.

## Validation

`Logs/SystemsRedesignRegressionFinalTests.xml`: 41 focused EditMode tests passed, zero failures (2026-09-28). Includes SystemsRedesignRegressionTests, PlaytestFollowUpTests, PlaytestStabilizationTests, and FragmentEconomyTests.

Authoring completed successfully in `Logs/SystemsRedesignAuthoringFinal.log`. Incidental passive-prefab overrides were restored to the user's UI checkpoint. No broad balance simulation, BalanceLab, push, or main-branch modification occurred.

Fresh Windows x64 build succeeded with zero errors and 831 warnings (`Logs/SystemsRedesignWindowsBuild.log`). Player: `Builds/SystemsRedesignWindows/BlackCube.exe`.

Hidden, null-graphics startup smoke reached `MainMenuUI.Awake` without logged exceptions (`Logs/SystemsRedesignWindowsStartup.log`). It logged the existing unassigned Achievements button warning. No character was loaded or created, and the temporary player process was stopped after the check. This is startup validation only, not rendered UI or gameplay validation.

Native visual/manual gameplay validation has not been performed.

## Focused manual checks

1. Open a Mage character with a Staff. Toggle Fireball off and Shock Barrage on, then reverse them. Confirm only enabled skills replace normal attack-gauge actions; disabling both must leave basic attacks. Hover both controls and verify their tooltips. Save, exit and load to verify toggle persistence.
2. Spend Mana below both skill costs. A ready skill must wait without spending Mana or restarting its cooldown. When Mana recovers, it must replace the next attack, not fire between gauge events.
3. Inspect the Mage generic passive branches. Reduced Shock and Chill nodes should show 8% each, and former generic Cold Damage choices should show 2% Spell Echo each. Existing allocated node IDs remain valid.
4. Keep Wanderer Stats open during combat and regeneration. Confirm rows remain stable without flashing or overlap. Expand/collapse Advanced Sources repeatedly; each click should toggle once.
5. Open Advanced Loot from Inventory. Select an item/weapon type, a modifier, its implicit/explicit mode and a legal minimum tier. Check horizontal space, scrolling, selected accents and Help. If a saved legacy rule is present, clear it explicitly using the visible legacy-rules control.
6. With filters rejecting an item, disable automatic dismantling and pick it up: it must remain in Inventory. Enable automatic dismantling and repeat with an unlocked item: the existing fragment rewards should apply. Locked items remain protected.
7. Hover an inventory item: the DPS comparison should appear in the tooltip's upper-right area, never on the grid icon. T1 modifier lines should be gold without recoloring the whole card.
8. After a death involving a critical hit or damaging ailment, inspect the recap. It should retain five events, mark Crit/Ailment where applicable and leave the killing event last.

## Still outstanding

Starting weapons and selection, expanded Relic pool/eight slots/save migration/fusion/Item Return, Rebirth rewards, Relic skill triggers, real-time ailment redesign, Rage redesign, production Unique items and their shared runtime/headless consumers, level-350 Unique Relic forge, and lightweight developer tools. The final authoritative tables and complete handoff depend on those systems being implemented and validated.
