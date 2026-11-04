# F1 Hill Climb — Design

Data: 2026-09-30 (revisado 2026-11-04: recorte, iluminação/sombreamento, projeções, modo aula)
Substitui como entrega principal: `2026-09-30-carro-cg-design.md` (a cena do vídeo continua no projeto como reserva).

## Objetivo

Jogo 2D jogável, estilo Hill Climb Racing, com um carro de Fórmula 1, feito na Unity para o trabalho
final de Computação Gráfica. Visual de jogo indie atual (arte HD, luzes 2D, pós-processamento,
partículas, som). O vídeo de entrega é o aluno jogando e narrando.

As técnicas da disciplina são **código próprio** (funções puras testadas) e aparecem no jogo:

| Técnica | Onde |
|---|---|
| Projeção paralela e perspectiva | Intro: F1 3D em wireframe projetado por matrizes próprias (paralela → perspectiva); jogo: parallax calculado pela projeção perspectiva, tecla P troca para paralela |
| Rasterização | Intro: arestas do wireframe e contorno do F1 rasterizados com Bresenham; minimapa desenhado com Bresenham |
| Preenchimento | Intro: regiões do F1 por scanline; terreno preenchido por scanline em textura HD; sombra do F1 |
| Curva de Bézier | Terreno infinito feito de Bézier cúbicas encadeadas |
| Recorte | Sutherland–Hodgman: polígono do terreno contra o chunk, reflexo contra a água; Cohen–Sutherland: terreno no minimapa (com janela → viewport) |
| Iluminação e sombreamento | Terreno: Lambert com modos Flat/Gouraud/Phong (tecla L), luz do sol do ciclo dia/noite; F1: shader Phong próprio com normal map; sombra projetada do F1 |
| Translação | Movimento do carro, parallax, coletáveis posicionados sobre a curva |
| Rotação | Carro inclinando, rodas girando, torque no ar; giro do wireframe 3D na intro |
| Espelhamento | Emenda C1 do terreno (controle espelhado); reflexo do carro no lago (matriz de reflexão) |

**Critério de sucesso:** jogo roda do início ao fim (intro → jogar → fim → reiniciar) sem erros; visual com
luzes, parallax, dia/noite e partículas; cada técnica é identificável na narração, com o modo aula ligado.

## Restrições

- Unity 6000.3.25f1 LTS, projeto existente `TrabFinal/CarroCG`.
- Migrar para **URP com 2D Renderer** (configurações copiadas do template 2D da própria Unity).
  A cena `Main.unity` (vídeo) é convertida (materiais unlit do URP) e mantida funcionando.
- PC, teclado:
  - → acelera, ← freia/ré; no ar, → e ← giram o carro
  - R reinicia, Esc pausa, Espaço pula a intro
  - Tab liga/desliga o modo aula, L troca o sombreamento (Flat/Gouraud/Phong), P troca a projeção (perspectiva/paralela)
- Arte do F1: vetorial gerada por script próprio (`Tools/gerar_f1.py`), com normal map, sem marcas reais
  (patrocínio fictício "CG Racing"). Cenário e sons: CC0 (Kenney) ou gerados.
- Algoritmos da disciplina em funções puras (`Raster`, `Projecao`, `Sombreamento`), testadas em EditMode.
- Prazo: 1–3 semanas. Entrega em **etapas jogáveis** (ver Ordem de entrega).

## Fluxo

```
[Intro] --Espaço/fim--> [Jogando] --combustível zero e parado / capacete no chão--> [Fim] --R--> [Jogando]
                              ^--Esc--> [Pausa] --Esc--^
```

- **Intro (~35 s, pulável):**
  1. F1 3D em wireframe girando, projetado em **paralela** e interpolando para **perspectiva**; arestas
     desenhadas com Bresenham numa textura.
  2. O modelo gira até a vista lateral, volta para paralela e se encaixa sobre o contorno 2D.
  3. Contorno rasterizado (Bresenham) e preenchimento por scanline região a região.
  4. Crossfade para o sprite HD; câmera abre; cenário aparece; carro parado na largada.
- **Jogando:** HUD com combustível (barra), moedas, distância (m), recorde (`PlayerPrefs`) e minimapa.
- **Fim:** painel com distância, moedas e recorde; R reinicia (recarrega a cena; a intro não repete na mesma sessão).

## Terreno

- Sequência infinita de Bézier cúbicas, cada trecho ~20 unidades de largura.
- Emenda: `P0 = P3ant`, `P1 = 2·P3ant − P2ant` (espelhamento de ponto → continuidade C1).
- `P2`, `P3` sorteados com `System.Random(semente)`; amplitude vertical cresce com a distância
  (limitada a um máximo jogável). Primeiro trecho é plano (largada).
- Trecho de lago ocasional (a cada ~8–12 trechos, nunca nos 3 primeiros): trecho reto e horizontal
  (ponte), com água abaixo da linha da ponte e reflexo ativo.
- Por trecho (chunk):
  - Amostragem: 64 pontos da curva, com a normal de cada ponto (perpendicular à tangente).
  - Polígono: pontos amostrados + dois cantos inferiores; **recortado por Sutherland–Hodgman** contra o
    retângulo do chunk (largura do trecho × altura máxima de textura, 2048 px).
  - Textura: `Raster.ScanlineSpans` preenche o polígono recortado em `Texture2D` a 64 px/unidade, filtro bilinear.
    Cor base: gradiente vertical de terra; faixa de grama a menos de ~0,35 unidade abaixo da superfície,
    com borda clara.
  - Iluminação: cor final = base × (ambiente + difusa·max(0, N·L)), com `L` = direção do sol (ver Iluminação).
  - Colisão: `EdgeCollider2D` com os mesmos 64 pontos.
- Streaming: 3 trechos à frente da câmera e 1 atrás; os demais são destruídos (texturas liberadas).
- Coletáveis: moedas em grupos e galões a intervalos crescentes, posicionados em `Point(t) + normal·altura`.

## F1 e física

- Arte: `Tools/gerar_f1.py` gera `f1_corpo.png`, `f1_corpo_normal.png` (normal map), `f1_roda.png`,
  `f1_roda_normal.png` em alta resolução, e emite como código C# os polígonos da intro e os vértices/arestas
  do modelo 3D em wireframe (mesmas proporções).
- Chassi: `Rigidbody2D` + `PolygonCollider2D`, centro de massa baixo.
- Rodas: `Rigidbody2D` + `CircleCollider2D`, `WheelJoint2D` com suspensão (frequência e amortecimento no Inspector),
  `PhysicsMaterial2D` de atrito alto.
- Tração traseira: → liga o motor da roda traseira; ← freia/ré. No ar (nenhuma roda tocando o chão),
  → e ← aplicam torque no chassi.
- Sensor no capacete (trigger): tocar o terreno → fim de jogo.
- Combustível (`Combustivel`, classe pura): consumo por tempo + extra ao acelerar; galão reabastece até o máximo;
  em zero o motor desliga; fim de jogo quando zero e velocidade ~0 por 2 s.

### Reflexo (espelhamento + recorte)

- Sobre trechos de lago, o reflexo do corpo e das rodas é posicionado a cada frame por
  `Raster.Refletir(posição, ângulo, h)`: `y' = 2h − y`, `ângulo' = −ângulo`, `escala.y = −1`.
- Renderizado como malha: o quad de cada sprite, refletido, é **recortado por Sutherland–Hodgman** contra o
  retângulo da água; a UV de cada vértice recortado é obtida pela inversa da transformação do quad.
- Tinta da água e alfa ~0,45.

### Sombra projetada

- O contorno simplificado do F1 é projetado na direção oposta ao sol até a superfície do terreno
  (interseção do raio com a curva amostrada); o polígono resultante é preenchido com preto translúcido
  sob o carro. Some à noite.

### Câmera

- Segue com `SmoothDamp`, olhando à frente na direção da velocidade.
- Zoom ortográfico aumenta com a velocidade (entre mínimo e máximo).
- Tremor curto em aterrissagens com impacto acima de um limiar.

## Recorte

- `Raster.ClipPolygon(poly, rect)` — **Sutherland–Hodgman**: recorta contra as 4 bordas em sequência.
  Variante com atributo (UV) interpolado para o reflexo.
- `Raster.ClipLine(a, b, rect, out a', out b')` — **Cohen–Sutherland**: códigos de região de 4 bits,
  aceitação/rejeição trivial e cortes sucessivos.
- `Raster.JanelaViewport(p, janela, viewport)` — mapeamento janela → viewport.
- **Minimapa** (canto do HUD): perfil do terreno em torno do carro; segmentos da curva recortados por
  Cohen–Sutherland contra a janela, mapeados para a viewport e desenhados com Bresenham numa textura pequena;
  o carro é um ponto.

## Iluminação e sombreamento

- `Sombreamento.Lambert(N, L, ambiente, difusa)` → intensidade.
- Terreno, por tecla L:
  - **Flat:** uma normal por segmento amostrado (constante entre dois pontos).
  - **Gouraud:** intensidade calculada nos pontos amostrados e interpolada linearmente entre eles.
  - **Phong:** normal interpolada entre os pontos e intensidade calculada por pixel.
  - O sol se move, então os chunks visíveis são re-sombreados a cada ~0,5 s (polígono e cor base guardados).
- F1: `Shaders/F1Phong.shader` (unlit, HLSL próprio) com **Phong** = ambiente + difusa·max(0,N·L) +
  especular·max(0,R·V)^n, usando o normal map; recebe direção e cor do sol por variáveis globais do shader,
  definidas pelo `CicloDia`.
- `Sombreamento.Phong(N, L, V, ...)` em C# espelha a fórmula do shader para teste.

## Projeções

- `Projecao.Ortografica()`, `Projecao.Perspectiva(d)`, `Projecao.Projetar(M, v)` (matrizes 4×4 próprias,
  divisão homogênea), `Projecao.Interpolar(a, b, k)`.
- **Intro:** modelo 3D (~60 vértices: chassi, asas, rodas como prismas de 12 lados) girado por matriz de rotação,
  projetado e desenhado com Bresenham.
- **Parallax:** cada camada de fundo tem profundidade `z` (ex.: 5, 20, 60); deslocamento e escala na tela vêm de
  `d/(d+z)` aplicado a `(x − cam.x)`. Tecla P: projeção paralela (fator 1 para todas as camadas, sem profundidade).

## Modo aula (Tab)

Sobreposição desenhada por cima do jogo, sem afetar a jogabilidade:
- Pontos de controle e polígono de controle das Bézier visíveis.
- Janela de recorte do chunk e da água, com as partes cortadas em vermelho.
- Normais do terreno (setas curtas) e vetor do sol.
- Eixos locais do F1 (rotação) e linha da água (reflexão).
- Rótulo: `Sombreamento: <modo> · Projeção: <modo> · Recorte: ON`.

## Visual, efeitos, áudio

- **Céu/dia-noite** (ciclo ~3 min): gradiente de céu entre dia, pôr do sol, noite e amanhecer; Global Light 2D
  acompanha; sol e lua em arco (definem `L`); estrelas à noite; à noite, luz traseira vermelha e farol do F1 com Light 2D pontual.
- **Parallax:** 3 camadas (Kenney Background Elements, CC0: montanhas, morros, árvores), tingidas pela hora do dia.
- **Pós-processamento (Volume):** Bloom leve, Vinheta, Color Adjustments por hora do dia.
- **Partículas:** poeira na roda traseira ao acelerar no chão; faíscas em aterrissagem forte; brilho ao pegar moeda;
  respingo no lago.
- **Áudio:** motor em loop com pitch pela rotação da roda traseira (sintetizado); moeda, galão, batida,
  fim de jogo (Kenney Impact/Interface Sounds, CC0).
- **UI:** TextMeshPro; HUD e painéis (pausa, fim) com fonte e paleta únicas.

## Arquitetura

```
CarroCG/Assets/
├── Scripts/
│   ├── Raster.cs          + BezierJoin, Refletir, ClipPolygon, ClipLine, JanelaViewport
│   ├── Projecao.cs        matrizes 4×4, ortográfica, perspectiva, projetar, interpolar
│   ├── Sombreamento.cs    Lambert, Phong, Flat/Gouraud/Phong ao longo de uma polilinha
│   ├── Anim.cs, PixelCanvas.cs              (reutilizados na intro)
│   ├── BezierPath.cs, CarFollower.cs, Director.cs   (cena do vídeo, mantidos)
│   └── Jogo/
│       ├── Terreno.cs       trechos: curva, recorte, textura scanline + iluminação, collider, streaming
│       ├── F1.cs            motor, freio, torque no ar, sensor do capacete
│       ├── Combustivel.cs   lógica pura de combustível
│       ├── Reflexo.cs       malha refletida e recortada na água
│       ├── SombraF1.cs      sombra projetada
│       ├── CameraSegue.cs   seguir, zoom por velocidade, tremor
│       ├── Parallax.cs      camadas com profundidade, perspectiva/paralela
│       ├── CicloDia.cs      céu, luz global, sol/lua, variáveis do shader, pós-processamento
│       ├── Coletaveis.cs    moedas e galões sobre a curva
│       ├── Minimapa.cs      recorte de linha + janela→viewport + Bresenham
│       ├── IntroF1.cs       wireframe 3D projetado + rasterização + crossfade
│       ├── ModoAula.cs      sobreposição didática
│       └── Partida.cs       estados (intro/jogando/pausa/fim), HUD, recorde, teclas L/P/Tab
├── Shaders/F1Phong.shader
├── Editor/MontarJogo.cs     monta Scenes/Jogo.unity por script
├── Editor/ConfigurarURP.cs  liga o URP (pipeline e níveis de qualidade)
├── Settings/                URP asset, Renderer2D, volume profile
├── Tests/Editor/            existentes + Terreno, Recorte, Projecao, Sombreamento, Reflexo, Combustivel
├── Art/F1, Art/Fundo, Audio/
└── Scenes/Main.unity (vídeo), Scenes/Jogo.unity (jogo)
Tools/gerar_f1.py            (fora de Assets)
```

`Terreno` separa a parte pura (pontos de controle, amostragem, polígono, recorte, buffer de cores) da parte
Unity (Texture2D/Sprite/EdgeCollider2D), para testar a parte pura.

## Testes (EditMode)

- Existentes (16) continuam passando.
- `BezierJoin`: tangente no fim do trecho N == tangente no início do N+1 (C1).
- Terreno: mesma semente → mesmos pontos; primeiro trecho plano; amplitude limitada; polígono fechado;
  pixel logo abaixo da curva preenchido e logo acima vazio; pontos do collider == pontos amostrados.
- Sutherland–Hodgman: polígono todo dentro inalterado; todo fora → vazio; triângulo cortado por uma borda →
  vértices esperados; UV interpolada no ponto de corte.
- Cohen–Sutherland: aceitação trivial, rejeição trivial, corte em duas bordas.
- Janela → viewport: cantos mapeiam em cantos; centro em centro.
- Lambert: N ∥ L → máximo; N ⟂ L ou oposto → só ambiente.
- Gouraud: intensidade no meio do segmento = média dos extremos; Flat: constante no segmento.
- Phong especular: máximo com R alinhado a V.
- Projeção: ortográfica preserva x, y; perspectiva aproxima do centro pontos com z maior; interpolação a 0,5
  fica entre as duas.
- Sombra: ponto projetado cai sobre a superfície, no lado oposto ao sol.
- `Refletir`: ponto refletido correto; refletir duas vezes volta ao original; ângulo invertido.
- `Combustivel`: consome por tempo e mais ao acelerar; galão reabastece até o máximo; em zero o motor desliga.

Sensação de física (suspensão, torque, atrito) é ajustada jogando; valores expostos no Inspector.
Verificação visual por capturas automáticas (render de câmera em batchmode) antes de cada entrega de etapa.

## Ordem de entrega (etapas jogáveis)

1. **Base jogável:** URP, terreno Bézier + scanline + recorte de polígono, F1 com física, câmera, combustível,
   moedas, fim de jogo, HUD.
2. **Iluminação:** ciclo dia/noite, Lambert Flat/Gouraud/Phong no terreno (L), shader Phong do F1, sombra projetada.
3. **Projeções e recorte de linha:** parallax por perspectiva (P), minimapa com Cohen–Sutherland.
4. **Lago:** reflexo recortado.
5. **Intro:** wireframe 3D projetado → rasterização → preenchimento → HD.
6. **Modo aula, efeitos e som:** sobreposição (Tab), partículas, pós-processamento, áudio, polimento.

Cada etapa termina com o jogo rodando e capturas de verificação; se o prazo apertar, grava-se com o que estiver pronto.

## Fora de escopo

- Menu/tela de título, loja, upgrades, múltiplos carros ou mapas.
- Cena de laboratório separada.
- Controle por gamepad/touch, build para outras plataformas.
- Ranking online.
