using System;
using System.Linq;
using System.Collections.Generic;
namespace Touchline {
public static class DecisionOutput {
 static string Name(Game g,string id){var p=g.Players.FirstOrDefault(x=>x.Id==id);return p==null?id:p.Name;}
 public static void Apply(Game g,Club c,AgentDecision d,string phase,Fixture f,Action<string> progress){
  bool live=f!=null&&g.Live!=null&&LiveCoaching.Phase(phase),home=f!=null&&f.Home==c.Id;string previous=live?(home?g.Live.HomeTactic:g.Live.AwayTactic):"",foeTactic=live?(home?g.Live.AwayTactic:g.Live.HomeTactic):"";int margin=live?(home?g.Live.HomeGoals-g.Live.AwayGoals:g.Live.AwayGoals-g.Live.HomeGoals):0;
  var lines=new List<string>();string prefix="【"+c.Name+" · "+phase+"】";
  if(phase=="阶段规划"){lines.Add("重点赛程："+(d.importantMatches.Length==0?"不额外指定":string.Join("；",d.importantMatches.Select(id=>{var fx=g.Fixtures.First(x=>x.Id==id);return fx.Competition+" W"+fx.Week+" "+g.Club(fx.Home).Name+" vs "+g.Club(fx.Away).Name;}).ToArray())));}
  if(phase=="赛前"){
   lines.Add("选用战术："+d.tactic);
   lines.Add("首发："+string.Join("；",d.lineup.Select((id,i)=>Rules.Slots[i]+"："+Name(g,id)).ToArray()));
  }else if(LiveCoaching.Phase(phase)&&g.Live!=null){
   var current=f.Home==c.Id?g.Live.HomeLineup:g.Live.AwayLineup;string old=f.Home==c.Id?g.Live.HomeTactic:g.Live.AwayTactic;
   string actual=f.Neutral&&d.tactic=="战术犯规"?"无战术":d.tactic;
   lines.Add(old==actual?"保持战术："+actual:"调整战术："+old+" → "+actual);
   foreach(var sub in d.substitutions)lines.Add("换人："+Name(g,current[sub.slot])+" ↓ ／ "+Name(g,sub.playerId)+" ↑（"+Rules.Slots[sub.slot]+"）");
   if(d.substitutions.Count==0)lines.Add("本次不换人。");var after=(string[])current.Clone();foreach(var sub in d.substitutions)after[sub.slot]=sub.playerId;foreach(var swap in d.swaps){lines.Add("换位："+Name(g,after[swap.a])+"（"+Rules.Slots[swap.a]+" → "+Rules.Slots[swap.b]+"）／"+Name(g,after[swap.b])+"（"+Rules.Slots[swap.b]+" → "+Rules.Slots[swap.a]+"）");string id=after[swap.a];after[swap.a]=after[swap.b];after[swap.b]=id;}
  }else if(phase=="续约")lines.Add(d.release.Length==0?(d.retire.Length==0?"全部球员续约。":"其余球员续约。 "):"不续约："+string.Join("、",d.release.Select(id=>Name(g,id)).ToArray()));
  foreach(string id in d.retire)lines.Add("主动退役："+Name(g,id));
  foreach(var tr in d.training)lines.Add("训练："+Name(g,tr.playerId)+" · "+tr.attribute+" · "+tr.levels+"级");
  AgentTurns.Apply(g,c,d,phase,f);
  foreach(string id in d.listPlayers){var p=g.Players.FirstOrDefault(x=>x.Id==id);lines.Add("挂牌："+Name(g,id)+(p!=null&&p.Listed?"（已挂牌）":"（未执行）"));}
  foreach(var proposal in d.offers){var offer=g.Offers.LastOrDefault(o=>o.PlayerId==proposal.playerId&&o.BuyerId==c.Id&&o.Season==g.Season);lines.Add("采购提案："+Name(g,proposal.playerId)+"，报价 "+proposal.fee.ToString("F2")+"；"+(offer==null?"未生成有效报价":offer.Status));}
  foreach(string id in d.acceptOffers.Concat(d.rejectOffers)){var o=g.Offers.First(x=>x.Id==id);lines.Add((d.acceptOffers.Contains(id)?"接受":"拒绝")+"报价："+Name(g,o.PlayerId)+"；执行结果："+o.Status);}
  if(lines.Count==0)lines.Add("本次不新增经营操作。");
  if(f!=null&&c.Id!=g.PlayerClubId&&(f.Home==g.PlayerClubId||f.Away==g.PlayerClubId)&&(phase=="赛前"||LiveCoaching.Phase(phase))){if(f.Decisions==null)f.Decisions=new List<OpponentDecisionRecord>();f.Decisions.Add(new OpponentDecisionRecord{ClubId=c.Id,Phase=phase,Minute=phase=="赛前"?0:g.Live==null?0:g.Live.Round*2,Lines=lines.ToArray(),BeforeTactic=previous,AfterTactic=d.tactic,OpponentTactic=foeTactic,ScoreMargin=margin,SwapSlots=d.swaps.Count==0?new int[0]:new[]{d.swaps[0].a,d.swaps[0].b}});}
  foreach(string line in lines){string message=prefix+line;g.Log(message);progress(message);}
  if(!String.IsNullOrWhiteSpace(d.reason))progress(prefix+"决策摘要："+d.reason);
 }
 public static void Result(Game g,Fixture f,Action<string> progress){progress("【比赛结束 · "+f.Competition+" / "+f.Stage+" / 第"+f.Week+"周】"+g.Club(f.Home).Name+" "+f.HomeGoals+" : "+f.AwayGoals+" "+g.Club(f.Away).Name);}
}
}

