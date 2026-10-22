using System;
using System.Linq;
using System.Collections.Generic;
namespace Touchline {
public class OpponentDecisionRecord {
 public string ClubId="",Phase="";public int Minute;public string[] Lines=new string[0];
 public string BeforeTactic="",AfterTactic="",OpponentTactic="";public int ScoreMargin;public int[] SwapSlots=new int[0];
}
public static class OpponentDecisions {
 public static string Text(Game g,Fixture f){
  string opponent=f.Home==g.PlayerClubId?f.Away:f.Home;
  var entries=(f.Decisions??new List<OpponentDecisionRecord>()).Where(x=>x.ClubId==opponent).ToArray();
  var lines=new List<string>();
  foreach(var entry in entries.Reverse()){
   lines.Add(L10n.T(entry.Phase=="赛前"?"赛前决定":entry.Minute+"′ · "+(entry.Phase=="中场"?"中场决定":entry.Phase=="临场"?"临场决定":entry.Phase)));
   // The two player tables already show the lineup. Keep this panel focused on changes.
   lines.AddRange((entry.Lines??new string[0]).Where(x=>!x.StartsWith("首发：")).Select(L10n.T));lines.Add("");
  }
  if(entries.Length==0){lines.Add(L10n.T("本存档尚无对手决策记录。"));lines.Add(L10n.T("当前战术：")+L10n.T(g.Live!=null&&g.Live.FixtureId==f.Id?(f.Home==opponent?g.Live.HomeTactic:g.Live.AwayTactic):g.Club(opponent).Tactic));}
  return string.Join("\r\n",lines.ToArray());
 }
}
}
