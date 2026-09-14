# Combat-ready idle and single-slash review

**Superseded by `AUTHORED-IDLE-AND-SLASH.md`:** the user rejected the deformation method. Current installed frames are eight independently drawn poses with only cutout/alignment processing. This document is historical.

Latest user direction: visible, natural left/right combat-ready weight shifts with different poses; a slow cycle with planted feet. Attack starts with the sword already in hand at idle, adopts the original concept's stronger ready stance, performs one downward slash (no frame-five stab), then recovers to swaying idle. Attack is review-only until user approval.

## Installed idle

The previous 2px breathing / 0.65px sway was too subtle. At the scene's orthographic size 5, scale 1 and 150 pixels per unit, a 900px-high viewport renders that sway at just 0.39 screen pixels each direction.

The revised idle uses a newly generated ready-guard drawing matching the character design. To avoid differing clothes, face details and alpha masks popping between generated poses, animation uses one consistent ready-guard source with independently articulated hip, shoulder, elbow/wrist, knee and counterbalancing head displacements. This creates coherent posture changes rather than translating the entire character rigidly.

- 32 RGBA frames, still 8fps and a four-second cycle.
- Upper-body shift up to 15 source pixels each direction, approximately 9 screen pixels at the representative 900px viewport; no increase in speed.
- Small alternating knee/arm adjustments and head counterbalance accompany the hip shift.
- Boots/lower legs from row 445 remain exactly identical throughout. Transparent canvas margins and continuous loop seam checked.
- Current runtime files remain `Assets/Art/PaperBattle/PlayerIdle/PlayerIdle1.png` through `PlayerIdle32.png`, preserving GUIDs and prefab wiring. No additional gameplay code edits.
- Current source: `ReviewCaptures/PlayerIdle/combat-idle-master.png`; full generated reference sheet, source crop and generation prompt retained alongside it.
- Rebuild with `Tools/Art/build_combat_idle.py`. Earlier `prepare_player_idle.py` / `refine_player_idle.py` are historical reproduction tools, not the latest recipe.

## Previews and checks

- Large idle preview: `ReviewCaptures/PlayerIdle/combat-idle-preview.gif`.
- Representative 900px viewport scale: `ReviewCaptures/PlayerIdle/combat-idle-game-scale.gif` (154×326 pixels). Actual screen size depends on the user's viewport; this is not a Unity capture.
- Pose review: `ReviewCaptures/PlayerIdle/combat-idle-key-poses.png`.
- Measurements: `ReviewCaptures/PlayerIdle/combat-idle-checks.json`.
- Reviewed key poses and source extraction; RGBA/margins/fixed boots/loop continuity passed. Both GIFs verified at 32 frames and exactly four seconds. Existing 32 GUIDs and prefab references verified.
- File-only C# harness passed order/wrap, pause/death/revive, facing, combat retention and ghoul behavior using actual actor source with minimal Unity substitutes.
- No Unity launch, batchmode, desktop/browser control or in-game tests. User is testing manually.

## Attack

Corrected concept generation uses the new ready-idle master for endpoints and the original dynamic attack concept for body engagement during the action. The sword remains a visible blue guide throughout, intended for future separate inventory-driven sprites. No attack or weapon architecture is implemented. Generation prompt retained at `ReviewCaptures/PlayerAttack/dynamic-slash-generation-prompt.txt`.

The dynamic-action revision was inspected: frame4 cuts down/right; frame5 lowers the hand/blade through follow-through rather than thrusting. A focused endpoint edit then requests the raised-fist current guard for1/8 and a matching settling pose7 while preserving action frames2–6.
