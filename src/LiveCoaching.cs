using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
namespace Touchline {
public class PositionSwap {public int a,b;}
public static class LiveCoaching {
 public static bool Phase(string phase){return phase=="中场"||phase=="临场";}
 public static string[] SwapPreview(Game g,bool home,int a,int b,string[] current=null,string tactic=null){
  var m=g.Live;if(m==null||m.Round>=45)throw new Exception("当前没有进行中的比赛。");if(a<0||a>=10||b<0||b>=10||a==b)throw new Exception("请选择两个不同的场上位置。");
  var f=g.Fixtures.First(x=>x.Id==m.FixtureId);var c=g.Club(home?f.Home:f.Away);var next=(string[])(current??(home?m.HomeLineup:m.AwayLineup)).Clone();string id=next[a];next[a]=next[b];next[b]=id;Football.ValidateLineup(g,c,next,tactic??(home?m.HomeTactic:m.AwayTactic));return next;
 }
 public static void Swap(Game g,bool home,int a,int b){var next=SwapPreview(g,home,a,b);var m=g.Live;var current=home?m.HomeLineup:m.AwayLineup;string message=m.Round*2+"′ 换位："+g.Players.First(p=>p.Id==current[a]).Name+"（"+Rules.Slots[a]+" → "+Rules.Slots[b]+"）／"+g.Players.First(p=>p.Id==current[b]).Name+"（"+Rules.Slots[b]+" → "+Rules.Slots[a]+"）";if(home)m.HomeLineup=next;else m.AwayLineup=next;m.Events.Add(message);}
 public static List<PositionSwap> Swaps(Game g,Club c,Fixture f){var choices=new List<PositionSwap>{null};bool home=f.Home==c.Id;for(int a=0;a<9;a++)for(int b=a+1;b<9;b++){try{SwapPreview(g,home,a,b);choices.Add(new PositionSwap{a=a,b=b});}catch{}}return choices;}
 public static void SetTactic(Game g,bool home,string tactic){var m=g.Live;if(m==null||m.Round>=45)throw new Exception("当前没有进行中的比赛。");var f=g.Fixtures.First(x=>x.Id==m.FixtureId);var c=g.Club(home?f.Home:f.Away);if(tactic!="无战术"&&!c.Tactics.Contains(tactic))throw new Exception("选择了未拥有的战术。");if(f.Neutral&&tactic=="战术犯规")throw new Exception("决赛禁用战术犯规。");if(home)m.HomeTactic=tactic;else m.AwayTactic=tactic;m.Events.Add(m.Round*2+"′ 玩家切换战术："+tactic);}
 public static async Task Boundary(Game g,IClubAgent agent,CancellationToken ct,Action<string> progress){var m=g.Live;if(m==null||m.Round>=45)return;var f=g.Fixtures.First(x=>x.Id==m.FixtureId);
  if(m.Round>=22&&!m.HalfTimeDone){await AgentTurns.HalfTime(g,f,agent,ct,progress);m.HalfTimeDone=true;}
  if(!CoachingPolicy.PlayerMatch(g,f)||m.Round==0||m.Round%5!=0)return;if(m.DecisionRounds==null)m.DecisionRounds=new List<int>();if(m.DecisionRounds.Contains(m.Round))return;
  var opponent=g.Club(f.Home==g.PlayerClubId?f.Away:f.Home);var decision=await CoachingPolicy.Decide(g,opponent,"临场",f,agent,ct,progress);DecisionOutput.Apply(g,opponent,decision,"临场",f,progress);m.DecisionRounds.Add(m.Round);
 }
}
}
