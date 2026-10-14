$ErrorActionPreference='Stop'
$sourceRoot=[System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..'))
$verifyRoot=Join-Path ([System.IO.Path]::GetTempPath()) ('RainbowLi-enemy-content-'+[Guid]::NewGuid().ToString('N'))
foreach($directory in @('Assets/RainbowLi/Scripts/Unity','Assets/Editor','ProjectSettings','Packages')){New-Item -ItemType Directory -Path (Join-Path $verifyRoot $directory) -Force | Out-Null}
$assets=Join-Path $verifyRoot 'Assets/RainbowLi'
Copy-Item -LiteralPath (Join-Path $sourceRoot 'Assets/RainbowLi/Scripts/Domain') -Destination (Join-Path $assets 'Scripts/Domain') -Recurse
Copy-Item -LiteralPath (Join-Path $sourceRoot 'Assets/RainbowLi/Scripts/Unity/Data') -Destination (Join-Path $assets 'Scripts/Unity/Data') -Recurse
Copy-Item -LiteralPath (Join-Path $sourceRoot 'Assets/RainbowLi/Data') -Destination (Join-Path $assets 'Data') -Recurse
Copy-Item -LiteralPath (Join-Path $sourceRoot 'ProjectSettings/ProjectVersion.txt') -Destination (Join-Path $verifyRoot 'ProjectSettings/ProjectVersion.txt')
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'EnemyContentNativeChecks.cs') -Destination (Join-Path $verifyRoot 'Assets/Editor/EnemyContentNativeChecks.cs')
Copy-Item -LiteralPath (Join-Path $sourceRoot 'Assets/RainbowLi/Tests/EditMode/EnemyEncounterChecks.cs') -Destination (Join-Path $verifyRoot 'Assets/Editor/EnemyEncounterChecks.cs')
Set-Content -LiteralPath (Join-Path $verifyRoot 'Packages/manifest.json') -Value '{"dependencies":{}}' -Encoding UTF8
Set-Content -LiteralPath (Join-Path $assets 'Scripts/Unity/RainbowLi.Runtime.asmdef') -Value '{"name":"RainbowLi.Runtime","references":["RainbowLi.Domain"]}' -Encoding UTF8
Set-Content -LiteralPath (Join-Path $verifyRoot 'Assets/Editor/EnemyContentChecks.asmdef') -Value '{"name":"RainbowLi.EnemyContentChecks","references":["RainbowLi.Domain","RainbowLi.Runtime"],"includePlatforms":["Editor"]}' -Encoding UTF8
Write-Output "Verification directory: $verifyRoot"
$log=Join-Path $verifyRoot 'enemy-content.log'
$process=Start-Process -FilePath 'D:/Unity/Editor/6000.3.12f1/Editor/Unity.exe' -ArgumentList @('-batchmode','-nographics','-noUpm','-projectPath',$verifyRoot,'-executeMethod','EnemyContentNativeChecks.Run','-logFile',$log) -WindowStyle Hidden -PassThru -Wait
$result=Join-Path $verifyRoot 'enemy-content-result.txt'
if(Test-Path -LiteralPath $result){Get-Content -LiteralPath $result}
if($process.ExitCode -ne 0 -or -not (Test-Path -LiteralPath $result)){Get-Content -LiteralPath $log -Tail 65;throw 'Enemy/BOSS native verification failed.'}
