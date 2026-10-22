param([ValidatePattern('^[A-Za-z0-9_-]+\.exe$')][string]$OutputName='TouchlineManager.exe')
$ErrorActionPreference='Stop'
$projectRoot=$PSScriptRoot
$buildRoot=Join-Path $projectRoot 'build'
New-Item -ItemType Directory -Force -Path $buildRoot | Out-Null
$compiler=Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319/csc.exe'
if(-not (Test-Path -LiteralPath $compiler)){throw '需要 Windows .NET Framework 4.x 编译器。'}
Push-Location (Join-Path $projectRoot 'src')
try {
 $compileArgs=@('/nologo','/utf8output','/codepage:65001','/target:winexe','/platform:x64','/optimize+','/debug+','/out:..\build\TouchlineManager.exe','/r:System.Windows.Forms.dll','/r:System.Drawing.dll','/r:System.Web.Extensions.dll','/r:System.Net.Http.dll','/r:System.Security.dll','/resource:..\data\en.json,Touchline.English','/resource:..\assets\main-menu.png,Touchline.MainMenuBackground')
 $compileArgs=$compileArgs | ForEach-Object {if($_ -like '/out:*'){ '/out:..\build\'+$OutputName }else{$_}}
 $compileArgs+=Get-ChildItem -Filter '*.cs' -File | Select-Object -ExpandProperty Name
 & $compiler $compileArgs
 if($LASTEXITCODE -ne 0){throw '编译失败。'}
} finally {Pop-Location}
Write-Output (Join-Path $buildRoot $OutputName)
