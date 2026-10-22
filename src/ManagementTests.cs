using System;
using System.Linq;
using System.Threading;
namespace Touchline {
public static class ManagementTests {
 public static bool Run(){var g=GameFactory.New(1442);var f=g.Fixtures.First();var names=g.Clubs.ToDictionary(c=>c.Id,c=>"测试"+c.Id);string home=f.Home;ClubNaming.AllBeforeStart(g,names);if(f.Home!=home||g.Club(home).Name!=names[home])throw new Exception("Rename changed identity");g.PlayerClubId=f.Home;bool rejected=false;try{ClubNaming.AllBeforeStart(g,names);}catch{rejected=true;}if(!rejected)throw new Exception("World rename allowed after start");rejected=false;try{ClubNaming.Own(g,f.Away,"外队");}catch{rejected=true;}if(!rejected)throw new Exception("Opponent rename allowed");ClubNaming.Own(g,f.Home,"自己的球队");
  if(!CoachingPolicy.RequiresModel(g,f))throw new Exception("Player match bypassed model");var invalid=new InvalidReplyAgent();rejected=false;try{invalid.Decide(g,g.Club(f.Away),"赛前",f,CancellationToken.None).GetAwaiter().GetResult();}catch{rejected=true;}if(!rejected||invalid.Calls!=3)throw new Exception("Player match illegally fell back");
  var normal=g.Fixtures.First(x=>x.Home!=g.PlayerClubId&&x.Away!=g.PlayerClubId&&x.Competition=="联赛");if(CoachingPolicy.RequiresModel(g,normal))throw new Exception("Routine model calls not reduced");
  var buyer=g.Club(g.PlayerClubId);buyer.Funds=1000000;var targets=g.Players.Where(p=>p.ClubId!=buyer.Id).Take(6).ToArray();foreach(var p in targets)p.ClubId="";for(int i=0;i<5;i++){var o=Transfers.Offer(g,targets[i],buyer,0);o.Status="已拒绝";}rejected=false;try{Transfers.Offer(g,targets[5],buyer,0);}catch{rejected=true;}if(!rejected||Transfers.OffersRemaining(Storage.Clone(g),buyer)!=0)throw new Exception("Quote cap or persistence failed");g.Season++;if(Transfers.OffersRemaining(g,buyer)!=5)throw new Exception("New window not reset");
  g=GameFactory.New(728);f=g.Fixtures.First();Football.Begin(g,f);for(int i=0;i<5;i++){var p=g.Squad(f.Home).First(x=>!g.Live.UsedHome.Contains(x.Id));Football.Substitute(g,true,i,p.Id);}rejected=false;try{Football.Substitute(g,true,5,g.Squad(f.Home).First(x=>!g.Live.UsedHome.Contains(x.Id)).Id);}catch{rejected=true;}if(!rejected||g.Live.HomeSubs!=5)throw new Exception("Sixth substitution accepted");return true;
 }
}
}
