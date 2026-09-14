# Developer map: Legacy diagnostic masks for unwanted gray pixels in PlayerAttack; review output only.
# See Docs/DEVELOPER_HANDOFF.md for system flow and validation.
from pathlib import Path
from collections import deque
import json
import numpy as np
from PIL import Image,ImageDraw
root=Path(__file__).resolve().parents[2]
out=root/'ReviewCaptures/PlayerAttack/ForwardStrikeFix'
contact=Image.new('RGB',(4*320,2*350),'white');records={}
for i in range(1,9):
    f=Image.open(out/'Before'/f'PlayerAttack{i}.png').convert('RGBA');a=np.array(f).astype(int)
    eligible=(a[:,:,3]>180)&((a[:,:,:3].max(2)-a[:,:,:3].min(2))<9)&(a[:,:,:3].max(2)>=64)&(a[:,:,:3].max(2)<=150)
    seen=np.zeros(eligible.shape,bool);groups=[]
    for y,x in zip(*np.where(eligible)):
        if seen[y,x]:continue
        q=deque([(x,y)]);seen[y,x]=True;points=[]
        while q:
            xx,yy=q.popleft();points.append((xx,yy))
            for nx,ny in [(xx-1,yy),(xx+1,yy),(xx,yy-1),(xx,yy+1)]:
                if 0<=nx<640 and 0<=ny<640 and eligible[ny,nx] and not seen[ny,nx]:seen[ny,nx]=True;q.append((nx,ny))
        if len(points)<30:continue
        xs,ys=zip(*points);groups.append({'area':len(points),'box':[min(xs),min(ys),max(xs),max(ys)],'seed':points[len(points)//2]})
    groups.sort(key=lambda z:-z['area']);records[i]=groups
    rgb=Image.new('RGB',(640,640),(255,70,200));rgb.paste(f,(0,0),f);d=ImageDraw.Draw(rgb)
    for j,g in enumerate(groups):d.rectangle(g['box'],outline=(0,255,0),width=2);d.text(g['box'][:2],str(j),fill=(255,255,0))
    tile=rgb.resize((320,320));x=((i-1)%4)*320;y=((i-1)//4)*350;contact.paste(tile,(x,y));ImageDraw.Draw(contact).text((x+145,y+328),str(i),fill='black')
contact.save(out/'gray-candidates.png');(out/'gray-candidates.json').write_text(json.dumps(records,indent=2,default=int))
print(json.dumps(records,default=int))
