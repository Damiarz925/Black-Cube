# UI Authoring Guide

Open **Black-Cube → UI Authoring**. Runtime code updates content and state; serialized prefabs/scenes determine permanent position, size, anchors, scale, rotation, hierarchy, and artwork.

## Move or resize UI

Open the listed prefab, select its RectTransform, drag/resize it in Prefab Mode, and save. Alternatively edit a scene instance and deliberately apply its overrides. The hub shows source/override state and provides Open, Ping, Apply, and Revert actions with Unity Undo. The selected-transform panel edits position, size, scale, rotation, anchors, and pivot directly on the serialized object. Runtime does not restore old layout values.

## Where to edit common screens

- HUD, portraits, bars, and top buttons: `Assets/Prefabs/UI/GameplayHUD.prefab`
- Skill buttons: `Assets/Prefabs/UI/SkillSelectionPanel.prefab`
- Inventory root, equipment, currencies, filters: `Assets/Prefabs/UI/InventoryPanel.prefab`
- Item/equipment icon size: `Assets/Prefabs/Other Prefabs/Item Slot.prefab` and `Assets/Prefabs/PaperBattle/EquipmentSlot.prefab`
- Passive tree: `Assets/Prefabs/UI/PassiveTreePanel.prefab`
- Pause: `Assets/Prefabs/UI/PauseMenu.prefab`
- Main menu/slots: `Assets/Prefabs/UI/MainMenu.prefab`, `CharacterSlots.prefab`, and `Assets/Scenes/Main Menu.unity`
- Challenges/crafting/subclass/rebirth/stats/enemy inspection: matching files under `Assets/Prefabs/UI/`
- Item tooltip: `Assets/Prefabs/UI/ItemTooltip.prefab`
- Currency/relic tooltips: `Assets/Resources/UI/Tooltips/`

Change overall screen scale on the authored root. Change Canvas scaling on the scene Canvas/CanvasScaler. Controllers do not overwrite either.

## Artwork and states

Global sprite references are in `Assets/GameData/UI/Libraries/SO_UIVisualLibrary.asset`. Button Normal, Hovered, Pressed, Disabled, and Selected appearance is in `SO_DefaultButtonVisualStyle.asset` or the style assigned to the control. Runtime selects a state; the style chooses sprites, colors, text/icon colors, and scale. Currency source art is under `Assets/Resources/UI/Currency/`; rarity and HUD sprites are exposed by the visual library. A missing production sprite logs a warning and uses the single configured fallback—no hidden runtime art is generated.

The inventory’s fixed layout is authored. Only genuinely variable item/stat/mod/relic/status rows are populated at runtime, using authored slot/row appearance. Tooltip content and temporary pointer fitting are runtime behavior; tooltip structure and art remain prefab-authored.

## Hub tabs

- **Overview:** scenes, all major assets, quick-open, validation, report.
- **HUD / Inventory / Menus / Crafting / Challenges / Tooltips:** screen assets and selected-transform/prefab workflows.
- **Passive Tree:** all 12 branch SOs, database, icons/effects, authoring-label toggle.
- **Visual Libraries:** sprite/effect/style authorities.
- **Validation:** validation, generated report, and runtime audit.

Run **Generate UI Authoring Report** after structural changes. It writes `ReviewCaptures/UIAuthoringReport.md`.
