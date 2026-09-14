# Developer map: Current enemy art/config installer cloning legacy normal/boss prefab gameplay blocks and changing only presentation. Updates both main battle slots with stable GUIDs.
# See Docs/DEVELOPER_HANDOFF.md for system flow and validation.
"""Install prepared enemy art as new prefab variants, preserving gameplay data.
Copies each existing normal/boss paper prefab and changes only presentation.
Stable GUIDs and both BattleManager slots make the assignment global to forests.
"""
from pathlib import Path
import re, uuid

ROOT=Path(__file__).resolve().parents[2]
ART=ROOT/'Assets/Art/PaperBattle/ForestEnemies'
PREFABS=ROOT/'Assets/Prefabs/PaperBattle'
def guid(name):return uuid.uuid5(uuid.NAMESPACE_URL,'black-cube/forest-enemies/'+name).hex
def meta(path):return re.search(r'^guid: (\w+)',Path(str(path)+'.meta').read_text(),re.M)[1]
def refs(field, filenames):
    return '  '+field+':\n'+''.join('  - {fileID: 21300000, guid: '+guid(f)+', type: 3}\n' for f in filenames)

def main():
    template=(ROOT/'Assets/Art/PaperBattle/ChibiPlayer/Idle1.png.meta').read_text()
    for png in ART.glob('*.png'):
        text=re.sub(r'^guid: .*$', 'guid: '+guid(png.name),template,flags=re.M)
        text=re.sub(r'    spriteID: .*','    spriteID: '+guid(png.name+'/sprite'),text)
        text=re.sub(r'  spritePivot: .*','  spritePivot: {x: 0.5, y: 0.08928571428571429}',text)
        Path(str(png)+'.meta').write_text(text)
    (ART.parent/'ForestEnemies.meta').write_text('fileFormatVersion: 2\nguid: '+guid('folder')+'\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n')
    (ROOT/'Assets/Scripts/PaperEnemyAnimationSet.cs.meta').write_text('fileFormatVersion: 2\nguid: '+guid('PaperEnemyAnimationSet.cs')+'\n')
    main_prefab=(PREFABS/'PaperBattle.prefab').read_text()
    main_prefab=re.sub(r'  enemiesToKillBeforeBoss: \d+', '  enemiesToKillBeforeBoss: 9', main_prefab)
    for name,old,slot in [('Goblin','Ghoul2D','normalEnemyPrefab'),('Hobgoblin','GhoulBoss2D','bossEnemyPrefab')]:
        config='%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n--- !u!114 &11400000\nMonoBehaviour:\n  m_ObjectHideFlags: 0\n  m_CorrespondingSourceObject: {fileID: 0}\n  m_PrefabInstance: {fileID: 0}\n  m_PrefabAsset: {fileID: 0}\n  m_GameObject: {fileID: 0}\n  m_Enabled: 1\n  m_EditorHideFlags: 0\n  m_Script: {fileID: 11500000, guid: '+guid('PaperEnemyAnimationSet.cs')+', type: 3}\n  m_Name: '+name+' Animation\n  m_EditorClassIdentifier: Assembly-CSharp::PaperEnemyAnimationSet\n  displayName: '+name+'\n'
        config+=refs('idleFrames',[name+'Idle.png'])+'  idleFramesPerSecond: 2\n'+refs('attackFrames',[f'{name}Attack{i}.png' for i in range(1,9)])
        config+=(refs('hitFrames',[f'GoblinHit{i}.png' for i in range(1,6)]) if name=='Goblin' else '  hitFrames: []\n')
        config+='  hitReactionDuration: 0.21\n'
        config+='  popupOffset: {x: 0, y: '+('3.2' if name=='Goblin' else '4.0')+', z: 0}\n'
        (ART/f'{name}Animation.asset').write_text(config)
        (ART/f'{name}Animation.asset.meta').write_text('fileFormatVersion: 2\nguid: '+guid(name+'Animation.asset')+'\nNativeFormatImporter:\n  externalObjects: {}\n  mainObjectFileID: 11400000\n')
        text=(PREFABS/f'{old}.prefab').read_text().replace('value: '+old+'\n','value: '+name+'2D\n')
        actor=re.search(r'(  m_EditorClassIdentifier: Assembly-CSharp::PaperSpriteActor\n  body: \{fileID: (\d+)\}\n)(.*?)(?=--- !u!|\Z)',text,re.S);assert actor
        body_id=actor[2]
        fields='  animationSet: {fileID: 0}\n  enemyAnimationSet: {fileID: 11400000, guid: '+guid(name+'Animation.asset')+', type: 2}\n  poses: []\n'+refs('idleFrames',[name+'Idle.png'])+'  idleFramesPerSecond: 2\n'+refs('attackFrames',[f'{name}Attack{i}.png' for i in range(1,9)])+(refs('hitFrames',[f'GoblinHit{i}.png' for i in range(1,6)]) if name=='Goblin' else '  hitFrames: []\n')+'  hitReactionDuration: 0.21\n  attackWeaponFrames: []\n  useWeaponAttachments: 0\n'
        text=text[:actor.start()]+actor[1]+fields+text[actor.end():]
        renderer=re.search(r'--- !u!212 &'+body_id+r'\n.*?(?=--- !u!)',text,re.S);assert renderer
        updated=re.sub(r'  m_Sprite: .*','  m_Sprite: {fileID: 21300000, guid: '+guid(name+'Idle.png')+', type: 3}',renderer[0])
        updated=re.sub(r'  m_Size: .*','  m_Size: {x: 7.68, y: 5.9733333}',updated)
        text=text[:renderer.start()]+updated+text[renderer.end():]
        target=PREFABS/f'{name}2D.prefab';target.write_text(text)
        Path(str(target)+'.meta').write_text('fileFormatVersion: 2\nguid: '+guid(target.name)+'\nPrefabImporter:\n  externalObjects: {}\n')
        # Keep the local root fileID from the cloned normal/boss prefab variant.
        main_prefab=re.sub(r'(  '+slot+r': \{fileID: \d+, guid: )\w+',lambda m:m[1]+guid(target.name),main_prefab)
    (PREFABS/'PaperBattle.prefab').write_text(main_prefab)
    print('Installed Goblin2D normal and Hobgoblin2D boss, with eight-frame animation assets; gameplay prefab blocks preserved.')

if __name__=='__main__':main()
