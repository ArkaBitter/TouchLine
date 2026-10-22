from pathlib import Path
root=Path(__file__).resolve().parents[1]/'src'
p=root/'MainForm.cs';s=p.read_text(encoding='utf-8');start=s.index(' static string TacticText(');end=s.index(' void SchedulePage()',start)
s=s[:start]+''' static string TacticText(string t){return RuleDescriptions.Tactic.ContainsKey(t)?RuleDescriptions.Tactic[t]:"未知战术";}
'''+s[end:]
s=s.replace(' public void Preview(Game g){game=g;page="俱乐部总览";Render();}',' public void Preview(Game g){game=g;page="俱乐部总览";Render();} public void PreviewPage(string p){page=p;Render();}')
# Replace the schedule selection block so group tables are never merged into fictitious standings.
s=s.replace('actions.Controls.Add(league);var split=', 'actions.Controls.Add(league);var groupSelect=new ComboBox{Width=180,DropDownStyle=ComboBoxStyle.DropDownList};actions.Controls.Add(groupSelect);var split=')
s=s.replace('var fs=game.Fixtures.Where(f=>f.Competition==label&&f.Stage=="小组").ToArray();','var fs=game.Fixtures.Where(f=>f.Competition==label&&f.Stage=="小组"&&(groupSelect.SelectedItem==null||f.Group==Convert.ToString(groupSelect.SelectedItem))).ToArray();')
s=s.replace('league.SelectedIndexChanged+=(s,e)=>draw();fixtures.CellDoubleClick', 'Action groups=()=>{groupSelect.Items.Clear();if(league.SelectedIndex>=3)foreach(string group in game.Fixtures.Where(f=>f.Competition==Convert.ToString(league.SelectedItem)&&f.Stage=="小组").Select(f=>f.Group).Distinct().OrderBy(x=>x))groupSelect.Items.Add(group);if(groupSelect.Items.Count>0)groupSelect.SelectedIndex=0;groupSelect.Visible=league.SelectedIndex>=3;};league.SelectedIndexChanged+=(s,e)=>{groups();draw();};groupSelect.SelectedIndexChanged+=(s,e)=>draw();groups();fixtures.CellDoubleClick')
p.write_text(s,encoding='utf-8')
p=root/'AgentRequests.cs';s=p.read_text(encoding='utf-8').replace('strength=p.Score,lineup=', 'strength=p.Score,effect=RuleDescriptions.Tactic[p.Tactic],lineup=')
s=s.replace('不要抄写球员资料。','不要抄写球员资料。每场仅一种战术，reason不可声称叠加战术或捏造规则。')
p.write_text(s,encoding='utf-8')
p=root/'Program.cs';s=p.read_text(encoding='utf-8')
s=s.replace('b.Save(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"preview.png"));','b.Save(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"preview.png"));}foreach(string page in new[]{"阵容与编队","理念与战术","赛程与积分","球员与转会","经营与荣誉","赛事统计","本地AI设置"}){f.PreviewPage(page);Application.DoEvents();using(var b=new Bitmap(f.Width,f.Height)){f.DrawToBitmap(b,new Rectangle(0,0,b.Width,b.Height));b.Save(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"preview_"+page+".png"));}')
p.write_text(s,encoding='utf-8')
print('group standings, complete tactic descriptions, multipage UI previews')
