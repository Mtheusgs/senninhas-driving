#!/bin/bash
# Joga sozinho e salva PNGs em previews/ (precisa de GPU: sem -nographics). Editor fechado.
cd "$(dirname "$0")/.."
U=$(ls -d ~/Unity/Hub/Editor/6000.*/Editor/Unity | head -1)
mkdir -p previews
CAPTURA_DIR="$PWD/previews" "$U" -batchmode -projectPath CarroCG -runTests -testPlatform PlayMode \
  -testFilter "CapturasTests.Menu;CapturasTests.Jogo;CapturasTests.Aula;CapturasTests.Lua" \
  -testResults "$PWD/test-results-captura.xml" -logFile "$PWD/test-captura.log"
rc=$?
echo "exit=$rc"; ls -1 previews/menu-*.png previews/jogo-*.png 2>/dev/null
exit $rc
