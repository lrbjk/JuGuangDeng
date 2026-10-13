param([switch]$Apply)
$ErrorActionPreference='Stop'
$sourceRoot=[System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..'))
$verifyRoot=Join-Path ([System.IO.Path]::GetTempPath()) ('RainbowLi-effect-pools-'+[Guid]::NewGuid().ToString('N'))
foreach($directory in @('Assets/RainbowLi/Scripts/Unity','Assets/RainbowLi/Editor','ProjectSettings','Packages')){New-Item -ItemType Directory -Path (Join-Path $verifyRoot $directory) -Force | Out-Null}
$assets=Join-Path $verifyRoot 'Assets/RainbowLi'
Copy-Item -LiteralPath (Join-Path $sourceRoot 'Assets/RainbowLi/Scripts/Domain') -Destination (Join-Path $assets 'Scripts/Domain') -Recurse
Copy-Item -LiteralPath (Join-Path $sourceRoot 'Assets/RainbowLi/Scripts/Unity/Data') -Destination (Join-Path $assets 'Scripts/Unity/Data') -Recurse
Copy-Item -LiteralPath (Join-Path $sourceRoot 'Assets/RainbowLi/Data') -Destination (Join-Path $assets 'Data') -Recurse
Copy-Item -LiteralPath (Join-Path $sourceRoot 'ProjectSettings/ProjectVersion.txt') -Destination (Join-Path $verifyRoot 'ProjectSettings/ProjectVersion.txt')
foreach($name in @('CardTableWorkbook.cs','EffectTableWorkbook.cs','EffectPoolWorkbookImport.cs','CardTableImportPlan.cs')){Copy-Item -LiteralPath (Join-Path $sourceRoot ('Assets/RainbowLi/Editor/'+$name)) -Destination (Join-Path $assets ('Editor/'+$name))}
foreach($name in @('EffectPoolImportChecks.cs')){Copy-Item -LiteralPath (Join-Path $PSScriptRoot $name) -Destination (Join-Path $assets ('Editor/'+$name))}
Copy-Item -LiteralPath (Join-Path $sourceRoot 'Assets/RainbowLi/Tests/EditMode/EffectPoolRuntimeChecks.cs') -Destination (Join-Path $assets 'Editor/EffectPoolRuntimeChecks.cs')
Set-Content -LiteralPath (Join-Path $verifyRoot 'Packages/manifest.json') -Value '{"dependencies":{}}' -Encoding UTF8
Set-Content -LiteralPath (Join-Path $assets 'Scripts/Unity/RainbowLi.Runtime.asmdef') -Value '{"name":"RainbowLi.Runtime","references":["RainbowLi.Domain"]}' -Encoding UTF8
Set-Content -LiteralPath (Join-Path $assets 'Editor/EffectPoolChecks.asmdef') -Value '{"name":"RainbowLi.EffectPoolChecks","references":["RainbowLi.Domain","RainbowLi.Runtime"],"includePlatforms":["Editor"]}' -Encoding UTF8
Write-Output "Verification directory: $verifyRoot"
$log=Join-Path $verifyRoot 'effect-pool.log'
$process=Start-Process -FilePath 'D:/Unity/Editor/6000.3.12f1/Editor/Unity.exe' -ArgumentList @('-batchmode','-nographics','-noUpm','-projectPath',$verifyRoot,'-executeMethod','EffectPoolImportChecks.Run','-logFile',$log) -WindowStyle Hidden -PassThru -Wait
$result=Join-Path $verifyRoot 'effect-pool-result.txt'
if(Test-Path -LiteralPath $result){Get-Content -LiteralPath $result}
if($process.ExitCode -ne 0 -or -not (Test-Path -LiteralPath $result)){Get-Content -LiteralPath $log -Tail 65;throw 'Effect pool import/verification failed; workspace assets were not changed.'}
if($Apply){
 # Only copy the validated data assets; preserve source workbooks and meta GUIDs.
 foreach($name in @('DefaultPlayerCards.asset','DefaultSequenceEffects.asset','DefaultEngravingEffects.asset','DefaultGame.asset','DefaultPlayer.asset')){Copy-Item -LiteralPath (Join-Path $assets ('Data/'+$name)) -Destination (Join-Path $sourceRoot ('Assets/RainbowLi/Data/'+$name)) -Force}
 Get-ChildItem -LiteralPath (Join-Path $assets 'Data/CardEffects') -Filter '*.asset' | ForEach-Object {Copy-Item -LiteralPath $_.FullName -Destination (Join-Path $sourceRoot ('Assets/RainbowLi/Data/CardEffects/'+$_.Name)) -Force}
 Write-Output 'Applied verified imported assets to the workspace.'
}
