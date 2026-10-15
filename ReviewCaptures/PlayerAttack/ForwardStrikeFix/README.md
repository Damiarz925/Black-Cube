# Attack-only transparency and forward-impact fix

Installed in the main checkout. The fourth impact drawing now has an authored, natural one-handed right wrist and a blue blade pointing horizontally screen-right. The pose comes from the built-in imagegen edit in `forward-strike-generated.png`; prompt is archived in `prompt.txt`. Uniform canvas scaling and sole alignment keep the existing sprite size/pivot. The blade remains a separate weapon overlay. The other seven weapon cels are unchanged.

Enclosed gray poster gaps were removed using visually checked seeds between arms, torso and coat tails, including the large gap beneath the overhead blade. These corrections target backdrop regions rather than globally deleting gray costume colors. Frames 6 and 7 did not require large-gap removal; frame 7 received a small gap correction. All eight poses were inspected against the checkerboard in `transparency-checker.png`.

Preview: `forward-attack-preview.gif`. Contact: `forward-attack-contact.png`. The standard installed attack preview/contact paths are also updated. `Before/` preserves the prior body and weapon assets.

Checks passed: all marked gap seeds have zero alpha; the impact blade angle is approximately -0.05 degrees (horizontal); the other seven blade cels are pixel-identical; all 20 installed animation PNGs are nonempty, transparent and unclipped, with valid GUID references, sprite pivots and PPU; preview duration is one second. SHA-256 checks confirm idle files, the player prefab, BattleManager and PaperSpriteActor are unchanged, preserving attack-speed scaling and frame-4 damage synchronization. The preparation script reapplies this approved correction when rebuilding art.

No Unity launch, Play control, desktop interaction or in-game validation was performed. Work is complete for these two requested attack-art fixes.
