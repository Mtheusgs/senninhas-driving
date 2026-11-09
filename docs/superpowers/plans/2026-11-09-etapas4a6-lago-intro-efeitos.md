# Senninhas Driving — Etapas 4 a 6: Lago, Intro e Efeitos/Som — Plano de Implementação

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Fechar o jogo: **lago** com ponte e reflexo do F1 (matriz de reflexão + Sutherland–Hodgman com UV), **intro** com o F1 3D em wireframe (rotação + projeção paralela→perspectiva, Bresenham, scanline por região, crossfade para o HD) e **efeitos/som** (partículas, tremor de câmera, pós-processamento, áudio sintetizado com volume no menu).

**Architecture:** Cada técnica nova é uma função pura testada (`TerrenoGerador` decide os lagos, `Raster.Refletir`, `Raster.ClipPolygon` com UV, `ReflexoGeometria`, `ModeloF1`, `IntroDesenho`, `SomSintetizado`); os componentes (`Reflexo`, `IntroF1`, `Efeitos`, `Som`) só aplicam. Cenas continuam montadas pelos scripts de Editor.

**Tech Stack:** Unity 6000.3.25f1, URP 17.3 2D (Volume de pós-processamento embutido no URP), uGUI/TMP, Python 3 + Pillow.

**Spec:** `docs/superpowers/specs/2026-09-30-f1-hillclimb-design.md` (Terreno — lago; Reflexo; Fluxo — Intro; Projeções — intro; Visual, efeitos, áudio; Ordem de entrega 4–6), com `docs/superpowers/specs/2026-11-04-menu-modo-aula-design.md` (volume só na etapa 6; selos da tela Técnicas; modo aula cresce por etapa).

## Global Constraints

- Unity 6000.3.25f1, URP 2D; testes em batchmode com o Editor fechado; nunca duas Unity no mesmo projeto (`timeout 1500`).
- Algoritmos da disciplina em funções puras testadas; componentes só aplicam. Nenhum pacote novo; nenhum asset baixado (sons sintetizados em C#).
- Lago: trecho reto e horizontal (ponte) **a cada ~8–12 trechos, nunca nos 3 primeiros**; só na Terra.
- Reflexo: `y' = 2h − y`, ângulo' = −ângulo, escala.y = −1; quad de cada sprite refletido e **recortado por Sutherland–Hodgman** contra o retângulo da água, UV interpolada; tinta da água e **alfa ~0,45**.
- Intro: **~35 s, pulável com Espaço**; não repete na mesma sessão (vale para R e para voltar do menu).
- Modelo 3D do F1: **~60 vértices** (chassi, asas, rodas como polígonos de 12 lados), girado por matriz de rotação e projetado por `Projecao` (paralela → perspectiva).
- Áudio: motor em loop com **pitch pela rotação da roda traseira**; moeda, galão, batida, fim. Volume salvo em `Preferencias`, opção no menu Configurações.
- Textos só Latin-1 (`-`, `·`). Cenas remontadas por script (`Tools/montar_cenas.sh`); testes não gravam nas chaves reais de `PlayerPrefs`.

## Review Focus

1. **Carro capotando/caindo sobre o lago** — o reflexo nunca aparece acima da linha da água nem fora do trecho do lago. Teste: `ReflexoGeometriaTests.QuadAcimaDaAgua_TodoRecortado` e `ReflexoGeometriaTests.NadaAcimaDaLinha` (Task 1).
2. **Lago logo depois de um trecho íngreme** — a emenda continua C1 e a ponte é horizontal. Teste: `TerrenoLagoTests.TrechoAntesDoLago_TerminaHorizontal` (Task 1).
3. **R ou voltar ao menu e jogar de novo na mesma sessão** — a intro não repete. Teste: `IntroTests.Intro_NaoRepeteNaSessao` (Task 4).
4. **Espaço logo no primeiro frame da intro** — pula sem deixar carro invisível, HUD escondido ou jogo congelado. Teste: `IntroTests.PularNoInicio_JogoNormal` (Task 4).
5. **Volume 0** — nenhum som audível e nenhum erro; pausa congela o motor. Teste: `SomTests.VolumeZero_Mudo` (Task 5).

## Estrutura de arquivos

```
Tools/gerar_f1.py                    + escreve Scripts/Jogo/ContornoF1.cs (regiões 2D da intro)
Tools/montar_cenas.sh                (novo) remonta Jogo e Menu em batchmode
CarroCG/Assets/
├── Scripts/Raster.cs                + Refletir, ClipPolygon com UV
├── Scripts/Jogo/
│   ├── TerrenoGerador.cs            + lagos (EhLago, ponte horizontal)
│   ├── ReflexoGeometria.cs          (novo, puro) quad → refletido → recortado
│   ├── Reflexo.cs                   (novo) malhas do reflexo sobre o lago
│   ├── Terreno.cs                   + água no trecho do lago
│   ├── ModeloF1.cs                  (novo, puro) vértices/arestas 3D, rotação
│   ├── ContornoF1.cs                (gerado) polígonos 2D e cores
│   ├── IntroDesenho.cs              (novo, puro) wireframe/contorno/preenchimento num buffer
│   ├── IntroF1.cs                   (novo) roteiro da intro
│   ├── SomSintetizado.cs            (novo, puro) amostras dos sons
│   ├── Som.cs                       (novo) fontes de áudio, pitch do motor
│   ├── Efeitos.cs                   (novo) partículas, tremor, pós por hora do dia
│   └── Partida.cs, ModoAula.cs, TecnicasCG.cs, Preferencias.cs, Menu.cs, CameraSegue.cs
├── Editor/MontagemComum.cs, MontarJogo.cs, MontarMenu.cs
└── Tests/Editor (+ TerrenoLago, ReflexoGeometria, ModeloF1, IntroDesenho, SomSintetizado), Tests/PlayMode (+ Lago, Intro, Som, Efeitos)
```

---

### Task 1: Lago e reflexo — partes puras

**Files:**
- Modify: `Scripts/Jogo/TerrenoGerador.cs`, `Scripts/Raster.cs`
- Create: `Scripts/Jogo/ReflexoGeometria.cs`
- Test: `Tests/Editor/TerrenoLagoTests.cs`, `Tests/Editor/ReflexoGeometriaTests.cs`, `Tests/Editor/RasterTests.cs`

**Interfaces:**
- Produces:
  - `Trecho.lago` (bool).
  - `TerrenoGerador.lagos` (bool, padrão false). Os índices dos lagos vêm de um `System.Random(semente + 1)` próprio, para não mudar o terreno já gerado: o primeiro lago fica no índice `Next(4, 8)` e os seguintes `+ Next(8, 13)`.
  - `bool TerrenoGerador.EhLago(int indice)`.
  - Trecho de lago: `p0 = p3ant`, `p1 = BezierJoin`, `p2 = (x0 + 2L/3, y0)`, `p3 = (x0 + L, y0)`. O trecho anterior ao lago sai com `p2.y = p3.y`, para que a emenda continue C1 e horizontal.
  - `Vector2 Raster.Refletir(Vector2 p, float h)` → `(p.x, 2h − p.y)`.
  - `List<Vector2> Raster.ClipPolygon(IReadOnlyList<Vector2> poly, Rect r, IReadOnlyList<Vector2> uv, out List<Vector2> uvRecortado)`: as duas versões compartilham `ClipBorda`, que passa a trabalhar sobre `Vector4` (xy = posição, zw = UV).
  - `static bool ReflexoGeometria.Quad(Vector2[] cantos, Vector2[] uvs, float h, Rect agua, List<Vector2> verts, List<Vector2> uvsSaida)`: reflete os 4 cantos, recorta contra `agua` e devolve false quando não sobra nada.

- [ ] **Step 1: testes**
  - `TerrenoLagoTests`:
    - `Lagos_NuncaNosTresPrimeiros_ECada8a12`: 200 trechos com `lagos = true`; os índices dos lagos são `≥ 4` e a distância entre lagos consecutivos fica em `[8, 12]`.
    - `TrechoDoLago_Horizontal`: `p0.y == p1.y == p2.y == p3.y`.
    - `TrechoAntesDoLago_TerminaHorizontal`: `p2.y == p3.y`.
    - `SemLagos_TerrenoIgualAoDeAntes`: com `lagos = false` e a mesma semente, a sequência de `p3` é igual à de um gerador novo.
    - `Lagos_NaoMudamOsTrechosAntesDoPrimeiro`.
  - `RasterTests`:
    - `Refletir_EspelhaEmH`: `(3, 5)` em h=1 → `(3, −3)`.
    - `ClipPolygonUV_InterpolaNaBorda`: quad `(0,0)-(2,0)-(2,2)-(0,2)` com UV 0..1, cortado por `Rect(0,0,1,2)`; vértice em x=1 → u = 0,5.
  - `ReflexoGeometriaTests`:
    - `QuadAbaixoDaLinha_SoEspelhado`: o quad inteiro acima de h vira um quad abaixo de h, com y espelhados.
    - `NadaAcimaDaLinha`: todos os vértices de saída têm `y ≤ agua.yMax + 1e-4` e ficam dentro de `agua`.
    - `QuadAcimaDaAgua_TodoRecortado`: depois de refletido, o quad fica fora de `agua` e a função retorna false.
    - `UVSegueOVertice`: o vértice refletido do canto UV (0,0) mantém UV (0,0) quando não é cortado.
- [ ] **Step 2: rodar** `Tools/rodar_testes.sh EditMode`. Expected: falha de compilação (`EhLago`, `Refletir`, `ReflexoGeometria` não existem).
- [ ] **Step 3: implementar** conforme o bloco Interfaces.
- [ ] **Step 4: rodar** `Tools/rodar_testes.sh EditMode`. Expected: `exit=0`, todos passam (os 133 antigos + os novos).
- [ ] **Step 5: commit** "Lago: gerador com ponte horizontal, reflexão e recorte com UV".

### Task 2: Lago no jogo — água, reflexo, modo aula

**Files:**
- Create: `Scripts/Jogo/Reflexo.cs`, `Tools/montar_cenas.sh`
- Modify:
  - `Scripts/Jogo/Terreno.cs`, `Scripts/Jogo/ModoJogo.cs`, `Scripts/Jogo/ConfigurarPartida.cs`;
  - `Scripts/Jogo/ModoAula.cs`, `Scripts/Jogo/TecnicasCG.cs`;
  - `Editor/MontagemComum.cs`, `Editor/MontarJogo.cs`, `Editor/MontarMenu.cs`;
  - `Tools/capturar.sh`, `Tests/PlayMode/CapturasTests.cs`.
- Test: `Tests/PlayMode/JogoTests.cs`

**Interfaces:**
- Consumes: Task 1.
- Produces:
  - `ParametrosModo.lagos` (Terra true, Lua false), repassado por `ConfigurarPartida` → `Terreno.lagos` → `Gerador.lagos`.
  - `Terreno.Chunk.agua` (`Rect?`). No lago:
    - o polígono do trecho é a ponte: a curva com fundo em `y0 − 0.4`;
    - a água vai de `x p0..p3` e de `y` da base da janela até `nivel = y0 − 1.0`;
    - a água é desenhada por um `SpriteRenderer` filho "Agua" (`quadrado.png`) com cor `(0.25, 0.5, 0.75, 0.85)`, ordem de desenho atrás do F1 e à frente do terreno, material lit, para escurecer à noite.
  - `Reflexo` (MonoBehaviour), com campos `Terreno terreno`, `SpriteRenderer[] pecas`, `Color tinta = (0.6, 0.8, 1, 0.45)`. Cria um `MeshRenderer` por peça (material unlit com a textura do sprite). Em cada `LateUpdate`, se o centro da peça está sobre um `Chunk.agua`, monta a malha:
    - pega os cantos de `sprite.vertices` (FullRect) em mundo e as UVs de `sprite.uv`;
    - chama `ReflexoGeometria.Quad`;
    - triangula em leque.
    - Fora do lago, a malha fica vazia. A cor é a da peça × tinta.
  - `ModoAula`: retângulo da água em azul claro e linha da água em ciano, por trecho de lago. O texto ganha a linha `"· Espelhamento: reflexo no lago (y' = 2h - y), recortado contra a água (azul)"`.
  - `TecnicasCG`: "Sutherland-Hodgman: reflexo no lago" e "reflexo no lago" passam para **no jogo**.
  - `Tools/montar_cenas.sh`: roda `-executeMethod MontarJogo.Executar` e `MontarMenu.Executar` (`-batchmode -quit`) e imprime `exit` e os `error CS`/`Exception`.
- [ ] **Step 1: testes PlayMode** em `JogoTests`:
  - `Lago_TemAguaEPonteHorizontal`: chama `terreno.Atualizar` até existir um trecho com `agua`; o collider é plano e a água fica abaixo da ponte.
  - `Reflexo_ApareceSoSobreOLago`: teleporta o chassi para o meio da ponte e espera 2 frames. A malha da pintura tem vértices, todos com `y ≤ agua.yMax`. No trecho 0 a malha fica vazia.
  - `Lua_SemLago`: em 30 trechos na Lua, nenhum tem `agua`.
- [ ] **Step 2: rodar** `Tools/rodar_testes.sh PlayMode`. Expected: falha de compilação (`agua`, `Reflexo`).
- [ ] **Step 3: implementar** e rodar `Tools/montar_cenas.sh`. Expected: dois `exit=0` e nenhum erro.
- [ ] **Step 4: rodar** `Tools/rodar_testes.sh PlayMode`. Expected: `exit=0`, `failed="0"`.
- [ ] **Step 5: captura** `jogo-lago.png` (chassi sobre a ponte, dia) e `jogo-lago-aula.png`, adicionadas ao filtro de `capturar.sh`. Rodar `timeout 1500 Tools/capturar.sh` e abrir as duas. Expected: ponte, água azul e o F1 refletido de cabeça para baixo dentro da água, sem nada acima da linha.
- [ ] **Step 6: commit** "Etapa 4: lago com ponte, água e reflexo recortado".

### Task 3: Intro — partes puras

**Files:**
- Modify: `Tools/gerar_f1.py` (escreve `CarroCG/Assets/Scripts/Jogo/ContornoF1.cs`)
- Create: `Scripts/Jogo/ModeloF1.cs`, `Scripts/Jogo/IntroDesenho.cs`
- Test: `Tests/Editor/ModeloF1Tests.cs`, `Tests/Editor/IntroDesenhoTests.cs`

**Interfaces:**
- Produces:
  - `ContornoF1` (gerado):
    - `public static readonly Vector2[][] Regioes` em pixels da arte (960×300, y para baixo), na ordem atrás → frente: placa traseira, asas traseiras, chassi, cobertura, cockpit, capacete (12 lados), asa dianteira (2), rodas (12 lados cada);
    - `public static readonly Color32[] Cores` (mesmo tamanho);
    - `public static Vector2 EmUnidades(Vector2 px)` → `((x − 480)/256, (150 − y)/256)`.
  - `ModeloF1`:
    - `static Vector3[] Vertices` e `static Vector2Int[] Arestas`, entre 50 e 90 vértices, todos derivados de `ContornoF1` em unidades:
      - chassi: o contorno `CHASSI` extrudado em z = ±0,35;
      - asa traseira e asa dianteira: caixas com z = ±0,45;
      - 4 rodas: anéis de 12 lados em z = ±0,45.
    - `static Matrix4x4 Rotacao(float yGraus, float xGraus)`.
  - `IntroDesenho` (buffer `Color32[]` w×h, janela em unidades → viewport em pixels por `Raster.JanelaViewport`):
    - `static void Wireframe(Color32[] px, int w, int h, Rect janela, Matrix4x4 rot, float kPerspectiva, float d, Color32 cor)`: projeta com `Projecao.Projetar(Projecao.Interpolar(Ortografica(), Perspectiva(d), k), rot·v)` e desenha as arestas com `Raster.Line`;
    - `static void Contorno(Color32[] px, int w, int h, Rect janela, int regioes, Color32 cor)`: Bresenham nas `regioes` primeiras regiões;
    - `static void Preencher(Color32[] px, int w, int h, Rect janela, int regioes)`: `Raster.ScanlineSpans` com `Cores`;
    - `static void Limpar(Color32[] px)`.
- [ ] **Step 1: testes**
  - `ModeloF1Tests`:
    - `TemEntre50E90Vertices`;
    - `ArestasValidas`: os índices estão em `[0, n)` e não há aresta degenerada;
    - `SimetricoEmZ`: para cada vértice existe o espelho com −z;
    - `RotacaoZeroEVistaLateral`: com `Rotacao(0,0)`, a projeção paralela de um vértice com z=+0,35 tem o mesmo xy do vértice com z=−0,35;
    - `NuncaAtrasDoObservador`: em qualquer rotação (passos de 15°), `z > −d` com `d = 4` (Review do etapa 3, minor 1).
  - `IntroDesenhoTests`:
    - `Wireframe_PintaPixels_NenhumForaDoBuffer` (sem exceção, alguns pixels com a cor);
    - `Perspectiva_DiferenteDaParalela` (`k = 0` e `k = 1` dão buffers diferentes com a rotação de 30°);
    - `Contorno_ZeroRegioes_NadaPintado`;
    - `Preencher_TodasAsRegioes_CentroDoChassiComCorDoChassi`;
    - `Preencher_Regiao0_NaoPintaRodas`.
- [ ] **Step 2: gerar** com `python3 -I Tools/gerar_f1.py CarroCG/Assets/Art`, que agora também escreve `ContornoF1.cs`, e rodar `Tools/rodar_testes.sh EditMode`. Expected: falha de compilação (`ModeloF1`, `IntroDesenho`).
- [ ] **Step 3: implementar.**
- [ ] **Step 4: rodar** `Tools/rodar_testes.sh EditMode`. Expected: `exit=0`, tudo passa.
- [ ] **Step 5: commit** "Intro: modelo 3D do F1, contorno gerado e desenho por Bresenham/scanline".

### Task 4: Intro no jogo

**Files:**
- Create: `Scripts/Jogo/IntroF1.cs`
- Modify:
  - `Scripts/Jogo/Preferencias.cs` (`Sessao.IntroVista`);
  - `Scripts/Jogo/Partida.cs`, `Scripts/Jogo/ModoAula.cs`, `Scripts/Jogo/TecnicasCG.cs`;
  - `Editor/MontarJogo.cs`;
  - os `SetUp` de `Tests/PlayMode/*.cs`, `Tests/PlayMode/CapturasTests.cs`, `Tools/capturar.sh`.
- Test: `Tests/PlayMode/IntroTests.cs`

**Interfaces:**
- Consumes: Task 3; `Partida.Congelar(bool)`; `Anim.Durante(dur, k => …, tempoReal: true)`.
- Produces:
  - `Sessao.IntroVista` (static bool).
  - `IntroF1` (MonoBehaviour) com os campos:
    - `Partida partida`, `F1 f1`, `CanvasGroup hud`, `SpriteRenderer tela`;
    - `Rect janela`, uma área de 6×2,6 unidades centrada no chassi, com textura de 480×208 pixels e filtro Point;
    - `float d = 4`.
  - Comportamento de `IntroF1`:
    - Se `Sessao.IntroVista` já está marcada, desliga a si mesmo.
    - Senão, marca `IntroVista`, congela a partida, esconde o HUD (`alpha` 0) e as camadas do F1, aproxima a câmera da janela e roda o roteiro em tempo real:
      1. **0–12 s**: gira em y (2 voltas, x = 15°), com k indo de 0 a 1 (paralela → perspectiva). Legenda "Projeção paralela -> perspectiva · rotação 3D".
      2. **12–18 s**: volta à vista lateral, com k de 1 a 0. Legenda "Vista lateral · volta para paralela".
      3. **18–26 s**: contorno região a região. Legenda "Rasterização: Bresenham".
      4. **26–32 s**: preenchimento região a região. Legenda "Preenchimento: scanline".
      5. **32–35 s**: crossfade da tela para as camadas HD e o HUD volta.
    - No fim (ou com **Espaço** a qualquer momento): mostra tudo, solta a câmera e descongela a partida.
    - `public bool Ativa`. `ModoAula` ignora Tab e F enquanto a intro está ativa.
  - `TecnicasCG`: "F1 3D em wireframe na intro", "Bresenham: wireframe e contorno do F1 na intro", "regiões do F1 na intro" e "wireframe 3D da intro" passam para **no jogo**.
- [ ] **Step 1: testes**
  - Em `IntroTests`:
    - `Intro_CongelaEMostraWireframe`: com `IntroVista = false`, carrega o Jogo. A partida fica `Congelado`, `intro.Ativa` é true e, depois de 1 s real, a textura da tela tem pixels não transparentes.
    - `PularNoInicio_JogoNormal`: chama `intro.Pular()` no primeiro frame. A partida volta a `Jogando`, o HUD fica com `alpha` 1 e as camadas do F1 com `alpha` 1.
    - `Intro_NaoRepeteNaSessao`: depois de pular, `partida.Reiniciar()`; na cena nova, `Ativa` é false e a partida está `Jogando`.
  - Nos `SetUp` existentes, `Sessao.IntroVista = true` (os testes de jogo começam jogando); o `TearDown` volta para true.
- [ ] **Step 2: rodar** `Tools/rodar_testes.sh PlayMode`. Expected: falha de compilação (`IntroF1`, `Sessao.IntroVista`).
- [ ] **Step 3: implementar** e rodar `Tools/montar_cenas.sh`. Expected: `exit=0` nas duas.
- [ ] **Step 4: rodar** `Tools/rodar_testes.sh PlayMode`. Expected: `exit=0`, `failed="0"`.
- [ ] **Step 5: capturas** `intro-wireframe.png` (6 s), `intro-contorno.png` (22 s) e `intro-preenchimento.png` (30 s). Rodar `capturar.sh` e abrir. Expected: wireframe em perspectiva legível, contorno do F1 e regiões coloridas alinhadas com o sprite HD.
- [ ] **Step 6: commit** "Etapa 5: intro com F1 3D em wireframe, Bresenham, scanline e crossfade".

### Task 5: Som

**Files:**
- Create: `Scripts/Jogo/SomSintetizado.cs`, `Scripts/Jogo/Som.cs`
- Modify:
  - `Scripts/Jogo/Preferencias.cs` (`Volume` 0..10, padrão 7, incluído em `Chaves`);
  - `Scripts/Jogo/Menu.cs` (botão "Volume: N" em Configurações, que cicla 0→10 de 1 em 1);
  - `Scripts/Jogo/Partida.cs` (evento `Terminou`), `Editor/MontarJogo.cs`, `Editor/MontarMenu.cs`.
- Test: `Tests/Editor/SomSintetizadoTests.cs`, `Tests/PlayMode/SomTests.cs`

**Interfaces:**
- Produces:
  - `SomSintetizado.Motor(int taxa)`: 1 s em loop, base 55 Hz com harmônicos e um leve ruído; a primeira e a última amostra quase iguais, para emendar sem estalo.
  - `SomSintetizado.Moeda(int taxa)`: dois bipes de 0,12 s subindo (988 → 1319 Hz).
  - `SomSintetizado.Galao(int taxa)`: varredura de 300 → 900 Hz em 0,3 s.
  - `SomSintetizado.Batida(int taxa)`: ruído com envelope de 0,25 s.
  - `SomSintetizado.Fim(int taxa)`: três notas descendo, 0,9 s.
  - Todos devolvem `float[]` em `[−1, 1]`.
  - `float SomSintetizado.PitchMotor(float grausPorSegundoRoda)` → `0.6 + 1.4·clamp01(|w| / 2400)`.
  - `Som` (MonoBehaviour), com campos `F1 f1`, `Partida partida`:
    - cria os `AudioClip`s com `AudioClip.Create`;
    - mantém o motor em loop com pitch pela `angularVelocity` da roda traseira e volume 0 quando o motor está desligado ou o jogo está pausado;
    - toca moeda e galão por `Coletavel.Coletado`, a batida por `CameraSegue.Impacto` (Task 6) e o fim por `Partida.Terminou`;
    - `AudioListener.volume = Preferencias.Volume / 10f`.
- [ ] **Step 1: testes**
  - `SomSintetizadoTests`:
    - `Sons_DentroDeMenosUmAUm_ENaoVazios`;
    - `Motor_EmendaSemEstalo`: `|a[0] − a[^1]| < 0.05`;
    - `Pitch_ParadoEMaximo`: 0 → 0,6, 2400 → 2,0, −2400 → 2,0;
    - `Moeda_Duracao`: 0,24 s ± 1 amostra.
  - `SomTests` (PlayMode):
    - `Motor_PitchSobeAcelerando`;
    - `VolumeZero_Mudo`: com `Preferencias.Volume = 0` e a cena recarregada, `AudioListener.volume == 0`, sem erro;
    - `Pausa_MotorSilencia`.
  - Em `MenuTests`: `Configuracoes_VolumeCicla`.
- [ ] **Step 2: rodar** EditMode e PlayMode. Expected: falha de compilação.
- [ ] **Step 3: implementar** e remontar as cenas.
- [ ] **Step 4: rodar** EditMode e PlayMode. Expected: `exit=0`, `failed="0"`.
- [ ] **Step 5: commit** "Som sintetizado: motor pela roda, moeda, galão, batida, fim e volume no menu".

### Task 6: Efeitos — partículas, tremor, pós-processamento

**Files:**
- Create: `Scripts/Jogo/Efeitos.cs`
- Modify:
  - `Scripts/Jogo/CameraSegue.cs` (tremor e evento `Impacto`);
  - `Scripts/Jogo/CicloDia.cs` (expõe `Tempo`), `Scripts/Jogo/Reflexo.cs` (respingo);
  - `Editor/MontagemComum.cs`, `Editor/MontarJogo.cs`, `Editor/MontarMenu.cs`;
  - `Tests/PlayMode/CapturasTests.cs`.
- Test: `Tests/PlayMode/JogoTests.cs`

**Interfaces:**
- Consumes: `F1.NoChao`, `F1.acelerador`, `Coletavel.Coletado`, `Terreno.Chunk.agua`.
- Produces:
  - `CameraSegue`:
    - `float tremor` (decai em 0,3 s) e `event Action<float> Impacto`;
    - um impacto acontece na aterrissagem, quando a velocidade vertical do carro estava abaixo de **−6** no frame anterior e o carro passa a tocar o chão;
    - `public void Tremer(float forca)`.
  - `Efeitos` (MonoBehaviour), com campos `F1 f1`, `CameraSegue cam`, `CicloDia ciclo`, `Volume volume`. Usa 4 `ParticleSystem` criados pelo `MontagemComum` (material de partícula unlit):
    - **poeira** na roda traseira (emite enquanto acelera no chão; cor da terra; na Lua, cinza);
    - **faíscas** no `Impacto` (burst de 20);
    - **brilho** na moeda coletada (burst amarelo na posição);
    - **respingo** quando o carro entra numa área de água (burst azul).
  - Pós-processamento: um `Volume` global com `Bloom` (intensidade 0,6, threshold 1,0), `Vignette` (0,25) e `ColorAdjustments`. Os `ColorAdjustments` seguem a hora do dia: noite com saturação −20 e exposição −0,3; dia neutro. A câmera tem `renderPostProcessing = true`.
- [ ] **Step 1: testes PlayMode** em `JogoTests`:
  - `Poeira_EmiteAcelerandoNoChao`;
  - `Aterrissagem_Forte_TremeEFaisca`: solta o carro de 6 m; a câmera desloca e as faíscas têm partículas;
  - `Moeda_Brilho`: força `Coletavel.Coletado` via a coleta de uma moeda no caminho;
  - `PosProcessamento_TemBloomVinheta`.
- [ ] **Step 2: rodar** `Tools/rodar_testes.sh PlayMode`. Expected: falha de compilação.
- [ ] **Step 3: implementar** e remontar as cenas.
- [ ] **Step 4: rodar** `Tools/rodar_testes.sh PlayMode`. Expected: `exit=0`, `failed="0"`.
- [ ] **Step 5: capturas** de todas as cenas (`capturar.sh`), abrindo dia, noite, lago e menu. Expected: bloom leve no sol e nos faróis, vinheta discreta e nada estourado.
- [ ] **Step 6: commit** "Etapa 6: partículas, tremor na aterrissagem e pós-processamento por hora do dia".

### Task 7: Visual da interface (pedido do usuário: "pode mudar font, elementos, tudo, tamanhos", "usar elementos visuais")

**Files:**
- Modify:
  - `Tools/gerar_f1.py`: gera `Art/UI/` com:
    - `painel.png`: retângulo arredondado de 64×64, raio 16, borda clara de 3 px, para 9-slice;
    - `botao.png`: retângulo arredondado de 64×64, raio 20;
    - ícones de 128 px: `icone_moeda` (reusa a moeda), `icone_galao`, `icone_bandeira` (quadriculada), `icone_trofeu`, `icone_pausa`;
    - `logo.png`: "SENNINHAS DRIVING" em itálico com faixa quadriculada e o F1.
  - `Editor/MontagemComum.cs`:
    - copia `Ubuntu-B.ttf` e `Ubuntu-M.ttf` (licença Ubuntu Font Licence, cópia em `Art/UI/`) e cria `TMP_FontAsset` dinâmicos (`Assets/Art/UI/Ubuntu-B SDF.asset`), usados como fonte padrão de todos os textos;
    - `Caixa` e `Botao` passam a usar os sprites 9-slice (`Image.Type.Sliced`), com cores de hover e seleção;
    - HUD novo:
      - combustível: ícone do galão + barra com gradiente verde→amarelo→vermelho pela fração, 360×34;
      - moedas: ícone da moeda + número grande (44);
      - distância: bandeira + número em itálico grande (64), com o recorde e o troféu embaixo (28);
      - todos sobre painéis arredondados semitransparentes.
  - `Editor/MontarJogo.cs`, `Editor/MontarMenu.cs`:
    - o menu usa o `logo.png` no lugar do título em texto;
    - os botões ficam maiores (420×72, fonte 36) e têm ícones à esquerda (bandeira em Jogar, troféu em Recorde);
    - os painéis de pausa e fim usam `painel.png`; o fim mostra os ícones com os números.
  - `Scripts/Jogo/Partida.cs`: a barra de combustível ganha cor pelo gradiente (`Color BarraCor(float fracao)`, pura e estática).
- Test: `Tests/Editor/` (`PartidaHudTests.BarraCor_VerdeCheioVermelhoVazio`), `Tests/PlayMode/MenuTests.cs` e `JogoTests.cs` continuam passando (eles acham os textos e botões pelos campos, não pela aparência).

- [ ] **Step 1: teste** `BarraCor_VerdeCheioVermelhoVazio`: 1 → verde (`g > r`), 0,5 → amarelo (`r > 0.8`, `g > 0.6`), 0 → vermelho (`r > g`). Rodar EditMode. Expected: falha de compilação.
- [ ] **Step 2: gerar a arte de UI**, implementar e remontar as cenas. Expected: `exit=0`.
- [ ] **Step 3: rodar** EditMode e PlayMode. Expected: `exit=0`, `failed="0"`.
- [ ] **Step 4: capturas** de todas as cenas, abrindo o menu, a garagem, o jogo e o fim. Expected: fonte Ubuntu, ícones visíveis, nada cortado ou sobreposto em 16:9.
- [ ] **Step 5: commit** "Interface nova: fonte Ubuntu, ícones, painéis arredondados e HUD maior".

### Task 8: Guia e fechamento

**Files:**
- Modify: `Guia para apresentação.pdf` (regerado pelo script do scratchpad, com o lago, a intro, o som e as teclas atualizadas). Atualizar a tabela de técnicas da spec se a implementação divergiu.

- [ ] **Step 1:** `TecnicasCGTests`: nenhum lugar "em breve" sobrou (`Lista.All(t => t.lugares.All(l => l.noJogo))`). Rodar EditMode. Expected: `exit=0`.
- [ ] **Step 2:** regerar o guia em uma página e conferir o PDF (abrir como imagem).
- [ ] **Step 3: commit** "Guia atualizado: lago, intro e efeitos; todas as técnicas no jogo".
