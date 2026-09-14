# Developer map: Copies six external forest source PNGs and writes metadata/background prefab references. Read the source folder before running; generation itself is unnecessary for already installed images.
# See Docs/DEVELOPER_HANDOFF.md for system flow and validation.
"""Copy user-supplied forest PNGs unchanged and wire the existing paper background."""
from pathlib import Path
import hashlib,json,re,shutil,uuid
from PIL import Image,ImageDraw
root=Path(__file__).resolve().parents[2]
source=Path('D:/Documents/BlackCubeAssets/Level Art/Forest/Forest 1')
art=root/'Assets/Art/PaperBattle/ForestCycle';art.mkdir(exist_ok=True)
review=root/'ReviewCaptures/ForestCycle';review.mkdir(exist_ok=True)
percentages=[0,20,40,60,80,100]
def guid(name):return uuid.uuid5(uuid.NAMESPACE_URL,'black-cube/forest-cycle/'+name).hex
template=(root/'Assets/Art/PaperBattle/PlayerAttack/PlayerAttack1.png.meta').read_text()
template=re.sub(r'  internalIDToNameTable:.*?  externalObjects:', '  internalIDToNameTable: []\n  externalObjects:',template,flags=re.S)
template=re.sub(r'  spriteSheet:.*?(?=  spritePackingTag:|  pSDRemoveMatte:|  userData:)', '  spriteSheet:\n    serializedVersion: 2\n    sprites: []\n    outline: []\n    physicsShape: []\n    bones: []\n    spriteID: PLACEHOLDER\n    internalID: 21300000\n    vertices: []\n    indices: \n    edges: []\n    weights: []\n    secondaryTextures: []\n',template,flags=re.S)
contact=Image.new('RGB',(3*480,2*302),(20,25,24));records=[]
for i,pct in enumerate(percentages):
    name=f'{pct}_Percent.png';shutil.copyfile(source/name,art/name);im=Image.open(art/name)
    before=hashlib.sha256((source/name).read_bytes()).hexdigest();after=hashlib.sha256((art/name).read_bytes()).hexdigest();assert before==after
    meta=re.sub(r'^guid: .*$', 'guid: '+guid(name),template,flags=re.M)
    meta=re.sub(r'  spritePivot: .*','  spritePivot: {x: 0.5, y: 0.5}',meta)
    meta=re.sub(r'  spritePixelsToUnits: .*','  spritePixelsToUnits: '+str(im.height/10),meta)
    meta=meta.replace('PLACEHOLDER',guid(name+'/sprite'))
    (art/(name+'.meta')).write_text(meta,newline='\n')
    tile=im.convert('RGB').resize((480,270),Image.Resampling.LANCZOS);x=(i%3)*480;y=(i//3)*302;contact.paste(tile,(x,y))
    ImageDraw.Draw(contact).text((x+12,y+280),f'Forest {i+1} | {pct}% | Levels {i*10+1}-{i*10+10}',fill='white')
    records.append({'forest':i+1,'percent':pct,'first_levels':[i*10+1,i*10+10],'size':im.size,'sha256':after,'source_unchanged':True})
(art.parent/'ForestCycle.meta').write_text('fileFormatVersion: 2\nguid: '+guid('folder')+'\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n',newline='\n')
prefab=root/'Assets/Prefabs/PaperBattle/PaperBattle.prefab';text=prefab.read_text()
pattern=r'(  m_EditorClassIdentifier: Assembly-CSharp::ZoneManager\n)(.*?)(?=--- !u!)';m=re.search(pattern,text,re.S);assert m
fields=re.sub(r'  paperBackground:.*?((?=  generate3DScenery:)|$)','',m[2],flags=re.S)
refs='  paperBackground: {fileID: 853234911179518672}\n  forestBackgrounds:\n'+''.join('  - {fileID: 21300000, guid: '+guid(f'{pct}_Percent.png')+', type: 3}\n' for pct in percentages)
text=text[:m.start()]+m[1]+refs+fields+text[m.end():]
pattern=r'(--- !u!212 &853234911179518672\n.*?)(?=--- !u!)';m=re.search(pattern,text,re.S);assert m
renderer=re.sub(r'  m_Sprite: .*','  m_Sprite: {fileID: 21300000, guid: '+guid('0_Percent.png')+', type: 3}',m[0])
text=text[:m.start()]+renderer+text[m.end():];prefab.write_text(text,newline='\n')
contact.save(review/'forest-cycle-contact.png')
(review/'asset-checks.json').write_text(json.dumps({'cycle_levels':60,'levels_per_image':10,'records':records},indent=2))
print(json.dumps({'copied_unchanged':6,'sprite_ppu':94.1,'cycle_levels':60,'preview':str(review/'forest-cycle-contact.png')}))
