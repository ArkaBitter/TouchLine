using System;
using System.Linq;
namespace Touchline {
public static class AutomaticCoach {
 public static void HalfTime(Game g,Club c,Fixture f,AgentDecision d){
  bool home=f.Home==c.Id;var m=g.Live;var lineup=home?m.HomeLineup:m.AwayLineup;var used=home?m.UsedHome:m.UsedAway;int left=5-(home?m.HomeSubs:m.AwaySubs);var next=(string[])lineup.Clone();
  bool rotate=WorldIdentity.AllowRotation(c,f)||g.Fixtures.Any(x=>!x.Played&&x.Id!=f.Id&&x.Week==g.Week&&(x.Home==c.Id||x.Away==c.Id));
  foreach(int slot in Enumerable.Range(0,10).OrderByDescending(i=>g.Players.First(p=>p.Id==lineup[i]).Fatigue)){
   if(d.substitutions.Count>=Math.Min(2,left))break;var old=g.Players.First(p=>p.Id==lineup[slot]);string expected=Rules.Expected(d.tactic,slot);
   var bench=g.Squad(c.Id).Where(p=>p.Injury==0&&!used.Contains(p.Id)&&!next.Contains(p.Id)&&p.Type==expected).OrderByDescending(p=>Score(g,c,f,d.tactic,p,slot)).FirstOrDefault();if(bench==null)continue;
   double ratio=Score(g,c,f,d.tactic,bench,slot)/Math.Max(1,Score(g,c,f,d.tactic,old,slot));
   if(ratio>=1.05||rotate&&ratio>=.9||old.Fatigue>=6&&ratio>=.85){d.substitutions.Add(new SubDecision{slot=slot,playerId=bench.Id});next[slot]=bench.Id;}
  }
  int own=home?m.HomeGoals:m.AwayGoals,other=home?m.AwayGoals:m.HomeGoals;string star=next.Select(id=>g.Players.First(p=>p.Id==id)).OrderByDescending(p=>p.Value).First().Id;string best=d.tactic;double value=double.NegativeInfinity;
  foreach(string tactic in new[]{"无战术"}.Concat(c.Tactics).Where(t=>t!="战术犯规"&&(t!="摆大巴"||d.tactic==t))){double sum=0;for(int i=0;i<10;i++){var p=g.Players.First(x=>x.Id==next[i]);var a=Football.Weight(p,i,c.Philosophy,tactic,g.Club(home?f.Away:f.Home).Philosophy,Rules.Round((p.Favor+7)/2.0),home&&!f.Neutral,p.Id==star);sum+=Football.Score(a)+(own<other?.08*a.R:own>other?.08*a.B:0);}if(sum>value){value=sum;best=tactic;}}
  d.tactic=best;d.reason="[自动教练] 根据比分、同周赛程与同位置替补能力调整；换人"+d.substitutions.Count+"次。";
 }
 static double Score(Game g,Club c,Fixture f,string tactic,Player p,int slot){return Football.Score(Football.Weight(p,slot,c.Philosophy,tactic,g.Club(f.Home==c.Id?f.Away:f.Home).Philosophy,Rules.Round((p.Favor+7)/2.0),f.Home==c.Id&&!f.Neutral,false));}
}
}
