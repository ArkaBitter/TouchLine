using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;
namespace Touchline {
// Complete a page replacement before Windows paints any intermediate empty state.
internal sealed class UiRedrawBatch : IDisposable {
 readonly Control owner;readonly IntPtr handle;
 [DllImport("user32.dll")]static extern IntPtr SendMessage(IntPtr hwnd,int msg,IntPtr wParam,IntPtr lParam);
 [DllImport("user32.dll")]static extern bool RedrawWindow(IntPtr hwnd,IntPtr rect,IntPtr region,uint flags);
 public UiRedrawBatch(Control control){owner=control;owner.SuspendLayout();if(owner.IsHandleCreated&&owner.Visible){handle=owner.Handle;SendMessage(handle,0x000B,IntPtr.Zero,IntPtr.Zero);}}
 public void Dispose(){try{if(!owner.IsDisposed)owner.ResumeLayout(true);}finally{if(handle!=IntPtr.Zero&&!owner.IsDisposed){SendMessage(handle,0x000B,new IntPtr(1),IntPtr.Zero);RedrawWindow(handle,IntPtr.Zero,IntPtr.Zero,0x0001|0x0004|0x0080|0x0100);}}}
}
}
