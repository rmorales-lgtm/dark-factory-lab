#!/usr/bin/env bash
# Stop hook: Claude can't finish while tests fail (max 3 automatic retries).
INPUT=$(cat)
COUNTER="$CLAUDE_PROJECT_DIR/.claude/.attempts"

# New request from the human: reset the counter
echo "$INPUT" | grep -q '"stop_hook_active": *true' || echo 0 > "$COUNTER"

ATTEMPTS=$(cat "$COUNTER" 2>/dev/null || echo 0)
if [ "$ATTEMPTS" -ge 3 ]; then exit 0; fi   # cap reached: let it stop

cd "$CLAUDE_PROJECT_DIR"
if ! dotnet test --nologo > /dev/null 2>&1; then
  echo $((ATTEMPTS + 1)) > "$COUNTER"
  echo "dotnet test is failing. Run 'dotnet test', read the errors and fix the code. Do not disable any test and do not add [Skip]." >&2
  exit 2
fi
exit 0
