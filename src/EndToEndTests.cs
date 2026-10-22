using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
namespace Touchline {
// Isolated integration driver. Only the human-controlled club is automated here.
// Other clubs use the production controller, coaching policy and actual local model.
public static class EndToEndTests {
 static string root,path;static int modelCalls;static Stopwatch watch;
 static void Log(string s){File.AppendAllText(Path.Combine(root,"run.log"),DateTime.Now.ToString("s")+" "+s+Environment.NewLine,Encoding.UTF8);}
 class ObservedAgent:IClubAgent {
  readonly IClubAgent inner=new LocalModelAgent(new AgentConfig{Enabled=true,Model="touchline-local",Endpoint="http://127.0.0.1:17845/v1/chat/completions",TimeoutSeconds=180});
  public async Task<AgentDecision> Decide(Game g,Club c,string phase,Fixture f,CancellationToken ct){var timer=Stopwatch.StartNew();Log("MODEL START S"+g.Season+" W"+g.Week+" "+c.Name+" "+phase);var d=await inner.Decide(g,c,phase,f,ct);modelCalls++;File.AppendAllText(Path.Combine(root,"decisions.jsonl"),Storage.Encode(new{season=g.Season,week=g.Week,club=c.Name,phase=phase,seconds=timer.Elapsed.TotalSeconds,decision=d})+Environment.NewLine,Encoding.UTF8);Log("MODEL END "+timer.Elapsed.TotalSeconds.ToString("F1")+"s "+d.reason);return d;}
 }
 static void Save(Game g){Storage.Save(g,path);}
 static void Assert(bool ok,string text){if(!ok)throw new Exception("E2E: "+text);}
 static void Snapshot(Game g){string name="season-"+g.Season;Storage.Save(g,Path.Combine(root,name+".tmsave"));File.WriteAllText(Path.Combine(root,name+"-results.json"),Storage.Encode(new{season=g.Season,matches=g.Fixtures.Count(f=>f.Played),leagues=new[]{1,2,3}.Select(l=>new{league=l,table=Competitions.League(g,l)}).ToArray(),cups=g.Competitions.Select(c=>new{name=c.Id,champion=g.Club(c.Champion).Name,runner=g.Club(c.Runner).Name}).ToArray(),clubs=g.Clubs.Select(c=>new{c.Name,c.Funds,players=g.Squad(c.Id).Count(),wages=g.Squad(c.Id).Sum(p=>p.Value)}).ToArray(),injuryReports=g.Fixtures.Count(f=>f.Commentary.Any(x=>x.Contains("伤病"))),forfeits=g.Fixtures.Count(f=>f.Commentary.Any(x=>x.Contains("弃权")))}),Encoding.UTF8);}
 static void PlayerResponses(Game g){foreach(var o in g.Offers.Where(o=>o.Status=="待回应"&&((o.PlayerDriven&&o.BuyerId==g.PlayerClubId)||(!o.PlayerDriven&&o.SellerId==g.PlayerClubId))).ToArray())Transfers.Complete(g,o,o.PlayerDriven);}
 static void PlayerMarket(Game g){var c=g.Club(g.PlayerClubId);foreach(var p in g.Squad(c.Id).OrderBy(p=>p.Value).ToArray())if(p.Type!="门将"&&g.Squad(c.Id).Count(x=>x.Type==p.Type)>5)p.Listed=true;
  foreach(var p in g.Players.Where(p=>p.ClubId!=c.Id&&(p.ClubId==""||p.Listed)).OrderByDescending(p=>p.Value).ToArray()){
   if(Transfers.OffersRemaining(g,c)==0)break;double fee=Transfers.Fee(g,p,false);if(!Transfers.Affordable(g,p,c,fee)||!Transfers.Useful(g,p,c))continue;
   if(g.Offers.Any(o=>o.Season==g.Season&&o.BuyerId==c.Id&&o.PlayerId==p.Id&&!o.PlayerDriven))continue;
   var o2=Transfers.Offer(g,p,c,fee);if(p.ClubId=="")Transfers.Complete(g,o2,true);
  }
 }
 public static void Run(){root=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"test-results","end-to-end-20261020");Directory.CreateDirectory(root);path=Path.Combine(root,"checkpoint.tmsave");watch=Stopwatch.StartNew();var agent=new ObservedAgent();
  Game g;if(File.Exists(path)){g=Storage.Load(path);Log("RESUME");}else{g=GameFactory.New(20261020,false,"球星迭起");ClubChoices.Draw(g,"板凳充裕","古典足球");Save(g);Log("NEW WORLD "+g.Club(g.PlayerClubId).Name);}
  // Persistence regression: failed saving must not replace the caller's current game.
  Game old=g;var candidate=Storage.Clone(g);candidate.Name="must not commit";string blocked=Path.Combine(root,"blocked-path");Directory.CreateDirectory(blocked);bool failed=false;try{g=Storage.Commit(candidate,blocked);}catch{failed=true;}Assert(failed&&Object.ReferenceEquals(g,old),"failed save changed active game");
  int steps=0;try{while(true){if(++steps>2500)throw new Exception("E2E loop guard");Log("STEP S"+g.Season+" W"+g.Week+" "+g.Phase+" played="+g.Fixtures.Count(f=>f.Played));
    if(g.Phase=="商业收入"){
     Assert(g.Fixtures.All(f=>f.Played)&&g.Competitions.All(c=>c.Completed),"unfinished season");Assert(g.Players.All(p=>p.Appearances<=p.Available),"appearance ledger");Snapshot(g);
     if(g.Season==2){Assert(g.Fixtures.Count(f=>f.Competition=="欧冠")==73&&g.Fixtures.Count(f=>f.Competition=="欧联")==73,"European knockout progression");break;}
    }
    var staged=Storage.Clone(g);
    if(staged.Live==null&&staged.Squad(staged.PlayerClubId).Count(p=>p.Injury==0)>=10)Football.Auto(staged,staged.Club(staged.PlayerClubId));
    if(staged.Phase=="续约"){
     var own=staged.Squad(staged.PlayerClubId).OrderBy(p=>p.Value).FirstOrDefault(p=>staged.Squad(staged.PlayerClubId).Count(x=>x.Type==p.Type)>5);if(own!=null)own.Renew=false;
    }
    PlayerResponses(staged);
    if(staged.Phase=="需求转会"){
     for(int round=0;round<3;round++){Log("MARKET ROUND "+(round+1));PlayerMarket(staged);awaitWindow(staged,agent);PlayerResponses(staged);Save(staged);g=Storage.Clone(staged);}
     File.WriteAllText(Path.Combine(root,"transfer-window.json"),Storage.Encode(new{offers=staged.Offers,clubs=staged.Clubs.Select(c=>new{c.Name,c.Funds,wages=staged.Squad(c.Id).Sum(p=>p.Value)}).ToArray()}),Encoding.UTF8);
     Assert(staged.Clubs.All(c=>staged.Offers.Count(o=>!o.PlayerDriven&&o.Season==staged.Season&&o.BuyerId==c.Id)<=5),"quote cap");SeasonFlow.Advance(staged);
    }else{
     Controller.Advance(staged,agent,CancellationToken.None,Log,checkpoint=>{Save(checkpoint);g=Storage.Clone(checkpoint);}).GetAwaiter().GetResult();
    }
    Rules.Validate(staged);Save(staged);g=Storage.Load(path);
   }
   File.WriteAllText(Path.Combine(root,"result.json"),Storage.Encode(new{Passed=true,season=g.Season,phase=g.Phase,modelCallsThisRun=modelCalls,secondsThisRun=watch.Elapsed.TotalSeconds,player=g.Club(g.PlayerClubId).Name,endedAt=DateTime.Now.ToString("o"),scope="Production controller + real local model; human club driven by test. First season/qualifier completed on baseline build; resumed offseason and European season two on minute-fatigue/six-week-planning revision. See revision-history.txt."}),Encoding.UTF8);Log("PASS");
  }catch(Exception ex){File.WriteAllText(Path.Combine(root,"failure.txt"),ex.ToString(),Encoding.UTF8);Log("FAIL "+ex);throw;}
 }
 static void awaitWindow(Game g,IClubAgent agent){AgentTurns.Window(g,agent,CancellationToken.None,Log).GetAwaiter().GetResult();}
}
}
