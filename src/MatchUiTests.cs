using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
namespace Touchline {
static class MatchUiTests {
 static void Snapshot(Form f,string name){Application.DoEvents();using(var bmp=new Bitmap(f.Width,f.Height)){f.DrawToBitmap(bmp,new Rectangle(0,0,bmp.Width,bmp.Height));bmp.Save(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,name));}}
 public static void Run(){
  using(var owner=new MainForm()){
   var fresh=GameFactory.New(317);using(var timer=new System.Windows.Forms.Timer{Interval=250}){timer.Tick+=(s,e)=>{var dialog=Application.OpenForms.Cast<Form>().FirstOrDefault(x=>x.Text=="选择执教理念与俱乐部方向");if(dialog!=null){Snapshot(dialog,"preview_creation.png");timer.Stop();dialog.Close();}};timer.Start();owner.ChooseNewClub(fresh);}
   using(var timer=new System.Windows.Forms.Timer{Interval=250}){timer.Tick+=(s,e)=>{var dialog=Application.OpenForms.Cast<Form>().FirstOrDefault(x=>x.Text.StartsWith("开局球队命名"));if(dialog!=null){Snapshot(dialog,"preview_club_names.png");timer.Stop();dialog.Close();}};timer.Start();owner.RenameWorld(fresh,owner);}
  }
  Exception error=null;System.Threading.ThreadExceptionEventHandler catcher=(s,e)=>error=e.Exception;Application.ThreadException+=catcher;
  try{
   using(var host=new Form()){var combo=new DarkComboBox();combo.Items.AddRange(new[]{"无战术","影锋"});host.Controls.Add(combo);host.Show();Application.DoEvents();
    for(int i=0;i<30;i++){combo.PerformClick();Application.DoEvents();combo.ActiveMenu.Items[i%2].PerformClick();combo.ActiveMenu.Close();Application.DoEvents();}
    host.Close();
   }
   if(error!=null)throw error;
   using(var log=new ProgressLogForm()){log.Append("—— 推进赛程 ——");log.Append("【主队 · 赛前】选用战术：影锋");log.Append("【主队 · 中场】换人：球员甲 ↓ ／ 球员乙 ↑（mid_B2B）");log.Append("【比赛结束 · 联赛 / 第4周】主队 2 : 1 客队");log.Append("【已提交】操作完成，已自动存档。");log.Show();Snapshot(log,"preview_progress_log.png");log.Close();if(log.Visible)throw new Exception("Progress window did not hide");log.Show();Application.DoEvents();}
   Game g=GameFactory.New(20261018);var f=g.Fixtures.First();g.PlayerClubId=f.Home;Football.Auto(g,g.Club(f.Home));Football.Auto(g,g.Club(f.Away));Football.Begin(g,f);
   for(int i=0;i<8;i++)Football.Step(g);
   using(var window=new MainForm()){window.Preview(g);window.Show();Snapshot(window,"preview_match.png");window.Close();}
   using(var owner=new MainForm()){owner.Preview(g);f.Review=g.Live.Review;using(var timer=new System.Windows.Forms.Timer{Interval=300}){timer.Tick+=(s,e)=>{var dialog=Application.OpenForms.Cast<Form>().FirstOrDefault(x=>x.Text==L10n.T("比赛复盘"));if(dialog!=null){Snapshot(dialog,"preview_match_review.png");timer.Stop();dialog.Close();}};timer.Start();owner.ShowMatchReview(f);}using(var timer=new System.Windows.Forms.Timer{Interval=300}){timer.Tick+=(s,e)=>{var dialog=Application.OpenForms.Cast<Form>().FirstOrDefault(x=>x.Text==L10n.T("转会预算预览"));if(dialog!=null){Snapshot(dialog,"preview_transfer_budget.png");timer.Stop();dialog.Close();}};timer.Start();owner.AskTransferFee(g.Squad(f.Away).First());}}
   using(var sub=new SubstitutionForm(g,true)){sub.Show();Snapshot(sub,"preview_substitution.png");sub.Close();}
   using(var host=new Form{Size=new Size(1000,700)}){var pitch=new MatchPitch{Dock=DockStyle.Fill};pitch.SetMatch(g,g.Live);host.Controls.Add(pitch);host.Show();
    foreach(var e in g.Live.BallEvents.Take(12).Concat(new[]{new BallEvent{Target="center",Kind="miss",Text="射偏飞出球场测试"}})){var task=pitch.Play(e,CancellationToken.None);while(!task.IsCompleted){Application.DoEvents();Thread.Sleep(5);}task.GetAwaiter().GetResult();}
    using(var cts=new CancellationTokenSource()){cts.Cancel();bool canceled=false;try{pitch.Play(new BallEvent{Target="center",Kind="reset",Text="test"},cts.Token).GetAwaiter().GetResult();}catch(OperationCanceledException){canceled=true;}if(!canceled)throw new Exception("Animation cancellation failed");}
    host.Close();
   }
   if(error!=null)throw error;
   File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"test-results","match-ui-result.json"),Storage.Encode(new{MenuCycles=30,AnimationEvents=13,Cancellation=true,Passed=true,CompletedAt=DateTime.UtcNow.ToString("o")}));
  }finally{Application.ThreadException-=catcher;}
 }
}
}
