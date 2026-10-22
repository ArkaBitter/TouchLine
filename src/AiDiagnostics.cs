using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
namespace Touchline {
public sealed class BoundedDiagnosticLog {
 readonly object gate=new object();readonly string path;readonly int limit;
 public BoundedDiagnosticLog(string file,int bytes){path=file;limit=bytes;}
 public bool Write(string kind,string message){try{lock(gate){
  string value=Regex.Replace(message??"",@"(?i)(Bearer\s+)[^\s""<>]+","$1[redacted]");
  value=Regex.Replace(value,@"(?i)((?:api[_-]?key|authorization|token)\s*[""']?\s*[:=]\s*[""']?)[^\s,""'}]+","$1[redacted]");
  value=value.Replace("\r"," ").Replace("\n"," ");if(value.Length>2000)value=value.Substring(0,2000)+" [truncated]";
  string line=DateTime.UtcNow.ToString("o")+" ["+kind+"] "+value+Environment.NewLine;
  byte[] data=Encoding.UTF8.GetBytes(line);if(data.Length>limit)data=Encoding.UTF8.GetBytes(DateTime.UtcNow.ToString("o")+" [entry truncated]"+Environment.NewLine);
  Directory.CreateDirectory(Path.GetDirectoryName(path));if(File.Exists(path)&&new FileInfo(path).Length+data.Length>limit){if(File.Exists(path+".1"))File.Delete(path+".1");File.Move(path,path+".1");}
  using(var stream=new FileStream(path,FileMode.Append,FileAccess.Write,FileShare.Read)){stream.Write(data,0,data.Length);}return true;
 }}catch{return false;}}
}
public static class AiDiagnostics {
 public static string DirectoryPath {get{return Path.Combine(Storage.Root,"diagnostics");}}
 static readonly BoundedDiagnosticLog log=new BoundedDiagnosticLog(Path.Combine(DirectoryPath,"local-ai.log"),1024*1024);
 public static void Write(string kind,string message){log.Write(kind,message);}
}
}
