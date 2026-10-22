"""Create and verify an offline judge package without personal saves or test data."""
import hashlib
import json
from pathlib import Path
import zipfile

root = Path(__file__).resolve().parent.parent
destination = root / '提交包'
destination.mkdir(exist_ok=True)
archive = destination / 'TouchLine_参赛离线运行包.zip'
files = [root / name for name in (
    '启动游戏.cmd', '参赛运行说明.txt', '游戏说明书.html', 'GameManual.html',
    '触线足球经理项目报告_最新版.docx', 'build/TouchlineManager.exe',
)]
files += sorted(f for f in (root / 'LocalAI').rglob('*') if f.is_file())
assert all(f.is_file() for f in files)
assert not any(f.suffix.lower() in ('.tmsave', '.bak', '.log', '.zip') for f in files)
assert len(files) == len(set(files))
with zipfile.ZipFile(archive, 'w', zipfile.ZIP_DEFLATED, compresslevel=6, allowZip64=True) as package:
    for f in files:
        if f.stat().st_size > 100_000_000:
            print('Compressing ' + f.relative_to(root).as_posix(), flush=True)
        package.write(f, 'FootballManager/' + f.relative_to(root).as_posix())
with zipfile.ZipFile(archive) as package:
    assert package.testzip() is None, 'CRC validation failed'
    names = set(package.namelist())
    assert names == {'FootballManager/' + f.relative_to(root).as_posix() for f in files}
    assert not any('test-results/' in n or n.endswith('.tmsave') for n in names)
    for relative in ('build/TouchlineManager.exe', 'LocalAI/qwen2.5-1.5b-instruct-q4_k_m.gguf'):
        source_hash = hashlib.file_digest((root / relative).open('rb'), 'sha256').hexdigest()
        with package.open('FootballManager/' + relative) as stream:
            assert hashlib.file_digest(stream, 'sha256').hexdigest() == source_hash
size = archive.stat().st_size
assert size < 2_000_000_000, 'Package exceeds the stricter decimal 2GB limit'
result = dict(Archive=archive.name, Bytes=size, DecimalGB=round(size / 1e9, 3),
              FileCount=len(files), UncompressedBytes=sum(f.stat().st_size for f in files),
              CRCVerified=True, CoreFilesSHA256Verified=True, UnderDecimal2GB=True,
              SHA256=hashlib.file_digest(archive.open('rb'), 'sha256').hexdigest())
(destination / '打包验证.json').write_text(json.dumps(result, ensure_ascii=False, indent=2), encoding='utf-8')
print(json.dumps(result, ensure_ascii=True), flush=True)
