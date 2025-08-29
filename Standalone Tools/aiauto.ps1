
<# ==== ai-auto.ps1 ====
Use this one-liner exactly like before:
.\ai-auto.ps1 -Goal "Find out why this computer is slow"

Optional:
-AllowMaintenance   # SFC/DISM, service restarts
-AllowKill          # taskkill/Stop-Process (targeted)
-CmdTimeoutSec 90   # if that machine is glacial


<# =====================================================================
 ai-auto.ps1 — Autonomous GPT-5 Nano Troubleshooter (ScreenConnect-ready)
 v4 — FIX: send UTF-8 JSON bytes to OpenAI (IRM + ContentType)
      + keeps v3 EncodedCommand runner + final human report
 --------------------------------------------------------------------- #>

[CmdletBinding()]
param(
  [Parameter(Mandatory = $true)] [string]$Goal,
  [int]$MaxSteps = 5,
  [int]$CmdTimeoutSec = 45,
  [switch]$DryRun,
  [switch]$AllowMaintenance,   # SFC/DISM, service restarts
  [switch]$AllowKill           # taskkill / Stop-Process (targeted)
  [switch]$AllowDangerous 
)

# ---- Config ----
$Model   = "gpt-5-nano"
$BaseUrl = "https://api.openai.com/v1/chat/completions"
$ApiKey  = [Environment]::GetEnvironmentVariable("OPENAI_API_KEY","Machine")
if ([string]::IsNullOrWhiteSpace($ApiKey)) { throw "OPENAI_API_KEY (Machine) not set." }

# ---- Logging ----
$LogRoot = "C:\ProgramData\AIExec\logs"
New-Item -ItemType Directory -Force -Path $LogRoot | Out-Null
$RunId   = (Get-Date -Format "yyyyMMdd_HHmmss") + "_" + [guid]::NewGuid().ToString().Substring(0,8)
$Log     = Join-Path $LogRoot ("run_" + $RunId + ".log")
function Write-Log($msg){ ("[{0}] {1}" -f (Get-Date -Format u), $msg) | Tee-Object -FilePath $Log -Append | Out-Null }

Write-Log "=== AI-AUTO START === Goal='$Goal' Steps=$MaxSteps DryRun=$DryRun AllowMaint=$AllowMaintenance AllowKill=$AllowKill"

# ---- Guardrails ----
$DenyList = @(
  '(?i)\b(format|bcdedit|shutdown)\b',
  '(?i)\b(del|erase|rd|rmdir)\b',
  '^(?i)(Remove-|Set-|New-|Disable-|Enable-|Clear-)',
  '(?i)Invoke-WebRequest\s+-OutFile',
  '(?i)curl\s+.*\s+-o\s+'
)
$AllowTriage = @(
  '^(?i)(Get-|Test-|Resolve-|Measure-)',
  '^(?i)(ipconfig(\s+/all)?)$',
  '^(?i)(ping|tracert|nslookup|whoami|systeminfo)$',
  '^(?i)(netstat(\s+-[a-z]+)*)$',
  '^(?i)(tasklist)$',
  '^(?i)(Get-Process|Get-Service)$',
  '^(?i)(sc(\.exe)?\s+query\b)',
  '^(?i)(Get-WinEvent\b|wevtutil\s+qe\b)',
  '^(?i)(gpresult\s+/R\b)',
  '^(?i)(wmic\s+process\b)'
)
$AllowMaint = @(
  '^(?i)sfc(\.exe)?\s+/scannow\b',
  '^(?i)DISM(\.exe)?\s+/Online\s+/Cleanup-Image\s+/(CheckHealth|ScanHealth|RestoreHealth)\b',
  '^(?i)(Restart-Service|Start-Service|Stop-Service)\s+\S+'
)
$AllowKillRx = @(
  '^(?i)taskkill\s+/PID\s+\d+(\s+/F)?$',
  '^(?i)Stop-Process\s+-Id\s+\d+(\s+-Force)?$'
)

function Is-Denied($cmd){ foreach($rx in $DenyList){ if($cmd -match $rx){ return $true } } return $false }
function Is-AllowedByAny($cmd, $rxList){ foreach($rx in $rxList){ if($cmd -match $rx){ return $true } } return $false }
function Is-Allowed($cmd){
  # Hard never-run list (even with -AllowDangerous)
  $HardDeny = '(?i)\b(mkfs|dd|format|bcdedit|shutdown|halt|reboot)\b'
  if ($cmd -match $HardDeny) { return $false }

  # If dangerous mode is on, let everything else through
  if ($AllowDangerous) { return $true }

  # Normal checks
  if (Is-Denied $cmd) { return $false }
  if (Is-AllowedByAny $cmd $AllowTriage) { return $true }
  if ($AllowMaintenance -and (Is-AllowedByAny $cmd $AllowMaint)) { return $true }
  if ($AllowKill -and (Is-AllowedByAny $cmd $AllowKillRx)) { return $true }
  return $false
}



# ---- HTTP helper (UTF-8 JSON fix) ----
function Invoke-Chat($messages) {
  $obj = @{ model = $Model; messages = $messages }
  $json = ($obj | ConvertTo-Json -Depth 12)
  $bytes = [System.Text.Encoding]::UTF8.GetBytes($json)
  try {
    $resp = Invoke-RestMethod -Uri $BaseUrl -Method Post -Headers @{ Authorization = "Bearer $ApiKey" } -ContentType 'application/json; charset=utf-8' -Body $bytes -TimeoutSec 90
  } catch {
    Write-Log "API error: $_"; throw
  }
  return $resp.choices[0].message.content
}

# ---- Command runner (EncodedCommand for pwsh) ----
function Encode-PwshCommand([string]$cmd){ [Convert]::ToBase64String([System.Text.Encoding]::Unicode.GetBytes($cmd)) }
function Run-Command($cmd){
  if ($DryRun) { Write-Log "DRYRUN: $cmd"; return "[dry-run]" }
  if (-not (Is-Allowed $cmd)) { Write-Log "BLOCKED: $cmd"; return "[blocked by policy]" }
  $usePwsh = ($cmd -match '^(?i)(Get-|Test-|Resolve-|Measure-|Restart-Service|Start-Service|Stop-Service|Get-Process|Get-Service|Get-WinEvent)')
  $psi = New-Object System.Diagnostics.ProcessStartInfo
  if ($usePwsh) {
    $psi.FileName  = "$env:SystemRoot\System32\WindowsPowerShell\v1.0\powershell.exe"
    $psi.Arguments = "-NoProfile -NonInteractive -EncodedCommand " + (Encode-PwshCommand $cmd)
  } else {
    $psi.FileName  = "$env:SystemRoot\System32\cmd.exe"
    $psi.Arguments = "/d /c $cmd"
  }
  $psi.RedirectStandardOutput = $true; $psi.RedirectStandardError  = $true; $psi.UseShellExecute = $false
  $p = New-Object System.Diagnostics.Process; $p.StartInfo = $psi; [void]$p.Start()
  if (-not $p.WaitForExit($CmdTimeoutSec * 1000)) { try { $p.Kill() } catch {} ; Write-Log "TIMEOUT: $cmd"; return "[timeout ${CmdTimeoutSec}s]" }
  $out = $p.StandardOutput.ReadToEnd(); $err = $p.StandardError.ReadToEnd(); if ($err) { $out += "`n[stderr]`n$err" }
  if ($out.Length -gt 6000) { $out = $out.Substring(0,6000) + "`n...[truncated]..." }
  Write-Log "EXEC: $cmd"; return $out
}

# ---- Conversation Setup ----
$lane = if ($AllowMaintenance) { "TRIAGE + limited MAINTENANCE" } else { "TRIAGE only" }
if ($AllowKill) { $lane += " (+ targeted KILL)" }
$systemPrompt = @"
You are a cautious Windows troubleshooting agent running in a restricted background shell.
GOAL: $Goal
MODE: $lane
Rules:
- Propose only 1–2 commands per step as JSON. NO prose.
- Prefer read-only diagnostics. Maintenance commands allowed ONLY if explicitly permitted by mode.
- JSON schema EXACTLY:
{ "commands":[ {"cmd":"<one-liner>","why":"<short reason>"} ] }
If you have nothing safe to do, return {"commands":[]}.
"@
$messages = @(
  @{role="system"; content=$systemPrompt},
  @{role="user";   content="Begin step 1."}
)
$Transcript = @()

# ---- Main Loop ----
for ($i=1; $i -le $MaxSteps; $i++) {
  Write-Host ("[AI] Planning step ${i}…")
  Write-Log  ("--- STEP ${i}: requesting plan ---")
  $raw = Invoke-Chat $messages
  Write-Log "RAW MODEL JSON: $raw"
  try { $plan = $raw | ConvertFrom-Json -ErrorAction Stop } catch { Write-Log "JSON parse failed at step $i. Stopping."; break }
  if (-not $plan.commands -or $plan.commands.Count -eq 0) { Write-Log "No commands proposed at step $i. Stopping."; break }
  $results = @()
  foreach ($c in $plan.commands) {
    $cmd = $c.cmd.ToString()
    Write-Host ("[AI] Running: " + $cmd)
    $out = Run-Command $cmd
    $results += @{ cmd=$cmd; why=$c.why; output_preview=$out }
  }
  $Transcript += [pscustomobject]@{ step=$i; commands=$plan.commands; results=$results }
  $messages += @{role="assistant"; content=$raw}
  $messages += @{role="user"; content=("Results JSON: " + ($results | ConvertTo-Json -Depth 6) + "`nProceed to next step.")}
  Start-Sleep -Milliseconds 200
}

# ---- Final Human-Readable Report ----
$reportPrompt = @"
You are writing a final sysadmin report for the completed investigation on a slow Windows machine.
Write concise, plain-English output with these sections:
1) Executive Summary (3–6 bullets)
2) Key Findings (processes/services, network, event log highlights)
3) Likely Root Causes (ranked)
4) Recommended Next Steps (safe diagnostics only)
5) Optional Maintenance Actions (ONLY if mode allowed; specify exact commands)
Keep it tight and readable. No raw logs; reference evidence briefly.
"@
$finalMessages = @(
  @{role="system"; content=$reportPrompt},
  @{role="user";   content=("Full transcript JSON follows: " + ($Transcript | ConvertTo-Json -Depth 12))}
)
try { $finalText = Invoke-Chat $finalMessages } catch { $finalText = "Final report generation failed: $_" }
Write-Host "`n===== Investigation Report =====`n$finalText`n"
Write-Log "=== AI-AUTO END ==="
Write-Host ("Log: " + $Log)



#>
[CmdletBinding()]
param(
  [Parameter(Mandatory=$true)] [string]$Goal,
  [int]$MaxSteps=5,
  [switch]$AllowMaintenance,
  [switch]$AllowKill,
  [switch]$AllowDangerous
)