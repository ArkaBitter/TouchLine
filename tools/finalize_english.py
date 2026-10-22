from pathlib import Path
import json,re
root=Path(__file__).resolve().parents[1]
p=root/'data/en.json';d=json.loads(p.read_text(encoding='utf-8-sig'))
pairs='''俱乐部总览|Club Overview
阵容与编队|Squad and Lineup
理念与战术|Philosophies and Tactics
赛程与积分|Fixtures and Tables
球员与转会|Players and Transfers
经营与荣誉|Finances and Honours
赛事统计|Competition Statistics
本地AI设置|Local AI Settings
前锋|Forward
中场|Midfielder
后卫|Defender
门将|Goalkeeper
红|Red
绿|Green
蓝|Blue
黑|Errors
黑点|Error Points
阵容|Squad
阶段|Stage
需求|Recruitment
古典足球|Classic Football
全攻全守|Total Football
九锋阵|Nine Forwards
传控足球|Possession Football
链式防守|Catenaccio
连续短传|Short Passing
低位防守|Low Block
快速渗透|Quick Penetration
高位压迫|High Press
防守反击|Counter Attack
过顶长传|Long Balls
边中结合|Wide and Central Play
无战术|No Tactic
角球战术|Corner Routine
中卫前提|Stepping Centre-Back
影锋|Shadow Striker
双后腰|Double Pivot
巨星战术|Star Focus
造越位|Offside Trap
摆大巴|Park the Bus
全场自由人|Free Roaming Midfielder
单后腰|Single Pivot
进攻终结点|Central Finisher
抢落点|Attack the Second Ball
肋部渗透|Half-Space Passing
倒三角|Cutbacks
关门协防|Covering Defence
出击型门将|Sweeper Keeper
45度炸|Diagonal Crosses
三中卫|Three Centre-Backs
中场内收|Inverted Midfielders
弧顶内切|Cut Inside
远射|Long Shots
边后卫内收|Inverted Full-Backs
主攻左路|Attack Down the Left
主攻右路|Attack Down the Right
战术犯规|Tactical Foul
联赛|League
欧冠|Champions League
欧联|Europa League
欧冠资格赛|Champions Qualifier
一级杯赛|First Cup
二级杯赛|Second Cup
进球|Goals
助攻|Assists
绝佳传球|Key Passes
抢夺球权|Possession Wins
成功抢断|Successful Tackles
成功扑救|Saves
资金充裕|Cash Rich
板凳充裕|Deep Squad
商业帝国|Commercial Empire
历史豪门|Historic Giant
文化型俱乐部|Happy Squad
好感|Affection
忠诚|Loyalty
身价|Value
伤停|Unavailable
疲劳|Fatigue
球星迭起|Rising Stars
长期培养|Long-Term Development
世界种子|World Seed
主动退役|Retire Player
主队进球|Home Goals
传球失误|Passing Error
不续约：|Release: 
。原因：|. Reason: 
，新好感|, new affection 
周。| weeks.
周】| weeks]
 · 第| / 
编辑球员名称|Edit Player Names
保存球员名称|Save Player Names
开局球员命名 · 不显示俱乐部归属|Player Names - Club Assignments Hidden
编号|Number
球员名字|Player Name
中文 / English|中文 / English
中文|Chinese
新建随机世界|New Random World
读取本地存档|Load Saved Game
保存游戏|Save Game
读取存档|Load Game
推进赛程|Advance Fixtures
推进日志|Progress Log
游戏说明|Game Guide
返回主菜单|Main Menu
修改名字|Rename Player
自动排阵|Auto Lineup
训练升级|Train
不续约／续约|Renew / Release
补录可出场数|Edit Availability
本队改名|Rename My Club
未找到游戏说明书，请保留完整游戏文件夹。|Game guide not found. Keep the complete game folder.
正式开局后只能修改本队球员名字。|After starting, you can only rename players in your club.
正式开局后不能批量修改其他球队球员。|You cannot edit other clubs' players after starting.
球员名单不完整。|The player list is incomplete.
本地模型请求失败。|The local model request failed.
请在比赛结束后购买战术。|Buy tactics after the current match.
身价归零，自动退役。|has zero value and retires automatically.
仅需求转会窗内可挂牌。|Players can only be listed during the recruitment window.
请回应本队的球员自主加盟提案。|Respond to the player-initiated proposals for your club.
主动退役仅在球星迭起模式的续约阶段开放。|Retirement is available during renewal in Rising Stars mode.
欧冠／欧联各12队已确定。进入续约阶段。|The 12 teams for each European competition are confirmed. Renewal begins.
本地智能体连续3次未生成合法决定，比赛暂停，请重试。原因：|The local agent returned three invalid decisions. Match paused; retry. Reason: 
球员自主意愿提案已生成，接收俱乐部由玩家／本地智能体决定。|Player-initiated proposals generated. The receiving clubs decide whether to accept.
请先在本地AI设置中启动模型。关键比赛及经营阶段需要本地模型。|Enable the local model in AI Settings. It is required for key matches and management.
未找到本地模型。请运行安装本地AI脚本，或在设置中指定已有的本地服务。|Local model files not found. Install the bundled model or configure a local service.
离线版只允许 localhost／127.0.0.1 本地模型地址。|Offline mode only supports a localhost or 127.0.0.1 model endpoint.
比赛、换人、战术、转会与经营，由你掌控。其余俱乐部由离线本地模型决策。|Manage matches, substitutions, tactics and transfers. Other clubs use local AI and automatic coaching.
【玩家比赛就绪】关闭日志窗口后可推进比赛、换人和调整战术。|[Match ready] Close the log to advance play, substitute players or change tactics.
【已暂停】保留上一完整回合；尚未播放完的回合及其中的决定未提交。|[Paused] The last complete round is saved. The unfinished round has not been committed.
旧存档缺少可出场数。请先在阵容页面为标记球员补录可出场数，不能凭空推算。|This old save has missing availability counts. Fill in the marked players' eligible match counts on the squad page.
淘汰赛连续100场加赛仍平局，已暂停以避免无限循环。请检查双方进攻能力。|100 tiebreak matches remained drawn. Play paused to avoid an infinite loop. Check attacking strength.
无法连接本地模型服务 |Cannot connect to the local model service 
。请检查服务是否运行及端口配置。详细原因：|. Check that the service is running and its port is correct. Details: 
本地模型响应超时（|Local model timed out (
秒）。请稍后重试或在设置中延长超时。| seconds). Retry or increase the timeout in Settings.'''
for line in pairs.splitlines():
 if '|' in line:k,v=line.split('|',1);d[k]=v
rule=(root/'src/RuleDescriptions.cs').read_text(encoding='utf-8-sig')
tactics=re.findall(r'\{"([^"]+)","([^"]+)"\}',rule.split('Dictionary<string,string> Tactic=')[1])
effects=[
'No additional effect.',
'After the opponent makes an initial kickoff/control error: next round, team red +15% and the best shooter shot attribute +15%.',
'Reweight def_mid as mid_B2B; that slot gains blue +10%.',
'Reweight mid_B2B as fwd_mid; that slot gains green +5%.',
'Reweight mid_B2B as def_mid; that slot gains green +5%.',
'Highest-value starter: four positive attributes +8%; everyone else -0.5%.',
'Players whose original type is Defender: red -10%, blue +10%.',
'Team red -20%, green -20%, blue +50%.',
'Treat mid_B2B as two-footed for weighting; central green bonus +50%.',
'Treat def_mid as two-footed for weighting; central blue bonus +50%.',
'Treat fwd_mid as two-footed for weighting; central shot bonus +50%.',
'fwd_mid shot attribute +5%.',
'mid_left and mid_right green +5%.',
'fwd_left and fwd_right red +5%.',
'def_left and def_right blue +5%.',
'gkp blue +5%.',
'mid_left and mid_right red +5%.',
'Reweight def_left and def_right as def_mid.',
'Reweight mid_left and mid_right as mid_B2B.',
'fwd_left and fwd_right shot attribute +2.5%.',
'mid_B2B shot attribute +5%.',
'Right-footed left-back or left-footed right-back: remove the 0.75 wrong-side blue factor. Type mismatch still applies.',
'Left forward red, left midfielder green, left defender blue +5%; corresponding right-side attributes -5%.',
'Right forward red, right midfielder green, right defender blue +5%; corresponding left-side attributes -5%.',
'Cancel the first shot on target before the keeper check, including its rebound. A defender receives a 4-week post-match suspension: 75% corresponding lane, 25% random among three defenders. Disabled in finals; no immediate dismissal.'
]
assert len(tactics)==len(effects)==25
for (_,src),en in zip(tactics,effects):d[src]=en
philos=re.findall(r'\{"([^"]+)","([^"]+)"\}',rule.split('Dictionary<string,string> Tactic=')[0])
philo_effects=[
'No permanent bonus. Against Nine Forwards, Possession Football or Catenaccio: original forwards blue +15%, midfielders green +15%, defenders red +15%.',
'Green +5%. Against Classic Football: team red/blue +10%. Against the seven situational philosophies below: red/blue -3%. No red/blue change against Nine Forwards, Possession Football or Catenaccio.',
'Team red +30%, blue -20%.',
'Team green +10%.',
'Team blue +30%, red -20%.',
'Green +5%. Extra green +15% against Wide and Central Play or Quick Penetration; extra green -15% against Low Block or Counter Attack.',
'Blue +5%. Extra blue +15% against Short Passing or Wide and Central Play; extra blue -15% against High Press or Quick Penetration.',
'Red +5%. Extra red +15% against Low Block or Long Balls; extra red -15% against Short Passing or High Press.',
'Red +5%. Blue +15% against Quick Penetration or Low Block; blue -15% against Counter Attack or Long Balls.',
'Blue +5%. Red +15% against High Press or Short Passing; red -15% against Long Balls or Wide and Central Play.',
'Red +5%. Extra red +15% against Counter Attack or High Press; extra red -15% against Wide and Central Play or Quick Penetration.',
'Green +5%. Extra green +15% against Long Balls or Counter Attack; extra green -15% against Short Passing or Low Block.'
]
assert len(philos)==len(philo_effects)==12
for (_,src),en in zip(philos,philo_effects):d[src]=en
d.update({"第3步：按可出场比例更新好感，最高12。":"Step 3: update affection by appearance ratio, capped at 12.","第2步：结算排名、奖金、分成、商业价值、荣誉和统计奖项。":"Step 2: settle rankings, prize money, shares, commercial value, honours and statistical awards.","第6步：球员自主意愿提出加盟申请，由玩家与本地AI回应。":"Step 6: respond to player-initiated transfer proposals.","第7步：挂牌、报价、回应报价，点击推进执行一轮AI经营；在转会页手动关闭窗口。":"Step 7: list players and handle offers. Advance to run AI management; close the window on the transfer page."})
d.update({'应用所选战术':'Use Tactic','购买所选 · 1000':'Buy · 1000','全部战术 · 应用与商店':'Tactics and Shop','全部足球理念':'All Philosophies','理念与战术':'Philosophy & Tactics','TOUCHLINE\n触线 · 足球经理':'TOUCHLINE\nManager','离线本地智能体\n独立 C# 引擎 · v0.1':'Offline AI\nC# engine · v0.1',' 人':' players','周':' weeks','第':'','  /  联赛':'  /  League ','赛季 · 第':' season / Week ','周  /  ':'  /  ','\r\n本场战术：':'\r\nCurrent tactic: ','整季工资 ':'Season wages: ','健康 ':'Available: ','理念：':'Philosophy: ','当前使用':'Active','进攻巨星':'Attacking Star','组织巨星':'Playmaking Star','防守巨星':'Defensive Star','未举办':'Not held','升级点':'Training Points','胜利积累 · 阵容训练':'Earned by winning'})
for k,v in list(d.items()):
 if '\n' not in k and k.strip():
  leading=k[:len(k)-len(k.lstrip())];trailing=k[len(k.rstrip()):]
  if leading or trailing:d[k]=leading+v.strip()+trailing
p.write_text(json.dumps(d,ensure_ascii=False,indent=2),encoding='utf-8')
print('Reviewed English entries:',len(d))
