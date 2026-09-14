# Developer map: Checks source PNG identity and forest metadata/prefab ordering; writes a JSON result. Requires the recorded external forest source directory.
# See Docs/DEVELOPER_HANDOFF.md for system flow and validation.
from pathlib import Path
import hashlib,json,re
from PIL import Image
root=Path(__file__).resolve().parents[2]
art=root/'Assets/Art/PaperBattle/ForestCycle'
source=Path('D:/Documents/BlackCubeAssets/Level Art/Forest/Forest 1')
prefab=(root/'Assets/Prefabs/PaperBattle/PaperBattle.prefab').read_text()
zone=re.search(r'  m_EditorClassIdentifier: Assembly-CSharp::ZoneManager\n(.*?)(?=--- !u!)',prefab,re.S)[1]
refs=re.findall(r'guid: (\w+)',re.search(r'  forestBackgrounds:\n((?:  - .*\n)+)',zone)[1])
assert len(refs)==6 and len(set(refs))==6
for i,pct in enumerate([0,20,40,60,80,100]):
    p=art/f'{pct}_Percent.png';m=p.with_suffix('.png.meta').read_text()
    assert hashlib.sha256(p.read_bytes()).digest()==hashlib.sha256((source/p.name).read_bytes()).digest()
    assert Image.open(p).size==(1672,941)
    assert re.search(r'^guid: (\w+)',m,re.M)[1]==refs[i]
    assert 'spriteMode: 1' in m and 'spritePixelsToUnits: 94.1' in m and 'spritePivot: {x: 0.5, y: 0.5}' in m
    assert 'enableMipMap: 0' in m and 'textureCompression: 0' in m and 'nPOTScale: 0' in m
assert 'paperBackground: {fileID: 853234911179518672}' in zone and 'generate3DScenery: 0' in zone
renderer=re.search(r'--- !u!212 &853234911179518672\n(.*?)(?=--- !u!)',prefab,re.S)[1]
assert 'm_Sprite: {fileID: 21300000, guid: '+refs[0]+', type: 3}' in renderer
assert 'm_DrawMode: 0' in renderer
report={'status':'PASS','original_pngs_byte_identical':6,'ordered_background_references_valid':True,'initial_background_percent':0,'image_size':[1672,941],'sprite_world_height':10,'ppu':94.1,'levels_per_forest':10,'cycle_levels':60,'unity_runtime_tested':False}
(root/'ReviewCaptures/ForestCycle/installation-checks.json').write_text(json.dumps(report,indent=2))
print(json.dumps(report))
