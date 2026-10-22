"""Summarize a completed isolated integration run; never edits a game save."""
import json
from pathlib import Path

project = Path(__file__).resolve().parents[1]
root = project / "build/test-results/end-to-end-20261020"

def read(name):
    return json.loads((root / name).read_text(encoding="utf-8-sig"))

result = read("result.json")
if not result.get("Passed"):
    raise RuntimeError("Integration run has not passed")

lines = [
    "# 隔离完整赛季验证结果",
    "",
    "测试使用独立随机世界与测试存档，没有修改玩家正式存档或MATLAB数据。",
    "首赛季及资格赛在原版本完成；休赛期及第二赛季在按分钟疲劳、六周智能体规划版本继续。"
    "这是跨版本流程回归，不是单一版本的排名预测或T12平衡测试。",
    "电脑球队使用生产代码和真实本地模型，普通比赛遵循自动教练策略；测试驾驶器代行玩家操作。",
    "",
    "验证覆盖：首赛季全部比赛、商业收入与成绩结算、好感、资格赛、年龄、续约、"
    "自主转会、需求转会、工资、新赛季建立、第二赛季全部比赛及欧战晋级。",
    "",
]
for season in (1, 2):
    data = read(f"season-{season}-results.json")
    lines += [f"## 第{season}赛季", "", f"已完成常规赛季比赛：{data['matches']}场。", ""]
    for league in data["leagues"]:
        lines += [
            f"### 联赛{league['league']}", "",
            "| 排名 | 球队 | 积分 | 进球 | 失球 | 净胜球 |",
            "| --- | --- | --- | --- | --- | --- |",
        ]
        for row in league["table"]:
            lines.append(
                f"| {row['Rank']} | {row['Name']} | {row['Points']} | "
                f"{row['GF']} | {row['GA']} | {row['GD']} |"
            )
        lines.append("")
    lines += ["| 杯赛 | 冠军 | 亚军 |", "| --- | --- | --- |"]
    for cup in data["cups"]:
        lines.append(f"| {cup['name']} | {cup['champion']} | {cup['runner']} |")
    lines.append("")
lines += [
    "## 验证边界", "",
    "本次为单存档、单条随机轨迹；通过流程不代表游戏已经达到商业发行质量。",
    "旧比赛缺少完整的弃权复盘字段，因此不把旧结果文件中的弃权计数当成准确总数。",
    "仍需处理的问题与实际验证状态以项目内《待完成任务.txt》为准，T12留给用户处理。",
    "", f"完成时间：{result['endedAt']}。",
]
output = project / "完整赛季验证结果_20261020.md"
output.write_text("\n".join(lines) + "\n", encoding="utf-8")
print(output)
