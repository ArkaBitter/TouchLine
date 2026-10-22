using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Web.Script.Serialization;

namespace Touchline {
public class Player {
 public string Id="",Name="",ClubId="",Type="中场",PrimarySlot=""; public double Red,Green,Blue,Black,Shot;
 public int Age,DeclineCount,RetiredSeason;
 public int Foot,Favor=7,Loyalty=7,Injury,Fatigue,Appearances,Available;
 public bool AvailabilityKnown=true,Listed,Renew=true; public Dictionary<string,double[]> Stats=new Dictionary<string,double[]>();
 public double Value { get { return Math.Max(0,Math.Round(Red+Green+Blue+Shot-Black,2)); } }
 public double[] Stat(string comp) { if(!Stats.ContainsKey(comp))Stats[comp]=new double[6];return Stats[comp]; }
}
public class Club {
 public string SquadTemplateKey="";public List<SquadTemplate> SquadTemplates=new List<SquadTemplate>();public int SquadTemplateBuilds;
 public string Id="",Name="",Philosophy="古典足球",Tactic="无战术",AgentIdentity="",AgentGoal="";
 public string Personality="";
 public string[] ImportantFixtures=new string[0];
 public int League,Honor,Commercial=20,Upgrade; public double Funds;
 public string[] Tactics=new string[0],Lineup=new string[0]; public List<string> Memory=new List<string>();
}
public class Fixture {
 public string Id="",Competition="联赛",Home="",Away="",Group="",Stage="小组",Tie="";
 public int Week,Leg,HomeGoals=-1,AwayGoals=-1;public bool Neutral;
 public MatchReview Review; public List<OpponentDecisionRecord> Decisions=new List<OpponentDecisionRecord>();
 public string Winner="";public List<string> Commentary=new List<string>();
 public bool Played {get{return HomeGoals>=0&&AwayGoals>=0;}}
}
public class Competition {
 public string Id="",Name="";public int League;public List<string[]> Groups=new List<string[]>();
 public string Champion="",Runner="";public string[] Semis=new string[0];public bool Completed;
}
public class TransferOffer {
 public string Id="",PlayerId="",BuyerId="",SellerId="",Status="待回应",Reason="";
 public double Fee;public int Season;public bool PlayerDriven;
}
public class AgentDecision {
 public string[] retire=new string[0];
 public int plan=-1,trainingPlan=-1,swapPlan=0;public List<PositionSwap> swaps=new List<PositionSwap>(); public Dictionary<string,string> responses;
 public string[] importantMatches=new string[0];
 public string goal="",reason="",tactic="无战术";public string[] lineup=new string[0],release=new string[0],listPlayers=new string[0];
 public List<AgentOffer> offers=new List<AgentOffer>();public string[] acceptOffers=new string[0],rejectOffers=new string[0];
 public List<TrainingDecision> training=new List<TrainingDecision>();
 public List<SubDecision> substitutions=new List<SubDecision>();
}
public class SubDecision {public int slot;public string playerId="";}
public class TrainingDecision {public string playerId="",attribute="";public int levels=1;}
public class AgentOffer { public string playerId="";public double fee; }
public class AgentConfig {
 public string Endpoint="http://localhost:11434/v1/chat/completions",Model="",ApiKey="";
 public int TimeoutSeconds=90;public bool Enabled;public string ProtectedKey="";
}
public class BallEvent { public string Target="",Kind="",Text=""; public int Round; }
public class MatchReview { public int[] Possessions=new int[6],Duels=new int[6],DuelsWon=new int[6],Shots=new int[6],OnTarget=new int[6],Corners=new int[2],Fouls=new int[2]; }
public class LiveMatch {
 public List<int> DecisionRounds=new List<int>();
 public Dictionary<string,int> Minutes=new Dictionary<string,int>();
 public MatchReview Review=new MatchReview();
 public List<BallEvent> BallEvents=new List<BallEvent>();
 public string FixtureId="",HomeTactic="无战术",AwayTactic="无战术";
 public string[] HomeLineup=new string[10],AwayLineup=new string[10];public int Round,HomeGoals,AwayGoals,HomeSubs,AwaySubs;
 public bool[] PendingCorner=new bool[2],FoulUsed=new bool[2];public bool HalfTimeDone;
 public List<string> UsedHome=new List<string>(),UsedAway=new List<string>(),Off=new List<string>(),Eligible=new List<string>();
 public Dictionary<string,int> Status=new Dictionary<string,int>(),Suspensions=new Dictionary<string,int>();
 public Dictionary<string,double[]> Stats=new Dictionary<string,double[]>();public List<string> Events=new List<string>();
}
public class Game {
 public string CareerMode="长期培养";public List<Player> RetiredPlayers=new List<Player>();public List<int> CareerSettled=new List<int>();public Dictionary<string,int> Relations=new Dictionary<string,int>();
 public int Schema=1,Season=1,Week=1;public string Name="新生赛季",PlayerClubId="",Phase="常规赛",Rng="202610180001",Origin="随机新游戏";
 public List<Player> Players=new List<Player>();public List<Club> Clubs=new List<Club>();
 public List<Fixture> Fixtures=new List<Fixture>();public List<Competition> Competitions=new List<Competition>();
 public List<TransferOffer> Offers=new List<TransferOffer>();public List<string> News=new List<string>();
 public Dictionary<string,int> LastRanks=new Dictionary<string,int>();public List<string> Champions=new List<string>(),Europa=new List<string>(),Qualifiers=new List<string>();
 public Dictionary<string,int> TieLots=new Dictionary<string,int>();
 public List<string> PlanningDone=new List<string>();
 public List<string> Settled=new List<string>();public Dictionary<string,double[]> SeasonReport=new Dictionary<string,double[]>();
 public bool FirstSeasonEurope=true;public Club Club(string id){return Clubs.First(c=>c.Id==id);}
 public LiveMatch Live;
 public IEnumerable<Player> Squad(string id){return Players.Where(p=>p.ClubId==id);}
 public void Log(string s){News.Insert(0,"S"+Season+" W"+Week+" · "+s);if(News.Count>3000)News.RemoveRange(3000,News.Count-3000);}
}
public class RandomSource {
 Game g;ulong state;public RandomSource(Game game){g=game;state=Convert.ToUInt64(g.Rng,16);if(state==0)state=88172645463325252UL;}
 public double NextDouble(){state^=state<<13;state^=state>>7;state^=state<<17;g.Rng=state.ToString("X16");return (state>>11)*(1.0/9007199254740992.0);}
 public int Next(int max){if(max<=0)throw new ArgumentException("随机上界必须为正。");return Math.Min(max-1,(int)(NextDouble()*max));}
 public int Dice(){return Next(6)+1;}public int TwoDice(){return Dice()+Dice();}
 public double Normal(){return Math.Sqrt(-2*Math.Log(Math.Max(1e-15,NextDouble())))*Math.Cos(2*Math.PI*NextDouble());}
 public T[] Shuffle<T>(IEnumerable<T> items){T[] a=items.ToArray();for(int i=a.Length-1;i>0;i--){int j=Next(i+1);T x=a[i];a[i]=a[j];a[j]=x;}return a;}
 public int Weighted(double[] a){double total=a.Sum();if(total<=0)return Next(a.Length);double x=NextDouble()*total;for(int i=0;i<a.Length;i++){x-=Math.Max(0,a[i]);if(x<0)return i;}return a.Length-1;}
}
public class SaveEnvelope {public int Version=1;public string Checksum="",Json="",SavedAt="";}
public static class Storage {
 public static readonly string Root=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"TouchlineManager");
 public static JavaScriptSerializer Json(){return new JavaScriptSerializer{MaxJsonLength=int.MaxValue,RecursionLimit=128};}
 public static string Encode(object o){return Json().Serialize(o);}public static T Decode<T>(string s){return Json().Deserialize<T>(s);}
 public static T Clone<T>(T o){return Decode<T>(Encode(o));}
 public static string Hash(string s){using(var h=SHA256.Create())return BitConverter.ToString(h.ComputeHash(Encoding.UTF8.GetBytes(s))).Replace("-","");}
 public static Game Commit(Game staged,string path){Game snapshot=Clone(staged);Save(snapshot,path);return snapshot;}
 public static void Save(Game g,string path){Rules.Validate(g);string s=Encode(g);var e=new SaveEnvelope{Json=s,Checksum=Hash(s),SavedAt=DateTime.UtcNow.ToString("o")};Atomic(path,Encode(e));}
 public static Game Load(string path){var e=Decode<SaveEnvelope>(File.ReadAllText(path,Encoding.UTF8));if(e.Version!=1||Hash(e.Json)!=e.Checksum)throw new Exception("存档校验失败，请读取 .bak 备份。");Game g=Decode<Game>(e.Json);WorldIdentity.Ensure(g);Rules.Validate(g);return g;}
 public static void Atomic(string path,string text){Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));string temp=path+".tmp";File.WriteAllText(temp,text,new UTF8Encoding(false));if(File.Exists(path))File.Replace(temp,path,path+".bak");else File.Move(temp,path);}
 public static AgentConfig LoadConfig(){try{var c=Decode<AgentConfig>(File.ReadAllText(Path.Combine(Root,"agent.json")));if(c.ProtectedKey!="")c.ApiKey=Encoding.UTF8.GetString(ProtectedData.Unprotect(Convert.FromBase64String(c.ProtectedKey),null,DataProtectionScope.CurrentUser));return c;}catch{return new AgentConfig();}}
 public static void SaveConfig(AgentConfig c){var s=Clone(c);s.ProtectedKey=c.ApiKey==""?"":Convert.ToBase64String(ProtectedData.Protect(Encoding.UTF8.GetBytes(c.ApiKey),null,DataProtectionScope.CurrentUser));s.ApiKey="";Atomic(Path.Combine(Root,"agent.json"),Encode(s));}
}
public static class Rules {
 public static string[] Philosophies={"古典足球","全攻全守","九锋阵","传控足球","链式防守","连续短传","低位防守","快速渗透","高位压迫","防守反击","过顶长传","边中结合"};
 public static string[] Tactics={"无战术","角球战术","中卫前提","影锋","双后腰","巨星战术","造越位","摆大巴","全场自由人","单后腰","进攻终结点","抢落点","肋部渗透","倒三角","关门协防","出击型门将","45度炸","三中卫","中场内收","弧顶内切","远射","边后卫内收","主攻左路","主攻右路","战术犯规"};
 public static string[] Slots={"fwd_left","fwd_mid","fwd_right","mid_left","mid_B2B","mid_right","def_left","def_mid","def_right","gkp"};
 public static string[] Types={"前锋","中场","后卫","门将"};
 public static double[] Status={.75,.80,.85,.90,.94,.97,1,1,1,1.03,1.06,1.10,1.15,1.20,1.25};
 public static int Round(double x){return (int)Math.Round(x,MidpointRounding.AwayFromZero);}
 public static double Clamp(double n,double lo,double hi){return Math.Min(hi,Math.Max(lo,n));}
 public static string Expected(string tactic,int slot){if(slot==9)return "门将";if(tactic=="中卫前提"&&slot==7)return "中场";if(tactic=="影锋"&&slot==4)return "前锋";if(tactic=="双后腰"&&slot==4)return "后卫";return Types[slot/3];}
 public static void Validate(Game g){
   if(g.CareerMode!="长期培养"&&g.CareerMode!="球星迭起")throw new Exception("未知游戏模式。");
   if(g.Players.Concat(g.RetiredPlayers).Select(p=>p.Id).Distinct().Count()!=g.Players.Count+g.RetiredPlayers.Count)throw new Exception("在役与退役球员ID重复。");
   if(g.CareerMode=="球星迭起"&&g.Players.Any(p=>p.Age<18||p.DeclineCount<0))throw new Exception("年龄或累计衰退次数无效。");
  if(g.Schema!=1||g.Clubs.Count!=30||g.Clubs.Select(c=>c.Id).Distinct().Count()!=30)throw new Exception("存档须包含30家不同俱乐部。");
  if(g.Clubs.GroupBy(c=>c.League).Count()!=3||g.Clubs.GroupBy(c=>c.League).Any(x=>x.Count()!=10))throw new Exception("每联赛必须10队。");
  if(g.Players.Select(p=>p.Id).Distinct().Count()!=g.Players.Count)throw new Exception("球员ID重复。");
  foreach(var c in g.Clubs){if(!Philosophies.Contains(c.Philosophy)||c.Tactics.Length<5||c.Tactics.Length>24||c.Tactics.Distinct().Count()!=c.Tactics.Length||c.Tactics.Any(t=>!Tactics.Contains(t)||t=="无战术"))throw new Exception("俱乐部战术配置无效："+c.Name);if(double.IsNaN(c.Funds)||double.IsInfinity(c.Funds))throw new Exception("资金无效。");}
  foreach(var p in g.Players){if((p.ClubId!=""&&!g.Clubs.Any(c=>c.Id==p.ClubId))||!Types.Contains(p.Type)||p.Favor<0||p.Favor>14||p.Loyalty<0||p.Loyalty>12||p.Foot<0||p.Foot>2||p.Injury<0||p.Fatigue<0||p.Appearances<0||p.Available<0||new[]{p.Red,p.Green,p.Blue,p.Black,p.Shot}.Any(x=>x<0||double.IsNaN(x)||double.IsInfinity(x)))throw new Exception("球员数据无效："+p.Name);}
  if(g.PlayerClubId!=""&&!g.Clubs.Any(c=>c.Id==g.PlayerClubId))throw new Exception("玩家俱乐部不存在。");
  if(g.Fixtures.Any(f=>!g.Clubs.Any(c=>c.Id==f.Home)||!g.Clubs.Any(c=>c.Id==f.Away)||f.Home==f.Away))throw new Exception("赛程球队无效。");
 }
}
}
