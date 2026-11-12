#!/bin/bash
# Gera Build/SenninhasDriving.x86_64 (Linux). Editor fechado.
cd "$(dirname "$0")/.."
U=$(ls -d ~/Unity/Hub/Editor/6000.*/Editor/Unity | head -1)
"$U" -batchmode -nographics -quit -projectPath CarroCG -executeMethod Construir.Linux -logFile "$PWD/build.log"
echo "exit=$?"; grep -E "Construir:|error CS" build.log | tail -3
