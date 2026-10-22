using System;
using System.Linq;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
namespace Touchline {
public static class ClubNaming {
 static string Clean(string name){name=(name??"").Trim();if(name.Length<1||name.Length>30||name.Any(char.IsControl))throw new Exception("球队名称须为1～30个字符，不能包含控制字符。");return name;}
 public static void AllBeforeStart(Game g,Dictionary<string,string> names){if(!String.IsNullOrEmpty(g.PlayerClubId))throw new Exception("正式开局后只能修改自己俱乐部的名称。");if(names.Count!=g.Clubs.Count||g.Clubs.Any(c=>!names.ContainsKey(c.Id)))throw new Exception("球队名单不完整。");var cleaned=names.ToDictionary(x=>x.Key,x=>Clean(x.Value));if(cleaned.Values.Distinct(StringComparer.OrdinalIgnoreCase).Count()!=g.Clubs.Count)throw new Exception("球队名称不能重复。");foreach(var c in g.Clubs)c.Name=cleaned[c.Id];}
 public static void Own(Game g,string id,string name){if(id!=g.PlayerClubId||String.IsNullOrEmpty(id))throw new Exception("只能修改自己执教的俱乐部名称。");name=Clean(name);if(g.Clubs.Any(c=>c.Id!=id&&String.Equals(c.Name,name,StringComparison.OrdinalIgnoreCase)))throw new Exception("与其他俱乐部重名。");g.Club(id).Name=name;}
}
public partial class MainForm {
 internal void RenameWorld(Game g,IWin32Window owner){using(var form=new Form{Text="开局球队命名 · 双击名称编辑",Size=new Size(660,700),StartPosition=FormStartPosition.CenterParent,BackColor=Bg,ForeColor=White,Font=Font}){var grid=Grid();Columns(grid,"联赛","球队名称");grid.ReadOnly=false;grid.Columns[0].ReadOnly=true;grid.AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.Fill;foreach(var c in g.Clubs.OrderBy(c=>c.League)){int i=grid.Rows.Add(c.League,c.Name);grid.Rows[i].Tag=c.Id;}form.Controls.Add(grid);var save=Button("保存所有名称",()=>{grid.EndEdit();var names=grid.Rows.Cast<DataGridViewRow>().ToDictionary(r=>(string)r.Tag,r=>Convert.ToString(r.Cells[1].Value));ClubNaming.AllBeforeStart(g,names);form.DialogResult=DialogResult.OK;});save.Dock=DockStyle.Bottom;form.Controls.Add(save);form.ShowDialog(owner);}}
 void RenameOwn(){var c=game.Club(game.PlayerClubId);string name=Ask("俱乐部改名","只修改本队名称，球队身份、赛程和资金保持不变。",c.Name);if(name==null)return;ClubNaming.Own(game,c.Id,name);AutoSave();Render();}
}
}
