param()
$ErrorActionPreference = 'Stop'
$sourceRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..')).Replace('\', '/')
$verifyRoot = Join-Path ([System.IO.Path]::GetTempPath()) ('RainbowLi-runtime-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $verifyRoot | Out-Null
$project = @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType><TargetFramework>net9.0</TargetFramework>
    <EnableDefaultCompileItems>false</EnableDefaultCompileItems>
    <StartupObject>RuntimeChecksRunner</StartupObject><LangVersion>9.0</LangVersion>
  </PropertyGroup>
  <ItemGroup>
    <Compile Include="$sourceRoot/Assets/RainbowLi/Scripts/Domain/**/*.cs" />
    <Compile Include="$sourceRoot/Assets/RainbowLi/Verification~/RuleChecks.cs" />
    <Compile Include="$sourceRoot/Assets/RainbowLi/Verification~/ContentChecks.cs" />
    <Compile Include="$sourceRoot/Assets/RainbowLi/Verification~/TableEffectChecks.cs" />
    <Compile Include="$sourceRoot/Assets/RainbowLi/Verification~/CardInstanceChecks.cs" />
    <Compile Include="$sourceRoot/Assets/RainbowLi/Verification~/SettlementChecks.cs" />
    <Compile Include="$sourceRoot/Assets/RainbowLi/Tests/EditMode/AdvancedRuntimeChecks.cs" />
    <Compile Include="$sourceRoot/Assets/RainbowLi/Tests/EditMode/EffectPoolRuntimeChecks.cs" />
    <Compile Include="Runner.cs" />
  </ItemGroup>
</Project>
"@
$runner = @'
using System;
public static class RuntimeChecksRunner {
    public static void Main() {
        int failures = 0;
        foreach (Action suite in new Action[] { RuleChecks.Main, ContentChecks.Main, TableEffectChecks.Main, CardInstanceChecks.Main, SettlementChecks.Main, AdvancedRuntimeChecks.Main, EffectPoolRuntimeChecks.Main }) {
            try { suite(); }
            catch (Exception error) { Console.WriteLine(error); failures++; }
        }
        if (failures > 0) Environment.Exit(1);
    }
}
'@
Set-Content -LiteralPath (Join-Path $verifyRoot 'RuntimeChecks.csproj') -Value $project -Encoding UTF8
Set-Content -LiteralPath (Join-Path $verifyRoot 'Runner.cs') -Value $runner -Encoding UTF8
Write-Output "Verification directory: $verifyRoot"
dotnet run --project (Join-Path $verifyRoot 'RuntimeChecks.csproj') --verbosity quiet
if ($LASTEXITCODE -ne 0) { throw "Runtime checks failed (exit code $LASTEXITCODE)." }
