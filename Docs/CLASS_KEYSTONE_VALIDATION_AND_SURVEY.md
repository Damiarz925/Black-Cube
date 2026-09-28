# Class keystone system — validation and controlled survey

PROVISIONAL WEAPON TREE VALUES — NOT FINAL BALANCE

## Validation

The September 28 continuation passed the complete EditMode suite: **640 tests, 640 passed, zero failures** (`Logs/ClassKeystoneFullTests9.xml`). This includes native/off-class gating, shared point budgets, explicit single-weapon selection, save legality, projectile travel, exclusive affixes and live/Lab mechanic tests. New regressions cover Venom Shot's would-be-hit Poison basis, empowered Bleed's normal basis, automatic Staff policy and Blood Engine recovery from separate Shatter damage.

The structure and UI-binding validators passed, and scripted prefab verification produced sixty images: six classes, ten states each (`Logs/GenericClassRework/Captures`). Those states cover empty/partially allocated trees, keystone eligibility and its three-choice popup, selected weapon hub preview, full-size weapon view, allocation and restored overview. These are automated renders and callback checks, **not native mouse/keyboard manual smoke**.

The complete bounded survey finished all **42 builds**. Final production-policy replay then validated those saved builds and **18 weapon ablations**, with **30 fights each: 1,800 fights, zero simulation errors**. The original optimization artifacts are preserved; final fight results and actor inputs are versioned separately. See [all final tables and counters](CLASS_KEYSTONE_SURVEY_RESULTS.md).

Fresh Windows x64 build: **Succeeded, zero errors, 827 warnings** (`Logs/ClassKeystoneFinalChecksBuild.log`, `Logs/GenericClassRework/BuildResult.txt`). The final checks log records both the structure/UI-binding PASS and final replay/capture/build PASS. Warnings include pre-existing deprecated API/package notices and connection `OnValidate`/RectTransform warnings; zero build errors is not a claim of a warning-free project.

Startup smoke: `Builds/GenericClassPassiveWindows/BlackCube.exe` reached `MainMenuUI.Awake` without exceptions (`Logs/ClassKeystoneFinalStartupSmoke.log`). The existing `achievementsButton not assigned` warning remains. Only the launched hidden smoke-test player was stopped; no New Game or saved-game load was initiated. This is a headless startup check, **not visual/native manual playtesting**.

## Authoritative references

- [System and editor-authoring guide](CLASS_KEYSTONE_SYSTEM_GUIDE.md)
- [All eighteen keystones, six provisional weapon trees and exclusive-affix tiers](CLASS_KEYSTONE_CONTENT_CONTRACT.md)
- [Level-30, level-31 and level-70 results, weapon ablations and per-build telemetry](CLASS_KEYSTONE_SURVEY_RESULTS.md)

## Survey contract

Six level-30 pre-keystone baselines, eighteen level-31 keystone variants and eighteen level-70 variants; no subclasses. Each level uses one straight-progression checkpoint without farming and one canonical ground-loot inventory. Every class/variant owns independent clones. The pre-keystone native stage is identical across the three variants of a class at a checkpoint; post-keystone allocations and fights are independent.

Serious crafting is bounded to twelve actions, passive search uses beam width twelve, gear search uses twenty candidates per slot and beam width forty-eight, and current resistance/combined-PDR targets are soft constraints. Final validation uses thirty Combat Lab fights per build. Level-70 weapon ablations remove weapon allocations while retaining final gear, class allocations and the final skill policy; refunded points are **not** reallocated.

Results are versioned by a fingerprint of production data, tuning and runtime/editor source. Prior interrupted rows are not reused across changed mechanics. The Combat Lab resolves sequencing, actual hit-based ailments, target conditions and self-hits; analytical DPS remains an optimization approximation and must not be substituted for the final fight results. No balance values are automatically tuned from this survey.

## Concise survey observations

- No allocation/progression errors were observed: every build spent exactly its level-owned budget and passed production legality checks. Native investment was 30 points at level 30 and 31 at levels 31/70. Level-70 weapon spending ranged from **0–14**, with **25–39** off-class points; selecting a tree did not force spending all 21 possible points. No second currency was used.
- Conditional inactivity is not the same as missing implementation. Stormglass/Deep Fracture did not meet their conditions in these selected builds. Crown of Fury had no capped-Rage uptime in fresh single-enemy fights. Molten Edge's selected Fire weapon provided no Physical component to convert. Prismatic Discipline had no active auras, though its random damage-type restriction could still reduce output. Endless Venom's level-70 build selected a non-Poison policy. The report records those cases; focused regressions exercise the mechanics separately.
- Large differences warrant later balance investigation, not automatic changes: level-31 Ranger split produced about **279 DPS** versus about **69** for Endless Venom; level-70 Mage Fire Commitment produced about **1,891** versus about **5,000** for the other two selected builds. Level-70 Thief/Warrior fights were often under one second. Gear re-evaluation, different allocations/policies, fight length and overkill prevent these from being pure keystone multipliers.
- Provisional weapon magnitude varies substantially: Warrior Blood Reservoir's 12 weapon points changed about **3,273 → 9,513 DPS**, while Mage Fire Commitment's nine points changed about **1,860 → 1,891**. The two zero-weapon-point Mage builds correctly had identical ablation DPS. These are diagnostics of provisional packages and bounded search, not final weapon rankings.
- Physical defense remains a warning: level-70 combined PDR was roughly **30–63%**, below the checkpoint's approximately **71.25%** soft target, despite every sampled rare fight being won. Those wins do not establish boss survival or adequate endgame defenses. No armour/resistance values were retuned from this result.
- Storm's Price recorded real enemy/self bolts at level 31; the incoming self-hit total was about **2,915** across thirty fights, with no losses. Its short level-70 fights finished before cooldown casts, so no self-hit activity there is expected. Focused tests guard against recursion and offensive recovery from incoming self-hits; encounter-chain Mana/self-hit survival is not established by these fresh fights.
- Projectile split/miss/Precision and captured-target travel have passing focused tests and nonzero relevant survey counters. More launches than impacts can reflect projectiles still traveling when the target dies; that is not proof of duplicate casts or damage transferred to the next enemy. No projectile regression was found in the automated checks.

## Manual checks still requiring a person

Native desktop control is unavailable in the current tool environment. In Unity, follow the guide's smoke sequence: allocate thirty native nodes, choose a keystone, select an off-class route and a non-signature weapon tree, enter/leave the hub weapon view with Back/Esc/right-click, equip/swap matching weapons, refund/respec, save/load and restart an encounter. In Layout, drag several nodes and adjust line anchors/offsets before explicitly saving; confirm no per-drag production-prefab reimport occurs.

## Repository scope

All changes remain on `codex/repository-cleanup-baseline`, starting at `cc83a9ed8b905660f620b4eeb7cc1aeae8b2ef3d`; main is untouched, **no commits were created and nothing was pushed**. No second passive currency or weapon experience was added. Only one weapon tree can be selected. Generated builds, images, logs and raw survey JSON remain local ignored artifacts. ProjectSettings' unrelated temporary build-symbol changes were restored by the build pipeline.

The initial worktree was clean. The combined generic-class foundation and continuation currently contain 74 modified tracked files and 49 added/untracked files (including Unity metadata and documentation). No pre-existing user-owned dirty edits were committed or discarded. The implementation remains uncommitted for review.

Changed modules: six class/twelve total branch assets, ModDatabase/icon-library/passive-panel assets; live progression, persistence, tree presentation/navigation, combat/ailment/resource consumers, affix generation/validation/tooltips; Workbench build/optimizer/Combat Lab adapters; staged layout and UI/visual validators; and compatibility/regression tests. Added modules include class keystone catalog/tuning/mechanics, shared progression rules, provisional weapon profiles, exclusive-affix rules, circular masking/navigation, authoring/export/survey/revalidation tools and focused tests. The following manifest lists exact paths.

## Modified tracked files

```
Assets/Editor/BalanceWorkbenchAdvancedAnalysis.cs
Assets/Editor/BalanceWorkbenchCombatTools.cs
Assets/Editor/BalanceWorkbenchCore.cs
Assets/Editor/BalanceWorkbenchOptimizers.cs
Assets/Editor/BalanceWorkbenchPlayerBuild.cs
Assets/Editor/PassiveBranchAuthoringEditor.cs
Assets/Editor/PassiveTreeLayoutWindow.cs
Assets/Editor/PassiveTreeVisualValidation.cs
Assets/Editor/SubclassSurveyRunner.cs
Assets/Editor/UIAuthoringValidation.cs
Assets/Editor/WarriorPassiveReauthoring.cs
Assets/GameData/PassiveTree/Branches/Class/SO_Barbarian_Branch.asset
Assets/GameData/PassiveTree/Branches/Class/SO_Mage_Branch.asset
Assets/GameData/PassiveTree/Branches/Class/SO_Priest_Branch.asset
Assets/GameData/PassiveTree/Branches/Class/SO_Ranger_Branch.asset
Assets/GameData/PassiveTree/Branches/Class/SO_Thief_Branch.asset
Assets/GameData/PassiveTree/Branches/Class/SO_Warrior_Branch.asset
Assets/GameData/PassiveTree/Branches/Weapon/SO_Bow_Branch.asset
Assets/GameData/PassiveTree/Branches/Weapon/SO_Dagger_Branch.asset
Assets/GameData/PassiveTree/Branches/Weapon/SO_Sceptre_Branch.asset
Assets/GameData/PassiveTree/Branches/Weapon/SO_Staff_Branch.asset
Assets/GameData/PassiveTree/Branches/Weapon/SO_Sword_Branch.asset
Assets/GameData/PassiveTree/Branches/Weapon/SO_TwoHandedAxe_Branch.asset
Assets/GameData/UI/Libraries/SO_PassiveNodeIconLibrary.asset
Assets/Prefabs/Scriptable Objects/ModDatabase.asset
Assets/Prefabs/UI/PassiveTreePanel.prefab
Assets/Scripts/AffixPolicy.cs
Assets/Scripts/AilmentCalculator.cs
Assets/Scripts/AilmentEligibilityResolver.cs
Assets/Scripts/Authoring/PassiveBranchBinding.cs
Assets/Scripts/Authoring/PassiveClassBranchSO.cs
Assets/Scripts/Authoring/PassiveConnectionBinding.cs
Assets/Scripts/Authoring/PassiveTreeAuthoringData.cs
Assets/Scripts/BattleManager.cs
Assets/Scripts/CombatCalculator.cs
Assets/Scripts/CombatSimulationCore.cs
Assets/Scripts/DamageContext.cs
Assets/Scripts/DamageReceiver.cs
Assets/Scripts/EnemyBuildOptimizer.cs
Assets/Scripts/GamePersistence.cs
Assets/Scripts/Gear.cs
Assets/Scripts/GearStatLists.cs
Assets/Scripts/HealthComponent.cs
Assets/Scripts/ItemTooltipUI.cs
Assets/Scripts/ItemizationValidator.cs
Assets/Scripts/ModManager.cs
Assets/Scripts/PassiveKeystoneState.cs
Assets/Scripts/PassiveTreeDefinition.cs
Assets/Scripts/PassiveTreePresentation.cs
Assets/Scripts/PlayerController.cs
Assets/Scripts/PlayerProgression.cs
Assets/Scripts/PlayerSkillController.cs
Assets/Scripts/PlayerSkillDefinition.cs
Assets/Scripts/PoedbAffixCatalog.cs
Assets/Scripts/SkillTreeUI.cs
Assets/Scripts/StatDisplayFormatting.cs
Assets/Scripts/StatTypes.cs
Assets/Scripts/StatsComponent.cs
Assets/Scripts/StatusController.cs
Assets/Scripts/StatusInstance.cs
Assets/Scripts/SubclassCombatState.cs
Assets/Scripts/SubclassMenuUI.cs
Assets/Scripts/WeaponMechanics.cs
Assets/Tests/Editor/BalanceWorkbenchTooling2Tests.cs
Assets/Tests/Editor/ModifierTierDataTests.cs
Assets/Tests/Editor/PassiveTreeTests.cs
Assets/Tests/Editor/PauseMenuTests.cs
Assets/Tests/Editor/PoedbAffixFoundationTests.cs
Assets/Tests/Editor/Step16ClassWeaponFoundationTests.cs
Assets/Tests/Editor/Step17FoundationTests.cs
Assets/Tests/Editor/Step18ProductionTests.cs
Assets/Tests/Editor/Step20EndgameItemizationTests.cs
Assets/Tests/Editor/UIAuthoringTests.cs
Assets/Tests/Editor/WarriorPassiveReauthoringTests.cs
```

## Added files (including Unity metadata)

```
Assets/Editor/ClassKeystoneAuthoring.cs
Assets/Editor/ClassKeystoneAuthoring.cs.meta
Assets/Editor/ClassKeystoneContractExport.cs
Assets/Editor/ClassKeystoneContractExport.cs.meta
Assets/Editor/ClassKeystoneSurveyReport.cs
Assets/Editor/ClassKeystoneSurveyReport.cs.meta
Assets/Editor/ClassKeystoneSurveyRunner.cs
Assets/Editor/ClassKeystoneSurveyRunner.cs.meta
Assets/Editor/GenericClassPassiveReauthoring.cs
Assets/Editor/GenericClassPassiveReauthoring.cs.meta
Assets/Editor/GenericClassPassiveVerification.cs
Assets/Editor/GenericClassPassiveVerification.cs.meta
Assets/Editor/PassiveCircularIconAuthoring.cs
Assets/Editor/PassiveCircularIconAuthoring.cs.meta
Assets/Resources/GameData/PassiveTree/SO_ClassKeystoneTuning.asset
Assets/Resources/GameData/PassiveTree/SO_ClassKeystoneTuning.asset.meta
Assets/Scripts/ClassKeystoneCatalog.cs
Assets/Scripts/ClassKeystoneCatalog.cs.meta
Assets/Scripts/ClassKeystoneMechanics.cs
Assets/Scripts/ClassKeystoneMechanics.cs.meta
Assets/Scripts/ClassKeystoneTuningSO.cs
Assets/Scripts/ClassKeystoneTuningSO.cs.meta
Assets/Scripts/ClassPassiveProgressionRules.cs
Assets/Scripts/ClassPassiveProgressionRules.cs.meta
Assets/Scripts/GenericPassiveMechanics.cs
Assets/Scripts/GenericPassiveMechanics.cs.meta
Assets/Scripts/PassiveCircleMaskGraphic.cs
Assets/Scripts/PassiveCircleMaskGraphic.cs.meta
Assets/Scripts/PassiveTreeNavigation.cs
Assets/Scripts/PassiveTreeNavigation.cs.meta
Assets/Scripts/ProvisionalWeaponTreeContent.cs
Assets/Scripts/ProvisionalWeaponTreeContent.cs.meta
Assets/Scripts/WeaponExclusiveAffixRules.cs
Assets/Scripts/WeaponExclusiveAffixRules.cs.meta
Assets/Tests/Editor/ClassKeystoneCombatTests.cs
Assets/Tests/Editor/ClassKeystoneCombatTests.cs.meta
Assets/Tests/Editor/ClassKeystoneFoundationTests.cs
Assets/Tests/Editor/ClassKeystoneFoundationTests.cs.meta
Assets/Tests/Editor/ClassKeystoneProgressionTests.cs
Assets/Tests/Editor/ClassKeystoneProgressionTests.cs.meta
Assets/Tests/Editor/GenericClassPassiveReworkTests.cs
Assets/Tests/Editor/GenericClassPassiveReworkTests.cs.meta
Docs/CLASS_KEYSTONE_CONTENT_CONTRACT.md
Docs/CLASS_KEYSTONE_SURVEY_RESULTS.md
Docs/CLASS_KEYSTONE_SYSTEM_CHECKPOINT.md
Docs/CLASS_KEYSTONE_SYSTEM_GUIDE.md
Docs/CLASS_KEYSTONE_VALIDATION_AND_SURVEY.md
Docs/GENERIC_CLASS_PASSIVE_REWORK_CHECKPOINT.md
Docs/GENERIC_CLASS_PASSIVE_TABLES.md
```
