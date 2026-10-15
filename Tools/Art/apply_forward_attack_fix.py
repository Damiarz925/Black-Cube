# Developer map: Legacy tall-player forward-strike cleanup; writes PlayerAttack PNGs and reviews. Superseded by ChibiPlayer; do not run to update the current player.
# See Docs/DEVELOPER_HANDOFF.md for system flow and validation.
"""Attack-only cleanup and authored forward impact replacement; no timing changes."""
from pathlib import Path
import json,hashlib
import numpy as np
from PIL import Image,ImageDraw,ImageFilter
from prepare_player_idle import flood

root=Path(__file__).resolve().parents[2]
review=root/'ReviewCaptures/PlayerAttack';work=review/'ForwardStrikeFix';art=root/'Assets/Art/PaperBattle/PlayerAttack'
seeds={1:[(235,337)],2:[(287,310),(391,333)],3:[(311,146),(254,292),(361,327)],
       4:[(177,405),(360,395)],5:[(258,383),(450,384)],6:[],7:[(244,362)],8:[(240,338),(238,304)]}
frames=[];weapons=[];removed={}
for i in range(1,9):
    body=Image.open(work/'Before'/f'PlayerAttack{i}.png').convert('RGBA')
    weapon=Image.open(work/'Before'/f'PlayerWeapon{i}.png').convert('RGBA')
    if i==4:
        original=Image.open(work/'forward-strike-generated.png').convert('RGB')
        rgb=original.resize((640,640),Image.Resampling.LANCZOS);a=np.array(rgb)
        fg=a.min(2)<230
        # Shared canvas proportion, then translate soles to existing ground line.
        y,x=np.where(fg & (np.indices(fg.shape)[0]>500));ground=int(y.max())
        alpha=Image.fromarray(np.uint8(fg)*255).filter(ImageFilter.MinFilter(3))
        rgba=rgb.convert('RGBA');rgba.putalpha(alpha)
        body=Image.new('RGBA',(640,640));body.paste(rgba,(0,612-ground))
        body.save(work/'forward-aligned-before-gray.png')
        a=np.array(body);blue=(a[:,:,2].astype(int)>a[:,:,0].astype(int)+24)&(a[:,:,2]>110)&(a[:,:,1]>75)
        blade=np.array(Image.fromarray(np.uint8(blue)*255).filter(ImageFilter.MaxFilter(3)))>0
        wa=a.copy();wa[:,:,3]=np.where(blade,a[:,:,3],0);a[:,:,3]=np.where(blade,0,a[:,:,3]);body=Image.fromarray(a);weapon=Image.fromarray(wa)
    a=np.array(body);c=a[:,:,:3].astype(int)
    allowed=(c.max(2)-c.min(2)<25)&(c.max(2)>58)&(a[:,:,3]>0)
    clear=flood(allowed,seeds[i])
    clear=np.array(Image.fromarray(np.uint8(clear)*255).filter(ImageFilter.MaxFilter(3)))>0
    removed[i]=int(np.count_nonzero(clear&(a[:,:,3]>0)));a[clear,3]=0;a[a[:,:,3]==0,:3]=0
    body=Image.fromarray(a);frames.append(body);weapons.append(weapon)
    body.save(art/f'PlayerAttack{i}.png');weapon.save(art/f'PlayerWeapon{i}.png')

contact=Image.new('RGB',(4*384,2*408),'white');checker=Image.new('RGB',contact.size,'white');tiles=[]
for i,(body,weapon) in enumerate(zip(frames,weapons)):
    full=Image.alpha_composite(body,weapon);bg=Image.new('RGB',(640,640),(104,134,113));bg.paste(full,(0,0),full)
    tile=bg.resize((384,384),Image.Resampling.LANCZOS);tiles.append(tile);x=(i%4)*384;y=(i//4)*408;contact.paste(tile,(x,y));ImageDraw.Draw(contact).text((x+175,y+390),str(i+1),fill='black')
    check=Image.new('RGB',(640,640),'white');d=ImageDraw.Draw(check)
    for yy in range(0,640,32):
        for xx in range(0,640,32):
            if (xx//32+yy//32)%2:d.rectangle((xx,yy,xx+31,yy+31),fill=(190,200,215))
    check.paste(full,(0,0),full);checker.paste(check.resize((384,384)),(x,y));ImageDraw.Draw(checker).text((x+175,y+390),str(i+1),fill='black')
contact.save(review/'installed-attack-contact.png');contact.save(work/'forward-attack-contact.png');checker.save(work/'transparency-checker.png')
for path in [review/'installed-attack-preview.gif',work/'forward-attack-preview.gif']:
    tiles[0].save(path,save_all=True,append_images=tiles[1:],duration=[120,130]*4,loop=0,disposal=2)
expected=json.loads((work/'unchanged-before.json').read_text())
assert all(hashlib.sha256((root/p).read_bytes()).hexdigest()==h for p,h in expected.items()),'Idle, prefab or timing source changed'
for i,points in seeds.items():
    a=np.array(frames[i-1]);assert all(a[y,x,3]==0 for x,y in points),'Uncleared verified background gap'
blade=np.array(weapons[3]);ys,xs=np.where(blade[:,:,3]>127)
angle=float(np.degrees(np.arctan(np.polyfit(xs,ys,1)[0])))
assert xs.max()-xs.min()>150 and abs(angle)<3,'Impact sword is not horizontal and forward'
for i in range(1,9):
    if i!=4:
        assert np.array_equal(np.array(weapons[i-1]),np.array(Image.open(work/'Before'/f'PlayerWeapon{i}.png'))),'Other sword cels changed'
report={'removed_gray_pixels':removed,'idle_prefab_timing_unchanged':True,'verified_gap_seeds_transparent':True,'forward_blade_angle_degrees':angle,'other_seven_blade_cels_unchanged':True,'canvas':[640,640],'impact_frame':4,'unity_runtime_tested':False}
print(json.dumps(report))
(work/'cleanup-checks.json').write_text(json.dumps(report,indent=2))
