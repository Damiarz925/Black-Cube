# Early progression, Unique tiering and Rebirth handoff

## Scope and numerical reference

The complete XP 1–10 table, all thirteen Unique definitions across all five roll bands, representative weapon base damage/APS/crit at levels 1/20/40/60/80/100, and first-two-tier class choices are in [EARLY_PROGRESSION_NUMERICAL_TABLES.md](EARLY_PROGRESSION_NUMERICAL_TABLES.md). `EarlyProgressionAuthoring.Report` regenerates those tables from production definitions; it does not run combat simulations.

XP requirements use multipliers 12.5%, 20%, 28%, 38%, 50%, 62%, 74%, 85%, 94%, 100% for levels 1–10. Enemy XP rewards, enemy strength and global XP multipliers are not reduced with the requirements. Level 10 onward retains the old requirement curve. Existing schema-15 saves retain earned XP; if that XP now crosses an early level threshold, migration grants the corresponding levels and passive points once.

## Early sustain

| Class | First accessible Life sustain | Passive tier |
|---|---|---:|
| Warrior | Life on Hit, 3.7 per qualifying hit | 2 |
| Barbarian | Life regeneration, 0.285% Maximum Life/sec; Life on Kill follows | 1 / 2 |
| Ranger | 2% of actual Poison damage leeched as Life; Poison Chance available at tier 2 | 1 / 2 |
| Thief | Life on Kill, 71.5 | 1 |
| Mage | Mana Ward and Recovery: 7% damage taken from Mana before Life, 0.75% Maximum Life/sec | 1 |
| Priest | 10% increased Life recovery, scaling existing Restorative Strike; Mana regeneration supports repeated healing; Life on Kill follows | 1 / 2 |

Mage's Mana Regeneration moved to the prior tier-2 Mana Ward location; Priest's Void Resistance moved to the prior tier-2 recovery location. Stable slot IDs, choice counts, subclass choices and manually authored layout remain unchanged. Other classes retain their existing sustain choices. No generic Life-on-Hit package was stamped onto all classes. Starter equipment has not been globally buffed.

## Unique drop audit and actual changes

Before this pass, `LootManager.RollItemRarity` already used a separate random Unique roll: `min(5%, 0.25% × (1 + max(0, active relic Item Rarity + development Item Rarity)))`, provided item level was at least 20. A failed Unique roll fell through to the ordinary Normal/Magic/Rare/Legendary weighted roll. Ordinary Legendary base weights run from 1% early to 4% late and also receive rarity bias. There was no ordinary boss or story-milestone guarantee to remove. Developer creation and special Unique Relic forging are separate, deliberate systems.

Changes: remove only the level-20 drop gate; every normal Unique is eligible at item level 1–100; keep the random roll, cap and rarity influence; add numerical roll bands. Uniques are protected before both advanced and saved legacy automatic-discard rules. Manual dismantling retains the existing deliberate unlocked-item path.

New Unique rolls carry `rollVersion=1`. Existing legacy Uniques retain their immutable old values and validate under the legacy range rules; they are not rerolled on load. Supporting numbers scale by bands 1–19, 20–39, 40–59, 60–79, 80–100. Signature Rage, Bleed and Multistrike affixes have a range floor above the best same-level ordinary affix. Spell Echo currently has no ordinary gear affix; Stormcaller's entry range is nevertheless at least 10–20 points, above a single Staff Echo choice. Defining switches, aura count, less-hit penalties, ailment-basis multipliers, Hydra ailment multiplier, self-Shock behavior and Echo self-damage remain unchanged. Prism's aura penalty/bonus and other supporting numbers retain wide immutable roll ranges.

Unique weapon bases reference the highest unlocked natural tier rather than the old level-independent starter-like bases. Their baseline includes a partial local-offense budget; authored local increased damage is normalized around its band midpoint to avoid double-counting that budget. Axe retains its slower/heavier identity. A Unique is not automatically a perfect six-affix Legendary. Comparisons in the numerical reference distinguish natural weapon base values from local offense and special-mechanic contributions; bare tooltip base damage alone is not the complete build DPS.

## One-way Rebirth

1. Request Rebirth at combat level 60+; cancellation is allowed only before confirmation.
2. Confirm permanently enters setup. Grant all earned relics immediately, using the existing 60/100/140/180/220/260/300/340 thresholds (1–8 rewards), rarity bias and relic-level rules. The level-350 forge opportunity remains.
3. Crafting phase: retain all earned Ancient currency. Open Relic Inventory, craft legal current-cycle relics, then reopen CONTINUE REBIRTH SETUP. Earlier relics stay owned but cannot receive Ancient crafting. Relics have no Crafting Potential.
4. NEXT permanently closes Ancient crafting and enters Equipment. Choose up to eight active relics through the existing relic inventory/equipment controls; at most one Unique Relic can be active.
5. NEXT locks the loadout and enters Returns. Each active return modifier supplies one maximum-item-level slot. Click to cycle eligible owned/equipped items; drag an inventory item or right-click to clear. Empty slots are legal. Duplicate, unowned and over-level selections are rejected.
6. NEXT enters Weapon. The dropdown offers only the class default and active relic weapon unlocks.
7. Resolve starting element using the existing deterministic class-default/active-relic override rules.
8. Review active relics, return items, weapon and element; BEGIN NEXT RUN is the final action.
9. Only now reset ordinary gear/run currency and player progression, recreate selected returns, create the selected starting weapon, and start combat level 1. Empowerment Catalyst retains its existing carryover rule. All relics remain uncraftable throughout the new run; equipment cannot be changed mid-run.

The setup phase, reached level, return item identities and weapon choice are saved per character in schema 16. Reloading setup does not grant rewards again or restart the old run. Combat and relic-trigger dispatch are paused/guarded during setup. HIDE SETUP only hides the wizard; it cannot cancel Rebirth. Normal pristine-relic fusion remains an explicitly designed transformation, now limited to the crafting phase in production gameplay.

## Relic-triggered skills

All listed effects attempt their associated skill after a successfully resolved root player attack, if a living target and sufficient Mana remain. They are cross-weapon, retain normal effective skill-level Mana costs, and do not acquire a new skill-cooldown requirement. Failed casts refund their reserved Mana. Echo's existing additional-cost rules remain.

| Relic modifier | Skill | Base Mana cost |
|---|---|---:|
| TriggerRapidFlurry | Rapid Flurry | 25 |
| TriggerArmourStrike | Armour Strike | 25 |
| TriggerRageStrike | Rage Strike | 30 |
| TriggerHemorrhage | Hemorrhage | 35 |
| TriggerVenomShot | Venom Shot | 35 |
| TriggerDoubleVolley | Double Volley | 40 |
| TriggerFireball | Fireball | 60 |
| TriggerShockBarrage | Shock Barrage | 50 |
| TriggerRestorativeStrike | Restorative Strike | 30 |
| TriggerFrostJudgment | Frost Judgment | 40 |
| TriggerBackstab | Backstab | 25 |
| TriggerQuickStrike | Quick Strike | 25 |

An authored collapsible RELIC SKILL TRIGGERS control sits above the bottom-right skill/action area. Only currently granted effects appear. Each is independently ON/OFF; disabled choices remain saved if a relic is temporarily unequipped. These switches are independent of Staff's two autocast controls and persist across Rebirth/save/load, but reset on New Game. Repeated grants of the same effect resolve once per root attack. The dispatcher has a re-entry guard, and its hit contexts carry `RelicTriggeredSkill`; dispatched skills do not invoke another root-attack dispatch. Ailment ticks, Echo, projectiles and secondary damage cannot create an infinite relic-trigger chain.

## Reduced Shock and Reduced Chill

The ordinary equipment ladder was authored as tiny fractional values but consumed as percentage points. Both stats now use the same meaningful ladder:

| Tier | Minimum item level | Old raw range (actual reduction) | New raw range (actual reduction) |
|---|---:|---|---|
| T5 | 1 | 0.04–0.06 (0.04–0.06%) | 4–6 (4–6%) |
| T4 | 20 | 0.07–0.10 (0.07–0.10%) | 7–10 (7–10%) |
| T3 | 40 | 0.11–0.15 (0.11–0.15%) | 11–15 (11–15%) |
| T2 | 60 | 0.16–0.20 (0.16–0.20%) | 16–20 (16–20%) |
| T1 | 80 | 0.21–0.25 (0.21–0.25%) | 21–25 (21–25%) |

Both can roll on Body Armour, Helmets and Boots, as both independent implicits and explicit prefixes. Three perfect T1 explicit rolls contribute 75%; three empowered explicit maxima contribute 93.75%. An implicit may repeat an explicit family: three perfect implicit-plus-explicit pairs contribute 150 raw percentage points, clamped to 100% actual reduction. With empowered explicits and ordinary perfect implicits, that raw total is 168.75 points. Implicits themselves cannot be empowered. Two T3 midpoint rolls contribute 26%, a moderate investment. Mage and Ranger each contain five choices per family at 8 points each (40% if all five are taken); Ranger's formerly zero-valued reduction choices are restored to the generator's intended 8 points. There are no added relic reduction modifiers. Shared gameplay/laboratory adapters read the same fraction-scaled stats.

Separately for each family: two T3 midpoints reduce a capped 100% Shock to 74% and a 30% Chill to 22.2%; three perfect ordinary T1s reduce those effects to 25% and 7.5%, respectively. Gear plus passive investment adds before the existing 100% reduction clamp. Full immunity requires deliberate combined investment; one T1 affix only supplies 25%. Storm Mage stack count and maximum-Shock increases are unchanged. Crown's intentionally fixed self-Shock remains a distinct Unique mechanic. Schema-15 gear rolls, including empowered tiny legacy rolls, migrate once to percentage-point values.

## Advanced loot controls

CLEAR THIS SLOT removes only the current item type/weapon subtype's modifier requirements. CLEAR ALL MODIFIER FILTERS requires a second confirmation click and removes modifier requirements across all types. Neither changes rarity, elements, weapon choices, ALL/ANY mode, minimum-match preferences or auto-dismantle state. Existing EITHER/IMPLICIT ONLY source selection, tier sliders and modifier-only highlighting are preserved. Clear-all confirmation resets on item-type changes or clearing a slot. Unique protection also overrides saved legacy filters.

## Validation and Git

Initial focused EditMode run: 74/74 passed (`EarlyProgressionQolTests`, `MageShockManaTests`, `SystemsRedesignRegressionTests`, `RelicFoundationTests`, `PlaytestFollowUpTests`). Final expanded suite: 163/163 passed, including actual Unique pickup protection, cross-weapon relic-trigger Mana/OFF/no-duplicate behavior, saved-game migrations, and revised Rebirth tests. The tier validator was unified with generation after the expanded tests exposed a stale ailment-range check. Older four-slot/schema assertions were updated to the current eight-slot/schema-16 contract; destroyed test-fixture singleton cleanup was corrected.

Fresh strict Windows x64 build succeeded with 0 errors and 833 warnings (primarily existing obsolete API/package warnings). Output: `Builds/SystemsRedesignWindows/BlackCube.exe`. Its managed gameplay assembly was freshly rebuilt. Hidden startup reached MainMenuUI without an exception; the existing unassigned Achievements-button warning remains. Only the test player launched for this smoke was stopped. Logs: `Logs/EarlyProgressionWindowsBuild.log`, `Logs/EarlyProgressionPlayerStartup.log`.

Real-scene functional smoke passed all six starter first encounters and the saved Rebirth crafting/returns/trigger flow, followed by a clean next run. Its save files are isolated under Temp, not the player's character slots. Log: `Logs/EarlyProgression-play-check.txt`.

Native visual/manual UI smoke is not claimed by batchmode checks. In Unity, check wizard text/return slots at your working resolution, opening relic inventory then continuing setup, per-trigger ON/OFF layout, Clear This Slot/confirmed Clear All, and Unique tooltip/pickup visibility. The real-scene automated check covers functionality, not pixel-level visual approval.

No broad BalanceLab pass, no push and no edits to main. Existing local Mage/Shock, loot UI and user ModDatabase changes were preserved. Nothing is staged or committed in this pass; overlapping files remain combined in the working tree rather than accidentally staging unrelated edits.
