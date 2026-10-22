using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
namespace Touchline {
public static class AiReliabilityTests {
 static void Check(bool ok,string message){if(!ok)throw new Exception("AI reliability: "+message);}
 class ReplyAgent:LocalModelAgent {
  public int Calls;readonly bool broken;
  public ReplyAgent(bool alwaysBroken):base(new AgentConfig{Enabled=true,Model="test",Endpoint="http://127.0.0.1/test"}){broken=alwaysBroken;}
  protected override Task<string> Request(List<object> messages,object schema,CancellationToken ct){
   Calls++;if(broken||Calls==1)return Task.FromResult("{\"plan\":0,\"tactic\":\"无战术\",\"substitutions\":[{\"slot\":0,\"playerId\":\"p1\"},{\"slot\":0,\"playerId\":\"p1\"}],\"reason\":\"虚构行动\"}");
   return Task.FromResult("{\"plan\":0,\"reason\":\"采用摆大巴并换入五名球员，已赢得欧冠。\"}");
  }
 }
 public static void Run(bool real){
  string root=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"test-results","ai-fixes");Directory.CreateDirectory(root);
  var g=GameFactory.New(20261023);var f=g.Fixtures.First();g.PlayerClubId=f.Away;Football.Begin(g,f);var c=g.Club(f.Home);
  for(int left=0;left<=5;left++){g.Live.HomeSubs=5-left;var plans=HalfTimePlans.Build(g,c,f);Check(plans[0].Count==0,"no-change candidate");
   foreach(var changes in plans){Check(changes.Count<=left,"substitution cap");LocalModelAgent.ValidateDecision(g,c,new AgentDecision{tactic="无战术",substitutions=changes},"中场",f);}
   if(left>0)Check(plans.Any(p=>p.Count==left),"multi-player candidate");else Check(plans.Count==1,"exhausted quota");
  }
  g.Live.HomeSubs=0;var unused=g.Squad(c.Id).Where(p=>!g.Live.UsedHome.Contains(p.Id)).ToArray();foreach(var p in unused)p.Injury=1;
  Check(HalfTimePlans.Build(g,c,f).Count==1,"no healthy bench");foreach(var p in unused)p.Injury=0;
  var agent=new ReplyAgent(false);var d=agent.Decide(g,c,"中场",f,CancellationToken.None).GetAwaiter().GetResult();
  Check(agent.Calls==2&&d.substitutions.Count==0,"invalid custom list rejected, corrected plan accepted");
  Check(!d.reason.Contains("摆大巴")&&!d.reason.Contains("欧冠")&&d.reason.Contains("不换人"),"invented explanation removed");
  var messages=new List<string>();DecisionOutput.Apply(g,c,d,"中场",f,messages.Add);
  Check(messages.All(s=>!s.Contains("摆大巴")&&!s.Contains("欧冠"))&&c.Memory.All(s=>!s.Contains("已赢得欧冠")),"public output and memory grounded");
  var bad=new ReplyAgent(true);int subs=g.Live.HomeSubs;bool paused=false;try{bad.Decide(g,c,"中场",f,CancellationToken.None).GetAwaiter().GetResult();}catch{paused=true;}
  Check(paused&&bad.Calls==3&&g.Live.HomeSubs==subs,"strict opponent never silently replaced");
  string logPath=Path.Combine(root,"bounded.log");var log=new BoundedDiagnosticLog(logPath,1024);
  for(int i=0;i<60;i++)Check(log.Write("test",new string('测',40)+" token=secret-123 Authorization: Bearer secret-456"),"log write");
  Check(File.Exists(logPath+".1")&&new FileInfo(logPath).Length<=1024&&new FileInfo(logPath+".1").Length<=1024,"bounded rotation");
  Check(!File.ReadAllText(logPath).Contains("secret-123")&&!File.ReadAllText(logPath).Contains("secret-456"),"secret redaction");
  Check(!new BoundedDiagnosticLog(root,1024).Write("test","cannot write a directory"),"log errors nonfatal");
  Check(TransactionRegressionTests.Run(),"transaction checks");Check(ExperienceTests.Run(),"planning and fatigue checks");Check(FallbackTests.Run(),"fallback checks");Check(ProgressTests.Run(),"progress output checks");
  string replay="not requested";
  if(real){
   string checkpoint=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"test-results","end-to-end-20261020","checkpoint.tmsave");string before=File.ReadAllText(checkpoint);
   var copy=Storage.Load(checkpoint);Check(copy.Live!=null,"incident has live match");var fixture=copy.Fixtures.First(x=>x.Id==copy.Live.FixtureId);var opponent=copy.Club(fixture.Home==copy.PlayerClubId?fixture.Away:fixture.Home);
   var local=new LocalModelAgent(new AgentConfig{Enabled=true,Endpoint="http://127.0.0.1:17845/v1/chat/completions",Model="touchline-local",TimeoutSeconds=180});
   var decision=local.Decide(copy,opponent,"中场",fixture,CancellationToken.None).GetAwaiter().GetResult();Check(!decision.reason.Contains("固定程序兜底"),"incident uses actual model");
   DecisionOutput.Apply(copy,opponent,decision,"中场",fixture,s=>{});Check(File.ReadAllText(checkpoint)==before,"original checkpoint unchanged");
   replay=Storage.Encode(new{Club=opponent.Name,Decision=decision});File.WriteAllText(Path.Combine(root,"incident-replay.json"),replay);
   Check(File.Exists(Path.Combine(AiDiagnostics.DirectoryPath,"local-ai.log")),"runtime diagnostic file");
  }
  File.WriteAllText(Path.Combine(root,real?"real-result.json":"result.json"),Storage.Encode(new{Passed=true,RealIncidentReplay=replay,Completed=DateTime.Now.ToString("o"),Scope="Targeted checks only; no full season run."}));
 }
}
}
