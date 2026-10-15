# Developer map: Legacy tall-player inbetween extraction/order/alignment and previews; inspect output option before running. Not the current eight-cel player pipeline.
# See Docs/DEVELOPER_HANDOFF.md for system flow and validation.
"""Assemble 8 untouched approved anchors + 24 independently imagegen-drawn poses.

Only cutout, uniform resize, and translation alignment are performed. No image
deformation, generated motion, crossfade, interpolation, or duplicate hold frames.
"""
from pathlib import Path
import json
import hashlib
import numpy as np
from PIL import Image, ImageFilter, ImageDraw
from prepare_player_idle import flood
from install_authored_idle import runs

ROOT=Path(__file__).resolve().parents[2]
REVIEW=ROOT/'ReviewCaptures/PlayerIdle'
OUT=ROOT/'Assets/Art/PaperBattle/PlayerIdle'


def bounds(frame):
    y,x=np.where(np.array(frame)[:,:,3]>127)
    return int(x.min()),int(y.min()),int(x.max()+1),int(y.max()+1)


def extract(path):
    sheet=Image.open(path).convert('RGB');rgb=np.array(sheet)
    ink=rgb.min(axis=2)<160
    top,bottom=max(runs(ink.sum(axis=1)>25),key=lambda v:v[1]-v[0])
    top=max(0,int(top)-4);bottom=min(sheet.height,int(bottom)+4)
    columns=[(int(a),int(b)) for a,b in runs(ink[top:bottom].sum(axis=0)>5) if b-a>70]
    if len(columns)!=3: raise RuntimeError(f'{path}: expected3 characters, found {columns}')
    results=[]
    for a,b in columns:
        left=max(0,a-4);right=min(sheet.width,b+4)
        crop=sheet.crop((left,top,right,bottom));rgb=np.array(crop)
        h,w,_=rgb.shape;white=rgb.min(axis=2)>210
        seeds=[(x,0) for x in range(w)]+[(x,h-1) for x in range(w)]
        seeds += [(0,y) for y in range(h)]+[(w-1,y) for y in range(h)]
        mask=~flood(white,seeds)
        mask=flood(mask,[(w//2,h//2)]) & ~white
        alpha=Image.fromarray(np.uint8(mask)*255).filter(ImageFilter.MinFilter(3)).filter(ImageFilter.GaussianBlur(.35))
        crop=crop.convert('RGBA');crop.putalpha(alpha)
        results.append((crop,[left,top,right,bottom]))
    return results


def align(frame,target_height):
    box=bounds(frame);scale=target_height/(box[3]-box[1])
    frame=frame.resize((round(frame.width*scale),round(frame.height*scale)),Image.Resampling.LANCZOS)
    a=np.array(frame);solid=a[:,:,3]>127;y,x=np.where(solid);bottom=int(y.max())
    foot=np.where(solid[max(0,bottom-25):bottom+1].any(axis=0))[0]
    center=round((int(foot.min())+int(foot.max()))/2)
    canvas=Image.new('RGBA',(320,544));canvas.paste(frame,(160-center,532-bottom))
    a=np.array(canvas);a[a[:,:,3]==0,:3]=0
    return Image.fromarray(a),{'uniform_scale':scale,'translation':[160-center,532-bottom]}


def order_drawings(frames):
    # Select an order of existing drawings only; never blend or synthesize pixels.
    # Image generation can overshoot a requested intermediate hand position.
    # Minimize visible pose jumps while preserving every fourth accepted anchor.
    features=[]
    yy,xx=np.mgrid[:136,:80]
    weights=np.ones((136,80,1))
    weights[(yy>35)&(yy<83)&(xx>15)&(xx<45)]=3
    for frame in frames:
        a=np.array(frame.resize((80,136),Image.Resampling.LANCZOS)).astype(float)/255
        features.append(np.concatenate((a[:,:,:3]*a[:,:,3:4],a[:,:,3:4]),axis=2)*weights)
    f=np.array(features)
    distances=np.abs(f[:,None]-f[None,:]).mean(axis=(2,3,4))
    slots=[i for i in range(32) if i%4]
    targets=[]
    for i in range(32):
        a=(i//4)*4;b=(a+4)%32;t=(i%4)/4
        target=f[a]*(1-t)+f[b]*t
        targets.append(np.abs(f-target).mean(axis=(1,2,3)))
    targets=np.array(targets)
    def cost(order):
        return sum(distances[order[i],order[(i+1)%32]]+.6*targets[i,order[i]] for i in range(32))
    best=list(range(32));before=cost(best);best_cost=before
    rng=np.random.default_rng(42)
    for attempt in range(12):
        order=best.copy()
        if attempt:
            for _ in range(6):
                a,b=rng.choice(slots,2,replace=False);order[a],order[b]=order[b],order[a]
        current=cost(order)
        while True:
            choice=None;gain=1e-9
            for n,a in enumerate(slots):
                for b in slots[n+1:]:
                    order[a],order[b]=order[b],order[a]
                    candidate=cost(order)
                    order[a],order[b]=order[b],order[a]
                    if current-candidate>gain:gain=current-candidate;choice=(a,b,candidate)
            if choice is None:break
            a,b,current=choice;order[a],order[b]=order[b],order[a]
        if current<best_cost:best=order;best_cost=current
    return [frames[i] for i in best],{'source_frame_order':[i+1 for i in best],
        'pose_jump_score_before':before,'pose_jump_score_after':best_cost,
        'operation':'reordering existing independently drawn images only; no pixel blending'}


def main():
    anchors=[Image.open(REVIEW/'Anchors'/f'IdleAnchor{i}.png').convert('RGBA') for i in range(1,9)]
    prepared=[];records=[]
    for i,anchor in enumerate(anchors):
        padded=Image.new('RGBA',(320,544));padded.paste(anchor,(32,0))
        prepared.append(padded)
        start=bounds(anchor);end=bounds(anchors[(i+1)%8])
        drawings=extract(REVIEW/'Inbetweens'/f'interval-{i+1}.png')
        for j,(drawing,rect) in enumerate(drawings,1):
            t=j/4
            target_height=(start[3]-start[1])*(1-t)+(end[3]-end[1])*t
            aligned,transform=align(drawing,target_height)
            prepared.append(aligned)
            records.append({'interval':i+1,'drawing':j,'source_rect':rect,**transform})
    prepared,ordering=order_drawings(prepared)
    pixels=[np.array(f) for f in prepared]
    assert len({a.tobytes() for a in pixels})==32,'Duplicate poses detected'
    assert all(np.array_equal(pixels[i*4][:,32:288],np.array(anchors[i])) for i in range(8))
    assert all(not a[0,:,3].any() and not a[-1,:,3].any() and not a[:,0,3].any() and not a[:,-1,3].any() for a in pixels),'Clipped margin'
    assert all(int(np.where(a[:,:,3]>127)[0].max())==532 for a in pixels),'Foot baseline changed'
    # Review before installation; this script never writes Assets by itself.
    output=REVIEW/'Prepared32';output.mkdir(exist_ok=True)
    for i,frame in enumerate(prepared,1):frame.save(output/f'PlayerIdle{i}.png')
    contact=Image.new('RGB',(8*320,4*574),(104,134,113));draw=ImageDraw.Draw(contact)
    for i,f in enumerate(prepared):
        x=(i%8)*320;y=(i//8)*574;contact.paste(f,(x,y),f)
        draw.text((x+95,y+552),f'{i+1}'+(' anchor' if i%4==0 else ''),fill='white')
    contact.save(REVIEW/'authored-32-contact-sheet.png')
    palette=contact.quantize(colors=255);large=[];small=[]
    for f in prepared:
        tile=Image.new('RGB',f.size,(104,134,113));tile.paste(f,(0,0),f)
        large.append(tile.quantize(palette=palette,dither=Image.Dither.NONE))
        small.append(tile.resize((192,326),Image.Resampling.LANCZOS).quantize(palette=palette,dither=Image.Dither.NONE))
    for name,frames in [('authored-32-preview.gif',large),('authored-32-game-scale.gif',small)]:
        frames[0].save(REVIEW/name,save_all=True,append_images=frames[1:],duration=[120,130]*16,
                       loop=0,optimize=False,disposal=2)
    report={'frames':32,'accepted_anchors_unchanged':8,'new_independently_drawn_frames':24,
            'cycle_seconds':4,'fps':8,'motion_synthesis':'none','anchor_padding_pixels_each_side':32,'records':records,'ordering':ordering,
            'pixel_hashes':[hashlib.sha256(a.tobytes()).hexdigest() for a in pixels]}
    (REVIEW/'authored-32-checks.json').write_text(json.dumps(report,indent=2))
    print('Prepared32 distinct authored frames for visual review; all8 anchors unchanged, margins and feet baseline passed.')


if __name__=='__main__':main()
