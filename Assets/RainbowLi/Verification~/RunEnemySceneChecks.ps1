$ErrorActionPreference='Stop'
$sourceRoot=[System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..'))
$verifyRoot=Join-Path ([System.IO.Path]::GetTempPath()) ('RainbowLi-enemy-scene-'+[Guid]::NewGuid().ToString('N'))
foreach($directory in @('Assets/RainbowLi','Assets/Editor','ProjectSettings','Packages')){New-Item -ItemType Directory -Path (Join-Path $verifyRoot $directory) -Force | Out-Null}
$assets=Join-Path $verifyRoot 'Assets/RainbowLi'
foreach($directory in @('Scripts','Data','Art','Prefabs','Resources','Scenes')){Copy-Item -LiteralPath (Join-Path $sourceRoot ('Assets/RainbowLi/'+$directory)) -Destination (Join-Path $assets $directory) -Recurse;Copy-Item -LiteralPath (Join-Path $sourceRoot ('Assets/RainbowLi/'+$directory+'.meta')) -Destination (Join-Path $assets ($directory+'.meta'))}
Get-ChildItem -LiteralPath (Join-Path $sourceRoot 'ProjectSettings') -File | Copy-Item -Destination (Join-Path $verifyRoot 'ProjectSettings')
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'EnemyContentSceneChecks.cs') -Destination (Join-Path $verifyRoot 'Assets/Editor/EnemyContentSceneChecks.cs')
# Compile cached runtime package sources in the disposable project. This avoids
# the local UPM server's unresolved project-path error and preserves script GUIDs.
New-Item -ItemType Directory -Path (Join-Path $verifyRoot 'Assets/VerificationPackages') -Force | Out-Null
foreach($package in @('com.unity.ugui','com.unity.inputsystem')){$cache=Get-ChildItem -LiteralPath (Join-Path $sourceRoot 'Library/PackageCache') -Directory | Where-Object Name -Like ($package+'@*') | Select-Object -First 1;$runtime=if($package -eq 'com.unity.inputsystem'){'InputSystem'}else{'Runtime'};Copy-Item -LiteralPath (Join-Path $cache.FullName $runtime) -Destination (Join-Path $verifyRoot ('Assets/VerificationPackages/'+$package)) -Recurse}
Get-ChildItem -LiteralPath (Join-Path $verifyRoot 'Assets/VerificationPackages') -Filter '*.asmdef' -Recurse | ForEach-Object {$definition=Get-Content -Raw -LiteralPath $_.FullName|ConvertFrom-Json;if($definition.PSObject.Properties['references']){$definition.references=@($definition.references|Where-Object {$_ -notlike 'UnityEngine.*Module'})};Set-Content -LiteralPath $_.FullName -Value ($definition|ConvertTo-Json -Depth 10) -Encoding UTF8}
Set-Content -LiteralPath (Join-Path $verifyRoot 'Packages/manifest.json') -Value '{"dependencies":{}}' -Encoding UTF8
Set-Content -LiteralPath (Join-Path $verifyRoot 'Assets/Editor/EnemySceneChecks.asmdef') -Value '{"name":"RainbowLi.EnemySceneChecks","references":["RainbowLi.Domain","RainbowLi.Runtime","UnityEngine.UI"],"includePlatforms":["Editor"]}' -Encoding UTF8
Write-Output "Verification directory: $verifyRoot"
$log=Join-Path $verifyRoot 'enemy-scene.log'
$process=Start-Process -FilePath 'D:/Unity/Editor/6000.3.12f1/Editor/Unity.exe' -ArgumentList @('-batchmode','-nographics','-noUpm','-projectPath',$verifyRoot,'-executeMethod','EnemyContentSceneChecks.Run','-logFile',$log) -WindowStyle Hidden -PassThru -Wait
$result=Join-Path $verifyRoot 'enemy-scene-result.txt'
if(Test-Path -LiteralPath $result){Get-Content -LiteralPath $result}
if($process.ExitCode -ne 0 -or -not (Test-Path -LiteralPath $result)){Get-Content -LiteralPath $log -Tail 60;throw 'Enemy/BOSS scene verification failed.'}
