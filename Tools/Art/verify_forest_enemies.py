# Developer map: Checks enemy sprite references/alpha, cloned gameplay blocks, normal/boss slots, unchanged progression logic and preview durations; writes a common-scale lineup and report.
# See Docs/DEVELOPER_HANDOFF.md for system flow and validation.
"""Check enemy art, prefab roles and untouched gameplay blocks without Unity.
Also builds a common-scale lineup so player/goblin/boss proportions are reviewable.
"""
from pathlib import Path
import hashlib,json,re
from PIL import Image,ImageDraw

ROOT=Path(__file__).resolve().parents[2];ART=ROOT/'Assets/Art/PaperBattle/ForestEnemies'
PREFABS=ROOT/'Assets/Prefabs/PaperBattle';REVIEW=ROOT/'ReviewCaptures/ForestEnemies'
def guid(path):return re.search(r'^guid: (\w+)',Path(str(path)+'.meta').read_text(),re.M)[1]
def blocks(text):return re.split(r'(?=^--- !u!)',text,flags=re.M)

def main():
    main=(PREFABS/'PaperBattle.prefab').read_text()
    setup=json.loads((REVIEW/'preparation.json').read_text());count=0
    for name,old,slot,hp,boss in [('Goblin','Ghoul2D','normalEnemyPrefab',250,0),('Hobgoblin','GhoulBoss2D','bossEnemyPrefab',500,1)]:
        prefab=(PREFABS/f'{name}2D.prefab').read_text();legacy=(PREFABS/f'{old}.prefab').read_text()
        root_id=re.search(r'--- !u!1 &(\d+) stripped',prefab)[1]
        assert f'{slot}: {{fileID: {root_id}, guid: {guid(PREFABS/(name+"2D.prefab"))}, type: 3}}' in main
        # Every non-actor MonoBehaviour is copied exactly, preserving serialized
        # health, role, stats, EnemyAI, status and damage settings from its old slot.
        def gameplay(text):return [b for b in blocks(text) if b.startswith('--- !u!114') and 'Assembly-CSharp::PaperSpriteActor' not in b]
        assert gameplay(prefab)==gameplay(legacy),name+' gameplay prefab changed'
        assert f'  maxLife: {hp}\n' in prefab and f'  isBoss: {boss}\n' in prefab
        config=(ART/f'{name}Animation.asset').read_text()
        assert 'enemyAnimationSet: {fileID: 11400000, guid: '+guid(ART/f'{name}Animation.asset') in prefab
        assert f'displayName: {name}' in config
        assert 'm_Script: {fileID: 11500000, guid: '+guid(ROOT/'Assets/Scripts/PaperEnemyAnimationSet.cs') in config
        expected=[guid(ART/f'{name}Attack{i}.png') for i in range(1,9)]
        for text in [config,prefab]:
            found=re.findall(r'guid: (\w+)',re.search(r'  attackFrames:\n((?:  - .*\n)+)',text)[1]);assert found==expected
        for suffix in ['Idle']+[f'Attack{i}' for i in range(1,9)]:
            path=ART/f'{name}{suffix}.png';im=Image.open(path)
            assert im.mode=='RGBA' and im.size==(1152,896)
            alpha=im.getchannel('A');box=alpha.getbbox();assert box and 0<box[0]<box[2]<1152 and 0<box[1]<box[3]<896
            assert alpha.getextrema()[0]==0 and alpha.getextrema()[1]>=250
            meta=Path(str(path)+'.meta').read_text()
            for setting in ['spriteMode: 1','spriteMeshType: 0','spritePixelsToUnits: 150','alphaIsTransparency: 1']:
                assert setting in meta,(path.name,setting)
            pivot=re.search(r'spritePivot: \{x: ([^,]+), y: ([^}]+)\}',meta)
            assert pivot and abs(float(pivot[1])-0.5)<1e-7 and abs(float(pivot[2])-(80/896))<1e-7,(path.name,'spritePivot')
            count+=1
        assert (ART/f'{name}Idle.png').read_bytes()==(ART/f'{name}Attack1.png').read_bytes()==(ART/f'{name}Attack8.png').read_bytes()
        preview=Image.open(REVIEW/f'{name.lower()}-attack.gif');duration=0
        assert preview.n_frames==8
        for i in range(8):preview.seek(i);duration+=preview.info['duration']
        assert duration==1000
    assert setup['Hobgoblin']['world_height']>setup['Goblin']['world_height']
    builder=(ROOT/'Assets/Editor/PaperBattleSceneBuilder.cs').read_text()
    assert 'MakeEnemy("Goblin2D", false, goblin)' in builder and 'MakeEnemy("Hobgoblin2D", true, hobgoblin)' in builder
    # Enemy stats and equipment remain untouched; the authorized stage10 boss
    # quota is exercised separately by verify_boss_cadence.ps1.
    import sys
    sys.path.insert(0,str(ROOT/'Tools'))
    from document_first_party import fingerprint
    baseline=json.loads((ROOT/'Docs/documentation-code-baseline.json').read_text())
    for rel in ['Assets/Scripts/EnemyAI.cs','Assets/Scripts/EnemyStatSetup.cs','Assets/Scripts/EquipmentManager.cs','Assets/Scripts/PlayerController.cs']:
        assert fingerprint(Path(rel),(ROOT/rel).read_text())==baseline[rel],rel+' gameplay changed'
    canvas=Image.new('RGB',(1600,850),(104,134,113));draw=ImageDraw.Draw(canvas)
    for label,path,x,pivot in [('Player',ROOT/'Assets/Art/PaperBattle/ChibiPlayer/Idle1.png',300,(320,612)),('Goblin',ART/'GoblinIdle.png',800,(576,816)),('Hobgoblin boss',ART/'HobgoblinIdle.png',1320,(576,816))]:
        im=Image.open(path);canvas.paste(im,(x-pivot[0],740-pivot[1]),im);draw.text((x-35,790),label,fill='white')
    canvas.save(REVIEW/'character-lineup.png')
    assert 'enemiesToKillBeforeBoss: 9' in main
    report={'status':'PASS','transparent_pngs':count,'attack_frames_per_enemy':8,'normal':'Goblin2D','boss':'Hobgoblin2D','normal_hp':250,'boss_hp':500,'gameplay_prefab_blocks_unchanged':True,'normal_stages':[1,9],'boss_stage':10,'unity_runtime_tested':False}
    (REVIEW/'asset-checks.json').write_text(json.dumps(report,indent=2));print(json.dumps(report))

if __name__=='__main__':main()
