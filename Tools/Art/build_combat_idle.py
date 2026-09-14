# Developer map: Archived synthetic idle experiment that writes PlayerIdle art using sampled deformations. Not approved for the current authored chibi animation.
# See Docs/DEVELOPER_HANDOFF.md for system flow and validation.
"""Articulate one consistent ready-guard drawing into a slow weight-shift cycle.

Independent hip, shoulder, knee, elbow, wrist and head displacements create guard
poses while retaining one costume/face texture and completely planted boots.
"""
from pathlib import Path
import json
import numpy as np
from PIL import Image, ImageFilter
from prepare_player_idle import flood

ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'Assets/Art/PaperBattle/PlayerIdle'
REVIEW=ROOT/'ReviewCaptures/PlayerIdle'
MASTER=REVIEW/'combat-idle-master.png'
SOURCE=REVIEW/'combat-ready-source-crop.png'


def sample(base,sx,sy):
    h,w,_=base.shape
    x0=np.floor(sx).astype(int); y0=np.floor(sy).astype(int)
    fx=(sx-x0)[:,:,None]; fy=(sy-y0)[:,:,None]
    x0=np.clip(x0,0,w-2);y0=np.clip(y0,0,h-2)
    a=(base[y0,x0]*(1-fx)*(1-fy)+base[y0,x0+1]*fx*(1-fy)
       +base[y0+1,x0]*(1-fx)*fy+base[y0+1,x0+1]*fx*fy)
    alpha=a[:,:,3:4]
    a[:,:,:3]=np.divide(a[:,:,:3],alpha,out=np.zeros_like(a[:,:,:3]),where=alpha>0)
    a=np.uint8(np.clip(np.rint(a*255),0,255));a[a[:,:,3]==0,:3]=0
    return Image.fromarray(a)


def main():
    if SOURCE.exists():
        source=Image.open(SOURCE).convert('RGB');rgb=np.array(source)
        h,w,_=rgb.shape
        allowed=rgb.min(axis=2)>210
        seeds=[(x,0) for x in range(w)]+[(x,h-1) for x in range(w)]
        seeds += [(0,y) for y in range(h)]+[(w-1,y) for y in range(h)]
        mask=~flood(allowed,seeds)
        mask=flood(mask,[(w//2,h//2)])
        # White enclosed between hair strands is backdrop too, not a hair highlight.
        mask &= ~allowed
        alpha=Image.fromarray(np.uint8(mask)*255).filter(ImageFilter.MinFilter(3)).filter(ImageFilter.GaussianBlur(.4))
        source=source.convert('RGBA');source.putalpha(alpha)
        bbox=alpha.getbbox();source=source.crop(bbox)
        source=source.resize((round(source.width*520/source.height),520),Image.Resampling.LANCZOS)
        canvas=Image.new('RGBA',(256,544));canvas.paste(source,((256-source.width)//2,13))
        a=np.array(canvas);a[a[:,:,3]==0,:3]=0;Image.fromarray(a).save(MASTER)
    base=np.array(Image.open(MASTER).convert('RGBA')).astype(float)/255
    base[:,:,:3]*=base[:,:,3:4]
    h,w,_=base.shape; yy,xx=np.mgrid[:h,:w].astype(float)
    # Skinning envelope is strongest through hips/chest and vanishes at the boots.
    vertical=np.interp(yy[:,0],[0,75,145,235,310,380,445,544],[.85,.9,1,1,.85,.45,0,0])[:,None]
    def region(cx,cy,rx,ry): return np.exp(-((xx-cx)/rx)**2-((yy-cy)/ry)**2)
    frames=[]
    for i in range(32):
        phase=2*np.pi*i/32; shift=np.sin(phase); settle=np.sin(phase)**2
        dx=15*shift*vertical*np.ones_like(xx)
        dy=2.5*settle*vertical*np.ones_like(xx)
        # Ready arms counter-adjust independently of hip/shoulder translation.
        dx-=4*shift*region(70,185,48,70)
        dy-=4*shift*region(81,192,38,48)
        dx+=3*shift*region(204,232,30,65)
        dy+=3*shift*region(204,232,30,48)
        # Alternating knee flexion transfers weight without moving either boot.
        dx+=3*shift*region(70,370,35,50)-3*shift*region(190,370,35,50)
        dy+=2.5*shift*region(70,370,35,50)-2.5*shift*region(190,370,35,50)
        # Small head counter-rotation keeps gaze steady as the torso leans.
        angle=.018*shift;head=region(134,58,65,72)
        dx+=angle*(yy-105)*head;dy-=angle*(xx-133)*head
        dx[445:]=0;dy[445:]=0
        frame=sample(base,xx-dx,yy-dy)
        frame.save(OUT/f'PlayerIdle{i+1}.png');frames.append(frame)
    contact=Image.new('RGB',(256*8,544),(104,134,113))
    for i in range(8):contact.paste(frames[i*4],(i*256,0),frames[i*4])
    contact.save(REVIEW/'combat-idle-key-poses.png')
    palette=contact.quantize(colors=255)
    big=[];small=[]
    for frame in frames:
        tile=Image.new('RGB',frame.size,(104,134,113));tile.paste(frame,(0,0),frame)
        big.append(tile.quantize(palette=palette,dither=Image.Dither.NONE))
        # Camera ortho height 10 units, 150 PPU, representative 900px viewport:
        # source pixel -> 900/(10*150) = 0.6 display pixels.
        small.append(tile.resize((154,326),Image.Resampling.LANCZOS).quantize(palette=palette,dither=Image.Dither.NONE))
    for name,sequence in [('combat-idle-preview.gif',big),('combat-idle-game-scale.gif',small)]:
        sequence[0].save(REVIEW/name,save_all=True,append_images=sequence[1:],duration=[120,130]*16,
                         loop=0,optimize=False,disposal=2)
    arrays=[np.array(f) for f in frames]
    assert all(np.array_equal(a[445:],arrays[0][445:]) for a in arrays)
    assert all(not a[0,:,3].any() and not a[-1,:,3].any() and not a[:,0,3].any() and not a[:,-1,3].any() for a in arrays)
    diff=[]
    for i,a0 in enumerate(arrays):
        a=a0.astype(float);b=arrays[(i+1)%32].astype(float)
        diff.append(float(np.abs(a[:,:,:3]*a[:,:,3:4]/255-b[:,:,:3]*b[:,:,3:4]/255).mean()))
    assert diff[-1] <= max(diff[:-1])*1.1
    report={'frames':32,'fps':8,'cycle_seconds':4,'upper_body_sway_pixels':15,
            'approx_900px_viewport_sway_each_side':9,'feet_fixed_from_row':445,
            'independent_articulation':'hips, shoulders, elbows/wrists, knees, counterbalancing head',
            'adjacent_difference':diff,'method':'generated ready-guard drawing with coherent deterministic articulated in-betweens'}
    (REVIEW/'combat-idle-checks.json').write_text(json.dumps(report,indent=2))
    print(json.dumps(report,indent=2))


if __name__=='__main__':main()
