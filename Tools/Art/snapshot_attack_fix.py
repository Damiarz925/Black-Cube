# Developer map: Archives legacy attack cels and prepares edit references before forward-strike cleanup.
# See Docs/DEVELOPER_HANDOFF.md for system flow and validation.
from pathlib import Path
import shutil,json,hashlib
from PIL import Image
root=Path(__file__).resolve().parents[2]
out=root/'ReviewCaptures/PlayerAttack/ForwardStrikeFix';out.mkdir(exist_ok=True)
snap=out/'Before';snap.mkdir(exist_ok=True)
art=root/'Assets/Art/PaperBattle/PlayerAttack'
for p in art.glob('*.png'):
    if not (snap/p.name).exists():shutil.copyfile(p,snap/p.name)
unchanged=list((root/'Assets/Art/PaperBattle/PlayerIdleConsistent').glob('*'))
unchanged += [root/'Assets/Scripts/BattleManager.cs',root/'Assets/Scripts/PaperSpriteActor.cs',root/'Assets/Prefabs/PaperBattle/PaperBattle.prefab']
record={str(p.relative_to(root)):hashlib.sha256(p.read_bytes()).hexdigest() for p in unchanged if p.is_file()}
if not (out/'unchanged-before.json').exists():(out/'unchanged-before.json').write_text(json.dumps(record,indent=2))
for i in [4]:
    full=Image.alpha_composite(Image.open(snap/f'PlayerAttack{i}.png'),Image.open(snap/f'PlayerWeapon{i}.png'))
    white=Image.new('RGB',full.size,'white');white.paste(full,(0,0),full);white.save(out/f'edit-frame-{i}.png')
print(str(out))
