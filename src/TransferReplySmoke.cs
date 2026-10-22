using System;
using System.IO;
using System.Linq;
using System.Threading;
namespace Touchline {
public static class TransferReplySmoke {
 public static void Run(){
  var g=GameFactory.New(20261022);var c=g.Clubs[1];g.PlayerClubId=g.Clubs[0].Id;g.Phase="自主转会";
  foreach(var p in g.Players.Where(p=>p.ClubId!=c.Id).Take(30))g.Offers.Add(new TransferOffer{Id=Guid.NewGuid().ToString("N"),PlayerId=p.Id,SellerId=p.ClubId,BuyerId=c.Id,PlayerDriven=true,Status="待回应",Fee=100});
  var agent=new LocalModelAgent(new AgentConfig{Enabled=true,Model="touchline-local",Endpoint="http://127.0.0.1:17845/v1/chat/completions",TimeoutSeconds=180});
  var d=agent.Decide(g,c,"自主转会",null,CancellationToken.None).GetAwaiter().GetResult();
  if(d.reason.Contains("固定程序兜底"))throw new Exception("Transfer reply fell back");
  if(d.acceptOffers.Concat(d.rejectOffers).Any(id=>!g.Offers.Any(o=>o.Id==id)))throw new Exception("Unknown offer after mapping");
  var root=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"test-results");Directory.CreateDirectory(root);
  File.WriteAllText(Path.Combine(root,"transfer-reply-real-model.json"),Storage.Encode(new{Passed=true,Offers=g.Offers.Count,Accepted=d.acceptOffers.Length,Rejected=d.rejectOffers.Length,Decision=d,Completed=DateTime.Now.ToString("o")}));
 }
}
}
