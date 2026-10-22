using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Touchline {
public static class Controller {
 public static async Task Advance(Game g,IClubAgent agent,CancellationToken ct,Action<string> progress,Action<Game> checkpoint=null){
  if(g.Live!=null){await PlaySegment(g,agent,ct,progress,5);return;}
  if(g.Phase=="常规赛"||g.Phase=="欧冠资格赛"){
   if(g.Phase=="常规赛"&&(g.Week-1)%6==0)await StrategicPlanning.Review(g,agent,ct,progress,checkpoint);
   while(true){ct.ThrowIfCancellationRequested();Competitions.Progress(g);var f=Competitions.Next(g);if(f==null){if(g.Phase=="常规赛"&&g.Week%6==0)await StrategicPlanning.Review(g,agent,ct,progress,checkpoint);SeasonFlow.WeekEnd(g);progress("【周末更新】当前第"+g.Week+"周，阶段："+g.Phase);return;}
    var a=g.Club(f.Home);var b=g.Club(f.Away);int ha=g.Squad(a.Id).Count(p=>p.Injury==0),hb=g.Squad(b.Id).Count(p=>p.Injury==0);
    if(ha<10||hb<10){f.HomeGoals=ha<10?0:3;f.AwayGoals=hb<10?0:3;string forfeit="可用人数不足10，判定弃权："+a.Name+" "+f.HomeGoals+":"+f.AwayGoals+" "+b.Name;g.Log(forfeit);f.Commentary.Add(forfeit);progress("【弃权】可用球员不足10人。");DecisionOutput.Result(g,f,progress);Competitions.Progress(g);if(checkpoint!=null)checkpoint(g);continue;}
    progress("【比赛准备 · "+f.Competition+"】"+a.Name+" vs "+b.Name);await AgentTurns.Prepare(g,f,agent,ct,progress);Football.Begin(g,f);
    if(f.Home==g.PlayerClubId||f.Away==g.PlayerClubId){progress("【玩家比赛就绪】关闭日志窗口后可推进比赛、换人和调整战术。");return;}
    await PlaySegment(g,agent,ct,progress,45);if(checkpoint!=null)checkpoint(g);
   }
  }
  else if(new[]{"续约","自主转会","需求转会"}.Contains(g.Phase))await AgentTurns.Window(g,agent,ct,progress);
  else SeasonFlow.Advance(g);
 }
 public static async Task PlaySegment(Game g,IClubAgent agent,CancellationToken ct,Action<string> progress,int rounds){
  int target=Math.Min(45,g.Live.Round+rounds);if(rounds<=0)return;await LiveCoaching.Boundary(g,agent,ct,progress);while(g.Live!=null&&g.Live.Round<target){ct.ThrowIfCancellationRequested();Football.Step(g);await LiveCoaching.Boundary(g,agent,ct,progress);}
  if(g.Live!=null&&g.Live.Round==45){var finished=g.Fixtures.First(f=>f.Id==g.Live.FixtureId);Football.Finish(g);DecisionOutput.Result(g,finished,progress);Competitions.Progress(g);}
 }
}
}

