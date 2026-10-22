"""Audit project copies against an external private save; report counts, never names."""
import json
import re
import sys
import zipfile
from pathlib import Path

root = Path(__file__).resolve().parents[1]
private = Path(sys.argv[1]).resolve()
assert not private.is_relative_to(root), 'Private save must be outside the project'
envelope = json.loads(private.read_text(encoding='utf-8-sig'))
world = json.loads(envelope['Json'])
names = {entity['Name'] for entity in world['Clubs'] + world['Players']}
failures = []
checked = 0

def roster_matches(value):
    if isinstance(value, dict):
        return int(isinstance(value.get('Name'), str) and value['Name'] in names) + sum(roster_matches(v) for v in value.values())
    if isinstance(value, list):
        return sum(roster_matches(v) for v in value)
    if isinstance(value, str) and value.startswith(('{', '[')):
        try:
            return roster_matches(json.loads(value))
        except ValueError:
            pass
    return 0

patterns = [re.compile(rb'"Name"\s*:\s*' + re.escape(json.dumps(n, ensure_ascii=False).encode('utf-8'))) for n in names]
for path in root.rglob('*'):
    if not path.is_file() or 'LocalAI' in path.relative_to(root).parts:
        continue
    checked += 1
    suffix = path.suffix.lower()
    if suffix in ('.exe', '.dll'):
        data = path.read_bytes()
        if any(pattern.search(data) for pattern in patterns):
            failures.append(str(path.relative_to(root)))
    if suffix in ('.json', '.jsonl', '.tmsave', '.bak', '.tmp'):
        try:
            text = path.read_text(encoding='utf-8-sig')
            try:
                count = roster_matches(json.loads(text))
            except ValueError:
                count = sum(roster_matches(json.loads(line)) for line in text.splitlines() if line.strip())
            if count:
                failures.append(str(path.relative_to(root)))
        except (ValueError, UnicodeError):
            pass
    if suffix == '.zip':
        with zipfile.ZipFile(path) as archive:
            if any('current_world.json' in entry.filename for entry in archive.infolist()):
                failures.append(str(path.relative_to(root)))

for obsolete in ('data/current_world.json', 'data/source_manifest.json', '原版代码归档.zip', 'tools/import_current.py'):
    if (root / obsolete).exists():
        failures.append(obsolete)
report = dict(Passed=not failures, CheckedFiles=checked, PrivateSaveOutsideProject=True,
              OriginalClubCount=len(world['Clubs']), OriginalPlayerCount=len(world['Players']),
              ResidualRosterFiles=sorted(set(failures)),
              Scope='Project roster records, embedded builds, known legacy snapshot/archive; third-party model weights excluded')
out = root / 'build/test-results/private-world/privacy-audit.json'
out.parent.mkdir(parents=True, exist_ok=True)
out.write_text(json.dumps(report, ensure_ascii=True, indent=2), encoding='utf-8')
print(json.dumps(report, ensure_ascii=True))
sys.exit(0 if not failures else 1)
