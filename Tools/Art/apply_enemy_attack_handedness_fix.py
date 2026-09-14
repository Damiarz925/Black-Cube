"""Install the approved left-hand enemy attack corrections and rebuild previews."""
from pathlib import Path
import json
import shutil

import numpy as np
from PIL import Image, ImageDraw


ROOT = Path(__file__).resolve().parents[2]
ART = ROOT / "Assets/Art/PaperBattle/ForestEnemies"
REVIEW = ROOT / "ReviewCaptures/ForestEnemies"
WORK = REVIEW / "HandednessFix"
DELIVERY = WORK / "FinalFrames"
GENERATED = WORK / "GeneratedSources"

SOURCES = {
    "Goblin": {
        "rest": "Goblin-exec-ba59c18a-30d2-43ea-a6b2-df715f23d3c0.png",
        "frame3": "Goblin-exec-67425374-e82c-4d6f-bfd9-f94fcf1ad397.png",
        "frame7": "Goblin-frame7-exec-462b48e0-d76d-467e-8e4b-a5785f63d0b0.png",
    },
    "Hobgoblin": {
        "rest": "Hobgoblin-exec-3011e254-0e86-43a1-86f8-6f0e5f2c7201.png",
        "frame3": "Hobgoblin-exec-158518c7-8e90-42fa-a780-5193c97bed6c.png",
        "frame7": "Hobgoblin-exec-02115c68-dc0f-4a0c-8665-bd3caa288d6c.png",
    },
}


def largest_component(mask: np.ndarray) -> np.ndarray:
    """Keep the connected character and discard low-level background variation."""
    parent = []
    runs = []
    previous = []

    def find(index):
        while parent[index] != index:
            parent[index] = parent[parent[index]]
            index = parent[index]
        return index

    for y, row in enumerate(mask):
        changes = np.diff(np.pad(row.astype(np.int8), (1, 1)))
        current = []
        for left, right in zip(np.where(changes == 1)[0], np.where(changes == -1)[0]):
            index = len(parent)
            parent.append(index)
            runs.append((y, int(left), int(right)))
            for old_left, old_right, old_index in previous:
                if old_right >= left and old_left <= right:
                    parent[find(index)] = find(old_index)
            current.append((left, right, index))
        previous = current
    groups = {}
    for index, run in enumerate(runs):
        groups.setdefault(find(index), []).append(run)
    group = max(groups.values(), key=lambda value: sum(right - left for _, left, right in value))
    result = np.zeros(mask.shape, dtype=bool)
    for y, left, right in group:
        result[y, left:right] = True
    return result


def remove_generated_background(path: Path) -> Image.Image:
    """Convert ImageGen's magenta or checker matte to a clean RGBA cutout."""
    rgb = np.asarray(Image.open(path).convert("RGB"), dtype=np.float32)
    key = np.array([255.0, 0.0, 255.0], dtype=np.float32)
    distance = np.max(np.abs(rgb - key), axis=2)
    if float(distance[0, 0]) < 28.0:
        character = largest_component(distance > 28.0)
        alpha = np.clip((distance - 18.0) / 82.0, 0.0, 1.0)
        alpha[~character] = 0.0
        # Undo the magenta edge matte before downsampling onto the game canvas.
        safe = np.maximum(alpha[..., None], 1.0 / 255.0)
        foreground = (rgb - (1.0 - alpha[..., None]) * key) / safe
        foreground = np.clip(foreground, 0, 255)
    else:
        # The frame-7 edit returned a baked neutral checker. Flood only the
        # bright low-saturation pixels connected to the canvas edge; enclosed
        # neutral weapon pixels remain part of the character.
        # Seed the connected character from its saturated paint and dark ink.
        # Filling the closed silhouette retains enclosed neutral dagger metal,
        # while the checker itself never enters the foreground seed.
        character_seed = ((rgb.max(2) - rgb.min(2)) >= 70.0) | (rgb.min(2) <= 120.0)
        character_seed = largest_component(character_seed)
        # copy() makes the NumPy-backed image writable for ImageDraw.floodfill.
        flood = Image.fromarray(np.uint8(~character_seed) * 255, "L").copy()
        ImageDraw.floodfill(flood, (0, 0), 128, thresh=0)
        alpha = (np.asarray(flood) != 128).astype(np.float32)
        foreground = rgb
    foreground[alpha <= 0.0] = 0
    rgba = np.dstack((foreground, alpha[..., None] * 255.0)).astype(np.uint8)
    return Image.fromarray(rgba, "RGBA")


def feet_registration(image: Image.Image):
    alpha = np.asarray(image.getchannel("A")) > 127
    ys = np.where(alpha)[0]
    ground = int(ys.max())
    xs = np.where(alpha[max(0, ground - 8) : ground + 1].any(axis=0))[0]
    return ground, (int(xs.min()) + int(xs.max())) / 2.0


def normalize(source: Path, target_height: int) -> Image.Image:
    """Match the target's pose height and foot registration on its fixed canvas."""
    target_ground, target_center = 816, 576

    cutout = remove_generated_background(source)
    box = cutout.getchannel("A").getbbox()
    assert box
    cutout = cutout.crop(box)
    scale = target_height / cutout.height
    cutout = cutout.resize(
        (round(cutout.width * scale), target_height), Image.Resampling.LANCZOS
    )
    source_ground, source_center = feet_registration(cutout)
    offset = (
        round(target_center - source_center),
        round(target_ground - source_ground),
    )
    assert offset[0] >= 0 and offset[1] >= 0
    assert offset[0] + cutout.width <= 1152 and offset[1] + cutout.height <= 896
    canvas = Image.new("RGBA", (1152, 896))
    canvas.alpha_composite(cutout, offset)
    return canvas


def rebuild_preview(name: str):
    frames = [Image.open(ART / f"{name}Attack{i}.png").convert("RGBA") for i in range(1, 9)]
    contact = Image.new("RGB", (2304, 944), (104, 134, 113))
    tiles = []
    draw = ImageDraw.Draw(contact)
    for index, frame in enumerate(frames):
        background = Image.new("RGB", frame.size, (104, 134, 113))
        background.paste(frame, (0, 0), frame)
        tile = background.resize((576, 448), Image.Resampling.LANCZOS)
        tiles.append(tile)
        x, y = index % 4 * 576, index // 4 * 472
        contact.paste(tile, (x, y))
        draw.text((x + 280, y + 452), str(index + 1), fill="white")
    stem = name.lower() + "-attack"
    contact.save(REVIEW / f"{stem}-contact.png")
    tiles[0].save(
        REVIEW / f"{stem}.gif",
        save_all=True,
        append_images=tiles[1:],
        duration=[120, 130] * 4,
        loop=0,
        disposal=2,
    )


def main():
    WORK.mkdir(parents=True, exist_ok=True)
    DELIVERY.mkdir(parents=True, exist_ok=True)
    report = {"method": "ImageGen redraws with alpha/matte cleanup, registered to original feet/pivot"}
    for name, generated in SOURCES.items():
        standing_height = 450 if name == "Goblin" else 570
        raised_height = 513 if name == "Goblin" else 689
        rest = normalize(GENERATED / generated["rest"], standing_height)
        frame3 = normalize(GENERATED / generated["frame3"], raised_height)
        frame7 = normalize(GENERATED / generated["frame7"], standing_height)
        for index, image in ((1, rest), (3, frame3), (7, frame7), (8, rest)):
            image.save(ART / f"{name}Attack{index}.png", optimize=True)
        rest.save(ART / f"{name}Idle.png", optimize=True)
        rebuild_preview(name)

        species_delivery = DELIVERY / name
        species_delivery.mkdir(exist_ok=True)
        for index in range(1, 9):
            source = ART / f"{name}Attack{index}.png"
            shutil.copyfile(source, species_delivery / source.name)
        report[name] = {
            "changed_attack_frames": [1, 3, 7, 8],
            "unchanged_reference_frames": [2, 4, 5, 6],
            "delivery": str(species_delivery.relative_to(ROOT)),
        }
    (WORK / "installation.json").write_text(json.dumps(report, indent=2))
    print(json.dumps(report, indent=2))


if __name__ == "__main__":
    main()
