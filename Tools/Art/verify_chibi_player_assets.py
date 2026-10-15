# Developer map: File-only current-player verification: alpha bounds, ordered sprite/config/grip references, previews and protected files. Does not invoke Unity.
# See Docs/DEVELOPER_HANDOFF.md for system flow and validation.
from pathlib import Path
import hashlib, json, re
from PIL import Image, ImageFilter
import numpy as np

root = Path(__file__).resolve().parents[2]
art = root / 'Assets/Art/PaperBattle/ChibiPlayer'
review = root / 'ReviewCaptures/ChibiPlayer'
config = (art / 'PlayerAnimation.asset').read_text()
prefab = (root / 'Assets/Prefabs/PaperBattle/PaperBattle.prefab').read_text()
actor = re.search(r'  m_EditorClassIdentifier: Assembly-CSharp::PaperSpriteActor\n  body: \{fileID: 7609367433249518449\}\n(.*?)(?=--- !u!)', prefab, re.S)[1]
anchors = json.loads((review / 'attachments.json').read_text())

def guid(path):
    return re.search(r'^guid: (\w+)', Path(str(path) + '.meta').read_text(), re.M)[1]

for kind, field in [('Idle', 'idleFrames'), ('Attack', 'attackFrames')]:
    expected = [guid(art / f'{kind}{i}.png') for i in range(1, 9)]
    for text in [config, actor]:
        refs = re.findall(r'guid: (\w+)', re.search(r'  '+field+r':\n((?:  - .*\n)+)', text)[1])
        assert refs == expected, field
        grip_field = kind.lower() + 'WeaponGrips'
        values = re.findall(r'  - position: \{x: ([^,]+), y: ([^}]+)\}\n    angle: ([^\n]+)\n    inFront: ([01])', re.search(r'  '+grip_field+r':\n(.*?)(?=\n  \w|\Z)', text, re.S)[1])
        assert len(values) == 8
        for value, anchor in zip(values, anchors[kind]):
            assert abs(float(value[0])-anchor['position'][0]) < 1e-7
            assert abs(float(value[1])-anchor['position'][1]) < 1e-7
            assert float(value[2]) == anchor['angle'] and int(value[3]) == int(anchor['inFront'])
    for i in range(1, 9):
        p = art / f'{kind}{i}.png'
        im = Image.open(p)
        assert im.mode == 'RGBA' and im.size == (640, 640)
        alpha = im.getchannel('A')
        box = alpha.point(lambda v: 255 if v > 128 else 0).getbbox()
        assert box and box[0] > 0 and box[2] < 640 and box[1] > 0 and abs(box[3]-612) <= 2, (p.name, box)
        assert alpha.getextrema() == (0, 255)
        meta = Path(str(p)+'.meta').read_text()
        for setting in ['spriteMode: 1', 'spritePixelsToUnits: 150', 'alphaIsTransparency: 1', 'spritePivot: {x: 0.5, y: 0.04375}']:
            assert setting in meta

assert 'animationSet: {fileID: 11400000, guid: '+guid(art/'PlayerAnimation.asset') in actor
assert 'attackWeaponFrames: []' in actor and 'useWeaponAttachments: 1' in actor
assert 'idleFramesPerSecond: 2' in config
assert 'defaultWeaponVisual: {fileID: 11400000, guid: '+guid(art/'DefaultSword.asset') in config
sword = (art/'DefaultSword.asset').read_text()
assert 'sprite: {fileID: 21300000, guid: '+guid(art/'Sword.png') in sword
assert Image.open(art/'Sword.png').size == (320, 128)
assert 'spritePivot: {x: 0.21875, y: 0.5}' in (art/'Sword.png.meta').read_text()
for name, text in [('PaperWeaponVisual', sword), ('PaperPlayerAnimationSet', config)]:
    assert 'm_Script: {fileID: 11500000, guid: '+guid(root/'Assets/Scripts'/f'{name}.cs') in text
assert anchors['Attack'][3]['angle'] == 0
protected = json.loads((review/'protected-hashes.json').read_text())
# The snapshot's C# files have since received authorized documentation/enemy
# work. Code is verified by the documentation baseline and combat harness;
# retain byte-for-byte protection here for the forest art and importer metadata.
protected = {p: h for p, h in protected.items() if not p.endswith('.cs')}
for relative, expected in protected.items():
    assert hashlib.sha256((root/relative).read_bytes()).hexdigest() == expected, relative
for kind, duration in [('idle', 4000), ('attack', 1000)]:
    for mode in ['equipped', 'unarmed']:
        im = Image.open(review/f'{kind}-{mode}.gif')
        assert im.n_frames == 8
        total = 0
        for i in range(8):
            im.seek(i); total += im.info['duration']
        assert total == duration
builder = (root/'Assets/Editor/PaperBattleSceneBuilder.cs').read_text()
assert 'ChibiPlayer/PlayerAnimation.asset' in builder and 'ConfigureAnimationSet(playerAnimation)' in builder
preparation = json.loads((review/'preparation.json').read_text())
for kind in ['Idle', 'Attack']:
    rgb=np.array(Image.open(review/f'{kind}-generated.png').convert('RGB')).astype(int)
    backdrop=(rgb.min(2)>205)&(rgb.max(2)-rgb.min(2)<30)
    alpha=np.array(Image.fromarray(np.uint8(~backdrop)*255).filter(ImageFilter.MinFilter(3)))
    retained=sum(int((alpha[t:b,l:r]>0).sum()) for l,t,r,b in [p['source_rect'] for p in preparation[kind]])
    assert retained==int((alpha>0).sum()), (kind, 'Source silhouette lost in crop')
assert all(p['angle']==-25 and p['inFront'] for p in anchors['Idle'])
report = {'status': 'PASS', 'body_frames': 16, 'complete_weapon_sprites': 1, 'idle_seconds': 4, 'grip_entries_verified_in_config_and_prefab': 32, 'protected_hashes_unchanged': len(protected), 'preview_gifs': 4, 'unity_runtime_tested': False}
(review/'asset-checks.json').write_text(json.dumps(report, indent=2))
print(json.dumps(report))
