"""Prepare, install and preview the five-frame goblin hit reaction."""
from pathlib import Path
import json
import re
import shutil
import uuid

from PIL import Image, ImageDraw

from apply_enemy_attack_handedness_fix import normalize


ROOT = Path(__file__).resolve().parents[2]
ART = ROOT / "Assets/Art/PaperBattle/ForestEnemies"
REVIEW = ROOT / "ReviewCaptures/ForestEnemies/GoblinHitReaction"
SOURCES = REVIEW / "GeneratedSources"
DELIVERY = REVIEW / "FinalFrames"
DURATION_MS = 210


def stable_guid(name: str) -> str:
    # Match install_forest_enemies.py so a later prefab/art rebuild preserves refs.
    return uuid.uuid5(uuid.NAMESPACE_URL, "black-cube/forest-enemies/" + name).hex


def sprite_refs(names):
    return "".join(
        f"  - {{fileID: 21300000, guid: {stable_guid(name)}, type: 3}}\n"
        for name in names
    )


def main():
    DELIVERY.mkdir(parents=True, exist_ok=True)
    names = [f"GoblinHit{i}.png" for i in range(1, 6)]
    frames = []
    for index in range(1, 5):
        frame = normalize(SOURCES / f"GoblinHit{index}.png", 450)
        target = ART / f"GoblinHit{index}.png"
        frame.save(target, optimize=True)
        frames.append(frame)
    idle = Image.open(ART / "GoblinIdle.png").convert("RGBA")
    idle.save(ART / "GoblinHit5.png", optimize=True)
    frames.append(idle)

    template = (ART / "GoblinAttack1.png.meta").read_text()
    for name in names:
        meta = re.sub(r"^guid: .*?$", "guid: " + stable_guid(name), template, flags=re.M)
        meta = re.sub(r"    spriteID: .*?$", "    spriteID: " + stable_guid(name + "/sprite"), meta, flags=re.M)
        Path(str(ART / name) + ".meta").write_text(meta)
        shutil.copyfile(ART / name, DELIVERY / name)

    asset_path = ART / "GoblinAnimation.asset"
    asset = asset_path.read_text()
    block = "  hitFrames:\n" + sprite_refs(names) + "  hitReactionDuration: 0.21\n"
    asset = re.sub(r"  hitFrames:\n(?:  - .*\n)*  hitReactionDuration: .*\n", block, asset)
    if "  hitFrames:\n" not in asset:
        asset = asset.replace("  popupOffset:", block + "  popupOffset:")
    asset_path.write_text(asset)

    contact = Image.new("RGB", (2880, 496), (104, 134, 113))
    tiles = []
    draw = ImageDraw.Draw(contact)
    for index, frame in enumerate(frames):
        background = Image.new("RGB", frame.size, (104, 134, 113))
        background.paste(frame, (0, 0), frame)
        tile = background.resize((576, 448), Image.Resampling.LANCZOS)
        tiles.append(tile)
        x = index * 576
        contact.paste(tile, (x, 0))
        draw.text((x + 280, 458), str(index + 1), fill="white")
    contact.save(REVIEW / "goblin-hit-contact.png")
    tiles[0].save(
        REVIEW / "goblin-hit.gif",
        save_all=True,
        append_images=tiles[1:],
        duration=[40, 50, 50, 40, 30],
        loop=0,
        disposal=2,
    )
    report = {
        "frames": names,
        "runtime_duration_seconds": 0.21,
        "preview_duration_ms": DURATION_MS,
        "policy": "Own attack wind-up/contact frames 0-3 retain priority; a hit starts at recovery frame 4 or immediately from idle/recovery; re-hit restarts frame 1 without queueing.",
        "delivery": str(DELIVERY.relative_to(ROOT)),
    }
    (REVIEW / "installation.json").write_text(json.dumps(report, indent=2))
    print(json.dumps(report, indent=2))


if __name__ == "__main__":
    main()
