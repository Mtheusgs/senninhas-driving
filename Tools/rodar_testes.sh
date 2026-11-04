#!/bin/bash
# Uso: Tools/rodar_testes.sh [EditMode|PlayMode]   — com o Editor da Unity fechado.
cd "$(dirname "$0")/.."
U=$(ls -d ~/Unity/Hub/Editor/6000.*/Editor/Unity | head -1)
P=${1:-EditMode}
rm -f "test-results-$P.xml"
"$U" -batchmode -nographics -projectPath CarroCG -runTests -testPlatform "$P" \
  -testResults "$PWD/test-results-$P.xml" -logFile "$PWD/test-$P.log"
rc=$?
echo "exit=$rc"
grep -oE '<test-run [^>]*' "test-results-$P.xml" 2>/dev/null | grep -oE '(total|passed|failed)="[0-9]+"' | tr '\n' ' '; echo
grep -E "error CS" "test-$P.log" | sort -u | head -10
grep -oE '<test-case [^>]*result="Failed"' "test-results-$P.xml" 2>/dev/null | grep -oE 'fullname="[^"]*"'
exit $rc
