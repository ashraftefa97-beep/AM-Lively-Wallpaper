[CmdletBinding()]
param(
 [Parameter(Mandatory=$true)][string]$OriginalPortable,
 [string]$NuGet = 'nuget.exe',
 [string]$MSBuild = 'msbuild.exe',
 [string]$Inno = "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe",
 [switch]$Installer
)
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
& "$PSScriptRoot\Get-UpstreamMedia.ps1"
$OriginalPortable=(Resolve-Path $OriginalPortable).Path
if(!(Test-Path "$OriginalPortable\livelywpf.exe")){throw 'Point OriginalPortable to the extracted OFFICIAL v0.9.6.0 portable directory.'}
$v=[Diagnostics.FileVersionInfo]::GetVersionInfo("$OriginalPortable\livelywpf.exe")
$assemblyVersion=[Reflection.AssemblyName]::GetAssemblyName("$OriginalPortable\livelywpf.exe").Version.ToString()
if($assemblyVersion -ne '0.9.6.0'){throw "Wrong upstream version: $assemblyVersion"}
if(!(Test-Path "$OriginalPortable\external\cef\LivelyCefSharp.exe")){throw 'Original CefSharp runtime missing. Do not replace it with a newer release.'}
$cef="$OriginalPortable\external\cef\CefSharp.Core.dll"
if(!(Test-Path $cef)){throw 'CefSharp.Core.dll missing.'}
if(([Diagnostics.FileVersionInfo]::GetVersionInfo($cef).FileVersion) -notlike '81.3.100*'){throw 'Expected upstream CefSharp 81.3.100; refuse a runtime upgrade.'}
& $NuGet restore "$root\src\livelywpf\livelywpf.sln" -NonInteractive
if($LASTEXITCODE -ne 0){throw 'NuGet restore failed.'}
New-Item -ItemType Directory -Force "$root\dist" | Out-Null
& $MSBuild "$root\src\livelywpf\livelywpf.sln" /t:Rebuild /p:Configuration=Release /p:Platform=x86 /m "/bl:$root\dist\build.binlog"
if($LASTEXITCODE -ne 0){throw 'Build failed. No installer was created.'}
$compiler=Join-Path (Split-Path (Get-Command $MSBuild).Source) 'Roslyn\csc.exe'
& "$PSScriptRoot\Test-Windows.ps1" -Compiler $compiler
$stage="$root\dist\portable"
if(Test-Path $stage){throw 'dist\portable already exists. Move it away before rebuilding to avoid mixing binaries.'}
New-Item -ItemType Directory -Force $stage | Out-Null
# Keep every upstream external runtime byte-for-byte; do not overwrite these with new versions.
Copy-Item "$OriginalPortable\external" $stage -Recurse
foreach($folder in @('wallpapers')) {if(Test-Path "$OriginalPortable\$folder"){Copy-Item "$OriginalPortable\$folder" $stage -Recurse}}
Copy-Item "$root\src\livelywpf\livelywpf\bin\x86\Release\*" $stage -Recurse -Force
Copy-Item "$root\src\livelywpf\livelySubProcess\bin\Release\AM-Lively-Helper.exe*" $stage -Force
Copy-Item "$root\License.txt" $stage
Copy-Item "$root\NOTICE-AM.md" $stage
# Compare all supplied external runtimes against their exact upstream bytes.
Get-ChildItem "$OriginalPortable\external" -File -Recurse | ForEach-Object {
 $relative=$_.FullName.Substring($OriginalPortable.Length+1)
 if((Get-FileHash $_.FullName -Algorithm SHA256).Hash -ne (Get-FileHash (Join-Path $stage $relative) -Algorithm SHA256).Hash){throw "Runtime altered: $relative"}
}
Get-ChildItem "$stage\external" -File -Recurse | Get-FileHash -Algorithm SHA256 | Export-Csv "$root\dist\runtime-sha256.csv" -NoTypeInformation
Write-Host "Portable build: $stage"
if($Installer){
 if(!(Test-Path $Inno)){throw 'Inno Setup 6.2.2 compiler not found. Pass -Inno with its exact path.'}
 & $Inno "$PSScriptRoot\AM-Lively.iss"
 if($LASTEXITCODE -ne 0){throw 'Installer compilation failed.'}
 Write-Host "Installer: $root\dist\AM-Lively-Wallpaper-Setup-x86.exe"
}
