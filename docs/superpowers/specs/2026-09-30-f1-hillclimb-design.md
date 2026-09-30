# F1 Hill Climb — Design

Data: 2026-09-30
Substitui como entrega principal: `2026-09-30-carro-cg-design.md` (a cena do vídeo continua no projeto como reserva).

## Objetivo

Jogo 2D jogável, estilo Hill Climb Racing, com um carro de Fórmula 1, feito na Unity para o trabalho
final de Computação Gráfica. Visual de jogo indie atual (arte HD, luzes 2D, pós-processamento,
partículas, som). O vídeo de entrega é o aluno jogando e narrando.

As seis técnicas da disciplina são código próprio e aparecem no jogo:

| Técnica | Onde |
|---|---|
| Rasterização | Intro: contorno do F1 rasterizado pixel a pixel (Bresenham) numa grade |
| Preenchimento | Intro: regiões do F1 preenchidas por scanline; terreno preenchido por scanline em textura HD |
| Curva de Bézier | Terreno infinito feito de Bézier cúbicas encadeadas |
| Translação | Movimento do carro, parallax, posicionamento de coletáveis sobre a curva |
| Rotação | Carro inclinando no terreno, rodas girando, torque no ar controlado pelo jogador |
| Espelhamento | Emenda C1 do terreno (controle espelhado); reflexo do carro no lago (matriz de reflexão) |

**Critério de sucesso:** jogo roda do início ao fim (intro → jogar → fim de jogo → reiniciar) sem erros;
visual com luzes, parallax, dia/noite e partículas; cada técnica é identificável para a narração.

## Restrições

- Unity 6000.3.25f1 LTS, projeto existente `TrabFinal/CarroCG`.
- Migrar para **URP com 2D Renderer**. A cena `Main.unity` (vídeo) é convertida e mantida funcionando.
- PC, teclado: → acelera, ← freia/ré (no ar: → e ← giram o carro), R reinicia, Esc pausa, Espaço pula a intro.
- Arte do F1: vetorial gerada por script próprio (`Tools/gerar_f1.py`), sem marcas reais
  (patrocínio fictício "CG Racing"). Cenário e sons: CC0 (Kenney/OpenGameArt) ou gerados.
- Algoritmos da disciplina em funções puras de `Raster.cs`, testadas em EditMode.
- Prazo: 1–3 semanas.

## Fluxo

```
[Intro] --Espaço/fim--> [Jogando] --combustível zero e parado / capacete no chão--> [Fim] --R--> [Jogando]
                              ^--Esc--> [Pausa] --Esc--^
```

- **Intro (~25 s, pulável):** grade escura; contorno do F1 rasterizado (Bresenham); preenchimento
  por scanline região a região; crossfade da versão rasterizada para o sprite HD; câmera abre;
  cenário aparece; carro parado na largada.
- **Jogando:** HUD com combustível (barra), moedas, distância (m) e recorde (`PlayerPrefs`).
- **Fim:** painel com distância, moedas e recorde; R reinicia (recarrega a cena, intro não repete na mesma sessão).

## Terreno

- Sequência infinita de Bézier cúbicas, cada trecho ~20 unidades de largura.
- Emenda: `P0 = P3ant`, `P1 = 2·P3ant − P2ant` (espelhamento de ponto → continuidade C1).
- `P2`, `P3` sorteados com `System.Random(semente)`; amplitude vertical cresce com a distância
  (limitada a um máximo jogável). Primeiro trecho é plano (largada).
- Trecho de lago ocasional (a cada ~8–12 trechos, nunca nos 3 primeiros): trecho reto e horizontal
  (ponte), com água abaixo da linha da ponte e reflexo ativo.
- Por trecho (chunk):
  - Amostragem: 64 pontos da curva.
  - Polígono: pontos amostrados + dois cantos inferiores (fundo fixo abaixo do ponto mais baixo).
  - Textura: `Raster.ScanlineSpans` preenche o polígono em `Texture2D` a 64 px/unidade, filtro bilinear.
    Cor: gradiente vertical de terra; pixels a menos de ~0,35 unidade abaixo da superfície recebem grama
    (com borda clara no topo). Textura limitada a 2048 px por lado (trechos mais altos reduzem a resolução).
  - Colisão: `EdgeCollider2D` com os mesmos 64 pontos.
- Streaming: 3 trechos à frente da câmera e 1 atrás; os demais são destruídos (texturas liberadas).
- Coletáveis: moedas em grupos e galões a intervalos crescentes, posicionados em `Point(t) + normal·altura`.

## F1 e física

- Arte: `Tools/gerar_f1.py` gera `f1_corpo.png` (carroceria, asas, halo, piloto, número, "CG Racing")
  e `f1_roda.png` (pneu, faixa, aro) em alta resolução, e emite os polígonos simplificados da intro
  (mesmas coordenadas, escaladas para a grade) como código C#.
- Chassi: `Rigidbody2D` + `PolygonCollider2D`, centro de massa baixo.
- Rodas: `Rigidbody2D` + `CircleCollider2D`, `WheelJoint2D` com suspensão (frequência e amortecimento no Inspector),
  `PhysicsMaterial2D` de atrito alto.
- Tração traseira: → liga o motor da roda traseira; ← freia/ré. No ar (nenhuma roda tocando o chão),
  → e ← aplicam torque no chassi.
- Sensor no capacete (trigger): tocar o terreno → fim de jogo.
- Combustível (`Combustivel`, classe pura): consumo por tempo + extra ao acelerar; galão reabastece até o máximo;
  em zero o motor desliga; fim de jogo quando zero e velocidade ~0 por 2 s.

### Reflexo (espelhamento)

- Sobre trechos de lago, um "carro-reflexo" (sprites do corpo e rodas) é posicionado a cada frame por
  `Raster.Refletir(posição, ângulo, h)`: `y' = 2h − y`, `ângulo' = −ângulo`, com `escala.y = −1`.
- Tinta da água e alfa ~0,45; visível apenas sobre a área da água (Sprite Mask).

### Câmera

- Segue com `SmoothDamp`, olhando à frente na direção da velocidade.
- Zoom ortográfico aumenta com a velocidade (entre mínimo e máximo).
- Tremor curto em aterrissagens com impacto acima de um limiar.

## Visual, efeitos, áudio

- **Céu/dia-noite** (ciclo ~3 min): gradiente de céu entre dia, pôr do sol, noite e amanhecer; Global Light 2D
  acompanha; sol e lua em arco; estrelas à noite; à noite, luz traseira vermelha e farol do F1 com Light 2D pontual.
- **Parallax:** 3 camadas (Kenney Background Elements, CC0) com fatores 0,1 / 0,3 / 0,6, repetidas.
- **Pós-processamento (Volume):** Bloom leve, Vinheta, Color Adjustments por hora do dia.
- **Partículas:** poeira na roda traseira ao acelerar no chão; faíscas em aterrissagem forte; brilho ao pegar moeda;
  respingo no lago.
- **Áudio:** motor em loop com pitch pela rotação da roda traseira (sintetizado se não houver CC0 adequado);
  moeda, galão, batida, fim de jogo.
- **UI:** TextMeshPro; HUD e painéis (pausa, fim) com fonte e paleta únicas.

## Arquitetura

```
CarroCG/Assets/
├── Scripts/
│   ├── Raster.cs          + BezierJoin (P1 espelhado), Refletir (reflexo)
│   ├── Anim.cs, PixelCanvas.cs              (reutilizados na intro)
│   ├── BezierPath.cs, CarFollower.cs, Director.cs   (cena do vídeo, mantidos)
│   └── Jogo/
│       ├── Terreno.cs       geração/reciclagem de trechos: curva, textura scanline, collider
│       ├── F1.cs            motor, freio, torque no ar, sensor do capacete
│       ├── Combustivel.cs   lógica pura de combustível
│       ├── Reflexo.cs       carro refletido na água
│       ├── CameraSegue.cs   seguir, zoom por velocidade, tremor
│       ├── Parallax.cs      camadas de fundo
│       ├── CicloDia.cs      céu, luz global, sol/lua, pós-processamento
│       ├── Coletaveis.cs    moedas e galões sobre a curva
│       └── Partida.cs       estados (intro/jogando/pausa/fim), HUD, recorde
├── Editor/MontarJogo.cs     monta Scenes/Jogo.unity por script
├── Tests/Editor/            testes existentes + TerrenoTests, ReflexoTests, CombustivelTests
├── Art/F1, Art/Fundo, Audio/
└── Scenes/Main.unity (vídeo), Scenes/Jogo.unity (jogo)
Tools/gerar_f1.py            (fora de Assets)
```

`Terreno` separa a parte pura (gerar pontos de controle de trechos, amostrar, montar polígono, preencher
buffer de cores) da parte Unity (criar Texture2D/Sprite/EdgeCollider2D), para testar a parte pura.

## Testes (EditMode)

- Existentes (16) continuam passando.
- `BezierJoin`: tangente no fim do trecho N == tangente no início do N+1 (C1).
- Terreno: mesma semente → mesmos pontos; primeiro trecho plano; amplitude limitada; polígono fechado;
  pixel logo abaixo da curva preenchido e logo acima vazio; pontos do collider == pontos amostrados.
- `Refletir`: ponto refletido correto; refletir duas vezes volta ao original; ângulo invertido.
- `Combustivel`: consome por tempo e mais ao acelerar; galão reabastece até o máximo; em zero o motor desliga.

Sensação de física (suspensão, torque, atrito) é ajustada jogando; valores expostos no Inspector.
Verificação visual por capturas automáticas (render de câmera em batchmode) antes de entregar.

## Fora de escopo

- Menu/tela de título, loja, upgrades, múltiplos carros ou mapas.
- Modo aula (sobreposição didática por tecla).
- Controle por gamepad/touch, build para outras plataformas.
- Ranking online.
