"""Extrai um .unitypackage (tar.gz de pastas <guid>/{asset, asset.meta, pathname}) dentro de <projeto>/Assets.
Uso: python3 -I Tools/extrair_unitypackage.py <pacote.unitypackage> <pasta-do-projeto>"""
import os, sys, tarfile

pacote, projeto = sys.argv[1], os.path.abspath(sys.argv[2])
raiz = os.path.join(projeto, 'Assets') + os.sep
with tarfile.open(pacote, 'r:gz') as t:
    entradas = {}
    for m in t.getmembers():
        partes = m.name.lstrip('./').split('/')
        if len(partes) == 2 and partes[1]:
            entradas.setdefault(partes[0], {})[partes[1]] = m
    for arquivos in entradas.values():
        if 'pathname' not in arquivos:
            continue
        caminho = t.extractfile(arquivos['pathname']).read().decode('utf-8').splitlines()[0].strip()
        destino = os.path.abspath(os.path.join(projeto, caminho))
        if not destino.startswith(raiz):
            raise SystemExit('caminho fora de Assets: ' + caminho)
        if 'asset' in arquivos:
            os.makedirs(os.path.dirname(destino), exist_ok=True)
            with open(destino, 'wb') as f:
                f.write(t.extractfile(arquivos['asset']).read())
        else:
            os.makedirs(destino, exist_ok=True)
        if 'asset.meta' in arquivos:
            with open(destino + '.meta', 'wb') as f:
                f.write(t.extractfile(arquivos['asset.meta']).read())
        print(caminho)
