# Chibi player replacement

Latest refinement: idle sword points forward/down at -25 degrees in front of the body, with matching attack endpoints. Complete alpha-bound extraction replaces dense-band cropping, preserving sparse hair tips in all sixteen source poses. Source retention and importer/canvas padding checks pass. The final expanded player/enemy combat harness has 3,940 passing assertions; see ForestEnemies/README.md for the complete batch outcome.

The main PaperBattle prefab and scene builder use ChibiPlayer/PlayerAnimation.asset: eight weapon-free idle poses at two frames per second and eight combat poses driven by the existing attack gauge. The fourth attack pose is the horizontal forward impact. This replaces the earlier tall player art configuration; older art remains archived.

Sword.png contains the complete sword, including its handle. Per-pose hand positions and angles attach it to the body. An equipped Gear can supply a PaperWeaponVisual profile; equipped gear without one uses DefaultSword. An empty weapon slot hides the entire weapon. Additional weapon art profiles are not supplied in this change.

## Verification

- verify_combat_animation.ps1: PASS, 3,158 assertions using the actual BattleManager, PaperSpriteActor, animation/weapon profile classes, and extracted PlayerController equipment getter/method with Unity and other gameplay dependencies stubbed. Covers cadence, speed changes, frame-four damage synchronization, pause, death, target replacement, catch-up, legacy ghoul behavior, equipment switching during all eight poses and idle, different profiles, attachment transforms/layers, disable/re-enable, and the configured eight-frame/four-second idle cycle.
- verify_chibi_player_assets.py: PASS, all sixteen transparent body frames, full sword, sprite/config/script references, 32 serialized grip entries, four GIFs and their duration, and 14 protected file hashes. BattleManager, ZoneManager, forest images and forest metadata remain unchanged from the pre-replacement snapshot.
- verify_forest_assets.py: PASS, six original forest images, metadata and ordered prefab references.
- Visual review: equipped idle/attack contact sheets and unarmed attack sheet inspected. Recovery frame six keeps the sword visible; impact points forward; body poses have empty hands.

Unity was not launched. Import, full-project compilation, and in-game rendering were not tested. GIFs are art previews, not live game captures. The attack preview shows a one-second example; gameplay cadence follows current attack speed. Eight authored poses remain visibly stepped.

## Previews

- idle-equipped.gif and idle-unarmed.gif
- attack-equipped.gif and attack-unarmed.gif
- idle-equipped-contact.png and attack-equipped-contact.png
- idle-body-contact.png and attack-body-contact.png

The copied generated sources and original outline references are alongside this file. preparation.json records frame extraction/alignment; attachments.json records hand positions, angles, and sword pivot.
