using System;
using System.Linq;
using System.Collections.Generic;

namespace Touchline {
public class Ability {public Player P;public double R,G,B,K,S;public Ability Copy(){return (Ability)MemberwiseClone();}}
public static class Football {
 public static Ability Weight(Player p,int slot,string philosophy,string tactic,string opponent,int status,bool home, bool star) {
  if(p==null)return new Ability();double r=p.Red,g=p.Green,b=p.Blue,s=p.Shot;int foot=p.Foot,pos=slot==9?0:slot%3;
  if((tactic=="三中卫"&&(slot==6||slot==8))||(tactic=="中场内收"&&(slot==3||slot==5)))pos=1;
  if((tactic=="全场自由人"&&slot==4)||(tactic=="单后腰"&&slot==7)||(tactic=="进攻终结点"&&slot==1)||(tactic=="边后卫内收"&&((slot==6&&foot==2)||(slot==8&&foot==1))))foot=0;
  string type=Rules.Expected(tactic,slot);
  if(type=="前锋") {if(pos==0){if(foot==1){r*=1.5;s*=.5;}else if(foot==2){r*=.75;s*=1.25;}}else if(pos==1){if(foot==0)s*=1.5;}else{if(foot==1){r*=.5;s*=1.5;}else if(foot==2){r*=1.25;s*=.75;}}}
  if(type=="中场") {if(pos==1&&foot==0)g*=1.5;else if(pos==0)g*=foot==1?1.25:foot==2?.75:1;else if(pos==2)g*=foot==2?1.25:foot==1?.75:1;}
  if(type=="后卫") {if(pos==1&&foot==0)b*=1.5;else if(pos==0)b*=foot==1?1.25:foot==2?.75:1;else if(pos==2)b*=foot==2?1.25:foot==1?.75:1;}
  if(type=="门将"&&foot!=0)s*=.8;
  if(p.Type!=type){r*=.9;g*=.9;b*=.9;s*=.9;if(type=="门将"||p.Type=="门将")s=0;}
  double f=Rules.Status[status]*Math.Max(0,1-p.Fatigue/100.0)*(home?1.05:1);r*=f;g*=f;b*=f;s*=f;
  double rm=1,gm=1,bm=1;
  switch(philosophy){
   case "古典足球":if(new[]{"九锋阵","传控足球","链式防守"}.Contains(opponent)){if(p.Type=="前锋")bm=1.15;if(p.Type=="中场")gm=1.15;if(p.Type=="后卫")rm=1.15;}break;
   case "全攻全守":gm=1.05;if(opponent=="古典足球"){rm=bm=1.1;}else if(new[]{"连续短传","低位防守","快速渗透","高位压迫","防守反击","过顶长传","边中结合"}.Contains(opponent)){rm=bm=.97;}break;
   case "九锋阵":rm=1.3;bm=.8;break;case "传控足球":gm=1.1;break;case "链式防守":bm=1.3;rm=.8;break;
   case "连续短传":gm=1.05+Delta(opponent,"边中结合","快速渗透","低位防守","防守反击");break;
   case "低位防守":bm=1.05+Delta(opponent,"连续短传","边中结合","高位压迫","快速渗透");break;
   case "快速渗透":rm=1.05+Delta(opponent,"低位防守","过顶长传","连续短传","高位压迫");break;
   case "高位压迫":rm=1.05;bm=1+Delta(opponent,"快速渗透","低位防守","防守反击","过顶长传");break;
   case "防守反击":bm=1.05;rm=1+Delta(opponent,"高位压迫","连续短传","过顶长传","边中结合");break;
   case "过顶长传":rm=1.05+Delta(opponent,"防守反击","高位压迫","边中结合","快速渗透");break;
   case "边中结合":gm=1.05+Delta(opponent,"过顶长传","防守反击","连续短传","低位防守");break;
  }
  r*=rm;g*=gm;b*=bm;
  if(tactic=="45度炸"&&(slot==3||slot==5))r*=1.05;
  if(tactic=="弧顶内切"&&(slot==0||slot==2))s*=1.025;
  if(tactic=="远射"&&slot==4)s*=1.05;
  if(tactic=="主攻左路"||tactic=="主攻右路") {double m=1;if(slot<9&&slot%3!=1)m=((slot%3==0)==(tactic=="主攻左路"))?1.05:.95;if(slot<3)r*=m;else if(slot<6)g*=m;else if(slot<9)b*=m;}
  if(tactic=="造越位"&&p.Type=="后卫"){r*=.9;b*=1.1;}
  if(tactic=="摆大巴"){r*=.8;g*=.8;b*=1.5;}
  if(tactic=="巨星战术"){double m=star?1.08:.995;r*=m;g*=m;b*=m;s*=m;}
  if(tactic=="中卫前提"&&slot==7)b*=1.1;
  if((tactic=="影锋"||tactic=="双后腰")&&slot==4)g*=1.05;
  if(tactic=="抢落点"&&slot==1)s*=1.05;
  if(tactic=="肋部渗透"&&(slot==3||slot==5))g*=1.05;
  if(tactic=="倒三角"&&(slot==0||slot==2))r*=1.05;
  if(tactic=="关门协防"&&(slot==6||slot==8))b*=1.05;
  if(tactic=="出击型门将"&&slot==9)b*=1.05;
  return new Ability{P=p,R=r,G=g,B=b,K=p.Black+5,S=s};
 }
 static double Delta(string o,string a,string b,string c,string d){return (o==a||o==b)?.15:(o==c||o==d)?-.15:0;}
 public static double Score(Ability a){return a.R+a.G+a.B+a.S-a.K+5;}
 public static string[] Optimize(Game g,Club c,string tactic,IEnumerable<Player> pool=null){
  Player[] ps=(pool??g.Squad(c.Id)).Where(p=>p.Injury==0).ToArray();if(ps.Length<10)throw new Exception(c.Name+"可用球员不足10人。");
  string[] best=null;double bestScore=double.NegativeInfinity;
  var stars=tactic=="巨星战术"?ps.OrderByDescending(p=>p.Value).ToArray():new Player[]{null};
  foreach(var star in stars){Player[] candidates=star==null?ps:ps.Where(p=>p.Value<=star.Value).ToArray();if(candidates.Length<10)continue;
   double[,] scores=new double[10,candidates.Length];
   for(int s=0;s<10;s++)for(int j=0;j<candidates.Length;j++){Player p=candidates[j];scores[s,j]=Score(Weight(p,s,c.Philosophy,tactic,"",Rules.Round((p.Favor+7)/2.0),false,p==star));if(s==9&&ps.Any(x=>x.Type=="门将")&&p.Type!="门将")scores[s,j]-=1e8;if(star!=null&&p==star)scores[s,j]+=1e6;if(tactic=="角球战术")scores[s,j]+=.015*Weight(p,s,c.Philosophy,tactic,"",Rules.Round((p.Favor+7)/2.0),false,false).R;}
   int[] chosen=Assign(scores);Player[] lineup=chosen.Select(j=>candidates[j]).ToArray();if(star!=null&&!lineup.Contains(star))continue;
   double value=LineupScore(g,c,lineup.Select(p=>p.Id).ToArray(),tactic);
   if(value>bestScore){bestScore=value;best=lineup.Select(p=>p.Id).ToArray();}
  }return best;
 }
 // Hungarian assignment: ten position rows, N player columns; no duplicate players.
 static int[] Assign(double[,] score){int n=score.GetLength(0),m=score.GetLength(1);double[] u=new double[n+1],v=new double[m+1];int[] p=new int[m+1],way=new int[m+1];
  for(int i=1;i<=n;i++){p[0]=i;int j0=0;double[] min=Enumerable.Repeat(double.PositiveInfinity,m+1).ToArray();bool[] used=new bool[m+1];do{used[j0]=true;int i0=p[j0],j1=0;double d=double.PositiveInfinity;for(int j=1;j<=m;j++)if(!used[j]){double cur=-score[i0-1,j-1]-u[i0]-v[j];if(cur<min[j]){min[j]=cur;way[j]=j0;}if(min[j]<d){d=min[j];j1=j;}}for(int j=0;j<=m;j++)if(used[j]){u[p[j]]+=d;v[j]-=d;}else min[j]-=d;j0=j1;}while(p[j0]!=0);do{int j1=way[j0];p[j0]=p[j1];j0=j1;}while(j0!=0);}
  int[] result=new int[n];for(int j=1;j<=m;j++)if(p[j]>0)result[p[j]-1]=j-1;return result;
 }
 public static double LineupScore(Game g,Club c,string[] ids,string tactic){var ps=ids.Select(id=>g.Players.First(p=>p.Id==id)).ToArray();double max=ps.Max(p=>p.Value);var rows=ps.Select((p,s)=>Weight(p,s,c.Philosophy,tactic,"",Rules.Round((p.Favor+7)/2.0),false,p.Value==max&&Array.IndexOf(ps,p)==Array.FindIndex(ps,x=>x.Value==max))).ToArray();double total=rows.Sum(a=>Score(a));if(tactic=="角球战术")total+=.015*(rows.Sum(a=>a.R)+rows.Take(9).Max(a=>a.S));return total;}
 public static void ValidateLineup(Game g,Club c,string[] ids,string tactic){if(ids==null||ids.Length!=10||ids.Distinct().Count()!=10||ids.Any(id=>!g.Squad(c.Id).Any(p=>p.Id==id&&p.Injury==0)))throw new Exception("阵容必须是10名不同且无伤停的本队球员："+c.Name);if(tactic!="无战术"&&!c.Tactics.Contains(tactic))throw new Exception("战术未解锁："+tactic);if(g.Squad(c.Id).Any(p=>p.Injury==0&&p.Type=="门将")&&g.Players.First(p=>p.Id==ids[9]).Type!="门将")throw new Exception("有健康门将时门将位置必须优先使用门将。");}
 public static void Auto(Game g,Club c,bool allowBus=false){string[] best=null;string tactic="无战术";double value=double.NegativeInfinity;foreach(string t in new[]{"无战术"}.Concat(c.Tactics).Where(t=>t!="战术犯规"&&(allowBus||t!="摆大巴"))){string[] ids=Optimize(g,c,t);double s=LineupScore(g,c,ids,t);if(s>value){value=s;best=ids;tactic=t;}}c.Lineup=best;c.Tactic=tactic;}
 public static string[] Rotation(Game g,Club c){var pool=g.Squad(c.Id).Where(p=>p.Injury==0).ToList();if(pool.Count<10)throw new Exception("轮换阵容可用人数不足。");var result=new string[10];foreach(int slot in new[]{9,0,1,2,3,4,5,6,7,8}){string type=Rules.Expected("无战术",slot);int foot=slot%3==0?1:slot%3==1?0:2;var exact=pool.Where(p=>p.Type==type&&(slot==9||p.Foot==foot)).ToList();var same=pool.Where(p=>p.Type==type).ToList();var candidates=exact.Count>0?exact:same.Count>0?same:pool;Player chosen=candidates.OrderBy(p=>p.Appearances).ThenByDescending(p=>Score(Weight(p,slot,c.Philosophy,"无战术","",Rules.Round((p.Favor+7)/2.0),false,false))).First();result[slot]=chosen.Id;pool.Remove(chosen);}return result;}
 public static void Begin(Game g,Fixture f){Club a=g.Club(f.Home),b=g.Club(f.Away);ValidateLineup(g,a,a.Lineup,a.Tactic);ValidateLineup(g,b,b.Lineup,b.Tactic);g.Live=new LiveMatch{FixtureId=f.Id,HomeLineup=(string[])a.Lineup.Clone(),AwayLineup=(string[])b.Lineup.Clone(),HomeTactic=f.Neutral&&a.Tactic=="战术犯规"?"无战术":a.Tactic,AwayTactic=f.Neutral&&b.Tactic=="战术犯规"?"无战术":b.Tactic};var m=g.Live;m.Events.AddRange(f.Commentary);m.UsedHome.AddRange(a.Lineup);m.UsedAway.AddRange(b.Lineup);m.Eligible.AddRange(g.Players.Where(p=>(p.ClubId==a.Id||p.ClubId==b.Id)&&p.Injury==0).Select(p=>p.Id));m.Events.Add(a.Name+" 对 "+b.Name+"，比赛开始。");}
 static Ability[] MatchRows(Game g,string[] ids,string tactic,Club c,Club foe,bool home,RandomSource rng){var m=g.Live;double max=ids.Where(id=>!String.IsNullOrEmpty(id)).Select(id=>g.Players.First(p=>p.Id==id).Value).DefaultIfEmpty(0).Max();string star=ids.FirstOrDefault(id=>!String.IsNullOrEmpty(id)&&g.Players.First(p=>p.Id==id).Value==max);return ids.Select((id,s)=>{Player p=g.Players.FirstOrDefault(x=>x.Id==id);if(p==null)return new Ability();if(!m.Status.ContainsKey(id))m.Status[id]=Math.Min(7,p.Favor)+rng.Next(Math.Abs(p.Favor-7)+1);return Weight(p,s,c.Philosophy,tactic,foe.Philosophy,m.Status[id],home,id==star);}).ToArray();}
 static void Ball(Game g,string target,string kind,string text){if(g.Live.BallEvents==null)g.Live.BallEvents=new List<BallEvent>();g.Live.BallEvents.Add(new BallEvent{Target=target,Kind=kind,Text=text,Round=g.Live.Round});}
 static void Bump(Game g,Player p,int metric){if(p==null)return;if(!g.Live.Stats.ContainsKey(p.Id))g.Live.Stats[p.Id]=new double[6];g.Live.Stats[p.Id][metric]++;}
 public static int Lane(int slot){return slot==9?1:slot%3;}
 public static double LaneRed(Ability[] rows,int lane){return rows.Where((a,i)=>Lane(i)==lane).Sum(a=>a.R);}
 public static double LaneBlue(Ability[] rows,int lane){return rows.Take(9).Where((a,i)=>Lane(i)==lane).Sum(a=>a.B)+rows[9].B/3.0;}
 static int LanePick(Ability[] rows,int lane,bool defending,RandomSource rng){var slots=Enumerable.Range(0,10).Where(i=>defending?(i==9||Lane(i)==lane):Lane(i)==lane).ToArray();return slots[rng.Weighted(slots.Select(i=>defending?rows[i].B*(i==9?1.0/3:1):rows[i].R).ToArray())];}
 static int Pick(Ability[] a,Func<Ability,double> f,RandomSource rng){return rng.Weighted(a.Select(f).ToArray());}
 static int Shooter(Ability[] a,double attack,double defend,RandomSource rng){int[] order=Enumerable.Range(0,9).OrderByDescending(i=>a[i].S).ToArray();double sigma=Math.Pow(defend/Math.Max(attack,1e-5),2)*3;int index=Math.Min(8,(int)Math.Abs(Math.Max(-8.999,Math.Min(8.999,rng.Normal()*sigma))));return order[index];}
 static double Rate(double s){return Rules.Clamp(.6*Math.Log(1+s/40),.02,.85);}
 public static void Step(Game g){var m=g.Live;if(m==null||m.Round>=45)return;var f=g.Fixtures.First(x=>x.Id==m.FixtureId);var rng=new RandomSource(g);Club ca=g.Club(f.Home),cb=g.Club(f.Away);
  MatchMinutes.Ensure(g,m);foreach(string id in m.HomeLineup.Concat(m.AwayLineup)){if(!m.Minutes.ContainsKey(id))m.Minutes[id]=0;m.Minutes[id]+=2;}if(m.Review==null)m.Review=new MatchReview();
  Ability[][] team={MatchRows(g,m.HomeLineup,m.HomeTactic,ca,cb,!f.Neutral,rng),MatchRows(g,m.AwayLineup,m.AwayTactic,cb,ca,false,rng)};
  for(int i=0;i<2;i++)if(m.PendingCorner[i]){m.Review.Corners[i]++;foreach(var p in team[i])p.R*=1.15;team[i][Enumerable.Range(0,9).OrderByDescending(j=>team[i][j].S).First()].S*=1.15;m.Events.Add("角球战术触发。");}m.PendingCorner=new bool[2];m.Round++;
  int atk=rng.Weighted(new[]{team[0].Sum(x=>x.G),team[1].Sum(x=>x.G)}),def=1-atk;var a=team[atk];var b=team[def];int owner=Pick(a,x=>x.G,rng);m.Review.Possessions[atk*3+Lane(owner)]++;Bump(g,a[owner].P,3);Ball(g,a[owner].P.Id,"control",a[owner].P.Name+"获得球权");
  if(rng.NextDouble()<a[owner].K/100){string opposing=def==0?m.HomeTactic:m.AwayTactic;if(opposing=="角球战术")m.PendingCorner[def]=true;m.Events.Add((m.Round*2)+"′ 开球／停球失误。");Ball(g,"center","reset","开球失误，重新争夺球权");return;}
  int lane=Lane(owner);m.Events.Add((m.Round*2)+"′ "+new[]{"左","中","右"}[lane]+"路推进。");
  for(int phase=0;phase<8;phase++){double red=LaneRed(a,lane),blue=LaneBlue(b,2-lane);m.Review.Duels[atk*3+lane]++;if(rng.NextDouble()<red/Math.Max(1e-9,red+blue)){
    m.Review.DuelsWon[atk*3+lane]++;int pass=LanePick(a,lane,false,rng);Ball(g,a[pass].P.Id,"pass",a[pass].P.Name+"接球");if(rng.NextDouble()<a[pass].K/100){Ball(g,"center","reset","传球失误");continue;}Bump(g,a[pass].P,2);int shot=Shooter(a,red,blue,rng);Ball(g,a[shot].P.Id,"pass",a[pass].P.Name+"送出妙传 → "+a[shot].P.Name+"准备射门");
    m.Review.Shots[atk*3+lane]++;if(rng.NextDouble()<Rate(a[shot].S)){m.Review.OnTarget[atk*3+lane]++;
     string dt=def==0?m.HomeTactic:m.AwayTactic;if(dt=="战术犯规"&&!m.FoulUsed[def]){m.FoulUsed[def]=true;m.Review.Fouls[def]++;int defender=rng.NextDouble()<.75?2-shot%3:rng.Next(3);Player victim=b[defender+6].P;if(victim!=null)m.Suspensions[victim.Id]=4;m.Events.Add((m.Round*2)+"′ 战术犯规阻止射正；"+(victim==null?"后卫":victim.Name)+"赛后停赛4周。");Ball(g,victim==null?"center":victim.Id,"foul","战术犯规阻止射正");return;}
     if(rng.NextDouble()>Rate(b[9].S))Goal(g,atk,a[shot].P,a[pass].P);
     else {Ball(g,b[9].P.Id,"save",b[9].P.Name+"扑救成功");Bump(g,b[9].P,5);if(rng.NextDouble()<b[9].K/100){int rebound=Shooter(a,red,blue,rng);Ball(g,a[rebound].P.Id,"pass",a[rebound].P.Name+"补射");m.Review.Shots[atk*3+lane]++;if(rng.NextDouble()<Rate(a[rebound].S)){m.Review.OnTarget[atk*3+lane]++;if(rng.NextDouble()>Rate(b[9].S))Goal(g,atk,a[rebound].P,null);else {Ball(g,b[9].P.Id,"save",b[9].P.Name+"扑出补射");Bump(g,b[9].P,5);}}else Ball(g,"center","miss","补射偏出，球回中圈");}}
    }else Ball(g,"center","miss","射门偏出，球回中圈");return;
   }else{int stop=LanePick(b,2-lane,true,rng);if(rng.NextDouble()<b[stop].K/100)continue;Bump(g,b[stop].P,4);Ball(g,b[stop].P.Id,"tackle",b[stop].P.Name+"抢断成功");return;}}
 }
 static void Goal(Game g,int side,Player scorer,Player assist){var m=g.Live;Ball(g,side==0?"goalAway":"goalHome","goal",(scorer==null?"球员":scorer.Name)+"射门得分！");if(side==0)m.HomeGoals++;else m.AwayGoals++;Bump(g,scorer,0);if(assist!=null&&scorer!=null&&assist.Id!=scorer.Id)Bump(g,assist,1);m.Events.Add((m.Round*2)+"′ ⚽ "+(scorer==null?"乌龙":scorer.Name)+" 进球！ "+m.HomeGoals+":"+m.AwayGoals);}
 public static void Substitute(Game g,bool home,int slot,string newId){var m=g.Live;if(m==null||m.Round>=45)throw new Exception("当前没有进行中的比赛。");var f=g.Fixtures.First(x=>x.Id==m.FixtureId);string clubId=home?f.Home:f.Away;var lineup=home?m.HomeLineup:m.AwayLineup;var used=home?m.UsedHome:m.UsedAway;int count=home?m.HomeSubs:m.AwaySubs;if(count>=5)throw new Exception("本场已用完5次换人。");Player p=g.Players.FirstOrDefault(x=>x.Id==newId&&x.ClubId==clubId&&x.Injury==0);if(p==null||used.Contains(newId)||lineup.Contains(newId))throw new Exception("换入球员无效，换下者不得重新上场。");MatchMinutes.Ensure(g,m);m.Off.Add(lineup[slot]);lineup[slot]=newId;used.Add(newId);if(home)m.HomeSubs++;else m.AwaySubs++;m.Events.Add((m.Round*2)+"′ 换人："+p.Name+"进入 "+Rules.Slots[slot]);}
 public static void Finish(Game g){var m=g.Live;if(m==null||m.Round<45)throw new Exception("比赛尚未结束。");var f=g.Fixtures.First(x=>x.Id==m.FixtureId);var rng=new RandomSource(g);MatchMinutes.Ensure(g,m);f.Review=m.Review;f.HomeGoals=m.HomeGoals;f.AwayGoals=m.AwayGoals;f.Commentary=new List<string>(m.Events);bool qualifier=f.Competition=="欧冠资格赛";
  foreach(string id in m.Eligible)if(!qualifier)g.Players.First(p=>p.Id==id).Available++;
  foreach(string id in m.UsedHome.Concat(m.UsedAway).Distinct()){Player p=g.Players.First(x=>x.Id==id);int minutes=m.Minutes.ContainsKey(id)?m.Minutes[id]:0;if(minutes<=0)continue;int fatigue=MatchMinutes.Fatigue(minutes);p.Fatigue+=fatigue;m.Events.Add(p.Name+"出场"+minutes+"分钟，疲劳+"+fatigue+"%。");if(!qualifier){p.Appearances++;double[] v;if(m.Stats.TryGetValue(id,out v))for(int k=0;k<6;k++)p.Stat(f.Competition)[k]+=v[k];}if(rng.NextDouble()<.025){p.Injury=Math.Max(p.Injury,rng.NextDouble()<.5?2:rng.Next(3)+3);string injury=p.Name+"伤病：赛后伤停"+p.Injury+"周。";g.Log(injury);m.Events.Add(injury);}}
  foreach(var x in m.Suspensions){var suspended=g.Players.First(p=>p.Id==x.Key);suspended.Injury=Math.Max(4,suspended.Injury);m.Events.Add(suspended.Name+"战术犯规停赛：赛后剩余伤停"+suspended.Injury+"周。");}
  m.UsedHome.RemoveAll(id=>!m.Minutes.ContainsKey(id)||m.Minutes[id]<=0);m.UsedAway.RemoveAll(id=>!m.Minutes.ContainsKey(id)||m.Minutes[id]<=0);
  if(!qualifier)SeasonFlow.Reward(g,f,m);else m.Events.Add("欧冠资格赛：不结算升级点及大胜／惨败奖惩。");
  f.Commentary=new List<string>(m.Events);
  g.Log(g.Club(f.Home).Name+" "+f.HomeGoals+":"+f.AwayGoals+" "+g.Club(f.Away).Name+"（"+f.Competition+"）");g.Live=null;
 }
}
}
