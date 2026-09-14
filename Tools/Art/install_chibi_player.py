# Developer map: Current deterministic sprite metadata, animation/sword assets and main-prefab installer. Run only after preparation and attachment authoring; persistent GUIDs preserve references.
# See Docs/DEVELOPER_HANDOFF.md for system flow and validation.
from pathlib import Path
import json,re,uuid
root=Path(__file__).resolve().parents[2];art=root/'Assets/Art/PaperBattle/ChibiPlayer';review=root/'ReviewCaptures/ChibiPlayer'
def guid(name):return uuid.uuid5(uuid.NAMESPACE_URL,'black-cube/chibi-player/'+name).hex
attachment=json.loads((review/'attachments.json').read_text())
template=(root/'Assets/Art/PaperBattle/PlayerAttack/PlayerAttack1.png.meta').read_text()
template=re.sub(r'  internalIDToNameTable:.*?  externalObjects:','  internalIDToNameTable: []\n  externalObjects:',template,flags=re.S)
template=re.sub(r'  spriteSheet:.*?(?=  spritePackingTag:|  pSDRemoveMatte:|  userData:)', '  spriteSheet:\n    serializedVersion: 2\n    sprites: []\n    outline: []\n    physicsShape: []\n    bones: []\n    spriteID: PLACEHOLDER\n    internalID: 21300000\n    vertices: []\n    indices: \n    edges: []\n    weights: []\n    secondaryTextures: []\n',template,flags=re.S)
for p in art.glob('*.png'):
    m=re.sub(r'^guid: .*$', 'guid: '+guid(p.name),template,flags=re.M).replace('PLACEHOLDER',guid(p.name+'/sprite'))
    pivot=attachment['Sword']['pivot'] if p.name=='Sword.png' else [.5,.04375]
    m=re.sub(r'  spritePivot: .*',f'  spritePivot: {{x: {pivot[0]}, y: {pivot[1]}}}',m)
    p.with_suffix('.png.meta').write_text(m,newline='\n')
for name in ['PaperWeaponVisual.cs','PaperPlayerAnimationSet.cs']:
    (root/'Assets/Scripts'/f'{name}.meta').write_text('fileFormatVersion: 2\nguid: '+guid(name)+'\n',newline='\n')
(art.parent/'ChibiPlayer.meta').write_text('fileFormatVersion: 2\nguid: '+guid('folder')+'\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n',newline='\n')
def asset_header(script,name):
    return '%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n--- !u!114 &11400000\nMonoBehaviour:\n  m_ObjectHideFlags: 0\n  m_CorrespondingSourceObject: {fileID: 0}\n  m_PrefabInstance: {fileID: 0}\n  m_PrefabAsset: {fileID: 0}\n  m_GameObject: {fileID: 0}\n  m_Enabled: 1\n  m_EditorHideFlags: 0\n  m_Script: {fileID: 11500000, guid: '+guid(script+'.cs')+', type: 3}\n  m_Name: '+name+'\n  m_EditorClassIdentifier: Assembly-CSharp::'+script+'\n'
def refs(field,prefix):return '  '+field+':\n'+''.join('  - {fileID: 21300000, guid: '+guid(f'{prefix}{i}.png')+', type: 3}\n' for i in range(1,9))
def grips(field,kind):
    result='  '+field+':\n'
    for pose in attachment[kind]:
        x,y=pose['position'];result+=f'  - position: {{x: {x:.8f}, y: {y:.8f}}}\n    angle: {pose["angle"]}\n    inFront: {int(pose["inFront"])}\n'
    return result
sword=asset_header('PaperWeaponVisual','Default Sword')+'  sprite: {fileID: 21300000, guid: '+guid('Sword.png')+', type: 3}\n  gripOffset: {x: 0, y: 0}\n  rotationOffset: 0\n  scale: 1\n'
(art/'DefaultSword.asset').write_text(sword,newline='\n')
config=asset_header('PaperPlayerAnimationSet','Chibi Player Animation')+refs('idleFrames','Idle')+'  idleFramesPerSecond: 2\n'+refs('attackFrames','Attack')+grips('idleWeaponGrips','Idle')+grips('attackWeaponGrips','Attack')+'  defaultWeaponVisual: {fileID: 11400000, guid: '+guid('DefaultSword.asset')+', type: 2}\n'
(art/'PlayerAnimation.asset').write_text(config,newline='\n')
for name in ['DefaultSword.asset','PlayerAnimation.asset']:
    (art/(name+'.meta')).write_text('fileFormatVersion: 2\nguid: '+guid(name)+'\nNativeFormatImporter:\n  externalObjects: {}\n  mainObjectFileID: 11400000\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n',newline='\n')
prefab=root/'Assets/Prefabs/PaperBattle/PaperBattle.prefab';text=prefab.read_text()
pattern=r'(  m_EditorClassIdentifier: Assembly-CSharp::PaperSpriteActor\n  body: \{fileID: 7609367433249518449\}\n)(.*?)(?=--- !u!)';m=re.search(pattern,text,re.S);assert m
fields='  animationSet: {fileID: 11400000, guid: '+guid('PlayerAnimation.asset')+', type: 2}\n  poses: []\n'+refs('idleFrames','Idle')+'  idleFramesPerSecond: 2\n'+refs('attackFrames','Attack')+'  attackWeaponFrames: []\n  useWeaponAttachments: 1\n  defaultWeaponVisual: {fileID: 11400000, guid: '+guid('DefaultSword.asset')+', type: 2}\n'+grips('idleWeaponGrips','Idle')+grips('attackWeaponGrips','Attack')
text=text[:m.start()]+m[1]+fields+text[m.end():]
pattern=r'(--- !u!212 &7609367433249518449\n.*?)(?=--- !u!)';m=re.search(pattern,text,re.S);assert m
renderer=re.sub(r'  m_Sprite: .*','  m_Sprite: {fileID: 21300000, guid: '+guid('Idle1.png')+', type: 3}',m[0]);renderer=re.sub(r'  m_Size: .*','  m_Size: {x: 4.2666667, y: 4.2666667}',renderer)
text=text[:m.start()]+renderer+text[m.end():];prefab.write_text(text,newline='\n')
print('Installed8 weapon-free idle +8 attack cels, hand anchors, whole-sword profile and shared animation config in main player prefab.')
