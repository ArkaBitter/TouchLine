using System;
using System.IO;
using System.Linq;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;

namespace Touchline {
static class MainMenuTests {
 static System.Collections.Generic.IEnumerable<Control> Descendants(Control parent){foreach(Control child in parent.Controls){yield return child;foreach(var nested in Descendants(child))yield return nested;}}
 static void Check(bool condition,string message){if(!condition)throw new Exception(message);}
 public static void Run(){
  string root=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"test-results","main-menu");Directory.CreateDirectory(root);
  string original=Storage.Root;bool language=L10n.English;var checks=new List<string>();
  typeof(Storage).GetField("Root").SetValue(null,Path.Combine(root,"isolated-profile"));
  try{
   Check(MainMenuBackground.SourceBounds==new Rectangle(0,0,2720,1420),"bottom strip crop");checks.Add("Supplied image embedded; bottom 100 pixels excluded before scaling");
   foreach(bool english in new[]{false,true})using(var form=new MainForm()){
    L10n.English=english;form.Show();L10n.Watch(form);Application.DoEvents();
    foreach(Size size in new[]{new Size(1440,920),new Size(1180,740)}){
     form.Size=size;Application.DoEvents();var panel=Descendants(form).OfType<MainMenuBackground>().Single();
     var buttons=Descendants(panel).OfType<Button>().ToArray();Check(buttons.Length==3,"three main menu actions");
     foreach(var button in buttons){Rectangle bounds=panel.RectangleToClient(button.RectangleToScreen(button.ClientRectangle));Check(panel.ClientRectangle.Contains(bounds),"menu button clipped: "+button.Text);Check(button.Enabled,"action disabled");}
     using(var image=new Bitmap(form.Width,form.Height)){form.DrawToBitmap(image,new Rectangle(Point.Empty,image.Size));image.Save(Path.Combine(root,(english?"en":"zh")+"-"+size.Width+".png"));}
     using(var image=new Bitmap(panel.Width,panel.Height)){panel.DrawToBitmap(image,panel.ClientRectangle);Check(image.GetPixel(panel.Width*3/4,panel.Height/2).ToArgb()!=Theme.Background.ToArgb(),"background not rendered");}
     checks.Add((english?"English":"Chinese")+" "+size.Width+": image painted, all three actions enabled and inside viewport");
    }
    Descendants(form).OfType<MainMenuBackground>().Single().Controls.OfType<FlowLayoutPanel>().Single().Controls.OfType<Button>().Last().PerformClick();Application.DoEvents();Check(!Descendants(form).OfType<MainMenuBackground>().Any(),"background should not persist on settings");
    Descendants(form).OfType<Button>().First(b=>b.Text==L10n.T("返回主菜单")).PerformClick();Application.DoEvents();Check(Descendants(form).OfType<MainMenuBackground>().Count()==1,"return to welcome failed");checks.Add((english?"English":"Chinese")+": settings and return to main menu");
    var game=GameFactory.New(21);game.PlayerClubId=game.Clubs.First().Id;form.Preview(game);Application.DoEvents();Check(!Descendants(form).OfType<MainMenuBackground>().Any(),"background leaked into gameplay");checks.Add((english?"English":"Chinese")+": gameplay pages retain existing appearance");
   }
   File.WriteAllText(Path.Combine(root,"result.json"),Storage.Encode(new{Passed=true,Checks=checks,FullSeasonRun=false,FormalSavesModified=false,CompletedAt=DateTime.UtcNow.ToString("o")}));
  }catch(Exception ex){File.WriteAllText(Path.Combine(root,"failure.txt"),ex.ToString());Environment.ExitCode=1;}finally{L10n.English=language;typeof(Storage).GetField("Root").SetValue(null,original);}
 }
}
}
