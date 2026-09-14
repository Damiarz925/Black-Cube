"""Focused validation for the goblin/hobgoblin handedness art delivery."""
from pathlib import Path
import hashlib
import json
import re

from PIL import Image


ROOT = Path(__file__).resolve().parents[2]
ART = ROOT / "Assets/Art/PaperBattle/ForestEnemies"
REVIEW = ROOT / "ReviewCaptures/ForestEnemies"
DELIVERY = REVIEW / "HandednessFix/FinalFrames"


def sha256(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def guid(path: Path) -> str:
    return re.search(r"^guid: (\w+)", Path(str(path) + ".meta").read_text(), re.M)[1]


def main():
    report = {"status": "PASS", "species": {}}
    for name in ("Goblin", "Hobgoblin"):
        frames = [ART / f"{name}Attack{i}.png" for i in range(1, 9)]
        hashes = []
        for frame in frames:
            image = Image.open(frame)
            assert image.mode == "RGBA" and image.size == (1152, 896), frame
            alpha = image.getchannel("A")
            box = alpha.getbbox()
            assert box and 0 < box[0] < box[2] < 1152 and 0 < box[1] < box[3] < 896, frame
            assert alpha.getextrema()[0] == 0 and alpha.getextrema()[1] >= 250, frame
            meta = Path(str(frame) + ".meta").read_text()
            for setting in ("spriteMode: 1", "spriteMeshType: 0", "spritePixelsToUnits: 150", "alphaIsTransparency: 1"):
                assert setting in meta, (frame, setting)
            pivot = re.search(r"spritePivot: \{x: ([^,]+), y: ([^}]+)\}", meta)
            assert pivot and abs(float(pivot[1]) - 0.5) < 1e-7
            assert abs(float(pivot[2]) - (80 / 896)) < 1e-7
            delivered = DELIVERY / name / frame.name
            assert sha256(delivered) == sha256(frame), delivered
            hashes.append(sha256(frame))

        asset = (ART / f"{name}Animation.asset").read_text()
        attack_block = re.search(r"  attackFrames:\n((?:  - .*\n)+)", asset)[1]
        assert re.findall(r"guid: (\w+)", attack_block) == [guid(path) for path in frames]
        assert hashes[0] == hashes[7], f"{name} frame 8 must return to frame 1"
        assert hashes[6] != hashes[0], f"{name} frame 7 must remain a distinct recovery pose"

        contact = Image.open(REVIEW / f"{name.lower()}-attack-contact.png")
        assert contact.size == (2304, 944)
        gif = Image.open(REVIEW / f"{name.lower()}-attack.gif")
        assert gif.n_frames == 8
        durations = []
        for index in range(gif.n_frames):
            gif.seek(index)
            durations.append(gif.info["duration"])
        assert sum(durations) == 1000

        report["species"][name] = {
            "frames": 8,
            "canvas": [1152, 896],
            "transparent_rgba": True,
            "import_settings": "PASS",
            "animation_asset_order": "PASS",
            "delivery_hashes_match": True,
            "gif_frames": 8,
            "gif_duration_ms": sum(durations),
            "frame_sha256": hashes,
        }

    actor = (ROOT / "Assets/Scripts/PaperSpriteActor.cs").read_text()
    assert "public const int ImpactFrame = 3" in actor
    report["damage_sync"] = "fourth authored frame (index 3) unchanged"
    output = REVIEW / "HandednessFix/validation.json"
    output.write_text(json.dumps(report, indent=2))
    print(json.dumps(report, indent=2))


if __name__ == "__main__":
    main()
