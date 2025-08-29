#!ps
$script = @'
[CmdletBinding()]
param(
  [Parameter(Mandatory = $true)] [string]$Goal,
  [int]$MaxSteps = 5,
  [int]$CmdTimeoutSec = 45,
  [switch]$DryRun,
  [switch]$AllowMaintenance,   # SFC/DISM, service restarts
  [switch]$AllowKill,          # taskkill / Stop-Process (targeted)
  [switch]$AllowDangerous      # overrides allowlist except hard-deny
)

# --- Auto-flag from environment (only if not passed explicitly) ---
if (-not $PSBoundParameters.ContainsKey('AllowMaintenance')) { $AllowMaintenance = ($env:OPENAI_ALLOW_MAINTENANCE -eq '1') }
if (-not $PSBoundParameters.ContainsKey('AllowKill'))         { $AllowKill        = ($env:OPENAI_ALLOW_KILL -eq '1') }
if (-not $PSBoundParameters.ContainsKey('AllowDangerous'))    { $AllowDangerous   = ($env:OPENAI_ALLOW_DANGEROUS -eq '1') }

# --- Inline tags in Goal override (e.g., "cleanup [danger] [kill] [maint]") ---
if ($Goal -match '\[maint\]')  { $AllowMaintenance = $true; $Goal = $Goal -replace '\[maint\]','' }
if ($Goal -match '\[kill\]')   { $AllowKill        = $true; $Goal = $Goal -replace '\[kill\]','' }
if ($Goal -match '\[danger\]') { $AllowDangerous   = $true; $Goal = $Goal -replace '\[danger\]','' }
$Goal = $Goal.Trim()

<# =====================================================================
 ai-auto.ps1 — Autonomous GPT-5 Nano Troubleshooter (ScreenConnect-ready)
 v4 — UTF-8 JSON fix + EncodedCommand + human-readable report
 --------------------------------------------------------------------- #>

# ---- Config ----
$Model   = "gpt-5-nano"
$BaseUrl = "https://api.openai.com/v1/chat/completions"
# Prefer machine scope for ScreenConnect background
$ApiKey  = [Environment]::GetEnvironmentVariable("OPENAI_API_KEY","Machine")
if ([string]::IsNullOrWhiteSpace($ApiKey)) {
  # fallback to user/process scope if needed
  $ApiKey = [Environment]::GetEnvironmentVariable("OPENAI_API_KEY","User")
  if (-not $ApiKey) { $ApiKey = $env:OPENAI_API_KEY }
  if ([string]::IsNullOrWhiteSpace($ApiKey)) { throw "OPENAI_API_KEY not set (Machine/User/Process)." }
}

# ---- Logging ----
$LogRoot = "C:\ProgramData\AIExec\logs"
New-Item -ItemType Directory -Force -Path $LogRoot | Out-Null
$RunId   = (Get-Date -Format "yyyyMMdd_HHmmss") + "_" + [guid]::NewGuid().ToString().Substring(0,8)
$Log     = Join-Path $LogRoot ("run_" + $RunId + ".log")
function Write-Log($msg){ ("[{0}] {1}" -f (Get-Date -Format u), $msg) | Tee-Object -FilePath $Log -Append | Out-Null }

Write-Log "=== AI-AUTO START === Goal='$Goal' Steps=$MaxSteps DryRun=$DryRun AllowMaint=$AllowMaintenance AllowKill=$AllowKill AllowDangerous=$AllowDangerous"

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
  '^(?i)tasklist(\s+.*)?$',
  '^(?i)(Get-Process|Get-Service)$',
  '^(?i)(sc(\.exe)?\s+query\b)',
  '^(?i)(Get-WinEvent\b|wevtutil\s+qe\b)',
  '^(?i)(gpresult\s+/R\b)',
  '^(?i)(wmic\s+process\b)',
  # Expanded safe triage so outputs aren’t “blocked”
  '^(?i)Get-ItemProperty\s+HKLM:\\Software\\Microsoft\\Windows\\CurrentVersion\\Run',
  '^(?i)Get-ItemProperty\s+HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\Run',
  # Registry and event log read-only queries
  '^(?i)reg\s+query\s+HKLM\\Software\\Microsoft\\Windows\\CurrentVersion\\Run\b',
  '^(?i)reg\s+query\s+HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Run\b',
  '^(?i)wevtutil\s+qe\s+System\b',
  '^(?i)wevtutil\s+qe\s+Application\b',
  '^(?i)Get-Process(\s+-IncludeUserName|\s+-Module|\s+-FileVersionInfo)?',
  '^(?i)wmic\s+cpu\s+get\s+LoadPercentage\b',
  # Extra wmic allowlist
  '^(?i)wmic\s+OS\s+get\s+FreePhysicalMemory,TotalVisibleMemorySize\b',
  '^(?i)wmic\s+pagefile\s+get\s+CurrentUsage,AllocatedBaseSize\b',
  '^(?i)wmic\s+logicaldisk\s+get\s+DeviceID,FreeSpace,Size\b',
  '^(?i)wmic\s+logicaldisk\s+get\s+Caption,FreeSpace,Size\b',
  '^(?i)wmic\s+diskdrive\s+get\s+Model,Status\b',
  # Allow PowerShell-wrapped diagnostics
  '^(?i)powershell(\.exe)?\s+-Command\s+Get-Process\b',
  '^(?i)powershell(\.exe)?\s+-Command\s+Get-Counter\b',
  '^(?i)powershell(\.exe)?\s+-Command\s+Get-Service\b',
  '^(?i)powershell(\.exe)?\s+-Command\s+Sort-Object\b',
  '^(?i)powershell(\.exe)?\s+-Command\s+Select-Object\b'
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

  if ($AllowDangerous) { return $true }   # operator override

  if (Is-Denied $cmd) { return $false }
  if (Is-AllowedByAny $cmd $AllowTriage) { return $true }
  if ($AllowMaintenance -and (Is-AllowedByAny $cmd $AllowMaint)) { return $true }
  if ($AllowKill -and (Is-AllowedByAny $cmd $AllowKillRx)) { return $true }
  return $false
}

# ---- HTTP helper (UTF-8 JSON body) ----
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

# ---- Command runner (EncodedCommand for pwsh to avoid quoting issues) ----
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
- Emit commands WITHOUT any 'powershell -Command' prefix; output the bare cmdlet or executable line.
- Propose only 1–2 commands per step as JSON. NO prose.
- Prefer read-only diagnostics. Maintenance commands allowed ONLY if explicitly permitted by mode.
- JSON schema EXACTLY:
{ ""commands"": [ { ""cmd"": ""<one-liner>"", ""why"": ""<short reason>"" } ] }
If you have nothing safe to do, return {""commands"":[]}.
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
  $messages  += @{role="assistant"; content=$raw}
  $messages  += @{
    role="user";
    content=("Results JSON: " + ($results | ConvertTo-Json -Depth 6) + "`nProceed to next step.")
  }
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

# Normalize curly quotes/dashes for CP437 consoles
$finalText = $finalText -replace ([char]0x2013), '-'   # en dash
$finalText = $finalText -replace ([char]0x2014), '-'   # em dash
$finalText = $finalText -replace ([char]0x2018), "'"   # left single quote
$finalText = $finalText -replace ([char]0x2019), "'"   # right single quote
$finalText = $finalText -replace ([char]0x201C), '"'   # left double quote
$finalText = $finalText -replace ([char]0x201D), '"'   # right double quote

Write-Host "`n===== Investigation Report =====`n$finalText`n"
Write-Log "=== AI-AUTO END ==="
Write-Host ("Log: " + $Log)
'@

# Write file & run
$path = "C:\Windows\Temp\ai-auto.ps1"
Set-Content -Path $path -Encoding UTF8 -Value $script
& $path -Goal "Investigate slowness and random power outages" -AllowMaintenance -AllowKill -AllowDangerous
