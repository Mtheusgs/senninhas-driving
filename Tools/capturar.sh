#!/bin/bash
# Joga ~6 s sozinho e salva PNGs em previews/ (precisa de GPU: sem -nographics). Editor fechado.
cd "$(dirname "$0")/.."
U=$(ls -d ~/Unity/Hub/Editor/6000.*/Editor/Unity | head -1)
mkdir -p previews
CAPTURA_DIR="$PWD/previews" "$U" -batchmode -projectPath CarroCG -runTests -testPlatform PlayMode \
  -testFilter "JogoTests.Capturar" -testResults "$PWD/test-results-captura.xml" -logFile "$PWD/test-captura.log"
echo "exit=$?"; ls -1 previews/jogo-*.png 2>/dev/null
