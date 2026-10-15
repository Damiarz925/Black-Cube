# Developer map: Legacy tall-player authored idle extraction and repeated-frame installation; also provides the runs helper imported by current preparation.
# See Docs/DEVELOPER_HANDOFF.md for system flow and validation.
"""Cut out, uniformly scale and translate eight independently drawn idle poses.

No mesh deformation, motion synthesis, morphing, crossfades or pose interpolation.
Each authored pose occupies four existing runtime slots (0.5s at 8fps).
"""
from pathlib import Path
import json
import numpy as np
from PIL import Image, ImageFilter, ImageDraw
from prepare_player_idle import flood

ROOT=Path(__file__).resolve().parents[2]
REVIEW=ROOT/'ReviewCaptures/PlayerIdle'
OUT=ROOT/'Assets/Art/PaperBattle/PlayerIdle'
ORDER=[1,4,3,2,7,6,8,5]


def runs(values):
    padded=np.r_[False,values,False].astype(int)
    return list(zip(np.where(np.diff(padded)==1)[0],np.where(np.diff(padded)==-1)[0]))


def main():
    sheet=Image.open(REVIEW/'authored-idle-source.png').convert('RGB')
    rgb=np.array(sheet)
    ink=rgb.min(axis=2)<160
    body=max(runs(ink.sum(axis=1)>40),key=lambda r:r[1]-r[0])
    top,bottom=max(0,int(body[0])-5),min(sheet.height,int(body[1])+4)
    ranges=[(int(a),int(b)) for a,b in runs(ink[top:bottom].sum(axis=0)>6) if b-a>100]
    assert len(ranges)==8,ranges
    scale=516/(bottom-top-9)
    poses=[];records=[]
    for index,(a,b) in enumerate(ranges,1):
        left=max(0,a-4);right=min(sheet.width,b+4)
        crop=sheet.crop((left,top,right,bottom));pixels=np.array(crop)
        h,w,_=pixels.shape
        allowed=pixels.min(axis=2)>210
        seeds=[(x,0) for x in range(w)]+[(x,h-1) for x in range(w)]
        seeds += [(0,y) for y in range(h)]+[(w-1,y) for y in range(h)]
        mask=~flood(allowed,seeds)
        mask=flood(mask,[(w//2,h//2)]) & ~allowed
        alpha=Image.fromarray(np.uint8(mask)*255).filter(ImageFilter.MinFilter(3)).filter(ImageFilter.GaussianBlur(.35))
        crop=crop.convert('RGBA');crop.putalpha(alpha)
        crop=crop.resize((round(w*scale),round(h*scale)),Image.Resampling.LANCZOS)
        arr=np.array(crop); solid=arr[:,:,3]>127
        ys,xs=np.where(solid);ground=int(ys.max())
        foot_x=np.where(solid[max(0,ground-25):ground+1].any(axis=0))[0]
        center=round((int(foot_x.min())+int(foot_x.max()))/2)
        canvas=Image.new('RGBA',(256,544));canvas.paste(crop,(128-center,532-ground))
        arr=np.array(canvas);arr[arr[:,:,3]==0,:3]=0;canvas=Image.fromarray(arr)
        canvas.save(REVIEW/f'authored-pose-{index}.png')
        poses.append(canvas)
        records.append({'source_pose':index,'source_rect':[left,top,right,bottom],
                        'uniform_scale':scale,'translation':[128-center,532-ground]})
    ordered=[poses[i-1] for i in ORDER]
    for i in range(32): ordered[i//4].save(OUT/f'PlayerIdle{i+1}.png')
    contact=Image.new('RGB',(8*256,574),(104,134,113));draw=ImageDraw.Draw(contact)
    for i,frame in enumerate(ordered):
        contact.paste(frame,(i*256,0),frame);draw.text((i*256+110,552),str(i+1),fill='white')
    contact.save(REVIEW/'authored-idle-contact-sheet.png')
    # Hand/coat details in source order make newly uncovered fabric inspectable.
    detail=Image.new('RGB',(8*220,250),(104,134,113))
    for i,frame in enumerate(ordered):
        tile=Image.new('RGB',(256,544),(104,134,113));tile.paste(frame,(0,0),frame)
        detail.paste(tile.crop((25,140,135,265)).resize((220,250)),(i*220,0))
    detail.save(REVIEW/'authored-hand-coat-review.png')
    palette=contact.quantize(colors=255)
    large=[];small=[]
    for frame in ordered:
        tile=Image.new('RGB',frame.size,(104,134,113));tile.paste(frame,(0,0),frame)
        large.append(tile.quantize(palette=palette,dither=Image.Dither.NONE))
        small.append(tile.resize((154,326),Image.Resampling.LANCZOS).quantize(palette=palette,dither=Image.Dither.NONE))
    for name,frames in [('authored-idle-preview.gif',large),('authored-idle-game-scale.gif',small)]:
        frames[0].save(REVIEW/name,save_all=True,append_images=frames[1:],duration=500,loop=0,optimize=False,disposal=2)
    arrays=[np.array(f) for f in ordered]
    assert all(not a[0,:,3].any() and not a[-1,:,3].any() and not a[:,0,3].any() and not a[:,-1,3].any() for a in arrays)
    assert all(int(np.where(a[:,:,3]>127)[0].max())==532 for a in arrays)
    assert len({a.tobytes() for a in arrays})==8
    report={'source_order':ORDER,'authored_poses':8,'seconds_per_pose':.5,'cycle_seconds':4,
            'runtime_slots':32,'identical_hold_slots_per_pose':4,'pose_synthesis':'NONE',
            'processing':'background extraction, single uniform sheet-wide scale, translation/foot alignment only',
            'records':records}
    (REVIEW/'authored-idle-checks.json').write_text(json.dumps(report,indent=2))
    print(json.dumps(report,indent=2))


if __name__=='__main__':main()
