# Developer map: Legacy review-only replacement of recovery panel six; writes comparison images, not the current player configuration.
# See Docs/DEVELOPER_HANDOFF.md for system flow and validation.
"""Replace only approved-for-edit review panel 6, preserving other pixels."""
from pathlib import Path
import json
import numpy as np
from PIL import Image

out=Path(__file__).resolve().parents[2]/'ReviewCaptures/PlayerAttack'
original=Image.open(out/'carry-grip-review.png').convert('RGB')
edited=Image.open(out/'recovery-six-generated-source.png').convert('RGB')
assert original.size==edited.size==(1536,1024)
box=(393,501,768,943)
result=original.copy();result.paste(edited.crop(box),box[:2])
a=np.array(original);b=np.array(result)
mask=np.ones(a.shape[:2],bool);mask[box[1]:box[3],box[0]:box[2]]=False
assert np.array_equal(a[mask],b[mask])
assert not np.array_equal(a[~mask],b[~mask])
result.save(out/'recovery-six-review.png')
result.crop((20,500,1145,943)).save(out/'recovery-five-six-seven-detail.png')
(out/'recovery-six-checks.json').write_text(json.dumps({'changed_panel':6,'edit_rectangle':box,'all_pixels_outside_panel_6_identical':True,'review_only':True,'implemented':False},indent=2))
print(str(out/'recovery-six-review.png'))
