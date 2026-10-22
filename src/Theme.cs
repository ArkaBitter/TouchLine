using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
namespace Touchline {
public static class Theme {
 public static readonly Color Background=ColorTranslator.FromHtml("#101923"),Surface=ColorTranslator.FromHtml("#1B2938"),Raised=ColorTranslator.FromHtml("#293D50"),Teal=ColorTranslator.FromHtml("#35D0A6"),Gold=ColorTranslator.FromHtml("#E4BD70"),Text=ColorTranslator.FromHtml("#EDF2F7"),Secondary=ColorTranslator.FromHtml("#A8B7C7");
 public static void DrawText(Graphics graphics,string text,Font original,Rectangle rect,Color color,TextFormatFlags flags){
  float size=original.Size;Font font=new Font(original.FontFamily,size,original.Style);
  try{while(size>Math.Min(8,original.Size)&&TextRenderer.MeasureText(graphics,text,font,rect.Size,flags).Height>rect.Height){size=Math.Max(Math.Min(8,original.Size),size-.5f);font.Dispose();font=new Font(original.FontFamily,size,original.Style);}TextRenderer.DrawText(graphics,text,font,rect,color,flags);}finally{font.Dispose();}
 }
 public static Color Mix(Color a,Color b,float t){return Color.FromArgb((int)(a.R+(b.R-a.R)*t),(int)(a.G+(b.G-a.G)*t),(int)(a.B+(b.B-a.B)*t));}
}
public class GradientPanel:Panel {
 public Color EndColor=Theme.Surface;
 public GradientPanel(){DoubleBuffered=true;}
 protected override void OnPaintBackground(PaintEventArgs e){if(Width<=0||Height<=0)return;using(var brush=new LinearGradientBrush(ClientRectangle,BackColor,EndColor,65f))e.Graphics.FillRectangle(brush,ClientRectangle);}
}
public class ThemeButton:Button {
 protected bool Hovered,Pressed;
 public ThemeButton(){SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer,true);}
 protected override void OnMouseEnter(EventArgs e){Hovered=true;Invalidate();base.OnMouseEnter(e);}
 protected override void OnMouseLeave(EventArgs e){Hovered=false;Pressed=false;Invalidate();base.OnMouseLeave(e);}
 protected override void OnMouseDown(MouseEventArgs e){Pressed=true;Invalidate();base.OnMouseDown(e);}
 protected override void OnMouseUp(MouseEventArgs e){Pressed=false;Invalidate();base.OnMouseUp(e);}
 protected override void OnPaint(PaintEventArgs e){
  if(Width<2||Height<2)return;
  Color top=Enabled?BackColor:Theme.Surface;top=Theme.Mix(top,Pressed?Theme.Background:Theme.Raised,Pressed?.28f:Hovered?.35f:0);
  using(var fill=new LinearGradientBrush(ClientRectangle,Theme.Mix(top,Theme.Text,.025f),Theme.Mix(top,Theme.Background,.15f),90f))e.Graphics.FillRectangle(fill,ClientRectangle);
  using(var border=new Pen(Hovered||Focused?Theme.Teal:Theme.Raised))e.Graphics.DrawRectangle(border,0,0,Width-1,Height-1);
  Theme.DrawText(e.Graphics,Text,Font,new Rectangle(5,2,Width-10,Height-4),Enabled?ForeColor:Theme.Secondary,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter|TextFormatFlags.WordBreak);
  if(Focused&&ShowFocusCues)ControlPaint.DrawFocusRectangle(e.Graphics,new Rectangle(4,4,Width-8,Height-8),Theme.Text,top);
 }
}
public class NavigationButton:ThemeButton {
 readonly Timer timer=new Timer{Interval=16};float amount;bool selected;
 public string PageName;
 public bool Selected{get{return selected;}set{if(selected==value)return;selected=value;Animate();}}
 internal float AnimationAmount{get{return amount;}}
 public NavigationButton(){timer.Tick+=(s,e)=>{float target=Selected?1:Hovered||Focused?.5f:0;amount+=Math.Sign(target-amount)*Math.Min(.12f,Math.Abs(target-amount));if(Math.Abs(amount-target)<.001f)timer.Stop();Invalidate();};}
 void Animate(){if(!IsDisposed)timer.Start();}
 protected override void OnMouseEnter(EventArgs e){base.OnMouseEnter(e);Animate();}
 protected override void OnMouseLeave(EventArgs e){base.OnMouseLeave(e);Animate();}
 protected override void OnGotFocus(EventArgs e){base.OnGotFocus(e);Animate();}
 protected override void OnLostFocus(EventArgs e){base.OnLostFocus(e);Animate();}
 protected override void OnPaint(PaintEventArgs e){
  base.OnPaint(e);if(Width<2||Height<2)return;
  using(var fill=new LinearGradientBrush(ClientRectangle,Theme.Mix(BackColor,Theme.Raised,amount),Theme.Mix(BackColor,Theme.Surface,amount),0f))e.Graphics.FillRectangle(fill,ClientRectangle);
  if(amount>0)using(var mark=new SolidBrush(Theme.Mix(BackColor,Theme.Teal,amount)))e.Graphics.FillRectangle(mark,0,7,3,Height-14);
  var rect=new Rectangle(16+(int)(amount*3),0,Width-24,Height);
  TextRenderer.DrawText(e.Graphics,Text,Font,rect,Enabled?(Selected?Theme.Teal:Theme.Text):Theme.Secondary,TextFormatFlags.Left|TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis);
  if(Focused&&ShowFocusCues)ControlPaint.DrawFocusRectangle(e.Graphics,new Rectangle(6,4,Width-12,Height-8));
 }
 protected override void Dispose(bool disposing){if(disposing)timer.Dispose();base.Dispose(disposing);}
}
}
