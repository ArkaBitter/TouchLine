using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Windows.Forms;
namespace Touchline {
static class MenuFlickerTests {
 static IEnumerable<Control> All(Control c){foreach(Control x in c.Controls){yield return x;foreach(var child in All(x))yield return child;}}
 static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
 public static void Run(){
  string root=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"test-results","menu-flicker");Directory.CreateDirectory(root);string before=Storage.Root;bool language=L10n.English;var checks=new List<string>();
  typeof(Storage).GetField("Root").SetValue(null,Path.Combine(root,"isolated-profile"));
  try{
   foreach(bool english in new[]{false,true}){
    L10n.English=english;using(var form=new MainForm()){
     form.Show();Application.DoEvents();L10n.Watch(form);
     var menu=All(form).OfType<MainMenuBackground>().Single();var body=menu.Parent;int removed=0;body.ControlRemoved+=(s,e)=>removed++;
     Action paint=()=>{using(var bitmap=new Bitmap(menu.Width,menu.Height))menu.DrawToBitmap(bitmap,menu.ClientRectangle);};paint();int builds=menu.FrameBuilds;
     for(int i=0;i<4;i++)foreach(var nav in All(form).OfType<NavigationButton>().Where(b=>b.PageName!="本地AI设置").ToArray()){nav.PerformClick();Application.DoEvents();Check(All(form).OfType<MainMenuBackground>().Single()==menu,"unloaded navigation rebuilt welcome");}
     var back=All(form).OfType<Button>().First(b=>b.Text==L10n.T("返回主菜单"));for(int i=0;i<8;i++)back.PerformClick();
     for(int i=0;i<20;i++){menu.Invalidate(true);menu.Update();paint();}
     Check(removed==0&&menu.FrameBuilds==builds,"same-size repaints rebuilt background");checks.Add((english?"English":"Chinese")+": 28 unused navigation clicks, 8 repeated returns and 20 repaints retain the same menu and cached background");
     L10n.Switch();form.PreviewPage("俱乐部总览");Application.DoEvents();Check(All(form).OfType<MainMenuBackground>().Single()==menu,"language switch rebuilt welcome");Check(All(menu).OfType<Button>().Any(b=>b.Text==L10n.T("读取本地存档")),"language did not update");L10n.Switch();checks.Add("Language switching updates existing menu controls");
     form.Size=new Size(1180,740);Application.DoEvents();paint();Check(menu.FrameBuilds>builds,"resize failed to rebuild cached background");int resized=menu.FrameBuilds;paint();Check(menu.FrameBuilds==resized,"resized cache not reused");checks.Add("Resize rebuilds once, subsequent paints reuse frame");
     All(form).OfType<NavigationButton>().First(b=>b.PageName=="本地AI设置").PerformClick();Application.DoEvents();Check(menu.IsDisposed&&!All(form).OfType<MainMenuBackground>().Any(),"settings retained old menu");var settings=body.Controls[0];All(form).OfType<NavigationButton>().First(b=>b.PageName=="本地AI设置").PerformClick();Check(body.Controls[0]==settings,"repeated settings rebuilt controls");back.PerformClick();Application.DoEvents();Check(body.Visible&&All(form).OfType<MainMenuBackground>().Count()==1,"return from settings failed");checks.Add("Settings entry and return remain visible; detached menu disposed");
     var game=GameFactory.New(34);game.PlayerClubId=game.Clubs.First().Id;
     for(int i=0;i<3;i++){
      menu=All(form).OfType<MainMenuBackground>().Single();form.Preview(game);Application.DoEvents();Check(menu.IsDisposed,"gameplay transition retained welcome bitmap");
      var finance=All(form).OfType<NavigationButton>().First(b=>b.PageName=="经营与荣誉");finance.PerformClick();Application.DoEvents();var grid=All(form).OfType<DataGridView>().First();finance.PerformClick();Check(All(form).OfType<DataGridView>().First()==grid,"same gameplay tab rebuilt");
      back.PerformClick();Application.DoEvents();Check(body.Visible&&All(form).OfType<MainMenuBackground>().Count()==1,"gameplay return failed");
     }
     checks.Add("Three gameplay to menu cycles; repeated current game tab does not rebuild");
     using(var bitmap=new Bitmap(form.Width,form.Height)){form.DrawToBitmap(bitmap,new Rectangle(Point.Empty,bitmap.Size));bitmap.Save(Path.Combine(root,english?"en.png":"zh.png"));}
     try{using(var batch=new UiRedrawBatch(body)){throw new InvalidOperationException("injected");}}catch(InvalidOperationException){}Check(body.Visible,"exception left redraw disabled");checks.Add("Redraw batch restores visibility after injected exception");
    }
   }
   File.WriteAllText(Path.Combine(root,"result.json"),Storage.Encode(new{Passed=true,Checks=checks,FullSeasonRun=false,FormalSavesModified=false}));
  }catch(Exception ex){File.WriteAllText(Path.Combine(root,"failure.txt"),ex.ToString());Environment.ExitCode=1;}finally{typeof(Storage).GetField("Root").SetValue(null,before);L10n.English=language;}
 }
}
}
