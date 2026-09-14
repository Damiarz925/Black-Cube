# Installed player idle and attack

Installed directly in `D:/Unity/Projects/Black-Cube`, replacing the rough 32-frame idle in the player prefab and keeping the scene builder consistent. No Unity launch, batch mode, Play controls or UI interaction was performed.

## Idle

Four distinct authored arm drawings over a fixed body, from proof poses 0–3. Playback is `1, 2, 3, 4, 3, 2` at 1.5 steps/second: four seconds total. Return steps reuse existing drawings; there are four unique images, not six. Larger arm jumps, overshoot, near-duplicate lowered cels and the lateral endpoint snap were excluded. This is the user-authorized best available subset, with limited waist-to-hip motion and visible stepping, not the earlier requested fully smooth 32-pose cycle.

Assets: `Assets/Art/PaperBattle/PlayerIdleConsistent/`. Preview: `PlayerIdle/BestAvailable/consistent-idle-game-scale.gif`; contact: `PlayerIdle/BestAvailable/consistent-idle-contact.png`. The old 32 PNGs are preserved but not referenced by the player or scene builder.

## Attack and damage timing

Eight approved poses, including corrected recovery frame 6, are installed under `Assets/Art/PaperBattle/PlayerAttack/`. The fourth drawing is the downward-slash impact. `BattleManager` sets that drawing immediately before the actual `DamageReceiver.TakeDamage` call. The renderer retains it through that frame's LateUpdate, including when a killing hit synchronously spawns a replacement enemy.

Both damage and visual phase use the existing speed-driven gauge. With threshold 100, an attack cycle lasts `1 / current attacks-per-second`; generally the duration is `turnThreshold / (100 × attackSpeed)`. The first windup starts at 62.5% of the first gauge fill and damage still occurs at 100%, preserving existing first-hit and sustained cadence. Recovery uses the next gauge's first 62.5%. Subsequent cycles include all eight poses, and changes in attack speed immediately scale the remaining phase progression. There is no separately timed delayed damage callback or animation event to duplicate or lose hits.

Pause and the skill-tree overlay freeze progression. Death, absent/dead targets and zero attack speed clear the player windup. Spawning a new target resets the gauge and visual state. Status ticks that kill the attacker or replace the target cancel that turn's damage through the existing target checks. Existing enemy gauges, damage calculation, on-hit effects and ten-turn catch-up guard are retained. At rates exceeding render frequency, several legitimate hits can share one displayed impact frame; intermediate poses cannot all be displayed at those rates. Catch-up preserves damage cadence rather than dropping hits.

The blue guide blade is isolated into eight weapon overlay sprites, synchronized with the body pose. `PaperSpriteActor.SetAttackWeaponFrames` provides a replacement-layer hook. The generic gold grip remains with the drawn hand; complete weapon replacements may need corresponding grip cleanup. This does not implement inventory visuals or a weapon system overhaul. Initial idle-to-attack changes and hand-drawn pose registration are still visible; art was accepted as good enough and was not aesthetically regenerated.

Attack preview: `PlayerAttack/installed-attack-preview.gif` (one-second demonstration cycle, actual gameplay rate varies). Contact: `PlayerAttack/installed-attack-contact.png`. Source review: `PlayerAttack/recovery-six-review.png`. Original imagegen prompts and source sheets remain archived beside the review artifacts; installation used deterministic slicing, background extraction, uniform scaling and layer separation.

## Verification

`Tools/Art/verify_combat_animation.ps1` compiled the actual `BattleManager.cs` and `PaperSpriteActor.cs` with small Unity substitutes: 3,049 assertions passed. Scenarios include 0.5/1.2/5/80 attacks per second, impact body/weapon pose at every damage call, all eight poses at displayable rates, mid-windup speed changes, pause and skill tree, zero speed, death/revive, target invalidation/replacement, status cancellation, synchronous kill/spawn, catch-up backlog, four-second idle wrapping and legacy ghoul strike.

`Tools/Art/verify_installed_animation_assets.py` passed: four distinct idle assets/six playback refs, eight body and eight weapon references, unique GUIDs resolving to single-sprite metadata, 150 PPU, correct pivots, twenty nonempty transparent PNGs with unclipped margins, and exact preview durations. `git diff --check` passed for the changed runtime/editor code and player prefab.

Unity asset import, real rendering, the full game build and in-game combat remain untested because Unity control was not authorized. No commits or pushes were made. Existing unrelated gameplay edits were preserved.
