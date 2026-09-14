"""Remove only the two baked orange HUD accents from corruption toolbar PNGs."""

from pathlib import Path
import shutil
import sys

from PIL import Image


PROJECT_ROOT = Path(__file__).resolve().parents[2]
TOOLBAR_DIR = PROJECT_ROOT / "Assets" / "Resources" / "UI" / "Corruption"
BACKUP_DIR = PROJECT_ROOT / "ReviewCaptures" / "HUDToolbarOriginals"
STAGE_ROWS = {
    "0": range(54, 77),
    "20": range(54, 77),
    "40": range(54, 77),
    "60": range(54, 77),
    "80": range(54, 77),
    "100": range(54, 77),
}
X_RANGES = (range(292, 543), range(710, 934))


def require_project_path(path: Path) -> Path:
    resolved = path.resolve()
    resolved.relative_to(PROJECT_ROOT)
    return resolved


def is_accent(pixel: tuple[int, int, int, int]) -> bool:
    red, green, blue, alpha = pixel
    return (
        alpha >= 180
        and red >= 30
        and green >= 15
        and blue <= 50
        and red - green >= 6
        and green - blue >= 2
    )


def clean_source_x(x: int) -> int:
    # The diagonal base texture repeats exactly in these clean source spans.
    return 950 + (x - 292) if x <= 542 else 955 + (x - 710)


def clean_stage(stage: str) -> str:
    source_path = require_project_path(TOOLBAR_DIR / f"{stage}_toolbar.png")
    backup_path = require_project_path(BACKUP_DIR / f"{stage}_toolbar.original.png")
    temporary_path = require_project_path(TOOLBAR_DIR / f"{stage}_toolbar.accent-clean.tmp.png")
    if not backup_path.exists():
        shutil.copy2(source_path, backup_path)
    if temporary_path.exists():
        temporary_path.unlink()

    # Always derive the output from the untouched backup so reruns are deterministic.
    with Image.open(backup_path) as loaded:
        source = loaded.convert("RGBA")
    with Image.open(BACKUP_DIR / "0_toolbar.original.png") as loaded:
        base_texture = loaded.convert("RGBA")
    if source.size != (1600, 98):
        raise RuntimeError(f"Unexpected dimensions for {source_path}: {source.size}")
    result = source.copy()
    source_pixels = source.load()
    clean_pixels = source_pixels if stage in {"0", "20"} else base_texture.load()
    result_pixels = result.load()
    remove = {
        (x, y)
        for y in STAGE_ROWS[stage]
        for x_range in X_RANGES
        for x in x_range
        if (
            source_pixels[x, y] != clean_pixels[clean_source_x(x), y]
            if stage in {"0", "20"}
            else is_accent(source_pixels[x, y])
        )
    }

    for x, y in remove:
        result_pixels[x, y] = clean_pixels[clean_source_x(x), y]

    result.save(temporary_path, format="PNG")
    with Image.open(temporary_path) as loaded:
        candidate = loaded.convert("RGBA")
    if candidate.size != (1600, 98):
        raise RuntimeError(f"Generated dimensions changed for stage {stage}: {candidate.size}")
    differences = {
        (x, y)
        for y in range(source.height)
        for x in range(source.width)
        if source_pixels[x, y] != candidate.getpixel((x, y))
    }
    if not differences.issubset(remove):
        unexpected = differences.difference(remove)
        sample = sorted(unexpected)[:5]
        raise RuntimeError(f"Stage {stage} changed unexpected pixels: {sample}")

    temporary_path.replace(source_path)
    return f"{stage}%: changed {len(differences)} of {len(remove)} matched accent pixels; 1600x98; every other RGBA pixel unchanged."


def main() -> int:
    BACKUP_DIR.mkdir(parents=True, exist_ok=True)
    for stage in STAGE_ROWS:
        print(clean_stage(stage))
    return 0


if __name__ == "__main__":
    sys.exit(main())
