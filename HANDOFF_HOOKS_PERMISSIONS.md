# Hand-off: hooks and permissions (Oscar)

Owner: Oscar Ortiz. Reviewer: Richard Morales. Dry-run prompt check: Andrés Felipe Parra Rojas.
Status: ready for review. Files changed, not committed (Richard does the final commit).

## Files

| File | Change |
|---|---|
| `.claude/hooks/check-tests.sh` | No-solution guard + test errors passed to the agent |
| `.claude/hooks/check-tests.ps1` | Same no-solution guard (Windows version) |
| `.claude/hooks/block-network.sh` | **New.** PreToolUse hook that blocks any non-localhost URL |
| `.claude/settings.json` | New allow/deny rules, PreToolUse hook registered, Stop hook timeout |

## What changed and why

**1. Stop hook: no-solution guard (was listed as "done" in the plan, but was missing)**
- `ls *.sln *.slnx 2>/dev/null | grep -q . || exit 0`
- Without it, the hook blocked the agent before the project existed (`dotnet test` fails in an empty repo).
- Handles both `.sln` and `.slnx` (`dotnet new sln` creates `.slnx` by default on .NET 10).

**2. Stop hook: errors go to the agent**
- The test output was discarded, so the agent had to re-run `dotnet test` to see what failed.
- Now the first 20 lines matching `error`, `Failed` or `Assert` are sent with the failure message. Saves a turn per retry.
- Still max 3 retries, and still "do not disable any test, do not add [Skip]".

**3. New network hook (`block-network.sh`)**
- Runs before every Bash command. Any URL other than `localhost` / `127.0.0.1` → blocked with:
  "Blocked: only http://localhost URLs are allowed. All data is simulated in data/*.json."
- Catches chained commands too (`curl localhost && curl https://...`).
- This is what lets us allow all `curl` safely (see below).

**4. `settings.json`**
- Allow: `Bash(curl:*)` (the evaluator will use `-sf`, `--retry`, `-X POST`, cookies... the exact-form rules would prompt), `Bash(kill:*)`, `Bash(ps:*)` (stopping the app on Linux; `taskkill` is Windows only), `Bash(git diff --stat data/)`.
- Deny: `WebFetch`, `WebSearch`, `Bash(curl https:*)`, `Edit(data/**)`, `Write(data/**)` (the input JSON can't be modified, enforced, not just asked).
- Stop hook `timeout: 300` (first restore + build + test can be slow).

## Tested

| Case | Result |
|---|---|
| Empty repo (no solution) | Stop hook exit 0, no loop |
| `.sln` + failing tests | exit 2, failing test name and assert message shown |
| `.slnx` + failing tests | exit 2, same |
| `curl http://localhost:5080/`, `curl 'http://127.0.0.1:5080/...'` | allowed |
| `curl https://dev.azure.com/...`, chained external URL | blocked |
| `settings.json` | valid JSON |

Not tested: a real `dotnet test` run (no SDK on my machine). The dry run is the real check.

## For the others

- **Richard (CLAUDE.md):** step 6 should use `dotnet run --urls http://localhost:5080` (in the background), `curl http://localhost:5080`, then `pkill -f Chronos.Web` or `kill`. All allowed, no prompt.
- **Andrés (reviewer):** the permissions already block real network calls and edits to `data/`. The reviewer's "real network call" check is a second line of defense.
- **Maurice (evaluator):** any `curl` to localhost is allowed, with any flags. `git diff --stat data/` is allowed.
- **Before the dry run:** `echo 0 > .claude/.attempts` (currently 0).
- **During the dry run:** if a permission prompt appears, send me the exact command and I add the rule.

## Full file contents (copy as-is)

The hooks need no `chmod`: `settings.json` runs them with `bash`.

### `.claude/hooks/check-tests.sh`

```bash
#!/usr/bin/env bash
# Stop hook: Claude can't finish while tests fail (max 3 automatic retries).
INPUT=$(cat)
COUNTER="$CLAUDE_PROJECT_DIR/.claude/.attempts"

# New request from the human: reset the counter
echo "$INPUT" | grep -q '"stop_hook_active": *true' || echo 0 > "$COUNTER"

ATTEMPTS=$(cat "$COUNTER" 2>/dev/null || echo 0)
if [ "$ATTEMPTS" -ge 3 ]; then exit 0; fi   # cap reached: let it stop

cd "$CLAUDE_PROJECT_DIR"
# No solution yet (project not created): nothing to test, let it stop
ls *.sln *.slnx 2>/dev/null | grep -q . || exit 0
OUT=$(dotnet test --nologo 2>&1)
if [ $? -ne 0 ]; then
  echo $((ATTEMPTS + 1)) > "$COUNTER"
  echo "dotnet test is failing. Fix the code. Do not disable any test and do not add [Skip]. Errors:" >&2
  echo "$OUT" | grep -E "error |Failed |Assert" | head -20 >&2
  exit 2
fi
exit 0
```

### `.claude/hooks/block-network.sh`

```bash
#!/usr/bin/env bash
# PreToolUse hook (Bash): only localhost URLs are allowed. No real system, no 7pace.
INPUT=$(cat)
if echo "$INPUT" | grep -oE "https?://[^\"\\\\ ']+" | grep -vqE '^https?://(localhost|127\.0\.0\.1)([:/]|$)'; then
  echo "Blocked: only http://localhost URLs are allowed. All data is simulated in data/*.json." >&2
  exit 2
fi
exit 0
```

### `.claude/hooks/check-tests.ps1`

```powershell
# Optional PowerShell version of the Stop hook. To use it, change the command in settings.json to:
# powershell -NoProfile -File "$CLAUDE_PROJECT_DIR/.claude/hooks/check-tests.ps1"
$hookInput = [Console]::In.ReadToEnd()
$counter = Join-Path $env:CLAUDE_PROJECT_DIR ".claude/.attempts"

if ($hookInput -notmatch '"stop_hook_active"\s*:\s*true') { Set-Content $counter 0 }
$attempts = [int](Get-Content $counter -ErrorAction SilentlyContinue)
if ($attempts -ge 3) { exit 0 }

Set-Location $env:CLAUDE_PROJECT_DIR
if (-not (Get-ChildItem *.sln, *.slnx -ErrorAction SilentlyContinue)) { exit 0 }
dotnet test --nologo *> $null
if ($LASTEXITCODE -ne 0) {
  Set-Content $counter ($attempts + 1)
  [Console]::Error.WriteLine("dotnet test is failing. Read the errors and fix the code. Do not disable any test.")
  exit 2
}
exit 0
```

### `.claude/settings.json`

```json
{
  "permissions": {
    "defaultMode": "acceptEdits",
    "allow": [
      "Bash(dotnet --version)",
      "Bash(dotnet --info)",
      "Bash(dotnet clean:*)",
      "Bash(cd:*)",
      "Bash(ls:*)",
      "Bash(cat:*)",
      "Bash(tail:*)",
      "Bash(seq:*)",
      "Bash(echo:*)",
      "Bash(mkdir:*)",
      "Bash(rm src/*)",
      "Bash(rm tests/*)",
      "Bash(git rm:*)",
      "Bash(git status:*)",
      "Bash(git diff:*)",
      "Bash(git add:*)",
      "Bash(git commit:*)",
      "Bash(git log:*)",
      "Bash(taskkill //F //IM:*)",
      "Bash(dotnet new:*)",
      "Bash(dotnet sln:*)",
      "Bash(dotnet add:*)",
      "Bash(dotnet restore:*)",
      "Bash(dotnet build:*)",
      "Bash(dotnet test:*)",
      "Bash(dotnet run:*)",
      "Bash(curl http://localhost:*)",
      "Bash(curl -s http://localhost:*)",
      "Bash(curl -s -o /dev/null -w:*)",
      "Bash(curl -s -i http://localhost:*)",
      "Bash(curl -i http://localhost:*)",
      "Bash(curl -s -w:*)",
      "Bash(curl -s -L http://localhost:*)",
      "Bash(printf:*)",
      "Bash(dotnet publish:*)",
      "Bash(dotnet watch:*)",
      "Bash(git branch:*)",
      "Bash(git show:*)",
      "Bash(pwd)",
      "Bash(wc:*)",
      "Bash(find:*)",
      "Bash(grep:*)",
      "Bash(head:*)",
      "Bash(sleep:*)",
      "Bash(pkill:*)",
      "Bash(taskkill:*)",
      "Bash(kill:*)",
      "Bash(ps:*)",
      "Bash(git diff --stat data/)",
      "Bash(curl:*)"
    ],
    "deny": [
      "Bash(rm -rf:*)",
      "Bash(git push:*)",
      "WebFetch",
      "WebSearch",
      "Bash(curl https:*)",
      "Edit(data/**)",
      "Write(data/**)"
    ]
  },
  "hooks": {
    "PreToolUse": [
      { "matcher": "Bash",
        "hooks": [ { "type": "command", "command": "bash \"$CLAUDE_PROJECT_DIR/.claude/hooks/block-network.sh\"" } ] }
    ],
    "Stop": [
      { "hooks": [ { "type": "command", "command": "bash \"$CLAUDE_PROJECT_DIR/.claude/hooks/check-tests.sh\"", "timeout": 300 } ] }
    ]
  }
}
```
