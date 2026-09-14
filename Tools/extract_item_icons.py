"""Extract 240 transparent item icons plus mod-filter and area-corruption borders."""
from __future__ import annotations

import argparse
from collections import deque
from pathlib import Path
from PIL import Image, ImageFilter


PANELS = (
    ("forest", False, 0, 0),
    ("forest", True, 1, 0),
    ("desert", False, 2, 0),
    ("desert", True, 3, 0),
    ("tundra", False, 4, 0),
    ("tundra", True, 0, 1),
    ("volcanic", False, 1, 1),
    ("volcanic", True, 2, 1),
    ("city", False, 3, 1),
    ("city", True, 4, 1),
)

# Cell boundaries measured through the gutters in the high-resolution 1448x1086
# transparent sheet. Source columns retain the original set 3, 2, 1 ordering.
X_BOUNDARIES = (0,105,191,283,397,480,566,683,768,853,969,1052,1140,1251,1337,1448)
Y_BOUNDARIES = (
    (0,87,156,222,289,359,423,483,543),
    (543,624,693,758,822,884,943,1001,1086),
)
GEAR = ("helmet", "body", "gloves", "boots", "sword", "amulet", "ring", "belt")
SETS_LEFT_TO_RIGHT = (3, 2, 1)
CELL_SIZE = 96
ICON_SIZE = 92
def isolate_icon(sheet: Image.Image, bounds: tuple[int, int, int, int]) -> Image.Image:
    """Keep the substantial artwork in one cell and discard adjacent-row bleed."""
    region = sheet.crop(bounds)
    width, height = region.size
    alpha = bytes(region.getchannel("A").get_flattened_data())
    # A firm connectivity threshold prevents faint glow fringes from joining
    # neighboring icons. Original antialiasing is restored around kept shapes.
    active = bytearray(value > 128 for value in alpha)
    visited = bytearray(width*height)
    components: list[tuple[list[int], bool]] = []

    for seed in range(width*height):
        if not active[seed] or visited[seed]:
            continue
        queue = deque([seed])
        visited[seed] = 1
        component: list[int] = []
        touches_edge = False
        while queue:
            index = queue.popleft()
            px, py = index % width, index // width
            component.append(index)
            touches_edge |= px == 0 or py == 0 or px == width-1 or py == height-1
            for ny in range(max(0, py-1), min(height, py+2)):
                row = ny*width
                for nx in range(max(0, px-1), min(width, px+2)):
                    neighbor = row+nx
                    if active[neighbor] and not visited[neighbor]:
                        visited[neighbor] = 1
                        queue.append(neighbor)
        components.append((component, touches_edge))

    largest = max((len(component) for component,_ in components), default=0)
    minimum_area = max(12, round(largest*.12))
    selected = bytearray(width*height)
    for component,touches_edge in components:
        if len(component) >= minimum_area and (not touches_edge or len(component) == largest):
            for index in component:
                selected[index] = 255

    selection = Image.frombytes("L", region.size, bytes(selected)).filter(ImageFilter.MaxFilter(7))
    kept_alpha = bytes(
        original if keep else 0
        for original,keep in zip(alpha,selection.get_flattened_data())
    )
    region.putalpha(Image.frombytes("L", region.size, kept_alpha))
    visible = region.getchannel("A").point(lambda value: 255 if value > 32 else 0).getbbox()
    if visible is None:
        raise ValueError(f"No isolated icon found in cell {bounds}")
    left, top, right, bottom = visible
    visible = (max(0,left-2), max(0,top-2), min(width,right+2), min(height,bottom+2))
    return region.crop(visible)


def extract_icons(sheet_path: Path, output_root: Path) -> None:
    sheet = Image.open(sheet_path).convert("RGBA")
    if sheet.size != (1448, 1086):
        raise ValueError(f"Expected the supplied 1448x1086 sheet, received {sheet.size}")

    exported = 0
    for theme, corrupted, panel_column, panel_row in PANELS:
        variant = f"corrupted_{theme}" if corrupted else theme
        for source_column, set_number in enumerate(SETS_LEFT_TO_RIGHT):
            global_column = panel_column*3+source_column
            x0,x1 = X_BOUNDARIES[global_column:global_column+2]
            destination = output_root / variant / f"set_{set_number}"
            destination.mkdir(parents=True, exist_ok=True)
            for row, gear_name in enumerate(GEAR):
                y0,y1 = Y_BOUNDARIES[panel_row][row:row+2]
                icon = isolate_icon(sheet, (x0,y0,x1,y1))
                icon.putalpha(icon.getchannel("A").point(lambda value: 0 if value <= 8 else value))
                icon.thumbnail((ICON_SIZE,ICON_SIZE),Image.Resampling.LANCZOS)
                tile = Image.new("RGBA",(CELL_SIZE,CELL_SIZE),(0,0,0,0))
                tile.alpha_composite(icon,((CELL_SIZE-icon.width)//2,(CELL_SIZE-icon.height)//2))
                tile.save(destination / f"{gear_name}.png", optimize=True)
                exported += 1
    if exported != 240:
        raise RuntimeError(f"Expected 240 icons, exported {exported}")


def prepare_border(border_path: Path, output_root: Path) -> None:
    border = Image.open(border_path).convert("RGBA")
    alpha = border.getchannel("A")
    # Ignore near-transparent fringe when finding the artwork bounds, but retain
    # the original antialiasing inside that crop.
    bounds = alpha.point(lambda value: 255 if value > 8 else 0).getbbox()
    if bounds is None:
        raise ValueError("Highlight border contains no visible pixels")
    border = border.crop(bounds).resize((256, 256), Image.Resampling.LANCZOS)
    border.save(output_root / "mod_highlight_border.png", optimize=True)


def prepare_corruption_borders(border_sheet_path: Path, output_root: Path) -> None:
    sheet = Image.open(border_sheet_path).convert("RGBA")
    if sheet.size != (2172,724):
        raise ValueError(f"Expected the supplied 2172x724 border sheet, received {sheet.size}")
    percentages = (0,20,40,60,80,100)
    for index,percentage in enumerate(percentages):
        x0=round(index*sheet.width/6);x1=round((index+1)*sheet.width/6)
        cell=sheet.crop((x0,0,x1,sheet.height))
        bounds=cell.getchannel("A").point(lambda value:255 if value>8 else 0).getbbox()
        if bounds is None:
            raise ValueError(f"No visible corruption border found for {percentage}%")
        border=cell.crop(bounds).resize((256,256),Image.Resampling.LANCZOS)
        border.save(output_root/f"corruption_border_{percentage}.png",optimize=True)


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--sheet", type=Path, required=True)
    parser.add_argument("--border", type=Path, required=True)
    parser.add_argument("--corruption-borders", type=Path)
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    args.output.mkdir(parents=True, exist_ok=True)
    extract_icons(args.sheet, args.output)
    prepare_border(args.border, args.output)
    if args.corruption_borders is not None:
        prepare_corruption_borders(args.corruption_borders,args.output)
    print(f"Exported 240 item icons and UI borders to {args.output}")


if __name__ == "__main__":
    main()
