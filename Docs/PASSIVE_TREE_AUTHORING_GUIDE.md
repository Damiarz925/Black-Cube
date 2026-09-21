# Passive Tree Authoring Guide

Open **Black-Cube → UI Authoring → Passive Tree**. The checked-in ScriptableObjects are authoritative; do not run the one-time migration over them.

## Data locations

- Class branches: `Assets/GameData/PassiveTree/Branches/Class/`
- Weapon branches: `Assets/GameData/PassiveTree/Branches/Weapon/`
- Database: `Assets/Resources/GameData/PassiveTree/SO_PassiveTreeDatabase.asset`
- Default icons: `Assets/GameData/UI/Libraries/SO_PassiveNodeIconLibrary.asset`
- Supported effects/search names: `Assets/GameData/UI/Libraries/SO_PassiveEffectCatalog.asset`
- Visual layout: `Assets/Prefabs/UI/PassiveTreePanel.prefab`

There is one SO per class (`SO_Warrior_Branch` through `SO_Priest_Branch`) and one per weapon (`SO_Sword_Branch`, `SO_TwoHandedAxe_Branch`, `SO_Bow_Branch`, `SO_Staff_Branch`, `SO_Dagger_Branch`, `SO_Sceptre_Branch`). Class assets contain 10 tiers; weapon assets contain 5.

## Editing a node

Select, for example, `SO_Warrior_Branch.asset`. Expand **Node 1 / Tier 1**, then the Spine, Right Choices, or Left Choices foldout. Each node has a stable ID, logical slot, one or more effect lines, a numeric value per effect, and an Auto/Custom icon choice. Use the effect selector rather than entering raw IDs. The Right and Left groups each provide A/B/C plus distinct Subclass A and Subclass B definitions. Only the selected subclass variant resolves into the shared fourth visual slot at runtime. Weapon tiers have only A/B/C.

Choice exclusivity and allocation routes are based on logical slot IDs and branch topology, not coordinates. Multiple effect lines are applied in order. Editing the SO changes runtime values directly; there is no second production copy in C#.

SO inspector edits use Unity Undo/Redo and `SaveAssetIfDirty`; asset edits made in Play Mode persist. The inspector displays that warning. Collapse tiers to keep the inspector manageable. **Select View** selects the corresponding binding where available; a node view can ping its branch data.

## Icons and artwork

With Icon Mode **Auto**, the node’s primary effect resolves through `SO_PassiveNodeIconLibrary.asset`; an unmapped effect uses that asset’s generic fallback. Choose **Custom** and assign a sprite to override one node only. Change the default Crit or any other effect icon in the icon library. Branch background/art references live on the matching branch SO/view binding.

## Moving the tree

Open `Assets/Prefabs/UI/PassiveTreePanel.prefab` in Prefab Mode. Move an individual `PassiveNodeBinding` RectTransform to change one node. Move a class/weapon branch root to move its entire branch. Connections are `[ExecuteAlways]` presentation derived from assigned node/junction RectTransforms, so they follow edits in Edit Mode. Stable IDs and serialized data references preserve gameplay wiring.

Manual layout is authoritative. No load/play/data-change path reapplies defaults. Only invoke an explicitly labelled default-layout authoring command if you intentionally want it. The supplied `Passive Branch (3).png` and `(4).png` references informed the checked-in V3C branch geometry; they are references, not runtime textures.

## Validation and tooltip preview

Use **Black-Cube → UI Authoring → Validate All UI**. It checks tier counts, routes, unique IDs, effects, subclass definitions, icons, view/data matches, and duplicate slots. The node inspector preview uses the same definition/formatter path as the runtime tooltip, so review its text before play-testing.
