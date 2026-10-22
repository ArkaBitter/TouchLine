using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
namespace Touchline {
public class ParityCase {
 public Player Player; public int Slot,Status; public string Philosophy,Tactic,Opponent;
 public bool Home,Star; public double[] Expected;
}
static class ParityTests {
 public static void Run(){
  string root=AppDomain.CurrentDomain.BaseDirectory;
  var cases=Storage.Decode<List<ParityCase>>(File.ReadAllText(Path.Combine(root,"..","data","matlab_parity.json")));
  var failures=new List<object>();double maximum=0;
  foreach(var c in cases){var a=Football.Weight(c.Player,c.Slot,c.Philosophy,c.Tactic,c.Opponent,c.Status,c.Home,c.Star);double[] actual={a.R,a.G,a.B,a.K,a.S};double error=actual.Zip(c.Expected,(x,y)=>Math.Abs(x-y)).Max();maximum=Math.Max(maximum,error);if(error>1e-8)failures.Add(new{c.Philosophy,c.Tactic,c.Slot,c.Player.Type,c.Player.Foot,c.Status,c.Expected,Actual=actual,Error=error});}
  Directory.CreateDirectory(Path.Combine(root,"test-results"));File.WriteAllText(Path.Combine(root,"test-results","matlab-parity-result.json"),Storage.Encode(new{Cases=cases.Count,Failed=failures.Count,MaximumError=maximum,Failures=failures}));
  if(failures.Count>0)throw new Exception("MATLAB parity failed: "+failures.Count+" / "+cases.Count);
 }
}
}
