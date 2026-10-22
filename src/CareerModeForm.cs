using System.Drawing;
using System.Windows.Forms;
namespace Touchline {
public partial class MainForm {
 string ChooseCareerMode(){using(var f=new Form{Text="选择生涯模式",Size=new Size(660,300),StartPosition=FormStartPosition.CenterParent,BackColor=Bg,ForeColor=White,Font=Font}){
  string mode=null;var help=new Label{Text="球星迭起：年龄、青年成长、衰退、退役与每季自由新秀。\n\n长期培养：无年龄结算及年度新秀，专注现有球员培养。\n\n模式在本次新建世界时确定，旧存档默认为长期培养。",Location=new Point(22,25),Size=new Size(610,130)};f.Controls.Add(help);
  var stars=Button("球星迭起",()=>{mode="球星迭起";f.DialogResult=DialogResult.OK;});stars.Location=new Point(90,180);stars.Width=190;f.Controls.Add(stars);
  var longTerm=Button("长期培养",()=>{mode="长期培养";f.DialogResult=DialogResult.OK;});longTerm.Location=new Point(340,180);longTerm.Width=190;f.Controls.Add(longTerm);
  return f.ShowDialog(this)==DialogResult.OK?mode:null;
 }}
}
}
