using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
namespace Touchline {
public static class CareerTests {
 static void Check(bool ok,string message){if(!ok)throw new Exception("Career/lane test: "+message);}
 public static bool Run(){
  var g=GameFactory.New(19218,false,"球星迭起");Check(g.Players.All(p=>p.Age>=18&&p.Age<=34),"initial ages");Check(g.Relations.Count==435,"relationship count");
  foreach(var a in g.Clubs)foreach(var b in g.Clubs.Where(c=>c!=a)){int r=WorldIdentity.Relation(g,a.Id,b.Id);Check(r==WorldIdentity.Relation(g,b.Id,a.Id),"symmetry");Check(a.League==b.League?r>=-99&&r<=100:r>=1&&r<=200,"relationship range");}
  Check(g.Clubs.All(c=>c.Personality!="银河战舰"||(c.Funds>=5000&&c.Commercial>=25)),"plausible personalities");string state=g.Rng;WorldIdentity.Ensure(g);Check(state==g.Rng,"migration idempotent");
  var old=Storage.Decode<Game>("{}");Check(old.CareerMode=="长期培养","legacy default");
  var rows=Enumerable.Range(0,10).Select(i=>new Ability{R=i+1,B=3*(i+1)}).ToArray();Check(Football.Lane(9)==1,"keeper possession middle");Check(Football.LaneRed(rows,0)==12&&Football.LaneRed(rows,1)==25&&Football.LaneRed(rows,2)==18,"lane red");Check(Enumerable.Range(0,3).Sum(i=>Football.LaneBlue(rows,i))==rows.Sum(a=>a.B),"keeper blue split conserves total");Check(Football.LaneBlue(rows,0)==46&&Football.LaneBlue(rows,2)==64,"defending mirrored lane inputs");
  Check(CareerCycle.DeclineProbability(30)==0&&Math.Abs(CareerCycle.DeclineProbability(31)-.1)<1e-9&&CareerCycle.DeclineProbability(40)==1&&CareerCycle.DeclineProbability(60)==1,"probability boundaries");
  var rng=new RandomSource(g);for(int n=1;n<=50;n++)foreach(string type in Rules.Types){var values=CareerCycle.GenerateAbilities(rng,type,n,7);Check(Math.Abs(values.Sum()-7-n)<.001&&values.All(v=>v>=0),"rookie exact target value");}
  var young=g.Players[0];young.Age=24;double before=young.Value,black=young.Black;var adult=g.Players[1];adult.Age=25;double adultValue=adult.Value;var senior=g.Players[2];senior.Age=40;senior.DeclineCount=3;
  int prior=g.Players.Count;CareerCycle.Settle(g);Check(young.Age==25&&young.Value>=before&&young.Black==black,"youth growth without black");Check(adult.Age==26&&adult.Value==adultValue,"adult stable");Check(senior.DeclineCount==4&&senior.Age==41,"guaranteed age40 decline");Check(g.Players.Where(p=>p.Id.StartsWith("R1_")).All(p=>p.Age==18&&p.Value>=1&&p.Value<=50&&p.ClubId==""&&p.Injury==0&&p.Fatigue==0),"rookies free and healthy");string once=Storage.Encode(g);CareerCycle.Settle(g);Check(once==Storage.Encode(g),"annual settlement once");
  g.Phase="续约";var retired=g.Players.First(p=>p.ClubId!=""&&g.Squad(p.ClubId).Count()>10);CareerCycle.Retire(g,retired);Check(!g.Players.Contains(retired)&&g.RetiredPlayers.Contains(retired)&&g.Clubs.All(c=>!c.Lineup.Contains(retired.Id)),"retirement archive and lineup cleanup");Rules.Validate(g);
  var longGame=GameFactory.New(918);string unchanged=Storage.Encode(longGame);CareerCycle.Settle(longGame);Check(unchanged==Storage.Encode(longGame),"long career mode unchanged");
  var laneGame=GameFactory.New(713);var fixture=laneGame.Fixtures.First();var home=laneGame.Club(fixture.Home);var away=laneGame.Club(fixture.Away);home.Philosophy=away.Philosophy="古典足球";home.Tactic=away.Tactic="无战术";
  foreach(var p in laneGame.Players){p.Red=10;p.Green=0;p.Blue=0;p.Black=0;p.Shot=0;p.Favor=7;}
  var owner=laneGame.Players.First(p=>p.Id==home.Lineup[0]);owner.Green=100;var shooter=laneGame.Players.First(p=>p.Id==home.Lineup[1]);shooter.Type="前锋";shooter.Shot=100;
  Football.Begin(laneGame,fixture);while(laneGame.Live.Round<45)Football.Step(laneGame);
  var passes=laneGame.Live.BallEvents.Where(e=>e.Kind=="pass"&&e.Text.EndsWith("接球")).ToArray();Check(passes.Length>0&&passes.All(e=>Football.Lane(Array.IndexOf(home.Lineup,e.Target))==0),"actual passes restricted to possession left lane");Check(laneGame.Live.BallEvents.Any(e=>e.Target==shooter.Id&&e.Text.Contains("准备射门")),"cross-lane center shooter allowed");
  var history=new List<object>();for(int year=2;year<=50;year++){g.Season=year;CareerCycle.Settle(g);Rules.Validate(g);if(year%10==0)history.Add(new{Year=year,Active=g.Players.Count,Retired=g.RetiredPlayers.Count,Oldest=g.Players.Max(p=>p.Age),MeanAge=g.Players.Average(p=>p.Age)});}
  File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"test-results","career-50-years.json"),Storage.Encode(history));return true;
 }
}
}
