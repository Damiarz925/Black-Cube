# CRITICAL PLAYTEST BUGS

This completes the implementation following checkpoint `49d7acd3` on
`codex/repository-cleanup-baseline`. The task began at `26d4b483` with a clean
checkout. The historical checkpoint document remains a record of that earlier,
partial delivery; this document supersedes its remaining-work list.

## Passive side-node bug

Source reproduction: an off-class choice popup attempted to resolve the native
subclass choice against a different class, then dereferenced a missing authored
node. Generic choices were legal but popup population aborted. Inaccessible
subclass choices are now excluded before resolution. Regression exercises Refund
All, Bleed→Momentum subclass swap, a different keystone, a new route, allocation
restore and real popup button invocation for both off-class sides.

Unlocks are historical progression state, not an obligation to keep the original
31 native points allocated forever. Native/off-class refunds, choice changes,
selected weapon specialization and unlocked routes survive restore. New unlocks
still require their normal completion gates. This is not a special case for one
Warrior build. Original-state route/hub/scroll regressions failed before repair.

## Weapon-tree interaction

The Player Hub Image had raycasting disabled while its Button expected input.
The authored prefab and authoring generator now enable the actual input target.
Focused tests cover that binding and selected weapon-spine allocation after respec.
Native hover/focus/click/refund visual acceptance still belongs to manual playtest.

## Encounter-selector freeze

The user clarified the failing screen is the enemy/encounter selector. It owned a
paused clock, then skipped restoring it when closing after starting an active
challenge. This let parts of the fight advance while scaled presentation stalled.
Selector and crafting panels restore their captured prior time scale on close,
including active challenges and already-paused gameplay. Tests cover both prior
clock states. No optimizer timeout or background balance search was added.

## Inventory scroll

The ScrollRect lacked a valid content binding and covered the wrong artwork
region. The grid is now bound as content, its immediate masked parent is explicitly
bound as viewport, and content height comes from GridLayoutGroup/ContentSizeFitter.
Mouse wheel scrolling and clipping are retained. Runtime no longer reapplies grid
cell sizes/spacing every frame. Final authored-UI checks verify these references.

# Sustain and combat

All six classes use a shared zero-Life/zero-Mana-regen baseline. The hidden shared
7 Mana/sec was removed. Class definitions already deliberately granted no innate
stat packages. Gear, passives, skills, auras and explicit recovery remain valid.

Life Regen remains percentage points in serialized stats, converted to a fraction
of Maximum Life/sec by StatsComponent. Gear and authored passive Regen values
were multiplied by three, with an idempotent migration marker. Representative
perfect gear ceilings: Body 1.76→5.28%, Helmet 1.28→3.84%, Belt .25→.75%.
The ordinary Body/Helmet tier ranges are their previous ladders ×3, not a new tier
distribution. Weapon/class authoring builders preserve the new sustain conventions.

Specialized ceiling: perfect regen gear 9.87%, Barbarian choices 1.425%, Axe choices
10.725% = 22.02% Max Life/sec before recovery effects. This is a best-choice ceiling,
not a typical build or a validated optimal 100-point allocation. A further 40%
Life Recovery Effect produces 30.828% effective regeneration; access/travel and
perfect-roll opportunity costs are substantial. No class-wide balance survey ran.

Blood Engine sets actual regeneration to zero and converts half the would-be
regen fraction into instant actual-damage recovery. At 30% would-be regen, it heals
15% of damage dealt. At the 22.02% ceiling, its base recovery fraction is 11.01%:
100 actual damage heals 11.01 before recovery-effect modifiers, versus 3.7 from
one ordinary Warrior Life-on-Hit choice. It is not flat Life/sec converted to damage.

Passive Life-on-Hit ×.10; passive Mana-on-Hit ×.15. Gear on-hit tiers are unchanged.
Warrior examples: Life-on-Hit 37→3.7, Mana-on-Hit 18.5→2.775. A three-hit Flurry:
111→11.1 Life and 55.5→8.325 Mana from one such passive; Double Volley with two
projectiles: 74→7.4 Life, 37→5.55 Mana. Additional hits scale these values normally.
There is no new internal cooldown. Consolidation changes the logical hit count.

Rapid Flurry now costs 25 base Mana (25% of the fresh 100-Mana pool; existing skill
level cost scaling still applies). Skills UI, hover details, Workbench and Combat
Lab consume the production definition. Three normal strikes consolidate into one
3.10× strike under One Decisive Strike, not three separately multiplied base
events. That consolidated hit also grants on-hit recovery once. Focused headless
tests use a single half-second action; no balance batch was run.

Revenge accumulates actual Life lost since the last initiated attack, including
multiple hits, self-hits and damaging status ticks; next attack consumes/reset
occurs once. Live and headless paths follow this policy.

Player attack/cast animation starts at action initiation, never at individual
projectile impact. Multihit/Flurry/projectile impacts don't restart it. Goblin's
per-enemy hit-reaction toggle is off; HP, damage numbers and death feedback remain.

Mixed hits lose Life once and display separately styled typed components in
mitigated proportions. The popup fan is deterministic: center, upper-left,
upper-right, higher-left/right. Crit stays yellow/left; Precision is green/right.
Both can display together. Focused tests check three typed numbers, Life deduction
and opposite-side markers.

Wanderer/Enemy effects now bind the authored StatusHUDView instead of searching
only the HUD controller's subtree. Strips sit above the relevant HUD content.
Badges retain their glyph/count styles and hover details. The death recap keeps
five logical incoming events, last event being the killing one, with source,
typed amounts, capped elemental resistance or Armour/explicit-PDR context. DOT
source/type is passed through correctly. History clears on revive/load; no large
combat-log system was introduced.

# Inventory, tooltips and filters

Inventory width 690→560, with its aspect preserved; this also scales equipment,
relic and currency regions. Grid cells 59→45 and spacing 17/9→13/7. The item-icon
RectTransform is authored in its slot prefab. Existing equipment/currency layout
is not continually moved by runtime code. New state overlays have small defaults.

Hold Alt over an unequipped inventory item for a side-by-side currently equipped
comparison, with horizontal screen-edge correction. Equipped/enemy views don't
create duplicate comparisons. L while hovering inventory or player equipment
toggles a visible/persisted lock. Enemy inspection cannot lock enemy gear. Locks
block manual/filtered dismantling, ordinary crafting, empowerment and boss-special
replacement at the gameplay authority, not only by disabling a button.

The optional `isLocked` save field defaults false on older saves; existing schema
13 remains compatible. Filter preferences are persisted separately in PlayerPrefs.

Estimated DPS Upgrade arrows use canonical noncritical attack contexts, crit,
attack speed, multistrike, projectile/precision rules, shared Workbench ailment
magnitude/duration helpers, active stat modifiers and aura bonuses. Candidate
replacement excludes old item/progression contributions, then reapplies legal
weapon-specific passives. Relics and unchanged gear remain included. No encounter
simulation is run per cell. Cache invalidates on gear/passive/stat changes and
meaningful aura changes; at most four missing previews are evaluated per refresh.
Mana regeneration does not invalidate every cell unless current Mana scales damage.
This is a neutral-target basic+modeled-ailment estimate, NOT a skill-rotation DPS
guarantee, mitigation solver, boss forecast or long-ramp poison simulation.

Advanced pickup filtering is distinct from existing-inventory Mod Highlight.
Open ADVANCED LOOT in Inventory, enable the master policy, cycle ITEM TYPE to the
desired armour/jewelry slot or weapon, and choose requirements. Each scope retains
its own ALL/ANY, minimum-count and requirement settings. Rarity always wins over
mods. Six weapon types and five elements support multi-select. T3 accepts T3/T2/T1.
IMPLICIT REQUIRED uses the separate implicit tier; EXPLICIT doesn't count an
implicit as an explicit affix. ANY counts selected requirements, not duplicate
rolls. If ALL selects more explicit families than the rarity permits, the item
must have its full legal explicit affix set drawn from that desired pool; implicit
requirements still apply. Current capacity is Rare=4, Legendary=6, not the brief's
hypothetical six-mod Rare example. Locked items are protected from rejection.

Legal lists use canonical production item pools and weapon-exclusive restrictions.
Legacy More Damage, ailment penetration/resistance, typed skill levels and retired
gear families aren't reintroduced. Legacy saved items retain exact existing rolls.
Physical→Bleed, Fire→Ignite, Cold→Chill, Lightning→Shock, Void→Poison implicit
eligibility is already enforced by production rolling. Audit of 90 seeded cases
(six weapons × five elements × three seeds) found no current illegal combination;
no fictitious new implicit bug fix is claimed.

# Currency audit

Manual and filtered pickup already share Inventory.TryDismantle. There was no
missing-fragment deletion bug to repair in the current source. Focused tests now
explicitly cover manual Rare alongside auto Rare, locked items and duplicate claims.
Each Rare grants one Magic→Rare fragment; ten combine into one orb. Legendary
grants two. Magic gives one Normal→Magic fragment, Normal none.

Small controlled progression sample: 250 reward rolls, 50 each at L10/25/50/75/100,
neutral enemy power, repeating Normal/Magic/Rare enemy rarity. Direct expected
Magic→Rare income 11.95, seeded observed 16. It generated 250 items: 69 Rare,
6 Legendary. Rejecting all those rares adds 69 fragments (6 orbs +9 fragments),
or 81 including Legendary fragments (8 orbs +1 fragment). Manual versus auto
split doesn't change total. Before/after current-source fragment handling is
unchanged: observed 16 direct alone vs up to 24 +1 fragment including these
dismantles. This is a rough reward sample, not a measured playthrough. Direct
drop rates were NOT increased because fragment availability is already meaningful.

# Armour and diagnostics

Only Body, Helmet, Gloves and Boots have inherent base Armour. Bases at L1/25/50/75/100
follow `round(maximum × (.02 + .98 × (clamp(level,1,100)/100)^1.65))`.
L100 maxima: Body 4454, Helmet 2475, Gloves/Boots 1485 each. Jewelry, weapons,
Belts and relics have zero inherent Armour.

| Item level | Body | Helmet | Gloves / Boots each |
|---|---:|---:|---:|
| 1 | 91 | 51 | 30 |
| 25 | 532 | 296 | 177 |
| 50 | 1480 | 822 | 493 |
| 75 | 2804 | 1558 | 935 |
| 100 | 4454 | 2475 | 1485 |

Item Armour = (base + local flat) × (1 + local increased Armour).
The final item amount enters the actor's flat Armour sum exactly once; that
piece's local % does not also enter global Armour. Global passive/effect/other
increased Armour then scales the sum. Existing local affix tier distributions
are retained; the new bases and +100% reference global investment calibrate the
requested target without broadly changing unrelated gear tiers.

Reference: documented 2053.714-sized L100 Rare hit. It is a hypothetical Physical
hit of that size; the sampled location's Fire identity is not mitigated by Armour.
Perfect local gear = 30813.9 Armour; +100% global = 61627.8. Body+Helmet explicit
PDR max rolls total .20. Armour / (Armour + 10 × hit) + explicit PDR = 95.005% raw,
90% actual after clamping. .02 explicit PDR is 2%, .10 is 10%, unchanged stable
fraction units. A larger hit has lower Armour reduction; this isn't elemental
resistance's fixed reduction. Armour is never capped.

All eligible native Warrior Armour choices total +500% global, yielding 184883.4
with this perfect gear before further sources, about 110% raw with .20 explicit
PDR, still 90% actual. Excess Armour remains offensive input to Armour Strike.
This is a source ceiling, not an optimized practical build.

Stats is compact and grouped: Offense, Damage Types, Defense, Resources, Special.
Advanced Sources is initially collapsed and shows local base/flat/%/item Armour,
global scaling, raw reference PDR and other detailed source rows. Item tooltips
show final item Armour and LOCAL labels without a full diagnostics table.

# Pause, Options and settings

Pause stops combat/time-scaled gameplay without closing Inventory or disabling
the EventSystem. Gear, sorting, crafting, passives and Stats remain interactive.
Options is a separate upper-right button. Closing Options restores its prior
clock, including a combat pause. SAVE / CODEX / EXIT opens existing game-menu
actions. Continue Running While Unfocused and Weapon Skill Tooltips persist in
PlayerPrefs; runInBackground applies on startup and immediately when toggled.

Weapon-skill hover details show identity, current character/weapon basis and types,
Mana cost, cooldown, hit/projectile counts, projectile speed and estimated
noncritical direct damage per use. Armour Strike and consolidated Flurry use
their current mechanics. Ailments/conditional triggers are explicitly excluded
from that tooltip estimate. Staff readiness remains automatic, not queued.

# Verification and Git

32/32 final focused checks passed in `Logs/PlaytestCompletionVerifiedTests.xml`
(about one second of test execution, excluding Unity import/startup). This includes
the critical regressions, six fresh-class baselines, filters/locks/save roundtrip,
local/global Armour and target, currency equivalence/sample, Flurry consolidation,
canonical estimate isolation and production UI bindings. Build/startup results
and completion commit are recorded in the final chat handoff. Logs live under
Logs/PlaytestCompletion*. No BalanceLab or
large simulation batches ran. Native visual/mouse playthrough is not claimed;
the user will perform broader manual acceptance. Production prefab binding tests,
targeted mechanics tests, a fresh Windows x64 build and startup smoke are separate
forms of verification and aren't described as native playtesting.

Fresh Windows x64 build succeeded, zero errors / 830 warnings (mostly obsolete
API and authored-geometry warnings), `Logs/PlaytestCompletionBuild.log`.
Fresh player startup reached MainMenuUI with Load enabled and no managed
exceptions, `Logs/PlaytestCompletionStartup.log`. Existing achievements-button
binding warning remains. The smoke did not load/change the user's save, and only
the spawned test-player process was closed.

Completion change set: 54 modified files, 17 added (including eight new C# files
and their metas, plus this document), zero removed. The four critical bug fixes
are in checkpoint `49d7acd3`; the follow-up commit completes systems/data/UI/tests.

Executable: `Builds/GenericClassPassiveWindows/BlackCube.exe` (generated/ignored,
not source-controlled). Source/data/prefabs/tests and this document are committed;
logs/build output are not. No files removed, no push, main untouched. Unrelated
passive-geometry serialization generated during prefab migration was restored
to the checkpoint's exact instance; enemy portrait geometry was not changed.

# HOW TO MANUALLY EDIT THE INVENTORY / STATS / MOD-HIGHLIGHT UI

1. Exit Play Mode. In Project, double-click
   `Assets/Prefabs/PaperBattle/PaperBattle.prefab` to open Prefab Mode. This is the
   production composite: several UI copies are inline, not linked to reference
   prefabs. Editing only `Assets/Prefabs/UI/InventoryPanel.prefab` does not
   automatically update this inline production copy. Edit production first;
   mirror reference changes only if you want the reference to match.
2. Search the prefab Hierarchy for the object with InventoryUI/InventoryView.
   Select that panel's RectTransform: anchored position moves the whole panel;
   width/height changes its bounds. Preserve its top-left pivot and intended
   aspect unless deliberately redesigning the artwork.
3. Follow InventoryView → Item Grid Root to `Compact inventory grid`. Edit its
   GridLayoutGroup Cell Size, Spacing, Padding, Constraint Count. The grid's
   ContentSizeFitter owns vertical content height, so don't hand-fix that height.
   Follow its parent to Viewport: edit RectTransform for the clipped region.
   Keep ScrollRect Content and Viewport assigned, Vertical enabled and RectMask2D.
4. Open `Assets/Prefabs/Other Prefabs/Item Slot.prefab`. Follow ItemSlotUI's Icon
   Image reference and adjust its RectTransform anchors/offsets for icon placement
   inside cells. Detail Text controls the level label. Don't modify a generated
   Play Mode instance; it disappears when Play Mode ends.
5. Back in PaperBattle, follow InventoryView Equipment Slots and Active Relic
   Slots to reposition their authored RectTransforms. EquipmentStatsUI Slot Icon
   references expose icon size independently of the hotspot. Follow Currency Tray,
   Ordinary Currency Root and Ancient Currency Root to resize currency icons and
   spacing. If a layout group owns a child, edit its group or LayoutElement rather
   than fighting the calculated child position.
6. For Stats, select the panel referenced by PaperBattleHUD → Stats Panel.
   Change its RectTransform position/size; `Stats scroll` anchors control inner
   bounds. Follow PlayerStatsPanelUI → Content Root for VerticalLayoutGroup spacing.
   Open its Row Prefab (`Assets/Prefabs/PaperBattle/StatRow.prefab`) for row height,
   name/value text anchors and font size. StatHeader prefab controls section headers.
   Runtime populates rows and text, not the outer panel/viewport location.
7. For Mod Highlight, select InventoryModHighlightUI, follow Panel / Simple Root /
   Advanced Root. Edit individual authored buttons' RectTransforms and group
   spacing/padding. Change the panel RectTransform for menu bounds. Open Button,
   mode controls and required-count controls are explicit references; move them
   directly. Runtime wires their actions and labels without rebuilding their geometry.
8. For the new pickup filter, select AdvancedLootFilterUI → Panel. Header controls
   and rarity/weapon/element buttons are authored. `Legal modifier rows` has one
   row template: change its LayoutElement height and four button RectTransforms;
   runtime clones that template once and only changes legal visibility/state.
   Do not duplicate hundreds of rows manually.
9. For Options, follow PauseMenuView → HUD Options Button and edit its
   RectTransform. Its settings-panel references expose card dimensions and toggle
   button positions. Weapon Skill Hover Details and its Details TMP object control
   skill-tooltip card dimensions and typography.
10. Save the prefab (Ctrl+S), leave Prefab Mode, enter Play Mode. Populate more
    than visible inventory rows, wheel-scroll, compare with Alt, toggle L locks,
    pause and craft, open/close Options from both running and paused states, hover
    skills, inspect active effects, and die once to check recap fit. Do not rerun
    the one-time authoring migration after manual geometry work: it intentionally
    applies this pass's initial compact metrics. Ordinary runtime preserves them.
