"""Gera 'Guia para apresentação.pdf' (uma página A4).
Uso: python3 Tools/gerar_guia.py   (precisa de reportlab: pip install reportlab)"""
import os
from reportlab.lib import colors
from reportlab.lib.pagesizes import A4
from reportlab.lib.styles import ParagraphStyle
from reportlab.lib.units import mm
from reportlab.pdfbase import pdfmetrics
from reportlab.pdfbase.ttfonts import TTFont
from reportlab.platypus import Paragraph, SimpleDocTemplate, Spacer, Table, TableStyle

RAIZ = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..')
UI = os.path.join(RAIZ, 'CarroCG', 'Assets', 'Art', 'UI')
pdfmetrics.registerFont(TTFont('Ubuntu', '/usr/share/fonts/truetype/ubuntu/Ubuntu-R.ttf'))
pdfmetrics.registerFont(TTFont('Ubuntu-B', os.path.join(UI, 'Ubuntu-B.ttf')))
pdfmetrics.registerFont(TTFont('Ubuntu-BI', os.path.join(UI, 'Ubuntu-BI.ttf')))
pdfmetrics.registerFont(TTFont('Ubuntu-RI', '/usr/share/fonts/truetype/ubuntu/Ubuntu-RI.ttf'))
pdfmetrics.registerFont(TTFont('DejaVu', '/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf'))   # setas (o Ubuntu não tem)
pdfmetrics.registerFontFamily('Ubuntu', normal='Ubuntu', bold='Ubuntu-B', italic='Ubuntu-RI', boldItalic='Ubuntu-BI')

AZUL = colors.HexColor('#1F3A5F')
base = ParagraphStyle('base', fontName='Ubuntu', fontSize=10, leading=13.2)
titulo = ParagraphStyle('titulo', parent=base, fontName='Ubuntu-BI', fontSize=20, leading=24, textColor=AZUL)
sub = ParagraphStyle('sub', parent=base, textColor=colors.HexColor('#555555'))
secao = ParagraphStyle('secao', parent=base, fontName='Ubuntu-B', fontSize=12.5, leading=15, textColor=AZUL, spaceBefore=8, spaceAfter=1)
cel = ParagraphStyle('cel', parent=base, fontSize=9.4, leading=11.6)
celB = ParagraphStyle('celB', parent=cel, fontName='Ubuntu-B')

def P(t, s=base):
    for seta in '→←':
        t = t.replace(seta, f'<font name="DejaVu">{seta}</font>')
    return Paragraph(t, s)

roteiro = [
    'Intro (~35 s, Espaço pula): F1 3D em <b>wireframe</b> girando (rotação), projeção <b>paralela → perspectiva</b>; '
    'volta à vista lateral; contorno por <b>Bresenham</b>; regiões por <b>scanline</b>; crossfade para a arte HD.',
    'Menu: fundo animado (o próprio jogo) e a tela <b>Técnicas de CG</b> (todas "no jogo").',
    'Garagem: trocar a cor — só a camada de pintura é tingida.',
    'Jogar → Terra: moedas, combustível (barra muda de cor), poeira, tremor e faíscas em aterrissagem forte.',
    '<b>Tab</b> (modo aula): controle da Bézier, emenda espelhada, janela de recorte com o pedaço cortado, normais, '
    'vetor do sol, eixos do F1, janela do minimapa.',
    '<b>F</b>: o jogo congela e o trecho é repintado linha a linha (scanline).',
    '<b>L</b>: Flat → Gouraud → Phong no terreno; ciclo dia/noite, brilho Phong do F1, sombra projetada.',
    '<b>P</b>: parallax por perspectiva x paralela. Minimapa: Cohen-Sutherland + janela→viewport + Bresenham.',
    '<b>Lago</b> (a cada 8–12 trechos): ponte sobre a água e o F1 <b>refletido</b> (y\' = 2h − y), recortado contra a água '
    '(no modo aula: retângulo azul e linha da água).',
    'Lua: gravidade baixa, luz fixa, sombra dura, sem lagos.',
]

tecnicas = [
    ('Curva de Bézier', 'Terreno infinito de Bézier cúbicas (modo aula: controle)', 'TerrenoGerador, Raster.BezierPoint'),
    ('Espelhamento', 'Emenda C1: P1 = 2·P3 − P2; reflexo do F1 no lago', 'Raster.BezierJoin, Raster.Refletir, Reflexo'),
    ('Recorte', 'Sutherland-Hodgman no terreno e no reflexo (com UV); Cohen-Sutherland no minimapa e na intro',
     'Raster.ClipPolygon / ClipLine, ReflexoGeometria'),
    ('Preenchimento', 'Scanline do terreno (F), da sombra e das regiões do F1 na intro', 'Raster.ScanlineSpans, IntroDesenho'),
    ('Rasterização', 'Bresenham: wireframe e contorno na intro; minimapa', 'Raster.Line, IntroDesenho, MinimapaDesenho'),
    ('Iluminação e sombreamento', 'Lambert Flat/Gouraud/Phong (L), shader Phong do F1, sombra projetada',
     'Sombreamento, F1Phong.shader, Sombra'),
    ('Projeções', 'Intro: wireframe paralela → perspectiva (matrizes 4×4); parallax (P)', 'Projecao, ModeloF1, Parallax'),
    ('Translação e rotação', 'Carro, rodas, giro no ar, moedas; rotação 3D (Ry·Rx) da intro', 'F1, Coletaveis, ModeloF1.Rotacao'),
]

historia = [
    P('Guia para apresentação — Senninhas Driving', titulo),
    P('Trabalho final de Computação Gráfica · jogo 2D estilo Hill Climb com um F1 · cada técnica da disciplina é código próprio', sub),
    P('Tecnologias', secao),
    P('<b>C#</b> na engine Unity 6 (URP 2D) — jogo, algoritmos e testes automáticos · <b>HLSL</b> — shaders próprios (F1 com '
      'Phong, terreno com luz por coluna) · <b>Python + Pillow</b> — gera a arte (F1, normal maps, céu, fundo, ícones, logo) e o '
      'contorno do F1 da intro. Sons sintetizados em C#. Algoritmos em <i>Assets/Scripts/Raster.cs</i> e '
      '<i>Assets/Scripts/Jogo/</i>.'),
    P('Antes de apresentar', secao),
    P('Abrir o projeto <b>CarroCG</b> no Unity Hub → cena <i>Assets/Scenes/Menu</i> → Play. Para gravar: Window → General → '
      'Recorder (Movie, MP4, 1920×1080, 60 fps). A intro passa uma vez por sessão.'),
    P('Roteiro sugerido (~6 min)', secao),
]
historia += [P(f'{i}. {t}') for i, t in enumerate(roteiro, 1)]
historia.append(P('Técnicas e onde mostrar', secao))
linhas = [[P('Técnica', celB), P('Onde aparece', celB), P('Código', celB)]] + \
         [[P(a, celB), P(b, cel), P(c, cel)] for a, b, c in tecnicas]
tabela = Table(linhas, colWidths=[35 * mm, 85 * mm, 60 * mm])
tabela.setStyle(TableStyle([
    ('BACKGROUND', (0, 0), (-1, 0), colors.HexColor('#DCE6F2')),
    ('ROWBACKGROUNDS', (0, 1), (-1, -1), [colors.white, colors.HexColor('#F4F7FB')]),
    ('GRID', (0, 0), (-1, -1), 0.4, colors.HexColor('#B8C4D4')),
    ('VALIGN', (0, 0), (-1, -1), 'MIDDLE'),
    ('TOPPADDING', (0, 0), (-1, -1), 3.5), ('BOTTOMPADDING', (0, 0), (-1, -1), 3.5),
]))
historia += [tabela, P('Controles', secao),
             P('→ acelera · ← freia/ré · no ar: setas giram o carro · Tab modo aula · F scanline · L sombreamento · '
               'P projeção · Espaço pula a intro · Esc pausa · R reinicia. Volume em Configurações.'),
             Spacer(1, 3),
             P('<i>Dica: os testes automáticos (EditMode e PlayMode) comprovam cada algoritmo — vale citar no relatório.</i>')]

saida = os.path.join(RAIZ, 'Guia para apresentação.pdf')
SimpleDocTemplate(saida, pagesize=A4, leftMargin=14 * mm, rightMargin=14 * mm, topMargin=12 * mm, bottomMargin=10 * mm,
                  title='Guia para apresentação — Senninhas Driving').build(historia)
print(saida)
