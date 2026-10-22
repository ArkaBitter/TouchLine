using System;
using System.Linq;
using System.Collections.Generic;

namespace Touchline {
public class AgentPlan {public string Tactic;public string[] Lineup;public double Score;}
public class AgentRequest {
 public List<LiveCoachPlan> LivePlans=new List<LiveCoachPlan>();
 public List<PositionSwap> SwapPlans=new List<PositionSwap>();
 public int ReplyTokens=350; public List<List<TrainingDecision>> TrainingPlans=new List<List<TrainingDecision>>(); public List<List<SubDecision>> SubPlans=new List<List<SubDecision>>();
 public Dictionary<string,string> OfferCodes=new Dictionary<string,string>();
 public string System,Prompt;public object Schema;public Dictionary<string,string> Codes;public List<AgentPlan> Plans=new List<AgentPlan>();
 public static Dictionary<string,object> D(params object[] args){var d=new Dictionary<string,object>();for(int i=0;i<args.Length;i+=2)d[(string)args[i]]=args[i+1];return d;}
 static object Strings(IEnumerable<string> ids,int maximum){return D("type","array","items",D("type","string","enum",ids.Any()?ids.ToArray():new[]{""}),"maxItems",Math.Max(0,maximum),"uniqueItems",true);}
 public static AgentRequest Build(Game g,Club c,string phase,Fixture f){WorldIdentity.Ensure(g);if(phase=="阶段规划")return StrategicPlanning.Request(g,c);var result=new AgentRequest();var squad=g.Squad(c.Id).OrderByDescending(p=>p.Value).ToArray();result.Codes=squad.Select((p,i)=>new{code="p"+(i+1),id=p.Id}).ToDictionary(x=>x.code,x=>x.id);
  var context=D("阶段",phase,"俱乐部",c.Name,"个性",c.AgentIdentity,"目标",c.AgentGoal,"资金",c.Funds,"季工资",squad.Sum(p=>p.Value),"升级点",c.Upgrade,"理念",c.Philosophy,"最近记忆",c.Memory.Skip(Math.Max(0,c.Memory.Count-4)).ToArray());
  var properties=D("reason",D("type","string","maxLength",45));var required=new List<string>{"reason"};
  if(phase=="赛前"){
   result.Plans=SquadTemplates.Plans(g,c,f);
   context["阵容策略"]=SquadTemplates.Important(f)?"默认主力阵容，按伤停与明显疲劳微调。":"二线阵容，按伤停、疲劳与低出场球员机会微调。";
   context["本周未来比赛"]=g.Fixtures.Where(x=>!x.Played&&x.Week==g.Week&&(x.Home==c.Id||x.Away==c.Id)&&x.Id!=f.Id).Select(x=>x.Competition+":"+g.Club(x.Home==c.Id?x.Away:x.Home).Name).ToArray();
   var other=g.Club(f.Home==c.Id?f.Away:f.Home);context["对手"]=D("名称",other.Name,"理念",other.Philosophy,"战术",other.Tactics,"总身价",g.Squad(other.Id).Sum(p=>p.Value));
   context["候选工具结果"]=result.Plans.Select((p,i)=>new{plan=i,tactic=p.Tactic,strength=p.Score,effect=RuleDescriptions.Tactic[p.Tactic],lineup=CoachingPolicy.PlayerMatch(g,f)?p.Lineup.Select(id=>g.Players.First(x=>x.Id==id).Name).ToArray():new string[0]}).ToArray();
   properties["plan"]=D("type","integer","enum",Enumerable.Range(0,result.Plans.Count).ToArray());required.Insert(0,"plan");
   result.System="你是足球俱乐部主教练。根据本队理念、对手与目标，从候选阵容工具结果中自主选择plan。不要机械地选最高分，需要考虑战术相克。只输出JSON：plan为编号，reason为一句中文。不要抄写球员资料。每场仅一种战术，reason不可声称叠加战术或捏造规则。";
  }else{
   context["本队球员"]=squad.Select((p,i)=>new{code="p"+(i+1),type=p.Type,foot=p.Foot,value=p.Value,age=g.CareerMode=="球星迭起"?p.Age:0,declines=p.DeclineCount,injury=p.Injury,favor=p.Favor,loyalty=p.Loyalty,apps=p.Appearances,fatigue=p.Fatigue,red=p.Red,green=p.Green,blue=p.Blue,shot=p.Shot,listed=p.Listed}).ToArray();
   var actionable=g.Offers.Where(o=>o.Status=="待回应"&&((!o.PlayerDriven&&o.SellerId==c.Id)||(o.PlayerDriven&&o.BuyerId==c.Id))).ToArray();
   var offerCodes=actionable.Select((o,i)=>new{code="q"+(i+1),offer=o}).ToArray();foreach(var q in offerCodes)result.OfferCodes[q.code]=q.offer.Id;
   result.ReplyTokens=Math.Min(1600,350+actionable.Length*12);
   context["需回应报价"]=offerCodes.Select(q=>new{id=q.code,player=g.Players.First(p=>p.Id==q.offer.PlayerId).Name,value=g.Players.First(p=>p.Id==q.offer.PlayerId).Value,fee=q.offer.Fee,buyer=g.Club(q.offer.BuyerId).Name,relationship=WorldIdentity.Relation(g,q.offer.BuyerId,q.offer.SellerId)}).ToArray();
   if(LiveCoaching.Phase(phase)){
    MatchMinutes.Ensure(g,g.Live);
    bool home=f.Home==c.Id;var used=home?g.Live.UsedHome:g.Live.UsedAway;int left=5-(home?g.Live.HomeSubs:g.Live.AwaySubs);var eligible=result.Codes.Where(x=>squad.Any(p=>p.Id==x.Value&&p.Injury==0)&&!used.Contains(x.Value)).Select(x=>x.Key).ToArray();
    context["当前首发"]= (home?g.Live.HomeLineup:g.Live.AwayLineup).Select((id,slot)=>new{slot=slot,code=result.Codes.First(x=>x.Value==id).Key,minutes=g.Live.Minutes.ContainsKey(id)?g.Live.Minutes[id]:0}).ToArray();context["场上"]=D("分钟",g.Live.Round*2,"主队进球",g.Live.HomeGoals,"客队进球",g.Live.AwayGoals,"本队主场",home,"目前战术",home?g.Live.HomeTactic:g.Live.AwayTactic,"可换入",eligible,"剩余换人",left);
    properties["tactic"]=D("type","string","enum",new[]{"无战术"}.Concat(c.Tactics).Where(t=>!f.Neutral||t!="战术犯规").ToArray());required.Insert(0,"tactic");
    result.SubPlans=HalfTimePlans.Build(g,c,f);result.SwapPlans=CoachingPolicy.PlayerMatch(g,f)?new List<PositionSwap>():LiveCoaching.Swaps(g,c,f);
    string currentTactic=home?g.Live.HomeTactic:g.Live.AwayTactic;var currentLineup=home?g.Live.HomeLineup:g.Live.AwayLineup;double baseScore=Football.LineupScore(g,c,currentLineup,currentTactic);
    context["合法换位方案"]=result.SwapPlans.Select((swap,i)=>new{swapPlan=i,slots=swap==null?new int[0]:new[]{swap.a,swap.b},delta=swap==null?0:Math.Round(Football.LineupScore(g,c,LiveCoaching.SwapPreview(g,home,swap.a,swap.b),currentTactic)-baseScore,1)}).ToArray();
    context["可用战术效果"]=new[]{"无战术"}.Concat(c.Tactics).Distinct().ToDictionary(t=>t,t=>RuleDescriptions.Tactic[t]);
    properties["swapPlan"]=D("type","integer","enum",Enumerable.Range(0,result.SwapPlans.Count).ToArray());required.Add("swapPlan");
    context["合法换人方案"]=result.SubPlans.Select((changes,i)=>new{plan=i,changes=changes.Select(s=>new{slot=s.slot,playerId=result.Codes.First(x=>x.Value==s.playerId).Key}).ToArray()}).ToArray();
    properties["plan"]=D("type","integer","enum",Enumerable.Range(0,result.SubPlans.Count).ToArray());required.Add("plan");
    result.System="你是比赛中的足球教练。结合当前分钟，自主决定tactic、plan和swapPlan；可以保持现状，不为调整而调整。换人全场最多5人，应为后续阶段保留名额。swapPlan是场上换位方案编号，0为不换位，不消耗换人名额，先换人再换位。plan是合法换人方案编号，0为不换人。候选包括单人、多人换人及不同进攻/防守/轮换取向。结合比分、能力、疲劳和赛程选取，不要机械地选最多换人的方案。只能输出方案编号，不自行输出substitutions。slot为0前左1前中2前右3中左4中中5中右6后左7后中8后右9门将。只输出指定JSON和简短reason。";
   }else if(phase=="续约"){
    if(g.CareerMode=="球星迭起"){properties["retire"]=Strings(result.Codes.Keys,Math.Max(0,squad.Length-10));context["退役说明"]="可用retire列出主动退役球员，不得与release重复，合计操作后至少留10人。退役永久离开活跃球员池。";}
    properties["release"]=Strings(result.Codes.Keys,Math.Max(0,squad.Length-10));required.Insert(0,"release");result.System="你是俱乐部经理。决定不续约名单release，填写本队code。考虑位置深度、能力、好感、资金和目标。不必释放球员，可返回空数组。只输出JSON release及简短中文reason。";
   }else{
    var replyProperties=new Dictionary<string,object>();foreach(var q in offerCodes)replyProperties[q.code]=D("type","string","enum",new[]{"accept","reject","defer"});
    properties["responses"]=D("type","object","properties",replyProperties,"required",offerCodes.Select(q=>q.code).ToArray(),"additionalProperties",false);required.Add("responses");
    if(phase=="需求转会"){
     var market=g.Players.Where(p=>p.ClubId!=c.Id&&(p.ClubId==""||p.Listed)&&Transfers.Affordable(g,p,c,Transfers.Fee(g,p,false))).OrderByDescending(p=>WorldIdentity.Priority(g,c,p)).Take(35).ToArray();context["剩余报价次数"]=Transfers.OffersRemaining(g,c);context["可负担市场"]=market.Select(p=>new{id=p.Id,type=p.Type,foot=p.Foot,value=p.Value,age=g.CareerMode=="球星迭起"?p.Age:0,relationship=WorldIdentity.Relation(g,c.Id,p.ClubId),fee=Transfers.Fee(g,p,false)}).ToArray();
     properties["listPlayers"]=Strings(result.Codes.Where(x=>{var p=squad.First(v=>v.Id==x.Value);return p.Type=="门将"||squad.Count(v=>v.Type==p.Type)>5;}).Select(x=>x.Key),6);required.Add("listPlayers");
     properties["offers"]=D("type","array","maxItems",market.Length>0?Math.Min(3,Transfers.OffersRemaining(g,c)):0,"items",D("type","object","properties",D("playerId",D("type","string","enum",market.Length>0?market.Select(p=>p.Id).ToArray():new[]{""}),"fee",D("type","number","minimum",0)),"required",new[]{"playerId","fee"},"additionalProperties",false));required.Add("offers");
    }
    result.System="你是俱乐部经营经理，独立决策转会。responses对每个报价编号只填一个动作：accept接受、reject拒绝、defer暂缓。没有报价时返回空对象{}。不可另外输出acceptOffers或rejectOffers。若有市场，可在offers提出至多3次购买，费用至少为市场fee；挂牌listPlayers用本队code。工资等于全队身价，买人后要保留工资。不需要买人可空。只输出指定JSON，reason简短中文。";
   }
  }
  if(CoachingPolicy.PlayerMatch(g,f)&&LiveCoaching.Phase(phase))LiveCoachPlans.Configure(result,context,properties,required,g,c,f);
  if(CoachingPolicy.PlayerMatch(g,f)){context["比赛观察"]=CoachingPolicy.MatchContext(g,c,f);context["理念效果"]=RuleDescriptions.Philosophy[c.Philosophy];if(phase=="赛前")context["候选三路评估"]=result.Plans.Select(p=>CoachingPolicy.PlanObservation(g,c,f,p)).ToArray();result.System+=" 根据实际路线攻防、当前比分、疲劳、替补和可用换人名额作出具体决策。允许保持战术，不要为变化而变化；只使用公开阵容信息。";}
  context["模式"]=g.CareerMode;
  context["经营约束"]="遵循本队长期经营方针，并结合当前资金与赛事阶段调整。竞技优先不代表可违背工资预算。关系越高越友好，低关系时慎重向竞争者出售核心，但关系不自动决定成交。";
  if(f!=null)context["当前赛事"]=f.Competition+" / "+f.Stage;
  result.System+=" 必须体现本队长期经营方针，但不得在公开reason中透露隐藏性格名称、完整内部方针或关系数值；只解释具体动作。";
  if(L10n.English)result.System+=" Write reason in concise English. Keep tactic names and all schema values exactly as specified.";
  result.Schema=D("type","object","properties",properties,"required",required.ToArray(),"additionalProperties",false);result.Prompt=Storage.Encode(context);return result;
 }
}
}

