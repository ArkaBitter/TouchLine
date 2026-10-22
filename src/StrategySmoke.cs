using System;
using System.IO;
using System.Linq;
using System.Threading;
namespace Touchline {
public static class StrategySmoke {
 public static void Run(){var g=GameFactory.New(20261020);g.PlayerClubId=g.Clubs[0].Id;var c=g.Clubs[1];var agent=new LocalModelAgent(new AgentConfig{Enabled=true,Model="touchline-local",Endpoint="http://127.0.0.1:17845/v1/chat/completions",TimeoutSeconds=180});var start=agent.Decide(g,c,"阶段规划",null,CancellationToken.None).GetAwaiter().GetResult();if(start.reason.Contains("固定程序兜底"))throw new Exception("Initial strategic model plan fell back");AgentTurns.Apply(g,c,start,"阶段规划",null);g.Week=6;c.Upgrade=7;var sixth=agent.Decide(g,c,"阶段规划",null,CancellationToken.None).GetAwaiter().GetResult();if(sixth.reason.Contains("固定程序兜底"))throw new Exception("Six-week strategic model plan fell back");AgentTurns.Apply(g,c,sixth,"阶段规划",null);if(c.Upgrade!=7-sixth.training.Sum(t=>t.levels))throw new Exception("Training spend mismatch");string root=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"test-results");Directory.CreateDirectory(root);File.WriteAllText(Path.Combine(root,"strategy-real-model.json"),Storage.Encode(new{Passed=true,Initial=start,SixthWeek=sixth,Remaining=c.Upgrade,Completed=DateTime.Now.ToString("o")}));}
}
}
