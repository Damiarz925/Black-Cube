# Black-Cube v1 content contract

This is the Step 14 scope ledger. Status describes the repository as verified against the current design documents; it does not turn a proposal into approved scope.

## 1. V1 Product Definition

**LOCKED FOR V1:** a Windows single-player paper/cut-paper idle-action RPG vertical release with automatic basic combat, seven selectable active attacks, itemization/crafting, a 290-node passive tree, persistence, and optional Rebirth meta progression. “Content complete” requires every **LOCKED FOR V1** item below and resolution of every release-blocking **USER DECISION REQUIRED** item.

## 2. Core Game Loop

**CURRENTLY IMPLEMENTED:** enter a combat level, automatically fight nine normal encounters and a stage-10 boss, collect/equip/filter/dismantle/craft gear, spend passive points, use active skills, advance, save/load, and optionally Rebirth at combat zone 60+. Pickup-filter rejection intentionally uses the same dismantle path.

## 3. World / Biomes

**CURRENTLY IMPLEMENTED:** one forest family, six corruption/progression backgrounds (0/20/40/60/80/100%), ten combat levels per background, repeating every 60 levels.

**USER DECISION REQUIRED:** the proposed v1 world is six biomes × ten distinct scenes per biome × six corruption variants = 360 combat levels. Existing authoritative documents do not approve those biome/scene counts and explicitly say final campaign length is unlocked. Therefore 360 is the current relic-scaling reference maximum, not yet a locked content commitment.

## 4. Encounter Structure

**LOCKED FOR V1 / CURRENTLY IMPLEMENTED:** nine normal encounters followed by the stage-10 boss at every combat level. A combat level advances only after the boss dies.

## 5. Normal Enemies

**PARTIALLY IMPLEMENTED:** the production pool contains one goblin normal enemy with scalable intrinsic stats and legal generated equipment.

**USER DECISION REQUIRED:** normal-enemy species, variants, biome allocation, attacks, and required count for content-complete v1.

## 6. Bosses

**PARTIALLY IMPLEMENTED:** one hobgoblin boss presentation uses the same turn-combat framework and scaling authority.

**USER DECISION REQUIRED:** boss count, biome allocation, mechanics, final boss, and whether the discussed active top-down/bullet-hell boss mode is v1 or **POST-V1 / STRETCH**. That mode is not currently locked.

## 7. Player Skills

**CURRENTLY IMPLEMENTED:** Heavy Strike, Ice Strike, Lightning Strike, Fireball, Envenom, Shiv, and Immolate, including skill levels and current ailment/projectile hooks.

**USER DECISION REQUIRED:** confirm whether these seven are the complete v1 skill set. Step 14 adds none.

## 8. Passive Tree

**LOCKED FOR V1 / CURRENTLY IMPLEMENTED:** the current 290-node stable-ID topology and twenty keystone effects in `PASSIVE_TREE.md`. Balance and presentation polish may continue; topology redesign is excluded. Deep Freeze’s zero-default maximum-effect increase remains unresolved tuning.

## 9. Items / Affixes / Rarities

**CURRENTLY IMPLEMENTED:** Normal/Magic/Rare/Legendary gear, stable slot pools, one permanent implicit, balanced Prefix/Suffix explicit caps, variable item-level tiers, paired weapon damage, validation, tooltips, and legacy compatibility. Equipment item level remains capped at 100.

**LOCKED FOR V1:** all eligible tiers remain possible. `AffixTierWeightPolicy` multiplies authored tier weight by a centralized level-progress quality bias; higher level statistically favors stronger tiers without guaranteeing T1.

## 10. Crafting / Currencies

**CURRENTLY IMPLEMENTED:** six ordinary operations, six Ancient relic operations, armed cursor/target validation, no spend on invalid relic action, protected implicits, and 10-fragment conversion. Dismantle rewards are Normal 0, Magic 1 N→M fragment, Rare 1 M→R fragment, Legendary 2 M→R fragments.

## 11. Ailments / Combat Mechanics

**CURRENTLY IMPLEMENTED:** direct Physical/Fire/Cold/Lightning/Void, Poison as Void DOT, Bleed, Ignite, Shock, Chill, criticals, Hit Twice, mitigation/penetration/resistance caps, resource recovery, and independent status timing described by the design and balance contracts. Accuracy/evasion, Block, cooldown gameplay, and minion entities are intentionally absent pending approval.

## 12. Rebirth / Relics

**LOCKED FOR V1 / CURRENTLY IMPLEMENTED:** Rebirth unlocks from authoritative combat zone 60, clears run state, preserves permanent relic ownership/active slots, creates one current-cycle relic, refreshes Ancient currency, provisions exactly one new starter through the New Game authority, and begins at zone 1.

Relic level is stored once at creation: `clamp(1 + floor((zone - 60) * 99 / 300), 1, 100)`. Zone 60 produces level 1; zone 360 produces level 100. Numerical relic families use variable tier ladders: T5 level 1, T4 20, T3 40, T2 60, T1 80. Lower eligible tiers remain possible through the shared quality policy. Schema-5 relics migrate as level 1 with legacy tier metadata and exact values unchanged.

**CURRENTLY IMPLEMENTED:** full-card hover/click targets; left click equips the first free active slot or unequips; a fifth relic reports “Relic slots full” without replacement; armed Ancient clicks craft instead of equipping.

Starter-specific stable families can select Fire/Cold/Lightning/Void base identity, add starter base damage, add effective item level (additive, capped at 100), and add Legendary chance (additive, capped at 100%). Strongest element tier wins, then earlier active slot. A Legendary result is rolled once from run/cycle-seeded RNG, receives a production implicit plus legal 3 Prefix/3 Suffix explicits, and the resulting item is persisted rather than regenerated on load.

## 13. Post-Level-100 / Endgame

**USER DESIGN DECISION REQUIRED — POST-ILVL-100 GEAR PROGRESSION:** player level and equipment item level cap at 100, while the proposed world/relic reference extends to combat level 360. No approved system explains player/equipment power growth from 100–360. Possible future directions include a separate endgame progression layer, bounded tier evolution, or Rebirth-focused power, but Step 14 chooses and implements none. Enemy balance above 100 must not assume an imaginary solution.

## 14. UI / Menus / Codex

**CURRENTLY IMPLEMENTED:** Main Menu, overwrite confirmation, Load gating, Options, Pause/Death flows, HUD, inventory/equipment/stats/passive/skill/crafting/relic views, affix Codex, tooltips, and full relic-card interaction. **PARTIALLY IMPLEMENTED:** Achievements is a visible placeholder only; some runtime-built UI still needs final art/polish.

## 15. Art Content

**PARTIALLY IMPLEMENTED:** paper player/enemy combat art, one forest progression family, gear/status/UI procedural presentation, and canonical ordinary/Ancient currency icons. Required biome, scene, enemy, boss, animation, VFX, and final UI-art counts depend on unresolved content scope.

## 16. Audio Content

**NOT IMPLEMENTED / USER DECISION REQUIRED:** approve the v1 music, ambience, UI, skill, hit, ailment, enemy, boss, and accessibility-audio requirements and counts.

## 17. Tutorial / Onboarding

**NOT IMPLEMENTED / USER DECISION REQUIRED:** approve required onboarding for automatic combat, skills/mana, inventory/filter auto-dismantle, affixes/crafting, passives, Rebirth, relic slots, and Ancient crafting.

## 18. Options / Accessibility / Input

**PARTIALLY IMPLEMENTED:** pause preferences and mouse/keyboard UI interaction exist. **USER DECISION REQUIRED:** final input rebinding, controller support, text scaling, color/flash/motion controls, audio sliders, and accessibility acceptance criteria.

## 19. Achievements

**NOT IMPLEMENTED:** the Main Menu control is a placeholder. Achievement quantity/content remains **USER DECISION REQUIRED**; no arbitrary count is approved here.

## 20. Save / Persistence

**CURRENTLY IMPLEMENTED:** schema-6 atomic primary/backup JSON, validated transactional restore, encounter-start resume, deterministic run identity, stable gear/relic state, and sequential V1→V2→V3→V4→V5→V6 migration. New Game confirms replacement and clears run/relic history/currency as contracted.

## 21. Release-Required Polish

**PARTIALLY IMPLEMENTED:** strict builds, smoke checks, reference validation, and extensive regression fixtures exist. Release still requires scoped content, final art/audio/onboarding/options decisions, human playtesting, balance follow-up, player-facing save recovery, warning cleanup triage, and clean distributable QA.

## 22. Explicit Post-V1 / Stretch Content

**POST-V1 / STRETCH unless later promoted:** minions, cloud conflict/multiple save-slot UI, unapproved Accuracy/Block/cooldowns, extra skills, and active boss mode. Achievements and the campaign’s missing content are not automatically stretch—they remain decisions because the current UI/product framing references them.

## 23. Content-Complete Exit Criteria

All locked systems pass focused and full regression; every required biome/scene/enemy/boss/skill/art/audio/tutorial/options count is approved and present; the post-ilvl-100 progression contract is approved before late-world balance; all v1 encounters are playable and save-safe; menus and accessibility requirements are complete; one clean Windows build passes startup and representative New Game→combat→Rebirth→save/load QA; no release-blocking placeholders remain.

## 24. Unresolved User Decisions

1. Approve or revise 6 biomes × 10 scenes × 6 variants and the 360-level total.
2. Approve required normal-enemy and boss rosters/mechanics, including final boss.
3. Confirm whether the seven current skills are the complete v1 set.
4. Classify active top-down/bullet-hell boss play as v1 or post-v1.
5. Choose post-ilvl-100 player/equipment progression before balancing levels 100–360.
6. Approve audio, tutorial, accessibility/input, achievement, and final art scope/counts.
