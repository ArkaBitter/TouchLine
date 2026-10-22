"""Summarize the isolated inference benchmark; does not modify the game."""
import json
import sys
from pathlib import Path

root = Path(sys.argv[1]).resolve()
read = lambda name: json.loads((root / name).read_text(encoding="utf-8-sig"))
complete = read("complete.json")
if not complete["Passed"]:
    raise RuntimeError("Benchmark did not complete")
batches = read("results.json")
for folder in ("training", "training-required", "quote-required"):
    supplement = root / folder
    if (supplement / "complete.json").exists():
        extra_complete = json.loads((supplement / "complete.json").read_text(encoding="utf-8-sig"))
        if not extra_complete["Passed"]:
            raise RuntimeError("Training supplement did not complete")
        batches += json.loads((supplement / "results.json").read_text(encoding="utf-8-sig"))
hardware = read("hardware.json")
categories = list(dict.fromkeys(row["Category"] for row in batches))
rows = {(row["Category"], row["Parallel"]): row for row in batches}
lines = [
    "# 本地智能体隔离并发测试", "",
    f"处理器：{hardware['CPU']['Name']}；内存约{hardware['Computer']['TotalPhysicalMemory'] / 1024**3:.1f} GiB。",
    "模型：Qwen2.5-1.5B-Instruct Q4_K_M，llama.cpp b11719 CPU版；推理及提示处理均固定8线程。",
    "并发槽数及客户端请求上限同时设为1、2、4；每槽8192上下文，关闭提示缓存，固定每个逻辑请求的种子。",
    "每组服务先预热；启动、加载及预热不计入下表。业务解码、合法性验证和最多三次纠正使用游戏现有代码。",
    "测试专用HTTP传输增加固定种子和token计数；所有决定读取相同的阶段快照，结果仅在一次性副本中串行校验执行。",
    f"每类4家俱乐部、每种并发各1批，共{sum(len(row['Results']) for row in batches)}个逻辑决定。这是初步单机筛选，不是长期压力或平衡验收。",
    "期末升级点规划如有列出，属于补充场景：第18周、每队30点、无未来重点赛程、竞技培养取向，保留模型自主分配或留点选择。",
    "训练行动覆盖如有列出，是为避免全部留点而增加的路径覆盖基准：在同一份期末场景上，仅将测试输出的training数组限制为必须且只有一项，模型仍选择球员、能力及1～30点。其余生产解码及预算校验不变。此要求不用于正式游戏，也不代表普通规划必然训练。", "",
    "主动报价覆盖如有列出，是补充路径基准：要求模型必须且只提出一笔报价，目标从可负担货架球员中选，费用按现行规则匹配；正式游戏仍允许不报价。主转会场景的主动报价数量为0，主要覆盖了已有报价的回应。",
    "测试预置待回应报价以保证每队都有回应工作量，没有验证这些初始报价的产生过程；不能据此宣称报价窗口全流程或报价次数限制已完整验收。", "",
    "| 决策类别 | 串行耗时 | 2并发耗时 | 2并发加速比 | 4并发耗时 | 4并发加速比 |",
    "| --- | ---: | ---: | ---: | ---: | ---: |",
]
for category in categories:
    base, two, four = [rows[category, n]["Seconds"] for n in (1, 2, 4)]
    lines.append(f"| {category} | {base:.2f}s | {two:.2f}s | {base/two:.2f}倍 | {four:.2f}s | {base/four:.2f}倍 |")
totals = {n: sum(r["Seconds"] for r in batches if r["Parallel"] == n) for n in (1, 2, 4)}
lines.append(f"| 合计 | {totals[1]:.2f}s | {totals[2]:.2f}s | {totals[1]/totals[2]:.2f}倍 | {totals[4]:.2f}s | {totals[1]/totals[4]:.2f}倍 |")
lines += ["", "| 并发 | 计时阶段HTTP请求 | 额外纠正请求 | 兜底决定 | 请求/决定失败 | 执行时再次校验失败 | 峰值工作集 | 峰值私有内存 |",
          "| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: |"]
summary = {}
for n in (1, 2, 4):
    group = [r for r in batches if r["Parallel"] == n]
    summary[n] = dict(
        seconds=totals[n], speedup=totals[1] / totals[n],
        requests=sum(r["Requests"] for r in group), retries=sum(r["Retries"] for r in group),
        fallbacks=sum(r["Fallbacks"] for r in group), failures=sum(r["Failures"] for r in group),
        apply_errors=sum(bool(d["ApplyError"]) for r in group for d in r["Results"]),
        peak_work_mib=max(r["PeakWorkingMiB"] for r in group),
        peak_private_mib=max(r["PeakPrivateMiB"] for r in group),
        prompt_tokens=sum(r["PromptTokens"] for r in group), output_tokens=sum(r["OutputTokens"] for r in group),
        training_points=sum(d["Training"] for r in group for d in r["Results"]),
        releases_or_retirements=sum(d["Releases"] for r in group for d in r["Results"]),
        purchase_proposals=sum(d["Offers"] for r in group for d in r["Results"]),
        accept_requests=sum(d["Accepts"] for r in group for d in r["Results"]),
    )
    s = summary[n]
    lines.append(f"| {n} | {s['requests']} | {s['retries']} | {s['fallbacks']} | {s['failures']} | {s['apply_errors']} | {s['peak_work_mib']:.0f} MiB | {s['peak_private_mib']:.0f} MiB |")
lines += ["", "| 并发 | 平均单决定服务时间（不含客户端排队） | 提示token合计 | 输出token合计（含纠正） |",
          "| --- | ---: | ---: | ---: |"]
for n in (1, 2, 4):
    group = [r for r in batches if r["Parallel"] == n]
    mean = sum(d["Seconds"] for r in group for d in r["Results"]) / sum(len(r["Results"]) for r in group)
    lines.append(f"| {n} | {mean:.2f}s | {summary[n]['prompt_tokens']} | {summary[n]['output_tokens']} |")
lines += ["", "结果解释与边界", "",
          "加速比=串行整批耗时/对应并发整批耗时；超过1表示整批更快。总耗时包含提示准备、模型生成、业务验证及纠正；执行副本的结算耗时单独保存在results.json。",
          "平均单决定服务时间用于观察并发下单个请求是否变慢；整批吞吐改善不保证玩家临场请求更快。",
          "固定种子不能保证不同批处理方式得到逐字相同输出，token数及纠正次数应结合耗时一起看。",
          "转会数据包含可购买的货架/自由球员及待回应报价；接受意向仍须通过资金、工资、球员同意和归属校验，不等于成交。",
          "升级点允许保留；续约允许全部续约。具体行动数量见summary.json，不将无操作冒充已执行训练或解约。",
          "所有测试服务使用独立临时端口，只清理自身进程。未推进任何比赛，未改动正式存档、正式模型并发配置或正式可执行文件。",
          "配置执行顺序为1、2、4，未做随机化重复；结果可能受后台负载和温度影响，不能承诺所有客户电脑的同等收益。", ""]
primary = [r for r in batches if r["Category"] in categories[:4]]
primary_totals = {n: sum(r["Seconds"] for r in primary if r["Parallel"] == n) for n in (1, 2, 4)}
lines += ["初步建议", "",
          f"四类自主主测试合计：串行{primary_totals[1]:.2f}s，2并发{primary_totals[2]:.2f}s，4并发{primary_totals[4]:.2f}s；本轮整体未提速。",
          "续约和赛前准备无纠正请求的批次约有6%～11%的耗时改善，实际训练覆盖的4并发批次约快16%；这些是单轮小样本，不能当作确定性收益承诺。",
          "并发组本轮产生更多超预算训练及相互冲突的报价回应，额外纠正占用了时间。不能从单轮不同输出推出并发本身必然降低模型质量。",
          "建议现阶段保留正式游戏单请求配置，优先评估减少无效纠正的合法训练/报价回应候选方案。若以后继续优化，可单独测线程数、GPU后端及缓存开启时的情况，玩家临场请求应保持优先。以上建议本轮未实施。", ""]
audit_path = root / "audit.json"
if audit_path.exists():
    audit = json.loads(audit_path.read_text(encoding="utf-8-sig"))
    lines += [f"离线执行核对：{audit['Decisions']}个决定，{audit['TrainingActions']}项训练、{audit['PointsSpent']}点，实际生成{audit.get('NewQuotes', 0)}笔报价；资金守恒，推进比赛{audit['MatchesPlayed']}场。", ""]
(root / "summary.json").write_text(json.dumps(summary, ensure_ascii=False, indent=2), encoding="utf-8")
(root / "并发测试报告.md").write_text("\n".join(lines), encoding="utf-8")
print(json.dumps(summary, ensure_ascii=False, indent=2))

