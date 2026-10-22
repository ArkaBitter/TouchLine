"""Render the local player manual without external packages."""
from pathlib import Path
import html
import re
import sys

root = Path(__file__).resolve().parents[1]
english = '--english' in sys.argv
source = root / ('GameManual.md' if english else '游戏说明书.md')
text = source.read_text(encoding='utf-8-sig')
tactics = re.findall(r'\{"([^"]+)","([^"]+)"\}', (root / 'src/RuleDescriptions.cs').read_text(encoding='utf-8-sig').split('Dictionary<string,string> Tactic=')[1])
table = '| 战术 | 效果 |\n| --- | --- |\n' + '\n'.join(f'| {name} | {effect} |' for name, effect in tactics)
if '<!-- TACTICS -->' in text:
    text = text.replace('<!-- TACTICS -->', table)
    source.write_text(text, encoding='utf-8')

def inline(line):
    return re.sub(r'`([^`]+)`', r'<code>\1</code>', html.escape(line))

blocks = []
in_table = False
in_list = False
for line in text.splitlines():
    is_table = line.startswith('|')
    is_list = line.startswith('- ')
    if in_table and not is_table:
        blocks.append('</tbody></table></div>')
        in_table = False
    if in_list and not is_list:
        blocks.append('</ul>')
        in_list = False
    if not line.strip():
        continue
    if is_table:
        if re.fullmatch(r'[| :\-]+', line):
            continue
        cells = line.strip('|').split('|')
        if not in_table:
            blocks.append('<div class="table"><table><thead><tr>' + ''.join('<th>'+inline(c.strip())+'</th>' for c in cells)+'</tr></thead><tbody>')
            in_table = True
        else:
            blocks.append('<tr>'+''.join('<td>'+inline(c.strip())+'</td>' for c in cells)+'</tr>')
    elif line.startswith('#'):
        level = len(line)-len(line.lstrip('#'))
        blocks.append(f'<h{level}>'+inline(line[level:].strip())+f'</h{level}>')
    elif is_list:
        if not in_list:
            blocks.append('<ul>')
            in_list = True
        blocks.append('<li>'+inline(line[2:])+'</li>')
    else:
        blocks.append('<p>'+inline(line)+'</p>')
out = '''<!doctype html><html lang="zh-CN"><meta charset="utf-8"><meta name="viewport" content="width=device-width, initial-scale=1"><title>触线足球经理 · 游戏说明书</title>
<style>body{background:#101b29;color:#e4ecf5;font:17px/1.85 "Microsoft YaHei",sans-serif;margin:0}article{max-width:1100px;margin:auto;padding:40px 28px}h1,h2{color:#64dfc2}h2{margin-top:48px;border-bottom:1px solid #33465c}table{border-collapse:collapse;width:100%;font-size:15px}th,td{border:1px solid #3c4e60;padding:10px;text-align:left}th{background:#23374a}.table{overflow-x:auto}code{background:#23374a;padding:2px 5px}li{margin:8px 0}@media print{body{background:white;color:black}h1,h2{color:black}th{background:#eee}}</style><article>'''+ '\n'.join(blocks)+'</article></html>'
if english:
    out=out.replace('lang="zh-CN"','lang="en"').replace('触线足球经理 · 游戏说明书','Touchline Football Manager — Game Manual')
(root/('GameManual.html' if english else '游戏说明书.html')).write_text(out,encoding='utf-8')
print(f'Manual rendered: {len(tactics)} tactics')
