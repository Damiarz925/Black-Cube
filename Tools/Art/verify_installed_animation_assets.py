# Developer map: Legacy player-layout verifier with superseded frame expectations. Use verify_chibi_player_assets.py for the installed player.
# See Docs/DEVELOPER_HANDOFF.md for system flow and validation.
from pathlib import Path
import re, json
import numpy as np
from PIL import Image
root=Path(__file__).resolve().parents[2]
prefab=(root/'Assets/Prefabs/PaperBattle/PaperBattle.prefab').read_text()
actor=re.search(r'  m_EditorClassIdentifier: Assembly-CSharp::PaperSpriteActor\n  body: \{fileID: 7609367433249518449\}\n(.*?)(?=--- !u!)',prefab,re.S)[1]
paths={}
for folder,count,prefixes,size,pivot in [('PlayerIdleConsistent',4,['PlayerIdle'],(320,544),'0.022058824'),('PlayerAttack',8,['PlayerAttack','PlayerWeapon'],(640,640),'0.04375')]:
    for prefix in prefixes:
        for i in range(1,count+1):
            p=root/f'Assets/Art/PaperBattle/{folder}/{prefix}{i}.png';im=Image.open(p);assert im.mode=='RGBA' and im.size==size
            a=np.array(im);assert a[:,:,3].any();assert not a[0,:,3].any() and not a[-1,:,3].any() and not a[:,0,3].any() and not a[:,-1,3].any(),str(p)+' clipped'
            meta=p.with_suffix('.png.meta').read_text();g=re.search(r'^guid: (\w+)',meta,re.M)[1];assert g not in paths
            paths[g]=p
            # Unity may serialize a cached sprites entry even in single-sprite mode.
            assert 'spriteMode: 1' in meta and 'spritePixelsToUnits: 150' in meta and re.search(r'^    internalID: 21300000$',meta,re.M)
            assert 'spritePivot: {x: 0.5, y: '+pivot+'}' in meta
for field,count,unique in [('idleFrames',6,4),('attackFrames',8,8),('attackWeaponFrames',8,8)]:
    block=re.search(r'  '+field+r':\n((?:  - .*\n)+)',actor)[1]
    refs=re.findall(r'fileID: (\d+), guid: (\w+)',block)
    assert len(refs)==count and len(set(g for f,g in refs))==unique
    assert all(f=='21300000' and g in paths for f,g in refs)
assert 'idleFramesPerSecond: 1.5' in actor
for rel,count,millis in [('ReviewCaptures/PlayerIdle/BestAvailable/consistent-idle-game-scale.gif',6,4000),('ReviewCaptures/PlayerAttack/installed-attack-preview.gif',8,1000)]:
    gif=Image.open(root/rel);assert gif.n_frames==count
    durations=[]
    for i in range(count):gif.seek(i);durations.append(gif.info['duration'])
    assert sum(durations)==millis,(rel,durations)
report={'status':'PASS','idle_unique':4,'idle_slots':6,'idle_seconds':4,'attack_body_cels':8,'weapon_cels':8,'single_sprite_GUID_refs_resolve':True,'transparent_unclipped_PNGs':20,'ppu':150,'unity_import_or_runtime_tested':False}
(root/'ReviewCaptures/ANIMATION-ASSET-VERIFICATION.json').write_text(json.dumps(report,indent=2))
print(json.dumps(report))
