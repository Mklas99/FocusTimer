from pathlib import Path
from collections import Counter
import hashlib
import json
import re
import zipfile
from PIL import Image

root = Path(__file__).resolve().parents[2]
change = root / 'openspec/changes/adopt-dense-frost-design-system'
docs = list(change.rglob('*.md')) + [root / p for p in (
    'docs/versions/current/Features.md',
    'docs/versions/current/OpenIssues.md',
    'docs/versions/current/OI-21-SettingsRedesign.md',
)]
missing = []
local_links = 0
for doc in docs:
    for target in re.findall(r'\[[^\]]*\]\(([^)]+)\)', doc.read_text(encoding='utf-8-sig')):
        if target.startswith(('https://', 'http://', '#')):
            continue
        local_links += 1
        if not (doc.parent / target.split('#')[0]).resolve().exists():
            missing.append({'document': str(doc.relative_to(root)), 'target': target})

attachment = root / '.codex-remote-attachments/01a10c2b-f615-79b1-9287-5a5b849d4411'
with zipfile.ZipFile(attachment / '759e35d1-eef5-4f72-9331-f24f3f95b16d/1-FocusTimer-screenshots.zip') as archive:
    original_hashes = Counter(hashlib.sha256(archive.read(item)).hexdigest() for item in archive.infolist() if Path(item.filename).suffix.lower() in ('.png', '.jpg', '.jpeg'))
baseline_hashes = Counter(hashlib.sha256(p.read_bytes()).hexdigest() for p in (change / 'references/baseline').iterdir())
navigation_matches = (change / 'references/navigation-user-reference.jpg').read_bytes() == (attachment / '4ef50e7d-df74-444e-857d-6ede41377777/1-Screenshot-2026-10-05-164734.jpg').read_bytes()
images = []
for image in sorted((change / 'references').rglob('*')):
    if image.suffix.lower() in ('.png', '.jpg', '.jpeg'):
        with Image.open(image) as data:
            data.verify()
        with Image.open(image) as data:
            images.append({'file': str(image.relative_to(change)), 'width': data.width, 'height': data.height})
tasks = re.findall(r'^- \[([^\]]*)\] (\d+\.\d+) ', (change / 'tasks.md').read_text(encoding='utf-8'), re.M)
result = {'local_links_checked': local_links, 'missing_links': missing, 'original_images_identical': original_hashes == baseline_hashes, 'navigation_identical': navigation_matches, 'images': images, 'implementation_tasks': len(tasks), 'completed_tasks': sum(marker.strip().lower() == 'x' for marker, _ in tasks)}
print(json.dumps(result, indent=2))
assert not missing
assert original_hashes == baseline_hashes
assert navigation_matches
assert len(images) == 18
assert tasks and not result['completed_tasks']
