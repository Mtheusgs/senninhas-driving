# Carro CG — Design

Data: 2026-09-30

## Objetivo

Vídeo de animação 2D (~2 min) para o trabalho final de Computação Gráfica, feito na Unity,
mostrando seis técnicas da disciplina: **rasterização, preenchimento, curva de Bézier,
translação, rotação e espelhamento**. A narração é gravada pelo aluno por cima do vídeo;
não há texto explicativo na tela.

**Critério de sucesso:** cada técnica aparece de forma visível e isolada o suficiente para
ser explicada na narração, e o código dos algoritmos pode ser citado no relatório.

## Restrições

- Unity 6 LTS (6000.x), template "2D (Built-In Render Pipeline)", projeto em `TrabFinal/CarroCG`.
- Pode usar recursos da engine; os algoritmos da disciplina (Bresenham, scanline, Bézier)
  são implementados à mão para ficarem visíveis.
- Estilo pixel art. O carro é geometria própria (polígonos) rasterizada em tempo real;
  o cenário usa sprites pixel art prontos (pack gratuito do itch.io ou Asset Store).
- Gravação via Unity Recorder: MP4 1920×1080, 60 fps.

## Roteiro

| Tempo       | Cena       | Técnica                | Conteúdo |
|-------------|------------|------------------------|----------|
| 0:00–0:10   | Abertura   | —                      | Tela escura com grade de pixels ampliada. |
| 0:10–0:35   | Contorno   | Rasterização           | Arestas dos polígonos do carro acendem pixel a pixel (Bresenham). |
| 0:35–0:55   | Pintura    | Preenchimento          | Cada região (lataria, janelas, rodas) é preenchida linha a linha (scanline). |
| 0:55–1:05   | Transição  | —                      | Câmera se afasta, grade some, carro aparece sobre o cenário. |
| 1:05–1:15   | Pista      | Curva de Bézier        | Estrada traçada progressivamente; pontos de controle e linhas-guia visíveis e depois somem. |
| 1:15–1:45   | Percurso   | Translação + Rotação   | Carro segue a curva, rotacionado conforme a tangente. |
| 1:45–1:55   | Volta      | Espelhamento           | Carro espelha (`scale.x = -1`) e percorre a curva de volta. |
| 1:55–2:05   | Final      | —                      | Carro para, fade out. |

Durações e pausas de cada fase ficam expostas no Inspector, para sincronizar com a narração.

## Arquitetura

```
CarroCG/Assets/
├── Scripts/
│   ├── Raster.cs        funções puras (static): Bresenham, scanline, Bézier
│   ├── PixelCanvas.cs   Texture2D do carro; anima contorno e preenchimento
│   ├── BezierPath.cs    pista: 4 pontos de controle, LineRenderers
│   ├── CarFollower.cs   move/rotaciona/espelha o carro ao longo da pista
│   ├── Director.cs      coroutine que encadeia as fases do roteiro
│   └── Anim.cs          helper: chama f(k), k de 0 a 1, ao longo de N segundos
├── Tests/Editor/        RasterTests, PixelCanvasTests, CarFollowerTests
├── Art/                 sprites do cenário
└── Scenes/Main.unity
```

### Raster.cs (static, sem MonoBehaviour)

- `IEnumerable<Vector2Int> Line(Vector2Int a, Vector2Int b)` — Bresenham para todos os octantes.
- `IEnumerable<(int y, int xStart, int xEnd)> ScanlineSpans(Vector2Int[] poly)` — para cada
  linha y, interseções com as arestas, ordenadas e pareadas; regra de vértice: aresta cobre
  `[yMin, yMax)` para não contar vértices em dobro.
- `Vector2 BezierPoint(Vector2 p0, p1, p2, p3, float t)` — forma de Bernstein.
- `Vector2 BezierTangent(Vector2 p0, p1, p2, p3, float t)` — derivada da cúbica.

Retornar sequências (em vez de pintar direto) permite que o chamador anime passo a passo.

### PixelCanvas.cs

- `Texture2D` de ~48×24, `filterMode = Point`, exibida num `SpriteRenderer` ampliado.
- Lista serializada de polígonos `{ Vector2Int[] vertices; Color cor; }` definindo o carro
  (lataria, janelas, rodas como octógonos). Formato inicial proposto no código; ajustável no Inspector.
- Coroutines `DesenharContornos(duracao)` e `Preencher(duracao)`: cada pixel do contorno e cada
  linha do scanline é um passo; os passos são distribuídos ao longo da duração pedida.
- Grade de pixels desenhada por um sprite/overlay que pode ser escondido na transição.
- A textura final é o próprio sprite do carro usado no percurso.

### BezierPath.cs

- 4 `Transform` como pontos de controle (arrastáveis na cena).
- Expõe `Point(t)` e `Tangent(t)` delegando para `Raster`.
- `LineRenderer` da estrada (revelado progressivamente) e `LineRenderer` das linhas-guia
  P0–P1–P2–P3 (mostrado e depois escondido). Gizmos no editor.

### CarFollower.cs

- Translação: `transform.position = path.Point(t)`.
- Rotação: `angle = Atan2(tangente.y, tangente.x)` na ida **e** na volta. Com `scale.x = -1` o eixo
  "frente" local aponta para -x, então a mesma fórmula faz o carro olhar no sentido da volta sem
  ficar de cabeça para baixo (inverter a tangente o deixaria de ponta-cabeça).
- Espelhamento: ao chegar em `t = 1`, `localScale.x *= -1` e `t` passa a decrescer.
- Velocidade parametrizada por `t` (não por comprimento de arco) — variação de velocidade aceita;
  trocar por reparametrização por arco se incomodar.

### Director.cs

Uma coroutine executa em sequência: contorno → preenchimento → transição (zoom da câmera,
esconder grade, mostrar cenário) → pista → percurso → volta → fade. Tempos e pausas serializados.

## Testes

`RasterTests.cs` (EditMode, Unity Test Framework):

- Bresenham: (0,0)→(5,2) produz os pixels esperados; linhas horizontal, vertical e com
  inclinação > 1 são contínuas (vizinhos 8-conectados, sem buracos) e incluem os extremos.
- Scanline: quadrado 4×4 gera exatamente 16 pixels.
- Bézier: `Point(0) == P0`, `Point(1) == P3`, `Tangent(0)` paralela a `P1 - P0`.

## Gravação

Unity Recorder (Movie, MP4, 1920×1080, 60 fps) iniciado manualmente com o Play Mode.
Narração e edição final em editor externo (Kdenlive/Shotcut/CapCut).

## Fora de escopo

- Texto/legendas na tela.
- Início automático da gravação pelo `Director`.
- Timeline/Animator.
- Velocidade constante por comprimento de arco (só se necessário).
