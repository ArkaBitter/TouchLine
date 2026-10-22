param([Parameter(Mandatory=$true)][string]$ResultRoot)
$ErrorActionPreference='Stop'
$projectRoot=Split-Path -Parent $PSScriptRoot
[void][Reflection.Assembly]::LoadFrom((Join-Path $projectRoot 'build\TouchlineBenchmarkCore.exe'))
[Touchline.Storage].GetField('Root').SetValue($null,(Join-Path $ResultRoot 'audit-profile'))
$serializer=[Touchline.Storage]::Json()
$decisions=0
$trainingActions=0
$spent=0
$newQuotes=0
$quoteFallbacks=0
foreach($folder in @($ResultRoot,(Join-Path $ResultRoot 'training'),(Join-Path $ResultRoot 'training-required'),(Join-Path $ResultRoot 'quote-required'))){
 if(-not (Test-Path -LiteralPath (Join-Path $folder 'complete.json'))){throw "Incomplete benchmark: $folder"}
 $inputs=Get-Content -LiteralPath (Join-Path $folder 'inputs.json') -Raw -Encoding UTF8 | ConvertFrom-Json
 $batches=Get-Content -LiteralPath (Join-Path $folder 'results.json') -Raw -Encoding UTF8 | ConvertFrom-Json
 foreach($batch in $batches){
  $scenario=$inputs|Where-Object {$_.Name -eq $batch.Category}|Select-Object -First 1
  $game=$serializer.Deserialize($scenario.Snapshot,[Touchline.Game])
  $initialFunds=($game.Clubs|Measure-Object Funds -Sum).Sum
  for($i=0;$i -lt $batch.Results.Count;$i++){
   $row=$batch.Results[$i]
   if($row.Error -or $row.ApplyError){throw "Failed decision: $($row.Club)"}
   $job=$scenario.Jobs[$i]
   $club=$game.Club($job.ClubId)
   $fixture=$null
   if($job.FixtureId){$fixture=$game.Fixtures|Where-Object {$_.Id -eq $job.FixtureId}|Select-Object -First 1}
   $decision=$serializer.Deserialize($row.DecisionJson,[Touchline.AgentDecision])
   [Touchline.LocalModelAgent]::ValidateDecision($game,$club,$decision,$scenario.Phase,$fixture)
   $before=$club.Upgrade
   $points=0
   foreach($tr in $decision.training){$points+=$tr.levels}
   $beforeQuotes=$game.Offers.Count
   [Touchline.AgentTurns]::Apply($game,$club,$decision,$scenario.Phase,$fixture)
   if($club.Upgrade -ne ($before-$points)){throw 'Incorrect training-point deduction'}
   [Touchline.Rules]::Validate($game)
   $decisions++
   $trainingActions+=$decision.training.Count
   $spent+=$points
   $newQuotes+=$game.Offers.Count-$beforeQuotes
   if($folder.EndsWith('quote-required')){if($row.Fallback){$quoteFallbacks++}elseif($game.Offers.Count -ne ($beforeQuotes+1)){throw 'Required quote was not created'}}
  }
  if([Math]::Abs(($game.Clubs|Measure-Object Funds -Sum).Sum-$initialFunds) -gt 0.000001){throw 'Funds not conserved'}
  if(@($game.Fixtures|Where-Object {$_.HomeGoals -ge 0 -or $_.AwayGoals -ge 0}).Count -gt 0){throw 'Unexpected match progression'}
 }
}
[pscustomobject]@{Passed=$true;Decisions=$decisions;TrainingActions=$trainingActions;PointsSpent=$spent;NewQuotes=$newQuotes;RequiredQuoteFallbacks=$quoteFallbacks;FundsConserved=$true;MatchesPlayed=0}|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $ResultRoot 'audit.json') -Encoding UTF8
