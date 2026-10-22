from pathlib import Path
p=Path(__file__).resolve().parents[1]/'src/Agents.cs'
s=p.read_text(encoding='utf-8')
start=s.index('  var squad=g.Squad(c.Id)')
end=s.index('string last="";',start)+len('string last="";')
s=s[:start]+'''  var spec=AgentRequest.Build(g,c,phase,fixture);var ids=spec.Codes;
  var messages=new List<object>{new{role="system",content=spec.System},new{role="user",content=spec.Prompt}};string last="";'''+s[end:]
s=s.replace('Request(messages,ct)','Request(messages,spec.Schema,ct)')
s=s.replace('Func<string,string> map=x=>ids.ContainsKey(x)?ids[x]:x;', '''if(phase=="赛前"&&d.plan>=0&&d.plan<spec.Plans.Count){d.lineup=spec.Plans[d.plan].Lineup;d.tactic=spec.Plans[d.plan].Tactic;}
    Func<string,string> map=x=>ids.ContainsKey(x)?ids[x]:x;''')
s=s.replace('Request(List<object> messages,CancellationToken ct)','Request(List<object> messages,object schema,CancellationToken ct)')
s=s.replace('max_tokens=1300,response_format=new{type="json_object"}','max_tokens=500,response_format=new{type="json_object",schema=schema}')
s=s.replace('(o.SellerId==c.Id||(o.PlayerDriven&&o.BuyerId==c.Id))','((!o.PlayerDriven&&o.SellerId==c.Id)||(o.PlayerDriven&&o.BuyerId==c.Id))')
p.write_text(s,encoding='utf-8')
print('phase-specific local agent schemas enabled')
