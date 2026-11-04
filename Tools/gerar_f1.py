"""Gera a arte HD do F1 (vista lateral, frente para +x), da roda e dos coletáveis.
Uso: python3 -I Tools/gerar_f1.py CarroCG/Assets/Art/F1
Coordenadas em pixels de uma tela 960x300, y para baixo. No Unity: PPU 256 (F1) e 200 (coletáveis).
Layout usado pelo Editor/MontarJogo.cs: rodas em (190,178) e (770,178), raio 92; capacete em (515,92)."""
import math, os, sys
from PIL import Image, ImageChops, ImageDraw, ImageFont

S = 4                                   # supersampling
W, H = 960, 300
RODA_R = 92
VERMELHO, VERMELHO_ESC = (208, 26, 42, 255), (120, 12, 22, 255)
BRANCO, PRETO, CINZA = (245, 245, 245, 255), (24, 24, 28, 255), (70, 72, 80, 255)
AMARELO, OURO, OURO_ESC = (250, 204, 40, 255), (250, 196, 40, 255), (176, 124, 14, 255)

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

CHASSI = [(105, 205), (115, 150), (230, 134), (330, 126), (440, 118), (560, 116), (650, 126),
          (770, 150), (880, 178), (935, 192), (935, 205)]
COBERTURA = [(240, 136), (290, 78), (350, 56), (420, 58), (466, 96), (470, 122), (240, 138)]

def corpo():
    img = Image.new('RGBA', (W * S, H * S))
    d = ImageDraw.Draw(img)
    # suspensões (ficam atrás das rodas)
    for a, b in [((250, 150), (190, 178)), ((250, 195), (190, 178)), ((700, 150), (770, 178)), ((700, 190), (770, 178))]:
        d.line(s([a, b]), fill=CINZA, width=4 * S)
    # asa traseira
    d.polygon(s([(150, 80), (165, 80), (200, 136), (186, 138)]), fill=CINZA)
    d.polygon(s([(50, 34), (178, 34), (178, 54), (50, 58)]), fill=PRETO)
    d.polygon(s([(60, 66), (170, 64), (170, 78), (60, 80)]), fill=PRETO)
    d.polygon(s([(58, 30), (84, 30), (96, 150), (70, 150)]), fill=VERMELHO)
    # assoalho
    d.polygon(s([(120, 200), (880, 200), (880, 214), (120, 214)]), fill=PRETO)
    # chassi + cobertura do motor
    mascara = Image.new('L', img.size)
    dm = ImageDraw.Draw(mascara)
    for poly in (CHASSI, COBERTURA):
        d.polygon(s(poly), fill=VERMELHO)
        dm.polygon(s(poly), fill=255)
    d.polygon(s([(250, 166), (860, 182), (860, 190), (250, 176)]), fill=BRANCO)             # faixa
    d.polygon(s([(330, 128), (352, 126), (352, 150), (330, 152)]), fill=PRETO)              # entrada do sidepod
    d.ellipse(s([(430, 66), (452, 86)]), fill=PRETO)                                        # airbox
    d.ellipse(s([(338, 148), (382, 192)]), fill=BRANCO)                                     # número
    d.text((360 * S, 170 * S), '7', font=fonte(30), fill=PRETO, anchor='mm')
    d.text((372 * S, 104 * S), 'CG RACING', font=fonte(16), fill=BRANCO, anchor='mm')
    img = sombrear(img, mascara, 56, 214)
    d = ImageDraw.Draw(img)
    for poly in (CHASSI, COBERTURA):
        d.line(s(poly + [poly[0]]), fill=VERMELHO_ESC, width=3 * S, joint='curve')
    # cockpit, piloto, halo
    d.polygon(s([(470, 118), (476, 100), (560, 100), (575, 118)]), fill=PRETO)
    d.ellipse(s([(489, 66), (541, 118)]), fill=AMARELO)
    d.polygon(s([(520, 82), (541, 84), (540, 96), (518, 94)]), fill=PRETO)
    d.line(s([(468, 118), (480, 74), (560, 70), (598, 114)]), fill=CINZA, width=10 * S, joint='curve')
    # asa dianteira
    d.polygon(s([(800, 212), (955, 208), (955, 222), (800, 224)]), fill=PRETO)
    d.polygon(s([(820, 198), (950, 192), (950, 204), (820, 208)]), fill=PRETO)
    d.polygon(s([(930, 178), (956, 178), (956, 224), (930, 224)]), fill=VERMELHO)
    return reduzir(img, W, H)

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
    d.ellipse(s([(48, 62), (80, 98)]), fill=BRANCO)                                         # gota
    d.polygon(s([(50, 72), (64, 44), (78, 72)]), fill=BRANCO)
    return reduzir(img, n, n)

if __name__ == '__main__':
    saida = sys.argv[1]
    os.makedirs(saida, exist_ok=True)
    for nome, fazer in [('f1_corpo', corpo), ('f1_roda', roda), ('moeda', moeda), ('galao', galao)]:
        caminho = os.path.join(saida, nome + '.png')
        fazer().save(caminho)
        print(caminho)
