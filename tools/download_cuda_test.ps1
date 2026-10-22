$ErrorActionPreference='Stop'
$taskRoot=Join-Path $PSScriptRoot '../LocalAI'
$taskAsset=(Get-Content (Join-Path $taskRoot 'release-b11719.json') -Raw | ConvertFrom-Json).assets | Where-Object name -eq 'llama-b11719-bin-win-cuda-12.4-x64.zip'
$taskJobs=@();$taskCount=8;$taskChunk=[long][Math]::Ceiling($taskAsset.size/$taskCount)
for($taskIndex=0;$taskIndex -lt $taskCount;$taskIndex++){
 $taskStart=$taskIndex*$taskChunk;$taskEnd=[Math]::Min($taskAsset.size-1,($taskIndex+1)*$taskChunk-1)
 $taskPart=Join-Path $taskRoot ('cuda-part-'+$taskIndex+'.bin')
 $taskArgs=@('-sS','-L','--fail','--retry','3','--retry-all-errors','--connect-timeout','15','--max-time','900','-H','"Accept: application/octet-stream"','--range',"$taskStart-$taskEnd",'-o',('"'+$taskPart+'"'),('"'+$taskAsset.url+'?part='+$taskIndex+'"'))
 $taskProcess=Start-Process curl.exe -ArgumentList $taskArgs -WindowStyle Hidden -PassThru -RedirectStandardError (Join-Path $taskRoot ('cuda-part-'+$taskIndex+'.log'))
 $taskJobs+=@{Process=$taskProcess;Path=$taskPart;Expected=$taskEnd-$taskStart+1}
}
foreach($taskJob in $taskJobs){$taskJob.Process.WaitForExit();if($taskJob.Process.ExitCode -ne 0 -or (Get-Item -LiteralPath $taskJob.Path).Length -ne $taskJob.Expected){throw ('CUDA part download failed or range unsupported: '+$taskJob.Path)}}
$taskZip=Join-Path $taskRoot $taskAsset.name
$taskStream=[System.IO.File]::Create($taskZip)
try{foreach($taskJob in $taskJobs){$taskInput=[System.IO.File]::OpenRead($taskJob.Path);try{$taskInput.CopyTo($taskStream)}finally{$taskInput.Dispose()}}}finally{$taskStream.Dispose()}
$taskDigest='sha256:'+(Get-FileHash -LiteralPath $taskZip -Algorithm SHA256).Hash.ToLower()
if($taskDigest -ne $taskAsset.digest){throw 'CUDA runtime SHA256 mismatch'}
Write-Output ('Verified '+$taskZip+' '+$taskDigest)
Expand-Archive -LiteralPath $taskZip -DestinationPath (Join-Path $taskRoot 'runtime-cuda-test') -Force
Write-Output 'CUDA runtime extracted. Existing CPU runtime unchanged.'
