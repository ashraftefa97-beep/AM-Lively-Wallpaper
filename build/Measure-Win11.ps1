[CmdletBinding()]
param([int]$Samples=8,[int]$IntervalSeconds=2)
$ErrorActionPreference='Stop'
if($Samples -lt 1 -or $Samples -gt 120){throw 'Samples must be 1..120.'}
if($IntervalSeconds -lt 1 -or $IntervalSeconds -gt 30){throw 'IntervalSeconds must be 1..30.'}
$records=@()
for($i=0;$i -lt $Samples;$i++){
 $procs=Get-Process -ErrorAction SilentlyContinue | Where-Object {$_.ProcessName -eq 'AM-Lively-Wallpaper'}
 if(!$procs){Write-Warning 'AM Lively Wallpaper is not running.';break}
 foreach($p in $procs){
  $records += [pscustomobject]@{
   Time=(Get-Date).ToString('o')
   PID=$p.Id
   WorkingSetMB=[math]::Round($p.WorkingSet64/1MB,1)
   PrivateMB=[math]::Round($p.PrivateMemorySize64/1MB,1)
   TotalCpuSeconds=[math]::Round($p.CPU,2)
  }
 }
 if($i -lt $Samples-1){Start-Sleep -Seconds $IntervalSeconds}
}
$records | Format-Table -AutoSize
Write-Host 'Repeat while the app is visible and hidden in tray. CPU seconds is cumulative, not CPU percentage.'
