using System;
using System.Linq;
using System.Drawing;
using System.Windows.Forms;
namespace Touchline {
public partial class MainForm {
 void ReferenceDetail(Control parent,DataGridView grid){var detail=new TextBox{Dock=DockStyle.Bottom,Height=130,Multiline=true,ReadOnly=true,ScrollBars=ScrollBars.Vertical,WordWrap=true,BackColor=Card,ForeColor=White,Font=new Font("Microsoft YaHei UI",11)};parent.Controls.Add(detail);grid.BringToFront();Action show=()=>{if(grid.CurrentRow==null)return;detail.Text=string.Join("\r\n",grid.CurrentRow.Cells.Cast<DataGridViewCell>().Where(c=>c.ColumnIndex==0||c.ColumnIndex==grid.Columns.Count-1).Select(c=>L10n.T(Convert.ToString(c.Value))).ToArray());};grid.SelectionChanged+=(s,e)=>show();grid.CellDoubleClick+=(s,e)=>show();show();
  foreach(DataGridViewColumn column in grid.Columns)column.DefaultCellStyle.WrapMode=DataGridViewTriState.True;
 }
}
}
