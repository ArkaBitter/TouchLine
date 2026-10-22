using System;
using System.Linq;
namespace Touchline {
public static class DecisionNarration {
 public static string Summary(Game g,AgentDecision d,string phase,Fixture f){
  string source=d.reason??"";string prefix=source.StartsWith("[固定程序兜底]")?"[固定程序兜底] ":source.StartsWith("[自动教练]")?"[自动教练] ":"";
  if(phase=="赛前"||LiveCoaching.Phase(phase)){
   string swaps=d.swaps.Count==0?"":(L10n.English?" Position swaps: ":" 换位：")+string.Join("；",d.swaps.Select(s=>Rules.Slots[s.a]+" ↔ "+Rules.Slots[s.b]))+"。";
   string tactic=f!=null&&f.Neutral&&d.tactic=="战术犯规"?"无战术":d.tactic;
   return prefix+(L10n.English?"Selected tactic: "+L10n.T(tactic)+". ":"选择战术："+tactic+"。")+swaps+
    (phase=="赛前"?(L10n.English?"Starting lineup submitted.":"已提交首发名单。"):
     (d.substitutions.Count==0?(L10n.English?"No substitutions.":"本次不换人。"):
      (L10n.English?"Substitutions: ":"换人：")+string.Join("；",d.substitutions.Select(s=>Rules.Slots[s.slot]+" ← "+g.Players.First(p=>p.Id==s.playerId).Name))+"。"));
  }
  if(phase=="阶段规划")return prefix+(L10n.English?"Priority fixtures: ":"额外重点比赛：")+d.importantMatches.Length+
   (L10n.English?"; training points allocated: ":"场；安排训练点：")+d.training.Sum(t=>t.levels)+(L10n.English?". Unallocated points retained.":"；未分配点数保留。");
  if(phase=="续约")return prefix+(L10n.English?"Release decisions: ":"不续约决定：")+d.release.Length+(L10n.English?"; retirements: ":"人；退役决定：")+d.retire.Length;
  return prefix+(L10n.English?"Transfer proposals: ":"提交采购提案：")+d.offers.Count+
   (L10n.English?"; listing requests: ":"笔；挂牌申请：")+d.listPlayers.Length+
   (L10n.English?"; accept requests: ":"人；申请接受报价：")+d.acceptOffers.Length+
   (L10n.English?"; rejections: ":"笔；拒绝报价：")+d.rejectOffers.Length+
   (L10n.English?". Completion remains subject to transaction checks.":"笔；成交仍以交易校验结果为准。");
 }
}
}
