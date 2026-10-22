using System;
using System.Linq;
namespace Touchline {
public static class FixedAgentFallback {
 public static AgentDecision Decide(Game g,Club c,string phase,Fixture fixture,string error){
  var d=new AgentDecision{reason="[固定程序兜底] 连续3次非法决定；"+phase+"。原因："+error};
  if(phase=="赛前"){
   var spec=AgentRequest.Build(g,c,phase,fixture);var plan=spec.Plans.Where(p=>p.Tactic!="战术犯规").OrderByDescending(p=>p.Score).First();d.lineup=plan.Lineup;d.tactic=plan.Tactic;d.reason+="；采用评分最高的合法阵容。";
  }else if(LiveCoaching.Phase(phase)&&g.Live!=null){
   string current=fixture.Home==c.Id?g.Live.HomeTactic:g.Live.AwayTactic;
   d.tactic=current=="无战术"||c.Tactics.Contains(current)?current:"无战术";
   if(fixture.Neutral&&d.tactic=="战术犯规")d.tactic="无战术";
   d.reason+="；保持当前合法战术与阵容，不换人。";
  }else if(phase=="阶段规划")d.reason+="；保留全部升级点，本阶段不额外指定重点赛程；玩家比赛和规则保障的关键赛事仍由智能体处理。";
  else if(phase=="续约")d.reason+="；全部续约，避免误解约。";
  else {
   d.rejectOffers=g.Offers.Where(o=>o.Status=="待回应"&&((!o.PlayerDriven&&o.SellerId==c.Id)||(o.PlayerDriven&&o.BuyerId==c.Id))).Select(o=>o.Id).ToArray();
   d.reason+="；拒绝本次待处理报价，不新增交易或挂牌。";
  }
  LocalModelAgent.ValidateDecision(g,c,d,phase,fixture);
  // The normal application path also records the reason in club memory and news.
  if(g.Live!=null)g.Live.Events.Add(c.Name+"："+d.reason);
  else if(fixture!=null)fixture.Commentary.Add(c.Name+"："+d.reason);
  return d;
 }
}
}
