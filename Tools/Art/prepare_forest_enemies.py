# Developer map: Current authored goblin/hobgoblin connected-component extraction, alpha cleanup and uniform species registration. Writes rest/attack PNGs and previews; no motion warping.
# See Docs/DEVELOPER_HANDOFF.md for system flow and validation.
"""Prepare authored goblin/hobgoblin cels, preserving complete silhouettes.
Writes art/review PNGs and GIFs only; install_forest_enemies.py handles Unity wiring.
Connected components allow extended weapons to cross nominal sheet columns.
"""
from pathlib import Path
import json, shutil
import numpy as np
from PIL import Image, ImageDraw, ImageFilter

ROOT=Path(__file__).resolve().parents[2]
REVIEW=ROOT/'ReviewCaptures/ForestEnemies'
ART=ROOT/'Assets/Art/PaperBattle/ForestEnemies'
SOURCES={'Goblin':'exec-f1e23fd2-288c-4b23-b8dc-cf38e374d922.png', 'Hobgoblin':'exec-16bc652d-7e1a-4f5d-b96d-c4a129ae5b88.png'}
GENERATED=Path('C:/Users/david/.codex/generated_images/01a07d34-23f8-7d30-817e-5747e52a1b17')

def components(mask):
    # Row-run union/find avoids a Python queue entry for every sprite pixel.
    parent=[];runs=[];previous=[]
    def find(i):
        while parent[i]!=i:parent[i]=parent[parent[i]];i=parent[i]
        return i
    for y,row in enumerate(mask):
        changes=np.diff(np.pad(row.astype(np.int8),(1,1)))
        current=[]
        for left,right in zip(np.where(changes==1)[0],np.where(changes==-1)[0]):
            idx=len(parent);parent.append(idx);runs.append((y,int(left),int(right)))
            for l,r,j in previous:
                if r>=left and l<=right:parent[find(idx)]=find(j)
            current.append((left,right,idx))
        previous=current
    groups={}
    for i,run in enumerate(runs):groups.setdefault(find(i),[]).append(run)
    return sorted(groups.values(),key=lambda g:sum(r-l for y,l,r in g),reverse=True)

def main():
    REVIEW.mkdir(exist_ok=True);ART.mkdir(exist_ok=True)
    report={}
    for name,filename in SOURCES.items():
        shutil.copyfile(GENERATED/filename,REVIEW/f'{name}-generated.png')
        source=Image.open(REVIEW/f'{name}-generated.png').convert('RGBA');a=np.array(source)
        if a[:,:,3].min()==255:
            # This sheet returned a baked neutral checker. Key only bright,
            # nearly-neutral paper; retain green skin and dark costume/metal.
            rgb=a[:,:,:3].astype(int)
            bg=(rgb.min(2)>218)&((rgb.max(2)-rgb.min(2))<18)
            # Keep enclosed bright metal/ivory highlights. Exterior checker and
            # large enclosed limb gaps are removed, small highlight islands stay.
            # Explicit bright-material regions in the hobgoblin source: cleavers
            # and fangs. Small white limb gaps elsewhere must remain transparent.
            highlights=[(10,385,155,495),(420,380,590,498),(815,90,1030,215),(1050,325,1240,410),
                        (10,735,200,825),(570,785,705,895),(745,810,940,925),(1130,805,1320,925),
                        (155,285,195,325),(520,330,560,375),(845,285,885,330),(1200,300,1250,350),
                        (130,705,175,755),(510,715,558,770),(880,710,930,775),(1260,710,1310,775)]
            for region in components(bg):
                touches=any(y==0 or y==a.shape[0]-1 or l==0 or r==a.shape[1] for y,l,r in region)
                cx=sum((l+r)*(r-l)/2 for y,l,r in region)/sum(r-l for y,l,r in region)
                cy=sum(y*(r-l) for y,l,r in region)/sum(r-l for y,l,r in region)
                keep=any(l<=cx<=r and t<=cy<=b for l,t,r,b in highlights)
                if not touches and keep and sum(r-l for y,l,r in region)<350:
                    for y,l,r in region:bg[y,l:r]=False
            a[:,:,3]=np.array(Image.fromarray(np.uint8(~bg)*255).filter(ImageFilter.MinFilter(3)))
        # Clear RGB under alpha as well: some image viewers ignore alpha in previews.
        a[a[:,:,3]==0,:3]=0
        groups=components(a[:,:,3]>24)
        figures=[g for g in groups if sum(r-l for y,l,r in g)>5000]
        assert len(figures)==8, (name,len(figures))
        bounds=lambda g:(min(l for y,l,r in g),min(y for y,l,r in g),max(r for y,l,r in g),max(y for y,l,r in g)+1)
        figures.sort(key=lambda g:(bounds(g)[1]+bounds(g)[3])/2)
        figures=sorted(figures[:4],key=lambda g:bounds(g)[0])+sorted(figures[4:],key=lambda g:bounds(g)[0])
        crops=[];rects=[]
        for group in figures:
            mask=np.zeros(a.shape[:2],np.uint8)
            for y,l,r in group:mask[y,l:r]=255
            mask=np.array(Image.fromarray(mask).filter(ImageFilter.MaxFilter(3)))>0
            cel=a.copy();cel[~mask,3]=0;cel[cel[:,:,3]==0,:3]=0
            im=Image.fromarray(cel);box=im.getbbox();rects.append(box);crops.append(im.crop(box))
        # One uniform scale for each species: crouching and raised weapons do not
        # change body scale. Frame 1's standing silhouette sets world height.
        height=450 if name=='Goblin' else 570
        scale=height/crops[0].height
        frames=[];records=[]
        for i,crop in enumerate(crops):
            crop=crop.resize((round(crop.width*scale),round(crop.height*scale)),Image.Resampling.LANCZOS)
            solid=np.array(crop.getchannel('A'))>127
            # Restrict the sole search to the body half; a low blade at far left
            # is not a foot and must not shift the character's ground baseline.
            ground=int(np.where(solid[:,int(crop.width*.45):])[0].max())
            feet=np.where(solid[max(0,ground-8):ground+1].any(0))[0]
            center=(int(feet.min())+int(feet.max()))/2
            offset=(round(576-center),816-ground)
            assert offset[0]>=0 and offset[1]>=0 and offset[0]+crop.width<=1152 and offset[1]+crop.height<=896,(name,i,crop.size,offset)
            cel=Image.new('RGBA',(1152,896));cel.paste(crop,offset)
            cel.save(ART/f'{name}Attack{i+1}.png');frames.append(cel)
            records.append({'source_rect':rects[i],'scale':scale,'offset':offset,'sole_y':816})
        # Exact same rest cel at either end avoids a redraw pop when idling.
        frames[-1]=frames[0].copy();frames[-1].save(ART/f'{name}Attack8.png')
        records[-1]={**records[0],'reused_from_frame':1}
        frames[0].save(ART/f'{name}Idle.png')
        contact=Image.new('RGB',(2304,944),(104,134,113));tiles=[]
        for i,frame in enumerate(frames):
            bg=Image.new('RGB',frame.size,(104,134,113));bg.paste(frame,(0,0),frame);tile=bg.resize((576,448),Image.Resampling.LANCZOS)
            tiles.append(tile);x=i%4*576;y=i//4*472;contact.paste(tile,(x,y));ImageDraw.Draw(contact).text((x+280,y+452),str(i+1),fill='white')
        contact.save(REVIEW/f'{name.lower()}-attack-contact.png')
        tiles[0].save(REVIEW/f'{name.lower()}-attack.gif',save_all=True,append_images=tiles[1:],duration=[120,130]*4,loop=0,disposal=2)
        report[name]={'frames':records,'standing_pixels':height,'ppu':150,'world_height':height/150,'rest_matches_end':True}
    (REVIEW/'preparation.json').write_text(json.dumps(report,indent=2))
    print('Prepared 8 attack cels + exact rest for each species; 1152x896 canvas, sole816, 150PPU.')

if __name__=='__main__':main()
