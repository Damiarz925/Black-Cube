# Developer map: Archived tall-player idle refinement experiment and review output; not part of the current authored chibi pipeline.
# See Docs/DEVELOPER_HANDOFF.md for system flow and validation.
"""Build a consistent four-second idle from one approved, cleaned source pose.

Uses deterministic subpixel deformation, premultiplied alpha, and fixed boots.
Run prepare_player_idle.py first when rebuilding from the original source sheet.
"""
from pathlib import Path
import json
import numpy as np
from PIL import Image, ImageFilter
from prepare_player_idle import flood

ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'Assets/Art/PaperBattle/PlayerIdle'
REVIEW=ROOT/'ReviewCaptures/PlayerIdle'
MASTER=REVIEW/'idle-master.png'
COUNT=32


def main():
    if not MASTER.exists():
        source=Image.open(OUT/'PlayerIdle1.png').convert('RGBA')
        rgba=np.array(source)
        mask=rgba[:,:,3]>127
        h,w=mask.shape
        seeds=[(x,0) for x in range(w)]+[(x,h-1) for x in range(w)]
        seeds += [(0,y) for y in range(h)]+[(w-1,y) for y in range(h)]
        # Preserve exterior gaps; fill enclosed accidental transparency in clothing.
        mask=~flood(~mask,seeds)
        alpha=Image.fromarray(np.uint8(mask)*255).filter(ImageFilter.GaussianBlur(.35))
        rgba[:,:,3]=np.array(alpha)
        rgba[rgba[:,:,3]==0,:3]=0
        Image.fromarray(rgba).save(MASTER)
    base=np.array(Image.open(MASTER).convert('RGBA')).astype(np.float64)/255
    h,w,_=base.shape
    yy,xx=np.mgrid[:h,:w].astype(float)
    # Lower legs and boots are exactly static; smooth weight ramp above the knees.
    weight=np.clip((440-yy)/300,0,1)
    weight=weight*weight*(3-2*weight)
    base[:,:,:3]*=base[:,:,3:4]
    frames=[]
    for i in range(COUNT):
        phase=2*np.pi*i/COUNT
        breath=(1-np.cos(phase))/2
        # Up to two pixels of breathing lift and less than one pixel of sway.
        sy=yy+2.0*breath*weight
        chest=np.exp(-((yy-210)/100)**2)
        sx=xx-.65*np.sin(phase)*weight-(xx-125)*.0025*breath*chest
        x0=np.floor(sx).astype(int);y0=np.floor(sy).astype(int)
        fx=(sx-x0)[:,:,None];fy=(sy-y0)[:,:,None]
        x0=np.clip(x0,0,w-2);y0=np.clip(y0,0,h-2)
        sampled=(base[y0,x0]*(1-fx)*(1-fy)+base[y0,x0+1]*fx*(1-fy)
                 +base[y0+1,x0]*(1-fx)*fy+base[y0+1,x0+1]*fx*fy)
        alpha=sampled[:,:,3:4]
        sampled[:,:,:3]=np.divide(sampled[:,:,:3],alpha,out=np.zeros_like(sampled[:,:,:3]),where=alpha>0)
        result=np.uint8(np.clip(np.rint(sampled*255),0,255))
        result[result[:,:,3]==0,:3]=0
        frame=Image.fromarray(result)
        frame.save(OUT/f'PlayerIdle{i+1}.png')
        frames.append(frame)
    # One shared GIF palette avoids quantization-induced color flicker.
    contact=Image.new('RGB',(256*8,544),(104,134,113))
    for i in range(8):contact.paste(frames[i*4],(i*256,0),frames[i*4])
    contact.save(REVIEW/'idle-refined-contact-sheet.png')
    palette=contact.quantize(colors=255)
    preview=[]
    for frame in frames:
        tile=Image.new('RGB',frame.size,(104,134,113));tile.paste(frame,(0,0),frame)
        preview.append(tile.quantize(palette=palette,dither=Image.Dither.NONE))
    preview[0].save(REVIEW/'idle-refined-preview.gif',save_all=True,append_images=preview[1:],
                    duration=[120,130]*16,loop=0,optimize=False,disposal=2)
    # Static contrast review for gaps and edges, independent of GIF palette.
    review=Image.new('RGB',(256*3,544))
    for i,color in enumerate(((245,240,230),(104,134,113),(35,37,40))):
        tile=Image.new('RGB',(256,544),color);tile.paste(frames[0],(0,0),frames[0]);review.paste(tile,(i*256,0))
    review.save(REVIEW/'idle-edge-review.png')
    arrays=[np.array(f) for f in frames]
    assert all(np.array_equal(a[440:],arrays[0][440:]) for a in arrays)
    assert all(not a[0,:,3].any() and not a[-1,:,3].any() and not a[:,0,3].any() and not a[:,-1,3].any() for a in arrays)
    differences=[]
    for i,a in enumerate(arrays):
        # Composite comparisons avoid invisible RGB dominating the metric.
        alpha=a[:,:,3:4]/255
        b=arrays[(i+1)%COUNT];ab=b[:,:,3:4]/255
        differences.append(float(np.abs(a[:,:,:3]*alpha-b[:,:,:3]*ab).mean()))
    assert differences[-1] <= max(differences[:-1])*1.1
    report={'frames':COUNT,'frames_per_second':8,'cycle_seconds':4,
            'method':'single cleaned source pose, deterministic subpixel breathing and sway',
            'fixed_feet_rows':[440,543],'max_breath_lift_pixels':2,'max_sway_pixels':.65,
            'adjacent_composited_mean_differences':differences,'checks':'fixed feet, transparent unclipped margins, loop seam continuity passed'}
    (REVIEW/'idle-refinement.json').write_text(json.dumps(report,indent=2))
    print(json.dumps(report,indent=2))


if __name__=='__main__':main()
