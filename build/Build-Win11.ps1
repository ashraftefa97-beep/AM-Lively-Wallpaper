[CmdletBinding()]
param(
 [Parameter(Mandatory=$true)][string]$OriginalPortable,
 [Parameter(Mandatory=$true)][string]$NuGet,
 [Parameter(Mandatory=$true)][string]$MSBuild,
 [Parameter(Mandatory=$true)][string]$Inno,
 [switch]$Installer
)
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
# Legacy wallpaper runtime stays exactly pinned and unchanged; recompiles source plus tests.
& "$PSScriptRoot\Build-Windows.ps1" -OriginalPortable $OriginalPortable -NuGet $NuGet -MSBuild $MSBuild -Inno $Inno
if (!(Test-Path "$root\dist\portable\AM-Lively-Wallpaper.exe")) {
 throw 'Windows 11 desktop executable is missing after build.'
}
if ($Installer) {
 if (!(Test-Path $Inno)) { throw 'Inno Setup compiler is missing.' }
 & $Inno "$PSScriptRoot\AM-Lively-Win11.iss"
 if ($LASTEXITCODE -ne 0) { throw 'Windows 11 installer compilation failed.' }
 if (!(Test-Path "$root\dist\AM-Lively-Wallpaper-Win11-Setup-x86.exe")) {
   throw 'Windows 11 installer was not generated.'
 }
}
Write-Host 'Windows 11 light edition compiled and packaged.'
