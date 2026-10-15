# Developer map: Archived fixed-body/arm compositing proof; writes review images only and does not install ChibiPlayer.
# See Docs/DEVELOPER_HANDOFF.md for system flow and validation.
"""Review-only manual cel assembly. No deformation, interpolation or installation.

Independently drawn arm pixels are cut along hand-marked boundaries and laid
over the down-key body. Exposed shirt/coat comes from that already drawn key.
"""
from pathlib import Path
import json
import numpy as np
from PIL import Image, ImageDraw

root=Path(__file__).resolve().parents[2]
review=root/'ReviewCaptures/PlayerIdle/CoherentKeys'
out=review/'FixedBodyProof';out.mkdir(exist_ok=True)
sources=[Image.open(review/'ProofSources'/f'pose-{i}.png').convert('RGBA') for i in range(9)]
# Interior boundaries follow sleeve/skin/glove; outer alpha is retained.
upper=[(40,105),(109,105),(114,143),(109,179),(106,197)]
tails=[
 [(111,208),(132,207),(140,220),(140,231),(129,242),(112,244),(91,235),(40,235)],
 [(118,215),(140,222),(145,235),(141,247),(130,254),(108,245),(40,244)],
 [(119,219),(143,231),(148,245),(139,258),(125,264),(105,252),(40,253)],
 [(120,228),(142,244),(146,254),(136,269),(119,276),(102,263),(40,263)],
 [(116,226),(113,243),(121,257),(128,280),(124,292),(111,298),(96,291),(40,287)],
 [(110,226),(106,250),(111,271),(117,298),(111,312),(95,316),(83,306),(40,306)],
 [(110,226),(106,250),(111,271),(117,298),(111,312),(95,316),(83,306),(40,306)],
 [(110,226),(106,250),(111,271),(117,298),(111,312),(95,316),(83,306),(40,306)],
 [(101,214),(96,238),(92,252),(95,277),(98,293),(90,309),(76,310),(40,300)]
]
def mask(points):
    m=Image.new('L',(320,544));ImageDraw.Draw(m).polygon(points,fill=255);return m
masks=[mask(upper+t) for t in tails]
base=np.array(sources[8]);base[:,:,3]=np.where(np.array(masks[8])>0,0,base[:,:,3])
base=Image.fromarray(base);base.save(out/'body-plate.png')
frames=[]
for i,(source,m) in enumerate(zip(sources,masks)):
    a=np.array(source);a[:,:,3]=np.minimum(a[:,:,3],np.array(m))
    arm=Image.fromarray(a);arm.save(out/f'arm-{i}.png')
    frame=Image.alpha_composite(base,arm);frame.save(out/f'proof-{i}.png');frames.append(frame)
contact=Image.new('RGB',(9*192,350),(104,134,113));detail=Image.new('RGB',(9*170,240),(104,134,113));tiles=[]
for i,f in enumerate(frames):
    rgb=Image.new('RGB',f.size,(104,134,113));rgb.paste(f,(0,0),f)
    tile=rgb.resize((192,326),Image.Resampling.LANCZOS);tiles.append(tile)
    contact.paste(tile,(i*192,0));ImageDraw.Draw(contact).text((i*192+80,333),str(i),fill='white')
    detail.paste(rgb.crop((35,90,205,330)),(i*170,0))
contact.save(out/'proof-game-scale.png');detail.save(out/'proof-arm-detail.png')
tiles[0].save(out/'proof-transition.gif',save_all=True,append_images=tiles[1:],duration=[350]+[125]*7+[350],loop=0,disposal=2)
arrays=[np.array(f) for f in frames]
locked=np.ones((544,320),bool);locked[105:320,:150]=False
assert all(np.array_equal(a[locked],arrays[0][locked]) for a in arrays)
assert len({a.tobytes() for a in arrays})==9
(out/'checks.json').write_text(json.dumps({'frames':9,'pixel_identical_outside_arm_region':True,'distinct_arm_drawings':9,'operations':'manual mask cutting and alpha composition only; no warp/morph/dissolve','installed':False},indent=2))
print(str(out))
