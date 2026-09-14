# Developer map: Legacy fixed-body idle subset installer that rewires the player prefab. Do not run against the current chibi configuration.
# See Docs/DEVELOPER_HANDOFF.md for system flow and validation.
"""Install four consistent authored cels with an explicit forward/return schedule."""
from pathlib import Path
import hashlib, json, re, uuid
import numpy as np
from PIL import Image, ImageDraw

root=Path(__file__).resolve().parents[2]
source=root/'ReviewCaptures/PlayerIdle/CoherentKeys/FixedBodyProof'
review=root/'ReviewCaptures/PlayerIdle/BestAvailable';review.mkdir(exist_ok=True)
art=root/'Assets/Art/PaperBattle/PlayerIdleConsistent';art.mkdir(exist_ok=True)
order=[1,2,3,4,3,2]
def guid(name): return uuid.uuid5(uuid.NAMESPACE_URL,'black-cube/player-idle-consistent/'+name).hex
template=(root/'Assets/Art/PaperBattle/PlayerIdle/PlayerIdle1.png.meta').read_text()
template=re.sub(r'  internalIDToNameTable:.*?  externalObjects:', '  internalIDToNameTable: []\n  externalObjects:',template,flags=re.S)
template=re.sub(r'  spriteSheet:.*?(?=  spritePackingTag:|  pSDRemoveMatte:|  userData:)', '  spriteSheet:\n    serializedVersion: 2\n    sprites: []\n    outline: []\n    physicsShape: []\n    bones: []\n    spriteID: PLACEHOLDER\n    internalID: 21300000\n    vertices: []\n    indices: \n    edges: []\n    weights: []\n    secondaryTextures: []\n',template,flags=re.S)
frames=[]
for i in range(1,5):
    frame=Image.open(source/f'proof-{i-1}.png').convert('RGBA');frames.append(frame)
    name=f'PlayerIdle{i}.png';frame.save(art/name)
    meta=re.sub(r'^guid: .*$', 'guid: '+guid(name),template,flags=re.M)
    meta=re.sub(r'(?m)^(\s+spriteID:) .*$',r'\1 '+guid(name+'/sprite'),meta)
    (art/(name+'.meta')).write_text(meta,newline='\n')
(art.parent/'PlayerIdleConsistent.meta').write_text('fileFormatVersion: 2\nguid: '+guid('folder')+'\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n',newline='\n')
prefab=root/'Assets/Prefabs/PaperBattle/PaperBattle.prefab';text=prefab.read_text()
pattern=r'(  m_EditorClassIdentifier: Assembly-CSharp::PaperSpriteActor\n  body: \{fileID: 7609367433249518449\}\n)(.*?)(?=--- !u!)'
m=re.search(pattern,text,re.S);assert m
existing_attack=re.search(r'  attackFrames:.*',m[2],re.S)
refs='\n'.join('  - {fileID: 21300000, guid: '+guid(f'PlayerIdle{i}.png')+', type: 3}' for i in order)
text=text[:m.start()]+m[1]+'  poses: []\n  idleFrames:\n'+refs+'\n  idleFramesPerSecond: 1.5\n'+(existing_attack[0] if existing_attack else '')+text[m.end():]
pattern=r'(--- !u!212 &7609367433249518449\n.*?)(?=--- !u!)';m=re.search(pattern,text,re.S);assert m
renderer=re.sub(r'  m_Sprite: .*','  m_Sprite: {fileID: 21300000, guid: '+guid('PlayerIdle1.png')+', type: 3}',m[0])
renderer=re.sub(r'  m_Size: .*','  m_Size: {x: 2.1333333, y: 3.6266667}',renderer)
text=text[:m.start()]+renderer+text[m.end():];prefab.write_text(text,newline='\n')
contact=Image.new('RGB',(4*192,350),(104,134,113));tiles=[]
for i,f in enumerate(frames):
    rgb=Image.new('RGB',f.size,(104,134,113));rgb.paste(f,(0,0),f)
    tile=rgb.resize((192,326),Image.Resampling.LANCZOS);tiles.append(tile);contact.paste(tile,(i*192,0))
    ImageDraw.Draw(contact).text((i*192+75,333),f'Pose {i+1}',fill='white')
contact.save(review/'consistent-idle-contact.png')
loop=[tiles[i-1] for i in order]
loop[0].save(review/'consistent-idle-game-scale.gif',save_all=True,append_images=loop[1:],duration=[670,660,670,670,660,670],loop=0,disposal=2)
a=[np.array(f) for f in frames]
assert len({f.tobytes() for f in a})==4
locked=np.ones((544,320),bool);locked[105:320,:150]=False
assert all(np.array_equal(f[locked],a[0][locked]) for f in a)
assert all(int(np.where(f[:,:,3]>127)[0].max())==532 for f in a)
assert all(not f[0,:,3].any() and not f[-1,:,3].any() and not f[:,0,3].any() and not f[:,-1,3].any() for f in a)
report={'distinct_drawings':4,'playback_order':order,'playback_slots':6,'steps_per_second':1.5,'cycle_seconds':4,'held_duplicate_slots':0,'return_uses_existing_drawings':True,'source_proof_indices':[0,1,2,3],'invariant_pixels_locked':True,'resolution':[320,544],'ppu':150,'foot_baseline':532,'hashes':[hashlib.sha256(f.tobytes()).hexdigest() for f in a],'limitation':'Limited waist-to-hip arm movement; deliberate visible stepping. Not the originally requested full-range smooth32.','installed_folder':str(art),'old_32_assets':'Preserved but unreferenced by player/builder.'}
(review/'checks.json').write_text(json.dumps(report,indent=2),newline='\n')
print(json.dumps(report,indent=2))
