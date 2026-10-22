using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
namespace Touchline {
public static class InferenceTuningTests {
 static object Expand(object value){var d=value as Dictionary<string,object>;if(d!=null){if(d.Count==2&&d.ContainsKey("columns")&&d.ContainsKey("rows")){var columns=((IEnumerable)d["columns"]).Cast<object>().Select(Convert.ToString).ToArray();return ((IEnumerable)d["rows"]).Cast<object>().Select(row=>{var cells=((IEnumerable)row).Cast<object>().ToArray();return columns.Select((key,i)=>new{key=key,value=Expand(cells[i])}).ToDictionary(x=>x.key,x=>x.value);}).ToArray();}return d.ToDictionary(x=>x.Key,x=>Expand(x.Value));}if(value==null||value is string||!(value is IEnumerable))return value;return ((IEnumerable)value).Cast<object>().Select(Expand).ToArray();}
 static void Check(bool ok,string label){if(!ok)throw new Exception("Inference tuning: "+label);}
 public static void Run(){
  bool compact=InferenceTuning.CompactInput,shortOutput=InferenceTuning.ShortOutput,matchOnly=InferenceTuning.MatchOnlyShort,gpuOnly=InferenceTuning.GpuOnlyShort;try{InferenceTuning.MatchOnlyShort=false;InferenceTuning.GpuOnlyShort=false;
   var g=GameFactory.New(20261029);g.PlayerClubId=g.Clubs[0].Id;var c=g.Clubs[1];g.Week=6;c.Upgrade=6;var f=g.Fixtures.First(x=>x.Home==c.Id||x.Away==c.Id);Football.Begin(g,f);g.Live.Round=22;
   foreach(string phase in new[]{"赛前","中场","阶段规划","续约","需求转会","自主转会"}){
    var r=AgentRequest.Build(g,c,phase,phase=="赛前"||phase=="中场"?f:null);string original=r.Prompt;var schema=Storage.Decode<Dictionary<string,object>>(Storage.Encode(r.Schema));((Dictionary<string,object>)schema["properties"]).Remove("reason");schema["required"]=((IEnumerable)schema["required"]).Cast<object>().Where(x=>Convert.ToString(x)!="reason").ToArray();InferenceTuning.CompactInput=true;InferenceTuning.ShortOutput=true;InferenceTuning.Prepare(r);Check(Storage.Encode(Expand(Storage.Decode<Dictionary<string,object>>(r.Prompt)))==original,"all input values preserved "+phase);Check(Storage.Encode(schema)==Storage.Encode(r.Schema),"only reason removed from schema "+phase);
   }
   InferenceTuning.GpuOnlyShort=true;foreach(bool gpu in new[]{false,true}){var request=AgentRequest.Build(g,c,"阶段规划",null);InferenceTuning.Prepare(request,"阶段规划",gpu);var properties=(Dictionary<string,object>)((Dictionary<string,object>)request.Schema)["properties"];Check(properties.ContainsKey("reason")==!gpu,"GPU-only output policy");}
   AiReliabilityTests.Run(false);Check(SquadTemplateTestsSafe(),"template regression");
   var root=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"test-results","inference-20261021");Directory.CreateDirectory(root);File.WriteAllText(Path.Combine(root,"tuning-tests.json"),Storage.Encode(new{Passed=true,Phases=6,LosslessInputRoundTrip=true,ActionSchemaUnchanged=true,ExistingAiRegression=true,FullSeason=false}));
  }finally{InferenceTuning.CompactInput=compact;InferenceTuning.ShortOutput=shortOutput;InferenceTuning.MatchOnlyShort=matchOnly;InferenceTuning.GpuOnlyShort=gpuOnly;}
 }
 static bool SquadTemplateTestsSafe(){// Existing tests include their own bounded local benchmark; no season.
  SquadTemplateTests.Run();return true;
 }
}
}
