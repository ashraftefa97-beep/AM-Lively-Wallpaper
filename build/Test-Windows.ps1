param([string]$Compiler='csc.exe')
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
$out=Join-Path $env:TEMP ('AM-Lively-Tests-'+[Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory $out | Out-Null
$source="$root\src\livelywpf\livelywpf\ProcessManager"
& $Compiler /nologo /target:exe "/out:$out\SafetyPolicyTests.exe" "$source\SafetyPolicy.cs" "$root\tests\SafetyPolicyTests.cs"
if($LASTEXITCODE -ne 0){throw 'Safety test compilation failed.'}
& "$out\SafetyPolicyTests.exe"
if($LASTEXITCODE -ne 0){throw 'Safety policy tests failed.'}
# Compile the complete non-UI engine against Framework assemblies, without changing the app's target.
& $Compiler /nologo /target:library "/out:$out\ProcessEngine.dll" /r:System.Management.dll /r:System.ServiceProcess.dll /r:System.Runtime.Serialization.dll "$source\SafetyPolicy.cs" "$source\NativeTrust.cs" "$source\ProcessEngine.cs"
if($LASTEXITCODE -ne 0){throw 'Process engine compilation failed.'}
# Voice command parser is testable without a microphone or speech recognizer.
$voiceSource="$root\src\livelywpf\livelywpf\Voice"
& $Compiler /nologo /target:exe "/out:$out\BarqVoiceCommandTests.exe" "$voiceSource\BarqVoiceCommands.cs" "$root\tests\BarqVoiceCommandTests.cs"
if($LASTEXITCODE -ne 0){throw 'BARQ voice command tests did not compile.'}
& "$out\BarqVoiceCommandTests.exe"
if($LASTEXITCODE -ne 0){throw 'BARQ voice command tests failed.'}
Write-Host 'Policy tests and engine compilation passed. These do not replace Windows 8.1 integration tests.'
