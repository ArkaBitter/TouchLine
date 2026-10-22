using System;
using System.Linq;
using System.Collections.Generic;
namespace Touchline {
public static class WorldIdentity {
 public static readonly Dictionary<string,string> Policies=new Dictionary<string,string>{
 {"收益经营","以可持续盈利为首要目标。优先低价年轻球员，给予出场和训练，溢价出售成熟球员；不为了买人透支工资。"},
 {"竞技至上","以取得最高竞技成绩为首要目标。宁可使用可支配预算补强短板，不为账面利润出售不可替代的主力；仍必须满足工资约束。"},
 {"欧冠雄心","欧冠及资格赛优先级最高，关键欧战前在其他赛事合理轮换。国内联赛仍须争取下季欧战资格，不为欧冠放弃基本生存。"},
 {"联赛立足","联赛稳定排名和联赛冠军优先，欧战适度轮换，但淘汰赛及冠军机会明显时仍应认真竞争。"},
 {"忠诚传统","珍视忠诚度高和长期出场的球员，不轻易挂牌核心；低能力或持续衰退且球队无法负担时仍可理性告别。"},
 {"银河战舰","追求高能力明星和冠军，优先引进市场顶尖球员，保留关键位置替补；只在付费后仍能承担全队工资时购买。"},
 {"青训建设","优先24岁及以下球员，给予训练和轮换，耐心培养；长期培养模式没有年龄奖励时重视低价、有明确属性短板的球员。"},
 {"稳健生存","保留充足工资资金，避免高额赌注和大规模解约，优先免费与低价补缺；成绩目标是稳定发展。"},
 {"杯赛猎手","优先杯赛淘汰赛和争冠机会，在非关键赛事轮换，但必须维持国内联赛基本竞争力。"},
 {"体系建队","优先符合本队理念、惯用脚和缺人位置的球员，不仅按身价采购；保留多位置轮换和稳定核心。"}};
 static string Key(string a,string b){return String.CompareOrdinal(a,b)<0?a+"|"+b:b+"|"+a;}
 public static int Relation(Game g,string a,string b){if(a==b)return 200;if(String.IsNullOrEmpty(a)||String.IsNullOrEmpty(b))return 0;int value;return g.Relations.TryGetValue(Key(a,b),out value)?value:0;}
 public static void Ensure(Game g){
  if(String.IsNullOrEmpty(g.CareerMode))g.CareerMode="长期培养";
  if(g.Relations==null)g.Relations=new Dictionary<string,int>();var rng=new RandomSource(g);
  for(int i=0;i<g.Clubs.Count;i++)for(int j=i+1;j<g.Clubs.Count;j++){string key=Key(g.Clubs[i].Id,g.Clubs[j].Id);if(!g.Relations.ContainsKey(key))g.Relations[key]=rng.Next(200)+1-(g.Clubs[i].League==g.Clubs[j].League?100:0);}
  foreach(var c in g.Clubs){if(!Policies.ContainsKey(c.Personality??"")){var eligible=Policies.Keys.Where(k=>k!="银河战舰"||(c.Commercial>=25&&c.Funds>=5000)).ToArray();c.Personality=eligible[rng.Next(eligible.Length)];}c.AgentIdentity=Policies[c.Personality];}
 }
 public static double Priority(Game g,Club c,Player p){
  double v=p.Value,ageBonus=g.CareerMode=="球星迭起"&&p.Age<=24?40:0;
  if(c.Personality=="青训建设")return v+ageBonus;
  if(c.Personality=="收益经营"||c.Personality=="稳健生存")return v/(1+Transfers.Fee(g,p,false)/100)+ageBonus;
  if(c.Personality=="体系建队")return v+(g.Squad(c.Id).Count(x=>x.Type==p.Type&&(p.Type=="门将"||x.Foot==p.Foot))<2?60:0);
  return v;
 }
 public static bool AllowRotation(Club c,Fixture f){return (c.Personality=="欧冠雄心"&&f.Competition!="欧冠"&&f.Competition!="欧冠资格赛")||(c.Personality=="联赛立足"&&f.Competition!="联赛")||(c.Personality=="杯赛猎手"&&f.Competition=="联赛")||c.Personality=="青训建设";}
}
}
