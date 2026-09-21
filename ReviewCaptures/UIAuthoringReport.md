# Black-Cube UI Authoring Report

Generated: 2026-09-21T19:59:56.1271432Z

## Production scenes

- `Assets/Scenes/SampleScene.unity`
- `Assets/Scenes/Main Menu.unity`

## Authoring assets

- Passive database: `Assets/Resources/GameData/PassiveTree/SO_PassiveTreeDatabase.asset`
- Passive icon library: `Assets/GameData/UI/Libraries/SO_PassiveNodeIconLibrary.asset`
- Passive effect catalog: `Assets/GameData/UI/Libraries/SO_PassiveEffectCatalog.asset`
- Class branches: `Assets/GameData/PassiveTree/Branches/Class`
- Weapon branches: `Assets/GameData/PassiveTree/Branches/Weapon`
- Global visual library: `Assets/GameData/UI/Libraries/SO_UIVisualLibrary.asset`
- Button visual style: `Assets/GameData/UI/Libraries/SO_DefaultButtonVisualStyle.asset`

## Major authored prefabs

- `Assets/Prefabs/UI/ChallengePanel.prefab`
- `Assets/Prefabs/UI/CharacterSlots.prefab`
- `Assets/Prefabs/UI/EndgameCraftingPanel.prefab`
- `Assets/Prefabs/UI/EnemyInspectionPanel.prefab`
- `Assets/Prefabs/UI/GameplayHUD.prefab`
- `Assets/Prefabs/UI/InventoryPanel.prefab`
- `Assets/Prefabs/UI/ItemTooltip.prefab`
- `Assets/Prefabs/UI/MainMenu.prefab`
- `Assets/Prefabs/UI/ModListPanel.prefab`
- `Assets/Prefabs/UI/PassiveTreePanel.prefab`
- `Assets/Prefabs/UI/PauseMenu.prefab`
- `Assets/Prefabs/UI/RebirthPanel.prefab`
- `Assets/Prefabs/UI/SkillSelectionPanel.prefab`
- `Assets/Prefabs/UI/StatsPanel.prefab`
- `Assets/Prefabs/UI/StatusBadge.prefab`
- `Assets/Prefabs/UI/StatusHUD.prefab`
- `Assets/Prefabs/UI/SubclassPanel.prefab`

## View/controller bindings

- GameplayHUDView → PaperBattleHUD
- StatusHUDView → StatusHUD
- InventoryView → InventoryUI
- PassiveTreeView → SkillTreeUI
- SkillSelectionView → PlayerSkillMenuUI
- SubclassView → SubclassMenuUI
- ChallengeView → ChallengeLauncherUI
- EndgameCraftingView → EndgameItemizationUI
- RebirthView → RebirthConfirmationUI
- EnemyInspectionView → EnemyInspectionPanelUI
- PauseMenuView → PauseMenuUI

## Runtime placement exceptions

- Passive connection-line RectTransforms derive from assigned node/junction RectTransforms in edit mode and play mode.
- Pointer-following authored tooltip instances and the crafting cursor change temporary screen position.
- Variable inventory, stat, mod, relic, and active-status entries instantiate authored row/slot presentation as data requires.
- Item/equipment marker, rarity/corruption-border, filter-highlight, and corruption button-frame children are state-dependent overlays inside authored hosts.
- Floating combat text, projectiles, enemies, and world drops are transient prefab instances.

## Validation

PASS

