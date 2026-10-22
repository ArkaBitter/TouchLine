using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Diagnostics;

namespace Touchline {
public interface IClubAgent {Task<AgentDecision> Decide(Game g,Club c,string phase,Fixture f,CancellationToken ct);}
public class LocalModelAgent : IClubAgent {
 int responseTokens=300;AgentConfig config;public LocalModelAgent(AgentConfig c){config=c;}
 public static void CheckEndpoint(string endpoint){Uri u;if(!Uri.TryCreate(endpoint,UriKind.Absolute,out u)||!(u.Scheme=="http"||u.Scheme=="https")||!(u.Host=="localhost"||u.Host=="127.0.0.1"||u.Host=="[::1]"||u.Host=="::1"))throw new Exception("离线版只允许 localhost／127.0.0.1 本地模型地址。");}
 public async Task<AgentDecision> Decide(Game g,Club c,string phase,Fixture fixture,CancellationToken ct){
  if(!config.Enabled||String.IsNullOrWhiteSpace(config.Model))throw new Exception("请先在本地AI设置中启动模型。关键比赛及经营阶段需要本地模型。");CheckEndpoint(config.Endpoint);
  bool bundledRuntime=LocalRuntime.IsBundled(config.Endpoint,config.Model);if(bundledRuntime)await LocalRuntime.EnsureReady(AppDomain.CurrentDomain.BaseDirectory,ct);var spec=AgentRequest.Build(g,c,phase,fixture);InferenceTuning.Prepare(spec,phase,bundledRuntime&&LocalRuntime.UsingGpu);responseTokens=CoachingPolicy.PlayerMatch(g,fixture)?500:phase=="赛前"?150:spec.ReplyTokens;var ids=spec.Codes;
  var messages=new List<object>{new{role="system",content=spec.System},new{role="user",content=spec.Prompt}};string last="";
  for(int attempt=0;attempt<3;attempt++){ct.ThrowIfCancellationRequested();
   string raw=await Request(messages,spec.Schema,ct);try{
    int start=raw.IndexOf('{'),end=raw.LastIndexOf('}');if(start<0||end<start)throw new Exception("输出不是JSON对象。");var d=Storage.Decode<AgentDecision>(raw.Substring(start,end-start+1));
    if(phase=="赛前"&&d.plan>=0&&d.plan<spec.Plans.Count){d.lineup=spec.Plans[d.plan].Lineup;d.tactic=spec.Plans[d.plan].Tactic;}
    if(LiveCoaching.Phase(phase)&&spec.LivePlans.Count>0){if(d.plan<0||d.plan>=spec.LivePlans.Count||d.substitutions==null||d.swaps==null||d.substitutions.Count>0||d.swaps.Count>0||Storage.Decode<Dictionary<string,object>>(raw.Substring(start,end-start+1)).Keys.Any(k=>k!="plan"&&k!="reason"))throw new Exception("临场必须选择联合调整方案编号。");var option=spec.LivePlans[d.plan];d.tactic=option.Tactic;d.substitutions=option.Subs.Select(s=>new SubDecision{slot=s.slot,playerId=s.playerId}).ToList();d.swaps=option.Swap==null?new List<PositionSwap>():new List<PositionSwap>{new PositionSwap{a=option.Swap.a,b=option.Swap.b}};}
    else if(LiveCoaching.Phase(phase)){if(d.plan<0||d.plan>=spec.SubPlans.Count||(d.substitutions!=null&&d.substitutions.Count>0))throw new Exception("中场必须选择合法换人方案编号，不可自行填写换人列表。");d.substitutions=spec.SubPlans[d.plan].Select(s=>new SubDecision{slot=s.slot,playerId=s.playerId}).ToList();if(d.swapPlan<0||d.swapPlan>=spec.SwapPlans.Count||(d.swaps!=null&&d.swaps.Count>0))throw new Exception("换位必须选择合法方案编号。");var swap=spec.SwapPlans[d.swapPlan];d.swaps=swap==null?new List<PositionSwap>():new List<PositionSwap>{new PositionSwap{a=swap.a,b=swap.b}};}
    if(phase=="阶段规划"){if(d.trainingPlan<0||d.trainingPlan>=spec.TrainingPlans.Count||(d.training!=null&&d.training.Count>0))throw new Exception("训练必须选择合法方案编号，不可混用自填训练列表。");d.training=spec.TrainingPlans[d.trainingPlan].Select(t=>new TrainingDecision{playerId=t.playerId,attribute=t.attribute,levels=t.levels}).ToList();}
    if((phase=="自主转会"||phase=="需求转会")&&d.responses==null)throw new Exception("转会决定须逐笔填写responses，没有报价时返回空对象。");
    if(d.responses!=null){
     if((d.acceptOffers!=null&&d.acceptOffers.Length>0)||(d.rejectOffers!=null&&d.rejectOffers.Length>0)||d.responses.Count!=spec.OfferCodes.Count||d.responses.Any(x=>!spec.OfferCodes.ContainsKey(x.Key)||!new[]{"accept","reject","defer"}.Contains(x.Value)))throw new Exception("每个报价须且只能选择一次接受、拒绝或暂缓，不可混用旧回应字段。");
     d.acceptOffers=d.responses.Where(x=>x.Value=="accept").Select(x=>x.Key).ToArray();d.rejectOffers=d.responses.Where(x=>x.Value=="reject").Select(x=>x.Key).ToArray();
    }
    Func<string,string> map=x=>ids.ContainsKey(x)?ids[x]:x;
    d.lineup=(d.lineup??new string[0]).Select(map).ToArray();d.importantMatches=d.importantMatches??new string[0];d.release=(d.release??new string[0]).Select(map).ToArray();d.retire=(d.retire??new string[0]).Select(map).ToArray();d.listPlayers=(d.listPlayers??new string[0]).Select(map).ToArray();
    if(d.offers==null)d.offers=new List<AgentOffer>();if(d.acceptOffers==null)d.acceptOffers=new string[0];if(d.rejectOffers==null)d.rejectOffers=new string[0];if(d.training==null)d.training=new List<TrainingDecision>();if(d.substitutions==null)d.substitutions=new List<SubDecision>();if(d.swaps==null)d.swaps=new List<PositionSwap>();
    d.acceptOffers=d.acceptOffers.Select(x=>spec.OfferCodes.ContainsKey(x)?spec.OfferCodes[x]:x).ToArray();d.rejectOffers=d.rejectOffers.Select(x=>spec.OfferCodes.ContainsKey(x)?spec.OfferCodes[x]:x).ToArray();
    foreach(var tr in d.training)tr.playerId=map(tr.playerId);foreach(var sub in d.substitutions)sub.playerId=map(sub.playerId);
    ValidateDecision(g,c,d,phase,fixture);AiDiagnostics.Write("decision",c.Name+" / "+phase+" / raw explanation: "+d.reason);d.reason=DecisionNarration.Summary(g,d,phase,fixture);return d;
   }catch(Exception ex){last=ex.Message;AiDiagnostics.Write("invalid",c.Name+" / "+phase+" / attempt "+(attempt+1)+" / "+last+" / output: "+raw);messages.Add(new{role="assistant",content=raw});messages.Add(new{role="user",content="决定不合法，请纠正后重新输出JSON。原因："+last});}
  }ct.ThrowIfCancellationRequested();if(CoachingPolicy.PlayerMatch(g,fixture)&&(phase=="赛前"||LiveCoaching.Phase(phase)))throw new Exception("本地智能体连续3次未生成合法决定，比赛暂停，请重试。原因："+last);return FixedAgentFallback.Decide(g,c,phase,fixture,last);
 }
 protected virtual async Task<string> Request(List<object> messages,object schema,CancellationToken ct){
  bool bundled=LocalRuntime.IsBundled(config.Endpoint,config.Model);if(bundled)await LocalRuntime.EnsureReady(AppDomain.CurrentDomain.BaseDirectory,ct);
  for(int transportAttempt=0;transportAttempt<2;transportAttempt++){if(bundled&&transportAttempt>0)await LocalRuntime.EnsureReady(AppDomain.CurrentDomain.BaseDirectory,ct);try{using(var handler=new HttpClientHandler{UseProxy=false})using(var client=new HttpClient(handler)){client.Timeout=TimeSpan.FromSeconds(config.TimeoutSeconds);var body=AgentRequest.D("model",config.Model,"messages",messages,"temperature",.35,"max_tokens",responseTokens,"response_format",new{type="json_object",schema=schema});if(bundled)body["cache_prompt"]=true;using(var response=await client.PostAsync(config.Endpoint,new StringContent(Storage.Encode(body),Encoding.UTF8,"application/json"),ct)){string text=await response.Content.ReadAsStringAsync();if(!response.IsSuccessStatusCode)AiDiagnostics.Write("http", "status="+(int)response.StatusCode+" body="+text);if(!response.IsSuccessStatusCode)throw new Exception("本地模型HTTP "+(int)response.StatusCode+"："+text.Substring(0,Math.Min(500,text.Length)));var root=Storage.Decode<Dictionary<string,object>>(text);var choices=((System.Collections.IEnumerable)root["choices"]).Cast<object>().ToArray();var message=(Dictionary<string,object>)((Dictionary<string,object>)choices[0])["message"];return Convert.ToString(message["content"]);}}}
  catch(HttpRequestException ex){AiDiagnostics.Write("connection",ex.GetBaseException().Message);if(bundled&&transportAttempt==0)continue;throw new Exception("无法连接本地模型服务 "+config.Endpoint+"。请检查服务是否运行及端口配置。详细原因："+ex.GetBaseException().Message,ex);}
  catch(TaskCanceledException ex){AiDiagnostics.Write(ct.IsCancellationRequested?"cancel":"timeout","timeoutSeconds="+config.TimeoutSeconds);ct.ThrowIfCancellationRequested();throw new Exception("本地模型响应超时（"+config.TimeoutSeconds+"秒）。请稍后重试或在设置中延长超时。",ex);}catch(Exception ex){AiDiagnostics.Write("request-error",ex.Message);throw;}}
  AiDiagnostics.Write("request-failed","Retry budget exhausted.");throw new Exception("本地模型请求失败。");
 }

 public static void ValidateDecision(Game g,Club c,AgentDecision d,string phase,Fixture f){
  if(d.acceptOffers.Intersect(d.rejectOffers).Any()||d.acceptOffers.Distinct().Count()!=d.acceptOffers.Length||d.rejectOffers.Distinct().Count()!=d.rejectOffers.Length)throw new Exception("报价回应不能重复或同时接受和拒绝。");
  if(d.offers.Select(o=>o.playerId).Distinct().Count()!=d.offers.Count)throw new Exception("同次决定不能重复向一名球员报价。");
  if(d.training.Count>0&&phase!="阶段规划")throw new Exception("智能体升级点仅在六周计划中分配。");
  if(phase=="阶段规划"){if(d.importantMatches.Length>3||d.importantMatches.Distinct().Count()!=d.importantMatches.Length||d.importantMatches.Any(id=>!StrategicPlanning.Candidates(g,c).Any(fx=>fx.Id==id)))throw new Exception("重点赛程选择无效。");if(g.Week%6!=0&&d.training.Count>0)throw new Exception("尚未到六周训练时间。");}
  if(d.offers.Count>Transfers.OffersRemaining(g,c))throw new Exception("报价数量超过本窗口剩余次数。");
  if(phase=="赛前")Football.ValidateLineup(g,c,d.lineup,d.tactic);
  if(d.tactic!="无战术"&&!c.Tactics.Contains(d.tactic))throw new Exception("选择了未拥有的战术。");
  if(d.release.Any(id=>!g.Squad(c.Id).Any(p=>p.Id==id)))throw new Exception("不续约名单有外队球员。");
  if(d.retire.Length>0&&(g.CareerMode!="球星迭起"||phase!="续约"||d.retire.Any(id=>!g.Squad(c.Id).Any(p=>p.Id==id))))throw new Exception("退役决定无效。");
  if(phase=="续约"&&(d.release.Intersect(d.retire).Any()||d.release.Concat(d.retire).Distinct().Count()!=d.release.Length+d.retire.Length||g.Squad(c.Id).Count()-d.release.Length-d.retire.Length<10))throw new Exception("续约操作须保留至少10人且不能重复。");
  int training=0;foreach(var t in d.training){if(!g.Squad(c.Id).Any(p=>p.Id==t.playerId)||t.levels<=0||!new[]{"红","绿","蓝","射门","黑"}.Contains(t.attribute))throw new Exception("训练参数错误。");training+=t.levels;}if(training>c.Upgrade)throw new Exception("训练超过剩余升级点。");
  if(LiveCoaching.Phase(phase)&&g.Live!=null){bool home=f.Home==c.Id;int count=home?g.Live.HomeSubs:g.Live.AwaySubs;var used=home?g.Live.UsedHome:g.Live.UsedAway;if(d.substitutions.Count+count>5)throw new Exception("换人次数超过5次。");if(d.substitutions.Select(s=>s.playerId).Distinct().Count()!=d.substitutions.Count||d.substitutions.Select(s=>s.slot).Distinct().Count()!=d.substitutions.Count)throw new Exception("换人重复。");foreach(var s in d.substitutions)if(s.slot<0||s.slot>=10||!g.Squad(c.Id).Any(p=>p.Id==s.playerId&&p.Injury==0)||used.Contains(s.playerId))throw new Exception("换入球员／位置无效。");}
  if(d.swaps.Count>0&&!LiveCoaching.Phase(phase))throw new Exception("换位仅用于比赛中。");
  if(LiveCoaching.Phase(phase)&&g.Live!=null){bool home=f.Home==c.Id;var projected=(string[])(home?g.Live.HomeLineup:g.Live.AwayLineup).Clone();foreach(var sub in d.substitutions)projected[sub.slot]=sub.playerId;if(d.swaps.Count>1)throw new Exception("每次临场方案最多一组换位。");foreach(var swap in d.swaps)projected=LiveCoaching.SwapPreview(g,home,swap.a,swap.b,projected,d.tactic);Football.ValidateLineup(g,c,projected,d.tactic);}
  foreach(string id in d.acceptOffers.Concat(d.rejectOffers))if(!g.Offers.Any(o=>o.Id==id&&o.Status=="待回应"&&((!o.PlayerDriven&&o.SellerId==c.Id)||(o.PlayerDriven&&o.BuyerId==c.Id))))throw new Exception("报价不属于本队。");
 }
}
public static class AgentTurns {
 public static void Apply(Game g,Club c,AgentDecision d,string phase,Fixture f){if(LiveCoaching.Phase(phase))LocalModelAgent.ValidateDecision(g,c,d,phase,f);d.reason=DecisionNarration.Summary(g,d,phase,f);if(!String.IsNullOrEmpty(d.goal))c.AgentGoal=d.goal;c.Memory.Add(phase+"："+d.reason);if(c.Memory.Count>40)c.Memory.RemoveAt(0);g.Log(c.Name+(d.reason.StartsWith("[自动教练]")?"自动教练：":"智能体：")+d.reason);
  if(phase=="阶段规划"){c.ImportantFixtures=d.importantMatches;foreach(var tr in d.training)SeasonFlow.Train(g,c,g.Players.First(p=>p.Id==tr.playerId),Array.IndexOf(new[]{"红","绿","蓝","射门","黑"},tr.attribute),tr.levels);}
  if(phase=="赛前"){c.Lineup=d.lineup;c.Tactic=d.tactic;foreach(var tr in d.training)SeasonFlow.Train(g,c,g.Players.First(p=>p.Id==tr.playerId),Array.IndexOf(new[]{"红","绿","蓝","射门","黑"},tr.attribute),tr.levels);}
  if(LiveCoaching.Phase(phase)&&g.Live!=null){bool home=f.Home==c.Id;if(home)g.Live.HomeTactic=f.Neutral&&d.tactic=="战术犯规"?"无战术":d.tactic;else g.Live.AwayTactic=f.Neutral&&d.tactic=="战术犯规"?"无战术":d.tactic;foreach(var s in d.substitutions)Football.Substitute(g,home,s.slot,s.playerId);foreach(var swap in d.swaps)LiveCoaching.Swap(g,home,swap.a,swap.b);}
  if(phase=="续约"){foreach(string id in d.retire)CareerCycle.Retire(g,g.Players.First(p=>p.Id==id));foreach(string id in d.release)Transfers.Release(g,g.Players.First(p=>p.Id==id));}
  if(phase=="需求转会"){foreach(string id in d.listPlayers){Player p=g.Players.FirstOrDefault(x=>x.Id==id&&x.ClubId==c.Id);if(p!=null&&(p.Type=="门将"||g.Squad(c.Id).Count(x=>x.Type==p.Type)>5))p.Listed=true;}foreach(var o in d.offers){var p=g.Players.FirstOrDefault(x=>x.Id==o.playerId&&(x.Listed||x.ClubId==""));if(p==null){g.Log(c.Name+"提案无效：目标不在货架。");continue;}try{var offer=Transfers.Offer(g,p,c,o.fee);if(p.ClubId=="")Transfers.Complete(g,offer,true);}catch(Exception ex){g.Log(c.Name+"报价被规则拒绝："+ex.Message);}}}
  foreach(string id in d.acceptOffers){var offer=g.Offers.First(o=>o.Id==id);Transfers.Complete(g,offer,true);}foreach(string id in d.rejectOffers)Transfers.Complete(g,g.Offers.First(o=>o.Id==id),false);
 }
 public static async Task Prepare(Game g,Fixture f,IClubAgent agent,CancellationToken ct,Action<string> progress){foreach(string id in new[]{f.Home,f.Away}){Club c=g.Club(id);if(id==g.PlayerClubId){Football.ValidateLineup(g,c,c.Lineup,c.Tactic);continue;}var d=await CoachingPolicy.Decide(g,c,"赛前",f,agent,ct,progress);DecisionOutput.Apply(g,c,d,"赛前",f,progress);}}
 public static async Task HalfTime(Game g,Fixture f,IClubAgent agent,CancellationToken ct,Action<string> progress){foreach(string id in new[]{f.Home,f.Away}.Where(id=>id!=g.PlayerClubId)){Club c=g.Club(id);var d=await CoachingPolicy.Decide(g,c,"中场",f,agent,ct,progress);DecisionOutput.Apply(g,c,d,"中场",f,progress);}}
 public static async Task Window(Game g,IClubAgent agent,CancellationToken ct,Action<string> progress){string phase=g.Phase;if(!new[]{"续约","自主转会","需求转会"}.Contains(phase))throw new Exception("当前不是AI经营阶段。");if(phase=="自主转会"&&!g.Offers.Any(o=>o.PlayerDriven&&o.Season==g.Season))Transfers.PlayerDriven(g);
  foreach(var c in g.Clubs.Where(c=>c.Id!=g.PlayerClubId)){progress(c.Name+" · "+phase);var d=await agent.Decide(g,c,phase,null,ct);DecisionOutput.Apply(g,c,d,phase,null,progress);}
  if(phase=="续约"){foreach(var p in g.Squad(g.PlayerClubId).Where(p=>!p.Renew).ToArray())Transfers.Release(g,p);g.Phase="自主转会";}
  else if(phase=="自主转会"){if(g.Offers.Any(o=>o.PlayerDriven&&o.BuyerId==g.PlayerClubId&&o.Status=="待回应"))g.Log("请回应本队的球员自主加盟提案。");else g.Phase="需求转会";}
 }
}
public static class LocalRuntime {
 internal static int Port=17845;internal static Func<ProcessStartInfo,bool,Process> Launch=(info,gpu)=>Process.Start(info);
 static readonly object launchSync=new object();
 static Process process;static readonly SemaphoreSlim launchGate=new SemaphoreSlim(1,1);
 public static bool IsBundled(string endpoint,string model){Uri u;return Uri.TryCreate(endpoint,UriKind.Absolute,out u)&&u.Port==Port&&(u.Host=="127.0.0.1"||u.Host=="localhost")&&model=="touchline-local";}
 static async Task<bool> Healthy(CancellationToken ct){ct.ThrowIfCancellationRequested();using(var socket=new System.Net.Sockets.TcpClient()){try{var connect=socket.ConnectAsync("127.0.0.1",Port);if(await Task.WhenAny(connect,Task.Delay(200,ct))!=connect){ct.ThrowIfCancellationRequested();return false;}await connect;}catch{ct.ThrowIfCancellationRequested();return false;}}using(var h=new HttpClientHandler{UseProxy=false})using(var client=new HttpClient(h)){client.Timeout=TimeSpan.FromSeconds(2);try{return (await client.GetAsync("http://127.0.0.1:"+Port+"/health",ct)).IsSuccessStatusCode;}catch{ct.ThrowIfCancellationRequested();return false;}}}
 public static async Task EnsureReady(string root,CancellationToken ct){if(await Healthy(ct))return;await launchGate.WaitAsync(ct);try{if(!await Healthy(ct))await StartCore(root,ct);}finally{launchGate.Release();}}

 public static bool PreferGpu=true,AllowBackgroundWarmup=true;
 public static bool UsingGpu{get;private set;}
 static CancellationTokenSource warmupCancellation;static bool warmupStarted;
 [System.Runtime.InteropServices.DllImport("nvcuda.dll")]static extern int cuInit(uint flags);
 [System.Runtime.InteropServices.DllImport("nvcuda.dll")]static extern int cuDeviceGetCount(out int count);
 public static bool CudaAvailable(){try{int count;return cuInit(0)==0&&cuDeviceGetCount(out count)==0&&count>0;}catch{return false;}}
 public static Task Start(string appRoot,CancellationToken ct){return EnsureReady(appRoot,ct);}
 public static void Warmup(string appRoot){if(!AllowBackgroundWarmup||warmupStarted)return;warmupStarted=true;warmupCancellation=new CancellationTokenSource();var token=warmupCancellation.Token;Task.Run(async()=>{try{await EnsureReady(appRoot,token);AiDiagnostics.Write("warmup","Model ready before foreground decision.");}catch(OperationCanceledException){}catch(Exception ex){AiDiagnostics.Write("warmup-failed",ex.Message);}});}
 static async Task StartCore(string appRoot,CancellationToken ct){
  ct.ThrowIfCancellationRequested();if(await Healthy(ct))return;string root=Path.GetFullPath(Path.Combine(appRoot,"..","LocalAI"));if(!Directory.Exists(root))root=Path.Combine(appRoot,"LocalAI");
  string cpu=Path.Combine(root,"runtime","llama-server.exe"),gpu=Path.Combine(root,"runtime-cuda","llama-server.exe");string model=Directory.Exists(root)?Directory.GetFiles(root,"*.gguf",SearchOption.TopDirectoryOnly).FirstOrDefault():null;
  if(!File.Exists(cpu)||model==null)throw new Exception("未找到本地模型。请运行安装本地AI脚本，或在设置中指定已有的本地服务。");
  bool useGpu=PreferGpu&&File.Exists(gpu)&&CudaAvailable();Exception last=null;KillOwned();
  foreach(bool accelerated in useGpu?new[]{true,false}:new[]{false}){
   ct.ThrowIfCancellationRequested();string exe=accelerated?gpu:cpu;
   try{
    var info=new ProcessStartInfo(exe,"-m \""+model+"\" --host 127.0.0.1 --port "+Port+" -c 8192 -t "+Math.Max(2,Math.Min(8,Environment.ProcessorCount/2))+" --parallel 1 --cache-ram 512 -ngl "+(accelerated?"99":"0")+" --alias touchline-local"){UseShellExecute=false,CreateNoWindow=true,WorkingDirectory=Path.GetDirectoryName(exe),RedirectStandardError=true,RedirectStandardOutput=true};
    AiDiagnostics.Write("startup","backend="+(accelerated?"CUDA":"CPU")+" exe="+exe+" model="+model);lock(launchSync){ct.ThrowIfCancellationRequested();process=Launch(info,accelerated);}var owned=process;owned.OutputDataReceived+=(s,e)=>{if(e.Data!=null)AiDiagnostics.Write("stdout",e.Data);};owned.ErrorDataReceived+=(s,e)=>{if(e.Data!=null)AiDiagnostics.Write("stderr",e.Data);};owned.EnableRaisingEvents=true;owned.Exited+=(s,e)=>{try{AiDiagnostics.Write("exit","code="+owned.ExitCode);}catch{}};owned.BeginOutputReadLine();owned.BeginErrorReadLine();
    for(int i=0;i<120;i++){ct.ThrowIfCancellationRequested();if(owned.HasExited)throw new Exception("本地模型服务启动失败，退出码："+owned.ExitCode);if(await Healthy(ct)){UsingGpu=accelerated;AiDiagnostics.Write("ready",accelerated?"CUDA model ready.":"CPU model ready.");return;}await Task.Delay(500,ct);}
    throw new Exception("本地模型启动超时。");
   }catch(OperationCanceledException){KillOwned();throw;}catch(Exception ex){last=ex;AiDiagnostics.Write(accelerated?"gpu-fallback":"startup-failed",ex.Message);KillOwned();}
  }
  throw new Exception("本地模型启动失败，请查看诊断日志："+AiDiagnostics.DirectoryPath,last);
 }
 static void KillOwned(){UsingGpu=false;try{if(process!=null&&!process.HasExited){process.Kill();process.WaitForExit(3000);}}catch{}}
 public static void Stop(){if(warmupCancellation!=null)warmupCancellation.Cancel();warmupStarted=false;lock(launchSync){AiDiagnostics.Write("stop","Requested local model shutdown.");KillOwned();}}

}
}



