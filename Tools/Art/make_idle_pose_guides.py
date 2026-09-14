# Developer map: Archived tall-player idle pose guides for authored-image generation; outputs review guides rather than current runtime art.
# See Docs/DEVELOPER_HANDOFF.md for system flow and validation.
"""Draw schematic pose targets for imagegen; these are not animation assets."""
from pathlib import Path
from PIL import Image,ImageDraw
ROOT=Path(__file__).resolve().parents[2]/'ReviewCaptures/PlayerIdle/Inbetweens'
# Approximate right-fist positions in accepted 256x544 anchor coordinate space.
hands=[(90,200),(96,204),(96,230),(51,274),(48,271),(91,218),(84,208),(88,202)]
for i in [0,1,4,5,6,7]:
    im=Image.new('RGB',(960,610),'white');d=ImageDraw.Draw(im)
    a=hands[i];b=hands[(i+1)%8]
    for j in range(3):
        t=(j+1)/4;ox=j*320+32
        hx=a[0]*(1-t)+b[0]*t;hy=a[1]*(1-t)+b[1]*t
        points={'head':(130,55),'neck':(128,108),'shoulder':(82,131),
                'elbow':(59,190+(hy-200)*.34),'hand':(hx,hy),
                'pelvis':(128,278),'knee1':(67,379),'knee2':(194,379),
                'ankle1':(58,499),'ankle2':(209,499),'other_shoulder':(177,132),
                'other_elbow':(205,207),'other_hand':(201,258)}
        for u,v in [('head','neck'),('neck','shoulder'),('shoulder','elbow'),('elbow','hand'),
                    ('neck','pelvis'),('pelvis','knee1'),('knee1','ankle1'),('pelvis','knee2'),('knee2','ankle2'),
                    ('neck','other_shoulder'),('other_shoulder','other_elbow'),('other_elbow','other_hand')]:
            x,y=points[u];x2,y2=points[v];d.line((ox+x,y+25,ox+x2,y2+25),fill=(175,175,175),width=3)
        d.ellipse((ox+hx-7,hy+18,ox+hx+7,hy+32),fill=(30,110,220))
        d.line((ox+15,557,ox+245,557),fill=(180,180,180),width=2)
        d.text((ox+93,570),f'{"ABC"[j]}: right fist ({hx:.0f},{hy:.0f})',fill='black')
    d.text((12,4),'POSE TARGETS ONLY - draw complete character; omit guide lines and blue dots',fill='black')
    im.save(ROOT/f'pose-guide-{i+1}.png')
