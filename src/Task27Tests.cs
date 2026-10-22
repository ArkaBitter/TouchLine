using System;
using System.IO;
using System.Linq;
using System.Drawing;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Collections.Generic;
namespace Touchline {
public static class Task27Tests {
 static int checks;
 static void Check(bool ok,string text){if(!ok)throw new Exception("T22/23/26/27: "+text);checks++;}
 class Replies:LocalModelAgent {
  readonly Queue<string> replies;public int Calls;
  public Replies(params string[] values):base(new AgentConfig{Enabled=true,Model="test",Endpoint="http://127.0.0.1/test"}){replies=new Queue<string>(values);}
  protected override Task<string> Request(List<object> messages,object schema,CancellationToken ct){Calls++;return Task.FromResult(replies.Count>1?replies.Dequeue():replies.Peek());}
 }
 class Measured:LocalModelAgent {
  public int Calls;public Measured():base(new AgentConfig{Enabled=true,Model="touchline-local",Endpoint="http://127.0.0.1:17845/v1/chat/completions",TimeoutSeconds=180}){}
  protected override async Task<string> Request(List<object> messages,object schema,CancellationToken ct){Calls++;return await base.Request(messages,schema,ct);}
 }
 public class Scenario {public string Name,Phase,Snapshot;public Job[] Jobs;}
 public class Job {public string ClubId;}
 static string Root{get{return Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"test-results","tasks-22-23-26-27");}}
 public static void Run(bool real){Directory.CreateDirectory(Root);if(real){Real();return;}
  var g=GameFactory.New(20261027);var c=g.Clubs[1];g.PlayerClubId=g.Clubs[0].Id;g.Week=6;
  foreach(int budget in new[]{0,1,2,5,6,30}){c.Upgrade=budget;var request=AgentRequest.Build(g,c,"阶段规划",null);Check(request.TrainingPlans[0].Count==0,"keep points");
   var schema=Storage.Encode(request.Schema);Check(schema.Contains("trainingPlan")&&!schema.Contains("\"levels\""),"schema selects a budget-safe plan");
   foreach(var plan in request.TrainingPlans){var copy=Storage.Clone(g);var club=copy.Club(c.Id);var d=new AgentDecision{training=plan};LocalModelAgent.ValidateDecision(copy,club,d,"阶段规划",null);AgentTurns.Apply(copy,club,d,"阶段规划",null);Check(club.Upgrade==budget-plan.Sum(t=>t.levels)&&club.Upgrade>=0,"legal plan executes exact cost");}
   var model=new Replies("{\"trainingPlan\":"+(request.TrainingPlans.Count-1)+",\"importantMatches\":[],\"reason\":\"test\"}");var result=model.Decide(g,c,"阶段规划",null,CancellationToken.None).GetAwaiter().GetResult();Check(model.Calls==1&&result.training.Sum(t=>t.levels)<=budget,"plan decoded without retry");
  }
  g.Week=7;Check(TrainingPlans.Build(g,c).Count==1,"no training outside review");g.Week=6;
  var bad=new Replies("{\"trainingPlan\":99999,\"reason\":\"bad\"}","{\"trainingPlan\":0,\"reason\":\"ok\"}");Check(bad.Decide(g,c,"阶段规划",null,CancellationToken.None).GetAwaiter().GetResult().training.Count==0&&bad.Calls==2,"invalid plan corrected");
  var mixed=new Replies("{\"trainingPlan\":0,\"training\":[{\"playerId\":\"p1\",\"attribute\":\"红\",\"levels\":1}]}","{\"trainingPlan\":0}");mixed.Decide(g,c,"阶段规划",null,CancellationToken.None).GetAwaiter().GetResult();Check(mixed.Calls==2,"mixed training fields rejected");
  for(int i=0;i<30;i++){var p=g.Squad(g.Clubs[2].Id).First();g.Offers.Add(new TransferOffer{Id="quote-"+i,PlayerId=p.Id,SellerId=p.ClubId,BuyerId=c.Id,PlayerDriven=true});}
  var req=AgentRequest.Build(g,c,"自主转会",null);string shape=Storage.Encode(req.Schema);Check(shape.Contains("responses")&&!shape.Contains("acceptOffers")&&!shape.Contains("rejectOffers"),"one response field per quote");
  var responses=req.OfferCodes.Keys.ToDictionary(x=>x,x=>x=="q1"?"accept":x=="q30"?"reject":"defer");var response=new Replies(Storage.Encode(new{responses=responses,reason="test"}));var decision=response.Decide(g,c,"自主转会",null,CancellationToken.None).GetAwaiter().GetResult();Check(response.Calls==1&&decision.acceptOffers.Single()=="quote-0"&&decision.rejectOffers.Single()=="quote-29","30 quotes mapped once, defer preserved");
  var wrong=new Replies("{\"responses\":{\"q99\":\"accept\"}}",Storage.Encode(new{responses=responses}));wrong.Decide(g,c,"自主转会",null,CancellationToken.None).GetAwaiter().GetResult();Check(wrong.Calls==2,"unknown or missing quote rejected");
  var conflict=new Replies(Storage.Encode(new{responses=responses,acceptOffers=new[]{"q1"}}),Storage.Encode(new{responses=responses}));conflict.Decide(g,c,"自主转会",null,CancellationToken.None).GetAwaiter().GetResult();Check(conflict.Calls==2,"mixed quote protocols rejected");
  var empty=GameFactory.New(20261027);var zero=new Replies("{\"responses\":{}}");Check(zero.Decide(empty,empty.Clubs[1],"自主转会",null,CancellationToken.None).GetAwaiter().GetResult().acceptOffers.Length==0,"empty quotes supported");
  DecisionPanelAndTheme();
  Check(ProgressTests.Run(),"decision output regression");Check(TransactionRegressionTests.Run(),"transaction conflict and checkpoint regression");
  File.WriteAllText(Path.Combine(Root,"result.json"),Storage.Encode(new{Passed=true,Checks=checks,Scope="Targeted isolated tests and UI only; no season simulation",Completed=DateTime.Now.ToString("o")}));
 }
 static void Snapshot(Form f,string name){Application.DoEvents();using(var image=new Bitmap(f.Width,f.Height)){f.DrawToBitmap(image,new Rectangle(Point.Empty,f.Size));image.Save(Path.Combine(Root,name+".png"));}}
 static IEnumerable<Control> Descendants(Control c){foreach(Control child in c.Controls){yield return child;foreach(var nested in Descendants(child))yield return nested;}}
 static void Pump(int milliseconds){var clock=Stopwatch.StartNew();while(clock.ElapsedMilliseconds<milliseconds){Application.DoEvents();Thread.Sleep(5);}}
 static void DecisionPanelAndTheme(){
  var g=GameFactory.New(20261027);var f=g.Fixtures.First();g.PlayerClubId=f.Home;var foe=g.Club(f.Away);foe.Tactics=new[]{"影锋"};foe.Tactic="无战术";Football.Begin(g,f);
  var d=new AgentDecision{lineup=foe.Lineup,tactic="无战术"};DecisionOutput.Apply(g,foe,d,"赛前",f,s=>{});g.Live.Round=22;var bench=g.Squad(foe.Id).First(p=>!g.Live.UsedAway.Contains(p.Id)&&p.Injury==0);string off=g.Live.AwayLineup[4];d=new AgentDecision{tactic="影锋",substitutions=new List<SubDecision>{new SubDecision{slot=4,playerId=bench.Id}}};DecisionOutput.Apply(g,foe,d,"中场",f,s=>{});DecisionOutput.Apply(g,foe,new AgentDecision{tactic="影锋"},"中场",f,s=>{});
  string text=OpponentDecisions.Text(g,f);Check(f.Decisions.Count==3&&text.Contains("44′")&&text.Contains(bench.Name)&&text.Contains(g.Players.First(p=>p.Id==off).Name)&&text.Contains("无战术 → 影锋")&&text.Contains("本次不换人"),"actual opponent actions recorded");
  var cloned=Storage.Clone(g);Check(OpponentDecisions.Text(cloned,cloned.Fixtures.First())==text,"decision records survive persistence");
  bool english=L10n.English;L10n.English=false;
  using(var form=new MainForm()){
   form.Preview(g);form.Show();Pump(220);var nav=Descendants(form).OfType<NavigationButton>().ToArray();Check(nav.Count(b=>b.Selected)==1&&nav.First(b=>b.Selected).PageName=="俱乐部总览","selected page marker");
   var hovered=nav[1];typeof(NavigationButton).GetMethod("OnMouseEnter",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(hovered,new object[]{EventArgs.Empty});Pump(160);Check(hovered.AnimationAmount>0&&!hovered.Selected,"hover animates without changing page");Snapshot(form,"match-hover");typeof(NavigationButton).GetMethod("OnMouseLeave",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(hovered,new object[]{EventArgs.Empty});Pump(180);Check(hovered.AnimationAmount==0,"hover recedes");
   var panel=Descendants(form).OfType<TextBox>().Single(x=>x.Name=="OpponentDecisions");Check(panel.Visible&&panel.Height>=100&&panel.Width>=350&&panel.Text.Contains(bench.Name),"opponent pane visible");Snapshot(form,"match-zh");form.Size=new Size(1180,740);Pump(100);Check(panel.Height>=100,"decision pane at minimum window");Snapshot(form,"match-minimum");form.Size=new Size(1440,920);L10n.English=true;L10n.Watch(form);L10n.Refresh(form);Pump(180);Snapshot(form,"match-en");Check(panel.Text.Contains("Keep tactic")&&Descendants(form).OfType<Label>().Any(x=>x.Text=="Opponent decisions (latest first)"),"decision pane English labels");
  }
  L10n.English=false;g.Live=null;using(var form=new MainForm()){form.Preview(g);form.Show();Pump(180);Snapshot(form,"overview");form.PreviewPage("赛事统计");Pump(180);Check(Descendants(form).OfType<NavigationButton>().Single(x=>x.Selected).PageName=="赛事统计","page selection follows navigation");Snapshot(form,"statistics");}
  f.Decisions=null;Check(OpponentDecisions.Text(g,f).Contains("尚无"),"old save history is not invented");L10n.English=english;
 }
 static void Real(){
  string input=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"test-results","concurrency-20261020","inputs.json");string original=File.ReadAllText(input);var scenarios=Storage.Decode<Scenario[]>(original);var results=new List<object>();
  foreach(var scenario in scenarios.Where(x=>x.Phase=="阶段规划"||x.Phase=="需求转会"))foreach(var job in scenario.Jobs.Take(2)){
   var g=Storage.Decode<Game>(scenario.Snapshot);var c=g.Club(job.ClubId);var model=new Measured();var watch=Stopwatch.StartNew();var d=model.Decide(g,c,scenario.Phase,null,CancellationToken.None).GetAwaiter().GetResult();watch.Stop();Check(!d.reason.Contains("固定程序兜底"),"real model no fallback");LocalModelAgent.ValidateDecision(g,c,d,scenario.Phase,null);int before=c.Upgrade;double funds=g.Clubs.Sum(x=>x.Funds);DecisionOutput.Apply(g,c,d,scenario.Phase,null,s=>{});Check(c.Upgrade==before-d.training.Sum(t=>t.levels),"real training applied");Check(Math.Abs(funds-g.Clubs.Sum(x=>x.Funds))<.001,"transfer funds conserved");results.Add(new{Phase=scenario.Phase,Club=c.Name,Calls=model.Calls,Seconds=watch.Elapsed.TotalSeconds,Training=d.training.Sum(t=>t.levels),Accepted=d.acceptOffers.Length,Rejected=d.rejectOffers.Length,Offers=d.offers.Count,Decision=d});File.WriteAllText(Path.Combine(Root,"real-progress.json"),Storage.Encode(results));
  }
  Check(File.ReadAllText(input)==original,"benchmark snapshots unchanged");File.WriteAllText(Path.Combine(Root,"real-result.json"),Storage.Encode(new{Passed=true,Results=results,Scope="Four production-schema decisions on copies of T20 snapshots; no full season or benchmark rerun",Completed=DateTime.Now.ToString("o")}));
 }
}
}
