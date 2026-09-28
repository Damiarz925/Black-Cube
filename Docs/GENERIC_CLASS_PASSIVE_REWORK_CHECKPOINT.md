# Generic class passive rework — implementation checkpoint

> Historical foundation checkpoint. The subsequent continuation is documented in `CLASS_KEYSTONE_SYSTEM_GUIDE.md`, `CLASS_KEYSTONE_CONTENT_CONTRACT.md` and `CLASS_KEYSTONE_VALIDATION_AND_SURVEY.md`; use those for current keystones, gating, seven-tier weapons, regeneration units and final validation evidence. Statements below about the subsequent task being unfinished describe this earlier checkpoint.

The five-class asset conversion and automated validation are complete. Native interactive editor checks are not complete. The task began on `codex/repository-cleanup-baseline` at `cc83a9ed` with a clean worktree. No commits or pushes have been made by this pass. The subsequent class-keystone/hub/weapon-tree task is a separate, unfinished implementation.

## Implemented in source

- Explicit five-class reauthoring entry point: `GenericClassPassiveReauthoring.Apply`. It changes only the five requested generic choice and spine data blocks, preserving IDs, subclass values and weapon values. It converts the existing prefab incrementally to ten spines and twenty physical choice slots per class, preserving Warrior layout.
- Ordinary values derive from the actual T1 midpoint at 50%; hybrid components use 30%; Heavy Physical uses 75%, with an Attack Speed penalty of 25% of that stat's T1 midpoint. Rounding is to the nearest half-point.
- Conservative custom values: Rage Generation 15%, Rage Effect 10%, Rage retention 5%, Revenge Effect 10%, Poison life leech 2%, Precision Chance 8%, Life Recovery 10%, maximum elemental resistance 1%, Aura Effect 10%, damaging ailment chance 10%, CDR 10%.
- New stable stat identities append to the enum; no old identities are repurposed.
- Revenge uses the latest hit's actual Life loss, not cumulative damage. The next ordinary attack event (including its replacement skill and repeat hits) consumes it once. DOT does not charge it. Multiplier: `1 + 2 * lostLifeFraction * (1 + increasedRevengeEffect)`; possessing Revenge Effect unlocks the mechanic. Independent cooldown casts do not consume it.
- Poison leech uses actual mitigated Poison Life loss, capped to remaining target Life; Life Recovery scales Life healing but not Mana.
- Matching-type auras use the most recent typed hit, not accumulated encounter damage, and expire after two global combat turns. First four require access plus typed damage; Void requires all three damaging ailments successfully applied by the same hit. Amulet Void access follows that same rule.
- At full intensity: Physical +20% more Physical/+10% Attack Speed; Fire +20% more Fire/+1 Ignite tick; Cold +20% more Cold/+20% Chill Effect; Lightning +20% more Lightning/+20% Shock Effect; Void +20% more Void/+20% Bleed, Ignite and Poison chance. Aura Effect scales matching bonuses without a 200% cap. Additional Fire ticks are whole ticks: floor(intensity * (1 + floor(Aura Effect))).
- Five level-70+, Rare-or-higher, non-empowerable Amulet prefix families grant aura access. Each family has base selection weight 1 versus Plus All Skills weight 5; all five combined have weight 5. Plus All Skills remains available.
- Player Multistrike is restricted to Sword, Two-Handed Axe, Dagger and Sceptre. Enemy extra-hit rules remain unchanged. Workbench metrics and focused combat laboratory rules use the same restriction and new mechanics.
- Global Crit Chance/Multiplier icon mappings, circular clipping, three generic popup choices plus optional unlocked subclass choice, signed/multi-effect popup text.
- Staged layout editor: dragging does not save the prefab; explicit SAVE writes it. Each connection exposes From/To normalized anchors and local offsets; default center geometry accounts for noncentral parent/node pivots. Inspector LAYOUT VIEW opens the selected branch.

## Validation so far

A standalone Roslyn compilation of runtime, editor and test source completed with zero errors using Unity's installed references. This does not substitute for Unity import, EditMode tests, play-mode verification or native editor interaction.

Fresh Unity validation on September 28, 2026:

- Asset conversion succeeded: `Logs/GenericClassApply.log`.
- Complete EditMode suite: **600/600 passed**, `Logs/GenericClassTests.xml`.
- After projectile foundation changes: **606/606 passed**, `Logs/KeystoneFoundationTests.xml`.
- After Warrior's new 5 Strength/5 Dexterity spine and staged Save test: **607/607 passed**, `Logs/PassiveFoundationFinalTests.xml`.
- Additional isolated staged-Save persistence test: **1/1 passed**, `Logs/PassiveLayoutSaveTests.xml`. Node coordinates and custom line anchors/offsets persist across save and reopen; the test never writes the production prefab.
- Twenty real prefab UI renders were captured under `Logs/GenericClassRework/Captures`: empty tree, three-choice popup, selected choice and existing signature-weapon unlock for each of the five classes. All five popup images and representative selected/unlocked images were visually inspected. This is not native mouse/keyboard verification.
- Actual ten-row class tables and T1 audit: `Docs/GENERIC_CLASS_PASSIVE_TABLES.md`.
- Final Windows x64 build, including the Warrior hybrid spine, succeeded with zero errors, 954 warnings: `Logs/GenericClassRework/BuildResult.txt`, `Logs/PassiveFoundationFinalWindowsBuild.log`. Hidden headless startup reached `MainMenuUI.Awake` without exceptions (`Logs/PassiveFoundationFinalStartupSmoke.log`); the existing unassigned achievements-button warning remains. Only the launched smoke-test process was stopped. The executable is `Builds/GenericClassPassiveWindows/BlackCube.exe`. Build-time temporary Sentis symbol changes were restored automatically; ProjectSettings has no diff.

The two original stale test failures were updated for one-tier aura prefixes and the new generic stat distribution, then the full suite passed. No production numbers were changed to satisfy those stale assertions.

## Required continuation

1. Manually verify native editor dragging, multiple edits before Save, Undo/Redo, branch switching, connection controls and runtime scrolling/zoom. Automated persistence/render checks do not substitute for this.
2. Continue the new task from `Docs/CLASS_KEYSTONE_SYSTEM_CHECKPOINT.md`; the previous signature-weapon-unlock render documents the old gate, not the requested new gate.
3. Do not push or modify main; do not run old BalanceLab.

## HOW TO EDIT A CLASS'S GENERIC PASSIVE OPTIONS

1. In Unity, open the class branch asset under `Assets/GameData/PassiveTree/Branches/Class`.
2. Use its Passive Tree authoring inspector; confirm the class identity.
3. Expand NODE / TIER 1–10.
4. Expand LEFT CHOICES, then A, B or C. Do not change stable IDs or subclass blocks.
5. Expand RIGHT CHOICES, then A, B or C.
6. Edit effect entries and values, and choose Auto/Custom icons. Hybrid nodes retain both effects. Percent values use percentage points (10 means 10%), except the existing Physical Damage Reduction identity, which uses fractions.
7. Click LAYOUT VIEW for that branch, then Open Editing Copy. The runtime presentation has one physical slot per side; its popup offers the authored choices. Verify the real runtime popup after saving data changes.
8. Move nodes or edit connection anchors/offsets in the layout copy. `(0.5, 0.5)` is the rect center; `(0, 0.5)` and `(1, 0.5)` are left/right edges. Offsets are node-local UI units. Use Unity Undo/Redo, then explicit SAVE when ready. Reload / Discard abandons staged geometry. Data inspector changes use Unity Undo and delayed asset saving; layout geometry stays staged until SAVE. Avoid concurrently editing the production prefab outside the layout copy.
