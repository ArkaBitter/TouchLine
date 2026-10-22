using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Collections.Generic;
namespace Touchline {
static class LiveQualityTests {
 static int checks;static void Check(bool ok,string text){if(!ok)throw new Exception(text);checks++;}
 static string Root{get{return Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"test-results","live-quality");}}
 static AgentRequest Request(Game g){var f=g.Fixtures.First();return AgentRequest.Build(g,g.Club(f.Away),"临场",f);}
 public static void Run(bool real){Directory.CreateDirectory(Root);string original=Storage.Root;typeof(Storage).GetField("Root").SetValue(null,Path.Combine(Root,"isolated-profile"));try{
  if(real){Real();return;}var g=LiveCoachingTests.Match();var f=g.Fixtures.First();var c=g.Club(f.Away);var m=g.Live;
  foreach(int minute in new[]{10,30,44,50,60,70,80}){m.Round=minute/2;m.AwaySubs=0;var r=Request(g);Check(r.LivePlans.Count>=1&&r.LivePlans.Count<=12,"bounded joint menu");Check(r.LivePlans[0].Subs.Count==0&&r.LivePlans[0].Swap==null&&r.LivePlans[0].Tactic==m.AwayTactic,"zero preserves entire current setup");
   foreach(var p in r.LivePlans){Check(p.Subs.Count<=LiveCoachPlans.Budget(g,c,f),"time-aware allowance");Check(p==r.LivePlans[0]||p.Gain>.25||p.Tactic=="战术犯规","nonzero plans improve net estimate");Football.ValidateLineup(g,c,p.Lineup,p.Tactic);}
  }
  m.Round=22;m.AwaySubs=2;Check(Request(g).LivePlans.All(p=>p.Subs.Count==0),"half-time retains three unused places");m.HomeGoals=2;Check(LiveCoachPlans.Budget(g,c,f)==3,"two-goal deficit relaxes early allowance");m.HomeGoals=0;m.AwaySubs=5;Check(Request(g).LivePlans.All(p=>p.Subs.Count==0),"quota exhausted still permits tactic/position options");m.AwaySubs=0;
  // Make a clear useful replacement, then protect it from being immediately withdrawn.
  var bench=g.Squad(c.Id).First(p=>!m.UsedAway.Contains(p.Id));bench.Type="前锋";bench.Red=300;bench.Green=300;bench.Blue=300;bench.Shot=300;bench.Foot=0;
  m.Round=30;var useful=Request(g);Check(useful.LivePlans.Any(p=>p.Subs.Any(s=>s.playerId==bench.Id)),"strong useful replacement remains available");Football.Substitute(g,false,0,bench.Id);m.Minutes[bench.Id]=10;var recent=Request(g);Check(recent.LivePlans.All(p=>p.Subs.All(s=>m.AwayLineup[s.slot]!=bench.Id)),"recent substitute is not immediately replaced");
  var inferior=LiveCoachingTests.Match();var fi=inferior.Fixtures.First();var ci=inferior.Club(fi.Away);foreach(var p in inferior.Squad(ci.Id).Where(p=>!inferior.Live.UsedAway.Contains(p.Id))){p.Red=p.Green=p.Blue=p.Shot=0;p.Black=10;}inferior.Live.Round=30;Check(Request(inferior).LivePlans.All(p=>p.Subs.Count==0),"no pointless replacement by vastly weaker bench");
  var neutral=LiveCoachingTests.Match();neutral.Fixtures.First().Neutral=true;Check(Request(neutral).LivePlans.All(p=>p.Tactic!="战术犯规"),"final excludes tactical foul");
  string before=Storage.Encode(inferior);Request(inferior);Check(Storage.Encode(inferior)==before,"candidate evaluation does not execute actions or consume RNG");
  var stable=LiveCoachingTests.Match();var sf=stable.Fixtures.First();var sc=stable.Club(sf.Away);stable.Live.Round=25;sf.Decisions.Add(new OpponentDecisionRecord{ClubId=sc.Id,BeforeTactic="无战术",AfterTactic=stable.Live.AwayTactic,OpponentTactic=stable.Live.HomeTactic,Minute=44,ScoreMargin=0,SwapSlots=new[]{0,2}});
  var small=new LiveCoachPlan{Tactic="角球战术",Gain=1};Check(!LiveCoachPlans.Stable(stable,sc,sf,small),"recent tactics not immediately abandoned for tiny gain");small.Gain=6;Check(LiveCoachPlans.Stable(stable,sc,sf,small),"major benefit can override stability");small.Gain=1;stable.Live.HomeGoals=1;Check(LiveCoachPlans.Stable(stable,sc,sf,small),"score change can trigger tactical response");stable.Live.HomeGoals=0;small.Tactic=stable.Live.AwayTactic;small.Swap=new PositionSwap{a=2,b=0};Check(!LiveCoachPlans.Stable(stable,sc,sf,small),"recent swap not immediately reversed");var copy=Storage.Clone(stable);Check(copy.Fixtures.First().Decisions.Last().SwapSlots.Length==2,"structured tactical memory survives save/load");
  LiveCoachingTests.Run(false);File.WriteAllText(Path.Combine(Root,"result.json"),Storage.Encode(new{Passed=true,Checks=checks,ExistingBoundaryAndAiRegression=true,FullSeasonRun=false}));
 }catch(Exception ex){File.WriteAllText(Path.Combine(Root,real?"real-failure.txt":"failure.txt"),ex.ToString());Environment.ExitCode=1;}finally{LocalRuntime.Stop();typeof(Storage).GetField("Root").SetValue(null,original);}}
 class Tracked:IClubAgent {
  public List<object> Decisions=new List<object>();public List<int> EarlyCounts=new List<int>();readonly IClubAgent model=new LocalModelAgent(new AgentConfig{Enabled=true,Endpoint="http://127.0.0.1:17845/v1/chat/completions",Model="touchline-local",TimeoutSeconds=180});
  public async Task<AgentDecision> Decide(Game g,Club c,string phase,Fixture f,CancellationToken ct){var spec=AgentRequest.Build(g,c,phase,f);bool home=f.Home==c.Id;int used=home?g.Live.HomeSubs:g.Live.AwaySubs;var watch=Stopwatch.StartNew();var d=await model.Decide(g,c,phase,f,ct);watch.Stop();Check(!d.reason.Contains("固定程序兜底"),"real player match remains model controlled");Check(d.plan>=0&&d.plan<spec.LivePlans.Count,"real selected joint option");var plan=spec.LivePlans[d.plan];Check(used+d.substitutions.Count<=LiveCoachPlans.Budget(g,c,f),"real reserves quota");if(g.Live.Round*2<60)EarlyCounts.Add(used+d.substitutions.Count);Decisions.Add(new{Minute=g.Live.Round*2,Phase=phase,Seconds=watch.Elapsed.TotalSeconds,Plan=d.plan,Tactic=d.tactic,Substitutions=d.substitutions.Count,Swaps=d.swaps.Count,Cumulative=used+d.substitutions.Count,NetEstimateGain=Math.Round(plan.Gain,2)});return d;}
 }
 static void Real(){var all=new List<object>();foreach(int seed in new[]{20261031,20261101,20261102}){var g=LiveCoachingTests.Match(seed);var coach=new Tracked();var log=new List<string>();var watch=Stopwatch.StartNew();for(int i=0;i<9&&g.Live!=null;i++)Controller.PlaySegment(g,coach,CancellationToken.None,log.Add,5).GetAwaiter().GetResult();watch.Stop();Check(g.Live==null&&coach.Decisions.Count==9,"real bounded match completed with nine model decisions");Check(coach.EarlyCounts.All(x=>x<=3),"no five substitutions before halftime");var f=g.Fixtures.First();all.Add(new{Seed=seed,Seconds=watch.Elapsed.TotalSeconds,Score=f.HomeGoals+":"+f.AwayGoals,Decisions=coach.Decisions,Completed=true});File.WriteAllText(Path.Combine(Root,"real-result.json"),Storage.Encode(new{Passed=true,Matches=all,Checks=checks,FullSeasonRun=false,StrategyOptimalityProven=false}));}}
}
}
