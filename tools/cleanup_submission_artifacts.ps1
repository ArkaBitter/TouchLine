$ErrorActionPreference='Stop'
$taskRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..')).TrimEnd('\')
$taskKeepHash=@{}
foreach($relative in @('build\TouchlineManager.exe','LocalAI\qwen2.5-1.5b-instruct-q4_k_m.gguf','触线足球经理项目报告_最新版.docx')){
 $taskKeepHash[$relative]=(Get-FileHash -LiteralPath (Join-Path $taskRoot $relative) -Algorithm SHA256).Hash
}
$taskTargets=@()
$taskTargets+=Get-ChildItem -LiteralPath (Join-Path $taskRoot 'build') -File | Where-Object {$_.Name -ne 'TouchlineManager.exe'}
foreach($relative in @('build\report-preview','build\report-update-20261021','build\report-format-20261022','触线足球经理项目报告.docx','LocalAI\llama-b11719-bin-win-cpu-x64.zip','LocalAI\llama-b11719-bin-win-cuda-12.4-x64.zip','LocalAI\release-b11719.json')){
 $taskPath=Join-Path $taskRoot $relative
 if(Test-Path -LiteralPath $taskPath){$taskTargets+=Get-Item -LiteralPath $taskPath}
}
$taskTargets+=Get-ChildItem -LiteralPath (Join-Path $taskRoot 'LocalAI') -File -Filter '*.log'
$taskTargets+=Get-ChildItem -LiteralPath (Join-Path $taskRoot 'build\test-results') -File -Recurse | Where-Object {$_.Extension -in @('.log','.tmsave','.bak','.tmp','.jsonl','.png','.1')}
$taskRecords=@()
foreach($target in $taskTargets){
 $taskAbsolute=[IO.Path]::GetFullPath($target.FullName)
 if(-not $taskAbsolute.StartsWith($taskRoot+'\',[StringComparison]::OrdinalIgnoreCase)){throw "清理路径越界: $taskAbsolute"}
 $taskBytes=if($target.PSIsContainer){(Get-ChildItem -LiteralPath $taskAbsolute -File -Recurse | Measure-Object Length -Sum).Sum}else{$target.Length}
 $taskRecords+=[pscustomobject]@{Path=$taskAbsolute.Substring($taskRoot.Length+1);Bytes=$taskBytes}
 Remove-Item -LiteralPath $taskAbsolute -Force -Recurse
}
foreach($relative in $taskKeepHash.Keys){
 if((Get-FileHash -LiteralPath (Join-Path $taskRoot $relative) -Algorithm SHA256).Hash -ne $taskKeepHash[$relative]){throw "保留文件发生变化: $relative"}
}
[pscustomobject]@{DeletedItems=$taskRecords;DeletedBytes=($taskRecords|Measure-Object Bytes -Sum).Sum;PreservedSHA256=$taskKeepHash;Verified=$true}|ConvertTo-Json -Depth 5|Set-Content -LiteralPath (Join-Path $taskRoot '清理记录.json') -Encoding UTF8
Write-Output ('删除总字节数: '+($taskRecords|Measure-Object Bytes -Sum).Sum)
