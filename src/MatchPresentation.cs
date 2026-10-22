using System;
using System.Linq;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Collections.Generic;

namespace Touchline {
public static partial class PlayerEditing {
 public static void Rename(Player p,string name){name=(name??"").Trim();if(name.Length==0||name.Length>30||name.Any(Char.IsControl))throw new Exception("名字必须为1～30字，不能包含换行或控制字符。");p.Name=name;}
}
public partial class MainForm {
 MatchPitch matchPitch;ListBox matchEvents;bool animationEnabled=true;
 async void PlayAnimated(int rounds){
  if(busy||game==null||game.Live==null||rounds<=0)return;
  busy=true;body.Enabled=false;nav.Enabled=false;advance.Enabled=false;cancel.Visible=true;running=new CancellationTokenSource();if(progressLog==null||progressLog.IsDisposed)progressLog=new ProgressLogForm();Progress("—— 推进比赛；每个完整回合提交存档 ——");
  try{
   Game staged=Storage.Clone(game);var coach=new LocalModelAgent(config);
   for(int n=0;n<rounds&&staged.Live!=null;n++){
    running.Token.ThrowIfCancellationRequested();await Task.Run(async()=>await LiveCoaching.Boundary(staged,coach,running.Token,Progress));LiveMatch m=staged.Live;var frameHome=(string[])m.HomeLineup.Clone();var frameAway=(string[])m.AwayLineup.Clone();int start=m.BallEvents==null?0:m.BallEvents.Count;
    await Task.Run(async()=>await Controller.PlaySegment(staged,coach,running.Token,Progress,1));
    if(matchPitch!=null&&!matchPitch.IsDisposed){matchPitch.SetMatch(staged,new LiveMatch{FixtureId=m.FixtureId,HomeLineup=frameHome,AwayLineup=frameAway,Round=m.Round,BallEvents=m.BallEvents});if(animationEnabled&&m.BallEvents!=null)foreach(var e in m.BallEvents.Skip(start)){status.Text=e.Round*2+"′ "+e.Text;await matchPitch.Play(e,running.Token);}else matchPitch.Invalidate();matchPitch.SetMatch(staged,m);}
    var f=staged.Fixtures.First(x=>x.Id==m.FixtureId);heading.Text=staged.Club(f.Home).Name+"  "+m.HomeGoals+" : "+m.AwayGoals+"  "+staged.Club(f.Away).Name;subheading.Text=f.Competition+" / "+m.Round*2+"′";
    if(matchEvents!=null&&!matchEvents.IsDisposed){matchEvents.Items.Clear();foreach(string e in m.Events.AsEnumerable().Reverse())matchEvents.Items.Add(L10n.T(e));}
    RefreshMatchInformation(staged,m);Rules.Validate(staged);CommitProgress(staged);
   }
   Progress("【已提交】比赛进度已保存。");
  }catch(OperationCanceledException){Progress("【已暂停】保留上一完整回合；尚未播放完的回合及其中的决定未提交。");}
  catch(Exception ex){Progress("【已暂停】保留上一完整回合。原因："+ex.Message);UiMessage.Show(ex.Message,"比赛暂停");}
  finally{busy=false;body.Enabled=true;nav.Enabled=true;cancel.Visible=false;running.Dispose();running=null;Render();}
 }
 void ShowSubstitution(){
  if(game.Live==null)return;bool home=game.Fixtures.First(f=>f.Id==game.Live.FixtureId).Home==game.PlayerClubId;
  using(var dialog=new SubstitutionForm(game,home)){if(dialog.ShowDialog(this)!=DialogResult.OK)return;Football.Substitute(game,home,dialog.Slot,dialog.PlayerId);AutoSave();Render();}
 }
}

public class SubstitutionForm:Form {
 public int Slot; public string PlayerId;DataGridView field,bench;Label summary;
 public SubstitutionForm(Game game,bool home){
  Text="换人 · 左侧选择下场球员，右侧选择替补";Size=new Size(1140,650);MinimumSize=new Size(950,540);StartPosition=FormStartPosition.CenterParent;BackColor=MainForm.Bg;ForeColor=MainForm.White;Font=new Font("Microsoft YaHei UI",10);
  var m=game.Live;var f=game.Fixtures.First(x=>x.Id==m.FixtureId);string club=home?f.Home:f.Away;var lineup=home?m.HomeLineup:m.AwayLineup;var used=home?m.UsedHome:m.UsedAway;int count=home?m.HomeSubs:m.AwaySubs;
  var header=new Label{Text=game.Club(club).Name+" · 已换 "+count+" / 5人 · 所有替补均列出，不可上场者标注原因",Dock=DockStyle.Top,Height=45,Padding=new Padding(12)};Controls.Add(header);
  var bottom=new Panel{Dock=DockStyle.Bottom,Height=85};Controls.Add(bottom);summary=new Label{Dock=DockStyle.Top,Height=40,Text="请选择两名球员",Padding=new Padding(8)};bottom.Controls.Add(summary);
  var split=new SplitContainer{Dock=DockStyle.Fill,Size=new Size(1100,500),SplitterDistance=530};Controls.Add(split);split.BringToFront();field=Table();bench=Table();split.Panel1.Controls.Add(field);split.Panel2.Controls.Add(bench);
  foreach(string col in new[]{"场上位置","球员","类型","脚","疲劳","红","绿","蓝","射/扑"})field.Columns.Add(col,col);
  foreach(string col in new[]{"替补球员","类型","脚","疲劳","红","绿","蓝","射/扑","可用状态"})bench.Columns.Add(col,col);
  for(int i=0;i<10;i++){Player p=game.Players.First(x=>x.Id==lineup[i]);int row=field.Rows.Add(Rules.Slots[i],p.Name,p.Type,Foot(p),p.Fatigue,p.Red,p.Green,p.Blue,p.Shot);field.Rows[row].Tag=i;}
  foreach(Player p in game.Squad(club).Where(p=>!lineup.Contains(p.Id))){string why=p.Injury>0?"伤停 "+p.Injury+"周":used.Contains(p.Id)?"已换下，不可再上":"可上场";int row=bench.Rows.Add(p.Name,p.Type,Foot(p),p.Fatigue,p.Red,p.Green,p.Blue,p.Shot,why);bench.Rows[row].Tag=p.Id;if(why!="可上场")bench.Rows[row].DefaultCellStyle.ForeColor=Color.Gray;}
  Action describe=()=>{if(field.SelectedRows.Count==0||bench.SelectedRows.Count==0)return;summary.Text="下场："+field.SelectedRows[0].Cells[1].Value+" → 上场："+bench.SelectedRows[0].Cells[0].Value;};field.SelectionChanged+=(s,e)=>describe();bench.SelectionChanged+=(s,e)=>describe();
  var ok=MainForm.Button("确认换人",()=>{if(count>=5){UiMessage.Show("本场已用完5次换人。");return;}if(field.SelectedRows.Count==0||bench.SelectedRows.Count==0)return;PlayerId=Convert.ToString(bench.SelectedRows[0].Tag);Player p=game.Players.First(x=>x.Id==PlayerId);if(p.Injury>0||used.Contains(PlayerId)){UiMessage.Show("该替补不可上场，请选择其他球员。");return;}Slot=(int)field.SelectedRows[0].Tag;DialogResult=DialogResult.OK;});ok.Location=new Point(12,42);bottom.Controls.Add(ok);var cancel=MainForm.Button("取消",()=>DialogResult=DialogResult.Cancel);cancel.Location=new Point(145,42);bottom.Controls.Add(cancel);describe();
 }
 static string Foot(Player p){return p.Foot==0?"双":p.Foot==1?"左":"右";}
 internal static DataGridView Table(){var v=new DataGridView{Dock=DockStyle.Fill,ReadOnly=true,AllowUserToAddRows=false,AllowUserToDeleteRows=false,RowHeadersVisible=false,MultiSelect=false,SelectionMode=DataGridViewSelectionMode.FullRowSelect,AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.AllCells,BackgroundColor=MainForm.Bg,EnableHeadersVisualStyles=false};v.DefaultCellStyle.BackColor=MainForm.Card;v.DefaultCellStyle.ForeColor=MainForm.White;v.DefaultCellStyle.SelectionBackColor=Color.FromArgb(35,90,88);v.DefaultCellStyle.SelectionForeColor=Color.White;v.ColumnHeadersDefaultCellStyle.BackColor=MainForm.Card;v.ColumnHeadersDefaultCellStyle.ForeColor=MainForm.White;v.RowTemplate.Height=32;return v;}
}

public class MatchPitch:Panel {
 Game game;LiveMatch match;PointF ball=new PointF(.5f,.5f);string carrier="",pulse="",caption="等待开球";float bounce;bool visibleBall=true;
 public MatchPitch(){DoubleBuffered=true;BackColor=Color.FromArgb(20,65,48);}
 public void SetMatch(Game g,LiveMatch m){bool initial=match==null;game=g;match=m;if(initial&&m.BallEvents!=null&&m.BallEvents.Count>0){ball=Target(m.BallEvents.Last().Target);caption=L10n.T(m.BallEvents.Last().Text);}Invalidate();}
 PointF Position(int slot,bool home){float x=slot==9?.06f:slot<3?.69f:slot<6?.44f:.18f;float y=slot==9?.5f:.22f+(slot%3)*.28f;return home?new PointF(x,y):new PointF(1-x,1-y);}
 PointF Target(string id){if(id=="goalAway")return new PointF(.985f,.5f);if(id=="goalHome")return new PointF(.015f,.5f);if(match!=null){int n=Array.IndexOf(match.HomeLineup,id);if(n>=0)return Position(n,true);n=Array.IndexOf(match.AwayLineup,id);if(n>=0)return Position(n,false);}return new PointF(.5f,.5f);}
 public async Task Play(BallEvent e,CancellationToken token){caption=L10n.T(e.Text);pulse="";visibleBall=true;
  if(e.Kind=="miss"){
   // No new randomness: the shooting side determines the exit direction.
   bool homeShot=match==null||!match.AwayLineup.Contains(carrier);PointF outside=new PointF(homeShot?1.025f:-.025f,ball.Y<=.5f?.30f:.70f);
   await MoveBall(outside,18,token);await Task.Delay(180,token);visibleBall=false;Invalidate();await Task.Delay(180,token);ball=new PointF(.5f,.5f);visibleBall=true;Invalidate();return;
  }
  if(match.HomeLineup.Contains(e.Target)||match.AwayLineup.Contains(e.Target))carrier=e.Target;visibleBall=e.Kind!="reset";await MoveBall(Target(e.Target),12,token);visibleBall=true;pulse=e.Target;
  for(int i=0;i<8;i++){bounce=(float)Math.Sin(i*Math.PI/7)*9;Invalidate();await Task.Delay(20,token);}bounce=0;Invalidate();
 }
 async Task MoveBall(PointF to,int frames,CancellationToken token){PointF from=ball;for(int i=1;i<=frames;i++){token.ThrowIfCancellationRequested();float t=i/(float)frames;ball=new PointF(from.X+(to.X-from.X)*t,from.Y+(to.Y-from.Y)*t);Invalidate();await Task.Delay(20,token);}}
 PointF Pixel(PointF p){return new PointF(28+p.X*(Width-56),55+p.Y*(Height-115));}
 protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);var g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;if(Width<100||Height<150)return;float w=Width-56,h=Height-115;
  using(var stripe=new SolidBrush(Color.FromArgb(24,73,53)))for(int i=0;i<10;i+=2)g.FillRectangle(stripe,28+i*w/10,55,w/10,h);
  using(var pen=new Pen(Color.FromArgb(125,187,156),2)){g.DrawRectangle(pen,28,55,w,h);g.DrawLine(pen,28+w/2,55,28+w/2,55+h);g.DrawEllipse(pen,28+w/2-45,55+h/2-45,90,90);g.DrawRectangle(pen,28,55+h*.25f,w*.12f,h*.5f);g.DrawRectangle(pen,28+w*.88f,55+h*.25f,w*.12f,h*.5f);g.DrawRectangle(pen,18,55+h*.42f,10,h*.16f);g.DrawRectangle(pen,28+w,55+h*.42f,10,h*.16f);}
  if(game!=null&&match!=null){var f=game.Fixtures.First(x=>x.Id==match.FixtureId);TextRenderer.DrawText(g,game.Club(f.Home).Name+"  →",Font,new Point(30,15),Color.White);TextRenderer.DrawText(g,"←  "+game.Club(f.Away).Name,Font,new Rectangle(Width/2,15,Width/2-30,30),Color.White,TextFormatFlags.Right);
   for(int side=0;side<2;side++){var ids=side==0?match.HomeLineup:match.AwayLineup;for(int slot=0;slot<ids.Length;slot++){var p=game.Players.FirstOrDefault(x=>x.Id==ids[slot]);if(p==null)continue;var pos=Pixel(Position(slot,side==0));if(p.Id==pulse)pos.Y-=bounce;using(var fill=new SolidBrush(side==0?Color.FromArgb(46,174,232):Color.FromArgb(241,118,77)))g.FillEllipse(fill,pos.X-18,pos.Y-18,36,36);using(var outline=new Pen(p.Id==pulse?Color.Yellow:Color.White,2))g.DrawEllipse(outline,pos.X-18,pos.Y-18,36,36);string letter=String.IsNullOrEmpty(p.Name)?"?":StringInfo.GetNextTextElement(p.Name);TextRenderer.DrawText(g,letter,Font,new Rectangle((int)pos.X-18,(int)pos.Y-18,36,36),Color.White,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter);TextRenderer.DrawText(g,p.Name,Font,new Rectangle((int)pos.X-60,(int)pos.Y+21,120,25),Color.White,TextFormatFlags.HorizontalCenter|TextFormatFlags.EndEllipsis);}}
  }
  if(visibleBall){var pos=Pixel(ball);pos.Y+=16;using(var brush=new SolidBrush(Color.White))g.FillEllipse(brush,pos.X-7,pos.Y-7,14,14);g.DrawEllipse(Pens.Black,pos.X-7,pos.Y-7,14,14);g.FillEllipse(Brushes.Black,pos.X-2.5f,pos.Y-2.5f,5,5);}
  TextRenderer.DrawText(g,caption,Font,new Rectangle(20,Height-43,Width-40,35),Color.White,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis);
 }
}
}

