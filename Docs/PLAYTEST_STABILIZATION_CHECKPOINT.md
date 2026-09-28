# Playtest stabilization — implementation checkpoint

Baseline: `26d4b483`, branch `codex/repository-cleanup-baseline`. The checkout was
clean at task start. No pushes, main changes, BalanceLab runs or broad balance
surveys. This document is a partial checkpoint, NOT the full brief's final handoff.

## Critical fixes

- **Off-class side popup:** resolving the native subclass ID against another
  class returned no authored subclass node, then the popup dereferenced it.
  Exclude inaccessible subclass choices before resolution. Generic A/B/C choices
  remain available; absent authored choices cannot abort the popup.
- **Respec freedom:** route selections now represent persistent unlocks. Refund
  All preserves them; save/allocation validation no longer requires the original
  completed native tree or earlier route spines to remain allocated. Initial
  selection gates and within-route prerequisites still apply. New Game resets
  selections. One selected weapon specialization remains the existing contract.
- **Weapon interaction:** the Central Hub Image had `raycastTarget=false` despite
  its Button. Enable input on the actual authored hub and its authoring generator.
- **Inventory scroll:** its ScrollRect content reference was missing; its old
  viewport covered equipment/other UI. Bind the actual grid and place the scroll
  region in the authored inventory artwork bounds. Existing RectMask2D clips it.
  Stop runtime grid cell/spacing overrides so Editor adjustments persist.
- **Encounter freeze:** user clarified this is the challenge selector, not a
  Workbench optimizer. Opening it set timeScale=0; closing it after launch skipped
  restoring the clock when a challenge was active. Selector/crafting now restore
  their prior clock on close, including active challenges and already-paused play.

Three original-state regressions failed as expected (route refund, hub input,
inventory content). The subclass popup and active-selector pause are source-
confirmed and covered by the repaired-state tests, not native mouse playtests.

## Additional changes

- Shared PlayerStatSetup grants zero Life/Mana regeneration instead of hidden
  7 Mana/sec. Explicit gear/passive/effect modifiers are retained.
- Revenge sums incoming direct-hit life-loss fractions until the next attack;
  live and headless paths both accumulate, including the Mage self-hit. Existing
  maximum scaling protection and consume/reset behavior remain.
- Player attack animation begins at basic-attack or skill initiation, not each
  projectile impact/repeated logical hit.
- Enemy animation assets expose `playHitReaction`; Goblin disables it, others
  retain their previous default.
- Mixed hits deduct Life once but show separate mitigated typed proportions.
  Source stats are passed to the receiver so conversion/penetration/conditional
  damage is represented correctly. Popups distribute actual Life loss by those
  proportions, including lethal overkill and Mana redirection.
- Deterministic center/upper-left/upper-right/higher-left/higher-right popup fan.
  Critical marker remains left; Precision is green on the right.

## Focused verification

- `Logs/PlaytestBefore.xml`: 3/3 original-state defects fail.
- `Logs/PlaytestP0After.xml`: 11/11 pass.
- `Logs/PlaytestCheckpointTests2.xml`: 15/15 pass, including subclass/keystone
  swap + restored allocation + actual popup button clicks for both off-class
  sides, unlocked-route refunds, selected-weapon allocation, active-challenge
  clock restoration, mixed-hit Life deduction/three numbers, Crit/Precision
  marker separation, zero baseline regeneration and Revenge accumulation.
- Native mouse/scroll/playthrough smoke remains for the user. This is not a claim
  that every requirement in the full playtest brief is implemented.
- Windows checkpoint build succeeded: zero errors, 827 warnings (existing
  obsolete API / authored connection geometry warnings). Logs:
  `Logs/PlaytestCheckpointBuild.log`, `Logs/GenericClassRework/BuildResult.txt`.
  Executable: `Builds/GenericClassPassiveWindows/BlackCube.exe`.
- Startup smoke reached MainMenuUI with Load enabled, no managed exceptions;
  existing achievements-button warning remains. Log:
  `Logs/PlaytestCheckpointStartup.log`. Closed only the spawned test player.

## Remaining brief work

1. Sustain: authored gear/passive Life Regen targets; passive-only Life/Mana on
   Hit reductions; Rapid Flurry Mana cost and consolidation integration.
2. New Armour item-base/local/global pipeline, tier targets and tooling diagnostics.
3. Active-effects readability, short death recap, compact grouped Stats panel.
4. Pause-vs-Options separation; unfocused and skill-tooltip persisted preferences;
   live weapon-skill hover tooltips.
5. Inventory shrinking, Alt comparison, cached canonical estimated-DPS arrows,
   item locks/hotkey/persistence/destructive-action protection.
6. Item-type advanced filters (rarity precedence, weapon types/elements, tier and
   implicit requirements, ALL/ANY/minimum matches/over-cap pool semantics), legal
   mod-list cleanup and weapon implicit eligibility audit.
7. Fragment/economy audit + small progression sample. Existing source already
   routes manual and filtered pickup dismantles through Inventory.TryDismantle;
   do not claim a missing-fragment bug or buff direct drops without reproducing it.
8. Remaining focused smoke, final Windows build/startup, final manual authoring
   instructions for Inventory, Stats, Mod Highlight and Options, full git handoff.

## Editing the repaired inventory grid now

Open `Assets/Prefabs/UI/InventoryPanel.prefab` in Prefab Mode. Locate
`Compact inventory grid` beneath `Viewport`. Its parent ScrollRect owns scrolling;
the Viewport RectMask2D owns clipping. Change GridLayoutGroup cell size, spacing,
padding and constraint count here; InventoryUI no longer rewrites them per frame.
Edit the ScrollRect RectTransform to move/resize the recovered-item region. Keep
content under Viewport with top pivot and vertical ContentSizeFitter Preferred
Size. The production PaperBattle prefab currently contains its inventory layout
inline; apply matching layout changes there until the later ownership cleanup.
Do not rerun layout-building commands merely to save manual adjustments.
