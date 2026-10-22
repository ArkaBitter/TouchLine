using System;
using System.Linq;
using System.Collections.Generic;
namespace Touchline {
public class LiveCoachPlan {
 public string Tactic;public List<SubDecision> Subs=new List<SubDecision>();public PositionSwap Swap;
 public double Utility,Gain,Attack,Against;public string[] Lineup;
}
// Decision support only: no RNG, no future state and no action execution here.
public static class LiveCoachPlans {
 public static int Budget(Game g,Club c,Fixture f){int minute=g.Live.Round*2;bool home=f.Home==c.Id;int behind=(home?g.Live.AwayGoals-g.Live.HomeGoals:g.Live.HomeGoals-g.Live.AwayGoals);return minute<30?1:minute<60?(behind>=2?3:2):minute<76?4:5;}
 class Evaluation {
  public Game G;public Club C,Foe;public Fixture F;public bool Home;public Dictionary<string,Player> Players;public Ability[] Opponent;
  public Ability[] Rows(string[] ids,string tactic,Club club,Club foe,bool home){string star=ids.OrderByDescending(id=>Players[id].Value).First();var rows=ids.Select((id,i)=>Football.Weight(Players[id],i,club.Philosophy,tactic,foe.Philosophy,Rules.Round((Players[id].Favor+7)/2.0),home&&!F.Neutral,id==star)).ToArray();if(tactic=="角球战术"){foreach(var row in rows)row.R*=1.015;rows.Take(9).OrderByDescending(x=>x.S).First().S*=1.015;}return rows;}
  static double Chance(Ability[] a,Ability[] b){double control=a.Sum(x=>x.G)/Math.Max(1,a.Sum(x=>x.G)+b.Sum(x=>x.G));double route=0;for(int lane=0;lane<3;lane++){double green=Enumerable.Range(0,9).Where(i=>i%3==lane).Sum(i=>a[i].G)+(lane==1?a[9].G:0);double red=Football.LaneRed(a,lane),blue=Football.LaneBlue(b,2-lane);route+=green/Math.Max(1,a.Sum(x=>x.G))*red/Math.Max(1,red+blue);}double shot=Rules.Clamp(.6*Math.Log(1+a.Take(9).Max(x=>x.S)/40),.02,.85),save=Rules.Clamp(.6*Math.Log(1+b[9].S/40),.02,.85);return control*route*shot*(1-save);}
  public LiveCoachPlan Evaluate(string[] ids,string tactic){var own=Rows(ids,tactic,C,Foe,Home);double attack=Chance(own,Opponent),against=Chance(Opponent,own);int minute=G.Live.Round*2,margin=Home?G.Live.HomeGoals-G.Live.AwayGoals:G.Live.AwayGoals-G.Live.HomeGoals;double urgency=minute<60?.25:.65;double aw=margin<0?1+urgency:1,dw=margin>0?1+urgency:1;double control=own.Sum(x=>x.G)/Math.Max(1,own.Sum(x=>x.G)+Opponent.Sum(x=>x.G));return new LiveCoachPlan{Tactic=tactic,Lineup=ids,Attack=attack,Against=against,Utility=1000*(aw*attack-dw*against)+60*control};}
 }
 public static List<LiveCoachPlan> Build(Game g,Club c,Fixture f,List<List<SubDecision>> substitutions){
  var m=g.Live;bool home=f.Home==c.Id;var current=home?m.HomeLineup:m.AwayLineup;string tactic=home?m.HomeTactic:m.AwayTactic;int minute=m.Round*2,used=home?m.HomeSubs:m.AwaySubs;var played=home?m.UsedHome:m.UsedAway;
  var e=new Evaluation{G=g,C=c,Foe=g.Club(home?f.Away:f.Home),F=f,Home=home,Players=g.Players.ToDictionary(p=>p.Id)};e.Opponent=e.Rows(home?m.AwayLineup:m.HomeLineup,home?m.AwayTactic:m.HomeTactic,e.Foe,c,!home);
  var baseline=e.Evaluate(current,tactic);baseline.Gain=0;var options=new List<LiveCoachPlan>();int budget=Math.Max(0,Budget(g,c,f)-used);
  foreach(string t in new[]{tactic,"无战术"}.Concat(c.Tactics).Distinct().Where(t=>!f.Neutral||t!="战术犯规")){
   var local=new List<LiveCoachPlan>();
   foreach(var changes in substitutions){
    if(changes.Count>budget)continue;
    var ids=(string[])current.Clone();bool acceptable=true;
    foreach(var change in changes){var old=e.Players[ids[change.slot]];var next=e.Players[change.playerId];int minutes; m.Minutes.TryGetValue(old.Id,out minutes);bool misfit=old.Type!=Rules.Expected(t,change.slot);double before=Football.Score(Football.Weight(old,change.slot,c.Philosophy,t,e.Foe.Philosophy,Rules.Round((old.Favor+7)/2.0),home&&!f.Neutral,false)),after=Football.Score(Football.Weight(next,change.slot,c.Philosophy,t,e.Foe.Philosophy,Rules.Round((next.Favor+7)/2.0),home&&!f.Neutral,false));
     // Give a substitute time to settle; a major correction remains possible.
     if(!played.Take(10).Contains(old.Id)&&minutes<20&&!(misfit&&after>before*1.3)){acceptable=false;break;}
     if(next.Type!=Rules.Expected(t,change.slot)&&!misfit||after<before*.95&&!misfit){acceptable=false;break;}
     if(change.slot==9&&minute<70&&!misfit&&old.Fatigue<9&&after<before*1.35){acceptable=false;break;}
     ids[change.slot]=next.Id;
    }
    if(!acceptable)continue;try{Football.ValidateLineup(g,c,ids,t);}catch{continue;}
    var option=e.Evaluate(ids,t);option.Subs=changes.Select(x=>new SubDecision{slot=x.slot,playerId=x.playerId}).ToList();Score(option,baseline,minute,tactic);local.Add(option);
   }
   // Jointly assess a swap after substitutions and tactical reweighting.
   foreach(var option in local.OrderByDescending(x=>x.Utility).Take(3).ToArray()){
    var swaps=new List<LiveCoachPlan>();for(int a=0;a<9;a++)for(int b=a+1;b<9;b++){
     var ids=(string[])option.Lineup.Clone();string id=ids[a];ids[a]=ids[b];ids[b]=id;
     var swapped=e.Evaluate(ids,t);swapped.Subs=option.Subs;swapped.Swap=new PositionSwap{a=a,b=b};Score(swapped,baseline,minute,tactic);if(swapped.Utility>option.Utility+.25)swaps.Add(swapped);
    }
    local.AddRange(swaps.OrderByDescending(x=>x.Utility).Take(2));
   }
   options.AddRange(local.Where(x=>x.Utility>baseline.Utility+.25&&Stable(g,c,f,x)).OrderByDescending(x=>x.Utility).Take(3));
  }
  // A one-shot foul cannot be compared by permanent attribute bonuses. Preserve it
  // as an explicitly labelled qualitative choice instead of silently disabling it.
  LiveCoachPlan foul=null;if(!f.Neutral&&tactic!="战术犯规"&&c.Tactics.Contains("战术犯规")&&!m.FoulUsed[home?0:1]){foul=e.Evaluate((string[])current.Clone(),"战术犯规");Score(foul,baseline,minute,tactic);if(!Stable(g,c,f,foul))foul=null;}
  // Zero always means keep the current tactic and all players. Never force an adjustment.
  return new[]{baseline}.Concat(options.OrderByDescending(x=>x.Utility).GroupBy(x=>x.Tactic+"|"+string.Join(",",x.Lineup)).Select(x=>x.First()).Take(foul==null?11:10)).Concat(foul==null?new LiveCoachPlan[0]:new[]{foul}).ToList();
 }
 static void Score(LiveCoachPlan option,LiveCoachPlan baseline,int minute,string current){double cost=option.Subs.Count*(minute<30?2:minute<60?1.2:minute<76?.5:.15)+(option.Tactic==current?0:.6)+(option.Swap==null?0:.35);option.Utility-=cost;option.Gain=option.Utility-baseline.Utility;}
 internal static bool Stable(Game g,Club c,Fixture f,LiveCoachPlan plan){
  var m=g.Live;bool home=f.Home==c.Id;string current=home?m.HomeTactic:m.AwayTactic,opponent=home?m.AwayTactic:m.HomeTactic;int minute=m.Round*2,margin=home?m.HomeGoals-m.AwayGoals:m.AwayGoals-m.HomeGoals;
  var recent=(f.Decisions??new List<OpponentDecisionRecord>()).Where(x=>x.ClubId==c.Id&&!String.IsNullOrEmpty(x.BeforeTactic)&&minute-x.Minute>=0&&minute-x.Minute<20&&x.ScoreMargin==margin&&x.OpponentTactic==opponent).ToArray();
  if(plan.Tactic!=current&&plan.Gain<5&&recent.Any(x=>x.BeforeTactic!=x.AfterTactic))return false;
  var lastFoul=(f.Decisions??new List<OpponentDecisionRecord>()).LastOrDefault(x=>x.ClubId==c.Id&&x.AfterTactic=="战术犯规"&&!String.IsNullOrEmpty(x.BeforeTactic)&&x.BeforeTactic!=x.AfterTactic);
  if(plan.Tactic!=current&&current=="战术犯规"&&!m.FoulUsed[home?0:1]&&plan.Gain<5&&lastFoul!=null&&lastFoul.ScoreMargin==margin&&lastFoul.OpponentTactic==opponent)return false;
  if(plan.Swap!=null&&plan.Gain<5&&recent.Any(x=>x.SwapSlots!=null&&x.SwapSlots.OrderBy(i=>i).SequenceEqual(new[]{plan.Swap.a,plan.Swap.b}.OrderBy(i=>i))))return false;
  return true;
 }
 public static void Configure(AgentRequest request,Dictionary<string,object> context,Dictionary<string,object> properties,List<string> required,Game g,Club c,Fixture f){
  request.LivePlans=Build(g,c,f,request.SubPlans);properties.Remove("tactic");properties.Remove("swapPlan");required.Remove("tactic");required.Remove("swapPlan");properties["plan"]=AgentRequest.D("type","integer","enum",Enumerable.Range(0,request.LivePlans.Count).ToArray());
  var review=g.Live.Review;if(review!=null)context["已发生比赛统计"]=AgentRequest.D("说明","数组前3项为主队左中右，后3项为客队左中右；少量样本不要过度反应", "球权",review.Possessions,"攻防成功",review.DuelsWon,"射门",review.Shots,"射正",review.OnTarget);
  context["最近实际调整"]=f.Decisions.Where(x=>x.ClubId==c.Id||x.ClubId==(f.Home==c.Id?f.Away:f.Home)).Reverse().Take(4).Reverse().Select(x=>new{club=x.ClubId,minute=x.Minute,actions=x.Lines}).ToArray();
  context.Remove("合法换位方案");context.Remove("合法换人方案");context["联合调整方案"]=request.LivePlans.Select((p,i)=>new{plan=i,tactic=p.Tactic,subs=p.Subs.Select(s=>new{slot=s.slot,player=request.Codes.First(x=>x.Value==s.playerId).Key}).ToArray(),swap=p.Swap==null?new int[0]:new[]{p.Swap.a,p.Swap.b},gain=Math.Round(p.Gain,2),attack=Math.Round(p.Attack,4),against=Math.Round(p.Against,4),cost=p.Subs.Count,special=p.Tactic=="战术犯规"?"一次射正拦截及4周停赛代价未计入数值gain，必须自行权衡；不是无代价防守加成":""}).ToArray();
  context["换人管理"]=AgentRequest.D("建议累计上限",Budget(g,c,f),"说明","AI候选管理：30分钟前累计至多1人、60分钟前2人（落后至少2球可3人）、76分钟前4人，其后5人；不限制玩家。刚换上不足20分钟不轻易再换。当前游戏不会随本场分钟自然扣能力，不可凭空假设球员踢久必然变弱。机会指数为公开能力估值，不是真实进球期望、胜率或未来结果。");
  request.System="你是与玩家对战的足球教练。只选联合调整方案plan，0是完整保持现状。方案已同时评估战术、换人和换位；不能再分别输出tactic、swapPlan或自填人员。gain是按比分和时间加权并扣除调整代价的启发式收益，不是胜率；attack越高、against越低通常越好。领先后期注意防守，落后后期兼顾进攻风险；早期保留名额，不要仅因到了决策节点就调整。结合本周赛程、板凳深度及本队目标选取方案，可始终保持现状。只输出指定JSON和简短reason。";
 }
}
}
