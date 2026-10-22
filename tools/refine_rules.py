from pathlib import Path
root=Path(__file__).resolve().parents[1]/'src'
p=root/'SeasonFlow.cs';s=p.read_text(encoding='utf-8')
s=s.replace('c.Funds+=Math.Round(money,2);c.Honor+=honor;', 'if((comp.Name=="一级杯赛"||comp.Name=="二级杯赛")&&stage==3&&!g.Fixtures.Any(f=>Competitions.Key(f)==comp.Id&&f.Stage=="八强"&&(f.Home==id||f.Away==id)))money=0;c.Funds+=Math.Round(money,2);c.Honor+=honor;')
old='public static bool Useful(Game g,Player p,Club buyer){var ps=g.Squad(buyer.Id).Where(x=>x.Type==p.Type&&(p.Type=="门将"||x.Foot==p.Foot));return ps.Count(x=>x.Value>=p.Value)<2;}'
new='''public static bool Useful(Game g,Player p,Club buyer){
 var ps=g.Squad(buyer.Id).Where(x=>x.Type==p.Type&&(p.Type=="门将"||x.Foot==p.Foot));
 int slot=p.Type=="门将"?9:Array.IndexOf(Rules.Types,p.Type)*3+(p.Foot==0?1:p.Foot==1?0:2);
 double candidate=Football.Score(Football.Weight(p,slot,"古典足球","无战术","",Rules.Round((p.Favor+7)/2.0),false,false));
 bool topTwo=ps.Count(x=>Football.Score(Football.Weight(x,slot,"古典足球","无战术","",Rules.Round((x.Favor+7)/2.0),false,false))>=candidate)<2;
 if(topTwo)return true;var healthy=g.Squad(buyer.Id).Concat(new[]{p}).Where(x=>x.Injury==0).ToArray();
 return healthy.Length>=10&&Football.Optimize(g,buyer,"无战术",healthy).Contains(p.Id);
}'''
assert old in s;s=s.replace(old,new)
s=s.replace('if(buyer.Id==p.ClubId)throw new Exception("不能向本队球员报价。");','''if(buyer.Id==p.ClubId)throw new Exception("不能向本队球员报价。");
if(!driven&&p.ClubId!=""&&fee+1e-7<Fee(g,p,false))throw new Exception("报价低于按现行倍率计算的转会费。");
if(!driven&&buyer.Id!=g.PlayerClubId){int rank=g.LastRanks.ContainsKey(buyer.Id)?g.LastRanks[buyer.Id]:10;int limit=rank<=2?3:rank<=4?4:rank<=6?5:rank<=8?6:7;if(g.Offers.Count(x=>x.BuyerId==buyer.Id&&!x.PlayerDriven)>=limit)throw new Exception("本队本季采购意向名额已用完。");}''')
p.write_text(s,encoding='utf-8')
p=root/'Agents.cs';s=p.read_text(encoding='utf-8').replace('c.AgentGoal=d.goal;','if(!String.IsNullOrEmpty(d.goal))c.AgentGoal=d.goal;')
p.write_text(s,encoding='utf-8')
p=root/'MainForm.cs';s=p.read_text(encoding='utf-8').replace('void Load(){','void LoadSave(){').replace('Button("读取存档",Load)','Button("读取存档",LoadSave)').replace('Button("读取本地存档",Load)','Button("读取本地存档",LoadSave)')
p.write_text(s,encoding='utf-8')
print('cup group payout, weighted reception and transfer limits updated')
