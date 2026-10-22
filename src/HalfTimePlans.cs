using System;
using System.Linq;
using System.Collections.Generic;
namespace Touchline {
public static class HalfTimePlans {
 public static List<List<SubDecision>> Build(Game g,Club c,Fixture f){
  var m=g.Live;bool home=f.Home==c.Id;var lineup=home?m.HomeLineup:m.AwayLineup;var used=home?m.UsedHome:m.UsedAway;
  int remaining=5-(home?m.HomeSubs:m.AwaySubs);var plans=new List<List<SubDecision>>{new List<SubDecision>()};if(remaining<=0)return plans;
  var bench=g.Squad(c.Id).Where(p=>p.Injury==0&&!used.Contains(p.Id)&&!lineup.Contains(p.Id)).ToArray();if(bench.Length==0)return plans;
  string tactic=home?m.HomeTactic:m.AwayTactic;var opponent=g.Club(home?f.Away:f.Home);
  Func<Player,int,double> score=(p,slot)=>Football.Score(Football.Weight(p,slot,c.Philosophy,tactic,opponent.Philosophy,Rules.Round((p.Favor+7)/2.0),home&&!f.Neutral,false));
  var current=lineup.Select(id=>g.Players.First(p=>p.Id==id)).ToArray();
  // Diverse legal candidates; the model, not this generator, chooses the action.
  for(int slot=0;slot<10;slot++)foreach(var p in bench.Where(p=>slot!=9||p.Type=="门将"||!g.Squad(c.Id).Any(k=>k.Type=="门将"&&k.Injury==0)).OrderByDescending(p=>score(p,slot)).Take(2))plans.Add(new List<SubDecision>{new SubDecision{slot=slot,playerId=p.Id}});
  var orders=new[]{
   Enumerable.Range(0,10).OrderByDescending(i=>current[i].Fatigue).ThenBy(i=>score(current[i],i)).ToArray(),
   Enumerable.Range(0,10).OrderByDescending(i=>bench.Max(p=>score(p,i))-score(current[i],i)).ToArray(),
   new[]{0,1,2,3,4,5,6,7,8,9},new[]{6,7,8,9,3,4,5,0,1,2}
  };
  foreach(var order in orders){var changes=new List<SubDecision>();foreach(int slot in order){if(changes.Count>=remaining)break;var p=bench.Where(x=>(slot!=9||x.Type=="门将"||!g.Squad(c.Id).Any(k=>k.Type=="门将"&&k.Injury==0))&&changes.All(s=>s.playerId!=x.Id)).OrderByDescending(x=>score(x,slot)).FirstOrDefault();if(p==null)continue;changes.Add(new SubDecision{slot=slot,playerId=p.Id});if(changes.Count>=2)plans.Add(changes.Select(s=>new SubDecision{slot=s.slot,playerId=s.playerId}).ToList());}}
  return plans.GroupBy(p=>string.Join("|",p.OrderBy(s=>s.slot).Select(s=>s.slot+":"+s.playerId))).Select(x=>x.First()).ToList();
 }
}
}
