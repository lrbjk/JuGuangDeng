param()
$ErrorActionPreference = 'Stop'
$sourceRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..'))
$unityProject = Join-Path $sourceRoot 'Assembly-CSharp.csproj'
if (-not (Test-Path -LiteralPath $unityProject)) {
    throw 'Generate Assembly-CSharp.csproj with Unity before running session checks.'
}
[xml]$unityReferences = Get-Content -Raw -LiteralPath $unityProject
$core = $unityReferences.SelectSingleNode('//Reference[@Include="UnityEngine.CoreModule"]')
if ($null -eq $core) { throw 'UnityEngine.CoreModule reference was not found.' }
$hint = [string]$core.HintPath
if (-not [System.IO.Path]::IsPathRooted($hint)) { $hint = Join-Path $sourceRoot $hint }
if (-not (Test-Path -LiteralPath $hint)) { throw "Unity assembly was not found: $hint" }
$escapedHint = [System.Security.SecurityElement]::Escape($hint)
$compileRoot = [System.Security.SecurityElement]::Escape($sourceRoot.Replace('\', '/'))
$verifyRoot = Join-Path ([System.IO.Path]::GetTempPath()) ('RainbowLi-session-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $verifyRoot | Out-Null
$project = @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType><TargetFramework>net9.0</TargetFramework>
    <EnableDefaultCompileItems>false</EnableDefaultCompileItems><LangVersion>9.0</LangVersion>
    <StartupObject>SettlementSessionChecks</StartupObject>
  </PropertyGroup>
  <ItemGroup>
    <Compile Include="$compileRoot/Assets/RainbowLi/Scripts/ContainmentGame.cs" />
    <Compile Include="$compileRoot/Assets/RainbowLi/Scripts/Domain/**/*.cs" />
    <Compile Include="$compileRoot/Assets/RainbowLi/Scripts/Unity/Data/**/*.cs" />
    <Compile Include="$compileRoot/Assets/RainbowLi/Scripts/Unity/Systems/**/*.cs" />
    <Compile Include="$compileRoot/Assets/RainbowLi/Verification~/SettlementSessionChecks.cs" />
    <Reference Include="UnityEngine.CoreModule"><HintPath>$escapedHint</HintPath></Reference>
  </ItemGroup>
</Project>
"@
$projectPath = Join-Path $verifyRoot 'SessionChecks.csproj'
Set-Content -LiteralPath $projectPath -Value $project -Encoding UTF8
Write-Output "Verification directory: $verifyRoot"
dotnet run --project $projectPath --verbosity quiet
if ($LASTEXITCODE -ne 0) { throw "Session settlement checks failed (exit code $LASTEXITCODE)." }
