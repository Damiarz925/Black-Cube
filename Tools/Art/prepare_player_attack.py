# Developer map: Legacy tall-player attack extraction and cleanup; outputs the superseded PlayerAttack set and forward-strike correction.
# See Docs/DEVELOPER_HANDOFF.md for system flow and validation.
"""Slice approved eight poses; remove poster gray and separate the blue guide blade.

Uniform scale shared by all poses preserves crouch/windup heights. No pose edits.
"""
from pathlib import Path
import json, re, uuid
import numpy as np
from PIL import Image, ImageFilter, ImageDraw
from prepare_player_idle import flood

root=Path(__file__).resolve().parents[2]
review=root/'ReviewCaptures/PlayerAttack';art=root/'Assets/Art/PaperBattle/PlayerAttack';art.mkdir(exist_ok=True)
sheet=Image.open(review/'recovery-six-review.png').convert('RGB')
cols=[20,392,769,1145,1512]
grounds=[439,442,440,440,861,862,870,869]
centers=[192,565,934,1332,161,578,926,1302]
scale=516/361
frames=[];weapons=[];records=[]
for i in range(8):
    row=i//4;col=i%4;left=cols[col]+3;right=cols[col+1]-3;top=65 if row==0 else 510
    bottom=grounds[i]+1
    crop=sheet.crop((left,top,right,bottom));a=np.array(crop);h,w,_=a.shape
    chroma=a.max(axis=2).astype(int)-a.min(axis=2)
    background=(chroma<23)&(a.max(axis=2)>63)
    seeds=[(x,0) for x in range(w)]+[(x,h-1) for x in range(w)]+[(0,y) for y in range(h)]+[(w-1,y) for y in range(h)]
    mask=~flood(background,seeds)
    # Foreground torso seeds; exclude isolated panel numbers and ground fragments.
    seedx=int(centers[i]-left);seedy=int((grounds[i]-top)*.52)
    mask=flood(mask,[(seedx,seedy)])
    # Include isolated colored blade pixels if anti-aliasing separated them.
    blue=(a[:,:,2].astype(int)>a[:,:,0].astype(int)+24)&(a[:,:,2]>110)&(a[:,:,1]>75)
    mask |= blue
    # Trim thin poster ground lines extending beyond either boot.
    support=mask[max(0,h-12):max(1,h-6)].any(axis=0)
    support=np.array(Image.fromarray(np.uint8(support[None,:])*255).filter(ImageFilter.MaxFilter(7)))[0]>0
    mask[max(0,h-5):] &= support
    alpha=Image.fromarray(np.uint8(mask)*255).filter(ImageFilter.MinFilter(3)).filter(ImageFilter.GaussianBlur(.3))
    alpha=Image.fromarray(np.maximum(np.array(alpha),np.uint8(blue)*255))
    rgba=crop.convert('RGBA');rgba.putalpha(alpha)
    # The blue guide blade is a separate cel; glove and generic gold grip stay with the hand.
    blade=Image.fromarray(np.uint8(blue)*255).filter(ImageFilter.MaxFilter(3))
    blade=np.array(blade)>0
    body=np.array(rgba);weapon=body.copy();weapon[:,:,3]=np.where(blade,body[:,:,3],0)
    body[:,:,3]=np.where(blade,0,body[:,:,3])
    result=[]
    for pixels in [body,weapon]:
        im=Image.fromarray(pixels).resize((round(w*scale),round(h*scale)),Image.Resampling.LANCZOS)
        canvas=Image.new('RGBA',(640,640))
        offset=(round(320-(centers[i]-left)*scale),612-round((grounds[i]-top)*scale))
        canvas.paste(im,offset);arr=np.array(canvas);arr[arr[:,:,3]==0,:3]=0
        result.append(Image.fromarray(arr))
    result[0].save(art/f'PlayerAttack{i+1}.png');result[1].save(art/f'PlayerWeapon{i+1}.png')
    frames.append(result[0]);weapons.append(result[1])
    records.append({'frame':i+1,'crop':[left,top,right,bottom],'foot_center':centers[i],'ground':grounds[i],'uniform_scale':scale,'offset':offset})

def guid(name):return uuid.uuid5(uuid.NAMESPACE_URL,'black-cube/player-attack/'+name).hex
template=(root/'Assets/Art/PaperBattle/PlayerIdleConsistent/PlayerIdle1.png.meta').read_text()
template=re.sub(r'  internalIDToNameTable:.*?  externalObjects:', '  internalIDToNameTable: []\n  externalObjects:',template,flags=re.S)
template=re.sub(r'    sprites:.*?    outline:', '    sprites: []\n    outline:',template,flags=re.S)
# Replace whole serialized sprite sheet to remove stale multi-sprite entries.
template=re.sub(r'  spriteSheet:.*?(?=  spritePackingTag:|  pSDRemoveMatte:|  userData:)', '  spriteSheet:\n    serializedVersion: 2\n    sprites: []\n    outline: []\n    physicsShape: []\n    bones: []\n    spriteID: PLACEHOLDER\n    internalID: 21300000\n    vertices: []\n    indices: \n    edges: []\n    weights: []\n    secondaryTextures: []\n',template,flags=re.S)
for prefix in ['PlayerAttack','PlayerWeapon']:
    for i in range(1,9):
        name=f'{prefix}{i}.png';meta=re.sub(r'^guid: .*$', 'guid: '+guid(name),template,flags=re.M)
        meta=meta.replace('spritePivot: {x: 0.5, y: 0.022058824}','spritePivot: {x: 0.5, y: 0.04375}')
        meta=meta.replace('PLACEHOLDER',guid(name+'/sprite'))
        (art/(name+'.meta')).write_text(meta,newline='\n')
(art.parent/'PlayerAttack.meta').write_text('fileFormatVersion: 2\nguid: '+guid('folder')+'\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n',newline='\n')
contact=Image.new('RGB',(4*384,2*408),(104,134,113));tiles=[]
for i,(f,w) in enumerate(zip(frames,weapons)):
    full=Image.alpha_composite(f,w);rgb=Image.new('RGB',full.size,(104,134,113));rgb.paste(full,(0,0),full)
    tile=rgb.resize((384,384),Image.Resampling.LANCZOS);tiles.append(tile)
    x=(i%4)*384;y=(i//4)*408;contact.paste(tile,(x,y));ImageDraw.Draw(contact).text((x+175,y+390),str(i+1),fill='white')
contact.save(review/'installed-attack-contact.png')
tiles[0].save(review/'installed-attack-preview.gif',save_all=True,append_images=tiles[1:],duration=[120,130]*4,loop=0,disposal=2)
(review/'installed-attack-art-checks.json').write_text(json.dumps({'frames':8,'canvas':[640,640],'ppu':150,'preview_cycle_seconds':1,'impact_frame':4,'weapon_separation':'Blue blade isolated as eight overlay cels; generic gold grip remains in glove art. Future complete weapon swaps need a matching grip/hand treatment.','records':records},indent=2))
print(str(art))
# Apply the subsequently approved local impact edit and enclosed-gap cleanup.
if (review/'ForwardStrikeFix/forward-strike-generated.png').is_file():
    import runpy
    runpy.run_path(str(root/'Tools/Art/apply_forward_attack_fix.py'),run_name='__main__')
