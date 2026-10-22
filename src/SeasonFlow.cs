using System;
using System.Linq;
using System.Collections.Generic;

namespace Touchline {
public static class GameFactory {
 public static Game New(int seed,bool europe=false,string mode="长期培养"){var g=new Game{Rng=((ulong)(uint)seed+1).ToString("X16"),FirstSeasonEurope=false};var rng=new RandomSource(g);
  string[] towns={"星港","海岚","北辰","白榆","云川","赤湾","月溪","青原","雪岭","临风","远岬","金谷","镜湖","松城","晨岛","银沙","霜河","烈阳","天穹","苍山","雨林","南渡","极光","晴川","望海","新洲","龙湾","翠峰","飞泉","墨城"};
  var clubs=rng.Shuffle(towns);for(int i=0;i<30;i++)g.Clubs.Add(new Club{Id="C"+i,Name=clubs[i]+"竞技",League=i/10+1,Funds=rng.Next(10000)+1,Commercial=6+rng.Next(30),Honor=rng.Next(100)+1,Philosophy=Rules.Philosophies[rng.Next(12)],Tactics=rng.Shuffle(Rules.Tactics.Skip(1)).Take(5).ToArray(),AgentIdentity=new[]{"稳健的长期建设者","重视进攻的冒险家","精打细算的经理","重视忠诚的传统派","重视轮换的务实教练"}[rng.Next(5)]});
  string[] surnames={"林","陈","周","许","沈","江","陆","叶","顾","秦","苏","夏","萧","白","温","程","方","宋","谢","唐","韩","尹","钟","易","杜","罗","黎","傅","乔","严"};
  string[] first={"星","云","川","远","泽","清","雨","森","曜","景","辰","明","安","羽","凌","洛","启","沐","航","晏","砚","驰","风","宁","溪","舟","朗","深","柏","霖"};
  var used=new HashSet<string>();for(int i=0;i<800;i++){int role=rng.Next(10);string type=role==0?"门将":role<4?"后卫":role<7?"中场":"前锋";string name;do{name=surnames[rng.Next(surnames.Length)]+first[rng.Next(first.Length)]+first[rng.Next(first.Length)];}while(!used.Add(name));
   double target=rng.Next(100)+1;int black=Rules.Round(10*rng.NextDouble());double x=rng.NextDouble(),y=rng.NextDouble(),lo=Math.Min(x,y),hi=Math.Max(x,y);double[] coef=type=="前锋"?new[]{.75,.2,.1}:type=="中场"?new[]{.3,.45,.3}:type=="后卫"?new[]{.1,.2,.75}:new[]{.05,.1,.9};double[] colors={100*lo*coef[0],100*(hi-lo)*coef[1],100*(1-hi)*coef[2]};double shot=(rng.Next((int)target)+1)*(type=="前锋"||type=="门将"?.6:type=="中场"?.3:.1),pool=target-shot+black,sum=colors.Sum();
   g.Players.Add(new Player{Id="P"+i,Name=name,Type=type,ClubId=g.Clubs[rng.Next(30)].Id,Red=Rules.Round(pool*colors[0]/sum),Green=Rules.Round(pool*colors[1]/sum),Blue=Rules.Round(pool*colors[2]/sum),Black=black,Shot=shot,Foot=rng.Next(3),Favor=rng.TwoDice(),Loyalty=rng.TwoDice(),PrimarySlot=role==0?"gkp":role<4?"def":role<7?"mid":"fwd"});
  }
  foreach(var c in g.Clubs){g.TieLots[c.Id]=rng.Next(int.MaxValue);g.LastRanks[c.Id]=rng.Next(10)+1;if(g.Squad(c.Id).Count(p=>p.Injury==0)>=10)Football.Auto(g,c);}
  g.CareerMode=mode;if(mode=="球星迭起"){var ageRandom=new RandomSource(g);foreach(var p in g.Players)p.Age=18+ageRandom.Next(17);}WorldIdentity.Ensure(g);Competitions.StartSeason(g,false);g.Log("新游戏：800人独立随机分配。身价按原生成器四舍五入，实际值与目标整数可能有小幅差异。");Rules.Validate(g);return g;
 }
}
public static class SeasonFlow {
 static readonly double[,] league={{3,6,400,.03},{2,4,325,.03},{1,2,275,.02},{0,0,225,.02},{0,0,175,.02},{0,0,150,.01},{0,0,125,.01},{-1,0,100,.01},{-2,0,87.5,.01},{-3,0,75,0}};
 public static void Recover(Game g){foreach(var p in g.Players){p.Injury=Math.Max(0,p.Injury-1);p.Fatigue=0;}g.Week++;g.Log("游戏周更新：伤停-1，疲劳清零。");}
 public static void WeekEnd(Game g){Competitions.Progress(g);if(Competitions.Next(g)!=null)throw new Exception("本周还有未完成比赛。");Recover(g);if(g.Phase=="欧冠资格赛"){if(g.Fixtures.Where(f=>f.Competition=="欧冠资格赛").All(f=>f.Played))Competitions.FinishQualifier(g);}else if(g.Week>18){if(g.Fixtures.Any(f=>!f.Played))throw new Exception("仍有未完成赛程，不能赛季结算。");g.Phase="商业收入";g.Log("赛季全部比赛结束，进入逐项结算。");}}
 public static void Advance(Game g){string key=g.Season+"/"+g.Phase;if(g.Settled.Contains(key))throw new Exception("本步骤已经执行，拒绝重复结算。");
  if(g.Phase=="商业收入"){foreach(var c in g.Clubs){double income=c.Commercial*36;c.Funds+=income;g.SeasonReport[c.Id]=new[]{income,0.0,0.0,0.0};}foreach(var p in g.Players){p.Injury=0;p.Fatigue=0;}g.Phase="成绩结算";g.Log("先按旧商业价值×36发放资金，剩余伤停不跨赛季。");}
  else if(g.Phase=="成绩结算"){SettleResults(g);g.Phase="好感结算";}
  else if(g.Phase=="好感结算"){if(g.Players.Any(p=>p.ClubId!=""&&!p.AvailabilityKnown))throw new Exception("旧存档缺少可出场数。请先在阵容页面为标记球员补录可出场数，不能凭空推算。");foreach(var p in g.Players.Where(p=>p.ClubId!="")){int d=0;if(p.Available>0){double r=(double)p.Appearances/p.Available;d=r>=.7?1:r>=.3?0:r>=.1?-1:-2;}if(d>0&&p.Favor+p.Loyalty<9)d=0;if(d<0&&p.Favor+p.Loyalty>=20)d=0;p.Favor=Math.Min(12,Math.Max(0,p.Favor+d));}Competitions.Allocate(g);g.Log("好感按出场比例结算，最高12；开始欧冠资格赛。");}
  else if(g.Phase=="续约"){throw new Exception("请先完成本队续约选择，再运行AI续约回合。");}
  else if(g.Phase=="自主转会"){throw new Exception("请通过本地AI执行自主转会阶段。");}
  else if(g.Phase=="需求转会"){g.Phase="工资结算";g.Log("转会窗关闭，等待发放工资。");}
  else if(g.Phase=="工资结算"){foreach(var c in g.Clubs){double wages=g.Squad(c.Id).Sum(p=>p.Value);c.Funds=Math.Round(c.Funds-wages,2);g.SeasonReport[c.Id][3]=wages;}g.Phase="新赛季准备";g.Log("全队季工资已扣除，允许负资金。");}
  else if(g.Phase=="新赛季准备"){g.Season++;foreach(var p in g.Players){p.Appearances=0;p.Available=0;p.AvailabilityKnown=true;p.Stats.Clear();p.Listed=false;p.Renew=true;p.Injury=0;p.Fatigue=0;}g.Offers.Clear();g.SeasonReport.Clear();Competitions.StartSeason(g,true);}
  else throw new Exception("当前阶段不能直接结算。");g.Settled.Add(key);
 }
 static void SettleResults(Game g){foreach(int l in new[]{1,2,3}){var rows=Competitions.League(g,l);double pool=g.Clubs.Where(c=>c.League==l).Sum(c=>g.Squad(c.Id).Sum(p=>p.Value));foreach(var r in rows){Club c=g.Club(r.ClubId);g.LastRanks[c.Id]=r.Rank;double income=league[r.Rank-1,2]+200+pool*(league[r.Rank-1,3]+.01);c.Funds+=Math.Round(income,2);c.Commercial+=(int)league[r.Rank-1,0];c.Honor+=(int)league[r.Rank-1,1];g.SeasonReport[c.Id][1]+=income;}}
  foreach(var comp in g.Competitions){double pool=comp.Groups.SelectMany(x=>x).Sum(id=>g.Squad(id).Sum(p=>p.Value));foreach(string id in comp.Groups.SelectMany(x=>x)){
   int stage=id==comp.Champion?0:id==comp.Runner?1:comp.Semis.Contains(id)?2:3;var c=g.Club(id);double money=0;int honor=0,cv=0;
   if(comp.Name=="一级杯赛"){money=new[]{200,100,37.5,15}[stage]+pool*new[]{.03,.02,.01,0}[stage];honor=stage==0?3:stage==1?1:0;cv=stage==0?1:0;}
   if(comp.Name=="二级杯赛"){money=new[]{100,50,20,0}[stage]+pool*new[]{.02,.01,0,0}[stage];honor=stage==0?1:0;}
   if(comp.Name=="欧冠"||comp.Name=="欧联"){money=(comp.Name=="欧冠"?new[]{500,250,100,50}:new[]{250,125,50,25})[stage]+pool*(new[]{.08,.06,.04,.02}[stage]+.01);if(comp.Name=="欧冠"){honor=stage==0?10:stage==1?5:0;cv=stage==0?5:stage==1?3:0;}else{honor=stage==0?5:stage==1?2:0;cv=stage==0?2:stage==1?1:0;}}
   if((comp.Name=="一级杯赛"||comp.Name=="二级杯赛")&&stage==3&&!g.Fixtures.Any(f=>Competitions.Key(f)==comp.Id&&f.Stage=="八强"&&(f.Home==id||f.Away==id)))money=0;c.Funds+=Math.Round(money,2);c.Honor+=honor;c.Commercial+=cv;g.SeasonReport[id][1]+=money;
  }}Awards(g);foreach(var c in g.Clubs)c.Commercial=Math.Max(5,c.Commercial);g.Log("排名、奖金、转播、商业与荣誉、球员统计奖项已结算。");}
 static double Achievement(Game g,string id){int rank=g.LastRanks.ContainsKey(id)?g.LastRanks[id]:10;double s=rank<=3?4-rank:0;foreach(var c in g.Competitions){if(c.Champion==id)s+=c.Name=="欧冠"?4:c.Name=="欧联"?2:c.Name=="一级杯赛"?1:.5;else if(c.Runner==id)s+=c.Name=="欧冠"?2:c.Name=="欧联"?1:0;else if(c.Name=="欧冠"&&c.Semis.Contains(id))s+=1;}return s;}
 static void Awards(Game g){var rng=new RandomSource(g);string[] comps={"联赛","一级杯赛","二级杯赛","欧冠","欧联"};double[] cw={3,1,.5,4,2},mw={1,1.5,.2,.075,.16,1.7};
  foreach(string comp in new[]{"联赛","欧冠","欧联"})foreach(int l in comp=="联赛"?new[]{1,2,3}:new[]{0}){var eligible=g.Players.Where(p=>p.ClubId!=""&&(l==0||g.Club(p.ClubId).League==l)&&p.Stats.ContainsKey(comp)).ToArray();for(int k=0;k<6;k++){double max=eligible.Select(p=>p.Stats[comp][k]).DefaultIfEmpty(0).Max();if(max<=0)continue;var clubs=eligible.Where(p=>p.Stats[comp][k]==max).Select(p=>p.ClubId).Distinct().ToArray();int best=clubs.Min(id=>g.LastRanks.ContainsKey(id)?g.LastRanks[id]:10);var tied=clubs.Where(id=>(g.LastRanks.ContainsKey(id)?g.LastRanks[id]:10)==best).ToArray();string winner=tied[rng.Next(tied.Length)];g.Club(winner).Honor++;g.Log(comp+"第"+(k+1)+"榜荣誉+1："+g.Club(winner).Name);}}
  var available=g.Players.Where(p=>p.ClubId!="").ToList();foreach(int category in new[]{0,1,2}){var scored=available.Select(p=>{double[] x=new double[6];for(int i=0;i<5;i++){double[] stat;if(p.Stats.TryGetValue(comps[i],out stat))for(int k=0;k<6;k++)x[k]+=stat[k]*cw[i]*mw[k];}return new {P=p,Score=category==0?x.Take(3).Sum():category==1?x[3]:x[4]+x[5]};}).ToList();double max=scored.Select(x=>x.Score).DefaultIfEmpty(0).Max();if(max<=0)continue;var ties=scored.Where(x=>Math.Abs(x.Score-max)<1e-8).ToList();double achievement=ties.Max(x=>Achievement(g,x.P.ClubId));var finalists=ties.Where(x=>Achievement(g,x.P.ClubId)==achievement).ToArray();Player winner=finalists[rng.Next(finalists.Length)].P;g.Club(winner.ClubId).Commercial++;available.Remove(winner);g.Log(new[]{"进攻巨星","组织巨星","防守巨星"}[category]+"："+winner.Name+"，所在队商业价值+1。");}
 }
 public static void Reward(Game g,Fixture f,LiveMatch m){
  if(f.HomeGoals==f.AwayGoals){m.Events.Add("赛后结算：平局，无胜负升级点及大胜／惨败奖惩。");return;}
  Club win=g.Club(f.HomeGoals>f.AwayGoals?f.Home:f.Away),lose=g.Club(f.HomeGoals>f.AwayGoals?f.Away:f.Home);var rng=new RandomSource(g);
  int before=win.Upgrade;win.Upgrade++;m.Events.Add(win.Name+"胜利奖励：升级点 "+before+" → "+win.Upgrade);
  if(lose.Upgrade>0){before=lose.Upgrade;lose.Upgrade--;m.Events.Add(lose.Name+"战败：升级点 "+before+" → "+lose.Upgrade);}
  else{var ids=lose.Id==f.Home?m.UsedHome:m.UsedAway;if(ids.Count>0){string victimId=ids[rng.Next(ids.Count)];ReportChange(m,g.Players.First(p=>p.Id==victimId),rng.Next(5),false,"无升级点战败惩罚");}}
  double a=m.HomeLineup.Where(id=>id!=null).Sum(id=>g.Players.First(p=>p.Id==id).Value),b=m.AwayLineup.Where(id=>id!=null).Sum(id=>g.Players.First(p=>p.Id==id).Value);bool allowed=Math.Abs(a-b)<150;
  int rankGap=0;if(win.League==lose.League){var table=Competitions.League(g,win.League);rankGap=Math.Abs(table.First(r=>r.ClubId==win.Id).Rank-table.First(r=>r.ClubId==lose.Id).Rank);allowed&=rankGap<5;}
  if(Math.Abs(f.HomeGoals-f.AwayGoals)<3){m.Events.Add("未触发大胜／惨败奖惩：净胜球不足3。");return;}
  if(!allowed){m.Events.Add("取消大胜／惨败奖惩：阵容身价差 "+Math.Abs(a-b).ToString("F2")+(win.League==lose.League?"，同联赛排名差 "+rankGap:"")+"（身价差≥150或排名差≥5）。");return;}
  foreach(string id in rng.Shuffle(win.Id==f.Home?m.HomeLineup:m.AwayLineup).Take(2)){int slot=Array.IndexOf(win.Id==f.Home?m.HomeLineup:m.AwayLineup,id);ReportChange(m,g.Players.First(p=>p.Id==id),SlotMetric(slot),true,"大胜奖励");}
  foreach(string id in rng.Shuffle(lose.Id==f.Home?m.HomeLineup:m.AwayLineup).Take(2)){int slot=Array.IndexOf(lose.Id==f.Home?m.HomeLineup:m.AwayLineup,id);ReportChange(m,g.Players.First(p=>p.Id==id),SlotMetric(slot),false,"惨败惩罚");}
 }
 static void ReportChange(LiveMatch m,Player p,int attr,bool up,string reason){double before=new[]{p.Red,p.Green,p.Blue,p.Shot,p.Black}[attr];Change(p,attr,up);double after=new[]{p.Red,p.Green,p.Blue,p.Shot,p.Black}[attr];m.Events.Add(reason+"："+p.Name+" · "+new[]{"红点","绿点","蓝点","射门/扑救","黑点"}[attr]+" "+before.ToString("F2")+" → "+after.ToString("F2"));}
 static int SlotMetric(int slot){return slot==1||slot==9?3:slot<3?0:slot<6?1:2;}
 public static void Change(Player p,int attr,bool up){double[] a={p.Red,p.Green,p.Blue,p.Shot,p.Black};if(attr==4)a[attr]=Math.Max(0,a[attr]+(up?-1:1));else a[attr]=Math.Round(Math.Max(0,a[attr]+(up?3*Math.Exp(-a[attr]/50):-3*Math.Log(1+a[attr]/50))),2);p.Red=a[0];p.Green=a[1];p.Blue=a[2];p.Shot=a[3];p.Black=a[4];}
 public static void Train(Game g,Club c,Player p,int metric,int levels){if(p.ClubId!=c.Id||levels<1||levels>c.Upgrade)throw new Exception("升级点不足或球员不属于本队。");for(int i=0;i<levels;i++)Change(p,metric,true);c.Upgrade-=levels;}
}
public static class Transfers {
 public static double Fee(Game g,Player p,bool desired){if(p.ClubId=="")return 0;double m=p.Foot==0?1.5:1;if(p.Value>=90)m*=2;else if(p.Value>=70)m*=1.5;int remain=g.Squad(p.ClubId).Count(x=>x.Type==p.Type)-1;if(remain<(p.Type=="门将"?2:3))m*=2;if(p.Favor+p.Loyalty<9)m*=.5;else if(p.Favor+p.Loyalty<12)m*=.75;if(desired)m*=.75;if(p.Value<10)m*=.75;return Math.Round(p.Value*10*m,2);}
 public static bool Affordable(Game g,Player p,Club buyer,double fee){return fee>=0&&!double.IsNaN(fee)&&!double.IsInfinity(fee)&&buyer.Funds-fee>=g.Squad(buyer.Id).Sum(x=>x.Value)+p.Value;}
 public static bool Useful(Game g,Player p,Club buyer){
 var ps=g.Squad(buyer.Id).Where(x=>x.Type==p.Type&&(p.Type=="门将"||x.Foot==p.Foot));
 int slot=p.Type=="门将"?9:Array.IndexOf(Rules.Types,p.Type)*3+(p.Foot==0?1:p.Foot==1?0:2);
 double candidate=Football.Score(Football.Weight(p,slot,"古典足球","无战术","",Rules.Round((p.Favor+7)/2.0),false,false));
 bool topTwo=ps.Count(x=>Football.Score(Football.Weight(x,slot,"古典足球","无战术","",Rules.Round((x.Favor+7)/2.0),false,false))>=candidate)<2;
 if(topTwo)return true;var healthy=g.Squad(buyer.Id).Concat(new[]{p}).Where(x=>x.Injury==0).ToArray();
 return healthy.Length>=10&&Football.Optimize(g,buyer,"无战术",healthy).Contains(p.Id);
}
 public static void Release(Game g,Player p){string old=p.ClubId;p.ClubId="";p.Listed=true;p.Renew=true;foreach(var c in g.Clubs)c.Lineup=c.Lineup.Where(id=>id!=p.Id).ToArray();g.Log(p.Name+"未获续约，成为自由球员（原队 "+g.Club(old).Name+"）。");}
 public static int OffersRemaining(Game g,Club c){return Math.Max(0,5-g.Offers.Count(o=>o.BuyerId==c.Id&&!o.PlayerDriven&&o.Season==g.Season));}
 public static TransferOffer Offer(Game g,Player p,Club buyer,double fee,bool driven=false){if(buyer.Id==p.ClubId)throw new Exception("不能向本队球员报价。");
if(!driven&&p.ClubId!=""&&fee+1e-7<Fee(g,p,false))throw new Exception("报价低于按现行倍率计算的转会费。");
if(!driven&&OffersRemaining(g,buyer)<=0)throw new Exception("本转会窗5次报价机会已用完。");if(p.ClubId=="")fee=0;if(!Affordable(g,p,buyer,fee))throw new Exception("支付转会费后资金不足以支付转入后全队工资。");if(driven&&!Useful(g,p,buyer))throw new Exception("球员无法进入该位置前二。");if(g.Offers.Any(existing=>existing.PlayerId==p.Id&&existing.BuyerId==buyer.Id&&existing.Status=="待回应"))throw new Exception("相同报价已在等待回应。");var o=new TransferOffer{Id=Guid.NewGuid().ToString("N"),PlayerId=p.Id,BuyerId=buyer.Id,SellerId=p.ClubId,Fee=fee,Season=g.Season,PlayerDriven=driven};g.Offers.Add(o);return o;}
 public static void Complete(Game g,TransferOffer o,bool accept){if(o.Status!="待回应")return;Player p=g.Players.First(x=>x.Id==o.PlayerId);if(!accept){o.Status="已拒绝";return;}if(p.ClubId!=o.SellerId){o.Status="失效：球员已转会";return;}Club buyer=g.Club(o.BuyerId);if(!Affordable(g,p,buyer,o.Fee)){o.Status="失败：工资预算不足";return;}var rng=new RandomSource(g);if(!o.PlayerDriven&&p.ClubId!=""&&rng.Next(20)+1<=p.Favor+p.Loyalty){o.Status="球员拒绝";return;}double before=g.Clubs.Sum(c=>c.Funds);buyer.Funds-=o.Fee;if(o.SellerId!="")g.Club(o.SellerId).Funds+=o.Fee;p.ClubId=buyer.Id;p.Favor=rng.TwoDice();p.Listed=false;p.Renew=true;o.Status="已成交";foreach(var c in g.Clubs)c.Lineup=c.Lineup.Where(id=>id!=p.Id||c.Id==buyer.Id).ToArray();if(Math.Abs(g.Clubs.Sum(c=>c.Funds)-before)>1e-6)throw new Exception("转会资金不守恒。");g.Log(p.Name+"加盟"+buyer.Name+"，费用"+o.Fee.ToString("F2")+"，新好感"+p.Favor);}
 public static void PlayerDriven(Game g){var rng=new RandomSource(g);foreach(var p in rng.Shuffle(g.Players.Where(p=>p.ClubId==""||p.Favor+p.Loyalty<12)).ToArray()){
  var clubs=g.Clubs.Where(c=>c.Id!=p.ClubId).ToArray();double[] weights=clubs.Select(c=>(c.Commercial*10+c.Honor*3)*(g.Champions.Contains(c.Id)?1.5:g.Europa.Contains(c.Id)?1.2:1)).ToArray();Club desired=clubs[rng.Weighted(weights)];double fee=Fee(g,p,true);if(Affordable(g,p,desired,fee)&&Useful(g,p,desired))try{Offer(g,p,desired,fee,true);}catch(InvalidOperationException){}catch(Exception ex){g.Log("自主转会提案取消："+ex.Message);}
 }g.Log("球员自主意愿提案已生成，接收俱乐部由玩家／本地智能体决定。");}
}
}
