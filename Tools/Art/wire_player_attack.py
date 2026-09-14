# Developer map: Legacy PlayerAttack reference installer that rewrites the player prefab; superseded by install_chibi_player.py.
# See Docs/DEVELOPER_HANDOFF.md for system flow and validation.
from pathlib import Path
import re, uuid
root=Path(__file__).resolve().parents[2]
def guid(name):return uuid.uuid5(uuid.NAMESPACE_URL,'black-cube/player-attack/'+name).hex
prefab=root/'Assets/Prefabs/PaperBattle/PaperBattle.prefab'
text=prefab.read_text()
pattern=r'(  m_EditorClassIdentifier: Assembly-CSharp::PaperSpriteActor\n  body: \{fileID: 7609367433249518449\}\n)(.*?)(?=--- !u!)'
m=re.search(pattern,text,re.S);assert m
actor=re.sub(r'  attackFrames:.*','',m[2],flags=re.S)
for field,prefix in [('attackFrames','PlayerAttack'),('attackWeaponFrames','PlayerWeapon')]:
    actor+='  '+field+':\n'
    for i in range(1,9):
        name=f'{prefix}{i}.png';assert (root/'Assets/Art/PaperBattle/PlayerAttack'/name).is_file()
        actor+='  - {fileID: 21300000, guid: '+guid(name)+', type: 3}\n'
text=text[:m.start()]+m[1]+actor+text[m.end():]
prefab.write_text(text,newline='\n')
print('Wired8 body and8 separate blade cels into main-checkout player prefab.')
