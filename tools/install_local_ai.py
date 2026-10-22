"""Download official llama.cpp CPU runtime and Qwen weights; never runs downloaded installers."""
from pathlib import Path
import urllib.request,json,hashlib,zipfile,time,sys
root=Path(__file__).resolve().parents[1];target=root/'LocalAI';target.mkdir(exist_ok=True)
def request(url):
 return urllib.request.Request(url,headers={'User-Agent':'TouchlineManager/0.1'})
def meta(url):
 with urllib.request.urlopen(request(url),timeout=45) as f:return json.load(f)
def download(url,path,size=0,sha=''):
 if path.exists() and (not size or path.stat().st_size==size):
  if not sha or hashlib.sha256(path.read_bytes()).hexdigest()==sha:return
 part=path.with_suffix(path.suffix+'.part');done=part.stat().st_size if part.exists() else 0
 req=request(url)
 if done:req.add_header('Range','bytes='+str(done)+'-')
 with urllib.request.urlopen(req,timeout=120) as response:
  if response.status!=206:done=0
  with part.open('ab' if done else 'wb') as f:
   last=time.time()
   while True:
    data=response.read(4*1024*1024)
    if not data:break
    f.write(data);done+=len(data)
    if time.time()-last>12:print(path.name,round(done/1024**2),'MiB',flush=True);last=time.time()
 if size and part.stat().st_size!=size:raise RuntimeError('Incomplete download '+str(path))
 if sha:
  h=hashlib.sha256()
  with part.open('rb') as f:
   for block in iter(lambda:f.read(8*1024*1024),b''):h.update(block)
  if h.hexdigest()!=sha:raise RuntimeError('SHA256 mismatch '+str(path))
 part.replace(path)
release=meta('https://api.github.com/repos/ggml-org/llama.cpp/releases/latest')
if not any(a['name'].endswith('bin-win-cpu-x64.zip') for a in release['assets']):
 release=next(r for r in meta('https://api.github.com/repos/ggml-org/llama.cpp/releases?per_page=20') if any(a['name'].endswith('bin-win-cpu-x64.zip') for a in r['assets']))
asset=next(a for a in release['assets'] if a['name'].endswith('bin-win-cpu-x64.zip'))
archive=target/asset['name'];digest=asset.get('digest') or ''
download(asset['browser_download_url'],archive,asset['size'],digest.removeprefix('sha256:'))
runtime=target/'runtime';runtime.mkdir(exist_ok=True)
with zipfile.ZipFile(archive) as z:
 for entry in z.infolist():
  dest=(runtime/entry.filename).resolve()
  if not dest.is_relative_to(runtime.resolve()):raise RuntimeError('Unsafe archive entry')
 z.extractall(runtime)
repo='Qwen/Qwen2.5-1.5B-Instruct-GGUF'
files=meta('https://huggingface.co/api/models/'+repo+'/tree/main?recursive=true')
model=next(a for a in files if a['path'].lower()=='qwen2.5-1.5b-instruct-q4_k_m.gguf')
sha=model.get('lfs',{}).get('oid','');modelpath=target/model['path']
download('https://huggingface.co/'+repo+'/resolve/main/'+model['path'],modelpath,model.get('size',0),sha)
download('https://raw.githubusercontent.com/ggml-org/llama.cpp/master/LICENSE',target/'LICENSE-llama.cpp.txt')
download('https://huggingface.co/'+repo+'/raw/main/LICENSE',target/'LICENSE-Qwen.txt')
info={'runtime_release':release['tag_name'],'runtime_url':asset['browser_download_url'],'runtime_sha256':hashlib.sha256(archive.read_bytes()).hexdigest(),'model_repo':repo,'model_file':modelpath.name,'model_sha256':sha,'model_bytes':modelpath.stat().st_size}
(target/'manifest.json').write_text(json.dumps(info,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps(info),flush=True)
