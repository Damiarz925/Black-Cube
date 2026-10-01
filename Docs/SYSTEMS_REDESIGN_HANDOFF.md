# Systems redesign — production handoff
Date: 2026-09-28. Branch: codex/repository-cleanup-baseline. Save schema: 15.
This is a first production implementation, not a final balance certification. No broad BalanceLab survey was run.

## 1. Bugfixes and combat/UI changes

Staff production skills are automatic **queued attack replacements**, not independent casts between attacks. Each enabled/ready/affordable skill competes in slot order; it replaces the next gauge attack and restarts its own cooldown once. Clicking its existing skill button toggles autocast. Both toggles persist; partial cooldowns and combat transients do not. Developer AutoCooldown fixtures remain supported.

Spell Echo is an independent stable stat (144), not recycled Cast Speed or cooldown recovery. Effective chance is capped at 60%. Each successful magic cast can chain full-strength echoes; original Mana cost is paid normally, echo n costs baseCost × (1 + 0.25n), and a failed Mana check stops the chain. A hard safety bound prevents infinite scheduling. Fireball/Shock Barrage Relics add chance to their respective spells.

Mage generic tree has functional incoming Shock/Chill reduction families totaling 40% each; its old generic Cold damage family is replaced by 10% Spell Echo. Stable passive IDs and UI geometry are preserved. Staff-tree Echo remains a separate investment.

Stats flashing: row/header destruction and redundant text writes during frequent combat refreshes were replaced with reusable pools/change-only updates. Header callbacks are rebound once. Advanced Sources is collapsed initially. DPS arrows appear in the item tooltip's upper-right estimate, not inventory cells; T1 modifier lines are gold. Death recap retains both critical and ailment tags.

Advanced Loot now has one item-type dropdown with separate legal policies, six actual weapon-type buttons, five **base/primary weapon element** buttons, full-width legal modifier rows, Explicit/Implicit controls, a whole-number minimum-tier slider, Help, Unique rarity, and an auto-dismantle toggle. Rarity takes precedence; locked items survive. ON dismantles rejected pickups through the existing fragment path; OFF retains them. Manual right-click dismantling remains. The obsolete separate highlight/filter menus and inventory-header dismantle button are removed. CLEAR SAVED LEGACY RULES explicitly removes old saved policies. No implicit redesign of pickup mismatch behavior was made.

Starting weapons receive 12% stronger base damage. Default elements: Warrior/Barbarian/Thief Physical, Ranger Void, Mage Fire, Priest Cold. Active starting-element Relics override this; best tier wins, then lowest equipped slot resolves ties.

## 2. Authoritative ordinary Relic modifier pool

Relics do **not** have prefix/suffix separation or crafting potential. First modifier is a permanent implicit; remaining modifiers are explicit. No repeated modifier identity within one ordinary Relic. Normal has 1 total modifier, Magic 2, Rare 3–4, Legendary 5–6.

All five-tier rows below are **T5 / T4 / T3 / T2 / T1**. Minimum Relic levels: 1 / 20 / 40 / 60 / 80; tier weights: 50 / 35 / 20 / 10 / 5. Fixed rows have one T5 tier available at level 1. Percentage values are displayed percentage points; consumers convert to fractions as appropriate.

| Modifier | Production ranges | Effect and stacking |
| --- | --- | --- |
| More Damage | 3–5 / 4–7 / 5–10 / 8–13 / 11–16% | All player damage; separate multiplicative MORE layer per modifier. |
| Increased Experience Gained | 6–10 / 8–14 / 10–20 / 16–25 / 22–30% | XP gained; additive increased bucket. |
| More Attack Speed | 2–4 / 3–6 / 4–8 / 6–10 / 8–12% | Attack frequency; separate multiplicative MORE layer per modifier. |
| All Resistances | 3–5 / 4–7 / 5–10 / 8–13 / 11–16% | Fire/Cold/Lightning resistance points; additive (not Void). |
| Multistrike Chance | 3–5 / 4–7 / 5–10 / 8–13 / 11–16% | Multistrike chance; additive points, normal weapon eligibility still applies. |
| Projectile Amount | Fixed 1 | Additional projectiles; additive integers. |
| Maximum Bleed Stacks | 1 / 2 / 3 / 4 / 5 | Added maximum Bleed stacks; sum before Warrior doubling. |
| Maximum Ignite Stacks | Fixed 1 | Added maximum Ignite stacks; additive. |
| Equipped Active Skill Level | Fixed 1 | Levels of the equipped weapon’s skills; additive. |
| Maximum Chill Slow | Fixed 5% | Raises maximum Chill slow by percentage points; additive. |
| Starting Weapon Becomes Fire | Fixed 1 | Overrides starting weapon element; best tier wins, then lowest active slot. Multiple overrides do not combine. |
| Starting Weapon Becomes Cold | Fixed 1 | Overrides starting weapon element; best tier wins, then lowest active slot. Multiple overrides do not combine. |
| Starting Weapon Becomes Lightning | Fixed 1 | Overrides starting weapon element; best tier wins, then lowest active slot. Multiple overrides do not combine. |
| Starting Weapon Becomes Void | Fixed 1 | Overrides starting weapon element; best tier wins, then lowest active slot. Multiple overrides do not combine. |
| Starting Weapon Base Damage | 5–10 / 10–18 / 18–28 / 28–40 / 40–55% | Starting weapon base damage increased bucket; additive before player scaling. |
| Starting Weapon Item Level | 5 / 10 / 20 / 35 / 50 | Starting weapon item-level bonus; additive, resulting ilvl clamped 1–100. |
| Starting Weapon Legendary Chance | 5 / 10 / 15 / 20 / 30% | Starting weapon Legendary chance; additive, capped 100%. |
| Maximum Shock Effect | 5–10 / 10–15 / 15–20 / 20–30 / 30–40% | Raises maximum per-instance Shock effect by percentage points; additive. |
| More Enemy Drops | 5–10 / 10–15 / 15–25 / 25–35 / 35–50% | Ordinary gear/currency reward budgets; each modifier multiplies (1+roll/100). |
| Increased Enemy Rarity | 10–20 / 20–35 / 35–50 / 50–75 / 75–100% | Higher enemy-rarity weighting; additive increase, weights biased by (1+increase)^rarity index. |
| Increased Item Rarity | 10–20 / 20–35 / 35–50 / 50–75 / 75–100% | Higher ordinary item-rarity weighting and Unique chance; additive increase. |
| Carry One Item Up To Item Level | 1–20 / 20–40 / 40–70 / 70–100 / 100–350 | Each active modifier grants one return slot, with its own maximum item level. |
| Starting Weapon: Sword | Fixed 1 | Unlocks this starting weapon in Rebirth selection; duplicate unlocks do not add power. |
| Starting Weapon: Axe | Fixed 1 | Unlocks this starting weapon in Rebirth selection; duplicate unlocks do not add power. |
| Starting Weapon: Bow | Fixed 1 | Unlocks this starting weapon in Rebirth selection; duplicate unlocks do not add power. |
| Starting Weapon: Staff | Fixed 1 | Unlocks this starting weapon in Rebirth selection; duplicate unlocks do not add power. |
| Starting Weapon: Sceptre | Fixed 1 | Unlocks this starting weapon in Rebirth selection; duplicate unlocks do not add power. |
| Starting Weapon: Dagger | Fixed 1 | Unlocks this starting weapon in Rebirth selection; duplicate unlocks do not add power. |
| RapidFlurry On Every Attack | Fixed 1 | Triggers this skill once after each successful attack; duplicates deduplicated, full Mana/skill pipeline, no recursive attack triggers. |
| ArmourStrike On Every Attack | Fixed 1 | Triggers this skill once after each successful attack; duplicates deduplicated, full Mana/skill pipeline, no recursive attack triggers. |
| RageStrike On Every Attack | Fixed 1 | Triggers this skill once after each successful attack; duplicates deduplicated, full Mana/skill pipeline, no recursive attack triggers. |
| Hemorrhage On Every Attack | Fixed 1 | Triggers this skill once after each successful attack; duplicates deduplicated, full Mana/skill pipeline, no recursive attack triggers. |
| VenomShot On Every Attack | Fixed 1 | Triggers this skill once after each successful attack; duplicates deduplicated, full Mana/skill pipeline, no recursive attack triggers. |
| DoubleVolley On Every Attack | Fixed 1 | Triggers this skill once after each successful attack; duplicates deduplicated, full Mana/skill pipeline, no recursive attack triggers. |
| Fireball On Every Attack | Fixed 1 | Triggers this skill once after each successful attack; duplicates deduplicated, full Mana/skill pipeline, no recursive attack triggers. |
| ShockBarrage On Every Attack | Fixed 1 | Triggers this skill once after each successful attack; duplicates deduplicated, full Mana/skill pipeline, no recursive attack triggers. |
| RestorativeStrike On Every Attack | Fixed 1 | Triggers this skill once after each successful attack; duplicates deduplicated, full Mana/skill pipeline, no recursive attack triggers. |
| FrostJudgment On Every Attack | Fixed 1 | Triggers this skill once after each successful attack; duplicates deduplicated, full Mana/skill pipeline, no recursive attack triggers. |
| Backstab On Every Attack | Fixed 1 | Triggers this skill once after each successful attack; duplicates deduplicated, full Mana/skill pipeline, no recursive attack triggers. |
| QuickStrike On Every Attack | Fixed 1 | Triggers this skill once after each successful attack; duplicates deduplicated, full Mana/skill pipeline, no recursive attack triggers. |
| Fireball Echo Chance | 5–8 / 8–12 / 12–16 / 16–22 / 22–30% | Added Fireball Echo chance; sums with global Echo, final cap 60%. |
| Shock Barrage Echo Chance | 5–8 / 8–12 / 12–16 / 16–22 / 22–30% | Added Shock Barrage Echo chance; sums with global Echo, final cap 60%. |

42 rollable identities. Removed from new rolls: Increased Maximum Life, Increased Maximum Mana, Increased Void Damage, Increased Ailment Damage, Fewer Shock Stacks for Trigger. Their serialized IDs remain reserved. Migration replaces obsolete payloads deterministically with valid, nonduplicate modifiers; old Shock threshold becomes Maximum Shock Effect where possible. Legacy Relics are conservatively marked Crafted because old saves did not record crafting history.

Added: Maximum Shock Effect, More Enemy Drops, Enemy/Item Rarity, Item Return, six starter-type unlocks, twelve attack-trigger skill identities, Fireball/Shock Barrage Echo. Changed: Bleed stack tiers, fixed Ignite/Projectile/skill-level/Chill bonuses, explicit Pristine/Crafted state. Existing ordinary damage, XP, attack-speed, resistances and starter scaling ladders remain intact.

Relic-trigger skills do not replace the originating attack, bypass ordinary queue cooldown selection, pay their full Mana costs, use normal level/skill/projectile/Echo consumers, and cannot recursively trigger the attack-trigger list. Each distinct triggered skill fires at most once per successful attack, regardless of duplicate Relics.

## 3. Rebirth, eight slots, fusion and returns

Reward count = clamp(1 + floor((reachedLevel−60)/40), 1, 8).
Relic level = clamp(1 + floor((reachedLevel−60) × 99/280), 1, 100).
Rarity weights update at ten-stage milestones, interpolating from 100/0/0/0 at 60 to 5/15/40/40 at 340. At 340+, the **first** reward is guaranteed Legendary; other rewards use the weights.

| Reached combat level | Relics earned | Relic level | Normal / Magic / Rare / Legendary weights |
| --- | --- | --- | --- |
| 60 | 1 | 1 | 100 / 0 / 0 / 0 |
| 100 | 2 | 15 | 86.43 / 2.14 / 5.71 / 5.71 |
| 140 | 3 | 29 | 72.86 / 4.29 / 11.43 / 11.43 |
| 180 | 4 | 43 | 59.29 / 6.43 / 17.14 / 17.14 |
| 220 | 5 | 57 | 45.71 / 8.57 / 22.86 / 22.86 |
| 260 | 6 | 71 | 32.14 / 10.71 / 28.57 / 28.57 |
| 300 | 7 | 85 | 18.57 / 12.86 / 34.29 / 34.29 |
| 340 | 8 | 100 | 5 / 15 / 40 / 40; first guaranteed Legendary |

Eight active slots migrate existing four slots in order and leave new slots empty. Ordinary active modifiers stack as listed above. Rebirth moves existing Relics out of current-cycle crafting eligibility and grants new-cycle rewards.

Pristine means never successfully modified with Ancient crafting. Successful crafting permanently marks Crafted, even if a later operation returns the original values. Five distinct owned Pristine Relics of the same rarity below Legendary fuse into one fresh next-rarity Relic, at rounded average ingredient Relic level, in the current cycle. Equipped inputs can be consumed safely; removed slots clear. Invalid recipes consume nothing; Legendary/Unique Relics are not fusion inputs.

Rebirth setup offers only the class default weapon plus active starter-type unlocks. Each active Item Return modifier produces one drag/drop return slot and item-level ceiling. Right-click clears a selection. Confirmation revalidates ownership, distinct item identity and individual ceilings; actual item IDs, rolls, rarity, locks and special flags survive through Gear snapshots. New Game remains a complete character reset, not a Rebirth.

## 4. Damaging ailments

These are scaled **game seconds**, independent of attack speed/global attack turns. Paused gameplay does not advance the real-time timers. Shock/Chill still use their existing turn-state mechanics.

| Ailment | Base maximum stacks | Duration | Tick interval | Damage per tick | Total base damage per application |
| --- | --- | --- | --- | --- | --- |
| Ignite | 1 | 4 s | 2 s | 25% of eligible applying hit | 50% |
| Bleed | 5 | 4 s | 1 s | 5% | 20% |
| Poison | 20 | 1 s | 0.5 s | 2.5% | 5% |

First ticks are delayed by their full interval. Duration = baseDuration × max(0.01, 1 + increasedDuration). Interval = max(0.02 s, baseInterval / max(0.01, 1 + increasedSpeed)). Duration does not weaken tick damage; speed does not shorten duration. Tick count is floor(duration/interval), with boundary tolerance. Partial final intervals do not invent an extra tick. Poison Speed and Poison Tick Speed add together.

Chance above 100% gives guaranteed whole applications plus one fractional chance (250% = 2 guaranteed + 50% third). Applications have independent timing, source and critical-ailment metadata. At a finite cap, stronger remaining-damage stacks can replace weaker ones. Each damaging tick is a separate Cull opportunity, including multiple ticks caught up in one frame. Remaining duration/next-tick timing survives Venomous Memory copying but not save/load.

Ignite/Bleed bonuses sum from active Relics. Warrior Bleed doubles the final maximum after Relic additions. Barbarian Fire grants an additional Ignite. Ranger Endless Poison remains uncapped/infinite until target death, with its existing duration-investment damage conversion. Bottomless Wound halves Bleed duration. Fire aura duration support uses the real-time interval.

Duration/tick-speed gear ladders: T5 3–5%, T4 6–9%, T3 10–14%, T2 15–19%, T1 20–25%, minimum ilvls 1/20/40/60/80. No arbitrary hard duration cap. Old serialized duration/tick-speed rolls migrate to percentage units (duration ×5; speed ×20) under legacy legality; no stable stat ID is reused. Canonical production timing is AilmentTimingRules, not the old turn-oriented asset metadata.

One Decisive Strike adds **110% of each actual independently resolved extra strike**, including its own crit and relevant modifiers; it does not estimate all extras from the first hit or run multiple on-hit loops.

## 5. Rage

Maximum 100. Baseline at 100: 20% MORE damage and 30% increased attack speed, before Rage Effect. No Rage damage reduction remains. Baseline decay is continuous 10 Rage/s, with no idle grace period.

A qualifying damaging hit refreshes a 2-second generation window. Rate = (18 + min(10, floor(damage/targetMaxLife ×50))) × eventMultiplier × (1+increasedGeneration), i.e. baseline 18–28 Rage/s. Overlapping generation refreshes the strongest current rate rather than summing infinitely. Generation and decay run simultaneously, then idle decay continues; Rage clamps 0–100. Taking damage feeds Revenge, not instant Rage.

| Affix (prefix) | T5 / T4 / T3 / T2 / T1 |
| --- | --- |
| Ring Rage Generation | 10–20 / 20–30 / 30–40 / 40–50 / 50–70% |
| Belt Rage Generation | 15–30 / 30–45 / 45–60 / 60–75 / 75–105% |
| Ring Reduced Rage Decay | 3–5 / 6–9 / 9–12 / 12–15 / 15–20% |
| Belt Reduced Rage Decay | 4.5–7.5 / 9–13.5 / 13.5–18 / 18–22.5 / 22.5–30% |

Belt uses one centralized 1.5× premium. Barbarian full-Rage keystone enables any-weapon Rage, triples decay, and grants an extra 30% MORE damage only at 100 Rage; normal baseline Rage bonuses remain below cap. Heart enables any-weapon Rage without that penalty. Taking Heart alongside the keystone does not remove its triple-decay cost. Finisher consumes Rage and clears the current generation window.

## 6. All thirteen production Uniques

Required item level is **20 for every identity**; actual drop ilvl can be higher. All rolls and Prism aura combinations are generated once and immutable. No ordinary reroll/add/remove/upgrade, implicit reforge, boss infusion or Empowerment. Crafting potential is zero and not shown as a craftable resource. First-pass Unique chance is 0.25% of eligible gear rolls at ilvl20+, multiplied by (1+item-rarity increase), capped at 5%; eligible identities are selected uniformly. Unique rarity retention is enabled when upgrading old default keep-all filter preferences.

Nonweapon bases use existing slot armour scaling: level-100 Body 4454, Helmet 2475, Gloves/Boots 1485, jewellery 0; lower levels use ItemArmourProfile. Heart's armour increase is local until extracted to a Relic.

Unique weapon base formula: B=(4+0.8×ilvl)×identityScale; range 0.85B–1.15B. Listed speed/crit are base attacks/s and base crit chance before local/global modifiers. These are first-pass authored bases, not a claim of final optimized superiority.

| Item / slot / element | Base | Immutable rolls and mechanics |
| --- | --- | --- |
| Heart of the Berserker / Body | Ordinary slot armour | Rage generation 40–120%, decay reduction 15–40%, flat Life 40–160, local Armour 20–70%; any-weapon Rage. |
| Crown of Static Glass / Helmet | Ordinary slot armour | Shock on you lasts 1.5–2.5×; 50% MORE incoming Shock effect. While Shocked: 25–75% MORE Lightning damage, 20–80% increased outgoing Shock effect, 10–35% cooldown reduction. |
| Hands of the Many / Gloves | Ordinary slot armour | Multistrike 60–120%; Poison/Bleed/Ignite chance 30–90% each; 30–45% less hit damage; ailment basis 2–3× pre-penalty hit. Current Multistrike eligibility/chance cap remains; ailment chances can overflow. |
| Ashen Footsteps / Boots | Ordinary slot armour | Expired player Ignite creates a 2-second battlefield lava-pool effect: 15–40% increased Fire damage taken (hits and Ignite). Survives enemy death; subsequent enemy shares the combat location. Repeats refresh duration and retain the strongest pool; no infinite stacking. No final pool artwork. |
| The Bottomless Wound / Belt | Jewellery base | Bleed chance 60–150%; halves Bleed duration; a direct hit consumes all remaining active Bleed damage before applying fresh Bleeds. Rupture damage is separately tagged, not a recursive attack. |
| Prism of Five Voices / Amulet | Jewellery base | 2 or 3 distinct Physical/Fire/Cold/Lightning/Void auras; Aura Effect −50% to +50%. Combination and effect never reroll. Negative Aura Effect really reduces aura output. |
| Venomous Memory / Ring | Jewellery base | Keeps 5–20 Poison stacks on enemy death, ranked by remaining expected mitigated damage; copies damage, lifetime and next-tick timing to the next enemy. Transient memory does not persist through load. |
| Final Argument / Physical Sword | Scale1.6; speed1.2; crit8% | Local Physical 70–150%, Attack Speed10–30%, increased Crit20–60%, Multistrike50–100%; extra-strike damage −5% to +10% as a separate multiplicative layer. Conventional Sword, not consolidation. |
| Heartsplitter / Physical Axe | Scale4; speed0.55; crit5% | Local Physical80–180%; additional Revenge scaling40–120%. Slow, large-hit base with ordinary Axe Rage. |
| Hydra String / Physical Bow | Scale1.8; speed1.15; crit5% | +3 projectiles; 30–40% less hit damage. Ailment multiplier =1.30/(actual hit multiplier), so ailment basis is 130% of equivalent unpenalized hit. |
| Stormcaller / Lightning Staff | Scale1.8; speed1; crit5% | Spell Echo20–40%; each echoed spell self-hits for15–40% of spell basis as Lightning and applies 10% Shock before incoming reduction/Crown effects. Lightning resistance mitigates self-hit; Mana escalation/global60% Echo cap remain. |
| Saint's Contradiction / Void Sceptre | Scale1.8; speed1; crit5% | Life regeneration1–4% Max Life/s; 10–50% of actual Void damage refreshes strongest temporary regeneration for2s; 10–50% of total Life regeneration duplicated as Void DPS, scaled by Void modifiers. Generated damage cannot feed its own regeneration; regeneration is not Dark Priest non-regeneration healing. |
| Last Breath / Physical Dagger | Scale1.8; speed1.6; crit8% | Ordinary/base Cull threshold replaced by 0.2 percentage points per active Poison +1 point per active Bleed, without a hard threshold cap. No free Cull chance. |

Cull chance is new stable stat145, **not** the old dagger threshold stat143. Base threshold is10% without Last Breath; ordinary higher threshold sources remain. New chance suffixes on Ring/Belt/Amulet/Gloves: T5 3–5%, T4 6–9%, T3 10–14%, T2 15–19%, T1 20–25%. HP is updated first; chance is rolled only once HP is within the current threshold and still alive. 0 chance cannot execute. Last Breath ignores ordinary threshold bonuses. Every qualifying direct-hit/DoT event can independently roll.

## 7. Stage-350 forge chase

Stages341–350 are boss-only, Legendary-rarity encounters using existing boss/rarity scaling. Normal encounter counters are normalized to the boss quota; old saves cannot restore an ordinary enemy there. Reaching350 before Rebirth awards one persistent forge opportunity on that Rebirth; future cycles can repeat. There is no automatic perfect Relic reward.

Inventory UNIQUE RELIC FORGE requires four distinct unlocked **inventory-owned** ingredients: two Uniques and two Legendaries with six explicit modifiers each (permanent implicit/base weapon fields are not counted). Select one actual modifier from each Unique; one actual explicit modifier is randomly extracted from each Legendary. No retained value is rerolled. Four ingredients and one opportunity are consumed only after constructing/validating the result.

Result is an immutable, noncraftable Unique Relic with four extracted entries and source identities. Own several, equip only one across eight slots. Local typed damage becomes meaningful global typed damage; local Armour% becomes global Armour%; local attack-speed/crit use their corresponding global stat consumers. The Axe Physical/Rage hybrid becomes global MORE Physical plus increased Rage Generation, preserving both actual component rolls as one extracted entry. Other paired damage ranges retain endpoints and use their mean in the Relic's global stat contribution. Unique powers retain their central consumers; Prism retains its actual aura mask. Boss-special metadata is retained and consumed by BossSpecialEffectRuntime. Pure weapon-base fields are ineligible.

Use the dropdowns in order: Unique1, Unique2, Legendary1, Legendary2; choose the two Unique modifier entries; review feedback/opportunity count; FORGE / CONSUME FOUR INPUTS. Equipped items must be unequipped first. Invalid/duplicate/locked recipes report a reason without spending.

## 8. Developer test tools — exact workflow

Open **Black-Cube → Development → Systems Redesign Test Tools**. Enter Play Mode and start a character first. The window is Editor-only; no automatic action or balance job runs. Buttons mutate your current test character: use a spare character slot. Temporary overrides reset on entering Play Mode and are not saved; gear/progression created by explicit actions follows normal save rules.

1. Curated equipment: choose item type, weapon type, base element, rarity, ilvl and MINIMUM/AVERAGE/MAXIMUM/RANDOM roll position. First row is the implicit; remaining rows are explicits. Add/remove rows, choose stat and T1–T5. ADD CURATED ITEM TO INVENTORY validates legal family/element/tier/side capacity before adding. Invalid selections add nothing.
2. Specific Unique: choose identity in Unique identity, ilvl≥20 and roll position above; ADD UNIQUE TO INVENTORY. It retains its real production rolls, including distinct Prism auras. Equip through normal Inventory.
3. Player level: set1–100, click SET PLAYER LEVEL / CLEAR PASSIVES. This explicitly resets/refunds allocations rather than inventing invalid allocated point counts.
4. Combat level: set desired level and SET COMBAT LEVEL; or ADVANCE X STAGES. Jump6/60/100/340/350 buttons use the same zone/encounter-reset authority.
5. XP: click ×1/×5/×10/×100. Enemy ID scope is for scoped enemy drop/rarity overrides, not XP.
6. Gear/currency rates: enter multipliers (1=baseline, 5=fivefold,0=off), optionally Enemy ID scope (blank=all).
7. Enemy/Item rarity: enter increased **fractions** (1=+100%,0.5=+50%). Optionally scope item rarity to an item type. RESET ALL TEMPORARY OVERRIDES restores defaults.
8. Relics: choose Relic level/rarity, Pristine vs Crafted. For curated mode, select distinct legal modifiers/eligible tiers and the rarity's valid count. GRANT RELIC adds an owned Relic.
9. Rebirth rewards: choose reward depth, inspect displayed count/level/rarity weights. GRANT REWARD MILESTONE (NO REBIRTH) tests rewards without resetting the run. For the actual loop, jump to milestone and use in-game Rebirth.
10. GRANT ONE FORGE OPPORTUNITY is explicitly test-only. Add two Uniques and two six-explicit Legendaries, then use the real forge UI.
11. SAVE CURRENT CHARACTER EXPLICITLY writes normal current-slot state. Do not treat this as a disposable preview.

## 9. Unity UI authoring instructions

Exit Play Mode before saving layouts. Edit the source prefab in Prefab Mode, not a temporary runtime clone. Gameplay uses **Assets/Prefabs/PaperBattle/PaperBattle.prefab**; standalone Inventory source is **Assets/Prefabs/UI/InventoryPanel.prefab**. If editing standalone defaults for future instantiation, keep the gameplay instance/source overrides in mind. This pass preserves user outer-menu/passive/portrait geometry.

Advanced Loot:
1. Locate InventoryUI → Advanced Loot Filter (or follow AdvancedLootFilterUI.Panel reference). Resize panel RectTransform anchors/offsets.
2. Follow Legal modifier viewport → Legal modifier rows. Change viewport RectTransform for usable width; content VerticalLayoutGroup controls spacing/width.
3. Edit the single authored row template: LayoutElement.preferredHeight controls row height (46 initially); Modifier Name/Explicit/Minimum Tier Slider RectTransforms control column widths/attachment.
4. Item Type Dropdown, HELP and Advanced Loot Help are authored children. Adjust RectTransforms/TMP font sizes; do not delete serialized references.
5. Six weapon buttons and five element buttons are in serialized lists. Edit dimensions/label art per child; retained selection/accent is state-driven.
6. Slider is whole-number, with Any/T5→T1 semantics set at runtime. Change handle/track visuals and label rectangle, not its semantic range.
7. Inventory's ADVANCED LOOT button can be moved in the source prefab. Unique Forge/Fusion buttons/panels are likewise authored. Runtime does not reset their outer RectTransforms. Do not rerun authoring installers after manual layout work unless deliberately upgrading data.

Stats:
1. Open Assets/Prefabs/UI/StatsPanel.prefab, inspect PlayerStatsPanelUI's contentRoot, rowPrefab, headerPrefab references. Follow each referenced prefab to edit row/header visuals.
2. Content layout spacing/padding controls row separation; preferred heights in row/header LayoutElements control individual height.
3. Runtime owns section content/order and collapse state, not static panel rectangles. Main summary sections are OFFENSE, DAMAGE TYPES, DEFENSE, RESOURCES, SPECIAL; Advanced Sources expands detailed categorized rows. Move the entire content viewport/panel in the prefab; arbitrary semantic section reordering remains a code change in BuildCompactPlayerStats/section emission, not draggable persistent runtime rows.
4. Advanced Sources starts collapsed; verify repeated expand/collapse does not duplicate listeners or shift focus unexpectedly.

Skills:
1. Follow PaperBattleHUD → SkillSelectionView skillButtons/skillLabels, or Assets/Prefabs/UI/SkillSelectionPanel.prefab.
2. Edit button/label RectTransforms, TMP typography and CorruptionUIButtonSkin visuals for autocast enabled/disabled. ON/OFF/READY/cooldown text remains runtime-owned.
3. BottomActionBarLayout can control horizontal sizing/order; edit its authoring settings when a child RectTransform is layout-driven.
4. Do not replace production Staff buttons with queued-only instructions; clicking toggles enable state.

## 10. Focused validation and remaining manual visual checks

Automated real-scene check: Logs/SystemsRedesign-play-check.txt and Logs/SystemsRedesignFinalPlayCheck4.log. Passed main-menu class launch, authored forge presence, stage341 boss/Legendary entry, stage342 legacy checkpoint normalization, duplicate recipe rejection without spend, exactly four inputs/one opportunity consumed, four extracted rolls, one-equipped limit, schema15 Unique/forge save/load, authored forge buttons/dropdowns, and actual stage350 Rebirth awarding exactly eight Relics plus one forge opportunity and restarting combat level1. Character saves used a unique Temp directory; real character files were not touched. Disposable smoke files remain in Temp for inspection, not Git.

EditMode final report and Windows build/startup results are recorded in the validation completion section below. Intermediate failed logs are retained for diagnosis; final success reports, not earlier checkpoints, are authoritative.

No native manual click/layout inspection was available. Before shipping, manually:
- Equip Staff, toggle each skill separately, verify slot-order readiness/Mana starvation/next-gauge replacement, and load preserves toggles but resets partial cooldown.
- Open Stats during combat, scroll/expand sources repeatedly and check flicker/focus.
- Adjust Advanced Loot template, save, play and check wide rows, dropdown, slider, accents, Help; verify ON destroys rejected pickups and OFF keeps them.
- Allocate Mage replacement families and inspect real values/tooltips.
- Test all13 Uniques on a spare slot, especially Venomous Memory between enemies, Ashen pool expiry/death, Rupture timing, Crown self-Shock, Saint regeneration/no feedback and Last Breath with/without Cull chance.
- Rebirth with returned items and alternative unlocked weapons; fuse five Pristine Relics; use a stage350 opportunity with hand-picked ingredients.
- Check death recap critical/ailment tags and tooltip T1/DPS presentation at your target resolution.

Workbench builds now carry Unique data and active Relics explicitly; isolated evaluation must not read live gear. Short deterministic combat tests cover core consumers; single-fight headless simulation has no next-enemy chain, so Venomous Memory's transfer is runtime-only at that boundary. Analytical DPS is a neutral-target estimate, not an exact timed rotation. Conditional pools/self-Shock/resource starvation and encounter persistence require Combat Lab/runtime checks. No broad optimizer claim or final economy certification is made.

## 11. Source map

- UniqueItems: immutable catalog, creation, validation, centralized power access and readable descriptions.
- UniqueCombatRuntime: encounter-spanning Poison/pools, Rupture, Echo self-hit, temporary regeneration/duplicate Void damage.
- UniqueLoadoutState / RelicLoadoutRules: explicit isolated preview/Workbench loadouts.
- UniqueRelicForge / UI: opportunity authority, four-item extraction, one-equipped data and authored popup.
- AilmentTimingRules / Calculator / StatusInstance / StatusController: shared real-time timing, application, caps, tick damage and expiry.
- CullingRules / DamageReceiver: shared threshold/chance execution after damage.
- WeaponMechanics / ClassKeystoneMechanics / tuning asset: continuous Rage and consolidation.
- SystemsAffixProfile / AffixPolicy / ModManager / ModDatabase: new/redesigned ladders, legal sides and stable fallback definitions.
- Gear / LootManager / EquipmentManager / Inventory filter: Unique ownership, rarity, crafting restrictions, drops/equipment consumers.
- RelicSystem / RelicProgressionRules / Rebirth UI: ordinary reward/crafting/fusion/return mechanics and persistent forge grants.
- GameManager / EnemyAI: boss gauntlet and Legendary rarity.
- GamePersistence: schema15 migration, immutable snapshots, forged state and clean encounter restore.
- BalanceWorkbenchPlayerBuild / CombatTools / CombatSimulationCore: explicit loadout transport, metrics and timed consumer parity.
- SystemsRedesignDevTools: opt-in Play Mode actions/session overrides; SystemsRedesignAuthoring: targeted authored upgrade; SystemsRedesignPlayCheck: isolated real-scene validation.
- UniqueRedesignTests / CombatRedesignTests / existing focused suites: regression evidence.

## Validation completion

### Advanced Loot / Relic UI follow-up

- Fusion and Unique Relic Forge now live in a dedicated row inside the Relic inventory root, with 52 pixels reserved above the list. Switching inventory tabs closes either recipe dialog.
- Rarity KEEP/DISCARD affects pickup retention, never item highlighting. Highlighting requires an enabled, nonempty modifier policy for that item's scope and a successful mod/tier match. Mod highlighting is independent of rarity retention.
- Auto-Dismantle OFF continues to retain rejected pickups; ON continues to dismantle them. Existing owned items are not retroactively destroyed by changing filters.
- Quick acceptance: open Gear/Advanced Loot and confirm neither Relic action appears; switch to Relics and confirm both appear above the list. With no selected mods, every rarity should remain unhighlighted. Select a mod/minimum tier and check that only matching items highlight; changing KEEP/DISCARD must not add highlights.
- Follow-up validation: 48/48 focused EditMode tests passed (`Logs/LootUiFixTests.xml`), including mod-only highlighting and both authored prefab placements. No new player build or native visual inspection was performed for this follow-up.
- Subsequent source-mode change: EITHER accepts explicit or implicit rolls at the selected tier; IMPLICIT ONLY requires the permanent implicit at its selected tier. Old saved non-implicit selections now mean EITHER, retaining their selected stats and tier thresholds. Mod buttons read SELECTED / NOT SELECTED instead of unsupported checkmarks. Added source/tier, ANY counting, ALL capacity and JSON regression coverage; this newer change has not been batchmode-tested because the user requested keeping Unity open.

- Final focused EditMode suite: **170 passed / 170, zero failures** — Logs/SystemsRedesignFinalTests6.xml and .log. Fourteen focused fixtures; no broad BalanceLab/optimizer survey.
- Real-scene smoke: **passed** — Logs/SystemsRedesign-play-check.txt / SystemsRedesignFinalPlayCheck4.log. Actual main-menu launch, forge UI wiring/authority/save-load, gauntlet normalization and stage350 Rebirth.
- Fresh Windows x64 build: **Succeeded, zero errors, 833 warnings** — Logs/SystemsRedesignFinalWindowsBuild.log. Player: Builds/SystemsRedesignWindows/BlackCube.exe. StrictMode; all enabled build scenes.
- Hidden null-graphics player startup: **reached MainMenuUI.Awake; no exceptions** — Logs/SystemsRedesignFinalWindowsStartup.log. Existing unassigned Achievements-button warning remains. This is not visual QA; the owned smoke PID was stopped.
- Native manual visual/gameplay inspection: **not performed**. The explicit manual checklist above remains the visual acceptance handoff.
- Implementation completion commit: **498b6932b055f3ce5394a8552cf4d6527e79cf99** (82 task-owned files). Detailed manifest: [SYSTEMS_REDESIGN_FILES.md](SYSTEMS_REDESIGN_FILES.md).
- Previous task checkpoints: 2cf3efc6 (user UI positions), 6e57de15 (Mage/UI regression implementation), 4de11c65 (checkpoint documentation), fcbe0c60 (Relic/Rebirth expansion).
- Documentation completion is the commit containing this handoff and the superseded-checkpoint update.
- Final expected dirty state: only Assets/Prefabs/Scriptable Objects/ModDatabase.asset, preserving the user's original 15-line empty SpellEchoChance identity144 entry. This asset is excluded from task commits. Incidental generated145 metadata/123/125 slot edits and ProjectSettings changes are not retained.
- User-authored outer menu, passive-tree instance and enemy portrait geometry preserved. No main changes, no push, no broad autonomous balance pass, no old BalanceLab run. Logs, disposable smoke saves and generated Windows player stay outside Git.
