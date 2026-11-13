#!/usr/bin/env bash
# Start-of-session sanity check for Mission Chronos.
# Usage: ./init.sh          -> state summary, build, tests, smoke check of the app on :5080
#        ./init.sh --no-run -> skip starting the app
set -u
cd "$(dirname "$0")"
PORT=5080
URL="http://localhost:${PORT}"

echo "== Progress =="
tail -n 15 claude-progress.txt 2>/dev/null
echo
echo "== Recent commits =="
git log --oneline -10 2>/dev/null
echo
echo "== Features passing =="
total=$(grep -c '"id": "T' feature_list.json)
passed=$(grep -c '"passes": true' feature_list.json)
echo "${passed} / ${total}"
echo
echo "== data/ untouched? =="
git status --porcelain -- data
echo "(empty above = OK)"

if [ ! -f Chronos.sln ]; then
  echo
  echo "No Chronos.sln yet: the project skeleton must be created first."
  exit 0
fi

echo
echo "== Build and test =="
dotnet build --nologo -v q || { echo "BUILD BROKEN: fix before any new work"; exit 1; }
dotnet test --nologo --no-build || { echo "TESTS FAILING: fix before any new work"; exit 1; }

[ "${1:-}" = "--no-run" ] && exit 0

echo
echo "== Smoke check =="
dotnet run --project src/Chronos.Web --no-build --urls "$URL" > /dev/null 2>&1 &
APP_PID=$!
code=$(curl -s -o /dev/null -w "%{http_code}" --retry 30 --retry-delay 1 --retry-connrefused "$URL/")
echo "GET / -> ${code}"
kill "$APP_PID" 2>/dev/null
taskkill //F //IM Chronos.Web.exe >/dev/null 2>&1
[ "$code" = "200" ] || exit 1
echo "OK"
