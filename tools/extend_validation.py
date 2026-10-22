from pathlib import Path
root=Path(__file__).resolve().parents[1]/'src'
p=root/'Program.cs';s=p.read_text(encoding='utf-8')
anchor='  bool rejected=false;try{LocalModelAgent.CheckEndpoint'
insert='''  for(int week=1;week<=18;week++){
   while(true){Competitions.Progress(full);var next=Competitions.Next(full);if(next==null)break;
    if(full.Squad(next.Home).Count(p=>p.Injury==0)<10||full.Squad(next.Away).Count(p=>p.Injury==0)<10){next.HomeGoals=0;next.AwayGoals=3;continue;}
    Football.Auto(full,full.Club(next.Home));Football.Auto(full,full.Club(next.Away));Football.Begin(full,next);while(full.Live.Round<45)Football.Step(full);Football.Finish(full);
   }SeasonFlow.WeekEnd(full);
  }
  Check(full.Fixtures.Count==734&&full.Competitions.Count==8&&full.Competitions.All(c=>c.Completed),"season two all five competitions + real injuries/fatigue");
  Check(full.Fixtures.Count(f=>f.Competition=="欧冠")==73&&full.Fixtures.Count(f=>f.Competition=="欧联")==73,"each Europe tournament 73 matches");
  Check(full.Players.All(p=>p.Appearances<=p.Available),"available matches track injury and substitutions");
  Fixture substitutionFixture=g.Fixtures.First(f=>!f.Played);Club sh=g.Club(substitutionFixture.Home),sa=g.Club(substitutionFixture.Away);
  foreach(var pp in g.Players)pp.Injury=0;Football.Auto(g,sh);Football.Auto(g,sa);Football.Begin(g,substitutionFixture);
  Player reserve=g.Squad(sh.Id).First(p=>!g.Live.UsedHome.Contains(p.Id));string off=g.Live.HomeLineup[0];Football.Substitute(g,true,0,reserve.Id);Check(g.Live.HomeSubs==1&&g.Live.Off.Contains(off),"manual substitution tracked");
  bool reentry=false;try{Football.Substitute(g,true,1,off);}catch{reentry=true;}Check(reentry,"substituted player cannot return");
  var restored=Storage.Clone(g);while(g.Live.Round<45)Football.Step(g);Football.Finish(g);Check(reserve.Appearances>0&&g.Players.First(p=>p.Id==off).Appearances>0,"both substituted players receive appearance");
'''
assert anchor in s;s=s.replace(anchor,insert+anchor)
# Expand real-model integration to managerial and match-time decisions, keeping all numerical tests separate.
needle='modelCheck=d.reason;File.WriteAllText'
replacement='''modelCheck=d.reason;
  var d2=agent.Decide(target,target.Club(f.Away),"赛前",f,CancellationToken.None).GetAwaiter().GetResult();AgentTurns.Apply(target,target.Club(f.Away),d2,"赛前",f);Football.Begin(target,f);for(int z=0;z<22;z++)Football.Step(target);
  var half=agent.Decide(target,target.Club(f.Home),"中场",f,CancellationToken.None).GetAwaiter().GetResult();AgentTurns.Apply(target,target.Club(f.Home),half,"中场",f);Check(half.tactic!="","real model halftime");target.Live=null;
  var renew=agent.Decide(target,target.Club(f.Home),"续约",null,CancellationToken.None).GetAwaiter().GetResult();Check(renew.release.Length<=target.Squad(f.Home).Count()-10,"real model renewal");
  target.Phase="需求转会";var window=agent.Decide(target,target.Club(f.Home),"需求转会",null,CancellationToken.None).GetAwaiter().GetResult();AgentTurns.Apply(target,target.Club(f.Home),window,"需求转会",null);Check(window.offers.Count<=3,"real model market actions");
  File.WriteAllText(Path.Combine(root,"test-results","local-manager-actions.json"),Storage.Encode(new{Home=d,Away=d2,HalfTime=half,Renewal=renew,Market=window}),Encoding.UTF8);
  File.WriteAllText'''
assert needle in s;s=s.replace(needle,replacement)
p.write_text(s,encoding='utf-8')
print('extended second season and actual local manager checks')
