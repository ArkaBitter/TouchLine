using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
namespace Touchline {
public static class StrategicPlanning {
 public static int Start(Game g){return g.Week%6==0?g.Week+1:((g.Week-1)/6)*6+1;}
 public static string Key(Game g,Club c){return g.Season+"/"+Start(g)+"/"+c.Id;}
 public static Fixture[] Candidates(Game g,Club c){int start=Start(g);return g.Fixtures.Where(f=>!f.Played&&f.Week>=start&&f.Week<start+6&&(f.Home==c.Id||f.Away==c.Id)).OrderBy(f=>f.Week).ThenBy(Competitions.Order).ToArray();}
 public static AgentRequest Request(Game g,Club c){var r=new AgentRequest();var ps=g.Squad(c.Id).OrderByDescending(p=>p.Value).ToArray();r.Codes=ps.Select((p,i)=>new{id=p.Id,code="p"+(i+1)}).ToDictionary(x=>x.code,x=>x.id);var fs=Candidates(g,c);bool train=g.Week%6==0;
  r.TrainingPlans=TrainingPlans.Build(g,c);
  var props=AgentRequest.D("reason",AgentRequest.D("type","string","maxLength",60),"importantMatches",AgentRequest.D("type","array","maxItems",Math.Min(3,fs.Length),"uniqueItems",true,"items",AgentRequest.D("type","string","enum",fs.Length>0?fs.Select(f=>f.Id).ToArray():new[]{""})),"trainingPlan",AgentRequest.D("type","integer","enum",Enumerable.Range(0,r.TrainingPlans.Count).ToArray()));
  r.Schema=AgentRequest.D("type","object","properties",props,"required",new[]{"reason","importantMatches","trainingPlan"},"additionalProperties",false);
  r.System="你是足球俱乐部经理。为未来六周选出最多3场额外重点比赛importantMatches，考虑积分排名、争冠、欧战资格、直接竞争对手、赛事和经营方针；可为空。玩家比赛、杯赛半决赛/决赛、欧战八强以及临近赛季末的直接排名争夺会额外保障，不必浪费名额重复选。trainingPlan选择已校验预算的训练方案编号，0为保留全部升级点，其余可集中培养、分散培养或只使用部分点数；不得自填training列表。红绿蓝射门增加能力，黑降低失误。优先适合位置和长期目标的球员，不盲目只练最贵球员。只输出JSON和简短原因，不透露隐藏性格。"+(L10n.English?" Write reason in English.":"");
  r.Prompt=Storage.Encode(new{club=c.Name,policy=c.AgentIdentity,season=g.Season,week=g.Week,allowTraining=train,upgrade=c.Upgrade,trainingPlans=r.TrainingPlans.Select((changes,i)=>new{plan=i,cost=changes.Sum(t=>t.levels),changes=changes.Select(t=>new{player=r.Codes.First(x=>x.Value==t.playerId).Key,attribute=t.attribute,levels=t.levels}).ToArray()}).ToArray(),standings=Competitions.League(g,c.League),fixtures=fs.Select(f=>new{id=f.Id,week=f.Week,competition=f.Competition,stage=f.Stage,opponent=g.Club(f.Home==c.Id?f.Away:f.Home).Name}).ToArray(),players=ps.Select((p,i)=>new{code="p"+(i+1),role=p.Type,value=p.Value,red=p.Red,green=p.Green,blue=p.Blue,shot=p.Shot,black=p.Black,apps=p.Appearances,age=p.Age}).ToArray()});return r;
 }
 public static async Task Review(Game g,IClubAgent agent,CancellationToken ct,Action<string> progress,Action<Game> checkpoint){
  if(g.PlanningDone==null)g.PlanningDone=new List<string>();foreach(var c in g.Clubs.Where(c=>c.Id!=g.PlayerClubId)){string key=Key(g,c);if(g.PlanningDone.Contains(key))continue;ct.ThrowIfCancellationRequested();progress("【六周计划】"+c.Name);var d=await agent.Decide(g,c,"阶段规划",null,ct);DecisionOutput.Apply(g,c,d,"阶段规划",null,progress);g.PlanningDone.Add(key);if(checkpoint!=null)checkpoint(g);}
 }
 public static bool RankCritical(Game g,Fixture f){if(f.Competition!="联赛"||f.Week<16)return false;var table=Competitions.League(g,g.Club(f.Home).League);var a=table.First(r=>r.ClubId==f.Home);var b=table.First(r=>r.ClubId==f.Away);return Math.Abs(a.Points-b.Points)<=3&&new[]{1,2,4,6,8}.Any(bound=>Math.Min(a.Rank,b.Rank)<=bound&&Math.Max(a.Rank,b.Rank)>bound);}
}
}
