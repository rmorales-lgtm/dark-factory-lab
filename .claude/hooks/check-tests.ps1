# Optional PowerShell version of the Stop hook. To use it, change the command in settings.json to:
# powershell -NoProfile -File "$CLAUDE_PROJECT_DIR/.claude/hooks/check-tests.ps1"
$hookInput = [Console]::In.ReadToEnd()
$counter = Join-Path $env:CLAUDE_PROJECT_DIR ".claude/.attempts"

if ($hookInput -notmatch '"stop_hook_active"\s*:\s*true') { Set-Content $counter 0 }
$attempts = [int](Get-Content $counter -ErrorAction SilentlyContinue)
if ($attempts -ge 3) { exit 0 }

Set-Location $env:CLAUDE_PROJECT_DIR
dotnet test --nologo *> $null
if ($LASTEXITCODE -ne 0) {
  Set-Content $counter ($attempts + 1)
  [Console]::Error.WriteLine("dotnet test is failing. Read the errors and fix the code. Do not disable any test.")
  exit 2
}
exit 0
