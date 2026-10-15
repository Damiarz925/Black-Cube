# Developer map: Legacy tall-player idle extraction and background flood helper. Current chibi preparation imports flood but does not run this script as an installer.
# See Docs/DEVELOPER_HANDOFF.md for system flow and validation.
"""Extract the user-supplied eight-frame idle without generative redraws.

Run with Pillow and NumPy. Source files are read-only; outputs stay in project.
"""
from collections import deque
from pathlib import Path
import json
import numpy as np
from PIL import Image, ImageFilter

ROOT = Path(__file__).resolve().parents[2]
SOURCE = Path('D:/Documents/BlackCubeAssets/PlayerCharacterBlackCubeIdleAnimation.png')
OUT = ROOT / 'Assets/Art/PaperBattle/PlayerIdle'
REVIEW = ROOT / 'ReviewCaptures/PlayerIdle'


def flood(allowed, seeds):
    h, w = allowed.shape
    visited = np.zeros((h, w), dtype=bool)
    q = deque()
    for x, y in seeds:
        if allowed[y, x] and not visited[y, x]:
            visited[y, x] = True
            q.append((x, y))
    while q:
        x, y = q.popleft()
        for nx, ny in ((x-1,y),(x+1,y),(x,y-1),(x,y+1)):
            if 0 <= nx < w and 0 <= ny < h and allowed[ny,nx] and not visited[ny,nx]:
                visited[ny,nx] = True
                q.append((nx,ny))
    return visited


def main():
    OUT.mkdir(parents=True, exist_ok=True)
    REVIEW.mkdir(parents=True, exist_ok=True)
    sheet = Image.open(SOURCE).convert('RGB')
    # Boundaries lie in the empty gutters, excluding the title and frame labels.
    boundaries = [24, 242, 456, 670, 884, 1096, 1308, 1520, 1740]
    frames, records = [], []
    for i, (left, right) in enumerate(zip(boundaries, boundaries[1:]), 1):
        crop = sheet.crop((left, 128, right, 662))
        rgb = np.array(crop).astype(float)
        h,w,_ = rgb.shape
        # Estimate the smooth neutral backdrop from each row's empty margins.
        bg = np.median(np.concatenate((rgb[:,:8], rgb[:,-8:]), axis=1), axis=1)
        delta = np.max(np.abs(rgb - bg[:,None,:]), axis=2)
        allowed = (delta < 17) & ((rgb.max(axis=2)-rgb.min(axis=2)) < 12)
        allowed[500:] = (delta[500:] < 26) & ((rgb.max(axis=2)-rgb.min(axis=2))[500:] < 12)
        seeds = [(x,0) for x in range(w)] + [(x,h-1) for x in range(w)]
        seeds += [(0,y) for y in range(h)] + [(w-1,y) for y in range(h)]
        background = flood(allowed, seeds)
        mask = ~background
        # Retain only the character connected to its torso, discarding labels/noise.
        mask = flood(mask, [(w//2, h//2)])
        # Remove thin ground-shadow streaks beside the soles, leaving boot interiors intact.
        opened = np.array(Image.fromarray((mask*255).astype('uint8'))
                          .filter(ImageFilter.MinFilter(5)).filter(ImageFilter.MaxFilter(5))) > 0
        mask[500:] &= opened[500:]
        alpha = Image.fromarray((mask*255).astype('uint8'))
        # Subpixel soften just the extracted contour; interior RGB is unchanged.
        alpha = alpha.filter(ImageFilter.GaussianBlur(0.35))
        rgba = crop.convert('RGBA')
        rgba.putalpha(alpha)
        bbox = alpha.getbbox()
        if bbox is None:
            raise RuntimeError(f'No character in frame {i}')
        # Match foot midpoint/baseline, keeping each frame at its original scale.
        solid_y,solid_x = np.where(mask)
        feet_y = int(solid_y.max())
        foot_x = np.where(mask[max(0,feet_y-20):feet_y+1].any(axis=0))[0]
        anchor_x = int(round((int(foot_x.min())+int(foot_x.max()))/2))
        canvas = Image.new('RGBA',(256,544))
        dx,dy = 128-anchor_x, 532-feet_y
        canvas.paste(rgba,(dx,dy))
        canvas.save(OUT / f'PlayerIdle{i}.png')
        frames.append(canvas)
        records.append({'frame':i,'source_rect':[left,128,right,662],
                        'source_feet_anchor':[anchor_x+left,feet_y+128],
                        'translation':[dx,dy],'output_bbox':canvas.getbbox()})
    bg = Image.new('RGB',(256*8,544),(104,134,113))
    for i,frame in enumerate(frames): bg.paste(frame,(i*256,0),frame)
    bg.save(REVIEW/'contact-sheet.png')
    preview=[]
    for frame in frames:
        tile=Image.new('RGB',frame.size,(104,134,113));tile.paste(frame,(0,0),frame)
        preview.append(tile)
    preview[0].save(REVIEW/'idle-preview.gif',save_all=True,append_images=preview[1:],
                    duration=125,loop=0,disposal=2)
    (REVIEW/'extraction.json').write_text(json.dumps(records,indent=2))
    print(json.dumps(records,indent=2))


if __name__ == '__main__': main()
