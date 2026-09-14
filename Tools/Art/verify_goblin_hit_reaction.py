"""Validate installed goblin hit art, references, timing and source wiring."""
from pathlib import Path
import hashlib
import json
import re

import numpy as np
from PIL import Image


ROOT = Path(__file__).resolve().parents[2]
ART = ROOT / "Assets/Art/PaperBattle/ForestEnemies"
REVIEW = ROOT / "ReviewCaptures/ForestEnemies/GoblinHitReaction"
DELIVERY = REVIEW / "FinalFrames"


def digest(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def meta_guid(path: Path) -> str:
    return re.search(r"^guid: (\w+)", Path(str(path) + ".meta").read_text(), re.M)[1]


def main():
    names = [f"GoblinHit{i}.png" for i in range(1, 6)]
    hashes = []
    for name in names:
        path = ART / name
        image = Image.open(path)
        assert image.mode == "RGBA" and image.size == (1152, 896), name
        alpha = np.asarray(image.getchannel("A"))
        ys, xs = np.where(alpha > 127)
        assert len(xs) and ys.max() == 816, (name, "sole registration", int(ys.max()))
        assert 0 < xs.min() < xs.max() < 1152 and 0 < ys.min() < ys.max() < 896
        assert alpha.min() == 0 and alpha.max() >= 250
        rgba = np.asarray(image)
        assert np.count_nonzero(rgba[alpha == 0, :3]) == 0, (name, "hidden RGB")
        meta = Path(str(path) + ".meta").read_text()
        for setting in ("spriteMode: 1", "spriteMeshType: 0", "spritePixelsToUnits: 150", "alphaIsTransparency: 1"):
            assert setting in meta, (name, setting)
        pivot = re.search(r"spritePivot: \{x: ([^,]+), y: ([^}]+)\}", meta)
        assert pivot and abs(float(pivot[1]) - 0.5) < 1e-7
        assert abs(float(pivot[2]) - (80 / 896)) < 1e-7
        assert digest(path) == digest(DELIVERY / name), (name, "delivery mismatch")
        hashes.append(digest(path))

    assert digest(ART / "GoblinHit5.png") == digest(ART / "GoblinIdle.png")
    asset = (ART / "GoblinAnimation.asset").read_text()
    block = re.search(r"  hitFrames:\n((?:  - .*\n)+)", asset)[1]
    assert re.findall(r"guid: (\w+)", block) == [meta_guid(ART / name) for name in names]
    assert "  hitReactionDuration: 0.21" in asset
    assert not list(ART.glob("HobgoblinHit*.png")), "Hobgoblin hit art was not authorized"

    contact = Image.open(REVIEW / "goblin-hit-contact.png")
    assert contact.size == (2880, 496)
    preview = Image.open(REVIEW / "goblin-hit.gif")
    assert preview.n_frames == 5
    durations = []
    for index in range(preview.n_frames):
        preview.seek(index)
        durations.append(preview.info["duration"])
    assert sum(durations) == 210

    receiver = (ROOT / "Assets/Scripts/DamageReceiver.cs").read_text()
    damage_index = receiver.index("health.LoseLife(damage);")
    reaction_index = receiver.index("paperSprite?.PlayHitReaction();")
    assert damage_index < reaction_index
    actor = (ROOT / "Assets/Scripts/PaperSpriteActor.cs").read_text()
    for token in ("hitReactionPending && nextPose > ImpactFrame", "Re-hit restarts from the impact drawing", "health.CurrentLife <= 0f"):
        assert token in actor

    report = {
        "status": "PASS",
        "frames": 5,
        "canvas": [1152, 896],
        "sole_y": 816,
        "transparent_rgba": True,
        "import_settings": "PASS",
        "asset_order": "PASS",
        "delivery_hashes_match": True,
        "frame_sha256": hashes,
        "runtime_duration_seconds": 0.21,
        "gif_frames": 5,
        "gif_duration_ms": sum(durations),
        "damage_before_visual_notification": True,
        "hobgoblin_hit_animation_created": False,
    }
    (REVIEW / "validation.json").write_text(json.dumps(report, indent=2))
    print(json.dumps(report, indent=2))


if __name__ == "__main__":
    main()
