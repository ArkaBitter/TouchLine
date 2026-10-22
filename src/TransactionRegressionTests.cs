using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
namespace Touchline {
public static class TransactionRegressionTests {
 class CompactReplyAgent:LocalModelAgent {public CompactReplyAgent():base(new AgentConfig{Enabled=true,Model="test",Endpoint="http://127.0.0.1/test"}){}protected override Task<string> Request(System.Collections.Generic.List<object> messages,object schema,CancellationToken ct){return Task.FromResult(Storage.Encode(new{responses=Enumerable.Range(1,30).ToDictionary(i=>"q"+i,i=>i==1?"accept":i==30?"reject":"defer"),reason="test"}));}}
 class FailingAgent:IClubAgent {public Task<AgentDecision> Decide(Game g,Club c,string phase,Fixture f,CancellationToken ct){throw new Exception("Injected model outage");}}
 static void Reject(Action action,string label){bool failed=false;try{action();}catch{failed=true;}if(!failed)throw new Exception(label);}
 public static bool Run(){
  var g=GameFactory.New(11020);g.PlayerClubId=g.Clubs[0].Id;var c=g.Clubs[1];
  var conflict=new AgentDecision{acceptOffers=new[]{"same"},rejectOffers=new[]{"same"}};
  var transferGame=Storage.Clone(g);var seller=transferGame.Clubs[0];var target=transferGame.Squad(seller.Id).First();for(int i=0;i<30;i++)transferGame.Offers.Add(new TransferOffer{Id="long-offer-id-"+i,PlayerId=target.Id,SellerId=seller.Id,BuyerId=c.Id,PlayerDriven=true,Status="待回应"});
  var request=AgentRequest.Build(transferGame,transferGame.Club(c.Id),"自主转会",null);if(request.OfferCodes.Count!=30||request.OfferCodes["q30"]!="long-offer-id-29"||request.ReplyTokens<=350)throw new Exception("Compact offer identifiers or response budget missing");
  var reply=new CompactReplyAgent().Decide(transferGame,transferGame.Club(c.Id),"自主转会",null,CancellationToken.None).GetAwaiter().GetResult();if(reply.acceptOffers.Single()!="long-offer-id-0"||reply.rejectOffers.Single()!="long-offer-id-29")throw new Exception("Compact offer response mapped to wrong transaction");
  Reject(()=>LocalModelAgent.ValidateDecision(g,c,conflict,"需求转会",null),"Conflicting offer response accepted");
  var duplicates=new AgentDecision();duplicates.offers.Add(new AgentOffer{playerId="same"});duplicates.offers.Add(new AgentOffer{playerId="same"});Reject(()=>LocalModelAgent.ValidateDecision(g,c,duplicates,"需求转会",null),"Duplicate proposals accepted");
  var semi=new Fixture{Home=g.Clubs[1].Id,Away=g.Clubs[2].Id,Stage="半决赛",Competition="一级杯赛"};if(!CoachingPolicy.RequiresModel(g,semi))throw new Exception("Semifinal bypassed model");
  foreach(var club in g.Clubs)g.PlanningDone.Add(StrategicPlanning.Key(g,club));
  g.Fixtures.Clear();g.Competitions.Clear();g.Fixtures.Add(new Fixture{Id="a",Home=g.Clubs[1].Id,Away=g.Clubs[2].Id,Week=1});g.Fixtures.Add(new Fixture{Id="b",Home=g.Clubs[0].Id,Away=g.Clubs[3].Id,Week=1});
  string root=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"test-results","transactions");Directory.CreateDirectory(root);string path=Path.Combine(root,"checkpoint.tmsave");Game committed=Storage.Clone(g);
  Reject(()=>Controller.Advance(g,new FailingAgent(),CancellationToken.None,s=>{},snapshot=>committed=Storage.Commit(snapshot,path)).GetAwaiter().GetResult(),"Injected model outage did not propagate");
  if(!committed.Fixtures[0].Played||committed.Fixtures[1].Played||Storage.Load(path).Fixtures.Count(f=>f.Played)!=1)throw new Exception("Completed match checkpoint lost");
  var forfeit=Storage.Clone(committed);foreach(var p in forfeit.Squad(forfeit.PlayerClubId))p.Injury=1;Controller.Advance(forfeit,new FailingAgent(),CancellationToken.None,s=>{}).GetAwaiter().GetResult();if(!forfeit.Fixtures[1].Played||!forfeit.Fixtures[1].Commentary.Any(s=>s.Contains("弃权")))throw new Exception("Forfeit missing from match review");
  string blocked=Path.Combine(root,"directory-not-save");Directory.CreateDirectory(blocked);Game before=committed;Reject(()=>committed=Storage.Commit(g,blocked),"Save failure was not reported");if(!Object.ReferenceEquals(before,committed))throw new Exception("Failed save committed memory");
  return true;
 }
}
}
