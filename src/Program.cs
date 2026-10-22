using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Drawing;
using System.Windows.Forms;
using System.Threading;

namespace Touchline {
static class Program {
 [STAThread]static void Main(string[] args){Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);if(args.Any(a=>a.StartsWith("--test")||a=="--render"||a=="--self-test"||a=="--end-to-end"||a=="--parity"))LocalRuntime.AllowBackgroundWarmup=false;L10n.Initialize();if(args.Contains("--english"))L10n.English=true;try{
  if(args.Contains("--test-private-world")){PrivateWorldTests.Run(args[Array.IndexOf(args,"--test-private-world")+1]);return;}
  if(args.Contains("--test-menu-flicker")){MenuFlickerTests.Run();return;}
  if(args.Contains("--test-main-menu")){MainMenuTests.Run();return;}
  if(args.Contains("--test-live-quality")){LiveQualityTests.Run(args.Contains("--real"));return;}
  if(args.Contains("--test-live-coaching")){LiveCoachingTests.Run(args.Contains("--real"));return;}
  if(args.Contains("--test-inference-runtime")){InferenceRuntimeTests.Run();return;}
  if(args.Contains("--test-inference-tuning")){InferenceTuningTests.Run();return;}
  if(args.Contains("--test-squad-templates")){SquadTemplateTests.Run();return;}
  if(args.Contains("--test-tasks27")){try{Task27Tests.Run(args.Contains("--real"));}finally{LocalRuntime.Stop();}return;}
  if(args.Contains("--test-ai-fixes")){try{AiReliabilityTests.Run(args.Contains("--real"));}finally{LocalRuntime.Stop();}return;}
  if(args.Contains("--test-transfer-reply")){try{TransferReplySmoke.Run();}finally{LocalRuntime.Stop();}return;}
  if(args.Contains("--test-strategy")){try{StrategySmoke.Run();}finally{LocalRuntime.Stop();}return;}
  if(args.Contains("--end-to-end")){try{EndToEndTests.Run();}finally{LocalRuntime.Stop();}return;}
  if(args.Contains("--test-match-ui")){MatchUiTests.Run();return;}
  if(args.Contains("--parity")){ParityTests.Run();return;}
  if(args.Contains("--self-test")){SelfTests.Run(false);return;}
  if(args.Contains("--test-local-ai")){try{SelfTests.Run(true);}finally{LocalRuntime.Stop();}return;}
  if(args.Contains("--render")){Game g=GameFactory.New(20261018);g.PlayerClubId=g.Clubs[0].Id;using(var f=new MainForm()){f.Preview(g);f.Show();Application.DoEvents();L10n.Watch(f);using(var b=new Bitmap(f.Width,f.Height)){f.DrawToBitmap(b,new Rectangle(0,0,b.Width,b.Height));b.Save(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"preview.png"));}foreach(string page in new[]{"阵容与编队","理念与战术","赛程与积分","球员与转会","经营与荣誉","赛事统计","本地AI设置"}){f.PreviewPage(page);Application.DoEvents();L10n.Watch(f);using(var b=new Bitmap(f.Width,f.Height)){f.DrawToBitmap(b,new Rectangle(0,0,b.Width,b.Height));b.Save(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"preview_"+page+".png"));}}f.Close();}return;}
  Application.ThreadException+=(s,e)=>UiMessage.Show(e.Exception.Message,"程序异常：已保留自动存档");Application.Run(new MainForm());
 }catch(Exception ex){Directory.CreateDirectory(Storage.Root);File.WriteAllText(Path.Combine(Storage.Root,"last_error.txt"),ex.ToString(),Encoding.UTF8);if(args.Length==0)UiMessage.Show(ex.ToString(),"触线足球经理启动失败");Environment.ExitCode=1;}}
}
static class SelfTests {
 static int passed;static void Check(bool condition,string label){if(!condition)throw new Exception("TEST FAILED: "+label);passed++;File.AppendAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"test-results","progress.log"),DateTime.UtcNow.ToString("o")+" PASS "+label+Environment.NewLine);}
 public static void Run(bool local){string root=AppDomain.CurrentDomain.BaseDirectory;Directory.CreateDirectory(Path.Combine(root,"test-results"));File.WriteAllText(Path.Combine(root,"test-results","progress.log"),"START"+Environment.NewLine);Game g=GameFactory.New(20261018);g.PlayerClubId=g.Clubs[0].Id;
  Check(ExperienceTests.Run(),"minute-based fatigue, legacy match migration, league separation, reviews and six-week policy");
  Check(TransactionRegressionTests.Run(),"save failure atomicity, per-match recovery, contradictory decisions and semifinal model policy");
  Check(SetupRevisionTests.Run(),"hidden-roster renaming permissions, initial philosophy, bilingual resources and runtime endpoint");
  Check(ManagementTests.Run(),"club naming permissions, player opponent requires model, five quotes and five substitutions");
  Check(ClubChoiceTests.Run(),"club directions and ties, selected philosophy, tactic shop funds, duplicate protection and save persistence");
  Check(CareerTests.Run(),"lanes, lifecycle boundaries, legacy mode, symmetric relations, hidden personalities and 50 annual cycles");
  Check(ProgressTests.Run(),"real-time decisions and final scores reach callback and saved news");
  Check(FallbackTests.Run(),"three invalid decisions fallback all phases and reward reports");
  Check(g.Players.Count==800,"800 players");Check(g.Clubs.Count==30,"30 clubs");Check(g.Clubs.All(c=>c.Funds>=1&&c.Funds<=10000&&c.Commercial>=6&&c.Commercial<=35&&c.Honor>=1&&c.Honor<=100),"initial finance distributions");Check(g.Players.All(p=>p.Loyalty>=2&&p.Loyalty<=12&&p.Favor>=2&&p.Favor<=12&&p.Injury==0&&p.Fatigue==0),"player initial state");Check(g.Players.All(p=>p.Foot>=0&&p.Foot<=2),"uniform foot support");Check(g.Fixtures.Count(f=>f.Competition=="联赛")==270,"league schedule count");Check(g.Fixtures.All(f=>f.Week>=4||f.Competition=="联赛"),"first three weeks only league");
  foreach(var c in g.Clubs)Check(g.Fixtures.Count(f=>f.Competition=="联赛"&&(f.Home==c.Id||f.Away==c.Id))==18,"18 league games "+c.Id);
  Player renamed=g.Players[0];string oldName=renamed.Name,oldId=renamed.Id;double oldValue=renamed.Value;PlayerEditing.Rename(renamed,"测试改名");Check(renamed.Id==oldId&&renamed.Value==oldValue&&renamed.Name=="测试改名","rename keeps player identity and attributes");renamed.Name=oldName;
  string s=Storage.Encode(g),s2=Storage.Encode(GameFactory.New(20261018));var same=Storage.Decode<Game>(s2);Check(g.Players.Select(p=>p.Name).SequenceEqual(same.Players.Select(p=>p.Name))&&g.Clubs.Select(c=>c.Funds).SequenceEqual(same.Clubs.Select(c=>c.Funds)),"deterministic initial seed");
  string path=Path.Combine(root,"test-results","roundtrip.tmsave");Storage.Save(g,path);Game loaded=Storage.Load(path);Check(loaded.Players.Count==800&&loaded.Clubs[0].Name==g.Clubs[0].Name,"save round trip");
  Fixture fixture=g.Fixtures.First();Club a=g.Club(fixture.Home),b=g.Club(fixture.Away);Football.Auto(g,a);Football.Auto(g,b);Football.Begin(g,fixture);for(int i=0;i<10;i++)Football.Step(g);Storage.Save(g,path);var mid=Storage.Load(path);Check(mid.Live.Round==10,"mid-match save");while(g.Live.Round<45)Football.Step(g);while(mid.Live.Round<45)Football.Step(mid);Check(g.Live.HomeGoals==mid.Live.HomeGoals&&g.Live.AwayGoals==mid.Live.AwayGoals,"saved PRNG resumes exact match");Check(g.Live.BallEvents.Count(e=>e.Kind=="goal")==g.Live.HomeGoals+g.Live.AwayGoals,"animation goals match simulation");Check(g.Live.BallEvents.Any(e=>e.Kind=="control")&&g.Live.BallEvents.Any(e=>e.Kind=="pass"),"animation actual possession and passes");Check(mid.Live.BallEvents.Count==g.Live.BallEvents.Count,"animation resumes with saved RNG");Football.Finish(g);Check(g.Squad(a.Id).Count(p=>p.Appearances==1)==10,"appearances actual players");Check(g.Squad(a.Id).All(p=>p.Available==1),"eligible appearance ledger");Check(g.Squad(a.Id).Count(p=>p.Fatigue==3)==10,"fatigue actual players");
  var p0=g.Squad(a.Id).First();var buyer=g.Clubs.First(c=>c.Id!=a.Id);buyer.Funds=100000;double total=g.Clubs.Sum(c=>c.Funds);p0.Favor=0;p0.Loyalty=0;var offer=Transfers.Offer(g,p0,buyer,100,true);Transfers.Complete(g,offer,true);Check(p0.ClubId==buyer.Id,"transfer ownership");Check(Math.Abs(total-g.Clubs.Sum(c=>c.Funds))<1e-6,"transfer conservation");Check(p0.Favor>=2&&p0.Favor<=12,"transfer favor two dice");buyer.Funds=0;Check(!Transfers.Affordable(g,g.Squad(a.Id).First(),buyer,1),"wage budget checked");
  Transfers.Release(g,p0);Check(Transfers.Fee(g,p0,false)==0,"free agency no fee");var modelCheck="not requested";
  Check(!typeof(Program).Assembly.GetManifestResourceNames().Contains("Touchline.CurrentWorld"),"no embedded private world");
  Game full=GameFactory.New(87654,false,"球星迭起");full.PlayerClubId=full.Clubs[0].Id;
  // Test-only numerical fixtures: no production fallback. AI integration is separately required below.
  for(int week=1;week<=18;week++){
   while(true){Competitions.Progress(full);var f=Competitions.Next(full);if(f==null)break;foreach(string id in new[]{f.Home,f.Away}){Club c=full.Club(id);foreach(var p in full.Squad(id))p.Injury=0;Football.Auto(full,c);}Football.Begin(full,f);while(full.Live.Round<45)Football.Step(full);Football.Finish(full);}
   SeasonFlow.WeekEnd(full);
  }
  Check(full.Phase=="商业收入"&&full.Competitions.All(c=>c.Completed),"full season all cups complete");Check(full.Fixtures.Count==588,"season one 270 league + 318 domestic cup fixtures");
  double firstIncome=full.Clubs[0].Commercial*36,firstFunds=full.Clubs[0].Funds;SeasonFlow.Advance(full);Check(Math.Abs(full.Clubs[0].Funds-firstFunds-firstIncome)<.001,"commercial money before CV update");SeasonFlow.Advance(full);Check(full.Clubs.All(c=>c.Commercial>=5),"commercial floor");SeasonFlow.Advance(full);Check(full.Players.All(p=>p.Favor<=12),"favor cap12");Check(full.Champions.Count==9&&full.Qualifiers.Count==9&&full.Europa.Count==6,"European slots allocation");
  for(int week=0;week<18;week++){foreach(var f in full.Fixtures.Where(f=>f.Competition=="欧冠资格赛"&&f.Week==full.Week)){foreach(string id in new[]{f.Home,f.Away}){foreach(var p in full.Squad(id))p.Injury=0;Football.Auto(full,full.Club(id));}Football.Begin(full,f);while(full.Live.Round<45)Football.Step(full);Football.Finish(full);}SeasonFlow.WeekEnd(full);}
  Check(full.Phase=="续约"&&full.Champions.Count==12&&full.Europa.Count==12,"qualifier top3 rest6");Check(full.CareerSettled.Contains(1)&&full.Players.Any(p=>p.Id.StartsWith("R1_")&&p.Age==18),"lifecycle automatically follows qualification before renewal");full.Phase="需求转会";SeasonFlow.Advance(full);SeasonFlow.Advance(full);SeasonFlow.Advance(full);Check(full.Season==2&&full.Fixtures.Count(f=>f.Competition=="欧冠")==60&&full.Fixtures.Count(f=>f.Competition=="欧联")==60,"season two European groups");
  for(int week=1;week<=18;week++){
   while(true){Competitions.Progress(full);var next=Competitions.Next(full);if(next==null)break;
    if(full.Squad(next.Home).Count(p=>p.Injury==0)<10||full.Squad(next.Away).Count(p=>p.Injury==0)<10){next.HomeGoals=0;next.AwayGoals=3;continue;}
    Football.Auto(full,full.Club(next.Home));Football.Auto(full,full.Club(next.Away));Football.Begin(full,next);while(full.Live.Round<45)Football.Step(full);Football.Finish(full);
   }SeasonFlow.WeekEnd(full);
  }
  Check(full.Fixtures.Count==734&&full.Competitions.Count==8&&full.Competitions.All(c=>c.Completed),"season two all five competitions + real injuries/fatigue");
  Check(full.Fixtures.Count(f=>f.Competition=="欧冠")==73&&full.Fixtures.Count(f=>f.Competition=="欧联")==73,"each Europe tournament 73 matches");
  Check(full.Fixtures.Any(f=>f.Commentary.Any(e=>e.Contains("伤病：赛后伤停"))),"injuries persisted in match report");
  Check(full.Players.All(p=>p.Appearances<=p.Available),"available matches track injury and substitutions");
  Fixture substitutionFixture=g.Fixtures.First(f=>!f.Played);Club sh=g.Club(substitutionFixture.Home),sa=g.Club(substitutionFixture.Away);
  foreach(var pp in g.Players)pp.Injury=0;Football.Auto(g,sh);Football.Auto(g,sa);Football.Begin(g,substitutionFixture);
  Football.Step(g);Player reserve=g.Squad(sh.Id).First(p=>!g.Live.UsedHome.Contains(p.Id));string off=g.Live.HomeLineup[0];Football.Substitute(g,true,0,reserve.Id);Check(g.Live.HomeSubs==1&&g.Live.Off.Contains(off),"manual substitution tracked");
  bool reentry=false;try{Football.Substitute(g,true,1,off);}catch{reentry=true;}Check(reentry,"substituted player cannot return");
  var restored=Storage.Clone(g);while(g.Live.Round<45)Football.Step(g);Football.Finish(g);Check(reserve.Appearances>0&&g.Players.First(p=>p.Id==off).Appearances>0,"both substituted players receive appearance");
  bool rejected=false;try{LocalModelAgent.CheckEndpoint("https://example.com/v1/chat/completions");}catch{rejected=true;}Check(rejected,"offline localhost only");
  if(local){LocalRuntime.Stop();var config=new AgentConfig{Enabled=true,Model="touchline-local",Endpoint="http://127.0.0.1:17845/v1/chat/completions",TimeoutSeconds=180};var agent=new LocalModelAgent(config);var target=GameFactory.New(20261018);var f=target.Fixtures.First();target.PlayerClubId=f.Away;var d=agent.Decide(target,target.Club(f.Home),"赛前",f,CancellationToken.None).GetAwaiter().GetResult();Check(d.lineup.Length==10,"real local model lineup");AgentTurns.Apply(target,target.Club(f.Home),d,"赛前",f);modelCheck=d.reason;
  var d2=agent.Decide(target,target.Club(f.Away),"赛前",f,CancellationToken.None).GetAwaiter().GetResult();AgentTurns.Apply(target,target.Club(f.Away),d2,"赛前",f);Football.Begin(target,f);for(int z=0;z<22;z++)Football.Step(target);
  var half=agent.Decide(target,target.Club(f.Home),"中场",f,CancellationToken.None).GetAwaiter().GetResult();AgentTurns.Apply(target,target.Club(f.Home),half,"中场",f);Check(half.tactic!="","real model halftime");target.Live=null;
  var renew=agent.Decide(target,target.Club(f.Home),"续约",null,CancellationToken.None).GetAwaiter().GetResult();Check(renew.release.Length<=target.Squad(f.Home).Count()-10,"real model renewal");
  target.Phase="需求转会";var window=agent.Decide(target,target.Club(f.Home),"需求转会",null,CancellationToken.None).GetAwaiter().GetResult();AgentTurns.Apply(target,target.Club(f.Home),window,"需求转会",null);Check(window.offers.Count<=3,"real model market actions");
  File.WriteAllText(Path.Combine(root,"test-results","local-manager-actions.json"),Storage.Encode(new{Home=d,Away=d2,HalfTime=half,Renewal=renew,Market=window}),Encoding.UTF8);
  File.WriteAllText(Path.Combine(root,"test-results","local-agent-decision.json"),Storage.Encode(d),Encoding.UTF8);}
  File.WriteAllText(Path.Combine(root,"test-results",local?"local-ai-result.json":"self-test-result.json"),Storage.Encode(new{Passed=passed,LocalModel=modelCheck,SeasonTwoMatches=full.Fixtures.Count,CompletedAt=DateTime.UtcNow.ToString("o")}),Encoding.UTF8);
 }
}
}
