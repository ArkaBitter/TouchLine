using System;
using System.Linq;
using System.Text;
using System.Globalization;
using System.Collections.Generic;
namespace Touchline {
public class SquadTemplate {public string Tactic="无战术";public string[] First=new string[0],Second=new string[0];}
public static class SquadTemplates {
 public static bool Important(Fixture f){return f.Competition=="联赛"||f.Competition=="欧冠"||f.Competition=="欧联"||f.Competition=="欧冠资格赛"||f.Competition=="一级杯赛"&&f.Stage!="小组"||f.Competition=="二级杯赛"&&new[]{"半决赛","四强","决赛"}.Contains(f.Stage);}
 static string Key(Game g,Club c){var text=new StringBuilder();text.Append(g.Season).Append('|').Append(c.Philosophy).Append('|').Append(string.Join(",",c.Tactics));foreach(var p in g.Squad(c.Id).OrderBy(p=>p.Id)){text.Append('|').Append(p.Id).Append(':').Append(p.Type).Append(':').Append(p.Foot).Append(':').Append(p.Favor);foreach(double v in new[]{p.Red,p.Green,p.Blue,p.Black,p.Shot})text.Append(':').Append(v.ToString("R",CultureInfo.InvariantCulture));}return text.ToString();}
 public static void Ensure(Game g,Club c){
  string key=Key(g,c);if(c.SquadTemplateKey==key&&c.SquadTemplates!=null&&c.SquadTemplates.Count>0)return;
  // Templates describe the healthy, rested squad. Temporary absences never erase a starter.
  var world=new Game{Players=g.Squad(c.Id).Select(p=>new Player{Id=p.Id,ClubId=p.ClubId,Type=p.Type,Foot=p.Foot,Favor=p.Favor,Red=p.Red,Green=p.Green,Blue=p.Blue,Black=p.Black,Shot=p.Shot}).ToList()};
  if(world.Players.Count<10)throw new Exception(c.Name+"可用球员不足10人。");
  var plans=new List<SquadTemplate>();
  foreach(string tactic in new[]{"无战术"}.Concat(c.Tactics).Distinct()){
   string[] first=Football.Optimize(world,c,tactic);var reserve=world.Players.Where(p=>!first.Contains(p.Id)).ToArray();string[] second;
   if(reserve.Length>=10)second=Football.Optimize(world,c,tactic,reserve);
   else{foreach(var p in world.Players)p.Appearances=first.Contains(p.Id)?1000000:0;second=Football.Rotation(world,c);}
   plans.Add(new SquadTemplate{Tactic=tactic,First=first,Second=second});
  }
  c.SquadTemplates=plans;c.SquadTemplateKey=key;c.SquadTemplateBuilds++;
 }
 public static List<AgentPlan> Plans(Game g,Club c,Fixture f){Ensure(g,c);bool main=Important(f);var plans=new List<AgentPlan>();
  foreach(var template in c.SquadTemplates.Where(t=>!f.Neutral||t.Tactic!="战术犯规")){
   string[] lineup=Repair(g,c,main?template.First:template.Second,template.Tactic,!main);
   plans.Add(new AgentPlan{Tactic=template.Tactic,Lineup=lineup,Score=Football.LineupScore(g,c,lineup,template.Tactic)});
  }
  return plans;
 }
 public static void Auto(Game g,Club c){var f=g.Fixtures.Where(x=>!x.Played&&(x.Home==c.Id||x.Away==c.Id)).OrderBy(x=>x.Week).ThenBy(Competitions.Order).FirstOrDefault()??new Fixture{Competition="联赛"};var best=Plans(g,c,f).Where(p=>p.Tactic!="战术犯规"&&p.Tactic!="摆大巴").OrderByDescending(p=>p.Score).First();c.Lineup=best.Lineup;c.Tactic=best.Tactic;}
 static double Score(Club c,Player p,int slot,string tactic){return Football.Score(Football.Weight(p,slot,c.Philosophy,tactic,"",Rules.Round((p.Favor+7)/2.0),false,false));}
 public static string[] Repair(Game g,Club c,string[] original,string tactic,bool opportunities){
  var healthy=g.Squad(c.Id).Where(p=>p.Injury==0).ToArray();if(healthy.Length<10)throw new Exception(c.Name+"可用球员不足10人。");var byId=healthy.ToDictionary(p=>p.Id);var lineup=new string[10];var used=new HashSet<string>();
  foreach(int slot in new[]{9,0,1,2,3,4,5,6,7,8}){
   string id=original!=null&&original.Length==10?original[slot]:null;
   if(id!=null&&byId.ContainsKey(id)&&!used.Contains(id)&&(slot!=9||!healthy.Any(p=>p.Type=="门将")||byId[id].Type=="门将")){lineup[slot]=id;used.Add(id);}
  }
  foreach(int slot in new[]{9,0,1,2,3,4,5,6,7,8})if(lineup[slot]==null){
   var pool=healthy.Where(p=>!used.Contains(p.Id)).ToArray();if(slot==9&&healthy.Any(p=>p.Type=="门将")){pool=healthy.Where(p=>p.Type=="门将"&&!used.Contains(p.Id)).ToArray();if(pool.Length==0){var keeper=healthy.First(p=>p.Type=="门将");int old=Array.IndexOf(lineup,keeper.Id);lineup[old]=null;used.Remove(keeper.Id);pool=new[]{keeper};}}
   var next=pool.OrderByDescending(p=>Score(c,p,slot,tactic)).First();lineup[slot]=next.Id;used.Add(next.Id);
  }
  // At most two discretionary changes; forced injury replacements do not consume this allowance.
  int changes=0;foreach(int slot in Enumerable.Range(0,10).OrderByDescending(s=>byId[lineup[s]].Fatigue)){
   if(changes>=2)break;var old=byId[lineup[slot]];if(!opportunities&&old.Fatigue<6)continue;var alternatives=healthy.Where(p=>!used.Contains(p.Id)&&p.Type==Rules.Expected(tactic,slot)&&(slot!=9||p.Type=="门将"||!healthy.Any(k=>k.Type=="门将"))).Where(p=>old.Fatigue>=6&&p.Fatigue<old.Fatigue&&Score(c,p,slot,tactic)>=.95*Score(c,old,slot,tactic)||opportunities&&p.Appearances+2<old.Appearances&&Score(c,p,slot,tactic)>=.85*Score(c,old,slot,tactic)).OrderBy(p=>opportunities?p.Appearances:p.Fatigue).ThenByDescending(p=>Score(c,p,slot,tactic)).ToArray();
   if(alternatives.Length==0)continue;var next=alternatives[0];used.Remove(old.Id);used.Add(next.Id);lineup[slot]=next.Id;changes++;
  }
  return lineup;
 }
}
}
