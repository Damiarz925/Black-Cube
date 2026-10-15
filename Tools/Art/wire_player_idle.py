# Developer map: Legacy PlayerIdle metadata/reference installer; superseded by install_chibi_player.py and may require old external sources.
# See Docs/DEVELOPER_HANDOFF.md for system flow and validation.
"""Write Unity sprite metadata and wire only the paper player's presentation."""
from pathlib import Path
import re
import shutil
import uuid

ROOT = Path(__file__).resolve().parents[2]
ART = ROOT/'Assets/Art/PaperBattle/PlayerIdle'


def guid(name):
    return uuid.uuid5(uuid.NAMESPACE_URL, 'black-cube/player-idle/'+name).hex


template = (ROOT/'Assets/Art/PaperBattle/Player.png.meta').read_text()
for i in range(1,33):
    name = f'PlayerIdle{i}.png'
    if not (ART/name).is_file(): raise RuntimeError(f'Missing {name}')
    meta = re.sub(r'^guid: .*$', 'guid: '+guid(name), template, flags=re.M)
    meta = meta.replace('spriteMode: 0','spriteMode: 1').replace('textureType: 0','textureType: 8')
    meta = meta.replace('alignment: 0','alignment: 9')
    meta = meta.replace('spritePivot: {x: 0.5, y: 0.5}','spritePivot: {x: 0.5, y: 0.022058824}')
    meta = meta.replace('spritePixelsToUnits: 100','spritePixelsToUnits: 150')
    meta = meta.replace('spriteMeshType: 1','spriteMeshType: 0')
    meta = meta.replace('spriteGenerateFallbackPhysicsShape: 1','spriteGenerateFallbackPhysicsShape: 0')
    meta = meta.replace('textureCompression: 1','textureCompression: 0')
    meta = meta.replace('    spriteID: \n','    spriteID: '+guid(name+'/sprite')+'\n')
    meta = meta.replace('    internalID: 0','    internalID: 21300000')
    (ART/(name+'.meta')).write_text(meta, newline='\n')
(ART.parent/'PlayerIdle.meta').write_text('fileFormatVersion: 2\nguid: '+guid('folder')+
    '\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n')

prefab = ROOT/'Assets/Prefabs/PaperBattle/PaperBattle.prefab'
text = prefab.read_text()
actor_pattern = r'(  m_EditorClassIdentifier: Assembly-CSharp::PaperSpriteActor\n  body: \{fileID: 7609367433249518449\}\n)(.*?)(?=--- !u!)'
match = re.search(actor_pattern,text,re.S)
if not match: raise RuntimeError('Expected paper player actor not found')
refs = '\n'.join('  - {fileID: 21300000, guid: '+guid(f'PlayerIdle{i}.png')+', type: 3}' for i in range(1,33))
text = text[:match.start()] + match[1] + '  poses: []\n  idleFrames:\n'+refs+'\n  idleFramesPerSecond: 8\n'+text[match.end():]
renderer_pattern = r'(--- !u!212 &7609367433249518449\n.*?)(?=--- !u!)'
match = re.search(renderer_pattern,text,re.S)
if not match: raise RuntimeError('Expected paper player renderer not found')
renderer = re.sub(r'  m_Sprite: .*', '  m_Sprite: {fileID: 21300000, guid: '+guid('PlayerIdle1.png')+', type: 3}',match[0])
renderer = renderer.replace('m_Size: {x: 5.2965517, y: 3.5310345}','m_Size: {x: 1.7066667, y: 3.6266667}')
text = text[:match.start()]+renderer+text[match.end():]
prefab.write_text(text, newline='\n')
source_dir=ROOT/'ReviewCaptures/PlayerIdle/Sources'
source_dir.mkdir(exist_ok=True)
for name in ('PlayerCharacterBlackCube.png','PlayerCharacterBlackCubeIdleAnimation.png'):
    shutil.copyfile(Path('D:/Documents/BlackCubeAssets')/name,source_dir/name)
print('Wired 32 single-sprite PNGs into a four-second loop; archived both source references.')
