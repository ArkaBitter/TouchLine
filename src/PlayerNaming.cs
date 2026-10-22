using System;
using System.Linq;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
namespace Touchline {
public partial class MainForm {
 internal void RenameWorldPlayers(Game g,IWin32Window owner){using(var form=new Form{Text="开局球员命名 · 不显示俱乐部归属",Size=new Size(720,720),StartPosition=FormStartPosition.CenterParent,BackColor=Bg,ForeColor=White,Font=Font}){var grid=Grid();Columns(grid,"编号","球员名字");grid.ReadOnly=false;grid.Columns[0].ReadOnly=true;grid.AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.Fill;int n=0;foreach(var p in g.Players.OrderBy(p=>p.Id,StringComparer.Ordinal)){int i=grid.Rows.Add(++n,p.Name);grid.Rows[i].Tag=p.Id;}form.Controls.Add(grid);var save=Button("保存球员名称",()=>{grid.EndEdit();var names=grid.Rows.Cast<DataGridViewRow>().ToDictionary(r=>(string)r.Tag,r=>Convert.ToString(r.Cells[1].Value));PlayerEditing.RenameAllBeforeStart(g,names);form.DialogResult=DialogResult.OK;});save.Dock=DockStyle.Bottom;form.Controls.Add(save);form.ShowDialog(owner);}}
}
public static partial class PlayerEditing {
 public static void RenameOwn(Game g,string id,string name){var p=g.Players.First(x=>x.Id==id);if(p.ClubId!=g.PlayerClubId||String.IsNullOrEmpty(g.PlayerClubId))throw new Exception("正式开局后只能修改本队球员名字。");Rename(p,name);}
 public static void RenameAllBeforeStart(Game g,Dictionary<string,string> names){if(!String.IsNullOrEmpty(g.PlayerClubId))throw new Exception("正式开局后不能批量修改其他球队球员。");if(names.Count!=g.Players.Count||g.Players.Any(p=>!names.ContainsKey(p.Id)))throw new Exception("球员名单不完整。");foreach(string name in names.Values)Rename(new Player(),name);foreach(var p in g.Players)Rename(p,names[p.Id]);}
}
}
