#!/bin/bash
# Remonta as cenas Jogo e Menu pelos scripts de Editor (CarroCG → Montar ...). Editor fechado.
cd "$(dirname "$0")/.."
U=$(ls -d ~/Unity/Hub/Editor/6000.*/Editor/Unity | head -1)
for m in MontarJogo MontarMenu; do
  "$U" -batchmode -nographics -quit -projectPath CarroCG -executeMethod $m.Executar -logFile "$PWD/montar-$m.log"
  echo "$m exit=$?"
  grep -E "error CS|Exception|referenced script" "montar-$m.log" | sort -u | head -5
done
