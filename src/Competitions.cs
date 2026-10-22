using System;
using System.Linq;
using System.Collections.Generic;

namespace Touchline {
public class TableRow {public string ClubId,Name;public int Played,Points,GF,GA,Rank;public int GD{get{return GF-GA;}}}
public static class Competitions {
 public static string Key(Fixture f){return f.Competition+"|"+f.Group.Split('-')[0];}
 public static List<TableRow> Table(Game g,IEnumerable<string> ids,IEnumerable<Fixture> fixtures){var fs=fixtures.Where(f=>f.Played).ToList();var rows=ids.Select(id=>{var games=fs.Where(f=>f.Home==id||f.Away==id);int pts=0,gf=0,ga=0,played=0;foreach(var f in games){int a=f.Home==id?f.HomeGoals:f.AwayGoals,b=f.Home==id?f.AwayGoals:f.HomeGoals;pts+=a>b?3:a==b?1:0;gf+=a;ga+=b;played++;}return new TableRow{ClubId=id,Name=g.Club(id).Name,Played=played,Points=pts,GF=gf,GA=ga};}).ToList();
  rows.Sort((a,b)=>{int d=b.Points.CompareTo(a.Points);if(d!=0)return d;d=b.GD.CompareTo(a.GD);if(d!=0)return d;d=b.GF.CompareTo(a.GF);if(d!=0)return d;int ap=0,bp=0,aa=0,ba=0;foreach(var f in fs.Where(f=>(f.Home==a.ClubId&&f.Away==b.ClubId)||(f.Home==b.ClubId&&f.Away==a.ClubId))){int x=f.Home==a.ClubId?f.HomeGoals:f.AwayGoals,y=f.Home==a.ClubId?f.AwayGoals:f.HomeGoals;ap+=x>y?3:x==y?1:0;bp+=y>x?3:x==y?1:0;if(f.Home==a.ClubId)ba+=f.AwayGoals;else aa+=f.AwayGoals;}d=bp.CompareTo(ap);if(d!=0)return d;d=ba.CompareTo(aa);if(d!=0)return d;int al=g.TieLots.ContainsKey(a.ClubId)?g.TieLots[a.ClubId]:a.ClubId.GetHashCode(),bl=g.TieLots.ContainsKey(b.ClubId)?g.TieLots[b.ClubId]:b.ClubId.GetHashCode();return al.CompareTo(bl);});
  for(int i=0;i<rows.Count;i++)rows[i].Rank=i+1;return rows;
 }
 public static List<TableRow> League(Game g,int l){return Table(g,g.Clubs.Where(c=>c.League==l).Select(c=>c.Id),g.Fixtures.Where(f=>f.Competition=="联赛"&&g.Club(f.Home).League==l));}
 public static void AddRoundRobin(Game g,string name,string[] teams,int startWeek,string group){var wheel=teams.ToList();if(wheel.Count%2==1)wheel.Add("");int rounds=wheel.Count-1;var first=new List<Fixture>();for(int r=0;r<rounds;r++){for(int j=0;j<wheel.Count/2;j++){string a=wheel[j],b=wheel[wheel.Count-1-j];if(a==""||b=="")continue;if((r+j)%2==0){string temp=a;a=b;b=temp;}first.Add(new Fixture{Id=Guid.NewGuid().ToString("N"),Competition=name,Group=group,Home=a,Away=b,Week=startWeek+r,Stage="小组"});}string last=wheel[wheel.Count-1];wheel.RemoveAt(wheel.Count-1);wheel.Insert(1,last);}g.Fixtures.AddRange(first);g.Fixtures.AddRange(first.Select(f=>new Fixture{Id=Guid.NewGuid().ToString("N"),Competition=name,Group=group,Home=f.Away,Away=f.Home,Week=f.Week+rounds,Stage="小组"}));}
 public static void StartSeason(Game g,bool europe){g.Fixtures.Clear();g.Competitions.Clear();g.Week=1;var rng=new RandomSource(g);for(int l=1;l<=3;l++){
   string[] teams=g.Clubs.Where(c=>c.League==l).Select(c=>c.Id).ToArray();AddRoundRobin(g,"联赛",teams,1,"L"+l);
   foreach(string name in new[]{"一级杯赛","二级杯赛"}){var draw=rng.Shuffle(teams);var comp=new Competition{Id=name+"|L"+l,Name=name,League=l};comp.Groups.Add(draw.Take(5).ToArray());comp.Groups.Add(draw.Skip(5).ToArray());g.Competitions.Add(comp);for(int j=0;j<2;j++)AddRoundRobin(g,name,comp.Groups[j],4,"L"+l+"-"+j);}
  }
  if(europe){foreach(string name in new[]{"欧冠","欧联"}){var ids=name=="欧冠"?g.Champions:g.Europa;if(ids.Count!=12||ids.Distinct().Count()!=12)throw new Exception("欧战须有12支不同球队。");var draw=rng.Shuffle(ids);var c=new Competition{Id=name+"|E",Name=name};c.Groups.Add(draw.Take(6).ToArray());c.Groups.Add(draw.Skip(6).ToArray());g.Competitions.Add(c);for(int j=0;j<2;j++)AddRoundRobin(g,name,c.Groups[j],4,"E-"+j);}}
  g.Phase="常规赛";g.Log("赛季赛程已生成，前三周仅联赛。每周顺序：联赛、一级杯赛、二级杯赛、欧冠、欧联。");
 }
 public static int Order(Fixture f){return Array.IndexOf(new[]{"联赛","一级杯赛","二级杯赛","欧冠","欧联","欧冠资格赛"},f.Competition);}
 public static Fixture Next(Game g){return g.Fixtures.Where(f=>!f.Played&&f.Week==g.Week).OrderBy(Order).ThenBy(f=>f.Id).FirstOrDefault();}
 public static void Progress(Game g){foreach(var c in g.Competitions.Where(x=>!x.Completed)){
   var fs=g.Fixtures.Where(f=>Key(f)==c.Id).ToList();if(fs.Count==0)continue;
   if(!fs.Any(f=>f.Stage!="小组")&&fs.All(f=>f.Played)){
    var a=Table(g,c.Groups[0],fs.Where(f=>f.Group.EndsWith("-0"))).Take(4).Select(r=>r.ClubId).ToArray();var b=Table(g,c.Groups[1],fs.Where(f=>f.Group.EndsWith("-1"))).Take(4).Select(r=>r.ClubId).ToArray();
    string[][] pairs={new[]{a[0],b[3]},new[]{a[1],b[2]},new[]{b[0],a[3]},new[]{b[1],a[2]}};AddKnockout(g,c,pairs,"八强",14);g.Log(c.Id+"八强产生。");
   }
   foreach(string stage in new[]{"八强","半决赛","决赛"}){var round=g.Fixtures.Where(f=>Key(f)==c.Id&&f.Stage==stage).ToList();if(round.Count==0||round.Any(f=>!f.Played))continue;var winners=new List<string>();
    foreach(var tie in round.GroupBy(f=>f.Tie).OrderBy(x=>x.Key)){var games=tie.ToList();string home=games[0].Home,away=games[0].Away;int x=games.Sum(f=>f.Home==home?f.HomeGoals:f.AwayGoals),y=games.Sum(f=>f.Home==home?f.AwayGoals:f.HomeGoals);string winner=games[0].Winner;if(winner=="")winner=x>y?home:x<y?away:Decide(g,games.Last());foreach(var f in games)f.Winner=winner;winners.Add(winner);}
    if(stage=="决赛"){c.Completed=true;c.Champion=winners[0];var f=round[0];c.Runner=f.Home==c.Champion?f.Away:f.Home;g.Log("🏆 "+g.Club(c.Champion).Name+"夺得"+c.Id+"冠军！");}
    else{string next=stage=="八强"?"半决赛":"决赛";if(g.Fixtures.Any(f=>Key(f)==c.Id&&f.Stage==next))continue;if(stage=="八强")c.Semis=winners.ToArray();var pairs=Enumerable.Range(0,winners.Count/2).Select(i=>new[]{winners[i*2],winners[i*2+1]}).ToArray();AddKnockout(g,c,pairs,next,stage=="八强"?16:18);}
   }
  }}
 static void AddKnockout(Game g,Competition c,string[][] pairs,string stage,int week){for(int j=0;j<pairs.Length;j++){string tie=c.Id+"/"+stage+"/"+j;for(int leg=0;leg<(stage=="决赛"?1:2);leg++)g.Fixtures.Add(new Fixture{Id=Guid.NewGuid().ToString("N"),Competition=c.Name,Group=c.Id.Split('|')[1],Stage=stage,Tie=tie,Leg=leg,Home=pairs[j][leg],Away=pairs[j][1-leg],Week=week+leg,Neutral=stage=="决赛"});}}
 static string Decide(Game g,Fixture f){Club a=g.Club(f.Home),b=g.Club(f.Away);var before=g.Live;try{int attempts=0;while(true){attempts++;if(attempts>100)throw new Exception("淘汰赛连续100场加赛仍平局，已暂停以避免无限循环。请检查双方进攻能力。");g.Live=new LiveMatch{FixtureId=f.Id,HomeLineup=(string[])a.Lineup.Clone(),AwayLineup=(string[])b.Lineup.Clone(),HomeTactic=a.Tactic,AwayTactic=b.Tactic};if(f.Neutral){if(g.Live.HomeTactic=="战术犯规")g.Live.HomeTactic="无战术";if(g.Live.AwayTactic=="战术犯规")g.Live.AwayTactic="无战术";}for(int i=0;i<45;i++)Football.Step(g);if(g.Live.HomeGoals!=g.Live.AwayGoals){string winner=g.Live.HomeGoals>g.Live.AwayGoals?a.Id:b.Id;f.Commentary.Add("总比分／决赛平局，额外模拟"+attempts+"场后 "+g.Club(winner).Name+"晋级。");return winner;}}}finally{g.Live=before;}}
 public static void Allocate(Game g){g.Champions.Clear();g.Europa.Clear();g.Qualifiers.Clear();foreach(int l in new[]{1,2,3}){
   var table=League(g,l);foreach(var r in table)g.LastRanks[r.ClubId]=r.Rank;string[] ranks=table.Select(r=>r.ClubId).ToArray();var cup1=g.Competitions.FirstOrDefault(c=>c.Name=="一级杯赛"&&c.League==l);var cup2=g.Competitions.FirstOrDefault(c=>c.Name=="二级杯赛"&&c.League==l);
   var direct=new[]{ranks[0],ranks[1],cup1==null?"":cup1.Champion}.Where(x=>x!="").Distinct().ToList();foreach(string id in ranks)if(direct.Count<3&&!direct.Contains(id))direct.Add(id);
   var qualifier=new[]{ranks[2],ranks[3],cup2==null?"":cup2.Champion}.Where(x=>x!=""&&!direct.Contains(x)).Distinct().ToList();foreach(string id in ranks)if(qualifier.Count<3&&!direct.Contains(id)&&!qualifier.Contains(id))qualifier.Add(id);
   var el=new[]{ranks[4],ranks[5]}.Where(x=>!direct.Contains(x)&&!qualifier.Contains(x)).ToList();foreach(string id in ranks)if(el.Count<2&&!direct.Contains(id)&&!qualifier.Contains(id)&&!el.Contains(id))el.Add(id);
   g.Champions.AddRange(direct);g.Qualifiers.AddRange(qualifier);g.Europa.AddRange(el);
  }AddRoundRobin(g,"欧冠资格赛",g.Qualifiers.ToArray(),g.Week,"Q");g.Phase="欧冠资格赛";g.Log("欧战分配：9队直通欧冠，9队资格赛，6队直通欧联。资格赛前3进欧冠，其余进欧联。");}
 public static void FinishQualifier(Game g){var rows=Table(g,g.Qualifiers,g.Fixtures.Where(f=>f.Competition=="欧冠资格赛"));g.Champions.AddRange(rows.Take(3).Select(r=>r.ClubId));g.Europa.AddRange(rows.Skip(3).Select(r=>r.ClubId));g.Phase="续约";CareerCycle.Settle(g);g.Log("欧冠／欧联各12队已确定。进入续约阶段。");}
}
}
