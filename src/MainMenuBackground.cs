using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Touchline {
// Keep the supplied asset intact. The displayed source rectangle excludes its bottom strip.
public sealed class MainMenuBackground : Panel {
 static readonly Image Stadium=LoadStadium();
 Bitmap frame;internal int FrameBuilds{get;private set;}
 internal static Rectangle SourceBounds {get{return new Rectangle(0,0,Stadium.Width,Stadium.Height-100);}}
 static Image LoadStadium(){using(var stream=typeof(MainMenuBackground).Assembly.GetManifestResourceStream("Touchline.MainMenuBackground"))using(var image=Image.FromStream(stream))return new Bitmap(image);}
 public MainMenuBackground(){DoubleBuffered=true;ResizeRedraw=true;BackColor=Theme.Background;}
 // Compose transparent labels and buttons together rather than exposing each child repaint.
 protected override CreateParams CreateParams{get{var cp=base.CreateParams;cp.ExStyle|=0x02000000;return cp;}}
 protected override void OnPaintBackground(PaintEventArgs e){
  if(Width<2||Height<2)return;
  if(frame==null||frame.Size!=ClientSize){
   var next=new Bitmap(Width,Height);
   try{using(var graphics=Graphics.FromImage(next))DrawFrame(graphics);}
   catch{next.Dispose();throw;}
   if(frame!=null)frame.Dispose();frame=next;FrameBuilds++;
  }
  e.Graphics.DrawImageUnscaled(frame,Point.Empty);
 }
 void DrawFrame(Graphics graphics){
  graphics.Clear(BackColor);
  Rectangle source=SourceBounds;
  float scale=Math.Max((float)Width/source.Width,(float)Height/source.Height);
  var target=new RectangleF((Width-source.Width*scale)/2,(Height-source.Height*scale)/2,source.Width*scale,source.Height*scale);
  graphics.InterpolationMode=InterpolationMode.HighQualityBicubic;
  graphics.DrawImage(Stadium,target,source,GraphicsUnit.Pixel);
  // A local scrim keeps menu text legible without darkening the whole stadium.
  using(var shade=new LinearGradientBrush(new Rectangle(0,0,Math.Max(2,Width),Height),Color.Transparent,Color.Transparent,0f)){
   shade.InterpolationColors=new ColorBlend{Positions=new[]{0f,.27f,.48f,1f},Colors=new[]{Color.FromArgb(235,8,17,30),Color.FromArgb(205,8,17,30),Color.FromArgb(45,8,17,30),Color.FromArgb(12,8,17,30)}};
   graphics.FillRectangle(shade,ClientRectangle);
  }
 }
 protected override void Dispose(bool disposing){if(disposing&&frame!=null){frame.Dispose();frame=null;}base.Dispose(disposing);}
}
}
