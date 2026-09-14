# Authored idle poses and corrected single-slash review

This supersedes the earlier master-deformation idle revisions. The user explicitly rejected morphing one drawing, particularly because a moving right hand must reveal freshly drawn coat fabric.

## Installed idle

- Generated eight independently drawn guard/weight-shift poses from the approved character references. Source: `ReviewCaptures/PlayerIdle/authored-idle-source.png`.
- Applied only background removal, one uniform sheet-wide rescale, cropping, and translation to align the foot baseline. **No deformation, motion synthesis, morphing, crossfades or interpolated poses.**
- Ordered the source drawings 1,4,3,2,7,6,8,5 to group the raised/half-lowered/lowered hand changes into a more gradual sequence and return to a similar raised-hand guard.
- Eight authored poses, each held 0.5 seconds: four-second loop. Existing 32 runtime slots are retained for compatibility, with four byte-identical copies of each pose at the existing 8fps. These duplicates are holds, not additional drawn poses.
- Current assets remain `Assets/Art/PaperBattle/PlayerIdle/PlayerIdle1.png` through `PlayerIdle32.png`. GUIDs, sprite import settings, actor code and prefab timing remain as previously wired. No C# edits or Unity interaction during this revision.
- Drawing variation remains inherent in these authored frames; no pixel-identical-foot or continuous-motion claim. Sole baseline is aligned at row532, with transparent unclipped margins.

## Visual review and checks

- Reviewed all eight aligned drawings and enlarged right-hand/coat crops. Lowered-hand poses show newly drawn exposed coat/shirt panels where the raised hand previously overlapped them. The hand is not dragging warped coat texture.
- Confirmed consistent side for the near/right arm and diagonal chest strap; knees, shoulders, arm angle and torso posture are independently drawn.
- Validated eight unique image hashes in32 runtime slots, four identical hold copies per pose; all existing GUIDs match prefab references. Both GIFs have eight poses and total four-second timing.
- Transparent margins, common sole baseline, and scoped diff whitespace checks passed. This remains file/visual inspection, not Unity validation. User handles in-game testing.
- Large preview: `ReviewCaptures/PlayerIdle/authored-idle-preview.gif`.
- Representative 900px-viewport scale preview: `ReviewCaptures/PlayerIdle/authored-idle-game-scale.gif`.
- Contact sheet: `ReviewCaptures/PlayerIdle/authored-idle-contact-sheet.png`.
- Hand/coat occlusion detail: `ReviewCaptures/PlayerIdle/authored-hand-coat-review.png`.
- Rebuild script: `Tools/Art/install_authored_idle.py`; extraction records and full image-generation prompt are alongside the previews.

## Attack review only

Final sheet: `ReviewCaptures/PlayerAttack/single-downward-slash-review.png`.

Inspected all eight frames: guarded idle with sword already held → stronger dynamic ready stance → high windup → downward slash → lower follow-through continuing the same arc → recovery → guard settling → matching guard. Frame5 is no longer the original horizontal stabbing thrust. Endpoint sword guides use the raised near/right hand to avoid swapping hands at entry/recovery.

The sheet is a flattened concept with a blue sword guide; it is not a production-ready separated weapon/body asset or a guarantee of final animation timing. The review endpoints follow the current guarded-idle posture; precise transition frames and grip transforms still require production preparation after approval. Attack remains **unimplemented pending user approval**. No inventory/weapon architecture changes.

Generated with the built-in image-generation tool. The dynamic action, endpoint and grip-edit prompts are retained in `ReviewCaptures/PlayerAttack/`. Original supplied design/idle references remain untouched.

No Unity launch, batchmode, desktop/browser control, or Unity tests occurred.
