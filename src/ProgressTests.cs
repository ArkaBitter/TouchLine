using System;
using System.Linq;
using System.Threading;
using System.Collections.Generic;
namespace Touchline {
static class ProgressTests {
 public static bool Run(){
  var g=GameFactory.New(1981);var f=g.Fixtures.First();g.PlayerClubId=g.Clubs.First(c=>c.Id!=f.Home&&c.Id!=f.Away).Id;
  var messages=new List<string>();Action<string> output=s=>messages.Add(s);var testAgent=new InvalidReplyAgent();
  AgentTurns.Prepare(g,f,testAgent,CancellationToken.None,output).GetAwaiter().GetResult();Football.Begin(g,f);
  Controller.PlaySegment(g,testAgent,CancellationToken.None,output,45).GetAwaiter().GetResult();
  if(!messages.Any(s=>s.Contains("首发：")&&s.Contains(g.Club(f.Home).Name))||!messages.Any(s=>s.Contains("选用战术"))||!messages.Any(s=>s.Contains("中场")&&(s.Contains("不换人")||s.Contains("换人：")))||!messages.Any(s=>s.Contains("自动教练")))throw new Exception("Decision output missing");
  if(!messages.Any(s=>s.Contains("比赛结束")&&s.Contains(g.Club(f.Home).Name+" "+f.HomeGoals+" : "+f.AwayGoals+" "+g.Club(f.Away).Name)))throw new Exception("Final score output missing");
  if(!g.News.Any(s=>s.Contains("首发：")))throw new Exception("Applied decisions not persisted");
  return true;
 }
}
}
