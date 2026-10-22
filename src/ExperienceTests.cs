using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
namespace Touchline {
public static class ExperienceTests {
 static void Check(bool value,string text){if(!value)throw new Exception("Experience: "+text);}
 class Planner:IClubAgent {public int Calls;public Task<AgentDecision> Decide(Game g,Club c,string phase,Fixture f,CancellationToken ct){Calls++;var d=new AgentDecision();d.training.Add(new TrainingDecision{playerId=g.Squad(c.Id).First().Id,attribute="绿",levels=1});return Task.FromResult(d);}}
 public static bool Run(){
  foreach(int min in new[]{0,1,29,30,31,59,60,61,89,90})Check(MatchMinutes.Fatigue(min)==(min<=30?0:min<=60?1:3),"fatigue boundary "+min);
  var g=GameFactory.New(20261021);var f=g.Fixtures.First();Football.Begin(g,f);var m=g.Live;string off=m.HomeLineup[0];for(int i=0;i<15;i++)Football.Step(g);var bench=g.Squad(f.Home).First(p=>!m.UsedHome.Contains(p.Id));Football.Substitute(g,true,0,bench.Id);
  var legacy=Storage.Clone(g);legacy.Live.Minutes=null;MatchMinutes.Ensure(legacy,legacy.Live);Check(legacy.Live.Minutes[off]==30&&legacy.Live.Minutes[bench.Id]==0,"legacy substitution recovery");
  while(g.Live.Round<45)Football.Step(g);Check(m.Minutes.Values.Sum()==1800,"total team minutes");Check(m.Minutes[off]==30&&m.Minutes[bench.Id]==60,"30/60 substitution minutes");Football.Finish(g);Check(g.Players.First(p=>p.Id==off).Fatigue==0&&bench.Fatigue==1,"minutes applied to fatigue");Check(f.Review!=null&&f.Review.Possessions.Sum()==45&&f.Review.OnTarget.Sum()<=f.Review.Shots.Sum(),"match review counters");
  foreach(var p in g.Players)p.Stat("联赛")[0]=1;foreach(int l in new[]{1,2,3})Check(CompetitionStatistics.Players(g,"联赛"+l,0).All(p=>g.Club(p.ClubId).League==l),"league filter");
  var warningGame=Storage.Clone(g);var warningClub=warningGame.Clubs[0];foreach(var p in warningGame.Squad(warningClub.Id)){p.Injury=1;p.Fatigue=6;}warningClub.Funds=-1;var warning=PlayerAdvice.Warnings(warningGame,warningClub);Check(warning.Contains("没有健康门将")&&warning.Contains("不足10人")&&warning.Contains("疲劳较高")&&warning.Contains("季工资"),"squad warnings");
  var c=g.Club(f.Home);c.Upgrade=5;g.Week=6;var training=new AgentDecision();training.training.Add(new TrainingDecision{playerId=g.Squad(c.Id).First().Id,attribute="红",levels=2});LocalModelAgent.ValidateDecision(g,c,training,"阶段规划",null);AgentTurns.Apply(g,c,training,"阶段规划",null);Check(c.Upgrade==3,"six-week training applied");
  g.Week=7;bool denied=false;try{LocalModelAgent.ValidateDecision(g,c,training,"阶段规划",null);}catch{denied=true;}Check(denied,"training outside sixth week rejected");
  int kept=c.Upgrade;var fallback=FixedAgentFallback.Decide(g,c,"阶段规划",null,"test");AgentTurns.Apply(g,c,fallback,"阶段规划",null);Check(c.Upgrade==kept&&fallback.rejectOffers.Length==0&&fallback.reason.Contains("点数保留"),"planning fallback preserves points and does not reject transfers");
  var normal=g.Fixtures.First(x=>!x.Played&&x.Competition=="联赛");g.Club(normal.Home).ImportantFixtures=new[]{normal.Id};Check(CoachingPolicy.RequiresModel(g,normal),"AI chosen important match");
  g.PlayerClubId=g.Clubs[0].Id;g.Week=6;foreach(var club in g.Clubs)club.Upgrade=2;var planner=new Planner();StrategicPlanning.Review(g,planner,CancellationToken.None,s=>{},null).GetAwaiter().GetResult();var resumed=Storage.Clone(g);StrategicPlanning.Review(resumed,planner,CancellationToken.None,s=>{},null).GetAwaiter().GetResult();Check(planner.Calls==29&&resumed.Clubs.Where(x=>x.Id!=g.PlayerClubId).All(x=>x.Upgrade==1),"six-week review is once-only after save/resume");
  return true;
 }
}
}
