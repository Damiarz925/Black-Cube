# Developer map: Archived tall-player transition cutouts and timing preview; does not install the chibi player.
# See Docs/DEVELOPER_HANDOFF.md for system flow and validation.
from pathlib import Path
import numpy as np
from PIL import Image, ImageFilter, ImageDraw
from assemble_authored_inbetweens import align
from prepare_player_idle import flood
from install_authored_idle import runs

root=Path(__file__).resolve().parents[2]
out=root/'ReviewCaptures/PlayerIdle/CoherentKeys'
def cutout(im):
    a=np.array(im.convert('RGB')); h,w,_=a.shape
    white=a.min(axis=2)>210
    seeds=[(x,0) for x in range(w)]+[(x,h-1) for x in range(w)]
    seeds += [(0,y) for y in range(h)]+[(w-1,y) for y in range(h)]
    mask=~flood(white,seeds)
    mask=flood(mask,[(w//2,h//2)]) & ~white
    alpha=Image.fromarray(np.uint8(mask)*255).filter(ImageFilter.MinFilter(3)).filter(ImageFilter.GaussianBlur(.35))
    im=im.convert('RGBA');im.putalpha(alpha)
    return align(im,516)[0]
sheet=Image.open(out/'inbetween-test.png').convert('RGB')
frames=[cutout(Image.open(out/'key-2.png'))]
a=np.array(sheet); ink=a.min(axis=2)<160
columns=[(int(x),int(y)) for x,y in runs(ink.sum(axis=0)>5) if y-x>70]
assert len(columns)==7, columns
for x,y in columns:
    frames.append(cutout(sheet.crop((max(0,x-3),0,min(sheet.width,y+3),sheet.height))))
frames.append(cutout(Image.open(out/'key-3.png')))
source=out/'ProofSources';source.mkdir(exist_ok=True)
for i,f in enumerate(frames): f.save(source/f'pose-{i}.png')
tiles=[]
contact=Image.new('RGB',(9*192,350),(104,134,113))
detail=Image.new('RGB',(9*220,360),(104,134,113))
for i,f in enumerate(frames):
    rgb=Image.new('RGB',f.size,(104,134,113));rgb.paste(f,(0,0),f)
    tile=rgb.resize((192,326),Image.Resampling.LANCZOS);tiles.append(tile)
    contact.paste(tile,(i*192,0));ImageDraw.Draw(contact).text((i*192+80,333),str(i),fill='white')
    detail.paste(rgb.crop((50,50,270,410)),(i*220,0))
contact.save(out/'transition-test-game-scale.png')
detail.save(out/'transition-test-detail.png')
tiles[0].save(out/'transition-test.gif',save_all=True,append_images=tiles[1:],duration=[350]+[125]*7+[350],loop=0,disposal=2)
print('Prepared review only: 2 endpoints and 7 authored drawings. No Assets modified.')
