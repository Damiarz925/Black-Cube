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

Native visual/manual gameplay validation has not been performed. A fresh Windows build has not yet been performed for this checkpoint.

## Still outstanding

Starting weapons and selection, expanded Relic pool/eight slots/save migration/fusion/Item Return, Rebirth rewards, Relic skill triggers, real-time ailment redesign, Rage redesign, production Unique items and their shared runtime/headless consumers, level-350 Unique Relic forge, and lightweight developer tools. The final authoritative tables and complete handoff depend on those systems being implemented and validated.
