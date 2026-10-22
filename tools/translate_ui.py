import json,time,urllib.request,re
from pathlib import Path
root=Path(__file__).resolve().parents[1]
source=json.loads((root/'data/localization_source.json').read_text(encoding='utf-8'))
out=root/'data/en.json'
translated=json.loads(out.read_text(encoding='utf-8-sig')) if out.exists() else {}
for attempt in range(120):
 try:
  urllib.request.urlopen('http://127.0.0.1:17845/health',timeout=2);break
 except Exception: time.sleep(1)
pending=[s for s in source if s not in translated]
for start in range(0,len(pending),12):
 batch=pending[start:start+12]
 schema={'type':'object','properties':{str(i):{'type':'string'} for i in range(len(batch))},'required':[str(i) for i in range(len(batch))],'additionalProperties':False}
 request={'model':'touchline-local','messages':[{'role':'system','content':'Translate Chinese football manager game UI text into concise natural English. Return JSON with the same numeric keys. Preserve numbers, percentages, code names and punctuation. Translate fragments as fragments. Do not explain.'},{'role':'user','content':json.dumps(dict(enumerate(batch)),ensure_ascii=False)}], 'temperature':0,'max_tokens':1800,'response_format':{'type':'json_object','schema':schema}}
 try:
  req=urllib.request.Request('http://127.0.0.1:17845/v1/chat/completions',data=json.dumps(request).encode(),headers={'Content-Type':'application/json'})
  result=json.load(urllib.request.urlopen(req,timeout=240))['choices'][0]['message']['content']
  values=json.loads(result)
  for i,s in enumerate(batch):
   value=values.get(str(i),'')
   if value and not re.search('[\u4e00-\u9fff]',value):translated[s]=value
 except Exception as ex:print('batch failed',start,str(ex),flush=True)
 out.write_text(json.dumps(translated,ensure_ascii=False,indent=2),encoding='utf-8')
 print('translated',len(translated),'/',len(source),flush=True)
