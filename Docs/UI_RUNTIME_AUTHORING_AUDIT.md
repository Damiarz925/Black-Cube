# Runtime UI Authoring Audit

Baseline: `8e59b250` on `codex/repository-cleanup-baseline`, save schema 12. This report was created before removing any runtime UI construction. The production scenes are `Assets/Scenes/Main Menu.unity` and `Assets/Scenes/SampleScene.unity`. Existing reusable UI prefabs are limited to the inventory manager/item slot, Paper Battle, equipment slot, stat header/row, and damage-popup prefabs; most newer screens are still assembled by scripts.

## A — Persistent screen UI that must become scene/prefab authored

| Current owner | Runtime-authored objects / placement | Migration target |
|---|---|---|
| `PaperBattleHUD` + `TopHUDLayout` | Top HUD art, portrait masks/images, player/enemy labels, resource bars, seven permanent buttons and their measured placement | `GameplayHUD.prefab` with `GameplayHUDView` serialized references |
| `BottomActionBarLayout` | Bottom action-bar root, layout component, child reparenting/order and permanent sizes | Authored action-bar root in `GameplayHUD.prefab` |
| `SkillTreeUI` | Entire panel, viewport/content, central hub, all nodes, junctions, connections, labels, close/refund controls and permanent geometry | `PassiveTreePanel.prefab` containing twelve movable branch roots and explicit slot bindings |
| `PlayerSkillMenuUI` | Entire skill-selection panel, labels and buttons | Authored `SkillSelectionPanel.prefab` |
| `SubclassMenuUI` | Entire subclass-selection panel, labels and buttons | Authored `SubclassPanel.prefab` |
| `StatusHUD` | Player/enemy status roots, fixed badge slots, icons and tooltip | Authored status containers and badge slots in `GameplayHUD.prefab` |
| `InventoryEquipmentPanelUI` | Equipment/relic roots, fixed equipment slots and their icon surfaces | Authored `InventoryPanel.prefab` |
| `InventoryFilterUI` | Permanent filter controls, labels and layout | Authored `InventoryPanel.prefab` |
| `InventoryModHighlightUI` | Permanent mod-filter/list panel and layout | Authored `InventoryPanel.prefab`; variable text rows may use an authored row prefab |
| `CraftingCurrencySystem` | Currency tray, fixed currency slots, fragment labels, captions, buttons and relic inventory root | Authored inventory/crafting views; variable relic rows use an authored row prefab |
| `RelicEquipmentUI` | Fixed active-relic root and slots | Authored `InventoryPanel.prefab` |
| `RebirthConfirmationUI` | Entire confirmation panel and controls | Authored `RebirthPanel.prefab` |
| `ItemTooltipUI` | Tooltip child hierarchy, panels, labels, divider/lock art and dimensions | Authored `ItemTooltip.prefab` |
| `CurrencyTooltipUI` / `RelicTooltipUI` | Tooltip roots and labels | Authored tooltip prefabs, instantiated only as presentation instances when needed |
| `PlayerStatsPanelUI` | Rows are populated from prefabs, but panel ownership and permanent host bindings require explicit views | Authored `StatsPanel.prefab`; stat rows remain data-populated from authored row/header prefabs |
| `EnemyInspectionPanelUI` | Clones the player stats panel, then creates its close button | Authored `EnemyInspectionPanel.prefab` |
| `ChallengeLauncherUI` / `EndgameItemizationUI` | Entire panels, text and buttons through `EndgameUIFactory` | Authored `ChallengePanel.prefab` and `EndgameCraftingPanel.prefab` |
| `CodexModListUI` | Entire mod-list panel, scrolling hierarchy, labels and buttons | Authored `ModListPanel.prefab` |
| `PauseMenuUI` | Runtime-created options hierarchy/labels where serialized scene references do not already exist | Complete authored `PauseMenu.prefab` bindings |
| `MainMenuUI` | Options, overwrite confirmation, class selection, character-slot selection, labels and cloned controls | Authored main-menu child panels and six fixed slot controls in `Main Menu.unity`/prefabs |
| `CorruptionUITheme` / `CorruptionUIButtonSkin` | Adds persistent overlay/frame children and images | Designer-authored overlay children with style references |
| `EquipmentStatsUI` / `ItemSlotUI` | Adds occupied interiors, themed item icons, element markers, corruption borders and filter highlights | Complete these children in the corresponding authored slot prefabs |

## B — Fixed-count data UI

These objects carry changing data but have a known production count and should be authored as real slots: six character slots; six class branches; six weapon branches; 750 passive data definitions and their visible branch slots; seven top-HUD buttons; two weapon-skill controls; equipment slots; active relic slots; ordinary/Ancient currency slots; fragment counters; status badge capacity; challenge entries; class/subclass choices; and fixed crafting controls. Runtime code may update their data and state but not create or position them.

Inventory capacity rows and long statistics/mod lists are data-dependent. Their containers and row appearance must be authored; controllers may instantiate only the corresponding authored row/slot prefab as needed.

## C — Intentional runtime-spawned presentation

| Owner | Reason it remains dynamic | Required authoring boundary |
|---|---|---|
| `DamagePopup` | Transient combat event with variable lifetime and position | Continue pooling/instantiating `Assets/Prefabs/Other Prefabs/Damage Popup Text.prefab`; move the critical marker into that prefab |
| `EnemyRewardDrops` | Variable world-space loot drops | Spawn designer-authored pickup prefabs; sprite/scale come from assets, not generated layout |
| `SkillProjectile` | Transient world-space skill projectile | Replace generated visuals with configured projectile prefab(s) |
| `BattleManager` / `EnemyAI` / `ZoneManager` | Runtime world actors, enemies and encounter scenery | Not screen UI; existing prefab/data ownership remains appropriate |
| `InventoryUI` | Variable number of inventory items | Instantiate only authored `Item Slot.prefab` under an authored grid; never create the grid or alter slot presentation at runtime |
| `PlayerStatsPanelUI` | Variable visible stat rows by equipped item/build | Instantiate authored `StatHeader.prefab` / `StatRow.prefab` under an authored content root |
| Relic/mod list rows | Variable inventory/mod contents | Instantiate authored row prefabs under authored containers |
| Tooltips | Transient presence and pointer-relative placement | Instantiate/show authored tooltip prefabs; content and temporary screen fitting may be runtime behavior |

## Existing assets and gaps

- Gameplay scene: `Assets/Scenes/SampleScene.unity`
- Main-menu scene: `Assets/Scenes/Main Menu.unity`
- Existing gameplay root: `Assets/Prefabs/PaperBattle/PaperBattle.prefab`
- Existing inventory manager UI: `Assets/Prefabs/Managers/Inventory UI.prefab`
- Existing item/equipment slots: `Assets/Prefabs/Other Prefabs/Item Slot.prefab`, `Assets/Prefabs/PaperBattle/EquipmentSlot.prefab`
- Existing stat rows: `Assets/Prefabs/PaperBattle/StatHeader.prefab`, `Assets/Prefabs/PaperBattle/StatRow.prefab`
- Existing popup: `Assets/Prefabs/Other Prefabs/Damage Popup Text.prefab`
- Missing at baseline: dedicated HUD, passive-tree, pause, subclass, skill, challenge, crafting, tooltip, and consolidated inventory presentation prefabs; serialized view/binding components; passive branch ScriptableObjects; passive icon/style libraries; and central UI authoring tooling.

## Migration rule

The migration will preserve all gameplay values and logical stable IDs. Runtime controllers retain behavior only. Layout is serialized in scene/prefab RectTransforms, passive effects are serialized in branch assets, visual state appearance is serialized in style/library assets, and every remaining runtime-instantiation exception must point at an authored prefab.
