using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Windows.Forms;
namespace Touchline {
public static class L10n {
 public static bool English;static Dictionary<string,string> words=new Dictionary<string,string>();static Regex terms;static string[] names=new string[0];static bool updating;static DateTime checkedAt;
 class Source {public string Text;}
 static ConditionalWeakTable<Control,Source> sources=new ConditionalWeakTable<Control,Source>();
 public static void Initialize(){try{English=File.ReadAllText(Path.Combine(Storage.Root,"language.txt")).Trim()=="en";}catch{}using(var stream=typeof(L10n).Assembly.GetManifestResourceStream("Touchline.English")){if(stream!=null)using(var reader=new StreamReader(stream))words=Storage.Decode<Dictionary<string,string>>(reader.ReadToEnd());}if(words.Count>0)terms=new Regex(string.Join("|",words.Keys.Where(x=>x.Length>0).OrderByDescending(x=>x.Length).Select(Regex.Escape).ToArray()));Application.Idle+=(s,e)=>{if((DateTime.UtcNow-checkedAt).TotalMilliseconds<150)return;checkedAt=DateTime.UtcNow;foreach(Form f in Application.OpenForms.Cast<Form>().ToArray())Watch(f);};}
 public static void Protect(Game g){names=g==null?new string[0]:g.Clubs.Select(c=>c.Name).Concat(g.Players.Select(p=>p.Name)).Where(n=>!String.IsNullOrEmpty(n)).Distinct().OrderByDescending(n=>n.Length).ToArray();}
 public static string T(string text){if(!English||String.IsNullOrEmpty(text))return text;string exact;if(words.TryGetValue(text,out exact))return exact;if(terms==null)return text;var protectedNames=new List<string>();foreach(string n in names)if(text.Contains(n)){string token="{{NAME"+protectedNames.Count+"}}";text=text.Replace(n,token);protectedNames.Add(n);}text=terms.Replace(text,m=>words[m.Value]);for(int i=0;i<protectedNames.Count;i++)text=text.Replace("{{NAME"+i+"}}",protectedNames[i]);return text;}
 public static void Switch(){English=!English;Directory.CreateDirectory(Storage.Root);File.WriteAllText(Path.Combine(Storage.Root,"language.txt"),English?"en":"zh");foreach(Form f in Application.OpenForms.Cast<Form>().ToArray())Refresh(f);}
 static bool LocalText(Control c){return !(c is DarkComboBox)&&(!(c is TextBoxBase)||((TextBoxBase)c).ReadOnly)&&!(c is DataGridView);}
 public static void Watch(Control c){Source source;if(!sources.TryGetValue(c,out source)){source=new Source{Text=c.Text};sources.Add(c,source);if(LocalText(c)){c.TextChanged+=(s,e)=>{if(updating)return;source.Text=c.Text;Update(c,source);};Update(c,source);}var grid=c as DataGridView;if(grid!=null){foreach(DataGridViewColumn col in grid.Columns)col.HeaderText=T(col.Name);grid.CellFormatting+=(s,e)=>{if(!English||e.Value==null||!(e.Value is string))return;string col=grid.Columns[e.ColumnIndex].Name;if(new[]{"球员","球员名字","俱乐部","球队名称","买方","卖方","名称"}.Contains(col))return;e.Value=T((string)e.Value);e.FormattingApplied=true;};if(grid.AutoSizeRowsMode!=DataGridViewAutoSizeRowsMode.None)grid.AutoResizeRows(grid.AutoSizeRowsMode);}var list=c as ListBox;if(list!=null&&list.DrawMode==DrawMode.Normal){for(int i=0;i<list.Items.Count;i++)if(list.Items[i] is string)list.Items[i]=T((string)list.Items[i]);}}
  foreach(Control child in c.Controls)Watch(child);
 }
 static void Update(Control c,Source source){if(c.IsDisposed)return;updating=true;try{c.Text=T(source.Text);}finally{updating=false;}}
 public static void Refresh(Control c){Source source;if(sources.TryGetValue(c,out source)&&LocalText(c))Update(c,source);var grid=c as DataGridView;if(grid!=null){foreach(DataGridViewColumn col in grid.Columns)col.HeaderText=T(col.Name);grid.Invalidate();}foreach(Control child in c.Controls)Refresh(child);}
}
public static class UiMessage {
 public static DialogResult Show(string text){return MessageBox.Show(L10n.T(text));}
 public static DialogResult Show(string text,string caption){return MessageBox.Show(L10n.T(text),L10n.T(caption));}
 public static DialogResult Show(string text,string caption,MessageBoxButtons buttons){return MessageBox.Show(L10n.T(text),L10n.T(caption),buttons);}
 public static DialogResult Show(string text,string caption,MessageBoxButtons buttons,MessageBoxIcon icon){return MessageBox.Show(L10n.T(text),L10n.T(caption),buttons,icon);}
}
}
