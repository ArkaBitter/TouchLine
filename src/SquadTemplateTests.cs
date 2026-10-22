using System;
using System.IO;
using System.Linq;
using System.Diagnostics;
namespace Touchline {
public static class SquadTemplateTests {
 static int checks;
 static void Check(bool ok,string label){if(!ok)throw new Exception("Squad template: "+label);checks++;}
 public static void Run(){
  var g=GameFactory.New(20261029);g.PlayerClubId=g.Clubs[0].Id;var c=g.Clubs[1];var f=g.Fixtures.First(x=>x.Home==c.Id||x.Away==c.Id);f.Competition="联赛";
  foreach(string competition in new[]{"联赛","欧冠","欧联","欧冠资格赛","一级杯赛","二级杯赛"})foreach(string stage in new[]{"小组","八强","四强","半决赛","决赛"})Check(SquadTemplates.Important(new Fixture{Competition=competition,Stage=stage})==(competition=="联赛"||competition=="欧冠"||competition=="欧联"||competition=="欧冠资格赛"||competition=="一级杯赛"&&stage!="小组"||competition=="二级杯赛"&&new[]{"四强","半决赛","决赛"}.Contains(stage)),"competition policy");
  var baseline=SquadTemplates.Plans(g,c,f);Check(c.SquadTemplateBuilds==1,"lazy initialization");var template=c.SquadTemplates.First(x=>x.Tactic=="无战术");Check(baseline.First(x=>x.Tactic=="无战术").Lineup.SequenceEqual(template.First),"stable first team");
  var copy=Storage.Clone(g);SquadTemplates.Plans(copy,copy.Club(c.Id),f);Check(copy.Club(c.Id).SquadTemplateBuilds==1,"cache survives save serialization");
  string injured=template.First[0];g.Players.First(p=>p.Id==injured).Injury=2;var repaired=SquadTemplates.Plans(g,c,f).First(x=>x.Tactic=="无战术").Lineup;Check(!repaired.Contains(injured)&&repaired.Where((id,i)=>id!=template.First[i]).Count()==1,"injury only replaces affected slot");Check(template.First[0]==injured&&c.SquadTemplateBuilds==1,"injury preserves template");g.Players.First(p=>p.Id==injured).Injury=0;Check(SquadTemplates.Plans(g,c,f).First(x=>x.Tactic=="无战术").Lineup.SequenceEqual(template.First),"recovered player returns");
  foreach(var p in g.Squad(c.Id))p.Appearances=20;foreach(var p in g.Squad(c.Id).Where(p=>!template.Second.Contains(p.Id)))p.Appearances=0;f.Competition="一级杯赛";f.Stage="小组";foreach(var plan in SquadTemplates.Plans(g,c,f))Football.ValidateLineup(g,c,plan.Lineup,plan.Tactic);Check(c.SquadTemplateBuilds==1,"appearance changes reuse templates");
  f.Competition="联赛";var tired=g.Players.First(p=>p.Id==template.First[3]);tired.Fatigue=90;var rest=SquadTemplates.Plans(g,c,f).First(x=>x.Tactic=="无战术").Lineup;Check(!rest.Contains(tired.Id),"severe fatigue allows suitable reserve");Check(rest.Where((id,i)=>id!=template.First[i]).Count()<=2,"discretionary changes limited");tired.Fatigue=0;
  string keeper=template.First[9];g.Players.First(p=>p.Id==keeper).Injury=3;foreach(var plan in SquadTemplates.Plans(g,c,f))Football.ValidateLineup(g,c,plan.Lineup,plan.Tactic);g.Players.First(p=>p.Id==keeper).Injury=0;
  var lost=g.Players.First(p=>p.Id==injured);lost.ClubId="";SquadTemplates.Plans(g,c,f);Check(c.SquadTemplateBuilds==2&&c.SquadTemplates.All(x=>!x.First.Contains(injured)&&!x.Second.Contains(injured)),"transfer invalidates cache");
  var player=g.Squad(c.Id).First();player.Red+=1;SquadTemplates.Plans(g,c,f);Check(c.SquadTemplateBuilds==3,"ability changes invalidate cache");g.Season++;SquadTemplates.Plans(g,c,f);Check(c.SquadTemplateBuilds==4,"season invalidates cache");
  var names=g.Squad(c.Id).Take(2).ToArray();string a=names[0].Id,b=names[1].Id;PlayerEditing.Rename(names[0],"同名球员");PlayerEditing.Rename(names[1],"同名球员");SquadTemplates.Plans(g,c,f);Check(names[0].Id==a&&names[1].Id==b&&a!=b&&c.SquadTemplateBuilds==4,"duplicate names keep separate IDs and cache");
  var thin=GameFactory.New(20261030);var thinClub=thin.Clubs[1];var roster=thin.Squad(thinClub.Id).ToArray();foreach(var p in roster)p.Injury=0;var keeperOnly=roster.First(p=>p.Type=="门将");foreach(var p in roster.Where(p=>p.Type=="门将"&&p!=keeperOnly))p.Type="后卫";var odd=roster.Where(p=>p!=keeperOnly).Take(10).Select(p=>p.Id).ToArray();odd[0]=keeperOnly.Id;var fixedKeeper=SquadTemplates.Repair(thin,thinClub,odd,"无战术",false);Football.ValidateLineup(thin,thinClub,fixedKeeper,"无战术");Check(fixedKeeper[9]==keeperOnly.Id,"keeper moved from outfield without duplicate");
  foreach(var clubTest in thin.Clubs){var fixtureTest=new Fixture{Competition="一级杯赛",Stage="小组",Home=clubTest.Id,Away=thin.Clubs.First(x=>x.Id!=clubTest.Id).Id};foreach(var candidate in SquadTemplates.Plans(thin,clubTest,fixtureTest))Football.ValidateLineup(thin,clubTest,candidate.Lineup,candidate.Tactic);}Check(true,"all thirty reserve squads legal");
  // Compare the replaced local candidate generation, not LLM network latency.
  var bench=GameFactory.New(20261029);var club=bench.Clubs[1];var fixture=bench.Fixtures.First(x=>x.Home==club.Id||x.Away==club.Id);SquadTemplates.Plans(bench,club,fixture);
  Action legacy=()=>{foreach(string tactic in new[]{"无战术"}.Concat(club.Tactics)){var ids=Football.Optimize(bench,club,tactic);Football.LineupScore(bench,club,ids,tactic);}};
  legacy();var watch=Stopwatch.StartNew();for(int i=0;i<200;i++)legacy();watch.Stop();double old=watch.Elapsed.TotalMilliseconds;watch.Restart();for(int i=0;i<200;i++)SquadTemplates.Plans(bench,club,fixture);watch.Stop();double cached=watch.Elapsed.TotalMilliseconds;Check(club.SquadTemplateBuilds==1,"repeated fixtures reuse cache");
  Check(AiReliabilityTestsScope(),"related AI regressions");string root=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"test-results","squad-templates");Directory.CreateDirectory(root);File.WriteAllText(Path.Combine(root,"result.json"),Storage.Encode(new{Passed=true,Checks=checks,Iterations=200,LegacyMilliseconds=old,CachedMilliseconds=cached,Speedup=old/cached,Scope="Local candidate generation only, warm cache. No model speedup claim, no full season, no user saves."}));
 }
 static bool AiReliabilityTestsScope(){AiReliabilityTests.Run(false);return true;}
}
}
