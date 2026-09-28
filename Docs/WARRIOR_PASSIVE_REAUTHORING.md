# Warrior passive tree: single-slot authoring handoff

## Scope and structure

The updated steering brief replaces the proposed visible multi-class / multi-choice expansion. The menu shows the active class only. Warrior has a neutral bottom player hub, its badge on the hub's upper edge, ten upward Strength spine nodes, and twenty side choice slots. The wider lower slots imply a crossguard; the upper slots narrow toward the blade tip. Readability takes priority over a literal sword silhouette.

Each empty slot is a procedural glowing ring. Clicking it opens a four-button authored popup: three generic alternatives and the existing selected-subclass alternative. The fourth option is locked until the existing subclass requirements are satisfied. Allocating a choice fills the slot with its actual icon. Click an allocated slot for details; right-click to refund using the existing refund rules. Refund before choosing another alternative. Each spine and selected side choice still costs one passive point.

The native signature weapon route becomes visible after completing the ten native spine nodes, preserving its existing unlock rule. Other class branches are hidden, not deleted. Other class/weapon numeric data and subclass numeric data were not rebalanced. Full multi-class selection/expansion is deferred. Legacy off-class allocations remain stored and effective, but are not editable through the new active-class-only display; the existing refund-all action remains available.

Logical IDs, choice exclusivity, allocated ranks and save schema **12** are unchanged. Slot selection is the already-saved allocated logical choice; no second selection store or schema migration was introduced. Existing Warrior ranks retain their identities and receive the reauthored Warrior effects.

## Values and option rotation

Every spine gives **+10 flat Strength**, for +100 if all ten are allocated. The increased-Strength-percent icon is not used for flat Strength; spines retain the existing flat-Strength artwork.

Generic values are half the current production T1 affix midpoint, rounded to the nearest 0.5, with midpoint rounding away from zero. They are uniform across tiers; there is no automatic tier inflation. Values below use the actual dirty ModDatabase already in the checkout, without editing that asset.

| Stat | T1 range | Half midpoint | Node value | Supplied icon |
|---|---:|---:|---:|---|
| Maximum Life (percentage) | 20–40 | 15 | 15% | Warrior_Life.png |
| Increased Armour | 129–270 | 99.75 | 100% | Warrior_Armour.png |
| All Elemental Resistance | 8–16 | 6 | 6% | Warrior_AllElementalResistance.png |
| Life on Hit | 48–100 | 37 | 37 flat | Warrior_LifeOnHit.png |
| Bleed Chance | 82–170 | 63 | 63% | Warrior_BleedChance.png |
| Multistrike Chance | 7–13 | 5 | 5% | Warrior_Multistrike.png |
| Increased Bleed Damage | 55–113 | 42 | 42% | Warrior_BleedDamage.png |
| Attack Speed | 13–27 | 10 | 10% | Warrior_AttackSpeed.png |
| Increased Critical Strike Chance | 55–113 | 42 | 42% | Warrior_CriticalStrikeChance.png |
| Mana on Hit | 24–50 | 18.5 | 18.5 flat | Warrior_ManaOnHit.png |
| Cooldown Reduction | No production gear tier | — | 10% provisional | Warrior_CooldownReduction.png |
| Increased Strength | 10–19 | 7.25 | 7.5% | Warrior_IncreasedStrengthPercent.png |

All Elemental Resistance means Fire, Cold and Lightning, **not Void**. Maximum Life here means the existing percentage-Life stat, not the very large flat-Life gear affix. Cooldown Reduction is an explicit first-pass exception, not a newly invented gear mod. Multistrike is the player-facing name for the existing ChanceToHitTwice stat (serialized ID 83); its combat behavior is unchanged. Critical Chance is increased chance, not added percentage points of final crit probability. These are first-pass values, not a final balance claim.

Each row below lists the three generic alternatives. The existing subclass alternative is the fourth popup choice on both sides.

| Tier | Left A | Left B | Left C | Right A | Right B | Right C |
|---:|---|---|---|---|---|---|
| 1 | Life | Armour | Mana on Hit | Bleed Chance | Multistrike | Crit Chance |
| 2 | Elemental Resistance | Life on Hit | Crit Chance | Bleed Damage | Attack Speed | Cooldown Reduction |
| 3 | Life | Elemental Resistance | Cooldown Reduction | Bleed Chance | Bleed Damage | Increased Strength |
| 4 | Armour | Life on Hit | Mana on Hit | Multistrike | Attack Speed | Crit Chance |
| 5 | Life | Life on Hit | Crit Chance | Bleed Chance | Attack Speed | Increased Strength |
| 6 | Armour | Elemental Resistance | Cooldown Reduction | Multistrike | Bleed Damage | Mana on Hit |
| 7 | Life | Armour | Mana on Hit | Bleed Chance | Multistrike | Cooldown Reduction |
| 8 | Elemental Resistance | Life on Hit | Crit Chance | Bleed Damage | Attack Speed | Increased Strength |
| 9 | Life | Elemental Resistance | Cooldown Reduction | Bleed Chance | Bleed Damage | Crit Chance |
| 10 | Armour | Life on Hit | Mana on Hit | Multistrike | Attack Speed | Increased Strength |

## Where to edit

| What | Authoritative location |
|---|---|
| Warrior effects, amounts, names and popup alternatives | `Assets/GameData/PassiveTree/Branches/Class/SO_Warrior_Branch.asset` |
| Per-option icons | The same SO: node Icon Mode = Custom, Icon Override = supplied sprite |
| Spine and slot positions, sizes, labels, popup geometry | `Assets/Prefabs/UI/PassiveTreePanel.prefab` RectTransforms and components |
| Hub/badge sprite, sizes, radial attachment, fit padding, zoom limits, weapon gap | `Assets/Resources/GameData/PassiveTree/SO_PassiveTreeLayout.asset` |
| Four-button popup references | PassiveTreeView.choicePopup → PassiveChoicePopupView on the prefab |
| Which logical alternatives a physical slot represents | PassiveChoiceSlotView.choiceGroupId on the prefab; keep existing stable group IDs |
| Exact first-pass seed table and T1 derivation | `Assets/Editor/WarriorPassiveReauthoring.cs` (explicit reset/migration, not runtime balance source) |
| Imported art | `Assets/Art/UI/PassiveTree/Warrior/` (all fourteen ZIP PNGs, unmodified) |

Hub uses `Warrior_PlayerHub.png`; the badge uses `Warrior_ClassBadge.png`. Those plus the twelve table icons exhaust the supplied set. Generic and subclass nodes share the same slot/popup mechanism but retain distinct existing logical IDs. Edit A/B/C effects within a side to change its three choices; edit Subclass A/B only when deliberately changing subclass design. Keep stable data IDs and logical slot IDs unchanged.

The existing branch inspector exposes effects, values, custom icons, validation, previews, copy/paste and tier foldouts. The new layout window supplements it; it does not replace the existing authoring database.

## HOW TO EDIT THE WARRIOR TREE VISUALLY

1. Exit Play Mode. Open **Black-Cube → Passive Tree Authoring → Layout**, or select the Warrior branch SO and click **LAYOUT VIEW**.
2. Leave Branch set to `SO_Warrior_Branch`. Click **Open Production Prefab** to enter Prefab Mode for `PassiveTreePanel.prefab`.
3. Click **Frame Branch**. The preview reads actual prefab node/slot RectTransforms and draws straight lines between their endpoints.
4. Click a spine or side slot. Drag it, or enter exact **Local X / Y** coordinates above the preview. Use the zoom slider or mouse wheel; right/middle-drag pans. Unity Undo/Redo is supported.
5. Alternatively select the same object in the Prefab hierarchy and use normal Unity RectTransform tools. Slot coordinates are relative to the Warrior branch root. Connections derive from endpoints and follow automatically; do not reposition their line images manually.
6. Click **Save Prefab** (or save in Prefab Mode). Do not run the old whole-tree prefab generator: it would replace the authored single-slot presentation.
7. Select the Warrior SO to edit effects/options/icons. Expand the tier and LEFT/RIGHT CHOICES, then A/B/C. Expand SPINE for flat Strength. Validate the branch after changes.
8. Enter Play Mode as Warrior. Open the passive menu; check the bottom hub/badge, ten spines and twenty single slots. Allocate a spine, click a side slot, choose an option, confirm the icon and details, then right-click to refund. Save/load to confirm the selected icon persists.
9. Complete the ten spine nodes and verify the Sword route appears above Warrior. Check zoom-out fits the newly visible bounds and dragging cannot lose the tree completely. Test smaller/larger window sizes.

Runtime moves the branch roots/hub/badge to fit the visible content, but does not rewrite individual spine/slot coordinates. The choice popup is an ordinary authored prefab panel. The layout window previews nodes and lines, not a complete live game Canvas or popup interaction.

**Do not routinely run Apply Warrior Reauthoring after hand-authoring values/icons.** It deliberately reapplies the first-pass stat table from the current gear database. The conversion is idempotent for already-converted node positions, but it remains an explicit content reset. Use it only when intentionally restoring this baseline.

## File inventory

Modified task files:

- `Assets/GameData/PassiveTree/Branches/Class/SO_Warrior_Branch.asset`
- `Assets/Prefabs/UI/PassiveTreePanel.prefab` (incremental task edits on top of the pre-existing user edits)
- `Assets/Scripts/Authoring/PassiveTreeAuthoringData.cs`, `PassiveTreeView.cs`, `UIAuthoringViews.cs`
- `Assets/Scripts/SkillTreeUI.cs`, `StatDisplayFormatting.cs`, `PassiveTreeDefinition.cs`, `RelicModifierDefinitions.cs`, `SubclassSystems.cs`, `CombatSimulationCore.cs` (last four changes are display terminology, not mechanic retuning)
- `Assets/Editor/PassiveBranchAuthoringEditor.cs`, `UIAuthoringValidation.cs`
- `Assets/Editor/BalanceWorkbenchPlayerBuild.cs`, `BalanceWorkbenchPlayerUI.cs` (Multistrike display labels; stable metric key unchanged)

Added task files, with Unity `.meta` files:

- `Assets/Art/UI.meta` and `Assets/Art/UI/PassiveTree/Warrior/` with its parent folder metadata and fourteen PNGs
- `Assets/Scripts/Authoring/PassiveChoiceSlotView.cs`, `PassiveChoicePopupView.cs`, `PassiveTreeLayoutSO.cs`
- `Assets/Scripts/EmptyPassiveSlotGraphic.cs`, `PassiveChoiceSlotInput.cs`, `PassiveTreePresentation.cs`
- `Assets/Editor/WarriorPassiveReauthoring.cs`, `PassiveTreeLayoutWindow.cs`, `WarriorPassiveVerification.cs`
- `Assets/Resources/GameData/PassiveTree/SO_PassiveTreeLayout.asset`
- `Assets/Tests/Editor/WarriorPassiveReauthoringTests.cs`
- `Docs/WARRIOR_PASSIVE_REAUTHORING.md`

The pre-existing ModDatabase edits, UIAuthoringReport and CurrentPassiveTreeReference are not task changes. Baseline backups are under ignored `Logs/WarriorReauthoring/Baseline/`. No global SaveAssets or whole-prefab regeneration was used. No push, main modification, broad balance survey or combat balance simulation is part of this pass.

## Validation artifacts

Focused tests cover the sixty generic alternatives and values, ten spines, unchanged subclass values, fourteen sprites/import settings, stable Multistrike identity, elemental/Void behavior, thirty physical Warrior positions, popup allocation/refund, save round-trip, visible bounds/weapon unlock, and Undo/Redo/connection geometry.

Local test XML, migration logs, build report and four rendered UI states are under ignored `Logs/WarriorReauthoring/`. These are verification outputs, not source assets for Git. Native manual Editor/popup click testing is distinct from automated component tests and rendered Canvas inspection; use the workflow above for hands-on review. Final run results are recorded separately in this directory when available.

Final validation (September 27, 2026 local time):

| Check | Result / artifact |
|---|---|
| Complete EditMode suite | **571 passed, 0 failed, 0 skipped**; `Logs/WarriorReauthoring/Full-Release.xml` |
| Focused Warrior coverage | All **13** Warrior tests passed within that complete run |
| Data structure / authored UI validation | Passed within migration and focused tests |
| Four production-prefab Canvas states | Rendered and inspected: empty tree, popup, selected Life, Sword unlocked; `Logs/WarriorReauthoring/Captures/` |
| Fresh Windows x64 build | **Succeeded, 0 errors, 1,666 warnings**; `Logs/WarriorReauthoring/BuildResult.txt`, `Build.log` |
| Player startup smoke | Headless 12-second launch reached `MainMenuUI.Awake`, no exceptions; `StartupSmoke.log` |
| Source whitespace check | `git diff --check -- Assets/Scripts Assets/Editor` passed; Unity-generated YAML / pre-existing YAML whitespace not normalized |
| Subclass preservation | All 40 raw Warrior subclass blocks match the baseline exactly |
| Art preservation | All 14 PNG SHA256 hashes match their ZIP entries |
| User dirty data/report preservation | ModDatabase and UIAuthoringReport SHA256 hashes match the start-of-task backups |

Executable: `Builds/WarriorPassiveWindows/BlackCube.exe`. Build warnings include existing obsolete Unity API calls, a StatusBadge serialization warning, and numerous packaged Sentis shader warnings. The startup log also contains the existing `MainMenuUI: achievementsButton not assigned` warning. None were changed in this scoped tree pass. Startup smoke used a hidden headless process and stopped only that launched process; it is not native mouse/keyboard interaction validation. No source changes were made after the final full test run except this documentation.

Changes are left **uncommitted and unpushed**. The production prefab necessarily includes the pre-existing user edits plus this incremental conversion, so do not blindly stage every dirty file as task-owned.

## Deferred follow-ups

- Full multi-class expansion and editing legacy off-class allocations in a unified view.
- Equivalent single-slot reauthoring and dedicated art for other classes; they currently keep their prior branch presentation when active.
- Final balancing, especially high current gear-derived Bleed Chance / Armour values and provisional Cooldown Reduction.
- Final popup styling, visual hover polish and accessibility/navigation polish after hands-on review.

Weapon/subclass balancing, enemy portraits and unrelated UI/gear edits are explicitly outside this pass.
