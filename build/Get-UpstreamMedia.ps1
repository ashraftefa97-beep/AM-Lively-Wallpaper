# Original tutorial videos are fetched from the exact source commit, never latest.
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
$baseline=Get-Content "$root\tests\UPSTREAM-SHA256.json" -Raw | ConvertFrom-Json
$commit='554bd7311b26953756ae9d11e5fe40c9d87f30bc'
foreach($relative in @('src/livelywpf/livelywpf/docs/help_vid_1.mp4','src/livelywpf/livelywpf/docs/help_vid_2.mp4')) {
 $target=Join-Path $root $relative
 $expected=$baseline.files.$relative
 if(!(Test-Path $target)) {
  New-Item -ItemType Directory -Force (Split-Path $target) | Out-Null
  Invoke-WebRequest "https://raw.githubusercontent.com/lively-community/lively/$commit/$relative" -OutFile $target -UseBasicParsing
 }
 if((Get-FileHash $target -Algorithm SHA256).Hash -ne $expected){throw "Upstream tutorial hash mismatch: $relative"}
}
