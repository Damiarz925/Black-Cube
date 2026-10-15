# Developer map: Current player grip authoring and equipped/unarmed previews. Run after preparation and before installation; source coordinates are mapped through extraction scale/offset.
# See Docs/DEVELOPER_HANDOFF.md for system flow and validation.
"""Per-frame authored grip anchors and rigid whole-weapon preview rendering."""
from pathlib import Path
import json,math
from PIL import Image,ImageDraw
root=Path(__file__).resolve().parents[2];review=root/'ReviewCaptures/ChibiPlayer';art=root/'Assets/Art/PaperBattle/ChibiPlayer'
data=json.loads((review/'preparation.json').read_text())
# Pixel centers of the anatomical RIGHT closed fist in each authored source sheet.
source_hands={'Idle':[(169,375),(510,381),(875,389),(1226,386),(174,830),(507,826),(863,827),(1221,827)],
              'Attack':[(116,349),(472,309),(860,178),(1407,285),(330,730),(659,759),(867,808),(1214,811)]}
# +X is forward for the right-facing player. Rest/endpoints share a forward
# down carry; the ready pose draws back before the overhead windup and impact.
angles={'Idle':[-25]*8,'Attack':[-25,-140,60,0,-10,-35,-25,-25]}
records={};sword=Image.open(art/'Sword.png').convert('RGBA')
# Whole sword pivot at center of the wrapped grip, not its guard or blade.
sword_grip=(70,64)
for kind in ['Idle','Attack']:
    poses=[];tiles=[];bare=[];contact=Image.new('RGB',(4*480,2*424),(104,134,113))
    guide=Image.new('RGB',contact.size,(104,134,113))
    for i,(hand,rec) in enumerate(zip(source_hands[kind],data[kind])):
        left,top,_,_=rec['source_rect'];scale=rec['scale'];ox,oy=rec['offset']
        hx=(hand[0]-left)*scale+ox;hy=(hand[1]-top)*scale+oy;angle=angles[kind][i]
        in_front=kind=='Idle' or i in [0,6,7]
        poses.append({'position':[(hx-320)/150,(612-hy)/150],'angle':angle,'inFront':in_front,'pixel_hand':[hx,hy]})
        # Put grip at rotation center; use the same rigid rotation as the runtime.
        weapon=Image.new('RGBA',(768,768));weapon.paste(sword,(384-sword_grip[0],384-sword_grip[1]))
        weapon=weapon.rotate(angle,Image.Resampling.BICUBIC,center=(384,384))
        full=Image.new('RGBA',(960,800));full.alpha_composite(weapon,(round(hx+160-384),round(hy+80-384)))
        body=Image.open(art/f'{kind}{i+1}.png').convert('RGBA')
        if in_front:
            weapon_layer=full;full=Image.new('RGBA',(960,800));full.alpha_composite(body,(160,80));full.alpha_composite(weapon_layer)
        else:full.alpha_composite(body,(160,80))
        bg=Image.new('RGB',full.size,(104,134,113));bg.paste(full,(0,0),full);tile=bg.resize((480,400),Image.Resampling.LANCZOS);tiles.append(tile)
        bg=Image.new('RGB',full.size,(104,134,113));bg.paste(body,(160,80),body);bare.append(bg.resize((480,400),Image.Resampling.LANCZOS))
        x=(i%4)*480;y=(i//4)*424;contact.paste(tile,(x,y));d=ImageDraw.Draw(contact);d.text((x+220,y+405),str(i+1),fill='white')
        annotated=tile.copy();d=ImageDraw.Draw(annotated);gx=(hx+160)/2;gy=(hy+80)/2;d.ellipse((gx-4,gy-4,gx+4,gy+4),outline=(255,0,255),width=2)
        d.line((gx,gy,gx+35*math.cos(math.radians(angle)),gy-35*math.sin(math.radians(angle))),fill=(255,0,255),width=2);guide.paste(annotated,(x,y))
    records[kind]=poses;contact.save(review/f'{kind.lower()}-equipped-contact.png');guide.save(review/f'{kind.lower()}-grip-guide.png')
    durations=[500]*8 if kind=='Idle' else [120,130]*4
    for frames,suffix in [(tiles,'equipped'),(bare,'unarmed')]:
        frames[0].save(review/f'{kind.lower()}-{suffix}.gif',save_all=True,append_images=frames[1:],duration=durations,loop=0,disposal=2)
records['Sword']={'grip_pixels':sword_grip,'pivot':[sword_grip[0]/320,1-sword_grip[1]/128],'ppu':150}
(review/'attachments.json').write_text(json.dumps(records,indent=2))
print(str(review))
