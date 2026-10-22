using System;
using System.Linq;
using System.Collections.Generic;
namespace Touchline {
// Budget-safe tools. The model chooses a plan, including retaining every point.
public static class TrainingPlans {
 static string Attribute(Player p){return p.Type=="前锋"?"红":p.Type=="中场"?"绿":p.Type=="门将"?"射门":"蓝";}
 public static List<List<TrainingDecision>> Build(Game g,Club c){
  var plans=new List<List<TrainingDecision>>{new List<TrainingDecision>()};
  if(g.Week%6!=0||c.Upgrade<=0)return plans;
  var squad=g.Squad(c.Id).ToArray();if(squad.Length==0)return plans;
  var targets=new List<Player>();
  foreach(string type in Rules.Types){var ps=squad.Where(p=>p.Type==type).ToArray();if(ps.Length==0)continue;targets.Add(ps.OrderByDescending(p=>p.Appearances).ThenByDescending(p=>p.Value).First());targets.Add(ps.OrderBy(p=>p.Age).ThenByDescending(p=>p.Value).First());}
  targets=targets.GroupBy(p=>p.Id).Select(x=>x.First()).ToList();
  foreach(var p in targets){
   foreach(int budget in new[]{Math.Max(1,c.Upgrade/2),c.Upgrade}.Distinct()){
    plans.Add(new List<TrainingDecision>{new TrainingDecision{playerId=p.Id,attribute=Attribute(p),levels=budget}});
   }
   if(p.Black>0)plans.Add(new List<TrainingDecision>{new TrainingDecision{playerId=p.Id,attribute="黑",levels=Math.Min(c.Upgrade,(int)Math.Ceiling(p.Black))}});
  }
  foreach(var order in new[]{targets.OrderByDescending(p=>p.Appearances),targets.OrderBy(p=>p.Age),targets.OrderBy(p=>p.Value)}){
   var ps=order.Take(Math.Min(3,c.Upgrade)).ToArray();int remaining=c.Upgrade;var changes=new List<TrainingDecision>();
   for(int i=0;i<ps.Length;i++){int n=remaining/(ps.Length-i);changes.Add(new TrainingDecision{playerId=ps[i].Id,attribute=Attribute(ps[i]),levels=n});remaining-=n;}
   plans.Add(changes);
  }
  return plans;
 }
}
}
