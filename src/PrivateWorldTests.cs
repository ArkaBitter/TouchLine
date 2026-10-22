using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Windows.Forms;
namespace Touchline {
static class PrivateWorldTests {
 public static void Run(string privatePath){
  string root=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"test-results","private-world");Directory.CreateDirectory(root);string before=Storage.Root;bool english=L10n.English;
  typeof(Storage).GetField("Root").SetValue(null,Path.Combine(root,"isolated-profile"));
  try{
   var checks=new List<string>();var assembly=typeof(MainForm).Assembly;
   if(assembly.GetManifestResourceNames().Any(n=>n.IndexOf("CurrentWorld",StringComparison.OrdinalIgnoreCase)>=0))throw new Exception("Private world still embedded");checks.Add("No embedded private-world resource");
   string original=File.ReadAllText(privatePath);var envelope=Storage.Decode<SaveEnvelope>(original);Game raw=Storage.Decode<Game>(envelope.Json),loaded=Storage.Load(privatePath);
   if(loaded.Clubs.Count!=30||loaded.Players.Count!=698||loaded.PlayerClubId!="")throw new Exception("Private template migration invalid");
   bool playerEqual=typeof(Player).GetFields().All(f=>raw.Players.Zip(loaded.Players,(a,b)=>Storage.Encode(f.GetValue(a))==Storage.Encode(f.GetValue(b))).All(x=>x));
   bool clubEqual=raw.Clubs.Zip(loaded.Clubs,(a,b)=>a.Id==b.Id&&a.Name==b.Name&&a.Funds==b.Funds&&a.Commercial==b.Commercial&&a.Honor==b.Honor).All(x=>x);
   if(!playerEqual||!clubEqual)throw new Exception("Private roster or funds changed");checks.Add("30 clubs and 698 players load with original names, attributes and funds");
   string path=Path.Combine(root,"isolated-profile","roundtrip.tmsave");Storage.Save(loaded,path);var restored=Storage.Load(path);if(!typeof(Player).GetFields().All(f=>restored.Players.Zip(loaded.Players,(a,b)=>Storage.Encode(f.GetValue(a))==Storage.Encode(f.GetValue(b))).All(x=>x)))throw new Exception("Private save round trip failed");checks.Add("Save checksum and local load/save round trip");
   using(var owner=new MainForm())using(var timer=new Timer{Interval=100}){
    bool seen=false;owner.Show();timer.Tick+=(s,e)=>{var dialog=Application.OpenForms.Cast<Form>().FirstOrDefault(f=>f!=owner&&f.Text==L10n.T("选择执教理念与俱乐部方向"));if(dialog!=null){seen=true;dialog.DialogResult=DialogResult.Cancel;timer.Stop();}};
    timer.Start();owner.LoadGameFile(privatePath);if(!seen)throw new Exception("Unassigned local save did not offer club selection");if(!owner.Controls.Cast<Control>().Any())throw new Exception("Owner lost");checks.Add("Local template opens club selection and cancellation leaves source intact");
   }
   if(File.ReadAllText(privatePath)!=original)throw new Exception("Private source overwritten");checks.Add("Original private save unchanged");
   // The private round-trip fixture must not remain in distributable test results.
   File.Delete(path);checks.Add("Private round-trip test fixture removed");
   if(File.Exists(Path.Combine(root,"failure.txt")))File.Delete(Path.Combine(root,"failure.txt"));
   File.WriteAllText(Path.Combine(root,"result.json"),Storage.Encode(new{Passed=true,Checks=checks,FullSeasonRun=false,FormalSavesModified=false}));
  }catch(Exception ex){File.WriteAllText(Path.Combine(root,"failure.txt"),ex.ToString());Environment.ExitCode=1;}finally{foreach(string suffix in new[]{"",".bak",".tmp"}){string fixture=Path.Combine(root,"isolated-profile","roundtrip.tmsave")+suffix;if(File.Exists(fixture))File.Delete(fixture);}typeof(Storage).GetField("Root").SetValue(null,before);L10n.English=english;}
 }
}
}
