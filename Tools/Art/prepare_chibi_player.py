# Developer map: Current body extraction/alpha cleanup/registration and full-sword preparation. Dense bands locate figures, empty gutters define cells, and complete alpha bounds preserve hair tips.
# See Docs/DEVELOPER_HANDOFF.md for system flow and validation.
"""Build new weapon-free chibi cels and a separate complete equipment sprite.
Generated sheets are cleaned/aligned; no motion morphing or body deformation.
"""
from pathlib import Path
import json,hashlib,re,shutil,uuid
import numpy as np
from PIL import Image,ImageDraw,ImageFilter
from install_authored_idle import runs
from prepare_player_idle import flood

root=Path(__file__).resolve().parents[2]
review=root/'ReviewCaptures/ChibiPlayer';review.mkdir(exist_ok=True)
art=root/'Assets/Art/PaperBattle/ChibiPlayer';art.mkdir(exist_ok=True)
generated=Path('C:/Users/david/.codex/generated_images/01a07d34-23f8-7d30-817e-5747e52a1b17')
sources={'Idle':'exec-9a26c489-b242-4c59-bed9-7522caaf1143.png','Attack':'exec-cef4d2b2-038e-4f3e-9e70-371b70db3efd.png','Sword':'exec-1afd3bde-5b44-46b7-be0f-3d43c1233635.png'}
for key,name in sources.items():shutil.copyfile(generated/name,review/f'{key}-generated.png')
for name,source in [('Idle-reference.png','D:/Downloads/ChatGPT Image Sep 7, 2026, 06_10_32 PM.png'),('Attack-reference.png','D:/Downloads/ChatGPT Image Sep 7, 2026, 06_08_07 PM.png')]:
    if not (review/name).exists():shutil.copyfile(source,review/name)

protected=list((root/'Assets/Art/PaperBattle/ForestCycle').glob('*'))+[root/'Assets/Scripts/BattleManager.cs',root/'Assets/Scripts/ZoneManager.cs']
if not (review/'protected-hashes.json').exists():
    (review/'protected-hashes.json').write_text(json.dumps({str(p.relative_to(root)):hashlib.sha256(p.read_bytes()).hexdigest() for p in protected if p.is_file()},indent=2))

def clean(im):
    im=im.convert('RGBA');a=np.array(im)
    if a[:,:,3].min()==255:
        # Neutral white/checker backdrop, including enclosed gaps. Skin is warm,
        # and all costume material is much darker than this keyed backdrop.
        rgb=a[:,:,:3].astype(int);background=(rgb.min(2)>205)&(rgb.max(2)-rgb.min(2)<30)
        alpha=Image.fromarray(np.uint8(~background)*255).filter(ImageFilter.MinFilter(3))
        a[:,:,3]=np.array(alpha)
    a[a[:,:,3]==0,:3]=0
    return Image.fromarray(a)

records={};allframes={}
for kind in ['Idle','Attack']:
    sheet=clean(Image.open(review/f'{kind}-generated.png'));a=np.array(sheet);ink=a[:,:,3]>127
    bands=[(int(y),int(z)) for y,z in runs(ink.sum(1)>40) if z-y>100];assert len(bands)==2,bands
    cells=[]
    for band_index,(top,bottom) in enumerate(bands):
        columns=[(int(x),int(y)) for x,y in runs(ink[top:bottom].sum(0)>5) if y-x>80];assert len(columns)==4,columns
        # Dense rows/columns locate figures, but are NOT crop edges: sparse hair
        # tips contain fewer pixels and used to be sliced flat by those thresholds.
        # Split only in the empty gutters, then retain the complete alpha bounds.
        cell_top=0 if band_index==0 else (bands[band_index-1][1]+top)//2
        cell_bottom=sheet.height if band_index==len(bands)-1 else (bottom+bands[band_index+1][0])//2
        for col_index,(left,right) in enumerate(columns):
            cell_left=0 if col_index==0 else (columns[col_index-1][1]+left)//2
            cell_right=sheet.width if col_index==len(columns)-1 else (right+columns[col_index+1][0])//2
            bounds=sheet.crop((cell_left,cell_top,cell_right,cell_bottom)).getbbox()
            assert bounds
            x0,y0,x3,y3=bounds
            cells.append((max(cell_left,cell_left+x0-3),max(cell_top,cell_top+y0-3),min(cell_right,cell_left+x3+3),min(cell_bottom,cell_top+y3+3)))
    # Standing height of pose1 defines one common scale for the entire action.
    # Do not scale individual crouches back to standing height.
    x1,y1,x2,y2=cells[0];solid=ink[y1:y2,x1:x2];ys,xs=np.where(solid);scale=516/(int(ys.max())-int(ys.min())+1)
    frames=[];recs=[]
    for i,(left,top,right,bottom) in enumerate(cells):
        crop=sheet.crop((left,top,right,bottom));solid=np.array(crop)[:,:,3]>127;ys,xs=np.where(solid);ground=int(ys.max())
        if kind=='Idle' or (kind=='Attack' and i in [6,7]):scale=516/(int(ys.max())-int(ys.min())+1)
        feet=np.where(solid[max(0,ground-12):ground+1].any(0))[0];center=(int(feet.min())+int(feet.max()))/2
        crop=crop.resize((round(crop.width*scale),round(crop.height*scale)),Image.Resampling.LANCZOS)
        solid=np.array(crop)[:,:,3]>127;yy,xx=np.where(solid);ground_resized=int(yy.max())
        feet_resized=np.where(solid[max(0,ground_resized-16):ground_resized+1].any(0))[0]
        center_resized=(int(feet_resized.min())+int(feet_resized.max()))/2
        offset=(round(320-center_resized),612-ground_resized);frame=Image.new('RGBA',(640,640));frame.paste(crop,offset)
        a=np.array(frame);a[a[:,:,3]==0,:3]=0;frame=Image.fromarray(a);frames.append(frame);frame.save(art/f'{kind}{i+1}.png')
        recs.append({'source_rect':[left,top,right,bottom],'scale':scale,'offset':offset})
    records[kind]=recs;allframes[kind]=frames
    contact=Image.new('RGB',(4*320,2*344),(104,134,113))
    for i,f in enumerate(frames):
        bg=Image.new('RGB',f.size,(104,134,113));bg.paste(f,(0,0),f);x=(i%4)*320;y=(i//4)*344;contact.paste(bg.resize((320,320)),(x,y));ImageDraw.Draw(contact).text((x+150,y+326),str(i+1),fill='white')
    contact.save(review/f'{kind.lower()}-body-contact.png')

sword=clean(Image.open(review/'Sword-generated.png'));a=np.array(sword)
main=flood(a[:,:,3]>16,[(sword.width//2,sword.height//2)])
main=np.array(Image.fromarray(np.uint8(main)*255).filter(ImageFilter.MaxFilter(3)))>0
a[~main,3]=0;sword=Image.fromarray(a);box=sword.getbbox();sword=sword.crop(box)
ratio=280/sword.width;sword=sword.resize((280,round(sword.height*ratio)),Image.Resampling.LANCZOS)
canvas=Image.new('RGBA',(320,128));canvas.paste(sword,(20,(128-sword.height)//2));canvas.save(art/'Sword.png')
records['Sword']={'source_bbox':box,'resize_ratio':ratio,'canvas':[320,128]}
(review/'preparation.json').write_text(json.dumps(records,indent=2))
print(json.dumps({'source_modes':{k:Image.open(review/f'{k}-generated.png').mode for k in sources},'canvas':[640,640],'idle_cels':8,'attack_cels':8,'sword':str(art/'Sword.png')}))
