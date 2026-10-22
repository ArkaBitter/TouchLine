using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
namespace Touchline {
class InvalidReplyAgent:LocalModelAgent {
 public int Calls;public InvalidReplyAgent():base(new AgentConfig{Enabled=true,Model="test",Endpoint="http://127.0.0.1:17845/v1/chat/completions"}){}
 protected override Task<string> Request(List<object> messages,object schema,CancellationToken ct){Calls++;return Task.FromResult("invalid-json");}
}
static class FallbackTests {
 public static bool Run(){
  var g=GameFactory.New(411);var f=g.Fixtures.First();var c=g.Club(f.Home);Football.Auto(g,c);Football.Auto(g,g.Club(f.Away));
  foreach(string phase in new[]{"赛前","中场","续约","自主转会","需求转会"}){
   if(phase=="中场")Football.Begin(g,f);
   var agent=new InvalidReplyAgent();var d=agent.Decide(g,c,phase,f,CancellationToken.None).GetAwaiter().GetResult();
   if(agent.Calls!=3||!d.reason.Contains("固定程序兜底"))throw new Exception("Fallback threshold failed: "+phase);
   LocalModelAgent.ValidateDecision(g,c,d,phase,f);AgentTurns.Apply(g,c,d,phase,f);
  }
  var canceled=new CancellationTokenSource();canceled.Cancel();var stopped=new InvalidReplyAgent();try{stopped.Decide(g,c,"赛前",f,canceled.Token).GetAwaiter().GetResult();throw new Exception("Cancellation ignored");}catch(OperationCanceledException){}if(stopped.Calls!=0)throw new Exception("Cancellation caused fallback");
  foreach(var p in g.Players){p.Red=p.Green=p.Blue=p.Shot=20;p.Black=3;}
  g.Club(f.Away).League=c.League==1?2:1;c.Upgrade=0;g.Club(f.Away).Upgrade=0;f.HomeGoals=4;f.AwayGoals=0;
  SeasonFlow.Reward(g,f,g.Live);
  if(g.Live.Events.Count(e=>e.StartsWith("大胜奖励"))!=2||g.Live.Events.Count(e=>e.StartsWith("惨败惩罚"))!=2||!g.Live.Events.Any(e=>e.Contains("无升级点战败惩罚")&&e.Contains("→")))throw new Exception("Post-match report incomplete");
  return true;
 }
}
}
