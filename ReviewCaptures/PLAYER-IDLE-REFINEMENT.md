# Idle refinement and attack review

**Superseded:** current combat-ready poses, visible sway, and latest attack direction are documented in `COMBAT-IDLE-AND-SLASH-REVISION.md`. This earlier restrained-breathing revision is historical.

The user reported silhouette artifacts/popping and excessive speed in the first installed idle.

## Installed idle revision

- Rebuilt from one approved cleaned pose, preserving the same character appearance instead of cycling eight inconsistent source drawings.
- Filled accidental enclosed alpha holes in the master. Kept outside gaps and removed invisible background RGB. Reviewed against light, green, and dark backgrounds.
- Added deterministic subpixel breathing and sway: at most 2 pixels of upper-body lift, 0.65 pixels of sway, and a tiny chest expansion. Boots/lower-leg rows 440–543 are exactly unchanged across the cycle.
- 32 coherent RGBA frames at 8 fps: **four seconds per loop**, replacing the earlier one-second loop. Intermediate deformation frames avoid visibly holding eight poses for half a second each.
- Preserved matching pivots, scale, right-facing playback, pause/death behavior, and ghoul animation. Updated the existing prefab's idle references and scene builder's frame count. No combat/weapon logic changes.
- Runtime assets: `Assets/Art/PaperBattle/PlayerIdle/PlayerIdle1.png` through `PlayerIdle32.png`.
- Reproduce: `prepare_player_idle.py` then `refine_player_idle.py` then `wire_player_idle.py` in `Tools/Art`. The retained `ReviewCaptures/PlayerIdle/idle-master.png` is the canonical cleaned source for refinement.

## Checks

- Reviewed the full 32-frame sequence as a contact sheet, plus contrasting-background edge review. No independent costume/hair/silhouette redraws occur between frames.
- Checked identical boot pixels, transparent canvas margins, and continuity at the wrap. Last-to-first composited pixel difference is 0.201; adjacent differences range approximately 0.201–0.230, so the seam adds no larger jump.
- GIF verified: 32 frames, total duration exactly 4,000 ms, shared palette to avoid color flicker.
- File-only C# harness passed with actual actor source and minimal Unity substitutes: 32-frame order/four-second wrap, combat idle retention, facing, pause, death/revival, ghoul poses.
- Unity asset GUID/reference checks and scoped diff whitespace checks passed. No Unity/editor/batchmode, desktop, browser, or in-game testing performed. User handles import and gameplay review.

Preview: `ReviewCaptures/PlayerIdle/idle-refined-preview.gif`.
Full sequence: `ReviewCaptures/PlayerIdle/idle-refined-all-frames.png`.
Edge review: `ReviewCaptures/PlayerIdle/idle-edge-review.png`.
Metrics: `ReviewCaptures/PlayerIdle/idle-refinement.json`.

## Attack concept — review only

`ReviewCaptures/PlayerAttack/sword-attack-eight-frame-review.png` shows eight numbered right-facing sword-attack poses using the supplied original character design. Generated with the built-in image-generation tool; copied into the project outside Assets. This is one flattened concept sheet, not cutout animation sprites or actual separate weapon layers. Blue sword guides and orange grip markers indicate intended future equipment attachment. No attack frames or weapon/inventory architecture were implemented.

The user must approve the attack concept before implementation. Pose timing and exact grip/weapon transforms will need production preparation after approval. Generation prompt is saved beside the review image.
