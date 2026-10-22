using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using Touchline;

// Test-only runner. One snapshot per category; never runs a fixture or writes a user save.
public static class ConcurrencyBenchmark {
 static string output,project,endpoint;static Process server;static readonly object logGate=new object();
 class Job {public string ClubId,FixtureId;public int Seed;}
 class Scenario {public string Name,Phase,Snapshot;public Job[] Jobs;}
 class Result {
  public string Club,Error="",DecisionJson="";public double Seconds,QueueSeconds;public int Requests,PromptTokens,OutputTokens,Training,Releases,Offers,Accepts;public bool Fallback,Applied;public string ApplyError="";
 }
 class Batch {
  public int Parallel;public string Category;public double Seconds,MeanDecisionSeconds,CpuCoreEquivalent,PeakWorkingMiB,PeakPrivateMiB,ApplySeconds;public int Requests,Retries,Fallbacks,Failures,PromptTokens,OutputTokens;public Result[] Results;
 }
 class Agent:LocalModelAgent {
  readonly int tokens,seed;readonly bool forceTraining;readonly object quoteItems;public int Calls,Prompt,Completion;
  public Agent(int maximum,int value,bool trainingRequired=false,object requiredQuoteItems=null):base(new AgentConfig{Enabled=true,Model="touchline-benchmark",Endpoint=endpoint,TimeoutSeconds=180}){tokens=maximum;seed=value;forceTraining=trainingRequired;quoteItems=requiredQuoteItems;}
  public Task<string> CallWarmup(CancellationToken ct){return Request(new List<object>{new{role="user",content="Return JSON with ok=true."}},AgentRequest.D("type","object","properties",AgentRequest.D("ok",AgentRequest.D("type","boolean")),"required",new[]{"ok"},"additionalProperties",false),ct);}
  protected override async Task<string> Request(List<object> messages,object schema,CancellationToken ct){
   Calls++;if(quoteItems!=null){var properties=(Dictionary<string,object>)((Dictionary<string,object>)schema)["properties"];var offers=(Dictionary<string,object>)properties["offers"];offers["minItems"]=1;offers["maxItems"]=1;offers["items"]=quoteItems;if(Calls==1)messages.Add(new{role="user",content="本次是主动报价动作覆盖测试，必须且仅提出一项合法采购报价，从可负担市场选择球员。"});}if(forceTraining){var properties=(Dictionary<string,object>)((Dictionary<string,object>)schema)["properties"];var training=(Dictionary<string,object>)properties["training"];training["minItems"]=1;training["maxItems"]=1;if(Calls==1)messages.Add(new{role="user",content="本次是训练动作覆盖测试，请生成且仅生成一项训练指令，选择本队球员、能力和合法点数。"});}using(var handler=new HttpClientHandler{UseProxy=false})using(var client=new HttpClient(handler)){client.Timeout=TimeSpan.FromSeconds(180);
    var body=new{model="touchline-benchmark",messages=messages,temperature=.35,seed=seed,max_tokens=tokens,cache_prompt=false,response_format=new{type="json_object",schema=schema}};
    using(var response=await client.PostAsync(endpoint,new StringContent(Storage.Encode(body),Encoding.UTF8,"application/json"),ct)){
     string raw=await response.Content.ReadAsStringAsync();if(!response.IsSuccessStatusCode)throw new Exception("HTTP "+(int)response.StatusCode+": "+raw.Substring(0,Math.Min(400,raw.Length)));
     var root=Storage.Decode<Dictionary<string,object>>(raw);if(root.ContainsKey("usage")){var usage=(Dictionary<string,object>)root["usage"];Prompt+=Convert.ToInt32(usage["prompt_tokens"]);Completion+=Convert.ToInt32(usage["completion_tokens"]);}
     var choice=(Dictionary<string,object>)((IEnumerable)root["choices"]).Cast<object>().First();return Convert.ToString(((Dictionary<string,object>)choice["message"])["content"]);
    }
   }
  }
 }
 static void Log(string value){lock(logGate){File.AppendAllText(Path.Combine(output,"progress.log"),DateTime.Now.ToString("s")+" "+value+Environment.NewLine,Encoding.UTF8);}}
 static Scenario Make(string name,string phase,Game g,Club[] clubs){Rules.Validate(g);return new Scenario{Name=name,Phase=phase,Snapshot=Storage.Encode(g),Jobs=clubs.Select((c,i)=>new Job{ClubId=c.Id,Seed=20261024+i,FixtureId=phase=="赛前"?g.Fixtures.First(f=>f.Week==6&&(f.Home==c.Id||f.Away==c.Id)).Id:null}).ToArray()};}
 static Scenario[] Inputs(){
  var baseGame=GameFactory.New(20261024,false,"球星迭起");baseGame.PlayerClubId=baseGame.Clubs.Last().Id;baseGame.Week=6;
  var fixtures=baseGame.Fixtures.Where(f=>f.Week==6&&f.Competition=="联赛"&&f.Home!=baseGame.PlayerClubId&&f.Away!=baseGame.PlayerClubId).Take(2).ToArray();
  var ids=fixtures.SelectMany(f=>new[]{f.Home,f.Away}).Distinct().ToArray();if(ids.Length!=4)throw new Exception("Need four clubs from two fixtures");
  var planning=Storage.Clone(baseGame);foreach(string id in ids)planning.Club(id).Upgrade=6;
  var renewal=Storage.Clone(baseGame);renewal.Phase="续约";
  foreach(string id in ids){renewal.Club(id).Funds=700;foreach(var p in renewal.Squad(id).OrderBy(p=>p.Value).Take(5)){p.Age=35;p.DeclineCount=2;p.Favor=2;}}
  var prep=Storage.Clone(baseGame);
  var market=Storage.Clone(baseGame);market.Phase="需求转会";
  foreach(var c in market.Clubs)c.Funds=15000;
  foreach(var p in market.Players.Where(p=>!ids.Contains(p.ClubId)).OrderByDescending(p=>p.Value).Take(8))p.Listed=true;
  foreach(var p in market.Players.Where(p=>!ids.Contains(p.ClubId)).OrderBy(p=>p.Value).Take(2).ToArray())Transfers.Release(market,p);
  foreach(string id in ids)foreach(var p in market.Squad(id).OrderByDescending(p=>p.Value).Take(2)){var buyer=market.Clubs.First(c=>!ids.Contains(c.Id));market.Offers.Add(new TransferOffer{Id=Guid.NewGuid().ToString("N"),PlayerId=p.Id,SellerId=id,BuyerId=buyer.Id,Fee=Transfers.Fee(market,p,false),Season=market.Season});}
  return new[]{Make("六周规划及升级点","阶段规划",planning,ids.Select(planning.Club).ToArray()),Make("续约与不续约","续约",renewal,ids.Select(renewal.Club).ToArray()),Make("多场赛前准备","赛前",prep,ids.Select(prep.Club).ToArray()),Make("转会报价及回应意向","需求转会",market,ids.Select(market.Club).ToArray())};
 }
 static object QuoteItems(Game g,Club c){return AgentRequest.D("oneOf",g.Players.Where(p=>p.ClubId!=""&&p.ClubId!=c.Id&&p.Listed&&Transfers.Affordable(g,p,c,Transfers.Fee(g,p,false))).Select(p=>AgentRequest.D("type","object","properties",AgentRequest.D("playerId",AgentRequest.D("type","string","enum",new[]{p.Id}),"fee",AgentRequest.D("type","number","enum",new[]{Transfers.Fee(g,p,false)})),"required",new[]{"playerId","fee"},"additionalProperties",false)).ToArray());}static int FreePort(){var listener=new TcpListener(IPAddress.Loopback,0);listener.Start();int port=((IPEndPoint)listener.LocalEndpoint).Port;listener.Stop();return port;}
 static async Task Start(int parallel,CancellationToken ct){
  int port=FreePort();endpoint="http://127.0.0.1:"+port+"/v1/chat/completions";int threads=Math.Max(2,Math.Min(8,Environment.ProcessorCount/2));
  string runtime=Path.Combine(project,"LocalAI","runtime","llama-server.exe"),model=Path.Combine(project,"LocalAI","qwen2.5-1.5b-instruct-q4_k_m.gguf");
  var info=new ProcessStartInfo(runtime,"-m \""+model+"\" --host 127.0.0.1 --port "+port+" --parallel "+parallel+" -c "+(8192*parallel)+" -t "+threads+" -tb "+threads+" --cont-batching --no-cache-prompt --alias touchline-benchmark"){UseShellExecute=false,CreateNoWindow=true,RedirectStandardError=true,RedirectStandardOutput=true,WorkingDirectory=Path.GetDirectoryName(runtime)};
  var logs=new BoundedDiagnosticLog(Path.Combine(output,"server-"+parallel+".log"),1024*1024);server=Process.Start(info);server.OutputDataReceived+=(s,e)=>{if(e.Data!=null)logs.Write("stdout",e.Data);};server.ErrorDataReceived+=(s,e)=>{if(e.Data!=null)logs.Write("stderr",e.Data);};server.BeginOutputReadLine();server.BeginErrorReadLine();
  Log("SERVER parallel="+parallel+" threads="+threads+" context="+(8192*parallel));
  using(var handler=new HttpClientHandler{UseProxy=false})using(var client=new HttpClient(handler)){client.Timeout=TimeSpan.FromSeconds(2);bool ready=false;for(int i=0;i<240;i++){ct.ThrowIfCancellationRequested();if(server.HasExited)throw new Exception("Benchmark server exited: "+server.ExitCode);try{ready=(await client.GetAsync("http://127.0.0.1:"+port+"/health",ct)).IsSuccessStatusCode;}catch{ct.ThrowIfCancellationRequested();}if(ready)break;await Task.Delay(500,ct);}if(!ready)throw new Exception("Startup timeout");}
  var agent=new Agent(16,1);await agent.CallWarmup(ct);Log("WARMUP complete parallel="+parallel);
 }
 static void Stop(){if(server==null)return;try{if(!server.HasExited){server.Kill();server.WaitForExit(3000);}}finally{server.Dispose();server=null;}}
 static async Task<Batch> Run(Scenario input,int parallel,CancellationToken ct){
  var batch=new Batch{Parallel=parallel,Category=input.Name};using(var gate=new SemaphoreSlim(parallel)){
   var watch=Stopwatch.StartNew();var cpu=server.TotalProcessorTime;long peakWork=0,peakPrivate=0;bool monitoring=true;
   var monitor=Task.Run(async()=>{while(monitoring){try{server.Refresh();peakWork=Math.Max(peakWork,server.WorkingSet64);peakPrivate=Math.Max(peakPrivate,server.PrivateMemorySize64);}catch{}await Task.Delay(100);}});
   var tasks=input.Jobs.Select(job=>Task.Run(async()=>{await gate.WaitAsync(ct);var timer=Stopwatch.StartNew();var row=new Result{QueueSeconds=watch.Elapsed.TotalSeconds};Agent agent=null;
    try{var g=Storage.Decode<Game>(input.Snapshot);var c=g.Club(job.ClubId);row.Club=c.Name;var f=job.FixtureId==null?null:g.Fixtures.First(x=>x.Id==job.FixtureId);var spec=AgentRequest.Build(g,c,input.Phase,f);agent=new Agent(input.Phase=="赛前"?150:spec.ReplyTokens,job.Seed,input.Name=="训练行动覆盖（必选一项）",input.Name=="主动报价覆盖（必选一项）"?QuoteItems(g,c):null);
     var decision=await agent.Decide(g,c,input.Phase,f,ct);row.Fallback=decision.reason.Contains("固定程序兜底");row.DecisionJson=Storage.Encode(decision);row.Training=decision.training.Sum(t=>t.levels);row.Releases=decision.release.Length+decision.retire.Length;row.Offers=decision.offers.Count;row.Accepts=decision.acceptOffers.Length;
    }catch(Exception ex){row.Error=ex.GetBaseException().Message;}
    finally{row.Seconds=timer.Elapsed.TotalSeconds;if(agent!=null){row.Requests=agent.Calls;row.PromptTokens=agent.Prompt;row.OutputTokens=agent.Completion;}gate.Release();Log("DONE p="+parallel+" "+input.Name+" "+row.Club+" "+row.Seconds.ToString("F2")+"s calls="+row.Requests+" fallback="+row.Fallback+" error="+row.Error);}return row;
   },ct)).ToArray();
   try{batch.Results=await Task.WhenAll(tasks);}finally{monitoring=false;monitor.GetAwaiter().GetResult();}
   batch.Seconds=watch.Elapsed.TotalSeconds;batch.CpuCoreEquivalent=(server.TotalProcessorTime-cpu).TotalSeconds/batch.Seconds;batch.PeakWorkingMiB=peakWork/1048576.0;batch.PeakPrivateMiB=peakPrivate/1048576.0;
   batch.MeanDecisionSeconds=batch.Results.Average(r=>r.Seconds);batch.Requests=batch.Results.Sum(r=>r.Requests);batch.Retries=batch.Results.Sum(r=>Math.Max(0,r.Requests-1));batch.Fallbacks=batch.Results.Count(r=>r.Fallback);batch.Failures=batch.Results.Count(r=>r.Error!="");batch.PromptTokens=batch.Results.Sum(r=>r.PromptTokens);batch.OutputTokens=batch.Results.Sum(r=>r.OutputTokens);
   // Deterministic serial commits to a disposable stage; never mutate the inference snapshots.
   var stage=Storage.Decode<Game>(input.Snapshot);double funds=stage.Clubs.Sum(c=>c.Funds);var apply=Stopwatch.StartNew();for(int i=0;i<batch.Results.Length;i++){var row=batch.Results[i];if(row.Error!="")continue;try{var c=stage.Club(input.Jobs[i].ClubId);var f=input.Jobs[i].FixtureId==null?null:stage.Fixtures.First(x=>x.Id==input.Jobs[i].FixtureId);var d=Storage.Decode<AgentDecision>(row.DecisionJson);LocalModelAgent.ValidateDecision(stage,c,d,input.Phase,f);AgentTurns.Apply(stage,c,d,input.Phase,f);Rules.Validate(stage);row.Applied=true;}catch(Exception ex){row.ApplyError=ex.Message;}}
   batch.ApplySeconds=apply.Elapsed.TotalSeconds;if(input.Phase=="需求转会"&&Math.Abs(stage.Clubs.Sum(c=>c.Funds)-funds)>1e-6)throw new Exception("Transfer funds not conserved");
   Log("BATCH p="+parallel+" "+input.Name+" wall="+batch.Seconds.ToString("F2")+"s retries="+batch.Retries+" fallbacks="+batch.Fallbacks);return batch;
  }
 }
 static void Main(string[] args){try{project=Path.GetFullPath(args[0]);output=Path.GetFullPath(args[1]);Directory.CreateDirectory(output);if(File.Exists(Path.Combine(output,"results.json")))throw new Exception("Choose a new empty benchmark directory.");typeof(Storage).GetField("Root").SetValue(null,Path.Combine(output,"isolated-profile"));L10n.Initialize();var inputs=Inputs();if(args.Length>2&&args[2]=="quote-required"){inputs[3].Name="主动报价覆盖（必选一项）";inputs=new[]{inputs[3]};}if(args.Length>2&&args[2].StartsWith("training")){var training=Storage.Decode<Game>(inputs[0].Snapshot);training.Week=18;foreach(var job in inputs[0].Jobs){var club=training.Club(job.ClubId);club.Funds=15000;club.Upgrade=30;club.AgentIdentity="竞技优先，当前落实年轻主力培养计划。本阶段优先用现有升级点提升年轻主力的红绿蓝或射门能力，不必等待新的升级点。";}inputs=new[]{Make(args[2]=="training-required"?"训练行动覆盖（必选一项）":"期末升级点规划","阶段规划",training,inputs[0].Jobs.Select(j=>training.Club(j.ClubId)).ToArray())};}File.WriteAllText(Path.Combine(output,"inputs.json"),Storage.Encode(inputs),Encoding.UTF8);
  var batches=new List<Batch>();using(var timeout=new CancellationTokenSource(TimeSpan.FromMinutes(25))){foreach(int parallel in new[]{1,2,4}){try{Start(parallel,timeout.Token).GetAwaiter().GetResult();foreach(var input in inputs){Log("BEGIN p="+parallel+" "+input.Name);batches.Add(Run(input,parallel,timeout.Token).GetAwaiter().GetResult());File.WriteAllText(Path.Combine(output,"results.json"),Storage.Encode(batches),Encoding.UTF8);}}finally{Stop();}}}
  File.WriteAllText(Path.Combine(output,"complete.json"),Storage.Encode(new{Completed=DateTime.Now.ToString("o"),Passed=batches.Count==inputs.Length*3,Batches=batches.Count,LogicalDecisions=batches.Sum(b=>b.Results.Length),Threads=Math.Max(2,Math.Min(8,Environment.ProcessorCount/2)),ContextPerSlot=8192,Order=new[]{1,2,4},Rounds=1,CachePrompt=false,Scope="Four clubs per category; model seed fixed per job; production decode/validation and three-attempt policy; benchmark-only HTTP transport records tokens and disables prompt cache. Decisions generated independently from identical snapshots, applied serially in disposable stages; no matches played and no user saves touched."}),Encoding.UTF8);Log("COMPLETE");
 }catch(Exception ex){if(output!=null)File.WriteAllText(Path.Combine(output,"failure.txt"),ex.ToString());Environment.ExitCode=1;Console.Error.WriteLine(ex);}finally{Stop();}}
}





