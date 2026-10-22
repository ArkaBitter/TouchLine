using System;
using System.Linq;
using System.Drawing;
using System.Windows.Forms;
namespace Touchline {
public class PositionSwapForm:Form {
 public int SlotA,SlotB;
 public PositionSwapForm(Game g,bool home){Text="场上换位";Size=new Size(680,620);MinimumSize=Size;StartPosition=FormStartPosition.CenterParent;BackColor=MainForm.Bg;ForeColor=MainForm.White;Font=new Font("Microsoft YaHei UI",10);
  var m=g.Live;var lineup=home?m.HomeLineup:m.AwayLineup;
  var description=new Label{Dock=DockStyle.Top,Height=76,Padding=new Padding(10),Text="选择两名场上球员互换位置，不消耗换人名额。换位后按新位置重新加权，仍遵守健康门将优先规则。"};Controls.Add(description);
  var choices=new FlowLayoutPanel{Dock=DockStyle.Top,Height=46};Controls.Add(choices);var a=new DarkComboBox{Name="SwapA",Width=305};var b=new DarkComboBox{Name="SwapB",Width=305};foreach(int i in Enumerable.Range(0,10)){string item=Rules.Slots[i]+" · "+g.Players.First(p=>p.Id==lineup[i]).Name;a.Items.Add(item);b.Items.Add(item);}a.SelectedIndex=0;b.SelectedIndex=1;choices.Controls.Add(a);choices.Controls.Add(b);
  var bottom=new Panel{Dock=DockStyle.Bottom,Height=110};Controls.Add(bottom);var preview=new Label{Name="SwapPreview",Dock=DockStyle.Top,Height=60,Padding=new Padding(10)};bottom.Controls.Add(preview);var ok=MainForm.Button("确认换位",()=>{LiveCoaching.SwapPreview(g,home,a.SelectedIndex,b.SelectedIndex);SlotA=a.SelectedIndex;SlotB=b.SelectedIndex;DialogResult=DialogResult.OK;});ok.SetBounds(390,65,125,36);bottom.Controls.Add(ok);var cancel=MainForm.Button("取消",()=>DialogResult=DialogResult.Cancel);cancel.SetBounds(525,65,100,36);bottom.Controls.Add(cancel);
  var table=SubstitutionForm.Table();table.Name="OnFieldPlayers";foreach(string column in new[]{"位置","球员","类型","身价","疲劳"})table.Columns.Add(column,column);foreach(int i in Enumerable.Range(0,10)){var p=g.Players.First(x=>x.Id==lineup[i]);table.Rows.Add(Rules.Slots[i],p.Name,p.Type,p.Value,p.Fatigue+"%");}Controls.Add(table);table.BringToFront();
  Action draw=()=>{try{LiveCoaching.SwapPreview(g,home,a.SelectedIndex,b.SelectedIndex);preview.Text=g.Players.First(p=>p.Id==lineup[a.SelectedIndex]).Name+" → "+Rules.Slots[b.SelectedIndex]+"\r\n"+g.Players.First(p=>p.Id==lineup[b.SelectedIndex]).Name+" → "+Rules.Slots[a.SelectedIndex];preview.ForeColor=MainForm.White;ok.Enabled=true;}catch(Exception ex){preview.Text=L10n.T(ex.Message);preview.ForeColor=Color.Salmon;ok.Enabled=false;}};a.SelectedIndexChanged+=(s,e)=>draw();b.SelectedIndexChanged+=(s,e)=>draw();draw();L10n.Watch(this);
 }
}
public class MatchTacticForm:Form {
 public string ChosenTactic;
 public MatchTacticForm(Game g,bool home){Text="战术与效果";Size=new Size(720,390);MinimumSize=new Size(600,340);StartPosition=FormStartPosition.CenterParent;BackColor=MainForm.Bg;ForeColor=MainForm.White;Font=new Font("Microsoft YaHei UI",10);
  var m=g.Live;var f=g.Fixtures.First(x=>x.Id==m.FixtureId);var c=g.Club(home?f.Home:f.Away);var choice=new DarkComboBox{Name="MatchTacticChoice",Dock=DockStyle.Top,Height=40};choice.Items.AddRange(new[]{"无战术"}.Concat(c.Tactics).Distinct().Cast<object>().ToArray());choice.SelectedItem=home?m.HomeTactic:m.AwayTactic;Controls.Add(choice);
  var footer=new FlowLayoutPanel{Dock=DockStyle.Bottom,Height=52,FlowDirection=FlowDirection.RightToLeft};Controls.Add(footer);var apply=MainForm.Button("应用战术",()=>{ChosenTactic=Convert.ToString(choice.SelectedItem);if(f.Neutral&&ChosenTactic=="战术犯规")throw new Exception("决赛禁用战术犯规。");DialogResult=DialogResult.OK;});footer.Controls.Add(apply);footer.Controls.Add(MainForm.Button("取消",()=>DialogResult=DialogResult.Cancel));
  var effect=new TextBox{Name="MatchTacticEffect",Dock=DockStyle.Fill,Multiline=true,ReadOnly=true,ScrollBars=ScrollBars.Vertical,WordWrap=true,BackColor=MainForm.Card,ForeColor=MainForm.White,Font=new Font("Microsoft YaHei UI",12),BorderStyle=BorderStyle.None};Controls.Add(effect);effect.BringToFront();
  Action draw=()=>{string t=Convert.ToString(choice.SelectedItem),description;RuleDescriptions.Tactic.TryGetValue(t,out description);bool allowed=!(f.Neutral&&t=="战术犯规");effect.Text=L10n.T(t)+"\r\n\r\n"+L10n.T(description??"")+"\r\n\r\n"+L10n.T(allowed?"查看效果后点击应用战术，选择本身不会改变当前战术。":"决赛禁用战术犯规。");apply.Enabled=allowed;};choice.SelectedIndexChanged+=(s,e)=>draw();draw();L10n.Watch(this);
 }
}
public partial class MainForm {
 internal void ShowPositionSwap(){if(game.Live==null)return;bool home=game.Fixtures.First(f=>f.Id==game.Live.FixtureId).Home==game.PlayerClubId;using(var dialog=new PositionSwapForm(game,home)){if(dialog.ShowDialog(this)!=DialogResult.OK)return;LiveCoaching.Swap(game,home,dialog.SlotA,dialog.SlotB);AutoSave();Render();}}
 internal void ShowMatchTactics(){if(game.Live==null)return;bool home=game.Fixtures.First(f=>f.Id==game.Live.FixtureId).Home==game.PlayerClubId;using(var dialog=new MatchTacticForm(game,home)){if(dialog.ShowDialog(this)!=DialogResult.OK)return;LiveCoaching.SetTactic(game,home,dialog.ChosenTactic);AutoSave();Render();}}
}
}
