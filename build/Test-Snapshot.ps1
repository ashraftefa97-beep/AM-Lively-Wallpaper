param([Parameter(Mandatory=$true)][string]$Compiler)
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
$out=Join-Path $env:TEMP ('AM-Snapshot-'+[Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory $out | Out-Null
$source="$root\src\livelywpf\livelywpf\ProcessManager"
& $Compiler /nologo /target:exe "/out:$out\SnapshotSmoke.exe" /r:System.Management.dll /r:System.ServiceProcess.dll /r:System.Runtime.Serialization.dll "$source\SafetyPolicy.cs" "$source\NativeTrust.cs" "$source\ProcessEngine.cs" "$root\tests\SnapshotSmoke.cs"
if($LASTEXITCODE -ne 0){throw 'Snapshot test compilation failed.'}
& "$out\SnapshotSmoke.exe"
if($LASTEXITCODE -ne 0){throw 'Read-only snapshot smoke test failed.'}
