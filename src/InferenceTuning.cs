using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
namespace Touchline {
public static class InferenceTuning {
 // Enabled only after the isolated benchmark and reliability checks.
 public static bool CompactInput=true,ShortOutput=true,MatchOnlyShort=false,GpuOnlyShort=true;
 public static void Prepare(AgentRequest request,string phase=null,bool gpu=false){
  if(CompactInput){var data=Storage.Decode<Dictionary<string,object>>(request.Prompt);request.Prompt=Storage.Encode(Pack(data));request.System+=" 输入表格的columns是列名，rows每行按列顺序给出；信息未删减。";}
  if(ShortOutput&&(!GpuOnlyShort||gpu)&&(!MatchOnlyShort||phase=="赛前"||LiveCoaching.Phase(phase))){var schema=(Dictionary<string,object>)request.Schema;var properties=(Dictionary<string,object>)schema["properties"];properties.Remove("reason");schema["required"]=((IEnumerable)schema["required"]).Cast<object>().Where(x=>Convert.ToString(x)!="reason").Select(Convert.ToString).ToArray();request.System+=" 本次schema优先：只输出动作字段，不生成reason或解释；公开摘要由程序依据实际动作生成。";}
 }
 public static object Pack(object value){
  var dictionary=value as Dictionary<string,object>;if(dictionary!=null)return dictionary.ToDictionary(x=>x.Key,x=>Pack(x.Value));
  if(value==null||value is string||!(value is IEnumerable))return value;
  var items=((IEnumerable)value).Cast<object>().ToArray();var records=items.OfType<Dictionary<string,object>>().ToArray();
  if(items.Length>=3&&records.Length==items.Length){var columns=records[0].Keys.ToArray();if(records.All(x=>x.Keys.SequenceEqual(columns)))return AgentRequest.D("columns",columns,"rows",records.Select(x=>columns.Select(k=>Pack(x[k])).ToArray()).ToArray());}
  return items.Select(Pack).ToArray();
 }
}
}
