using System;
using System.Linq;
namespace Touchline {
public static class CareerCycle {
 public static double DeclineProbability(int age){return Rules.Clamp(.1*(age-30),0,1);}
 public static double[] GenerateAbilities(RandomSource rng,string type,double target,int black){
  if(target<=0)return new double[4];double x=rng.NextDouble(),y=rng.NextDouble(),lo=Math.Min(x,y),hi=Math.Max(x,y);
  double[] coef=type=="前锋"?new[]{.75,.2,.1}:type=="中场"?new[]{.3,.45,.3}:type=="后卫"?new[]{.1,.2,.75}:new[]{.05,.1,.9};
  double[] colors={lo*coef[0],(hi-lo)*coef[1],(1-hi)*coef[2]};double shot=Math.Min(target,(rng.Next(Math.Max(1,(int)Math.Ceiling(target)))+1)*(type=="前锋"||type=="门将"?.6:type=="中场"?.3:.1));
  double pool=target-shot+black,sum=colors.Sum();var result=new[]{Math.Round(pool*colors[0]/sum,2),Math.Round(pool*colors[1]/sum,2),Math.Round(pool*colors[2]/sum,2),Math.Round(shot,2)};
  int largest=Array.IndexOf(result.Take(3).ToArray(),result.Take(3).Max());result[largest]=Math.Round(result[largest]+Math.Round(target,2)+black-result.Sum(),2);return result;
 }
 public static void Decline(Player p,double percent){double f=1-Rules.Clamp(percent,0,100)/100;p.Red=Math.Round(p.Red*f,2);p.Green=Math.Round(p.Green*f,2);p.Blue=Math.Round(p.Blue*f,2);p.Shot=Math.Round(p.Shot*f,2);}
 public static void Retire(Game g,Player p,bool automatic=false){
  if(g.CareerMode!="球星迭起")throw new Exception("长期培养模式不启用退役。");if(!automatic&&(g.Phase!="续约"||g.Live!=null))throw new Exception("请在续约阶段办理退役。");
  if(!g.Players.Contains(p))return;
  if(!automatic&&p.ClubId!=""&&g.Squad(p.ClubId).Count()<=10)throw new Exception("至少保留10名球员，不能主动退役该球员。");
  foreach(var c in g.Clubs)c.Lineup=c.Lineup.Where(id=>id!=p.Id).ToArray();foreach(var offer in g.Offers.Where(o=>o.PlayerId==p.Id&&o.Status=="待回应"))offer.Status="失效：球员已退役";
  p.RetiredSeason=g.Season;g.Players.Remove(p);g.RetiredPlayers.Add(p);g.Log(p.Name+"（"+p.Age+"岁）"+(automatic?"身价归零，自动退役。":"办理退役。"));
 }
 public static void Settle(Game g){
  if(g.CareerMode!="球星迭起"||g.CareerSettled.Contains(g.Season))return;
  var rng=new RandomSource(g);
  foreach(var p in g.Players.ToArray()){
   double before=p.Value;
   if(p.Age<=24){double growth=Math.Max(0,10+5*rng.Normal());var v=GenerateAbilities(rng,p.Type,growth,0);p.Red+=v[0];p.Green+=v[1];p.Blue+=v[2];p.Shot=Math.Round(p.Shot+v[3],2);g.Log(p.Name+"青年成长：身价 "+before.ToString("F2")+" → "+p.Value.ToString("F2"));}
   else if(rng.NextDouble()<DeclineProbability(p.Age)){p.DeclineCount++;double percent=Rules.Clamp((double)p.DeclineCount*p.DeclineCount+3*rng.Normal(),0,100);Decline(p,percent);g.Log(p.Name+"第"+p.DeclineCount+"次年龄衰退 "+percent.ToString("F2")+"%：身价 "+before.ToString("F2")+" → "+p.Value.ToString("F2"));}
   p.Age++;if(p.Value<=0)Retire(g,p,true);
  }
  int count=Math.Max(0,Rules.Round(45+9*rng.Normal()));var names=new System.Collections.Generic.HashSet<string>(g.Players.Concat(g.RetiredPlayers).Select(p=>p.Name));
  const string surnames="林陈周许沈江陆叶顾秦苏夏萧白温程方宋谢唐韩尹钟易杜罗黎傅乔严";const string words="星云川远泽清雨森曜景辰明安羽凌洛启沐航晏砚驰风宁溪舟朗深柏霖";
  for(int i=0;i<count;i++){int role=rng.Next(10);string type=role==0?"门将":role<4?"后卫":role<7?"中场":"前锋";string name;do{name=""+surnames[rng.Next(surnames.Length)]+words[rng.Next(words.Length)]+words[rng.Next(words.Length)];}while(!names.Add(name));int target=rng.Next(50)+1,black=Rules.Round(10*rng.NextDouble());var a=GenerateAbilities(rng,type,target,black);
   string id="R"+g.Season+"_"+i;while(g.Players.Concat(g.RetiredPlayers).Any(p=>p.Id==id))id+="_";
   var rookie=new Player{Id=id,Name=name,Age=18,Type=type,Red=a[0],Green=a[1],Blue=a[2],Shot=a[3],Black=black,Foot=rng.Next(3),Favor=rng.TwoDice(),Loyalty=rng.TwoDice(),Listed=true};g.Players.Add(rookie);if(rookie.Value<=0)Retire(g,rookie,true);
  }
  g.CareerSettled.Add(g.Season);g.Log("年度年龄结算完成：本季生成"+count+"名18岁自由球员新秀；先按旧年龄成长／衰退，再年龄+1。");
 }
}
}
