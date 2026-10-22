using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
namespace Touchline {
public static class MatchMinutes {
 public static int Fatigue(int minutes){return minutes<=30?0:minutes<=60?1:3;}
 public static void Ensure(Game g,LiveMatch m){
  if(m.Minutes==null)m.Minutes=new Dictionary<string,int>();if(m.Minutes.Count>0||m.Round==0)return;
  int elapsed=m.Round*2;foreach(string id in m.UsedHome.Take(10).Concat(m.UsedAway.Take(10)))m.Minutes[id]=elapsed;
  // Legacy saves contain chronological substitutions and the exact outgoing IDs.
  var home=new Queue<string>(m.UsedHome.Skip(10));var away=new Queue<string>(m.UsedAway.Skip(10));int index=0;
  foreach(string e in m.Events){var match=Regex.Match(e,@"^(\d+)′ 换人：(.+)进入 ([A-Za-z_]+)$");if(!match.Success)continue;int at=int.Parse(match.Groups[1].Value);string name=match.Groups[2].Value;
   bool h=home.Count>0&&g.Players.First(p=>p.Id==home.Peek()).Name==name;bool a=away.Count>0&&g.Players.First(p=>p.Id==away.Peek()).Name==name;
   if(h&&a&&index<m.Off.Count){h=m.UsedHome.Contains(m.Off[index]);a=!h;}
   if((!h&&!a)||index>=m.Off.Count)throw new Exception("无法恢复旧比赛换人分钟，请读取赛前存档。");
   string incoming=h?home.Dequeue():away.Dequeue(),outgoing=m.Off[index++];m.Minutes[outgoing]-=elapsed-at;m.Minutes[incoming]=elapsed-at;
  }
  if(home.Count+away.Count>0||index!=m.Off.Count)throw new Exception("旧比赛缺少完整换人记录，请读取赛前存档。");
 }
}
}
