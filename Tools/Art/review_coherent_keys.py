# Developer map: Archived tall-player key-pose registration, contact sheets and detail previews.
# See Docs/DEVELOPER_HANDOFF.md for system flow and validation.
from pathlib import Path
from PIL import Image, ImageDraw
import numpy as np

root=Path(__file__).resolve().parents[2]
out=root/'ReviewCaptures/PlayerIdle/CoherentKeys'
sheet=Image.open(out/'key-sheet-arm-corrected.png').convert('RGB')
frames=[]
for i in range(4):
    cell=sheet.crop((round(i*sheet.width/4)+5,5,round((i+1)*sheet.width/4)-5,sheet.height-5))
    cell.save(out/f'key-{i+1}.png')
    frames.append(cell.resize((163,326),Image.Resampling.LANCZOS))
contact=Image.new('RGB',(652,350),'white')
for i,f in enumerate(frames):
    contact.paste(f,(i*163,0))
    ImageDraw.Draw(contact).text((i*163+70,333),str(i+1),fill='black')
contact.save(out/'keys-game-scale.png')
frames[0].save(out/'keys-registration-review.gif',save_all=True,append_images=frames[1:],duration=600,loop=0)
detail=Image.new('RGB',(sheet.width,430),'white')
detail.paste(sheet.crop((0,30,sheet.width,230)),(0,0))
detail.paste(sheet.crop((0,280,sheet.width,510)),(0,200))
detail.save(out/'keys-face-hand-detail.png')
print(str(out))
