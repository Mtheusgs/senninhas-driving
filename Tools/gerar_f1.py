"""Gera a arte HD do F1 (vista lateral, frente para +x), da roda, dos coletáveis e do céu lunar.
Uso: python3 -I Tools/gerar_f1.py CarroCG/Assets/Art
Saídas em F1/: f1_atras.png, f1_pintura.png (cinza claro, tingida no Unity pela cor da garagem), f1_frente.png,
f1_corpo.png (silhueta completa em vermelho, usada no collider), f1_roda.png, moeda.png, galao.png.
Saídas em Ceu/: estrelas.png, terra.png.
Coordenadas do F1 em pixels de 960x300, y para baixo. PPU 256 no Unity.
Layout usado por Editor/MontagemComum.cs: rodas em (190,178) e (770,178), raio 92; capacete em (515,92)."""
import math, os, random, sys
from PIL import Image, ImageChops, ImageDraw, ImageFilter, ImageFont

S = 4                                   # supersampling
W, H = 960, 300
RODA_R = 92
VERMELHO, VERMELHO_ESC = (208, 26, 42, 255), (120, 12, 22, 255)
BRANCO, PRETO, CINZA = (245, 245, 245, 255), (24, 24, 28, 255), (70, 72, 80, 255)
AMARELO, OURO, OURO_ESC = (250, 204, 40, 255), (250, 196, 40, 255), (176, 124, 14, 255)
BASE_PINTURA, CONTORNO_PINTURA = (215, 215, 215, 255), (95, 95, 95, 255)
TINTA_VERMELHA = (247, 31, 50)          # base 215 x tinta = vermelho original (208, 26, 42)

CHASSI = [(105, 205), (115, 150), (230, 134), (330, 126), (440, 118), (560, 116), (650, 126),
          (770, 150), (880, 178), (935, 192), (935, 205)]
COBERTURA = [(240, 136), (290, 78), (350, 56), (420, 58), (466, 96), (470, 122), (240, 138)]
PLACA_TRASEIRA = [(58, 30), (84, 30), (96, 150), (70, 150)]
PLACA_DIANTEIRA = [(930, 178), (956, 178), (956, 224), (930, 224)]

def s(pts): return [(x * S, y * S) for x, y in pts]

def fonte(tam):
    f = '/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf'
    return ImageFont.truetype(f, tam * S) if os.path.exists(f) else ImageFont.load_default(size=tam * S)

def sombrear(img, mascara, y0, y1):
    """Brilho em cima e sombra embaixo, só dentro da máscara (modo L)."""
    w, h = img.size
    col = Image.new('RGBA', (1, h))
    for y in range(h):
        k = (y / S - y0) / (y1 - y0)
        col.putpixel((0, y), (255, 255, 255, int(80 * max(0, 1 - k / 0.45))) if k < 0.45
                     else (0, 0, 0, int(100 * min(1, (k - 0.45) / 0.55))))
    grad = col.resize((w, h))
    grad.putalpha(ImageChops.multiply(grad.split()[3], mascara))
    return Image.alpha_composite(img, grad)

def reduzir(img, w, h): return img.resize((w, h), Image.LANCZOS)
def tela(): return Image.new('RGBA', (W * S, H * S))

def tingir(img, cor):
    r, g, b, a = img.split()
    r, g, b = (canal.point(lambda v, c=c: v * c // 255) for canal, c in zip((r, g, b), cor))
    return Image.merge('RGBA', (r, g, b, a))

def camada_atras():
    img = tela()
    d = ImageDraw.Draw(img)
    for a, b in [((250, 150), (190, 178)), ((250, 195), (190, 178)), ((700, 150), (770, 178)), ((700, 190), (770, 178))]:
        d.line(s([a, b]), fill=CINZA, width=4 * S)                                           # suspensões
    d.polygon(s([(150, 80), (165, 80), (200, 136), (186, 138)]), fill=CINZA)                 # pilone
    d.polygon(s([(50, 34), (178, 34), (178, 54), (50, 58)]), fill=PRETO)                     # asa traseira
    d.polygon(s([(60, 66), (170, 64), (170, 78), (60, 80)]), fill=PRETO)
    d.polygon(s([(120, 200), (880, 200), (880, 214), (120, 214)]), fill=PRETO)               # assoalho
    return reduzir(img, W, H)

def camada_pintura():
    img = tela()
    d = ImageDraw.Draw(img)
    mascara = Image.new('L', img.size)
    dm = ImageDraw.Draw(mascara)
    for poly in (CHASSI, COBERTURA):
        d.polygon(s(poly), fill=BASE_PINTURA)
        dm.polygon(s(poly), fill=255)
    img = sombrear(img, mascara, 56, 214)
    d = ImageDraw.Draw(img)
    for poly in (CHASSI, COBERTURA):
        d.line(s(poly + [poly[0]]), fill=CONTORNO_PINTURA, width=3 * S, joint='curve')
    for poly in (PLACA_TRASEIRA, PLACA_DIANTEIRA):
        d.polygon(s(poly), fill=BASE_PINTURA)
    return reduzir(img, W, H)

def camada_frente():
    img = tela()
    d = ImageDraw.Draw(img)
    d.polygon(s([(250, 166), (860, 182), (860, 190), (250, 176)]), fill=BRANCO)             # faixa
    d.polygon(s([(330, 128), (352, 126), (352, 150), (330, 152)]), fill=PRETO)              # entrada do sidepod
    d.ellipse(s([(430, 66), (452, 86)]), fill=PRETO)                                        # airbox
    d.ellipse(s([(338, 148), (382, 192)]), fill=BRANCO)                                     # número
    d.text((360 * S, 170 * S), '7', font=fonte(30), fill=PRETO, anchor='mm')
    d.text((372 * S, 104 * S), 'CG RACING', font=fonte(16), fill=BRANCO, anchor='mm')
    d.polygon(s([(470, 118), (476, 100), (560, 100), (575, 118)]), fill=PRETO)              # cockpit
    d.ellipse(s([(489, 66), (541, 118)]), fill=AMARELO)                                     # capacete
    d.polygon(s([(520, 82), (541, 84), (540, 96), (518, 94)]), fill=PRETO)                  # viseira
    d.line(s([(468, 118), (480, 74), (560, 70), (598, 114)]), fill=CINZA, width=10 * S, joint='curve')  # halo
    d.polygon(s([(800, 212), (955, 208), (955, 222), (800, 224)]), fill=PRETO)              # asa dianteira
    d.polygon(s([(820, 198), (950, 192), (950, 204), (820, 208)]), fill=PRETO)
    return reduzir(img, W, H)

def corpo(atras, pintura, frente):
    return Image.alpha_composite(Image.alpha_composite(atras, tingir(pintura, TINTA_VERMELHA)), frente)

def roda():
    n = RODA_R * 2
    img = Image.new('RGBA', (n * S, n * S))
    d = ImageDraw.Draw(img)
    c = RODA_R * S
    def circ(r, **kw): d.ellipse([c - r * S, c - r * S, c + r * S, c + r * S], **kw)
    circ(RODA_R, fill=PRETO)
    circ(74, outline=AMARELO, width=5 * S)
    circ(58, fill=(50, 52, 58, 255))
    for k in range(5):
        a = k * 2 * math.pi / 5
        d.line([(c, c), (c + 56 * S * math.cos(a), c + 56 * S * math.sin(a))], fill=(120, 122, 130, 255), width=8 * S)
    circ(58, outline=(150, 150, 160, 255), width=3 * S)
    circ(12, fill=(205, 205, 210, 255))
    return reduzir(img, n, n)

def moeda():
    n = 128
    img = Image.new('RGBA', (n * S, n * S))
    d = ImageDraw.Draw(img)
    d.ellipse(s([(6, 6), (122, 122)]), fill=OURO, outline=OURO_ESC, width=8 * S)
    d.ellipse(s([(24, 24), (104, 104)]), outline=(255, 230, 120, 255), width=4 * S)
    estrela = [(64 + (30 if k % 2 == 0 else 13) * math.sin(k * math.pi / 5),
                64 - (30 if k % 2 == 0 else 13) * math.cos(k * math.pi / 5)) for k in range(10)]
    d.polygon(s(estrela), fill=(255, 240, 160, 255))
    return reduzir(img, n, n)

def galao():
    n = 128
    img = Image.new('RGBA', (n * S, n * S))
    d = ImageDraw.Draw(img)
    d.rounded_rectangle(s([(22, 30), (106, 120)]), radius=12 * S, fill=VERMELHO, outline=VERMELHO_ESC, width=5 * S)
    d.rounded_rectangle(s([(40, 12), (88, 32)]), radius=6 * S, fill=PRETO)
    d.rectangle(s([(52, 18), (76, 26)]), fill=(0, 0, 0, 0))
    d.ellipse(s([(48, 62), (80, 98)]), fill=BRANCO)
    d.polygon(s([(50, 72), (64, 44), (78, 72)]), fill=BRANCO)
    return reduzir(img, n, n)

def estrelas():
    rnd = random.Random(3)
    img = Image.new('RGBA', (1024, 512))
    d = ImageDraw.Draw(img)
    for _ in range(420):
        x, y, r = rnd.uniform(0, 1024), rnd.uniform(0, 512), rnd.choice([0.6, 0.8, 1.0, 1.4, 2.0])
        d.ellipse([x - r, y - r, x + r, y + r], fill=(255, 255, 255, rnd.randint(140, 255)))
    return img

def terra():
    n, rnd = 256, random.Random(5)
    img = Image.new('RGBA', (n * S, n * S))
    d = ImageDraw.Draw(img)
    d.ellipse(s([(8, 8), (248, 248)]), fill=(40, 110, 200, 255))                             # oceano
    for _ in range(9):                                                                     # continentes
        x, y, rx, ry = rnd.uniform(40, 216), rnd.uniform(40, 216), rnd.uniform(14, 40), rnd.uniform(10, 30)
        d.ellipse(s([(x - rx, y - ry), (x + rx, y + ry)]), fill=(70, 160, 80, 255))
    for _ in range(7):                                                                     # nuvens
        x, y, rx = rnd.uniform(40, 216), rnd.uniform(40, 216), rnd.uniform(18, 46)
        d.ellipse(s([(x - rx, y - 6), (x + rx, y + 6)]), fill=(255, 255, 255, 150))
    mascara = Image.new('L', img.size)
    ImageDraw.Draw(mascara).ellipse(s([(8, 8), (248, 248)]), fill=255)
    sombra = Image.new('RGBA', img.size)                                                   # lado da noite
    ImageDraw.Draw(sombra).ellipse(s([(70, -20), (330, 280)]), fill=(0, 0, 20, 150))
    img = Image.alpha_composite(img, sombra)
    img.putalpha(ImageChops.multiply(img.split()[3], mascara))
    return reduzir(img, n, n)

def mapa_normal(img, raio, forca):
    """Normal map (codificado n*0,5+0,5) a partir da silhueta: altura = alfa desfocado; normal = (-dh/dx, dh/dy_img, 1)."""
    h = img.split()[3].filter(ImageFilter.GaussianBlur(raio))
    w, alt = h.size
    px = h.load()
    out = Image.new('RGBA', (w, alt))
    o = out.load()
    for y in range(alt):
        for x in range(w):
            dx = (px[min(w - 1, x + 1), y] - px[max(0, x - 1), y]) / 255 * forca
            dy = (px[x, min(alt - 1, y + 1)] - px[x, max(0, y - 1)]) / 255 * forca   # y da imagem para baixo
            nx, ny, nz = -dx, dy, 1.0                                                 # em UV (y para cima)
            l = (nx * nx + ny * ny + nz * nz) ** 0.5
            o[x, y] = (int((nx / l * 0.5 + 0.5) * 255), int((ny / l * 0.5 + 0.5) * 255), int((nz / l * 0.5 + 0.5) * 255), 255)
    return out

def sol():
    n = 128
    img = Image.new('RGBA', (n * S, n * S))
    d = ImageDraw.Draw(img)
    for r, a in [(62, 50), (52, 90), (40, 255)]:
        d.ellipse(s([(64 - r, 64 - r), (64 + r, 64 + r)]), fill=(255, 214, 90, a))
    d.ellipse(s([(30, 30), (98, 98)]), fill=(255, 238, 150, 255))
    return reduzir(img, n, n)

def lua():
    n, rnd = 128, random.Random(9)
    img = Image.new('RGBA', (n * S, n * S))
    d = ImageDraw.Draw(img)
    d.ellipse(s([(14, 14), (114, 114)]), fill=(225, 228, 235, 255))
    for _ in range(7):
        x, y, r = rnd.uniform(36, 92), rnd.uniform(36, 92), rnd.uniform(5, 12)
        d.ellipse(s([(x - r, y - r), (x + r, y + r)]), fill=(190, 194, 205, 255))
    return reduzir(img, n, n)

def ceu_gradiente():
    img = Image.new('RGBA', (4, 256))
    for y in range(256):
        for x in range(4):
            img.putpixel((x, y), (255, 255, 255, 255 - y))      # opaco em cima, transparente embaixo
    return img

def feixe():
    w, h = 256, 96
    img = Image.new('RGBA', (w * S, h * S))
    d = ImageDraw.Draw(img)
    for k in range(24):                                          # cone que abre e some para a frente
        f = k / 24
        d.polygon(s([(0, 44), (w * (1 - f), 48 - 46 * (1 - f)), (w * (1 - f), 48 + 46 * (1 - f)), (0, 52)]),
                  fill=(255, 250, 220, int(14 + 4 * f)))
    return reduzir(img, w, h)

# ---------- fundo (parallax): silhuetas claras, emendáveis na horizontal (período 1024) ----------
LF, AF = 1024, 256

def _silhueta(altura_em):
    """Preenche, em cada coluna x, do topo altura_em(x) até o fim da imagem (com supersampling)."""
    img = Image.new('RGBA', (LF * S, AF * S))
    d = ImageDraw.Draw(img)
    pts = [(x * S, (AF - altura_em(x)) * S) for x in range(0, LF + 1)]         # x em pixels reais
    d.polygon(pts + [(LF * S, AF * S), (0, AF * S)], fill=(240, 244, 250, 255))
    return reduzir(img, LF, AF)

def montanhas():
    def h(x):                                    # picos pontudos: soma de "dentes" triangulares periódicos
        t = x / LF
        dente = lambda f, a, fase: a * (1 - 2 * abs(((t * f + fase) % 1) - 0.5))
        return 40 + dente(5, 120, 0.1) + dente(9, 50, 0.37) + dente(17, 16, 0.71)
    return _silhueta(h)

def morros():
    def h(x):
        t = 2 * math.pi * x / LF
        return 95 + 38 * math.sin(2 * t + 0.4) + 22 * math.sin(5 * t + 1.7) + 9 * math.sin(9 * t + 0.2)
    return _silhueta(h)

def pinheiros():
    rnd = random.Random(21)
    img = Image.new('RGBA', (LF * S, AF * S))
    d = ImageDraw.Draw(img)
    base = 70
    d.rectangle(s([(0, AF - base), (LF, AF)]), fill=(240, 244, 250, 255))
    arvores = [(rnd.uniform(0, LF), rnd.uniform(60, 130), rnd.uniform(48, 80)) for _ in range(26)]
    for x, alt, larg in arvores:
        for dx in (-LF, 0, LF):                 # repete nas bordas: emenda sem degrau
            cx = x + dx
            topo = AF - base - alt
            d.polygon(s([(cx, topo), (cx + larg / 2, AF - base + 4), (cx - larg / 2, AF - base + 4)]), fill=(240, 244, 250, 255))
    return reduzir(img, LF, AF)

def poligono_circulo(cx, cy, r, n=12):
    return [(round(cx + r * math.cos(2 * math.pi * k / n), 1), round(cy + r * math.sin(2 * math.pi * k / n), 1)) for k in range(n)]

# Regiões da intro (atrás → frente), mesmas formas da arte. Escritas como C# em Scripts/Jogo/ContornoF1.cs.
def regioes_intro():
    return [
        ('PlacaTraseira', PLACA_TRASEIRA, VERMELHO),
        ('AsaTraseira', [(50, 34), (178, 34), (178, 54), (50, 58)], PRETO),
        ('AsaTraseira2', [(60, 66), (170, 64), (170, 78), (60, 80)], PRETO),
        ('Assoalho', [(120, 200), (880, 200), (880, 214), (120, 214)], PRETO),
        ('Chassi', CHASSI, VERMELHO),
        ('Cobertura', COBERTURA, VERMELHO),
        ('Cockpit', [(470, 118), (476, 100), (560, 100), (575, 118)], PRETO),
        ('Capacete', poligono_circulo(515, 92, 26), AMARELO),
        ('AsaDianteira', [(800, 212), (955, 208), (955, 222), (800, 224)], PRETO),
        ('AsaDianteira2', [(820, 198), (950, 192), (950, 204), (820, 208)], PRETO),
        ('PlacaDianteira', PLACA_DIANTEIRA, VERMELHO),
        ('RodaTraseira', poligono_circulo(190, 178, RODA_R), PRETO),
        ('RodaDianteira', poligono_circulo(770, 178, RODA_R), PRETO),
    ]

def contorno_cs(caminho):
    rs = regioes_intro()
    linhas = ['// GERADO por Tools/gerar_f1.py: não editar. Regiões do F1 para a intro (pixels da arte 960x300, y para baixo).',
              'using UnityEngine;', '', 'public static class ContornoF1', '{']
    for i, (nome, _, _) in enumerate(rs):
        linhas.append(f'    public const int {nome} = {i};')
    linhas.append('    public static readonly Vector2[][] Regioes =\n    {')
    for _, pts, _ in rs:
        linhas.append('        new Vector2[] { ' + ', '.join(f'new({x}f, {y}f)' for x, y in pts) + ' },')
    linhas.append('    };')
    linhas.append('    public static readonly Color32[] Cores =\n    {')
    for _, _, c in rs:
        linhas.append(f'        new({c[0]}, {c[1]}, {c[2]}, 255),')
    linhas.append('    };')
    linhas.append('    public static Vector2 EmUnidades(Vector2 px) => new((px.x - 480) / 256f, (150 - px.y) / 256f);')
    linhas.append('}')
    with open(caminho, 'w') as f:
        f.write('\n'.join(linhas) + '\n')

if __name__ == '__main__':
    raiz = sys.argv[1]
    f1, ceu = os.path.join(raiz, 'F1'), os.path.join(raiz, 'Ceu')
    os.makedirs(f1, exist_ok=True)
    os.makedirs(ceu, exist_ok=True)
    atras, pintura, frente = camada_atras(), camada_pintura(), camada_frente()
    saidas = [(f1, 'f1_atras', atras), (f1, 'f1_pintura', pintura), (f1, 'f1_frente', frente),
              (f1, 'f1_corpo', corpo(atras, pintura, frente)), (f1, 'f1_roda', roda()), (f1, 'moeda', moeda()),
              (f1, 'galao', galao()), (ceu, 'estrelas', estrelas()), (ceu, 'terra', terra()),
              (ceu, 'sol', sol()), (ceu, 'lua', lua()), (ceu, 'ceu_gradiente', ceu_gradiente()), (f1, 'feixe', feixe())]
    fundo = os.path.join(raiz, 'Fundo')
    os.makedirs(fundo, exist_ok=True)
    saidas += [(fundo, 'montanhas', montanhas()), (fundo, 'morros', morros()), (fundo, 'pinheiros', pinheiros())]
    saidas += [(f1, 'f1_normal', mapa_normal(saidas[3][2], 14, 8)), (f1, 'f1_roda_normal', mapa_normal(saidas[4][2], 10, 6))]
    for pasta, nome, img in saidas:
        caminho = os.path.join(pasta, nome + '.png')
        img.save(caminho)
        print(caminho)
    cs = os.path.join(raiz, '..', 'Scripts', 'Jogo', 'ContornoF1.cs')
    contorno_cs(cs)
    print(cs)
