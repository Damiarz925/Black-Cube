# UI Binding Map

Runtime controllers own state and behavior. Prefabs and scenes own permanent hierarchy, transforms, art, and visual-state configuration. Moving or resizing a referenced `RectTransform` does not change its logical identity.

| Screen | Production prefab / scene | View | Controller | Important bindings | Layout / visual authority |
|---|---|---|---|---|---|
| Gameplay HUD | `Assets/Prefabs/UI/GameplayHUD.prefab`, installed in `Assets/Prefabs/PaperBattle/PaperBattle.prefab` / `Assets/Scenes/SampleScene.unity` | `GameplayHUDView` | `PaperBattleHUD` | portraits, names, life/mana bars, Skills, Passives, Enemy, Inventory, Stats, Pause/Play, action-bar roots | prefab RectTransforms; `SO_UIVisualLibrary.asset` |
| Status HUD | `Assets/Prefabs/UI/StatusHUD.prefab`, `StatusBadge.prefab` | `StatusHUDView` | `StatusHUD` | player/enemy strips, tooltip, badge prefab | prefab; variable active badges use authored prefab + GridLayoutGroup |
| Inventory | `Assets/Prefabs/UI/InventoryPanel.prefab` | `InventoryView` | `InventoryUI` and inventory subcontrollers | item grid, equipment, relics, currencies, filters, item-tooltip prefab | prefab; item/equipment slot prefabs; visual library |
| Passive Tree | `Assets/Prefabs/UI/PassiveTreePanel.prefab` | `PassiveTreeView`, `PassiveBranchBinding`, `PassiveNodeBinding` | `SkillTreeUI` | scroll/content, 12 branches, node slots, connections, close/refund | prefab geometry; branch SOs; passive icon library |
| Skills | `Assets/Prefabs/UI/SkillSelectionPanel.prefab` | `SkillSelectionView` | `PlayerSkillMenuUI` | skill buttons, finisher, descriptions, close | prefab and button style |
| Subclass | `Assets/Prefabs/UI/SubclassPanel.prefab` | `SubclassView` | `SubclassMenuUI` | open/mode/choice buttons and labels | prefab and button style |
| Rebirth | `Assets/Prefabs/UI/RebirthPanel.prefab` | `RebirthView` | `RebirthConfirmationUI` | open, confirmation, confirm/cancel, message | prefab |
| Challenges | `Assets/Prefabs/UI/ChallengePanel.prefab` | `ChallengeView` | `ChallengeLauncherUI` | entries, resource/description labels, enter/close | prefab |
| Endgame crafting | `Assets/Prefabs/UI/EndgameCraftingPanel.prefab` | `EndgameCraftingView` | `EndgameItemizationUI` | currency/action controls, selected-item labels, close | prefab; currency sprites in visual library/resources |
| Stats | `Assets/Prefabs/UI/StatsPanel.prefab` | `StatsView` | `PlayerStatsPanelUI` | authored content root and row/header prefabs | prefab; `StatHeader.prefab`, `StatRow.prefab` |
| Enemy inspection | `Assets/Prefabs/UI/EnemyInspectionPanel.prefab` | `EnemyInspectionView` | `EnemyInspectionPanelUI` | panel, stats, close | prefab |
| Pause/options | `Assets/Prefabs/UI/PauseMenu.prefab` | `PauseMenuView` | `PauseMenuUI` | resume, save/load, options, menu confirmation | prefab |
| Main menu / slots | `Assets/Scenes/Main Menu.unity`, `Assets/Prefabs/UI/MainMenu.prefab`, `CharacterSlots.prefab` | `MainMenuView`, `CharacterSlotsView` | `MainMenuUI` | new/load, class selection, six slots, overwrite confirmation | scene/prefabs |
| Codex/mod list | `Assets/Prefabs/UI/ModListPanel.prefab` | `ModListView` | `CodexModListUI` | scroll content, close/filter controls | prefab; variable authored rows |
| Tooltips | `ItemTooltip.prefab`; `Assets/Resources/UI/Tooltips/*.prefab` | serialized tooltip components | `ItemTooltipUI`, `CurrencyTooltipUI`, `RelicTooltipUI` | labels, frames, divider, lock sprite | tooltip prefabs; temporary pointer fitting remains runtime |

Stable `UIAuthoringElement` IDs identify important controls independently of name, child index, or position. Open **Black-Cube → UI Authoring** to locate, select, validate, and edit these assets.
