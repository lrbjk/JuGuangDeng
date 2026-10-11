param([string]$UnityEditor)
$ErrorActionPreference = 'Stop'
$sourceRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..'))
if ([string]::IsNullOrWhiteSpace($UnityEditor)) {
    [xml]$references = Get-Content -Raw -LiteralPath (Join-Path $sourceRoot 'Assembly-CSharp.csproj')
    $core = [string]$references.SelectSingleNode('//Reference[@Include="UnityEngine.CoreModule"]').HintPath
    $editorDirectory = [System.IO.Path]::GetFullPath((Join-Path ([System.IO.Path]::GetDirectoryName($core)) '../../..'))
    $UnityEditor = Join-Path $editorDirectory 'Unity.exe'
}
if (-not (Test-Path -LiteralPath $UnityEditor)) { throw 'Pass -UnityEditor with the installed Unity.exe path.' }
$verifyRoot = Join-Path ([System.IO.Path]::GetTempPath()) ('RainbowLi-native-core-' + [Guid]::NewGuid().ToString('N'))
foreach ($directory in @('Assets/RainbowLi/Scripts/Unity', 'Assets/Editor', 'ProjectSettings', 'Packages')) {
    New-Item -ItemType Directory -Path (Join-Path $verifyRoot $directory) -Force | Out-Null
}
$assets = Join-Path $verifyRoot 'Assets/RainbowLi'
Copy-Item -LiteralPath (Join-Path $sourceRoot 'Assets/RainbowLi/Scripts/Domain') -Destination (Join-Path $assets 'Scripts/Domain') -Recurse
Copy-Item -LiteralPath (Join-Path $sourceRoot 'Assets/RainbowLi/Scripts/Unity/Data') -Destination (Join-Path $assets 'Scripts/Unity/Data') -Recurse
New-Item -ItemType Directory -Path (Join-Path $assets 'Scripts/Unity/Systems') | Out-Null
Get-ChildItem -LiteralPath (Join-Path $sourceRoot 'Assets/RainbowLi/Scripts/Unity/Systems') -File |
    Where-Object { $_.Name -ne 'BattleModule.cs' -and $_.Name -ne 'BattleModule.cs.meta' } |
    Copy-Item -Destination (Join-Path $assets 'Scripts/Unity/Systems')
Copy-Item -LiteralPath (Join-Path $sourceRoot 'Assets/RainbowLi/Data') -Destination (Join-Path $assets 'Data') -Recurse
Copy-Item -LiteralPath (Join-Path $sourceRoot 'ProjectSettings/ProjectVersion.txt') -Destination (Join-Path $verifyRoot 'ProjectSettings/ProjectVersion.txt')
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'NativeCoreChecks.cs') -Destination (Join-Path $verifyRoot 'Assets/Editor/NativeCoreChecks.cs')
Copy-Item -LiteralPath (Join-Path $sourceRoot 'Assets/RainbowLi/Tests/EditMode/AdvancedRuntimeChecks.cs') -Destination (Join-Path $verifyRoot 'Assets/Editor/AdvancedRuntimeChecks.cs')
Set-Content -LiteralPath (Join-Path $verifyRoot 'Packages/manifest.json') -Value '{"dependencies":{}}' -Encoding UTF8
Set-Content -LiteralPath (Join-Path $assets 'Scripts/Unity/RainbowLi.Runtime.asmdef') -Value '{"name":"RainbowLi.Runtime","references":["RainbowLi.Domain"]}' -Encoding UTF8
Set-Content -LiteralPath (Join-Path $verifyRoot 'Assets/Editor/NativeChecks.asmdef') -Value '{"name":"RainbowLi.NativeChecks","references":["RainbowLi.Domain","RainbowLi.Runtime"],"includePlatforms":["Editor"]}' -Encoding UTF8
Write-Output "Verification directory: $verifyRoot"
$log = Join-Path $verifyRoot 'native.log'
$process = Start-Process -FilePath $UnityEditor -ArgumentList @('-batchmode','-nographics','-noUpm','-projectPath',$verifyRoot,'-executeMethod','NativeCoreChecks.Run','-logFile',$log) -WindowStyle Hidden -PassThru -Wait
$result = Join-Path $verifyRoot 'native-result.txt'
if (Test-Path -LiteralPath $result) { Get-Content -LiteralPath $result }
if ($process.ExitCode -ne 0 -or -not (Test-Path -LiteralPath $result)) {
    Get-Content -LiteralPath $log -Tail 50
    throw 'Native Unity core verification failed.'
}
