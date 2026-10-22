using System;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Collections.Generic;
namespace Touchline {
public static class InferenceRuntimeTests {
 static int FreePort(){var listener=new TcpListener(IPAddress.Loopback,0);listener.Start();int port=((IPEndPoint)listener.LocalEndpoint).Port;listener.Stop();return port;}
 public static void Run(){
  string root=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"test-results","inference-20261021");Directory.CreateDirectory(root);string originalRoot=Storage.Root;var launch=LocalRuntime.Launch;int originalPort=LocalRuntime.Port;bool originalGpu=LocalRuntime.PreferGpu,originalWarmup=LocalRuntime.AllowBackgroundWarmup;var launches=new List<bool>();
  typeof(Storage).GetField("Root").SetValue(null,Path.Combine(root,"runtime-profile"));
  try{
   if(!LocalRuntime.CudaAvailable())throw new Exception("CUDA unavailable for authorized GPU test");LocalRuntime.PreferGpu=true;LocalRuntime.AllowBackgroundWarmup=true;LocalRuntime.Port=FreePort();LocalRuntime.Launch=(info,gpu)=>{launches.Add(gpu);return Process.Start(info);};
   var watch=Stopwatch.StartNew();LocalRuntime.Start(AppDomain.CurrentDomain.BaseDirectory,CancellationToken.None).GetAwaiter().GetResult();double cold=watch.Elapsed.TotalSeconds;if(launches.Count!=1||!launches[0])throw new Exception("GPU backend not selected");
   var g=GameFactory.New(20261029);var f=g.Fixtures.First();g.PlayerClubId=f.Home;var c=g.Club(f.Away);var agent=new LocalModelAgent(new AgentConfig{Enabled=true,Endpoint="http://127.0.0.1:"+LocalRuntime.Port+"/v1/chat/completions",Model="touchline-local",TimeoutSeconds=180});watch.Restart();var d=agent.Decide(g,c,"赛前",f,CancellationToken.None).GetAwaiter().GetResult();double decision=watch.Elapsed.TotalSeconds;if(d.reason.Contains("固定程序兜底"))throw new Exception("Player opponent unexpectedly fell back");DecisionOutput.Apply(g,c,d,"赛前",f,s=>{});LocalRuntime.Stop();
   launches.Clear();LocalRuntime.Port=FreePort();LocalRuntime.Launch=(info,gpu)=>{launches.Add(gpu);if(gpu)throw new Exception("Injected GPU initialization failure");return Process.Start(info);};LocalRuntime.Start(AppDomain.CurrentDomain.BaseDirectory,CancellationToken.None).GetAwaiter().GetResult();if(!launches.SequenceEqual(new[]{true,false}))throw new Exception("GPU failure did not retry CPU inference");LocalRuntime.Stop();
   launches.Clear();LocalRuntime.Port=FreePort();LocalRuntime.Launch=(info,gpu)=>{launches.Add(gpu);return Process.Start(info);};watch.Restart();LocalRuntime.Warmup(AppDomain.CurrentDomain.BaseDirectory);double dispatch=watch.Elapsed.TotalMilliseconds;Thread.Sleep(5000);watch.Restart();LocalRuntime.Start(AppDomain.CurrentDomain.BaseDirectory,CancellationToken.None).GetAwaiter().GetResult();double foreground=watch.Elapsed.TotalSeconds;if(launches.Count!=1||dispatch>200)throw new Exception("Warmup blocked or duplicated launch");LocalRuntime.Stop();
   LocalRuntime.Port=FreePort();launches.Clear();LocalRuntime.Warmup(AppDomain.CurrentDomain.BaseDirectory);LocalRuntime.Stop();int stoppedCount=launches.Count;Thread.Sleep(300);if(launches.Count!=stoppedCount)throw new Exception("Warmup launched a process after Stop");
   File.WriteAllText(Path.Combine(root,"runtime-tests.json"),Storage.Encode(new{Passed=true,GpuLaunch=true,PlayerOpponentModelDecision=true,GpuFailureUsesCpuModel=true,WarmupDispatchMs=dispatch,ColdStartSeconds=cold,DecisionSeconds=decision,SimulatedPlayerThinkSeconds=5,ForegroundAfterWarmupSeconds=foreground,NoCachedDecisions=true,StopPendingWarmup=true,NoUserSaves=true}));
  }finally{LocalRuntime.Stop();LocalRuntime.Launch=launch;LocalRuntime.Port=originalPort;LocalRuntime.PreferGpu=originalGpu;LocalRuntime.AllowBackgroundWarmup=originalWarmup;typeof(Storage).GetField("Root").SetValue(null,originalRoot);}
 }
}
}
