from pathlib import Path
import json
import statistics

root = Path(__file__).resolve().parents[1] / 'build/test-results/inference-20261021'
all_rows = []
summary = []
for backend in ('cpu', 'gpu'):
    rows = json.loads((root / f'{backend}-results.json').read_text(encoding='utf-8-sig'))
    all_rows.extend(rows)
    for variant in dict.fromkeys(r['Variant'] for r in rows):
        group = [r for r in rows if r['Variant'] == variant]
        summary.append(dict(backend=backend, variant=variant, decisions=len(group),
                            seconds=sum(r['Seconds'] for r in group),
                            mean=statistics.mean(r['Seconds'] for r in group),
                            median=statistics.median(r['Seconds'] for r in group),
                            input=sum(r['InputTokens'] for r in group), output=sum(r['OutputTokens'] for r in group),
                            retries=sum(r['Calls'] - 1 for r in group), fallbacks=sum(r['Fallback'] for r in group),
                            failures=sum(bool(r['Error']) or not r['Applied'] for r in group)))
runtime = json.loads((root / 'runtime-tests.json').read_text(encoding='utf-8-sig'))
data = dict(groups=summary, runtime=runtime, logical_decisions=len(all_rows),
            requests=sum(r['Calls'] for r in all_rows), full_season=False,
            published_policy='CUDA preferred with CPU fallback; lossless compact input; terse output only on owned bundled GPU; existing prompt cache retained; background model startup only, no cached decisions')
(root / 'summary.json').write_text(json.dumps(data, ensure_ascii=False, indent=2), encoding='utf-8')
lines = ['# 本地推理提速专项报告', '',
         '日期：2026-10-21。RTX 4070 Laptop 8GB、i7-14650HX；同一Qwen2.5-1.5B-Instruct Q4_K_M，llama.cpp b11719。CUDA 12.4运行包经官方SHA256校验，使用项目内复制的CUDA 12.5兼容依赖。', '',
         '五种快照：六周规划、续约、需求转会、玩家比赛赛前、中场。每种配置每场景两次，固定种子，第二轮反转配置顺序；CPU与GPU顺序运行。模型启动另计。除明确缓存组外禁用提示缓存，便于拆分变量；这与游戏原来省略参数时服务默认启用缓存不同。没有推进任何比赛或写入正式存档。', '',
         '| 后端 | 配置 | 决定数 | 合计秒 | 平均秒 | 输入token | 输出token | 纠正/兜底/失败 |',
         '| --- | --- | ---: | ---: | ---: | ---: | ---: | --- |']
for row in summary:
    lines.append(f"| {row['backend']} | {row['variant']} | {row['decisions']} | {row['seconds']:.2f} | {row['mean']:.2f} | {row['input']} | {row['output']} | {row['retries']}/{row['fallbacks']}/{row['failures']} |")
lines += ['', '配置含义：baseline为原输入输出；compact为所有输入事实保留、重复对象转列名和数据行；short为去掉模型reason字段；combined为两者合用；combined-cache显式启用提示缓存；combined-default-cache省略缓存参数；selective-cache只对赛前和中场精简输出。', '',
          '## 接入结果', '',
          '- GPU：采用。有兼容CUDA设备和已打包运行程序时优先GPU，启动失败会记录原因并重试CPU模型，玩家对手不会切换成固定教练。',
          '- 输入精简：采用。CPU对照约节省32%时间，GPU单独收益较小，但与精简输出组合有效。六阶段输入可逐项还原原内容，不删掉经营或比赛事实。',
          '- 输出精简：GPU采用，CPU不采用。单独应用在CPU组未提速（平均19.22秒对18.84秒），部分决定生成了更多经营动作；GPU输入输出组合较GPU基线节省约25%。因此CPU回退保留原说明协议，GPU使用结构化动作协议。公开摘要和比赛对手决策栏始终保留。',
          '- 预热/缓存：采用后台加载模型与快速关闭端口探测，保留并明确启用内置服务已有提示缓存。缓存参数省略与true结果相近，证明服务本来就默认使用缓存，不能算新增收益。缓存组多为重复同一快照，属于高命中情况，真实比赛持续变化时收益可能更低。', '',
          '## 首次启动及正式调用验证', '',
          f"正式启动路径冷启动{runtime['ColdStartSeconds']:.3f}秒，实际玩家对手决定{runtime['DecisionSeconds']:.3f}秒。后台启动派发{runtime['WarmupDispatchMs']:.3f}毫秒；模拟玩家先操作5秒后，前台剩余启动等待{runtime['ForegroundAfterWarmupSeconds']:.3f}秒。未人为在正式游戏加入5秒延时。", '',
          '初版正式路径因重复HTTP关闭端口探测冷启动7.63秒、5秒预热后仍等2.60秒；增加快速TCP探测后得到上述结果。预热只提前装载模型，不提前决定战术、交易或续约；每次真正决策仍读取最新状态。', '',
          '已注入GPU启动异常，确认随后启动CPU推理服务；正式GPU请求与玩家比赛输出验证通过。六阶段无损输入还原、仅reason字段的schema差异及既有AI/阵容回归均通过。', '',
          '## 限制', '',
          '共100个计时决定，无非法纠正、兜底或执行失败；另有正式调用和启动测试。每场景仅两个种子，不是长期质量或稳定性保证。不同后端/协议可能选择不同合法动作，工作量也不同，因此耗时不是纯内核基准，也不保证战略质量完全相同。未测试多并发、未更换模型、未运行完整赛季。', '',
          '原始记录：cpu-results.json、gpu-results.json、各后端inputs/startup/server日志、tuning-tests.json、runtime-tests.json。官方运行包来源及校验值记录在LocalAI/release-b11719.json和manifest.json。']
(root / '推理提速测试报告.md').write_text('\n'.join(lines) + '\n', encoding='utf-8')
print(f'Report written: {len(all_rows)} decisions, {data["requests"]} requests')
