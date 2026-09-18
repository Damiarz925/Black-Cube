# Black-Cube v1 content contract

This is the V1 scope ledger through Step 15. Status distinguishes implemented architecture, placeholder references, and production-complete content.

## 1. V1 Product Definition

**LOCKED FOR V1:** a Windows single-player paper/cut-paper idle-action RPG vertical release with automatic basic combat, seven selectable active attacks, itemization/crafting, a 290-node passive tree, persistence, and optional Rebirth meta progression. “Content complete” requires every **LOCKED FOR V1** item below and resolution of every release-blocking **USER DECISION REQUIRED** item.

## 2. Core Game Loop

**CURRENTLY IMPLEMENTED:** enter a combat level, automatically fight nine normal encounters and a stage-10 boss, collect/equip/filter/dismantle/craft gear, spend passive points, use active skills, advance, save/load, and optionally Rebirth at combat zone 60+. Pickup-filter rejection intentionally uses the same dismantle path.

## 3. World / Biomes

**LOCKED FOR V1 / ARCHITECTURE IMPLEMENTED:** six biomes × ten base locations per biome × six explicit corruption presentations (0/20/40/60/80/100%) = 360 main-progression combat levels. One resolver derives biome, location, corruption, stage, and encounter from combat level. Levels above 360 safely reuse the final authored position as an endless fallback pending a later post-360 design.

**PLACEHOLDER CONTENT:** the current reference catalog maps all positions but reuses the paper forest, Goblin, and Hobgoblin. It does not represent completed biome/location/enemy/boss production content. See [WORLD_CONTENT_ARCHITECTURE.md](WORLD_CONTENT_ARCHITECTURE.md).

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

**LOCKED FOR V1 / FOUNDATION IMPLEMENTED:** player and equipment item level remain capped at 100. Post-100 item power comes from deliberately applying Empowerment to eligible T1 numeric explicits, with per-item caps of 1/2/3/4/5/6 at combat levels 120/160/210/260/310/360. Empowerment Catalysts exist in inventory/save/crafting but intentionally have no normal-enemy drop source.

**LOCKED FOR V1 / CONTENT NOT YET IMPLEMENTED:** optional challenge bosses require an entry/farming loop and reward Empowerment plus boss-specific crafting. Stable special-affix pool and same-side Legendary replacement architecture exists, but actual bosses, pool contents, catalysts, encounter rules, and rewards remain future content. Deep endgame also requires an extremely rare bounded implicit-repair/replacement path; ordinary crafting cannot alter implicits.

The target item journey is natural-drop selection and finite Crafting Potential at levels 1–100, then player-built refinement of strong ilvl-100 gear. Zone 360 is the final V1 main-world mapping point, but equipment item level remains capped at 100 and the current placeholder catalog is not authored world completion.

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

**CURRENTLY IMPLEMENTED:** schema-8 atomic primary/backup JSON, validated transactional restore, encounter-start resume, deterministic run identity, stable gear/relic/class/weapon state, and sequential V1→V2→V3→V4→V5→V6→V7→V8 migration. Schema 8 adds class, subclass-unlock/selection, and weapon-type identity. New Game confirms replacement and clears run/relic history/currency as contracted.

### Step 16 scope lock

V1 targets six base classes, six freely equippable weapon types, and exactly two authored active skills per weapon. The twelve final weapon-skill assignments remain TBD, as do the twelve production subclass identities/effects. The story-boss milestone hook exists, but the boss/content does not. No base-class innate stat bonuses or Accuracy stat ship in this step. See [CLASS_WEAPON_ARCHITECTURE.md](CLASS_WEAPON_ARCHITECTURE.md).

## 20A. Step 14.5 item/crafting content status

Natural Normal/Magic/Rare/Legendary equipment has 6/8/10/14 maximum Crafting Potential. Ordinary upgrades/rerolls cost 1; Add/Remove cost 2. Upgrades retain their lower origin maximum. Empowerment Catalyst is logical/placeholder content only. A developer-created in-memory special-affix fixture verifies the API; no production boss pool or boss catalyst asset exists.

Inventory overload is a release UX requirement: later pickup/filter/Codex work must support desired-mod profiles, match counts/ranking, and stronger auto-dismantle decisions. This is not permission to redesign the filter during unrelated content work.

## 21. Release-Required Polish

**PARTIALLY IMPLEMENTED:** strict builds, smoke checks, reference validation, and extensive regression fixtures exist. Release still requires scoped content, final art/audio/onboarding/options decisions, human playtesting, balance follow-up, player-facing save recovery, warning cleanup triage, and clean distributable QA.

## 22. Explicit Post-V1 / Stretch Content

**POST-V1 / STRETCH unless later promoted:** minions, cloud conflict/multiple save-slot UI, unapproved Accuracy/Block/cooldowns, extra skills, and active boss mode. Achievements and the campaign’s missing content are not automatically stretch—they remain decisions because the current UI/product framing references them.

## 23. Content-Complete Exit Criteria

All locked systems pass focused and full regression; every required biome/scene/enemy/boss/skill/art/audio/tutorial/options count is approved and present; the post-ilvl-100 progression contract is approved before late-world balance; all v1 encounters are playable and save-safe; menus and accessibility requirements are complete; one clean Windows build passes startup and representative New Game→combat→Rebirth→save/load QA; no release-blocking placeholders remain.

## 24. Unresolved User Decisions

1. Approve required normal-enemy and boss rosters/mechanics, including final boss.
2. Confirm whether the seven current skills are the complete v1 set.
3. Classify active top-down/bullet-hell boss play as v1 or post-v1.
4. Approve audio, tutorial, accessibility/input, achievement, and final art scope/counts.
