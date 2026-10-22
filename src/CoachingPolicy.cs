using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
namespace Touchline {
public static class CoachingPolicy {
 public static bool PlayerMatch(Game g,Fixture f){return f!=null&&!String.IsNullOrEmpty(g.PlayerClubId)&&(f.Home==g.PlayerClubId||f.Away==g.PlayerClubId);}
 public static bool RequiresModel(Game g,Fixture f){return f==null||PlayerMatch(g,f)||StrategicPlanning.RankCritical(g,f)||g.Clubs.Any(c=>(c.Id==f.Home||c.Id==f.Away)&&(c.ImportantFixtures??new string[0]).Contains(f.Id))||(f.Stage=="半决赛"||f.Stage=="四强")||f.Stage=="决赛"||((f.Competition=="欧冠"||f.Competition=="欧联")&&f.Stage=="八强");}
 public static async Task<AgentDecision> Decide(Game g,Club c,string phase,Fixture f,IClubAgent agent,CancellationToken ct,Action<string> progress){ct.ThrowIfCancellationRequested();if(RequiresModel(g,f)){progress("本地智能体 · "+phase+"："+c.Name);return await agent.Decide(g,c,phase,f,ct);}progress("自动教练 · "+phase+"："+c.Name);return Routine(g,c,phase,f);}
 public static AgentDecision Routine(Game g,Club c,string phase,Fixture f){if(PlayerMatch(g,f))throw new Exception("此比赛需要本地智能体决策。");var d=new AgentDecision{reason="[自动教练] 常规比赛按合法阵容与轮换需求执行。"};
  if(phase=="赛前"){var spec=AgentRequest.Build(g,c,phase,f);var pool=spec.Plans.Where(p=>p.Tactic!="战术犯规");var plan=pool.OrderByDescending(p=>p.Score).First();d.lineup=plan.Lineup;d.tactic=plan.Tactic;
  }else{d.tactic=f.Home==c.Id?g.Live.HomeTactic:g.Live.AwayTactic;AutomaticCoach.HalfTime(g,c,f,d);}
  LocalModelAgent.ValidateDecision(g,c,d,phase,f);return d;
 }
 public static object MatchContext(Game g,Club c,Fixture f){bool home=f.Home==c.Id;var opponent=g.Club(home?f.Away:f.Home);string[] ids=g.Live==null?opponent.Lineup:home?g.Live.AwayLineup:g.Live.HomeLineup;
  return new{home=home,neutral=f.Neutral,opponentTactic=g.Live==null?opponent.Tactic:home?g.Live.AwayTactic:g.Live.HomeTactic,
   opponentLineup=ids.Select((id,i)=>{var p=g.Players.FirstOrDefault(x=>x.Id==id);return new{slot=i,name=p==null?"":p.Name,red=p==null?0:p.Red,green=p==null?0:p.Green,blue=p==null?0:p.Blue,shot=p==null?0:p.Shot,fatigue=p==null?0:p.Fatigue};}).ToArray(),
   ownLineup=g.Live==null?c.Lineup:home?g.Live.HomeLineup:g.Live.AwayLineup,
   recent=g.Fixtures.Where(x=>x.Played&&(x.Home==c.Id||x.Away==c.Id)).Reverse().Take(3).Select(x=>x.HomeGoals+":"+x.AwayGoals+" "+(x.Home==c.Id?"主":"客")).ToArray()};
 }
 public static object PlanObservation(Game g,Club c,Fixture f,AgentPlan plan){var ps=plan.Lineup.Select(id=>g.Players.First(p=>p.Id==id)).ToArray();var opponent=g.Club(f.Home==c.Id?f.Away:f.Home);double max=ps.Max(p=>p.Value);int star=Array.FindIndex(ps,p=>p.Value==max);var rows=ps.Select((p,i)=>Football.Weight(p,i,c.Philosophy,plan.Tactic,opponent.Philosophy,Rules.Round((p.Favor+7)/2.0),f.Home==c.Id&&!f.Neutral,i==star)).ToArray();return new{plan=plan.Tactic,green=Math.Round(rows.Sum(x=>x.G),1),red=Enumerable.Range(0,3).Select(i=>Math.Round(Football.LaneRed(rows,i),1)).ToArray(),blue=Enumerable.Range(0,3).Select(i=>Math.Round(Football.LaneBlue(rows,i),1)).ToArray(),shot=Math.Round(rows.Take(9).Max(x=>x.S),1)};}
}
}