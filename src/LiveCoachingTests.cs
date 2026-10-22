using System;
using System.IO;
using System.Linq;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
namespace Touchline {
public static class LiveCoachingTests {
 static int checks;
 static string Root{get{return Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"test-results","live-coaching");}}
 static void Check(bool condition,string name){if(!condition)throw new Exception("Live coaching: "+name);checks++;}
 class Coach:IClubAgent {
  public List<int> Rounds=new List<int>();public List<string> Phases=new List<string>();public bool Changes=true;
  public Task<AgentDecision> Decide(Game g,Club c,string phase,Fixture f,CancellationToken ct){ct.ThrowIfCancellationRequested();Rounds.Add(g.Live.Round);Phases.Add(phase);bool home=f.Home==c.Id;var m=g.Live;var d=new AgentDecision{tactic=home?m.HomeTactic:m.AwayTactic};if(Changes&&phase=="临场"){
   int count=home?m.HomeSubs:m.AwaySubs;var used=home?m.UsedHome:m.UsedAway;var bench=g.Squad(c.Id).FirstOrDefault(p=>p.Injury==0&&!used.Contains(p.Id));if(count<5&&bench!=null)d.substitutions.Add(new SubDecision{slot=0,playerId=bench.Id});
   d.swaps.Add(new PositionSwap{a=0,b=2});
  }return Task.FromResult(d);}
 }
 class Bad:LocalModelAgent {public int Calls;public Bad():base(new AgentConfig{Enabled=true,Model="test",Endpoint="http://127.0.0.1/test"}){}protected override Task<string> Request(List<object> messages,object schema,CancellationToken ct){Calls++;return Task.FromResult("{\"plan\":99999,\"swapPlan\":0,\"tactic\":\"无战术\"}");}}
 class LegalReply:LocalModelAgent {public LegalReply():base(new AgentConfig{Enabled=true,Model="test",Endpoint="http://127.0.0.1/test"}){}protected override Task<string> Request(List<object> messages,object schema,CancellationToken ct){return Task.FromResult("{\"plan\":0}");}}
 internal static Game Match(int seed=20261031){var g=GameFactory.New(seed);var f=g.Fixtures.First();g.PlayerClubId=f.Home;g.Club(f.Home).Tactics=new[]{"角球战术","影锋","战术犯规"};g.Club(f.Home).Tactic="无战术";Football.Begin(g,f);return g;}
 public static void Run(bool real){Directory.CreateDirectory(Root);if(real){Real();return;}
  var g=Match();var f=g.Fixtures.First();var c=g.Club(f.Away);var coach=new Coach();var log=new List<string>();
  Controller.PlaySegment(g,coach,CancellationToken.None,log.Add,5).GetAwaiter().GetResult();Check(g.Live.Round==5&&coach.Rounds.SequenceEqual(new[]{5}),"10-minute boundary triggers immediately");Check(g.Live.AwaySubs==1&&g.Live.DecisionRounds.SequenceEqual(new[]{5}),"single substitution and persisted marker");
  var restored=Storage.Clone(g);Controller.PlaySegment(restored,coach,CancellationToken.None,log.Add,1).GetAwaiter().GetResult();Check(coach.Rounds.Count==1,"reload does not repeat boundary");g=restored;
  for(int i=0;i<9&&g.Live!=null;i++)Controller.PlaySegment(g,coach,CancellationToken.None,log.Add,5).GetAwaiter().GetResult();
  Check(g.Live==null&&coach.Rounds.SequenceEqual(new[]{5,10,15,20,22,25,30,35,40}),"10 through 80 plus halftime, never 90");Check(coach.Phases.Count(p=>p=="临场")==8&&coach.Phases.Count(p=>p=="中场")==1,"one decision at each scheduled node");var final=g.Fixtures.First();Check(final.Decisions.Count==9&&final.Decisions.Count(x=>x.Lines.Any(s=>s.StartsWith("换位：")))==8,"opponent panel records every adjustment");Check(final.Commentary.Count(s=>s.Contains("换人："))==5,"five-substitution cap for entire match");Check(log.Any(x=>x.Contains("换位：")),"public swap output");
  var swapGame=Match();var m=swapGame.Live;for(int i=0;i<15;i++)Football.Step(swapGame);var beforeMinutes=Storage.Encode(m.Minutes);var beforeUsed=Storage.Encode(m.UsedHome);var beforeOff=Storage.Encode(m.Off);int subs=m.HomeSubs;string a=m.HomeLineup[0],b=m.HomeLineup[2];LiveCoaching.Swap(swapGame,true,0,2);Check(m.HomeLineup[0]==b&&m.HomeLineup[2]==a&&m.HomeSubs==subs,"swap is not a substitution");Check(Storage.Encode(m.Minutes)==beforeMinutes&&Storage.Encode(m.UsedHome)==beforeUsed&&Storage.Encode(m.Off)==beforeOff,"swap preserves minutes, eligibility and off ledger");m.HomeSubs=5;LiveCoaching.Swap(swapGame,true,0,2);Check(m.HomeSubs==5&&m.HomeLineup[0]==a,"swap remains available after five substitutions");
  m.HomeSubs=0;while(m.Round<45)Football.Step(swapGame);Check(m.Minutes.Values.Sum()==1800&&m.Minutes.Values.All(x=>x==90),"position changes never duplicate or reset minutes");Football.Finish(swapGame);swapGame=Match();m=swapGame.Live;
  var candidates=LiveCoaching.Swaps(swapGame,swapGame.Club(swapGame.PlayerClubId),swapGame.Fixtures.First());foreach(var pair in candidates.Where(p=>p!=null))Football.ValidateLineup(swapGame,swapGame.Club(swapGame.PlayerClubId),LiveCoaching.SwapPreview(swapGame,true,pair.a,pair.b),m.HomeTactic);Check(candidates.Count>1&&candidates.Skip(1).All(x=>x.a<9&&x.b<9),"legal outfield swap plans");
  var old=Match();old.Live.Round=12;old.Live.DecisionRounds=null;var noChange=new Coach{Changes=false};Controller.PlaySegment(old,noChange,CancellationToken.None,s=>{},3).GetAwaiter().GetResult();Check(noChange.Rounds.SequenceEqual(new[]{15}),"old save never backfills past decisions");
  var other=Match();other.PlayerClubId=other.Clubs.First(x=>x.Id!=other.Fixtures.First().Home&&x.Id!=other.Fixtures.First().Away).Id;var unused=new Coach{Changes=false};Controller.PlaySegment(other,unused,CancellationToken.None,s=>{},10).GetAwaiter().GetResult();Check(unused.Rounds.Count==0,"non-player fixtures do not get ten-minute calls");
  var badGame=Match();badGame.Live.Round=5;var invalid=new Bad();bool paused=false;try{Controller.PlaySegment(badGame,invalid,CancellationToken.None,s=>{},1).GetAwaiter().GetResult();}catch{paused=true;}Check(paused&&invalid.Calls==3&&badGame.Live.DecisionRounds.Count==0&&badGame.Live.Round==5,"illegal opponent decision pauses without marking done");
  var legalGame=Match();var legalFixture=legalGame.Fixtures.First();var legal=new LegalReply().Decide(legalGame,legalGame.Club(legalFixture.Away),"临场",legalFixture,CancellationToken.None).GetAwaiter().GetResult();Check(legal.swaps.Count==0&&legal.substitutions.Count==0&&legal.tactic==legalGame.Live.AwayTactic,"joint plan zero preserves current lineup and tactic");
  Ui();AiReliabilityTests.Run(false);File.WriteAllText(Path.Combine(Root,"result.json"),Storage.Encode(new{Passed=true,Checks=checks,DecisionMinutes=new[]{10,20,30,40,44,50,60,70,80},FullSeason=false,Scope="Bounded isolated fixtures, boundaries, UI and existing AI regression; no user saves or season simulation."}));
 }
 static IEnumerable<Control> Children(Control c){foreach(Control child in c.Controls){yield return child;foreach(var n in Children(child))yield return n;}}
 static void Snapshot(Form f,string name){Application.DoEvents();using(var image=new Bitmap(f.Width,f.Height)){f.DrawToBitmap(image,new Rectangle(Point.Empty,f.Size));image.Save(Path.Combine(Root,name+".png"));}}
 static void Ui(){var g=Match();bool language=L10n.English;L10n.English=false;
  using(var swap=new PositionSwapForm(g,true)){swap.Show();Snapshot(swap,"swap-zh");Check(Children(swap).OfType<DataGridView>().Single().Rows.Count==10,"swap dialog lists all on-field players");swap.Close();}
  using(var tactic=new MatchTacticForm(g,true)){tactic.Show();var choice=Children(tactic).OfType<DarkComboBox>().Single();choice.SelectedItem="影锋";var text=Children(tactic).OfType<TextBox>().Single();Check(text.Text.Contains(RuleDescriptions.Tactic["影锋"])&&g.Live.HomeTactic=="无战术","effect preview without applying");Snapshot(tactic,"tactic-zh");tactic.Close();Check(g.Live.HomeTactic=="无战术","cancel preview leaves tactic unchanged");}
  LiveCoaching.SetTactic(g,true,"影锋");Check(g.Live.HomeTactic=="影锋","explicit apply changes live tactic");LiveCoaching.SetTactic(g,true,"无战术");
  g.Fixtures.First().Neutral=true;using(var tactic=new MatchTacticForm(g,true)){tactic.Show();Children(tactic).OfType<DarkComboBox>().Single().SelectedItem="战术犯规";Check(!Children(tactic).OfType<Button>().Single(x=>x.Text=="应用战术").Enabled,"finals block tactical foul");tactic.Close();}g.Fixtures.First().Neutral=false;
  L10n.English=true;using(var swap=new PositionSwapForm(g,true)){swap.Show();Snapshot(swap,"swap-en");swap.Close();}using(var tactic=new MatchTacticForm(g,true)){tactic.Show();Snapshot(tactic,"tactic-en");tactic.Close();}L10n.English=false;using(var window=new MainForm()){window.Preview(g);window.Show();L10n.Watch(window);Snapshot(window,"match-zh");Check(Children(window).OfType<Button>().Any(x=>x.Text=="场上换位")&&Children(window).OfType<Button>().Any(x=>x.Text=="战术与效果"),"match actions visible");window.Close();}L10n.English=language;
 }
 static void Real(){string beforeRoot=Storage.Root;typeof(Storage).GetField("Root").SetValue(null,Path.Combine(Root,"isolated-profile"));try{
  var g=Match();var log=new List<string>();var agent=new LocalModelAgent(new AgentConfig{Enabled=true,Endpoint="http://127.0.0.1:17845/v1/chat/completions",Model="touchline-local",TimeoutSeconds=180});var watch=System.Diagnostics.Stopwatch.StartNew();for(int i=0;i<9&&g.Live!=null;i++)Controller.PlaySegment(g,agent,CancellationToken.None,log.Add,5).GetAwaiter().GetResult();watch.Stop();var f=g.Fixtures.First();Check(g.Live==null&&f.Decisions.Count==9,"real model nine scheduled decisions");Check(f.Decisions.All(x=>x.Phase=="临场"||x.Phase=="中场")&&log.All(x=>!x.Contains("固定程序兜底")),"player opponent always model-controlled");
  File.WriteAllText(Path.Combine(Root,"real-result.json"),Storage.Encode(new{Passed=true,Seconds=watch.Elapsed.TotalSeconds,Score=f.HomeGoals+":"+f.AwayGoals,Decisions=f.Decisions,Events=f.Commentary,FullSeason=false}));
 }finally{LocalRuntime.Stop();typeof(Storage).GetField("Root").SetValue(null,beforeRoot);}}
}
}
