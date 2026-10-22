using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
namespace Touchline {
public class ProgressLogForm:Form {
 ListBox output;CheckBox follow;
 public ProgressLogForm(){Text="赛程推进 · 智能体决策与比赛结果";Size=new Size(940,650);MinimumSize=new Size(680,420);StartPosition=FormStartPosition.CenterParent;BackColor=MainForm.Bg;ForeColor=MainForm.White;
  var bar=new FlowLayoutPanel{Dock=DockStyle.Top,Height=45};Controls.Add(bar);follow=new CheckBox{Text="跟随最新消息",Checked=true,AutoSize=true,ForeColor=MainForm.White,Margin=new Padding(12)};bar.Controls.Add(follow);
  var copy=MainForm.Button("复制日志",()=>{if(output.Items.Count>0)Clipboard.SetText(string.Join(Environment.NewLine+Environment.NewLine,output.Items.Cast<string>().ToArray()));});bar.Controls.Add(copy);
  output=new ListBox{Dock=DockStyle.Fill,BackColor=MainForm.Bg,ForeColor=MainForm.White,BorderStyle=BorderStyle.None,Font=new Font("Microsoft YaHei UI",10),DrawMode=DrawMode.OwnerDrawVariable,IntegralHeight=false};
  output.MeasureItem+=(s,e)=>{if(e.Index>=0)e.ItemHeight=Math.Max(32,TextRenderer.MeasureText(Convert.ToString(output.Items[e.Index]),output.Font,new Size(Math.Max(100,output.ClientSize.Width-28),10000),TextFormatFlags.WordBreak|TextFormatFlags.NoPrefix).Height+16);};
  output.DrawItem+=(s,e)=>{if(e.Index<0)return;e.Graphics.FillRectangle(System.Drawing.Brushes.Transparent,e.Bounds);using(var brush=new SolidBrush(MainForm.Bg))e.Graphics.FillRectangle(brush,e.Bounds);string text=Convert.ToString(output.Items[e.Index]);Color color=text.Contains("比赛结束")?MainForm.Accent:text.Contains("固定程序兜底")?Color.Gold:MainForm.White;var box=new Rectangle(e.Bounds.X+10,e.Bounds.Y+8,Math.Max(100,e.Bounds.Width-28),e.Bounds.Height-8);TextRenderer.DrawText(e.Graphics,text,output.Font,box,color,TextFormatFlags.WordBreak|TextFormatFlags.NoPrefix);};
  Controls.Add(output);output.BringToFront();
 }
 public void Append(string text){if(IsDisposed)return;output.Items.Add(DateTime.Now.ToString("HH:mm:ss")+"  "+L10n.T(text));if(follow.Checked)output.TopIndex=output.Items.Count-1;}
 protected override void OnFormClosing(FormClosingEventArgs e){if(e.CloseReason==CloseReason.UserClosing){e.Cancel=true;Hide();}base.OnFormClosing(e);}
}
public partial class MainForm {
 ProgressLogForm progressLog;
 void ShowProgressLog(){if(progressLog==null||progressLog.IsDisposed)progressLog=new ProgressLogForm();if(!progressLog.Visible)progressLog.Show(this);else progressLog.BringToFront();}
 void BeginProgressLog(string action){ShowProgressLog();Progress("—— "+action+"：本次记录为处理中结果，完成并保存后生效 ——");}
}
}
