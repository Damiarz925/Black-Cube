# Player character and idle replacement

**Superseded by the refinement:** see `PLAYER-IDLE-REFINEMENT.md`. The installed idle now has 32 consistent frames over four seconds, derived from one cleaned master pose. The eight-pose, one-second version described below is historical.

Implemented directly in the existing Black-Cube checkout on 2026-09-07.

- Source art: `D:/Documents/BlackCubeAssets/PlayerCharacterBlackCube.png` and `PlayerCharacterBlackCubeIdleAnimation.png`. Exact copies retained in `ReviewCaptures/PlayerIdle/Sources/`; originals untouched.
- Eight original poses extracted to `Assets/Art/PaperBattle/PlayerIdle/PlayerIdle1.png` through `PlayerIdle8.png`. True RGBA transparency, labels and dark backdrop removed, thin ground-shadow streaks cleaned. Identical 256×544 canvases, foot baseline at row 532, no resizing or generative redraw. Minor source pose/contour differences are retained.
- Unity import metadata: single sprites, full rectangular mesh, bilinear filtering, no mipmaps, uncompressed, clamp wrap, 150 pixels per unit, shared bottom foot pivot. Visible character height is about 3.45 world units, close to the prior player's scale.
- `PaperBattle.prefab` now assigns the first new frame to the player renderer and the eight frames to its idle sequence. `SampleScene` already instantiates this prefab and has no player sprite override. No scene rebuild is required.
- `PaperSpriteActor` plays the supplied sequence at 8 fps (one second per loop), uses scaled time, holds during pause/death, resumes on revival, and keeps the player facing right. Anticipation/strike calls leave the new idle art active until the user supplies combat animations. Combat logic and damage-popup positioning were not changed. Ghoul four-pose playback remains intact.
- `PaperBattleSceneBuilder` now loads these sprites if the user explicitly rebuilds later. The builder was not run.

## Validation

- Visually reviewed the extraction contact sheet, including hair, clothes, hands, soles, and transparent gaps on a contrasting green background.
- Eight RGBA files verified with transparent margins, no canvas clipping, common foot baseline, unique GUIDs matching prefab references, and expected import settings. Archived inputs match originals by SHA-256.
- `Tools/Art/verify_player_idle.ps1` compiled the actual `PaperSpriteActor.cs` with minimal Unity substitutes and exercised frame ordering/wrap, combat idle retention, facing, pause, death/revival, and legacy ghoul idle/anticipation/strike. Passed. This is a file-only behavior check, not a Unity compilation or Play-mode test.
- Scoped `git diff --check` passed. Prefab diff is limited to the player's animation references and renderer sprite/size. Existing unrelated working-tree changes preserved.
- Unity was not launched, controlled, run in batchmode, or used for testing. No desktop/browser interaction. The user must verify Unity import and actual in-game appearance/playback.

## Review and reproduction

- Contact sheet: `ReviewCaptures/PlayerIdle/contact-sheet.png`.
- Animated preview: `ReviewCaptures/PlayerIdle/idle-preview.gif` (independent preview, not a game capture).
- Crop/translation records: `ReviewCaptures/PlayerIdle/extraction.json`.
- Extraction and wiring scripts: `Tools/Art/prepare_player_idle.py`, `Tools/Art/wire_player_idle.py` (Pillow/NumPy; no network).
- The user explicitly authorized deterministic local image cleanup. No image-generation tool, API call, or generation prompt was used by this implementation task.

In Unity, let the new files import, open the existing SampleScene and test when convenient. Check the new right-facing appearance, idle-loop cadence, feet placement, combat, pause, and death/restart. There is no need to run Build Paper Battle Scene.
