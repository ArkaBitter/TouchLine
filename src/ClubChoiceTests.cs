using System;
using System.Linq;
using System.IO;
namespace Touchline {
public static class ClubChoiceTests {
 public static bool Run(){var g=GameFactory.New(41027);foreach(string direction in ClubChoices.Directions){var pool=ClubChoices.Candidates(g,direction);if(pool.Length<10||g.Clubs.Except(pool).Any(c=>ClubChoices.Metric(g,c,direction)>=pool.Min(x=>ClubChoices.Metric(g,x,direction))))throw new Exception("Direction shortlist incorrect");}
  foreach(var c in g.Clubs)c.Funds=500; if(ClubChoices.Candidates(g,"资金充裕").Length!=30)throw new Exception("Boundary ties excluded");
  foreach(var p in g.Players)p.Favor=9;if(g.Clubs.Any(c=>Math.Abs(ClubChoices.Metric(g,c,"文化型俱乐部")-9)>.0001))throw new Exception("Culture must average favor");
  var chosen=ClubChoices.Draw(g,"历史豪门","传控足球");if(chosen.Id!=g.PlayerClubId||chosen.Philosophy!="传控足球"||!ClubChoices.Candidates(g,"历史豪门").Contains(chosen))throw new Exception("New club choice invalid");Football.ValidateLineup(g,chosen,chosen.Lineup,chosen.Tactic);
  string tactic=Rules.Tactics.First(t=>t!="无战术"&&!chosen.Tactics.Contains(t));string before=Storage.Encode(g);bool rejected=false;try{ClubChoices.Purchase(g,tactic);}catch{rejected=true;}if(!rejected||Storage.Encode(g)!=before)throw new Exception("Insufficient funds mutation");
  chosen.Funds=1000;ClubChoices.Purchase(g,tactic);if(chosen.Funds!=0||chosen.Tactics.Length!=6||!chosen.Tactics.Contains(tactic))throw new Exception("Shop purchase incorrect");before=Storage.Encode(g);rejected=false;try{ClubChoices.Purchase(g,tactic);}catch{rejected=true;}if(!rejected||Storage.Encode(g)!=before)throw new Exception("Duplicate purchase mutation");
  chosen.Funds=50000;foreach(string t in Rules.Tactics.Where(t=>t!="无战术"&&!chosen.Tactics.Contains(t)).ToArray())ClubChoices.Purchase(g,t);Rules.Validate(g);if(chosen.Tactics.Length!=24)throw new Exception("Shop max tactics");
  string path=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"test-results","shop-roundtrip.tmsave");Storage.Save(g,path);var loaded=Storage.Load(path);if(loaded.Club(g.PlayerClubId).Tactics.Length!=24||loaded.Club(g.PlayerClubId).Funds!=chosen.Funds)throw new Exception("Shop persistence");
  if(RuleDescriptions.Philosophy.Count!=12||RuleDescriptions.Tactic.Count!=25)throw new Exception("Reference incomplete");return true;
 }
}
}
