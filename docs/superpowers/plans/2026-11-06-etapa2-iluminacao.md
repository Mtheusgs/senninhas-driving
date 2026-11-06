# Senninhas Driving — Etapa 2: Iluminação e Sombreamento — Plano de Implementação

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Ciclo dia/noite (~3 min) com sol e lua definindo a luz; terreno sombreado por Lambert em três modos (Flat/Gouraud/Phong, tecla L); F1 com shader Phong próprio e normal map; sombra projetada do F1 preenchida por scanline; Lua com luz fixa e sombra dura; modo aula mostrando normais e o vetor do sol.

**Architecture:** O cálculo de luz é nosso e puro: `Sombreamento` (Lambert, Flat/Gouraud/Phong, Phong especular), `Ceu` (estado do céu por hora do dia) e `Sombra` (projeção de ponto na superfície) — testados em EditMode. O terreno guarda, por trecho, uma textura 1×w de **luz por coluna** calculada em C#; um shader mínimo (`CarroCG/TerrenoLuz`) só multiplica a cor pela luz. O F1 usa `CarroCG/F1Phong` (HLSL próprio) com normal map gerado pelo `gerar_f1.py` e a direção/cor do sol em variáveis globais definidas pelo `CicloDia`. A sombra é um polígono (faixa na superfície) preenchido com `Raster.ScanlineSpans` numa textura pequena a cada frame.

**Tech Stack:** Unity 6000.3.25f1, URP 17.3 2D (shaders HLSL com `LightMode = Universal2D`), Light2D, Python 3 + Pillow 10.2.

**Spec:** `docs/superpowers/specs/2026-09-30-f1-hillclimb-design.md` (seções "Iluminação e sombreamento", "Sombra projetada", "Visual" e "Ordem de entrega", etapa 2), com `docs/superpowers/specs/2026-11-04-menu-modo-aula-design.md` (Lua sem ciclo: luz fixa, sombras duras; modo aula cresce por etapa).

## Global Constraints

- Unity 6000.3.25f1, URP 2D; testes em batchmode com o Editor fechado; **nunca rodar duas Unity no mesmo projeto ao mesmo tempo** (a segunda falha e a primeira pode travar).
- Algoritmos de luz em funções puras testadas; shaders só aplicam o resultado (terreno) ou espelham a fórmula testada (`Sombreamento.Phong` ↔ `F1Phong`).
- Ciclo ~3 min: dia → pôr do sol → noite → amanhecer; a luz sempre vem de cima (y ≥ 0,15) e muda de forma contínua (sem saltos, inclusive na troca sol↔lua).
- Lua: sem ciclo; sol fixo a ~35° de elevação; ambiente baixo (sombra dura); céu continua preto com estrelas e a Terra.
- Tecla L troca Flat → Gouraud → Phong → Flat (padrão: Phong) e mostra o modo por ~1,5 s no HUD.
- Textos só Latin-1 (`-`, `·`; nunca `–`, `−`, `•`).
- Cenas remontadas por script (`MontarJogo`, `MontarMenu`); o menu também recebe o `CicloDia` (fixo de dia) para o F1 do fundo ter luz.
- Testes não gravam nas chaves reais de `PlayerPrefs` (padrão já usado: `Preferencias.Prefixo = "teste."`).

## Review Focus

1. **Troca sol ↔ lua no horizonte** — a direção da luz não pode saltar (o terreno "piscaria" no pôr do sol). Teste: `CeuTests.DirecaoDaLuz_Continua` (Task 1).
2. **Sol rente ao horizonte** — raio de sombra quase horizontal não acha o chão; não pode travar nem gerar sombra infinita. Teste: `SombraTests.SolNoHorizonte_SemSombra` (Task 2) e `SombraTests.Faixa_LimitadaAoMaximo` (Task 2).
3. **Shader sem a variável global definida** (EditMode, cena sem `CicloDia`) — o F1 não pode ficar preto. Teste: `JogoTests.F1Phong_SemCicloNaoFicaPreto` (Task 6) — o shader usa luz padrão quando `_DirSol.w == 0`.
4. **Rodas girando com normal map** — o brilho não pode girar junto com a roda como se estivesse pintado. Teste: `JogoTests.PhongF1_PassaARotacaoDeCadaPeca` (Task 6).
5. **Sessão longa** — textura de luz de cada trecho liberada com o trecho. Teste: `TerrenoComponenteTests.Atualizar_DescartaTrechosAntigos` passa a checar também `luz` (Task 3).

## Estrutura de arquivos

```
Tools/gerar_f1.py                 + f1_normal.png, f1_roda_normal.png, Ceu/sol.png, Ceu/lua.png, Ceu/ceu_gradiente.png, F1/feixe.png
CarroCG/Assets/
├── Shaders/TerrenoLuz.shader     cor × luz por coluna
├── Shaders/F1Phong.shader        Phong com normal map + rotação por peça
├── Scripts/Jogo/
│   ├── Sombreamento.cs           ModoSombreamento, Lambert, Flat/Gouraud/Phong, Phong especular, luz por coluna (puro)
│   ├── Ceu.cs                    EstadoCeu, Ceu.Estado(t) (puro)
│   ├── Sombra.cs                 Projetar, Faixa (puro)
│   ├── CicloDia.cs               tempo do dia, globais do shader, céu/sol/lua/estrelas, luz global, faróis, ilumina o terreno
│   ├── SombraF1.cs               sombra do F1 (faixa preenchida por scanline)
│   ├── PhongF1.cs                rotação de cada peça para o shader
│   ├── Terreno.cs                + luz por trecho, Iluminar, modoSombreamento, AlturaEm
│   ├── Partida.cs                + tecla L e aviso do modo
│   ├── ModoAula.cs               + normais e vetor do sol
│   ├── ConfigurarPartida.cs      + ciclo fixo/lunar
│   └── TecnicasCG.cs             selos da iluminação e da sombra
├── Editor/MontagemComum.cs       + materiais, céu, sol/lua, faróis, sombra
├── Tests/Editor/                 + SombreamentoTests, CeuTests, SombraTests
└── Tests/PlayMode/               JogoTests (+), CapturasTests (+ Luz)
```

---

### Task 1: Sombreamento e céu (puros)

**Files:**
- Create: `CarroCG/Assets/Scripts/Jogo/Sombreamento.cs`, `CarroCG/Assets/Scripts/Jogo/Ceu.cs`
- Test: `CarroCG/Assets/Tests/Editor/SombreamentoTests.cs`, `CarroCG/Assets/Tests/Editor/CeuTests.cs`

**Interfaces:**
- Produces:
  - `enum ModoSombreamento { Flat, Gouraud, Phong }`
  - `static class Sombreamento`:
    - `float Lambert(Vector2 n, Vector2 l, float ambiente, float difusa)` = `ambiente + difusa·max(0, n̂·l̂)`
    - `float Phong(Vector3 n, Vector3 l, Vector3 v, float ambiente, float difusa, float especular, float brilho)` = `ambiente + difusa·max(0,N·L) + especular·max(0,R·V)^brilho`, `R = 2(N·L)N − L` (vetores normalizados)
    - `Vector2[] NormaisDosSegmentos(Vector2[] facetas)` — normal à esquerda de cada segmento (`(−d.y, d.x)` normalizado)
    - `Vector2[] NormaisDosVertices(Vector2[] facetas)` — extremos = normal do segmento; internos = média normalizada dos dois vizinhos
    - `float DifusaEm(ModoSombreamento modo, Vector2[] facetas, float x, Vector2 l)` — fator `max(0, N·L)` em x: Flat = normal do segmento; Gouraud = interpola linearmente os fatores dos vértices; Phong = interpola as normais dos vértices, normaliza e calcula
    - `Color32[] LuzPorColuna(ModoSombreamento modo, Vector2[] facetas, Rect janela, float ppu, int w, Vector2 l, Color corSol, Color corAmbiente, float difusa = 0.6f)` — coluna x (centro em `janela.xMin + (x+0,5)/ppu`) = `corAmbiente + corSol·difusa·DifusaEm(...)`, clampado
  - `struct EstadoCeu { Vector2 dirSol, dirLuz; Color corLuz, corAmbiente, corCeuTopo, corCeuHorizonte; float noite; }`
  - `static EstadoCeu Ceu.Estado(float t)` — `t ∈ [0,1)`: 0 nascer do sol (sol à direita, no horizonte), 0,25 meio-dia, 0,5 pôr do sol, 0,75 meia-noite.

- [ ] **Step 1: testes**

`SombreamentoTests.cs`:
```csharp
using NUnit.Framework;
using UnityEngine;

public class SombreamentoTests
{
    static readonly Vector2 Cima = Vector2.up;

    [Test] public void Lambert_NormalParalelaALuz_Maximo() => Assert.AreEqual(1.0f, Sombreamento.Lambert(Cima, Cima, 0.3f, 0.7f), 1e-5);
    [Test] public void Lambert_Perpendicular_SoAmbiente() => Assert.AreEqual(0.3f, Sombreamento.Lambert(Cima, Vector2.right, 0.3f, 0.7f), 1e-5);
    [Test] public void Lambert_Oposta_SoAmbiente() => Assert.AreEqual(0.3f, Sombreamento.Lambert(Cima, Vector2.down, 0.3f, 0.7f), 1e-5);

    [Test]
    public void Phong_EspecularMaximoComReflexoAlinhadoAVisao()
    {
        var n = Vector3.forward; var v = Vector3.forward;
        float alinhado = Sombreamento.Phong(n, Vector3.forward, v, 0, 0, 1, 16);
        float inclinado = Sombreamento.Phong(n, new Vector3(1, 0, 1), v, 0, 0, 1, 16);
        Assert.AreEqual(1f, alinhado, 1e-4);
        Assert.Less(inclinado, 0.1f);
    }

    [Test]
    public void Phong_LuzPorTras_SemDifusaNemEspecular() =>
        Assert.AreEqual(0.2f, Sombreamento.Phong(Vector3.forward, Vector3.back, Vector3.forward, 0.2f, 1, 1, 16), 1e-5);

    // telhado: sobe de (0,0) a (1,1) e desce até (2,0)
    static readonly Vector2[] Telhado = { new(0, 0), new(1, 1), new(2, 0) };
    static readonly Vector2 Diagonal = new Vector2(-1, 1).normalized;     // de frente para a 1ª água

    [Test]
    public void NormaisDosVertices_MediaNoMeio()
    {
        var nv = Sombreamento.NormaisDosVertices(Telhado);
        Assert.AreEqual(3, nv.Length);
        Assert.AreEqual(0f, nv[1].x, 1e-5);
        Assert.AreEqual(1f, nv[1].y, 1e-5);
    }

    [Test]
    public void Flat_ConstanteNoSegmento() =>
        Assert.AreEqual(Sombreamento.DifusaEm(ModoSombreamento.Flat, Telhado, 0.2f, Diagonal),
                        Sombreamento.DifusaEm(ModoSombreamento.Flat, Telhado, 0.8f, Diagonal), 1e-5);

    [Test]
    public void Gouraud_MeioEhMediaDosExtremos()
    {
        float a = Sombreamento.DifusaEm(ModoSombreamento.Gouraud, Telhado, 0f, Diagonal);
        float b = Sombreamento.DifusaEm(ModoSombreamento.Gouraud, Telhado, 1f, Diagonal);
        Assert.AreEqual((a + b) / 2, Sombreamento.DifusaEm(ModoSombreamento.Gouraud, Telhado, 0.5f, Diagonal), 1e-5);
    }

    [Test]
    public void Phong_NormalInterpoladaDiferenteDeGouraud()
    {
        var nv = Sombreamento.NormaisDosVertices(Telhado);
        float esperado = Mathf.Max(0, Vector2.Dot(Vector2.Lerp(nv[0], nv[1], 0.5f).normalized, Diagonal));
        float phong = Sombreamento.DifusaEm(ModoSombreamento.Phong, Telhado, 0.5f, Diagonal);
        Assert.AreEqual(esperado, phong, 1e-5);
        Assert.AreNotEqual(Sombreamento.DifusaEm(ModoSombreamento.Gouraud, Telhado, 0.5f, Diagonal), phong);
    }

    [Test]
    public void LuzPorColuna_UmaCorPorColunaComAmbienteNaSombra()
    {
        var luz = Sombreamento.LuzPorColuna(ModoSombreamento.Flat, Telhado, new Rect(0, -1, 2, 3), 10, 20,
                                            Diagonal, Color.white, new Color(0.2f, 0.2f, 0.2f), 0.6f);
        Assert.AreEqual(20, luz.Length);
        Assert.Greater(luz[5].r, luz[15].r, "água virada para a luz deveria ser mais clara");
        Assert.AreEqual((Color32)new Color(0.2f, 0.2f, 0.2f), luz[15], "água de costas: só ambiente");
    }
}
```

`CeuTests.cs`:
```csharp
using NUnit.Framework;
using UnityEngine;

public class CeuTests
{
    [Test]
    public void MeioDia_SolEmCimaECeuAzul()
    {
        var e = Ceu.Estado(0.25f);
        Assert.Greater(e.dirLuz.y, 0.95f);
        Assert.Less(e.noite, 0.05f);
        Assert.Greater(e.corCeuTopo.b, e.corCeuTopo.r);
    }

    [Test]
    public void MeiaNoite_LuzDaLuaFraca()
    {
        var e = Ceu.Estado(0.75f);
        Assert.Greater(e.noite, 0.95f);
        Assert.Less(e.corLuz.maxColorComponent, 0.6f);
        Assert.Greater(e.dirLuz.y, 0.9f, "luz da lua vem de cima à meia-noite");
    }

    [Test]
    public void PorDoSol_HorizonteAlaranjado()
    {
        var e = Ceu.Estado(0.49f);
        Assert.Greater(e.corCeuHorizonte.r, e.corCeuHorizonte.b);
    }

    [Test]
    public void LuzSempreDeCima()
    {
        for (float t = 0; t < 1; t += 0.005f) Assert.GreaterOrEqual(Ceu.Estado(t).dirLuz.y, 0.15f - 1e-4f, $"t={t}");
    }

    [Test]
    public void DirecaoDaLuz_Continua()
    {
        for (float t = 0; t < 1; t += 0.001f)
        {
            float salto = Vector2.Angle(Ceu.Estado(t).dirLuz, Ceu.Estado(t + 0.001f).dirLuz);
            Assert.Less(salto, 3f, $"salto de {salto:0.0}° em t={t}");
        }
    }
}
```

- [ ] **Step 2: ver falhar** — `Tools/rodar_testes.sh EditMode` → `exit=1`, `'Sombreamento' does not exist`.

- [ ] **Step 3: implementar** `Sombreamento.cs` (assinaturas do Interfaces) e `Ceu.cs`. Fórmulas do céu (decisão do plano):
```csharp
// sol = (cos 2πt, sin 2πt); dia = clamp01(sol.y·1,5 + 0,5); horizonte = 1 − clamp01(|sol.y|·3)
// dirSolDeCima = normalize(sol.x, max(sol.y, 0.5)); dirLua = normalize(−sol.x, max(−sol.y, 0.5))
// dirLuz = normalize(lerp(dirLua, dirSolDeCima, dia))          // contínua: transição larga, luz nunca rente ao chão
// corLuz = lerp((0.32,0.38,0.55), lerp(branco, (1,0.6,0.3), horizonte), dia)
// corAmbiente = lerp((0.12,0.14,0.25), (0.42,0.44,0.5), dia)
// corCeuTopo = lerp((0.02,0.03,0.08), (0.3,0.55,0.95), dia)
// corCeuHorizonte = lerp(lerp((0.06,0.08,0.18), (0.98,0.55,0.3), horizonte·0.8), lerp((0.62,0.82,1), (1,0.6,0.35), horizonte), dia)
// noite = 1 − dia
```

- [ ] **Step 4: ver passar** — `Tools/rodar_testes.sh EditMode` → `exit=0`, todos passam (89 + 10 + 5 = 104).

- [ ] **Step 5: commit** — `git commit -m "Sombreamento (Lambert, Flat/Gouraud/Phong, Phong especular) e estado do céu"`

---

### Task 2: Sombra projetada (pura) e altura do terreno

**Files:**
- Create: `CarroCG/Assets/Scripts/Jogo/Sombra.cs`
- Modify: `CarroCG/Assets/Scripts/Jogo/Terreno.cs`
- Test: `CarroCG/Assets/Tests/Editor/SombraTests.cs`, `CarroCG/Assets/Tests/Editor/TerrenoComponenteTests.cs`

**Interfaces:**
- Produces:
  - `static Vector2? Sombra.Projetar(Vector2 p, Vector2 dirLuz, System.Func<float, float> altura, float alcance = 40f)` — anda de `p` na direção `−dirLuz` até cruzar a superfície (`y ≤ altura(x)`), refina por bisseção (erro < 0,01); `null` se não cruzar dentro do alcance ou se `dirLuz.y < 0,05`; `altura` pode devolver `NaN` (fora do terreno) → trata como sem chão.
  - `static (float xMin, float xMax)? Sombra.Faixa(IEnumerable<Vector2> pontos, Vector2 dirLuz, System.Func<float, float> altura, float comprimentoMax = 12f)` — extensão em x dos pontos projetados; `null` se nenhum projetar; largura limitada a `comprimentoMax` (corta do lado mais longe do carro).
  - `float Terreno.AlturaEm(float x)` — altura da superfície pelo trecho que contém x (`TerrenoChunk.AlturaEm` da curva); `NaN` fora dos trechos existentes.

- [ ] **Step 1: testes**

`SombraTests.cs`:
```csharp
using NUnit.Framework;
using UnityEngine;

public class SombraTests
{
    static float Plano(float x) => 0;

    [Test]
    public void Projetar_CaiNaSuperficieDoLadoOpostoAoSol()
    {
        var p = Sombra.Projetar(new Vector2(0, 2), new Vector2(1, 1), Plano).Value;
        Assert.AreEqual(-2f, p.x, 0.02f);
        Assert.AreEqual(0f, p.y, 0.02f);
    }

    [Test]
    public void Projetar_SolBaixo_SombraMaisLonge()
    {
        float alto = Sombra.Projetar(new Vector2(0, 2), new Vector2(1, 1), Plano).Value.x;
        float baixo = Sombra.Projetar(new Vector2(0, 2), new Vector2(1, 0.3f), Plano).Value.x;
        Assert.Less(baixo, alto);
        Assert.AreEqual(-2f / 0.3f, baixo, 0.05f);
    }

    [Test]
    public void SolNoHorizonte_SemSombra() => Assert.IsNull(Sombra.Projetar(new Vector2(0, 2), new Vector2(1, 0), Plano));

    [Test]
    public void ForaDoTerreno_SemSombra() => Assert.IsNull(Sombra.Projetar(new Vector2(0, 2), Vector2.up, x => float.NaN));

    [Test]
    public void Faixa_ExtensaoDosPontosProjetados()
    {
        var f = Sombra.Faixa(new[] { new Vector2(0, 1), new Vector2(1, 1) }, Vector2.up, Plano).Value;
        Assert.AreEqual(0f, f.xMin, 0.02f);
        Assert.AreEqual(1f, f.xMax, 0.02f);
    }

    [Test]
    public void Faixa_LimitadaAoMaximo()
    {
        // sol baixo: (0,1) projeta em x≈−12,5 e (0,2,5) em x≈−31 → faixa de ~19 cortada em 12
        var f = Sombra.Faixa(new[] { new Vector2(0, 1), new Vector2(0, 2.5f) }, new Vector2(1, 0.08f), Plano, 12f).Value;
        Assert.LessOrEqual(f.xMax - f.xMin, 12f + 1e-3f);
    }
}
```

Em `TerrenoComponenteTests.cs`:
```csharp
    [Test]
    public void AlturaEm_UsaOTrechoCertoENaNFora()
    {
        terreno.Atualizar(0);
        Assert.AreEqual(0f, terreno.AlturaEm(5), 1e-4, "1º trecho é plano");
        Assert.IsNaN(terreno.AlturaEm(-50));
    }
```

- [ ] **Step 2: ver falhar** — `exit=1`, `'Sombra' does not exist` / `'Terreno' does not contain 'AlturaEm'`.
- [ ] **Step 3: implementar** `Sombra.cs` e `Terreno.AlturaEm` (assinaturas acima; passo da marcha 0,1).
- [ ] **Step 4: ver passar** — `exit=0` (104 + 7 = 111).
- [ ] **Step 5: commit** — `git commit -m "Sombra projetada (pura) e altura do terreno"`

---

### Task 3: Terreno iluminado por coluna + shader TerrenoLuz

**Files:**
- Create: `CarroCG/Assets/Shaders/TerrenoLuz.shader`
- Modify: `CarroCG/Assets/Scripts/Jogo/Terreno.cs`
- Test: `CarroCG/Assets/Tests/Editor/TerrenoComponenteTests.cs`

**Interfaces:**
- Consumes: `Sombreamento.LuzPorColuna`, `ModoSombreamento` (Task 1).
- Produces:
  - `Terreno`: campos `Material materialLuz` (pode ser null), `int amostrasLuz = 12` (facetas por trecho, de propósito poucas para o Flat aparecer), `ModoSombreamento modoSombreamento = Phong`; `Chunk` ganha `Vector2[] facetas` (`TerrenoGerador.Amostrar(t, amostrasLuz + 1)`) e `Texture2D luz` (w×1, RGBA32, `FilterMode.Point`, `Clamp`); `void Iluminar(Vector2 dirLuz, Color corSol, Color corAmbiente)` (guarda e reaplica em todos os trechos); `void ProximoSombreamento()` (Flat→Gouraud→Phong→Flat e reaplica). Trecho novo nasce com a luz atual (padrão antes de `Iluminar`: luz de cima, branca, ambiente 0,42).
  - Aplicação: `SpriteRenderer.sharedMaterial = materialLuz` (se houver) e `_Luz` via `MaterialPropertyBlock`. `Liberar` destrói também `luz`.
  - Shader `CarroCG/TerrenoLuz`: `_MainTex` (PerRendererData) × `_Luz` amostrada em `(uv.x, 0.5)`; transparente, `Cull Off`, `ZWrite Off`, uma pass `Tags { "LightMode" = "Universal2D" }`, `#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"`.

- [ ] **Step 1: testes** (em `TerrenoComponenteTests.cs`)
```csharp
    [Test]
    public void Trecho_TemLuzPorColuna()
    {
        var c = terreno.GerarProximo();
        Assert.IsNotNull(c.luz);
        Assert.AreEqual(c.tex.width, c.luz.width);
        Assert.AreEqual(1, c.luz.height);
        Assert.AreEqual(terreno.amostrasLuz + 1, c.facetas.Length);
    }

    [Test]
    public void Iluminar_MudaALuzDosTrechos()
    {
        terreno.GerarProximo(); terreno.GerarProximo();
        var c = terreno.GerarProximo();                          // trecho com morro
        var antes = c.luz.GetPixels32();
        terreno.Iluminar(new Vector2(1, 0.2f).normalized, Color.white, Color.black);
        CollectionAssert.AreNotEqual(antes, c.luz.GetPixels32());
    }

    [Test]
    public void ProximoSombreamento_CiclaOsTresModos()
    {
        Assert.AreEqual(ModoSombreamento.Phong, terreno.modoSombreamento);
        terreno.ProximoSombreamento();
        Assert.AreEqual(ModoSombreamento.Flat, terreno.modoSombreamento);
        terreno.ProximoSombreamento();
        Assert.AreEqual(ModoSombreamento.Gouraud, terreno.modoSombreamento);
        terreno.ProximoSombreamento();
        Assert.AreEqual(ModoSombreamento.Phong, terreno.modoSombreamento);
    }
```
E em `Atualizar_DescartaTrechosAntigos`, antes do laço, guardar `var primeira = terreno.GerarProximo().luz;` e no fim `Assert.IsTrue(primeira == null, "textura de luz do trecho descartado não foi liberada");` (objeto Unity destruído compara igual a null).

- [ ] **Step 2: ver falhar** — `exit=1`, `'Terreno.Chunk' does not contain 'luz'`.
- [ ] **Step 3: implementar** campos/métodos acima em `Terreno.cs` e o shader `TerrenoLuz.shader`.
- [ ] **Step 4: ver passar** — `exit=0` (111 + 3 = 114).
- [ ] **Step 5: commit** — `git commit -m "Terreno: luz por coluna (Lambert Flat/Gouraud/Phong) aplicada por shader"`

---

### Task 4: Arte (normal maps, sol, lua, céu, feixe) e shader Phong do F1

**Files:**
- Modify: `Tools/gerar_f1.py`
- Create: `CarroCG/Assets/Art/F1/f1_normal.png`, `f1_roda_normal.png`, `feixe.png`; `CarroCG/Assets/Art/Ceu/sol.png`, `lua.png`, `ceu_gradiente.png`
- Create: `CarroCG/Assets/Shaders/F1Phong.shader`, `CarroCG/Assets/Scripts/Jogo/PhongF1.cs`

**Interfaces:**
- Produces:
  - `gerar_f1.py`: `mapa_normal(img, raio, forca)` — altura = alfa desfocado (Gaussian, `raio`), normal = `normalize(−∂h/∂x, +∂h/∂y_imagem, 1)·forca` codificada `n·0,5+0,5` em RGB (y da imagem para baixo → sinal já convertido para UV). `f1_normal.png` 960×300 a partir de `f1_corpo` (raio 14, força 3); `f1_roda_normal.png` 184×184 a partir de `f1_roda` (raio 10, força 3). `sol.png` 128 (disco amarelo com halo), `lua.png` 128 (disco cinza-claro com crateras), `ceu_gradiente.png` 4×256 (branco, alfa 0 embaixo → 255 em cima), `feixe.png` 256×96 (cone branco com alfa decrescente para a frente).
  - Shader `CarroCG/F1Phong`: propriedades `[PerRendererData] _MainTex`, `_NormalMap` (linear), `_Especular` (0–1), `_Brilho` (1–128), `[HideInInspector] _Rotacao` (vetor, padrão (1,0,0,0)). Globais: `float4 _DirSol` (xyz direção, w=1 quando definido), `half4 _CorSol`, `half4 _CorAmbiente`. Fragment: `N` = normal do mapa girado no plano por `_Rotacao` (eixoX = (c,s), eixoY = (−s,c)); `L = normalize(_DirSol.xyz)` ou `normalize(0.4, 0.8, 0.6)` se `_DirSol.w == 0`; `V = (0,0,1)`; cor = `base·(amb + corSol·max(0,N·L)) + corSol·especular·max(0,R·V)^brilho·alfa`; ambiente/cor do sol padrão (0,42 / branco) quando w == 0. Mesma estrutura de pass do `TerrenoLuz`.
  - `PhongF1 : MonoBehaviour` — `SpriteRenderer[] pecas`; em `LateUpdate` escreve `_Rotacao = (cos z, sin z)` de cada peça via `MaterialPropertyBlock` (Get antes de Set, para não perder a textura do sprite).

- [ ] **Step 1: gerar e conferir** — `python3 -I Tools/gerar_f1.py CarroCG/Assets/Art`; conferir tamanhos (`f1_normal (960, 300)`, `f1_roda_normal (184, 184)`, `sol/lua (128, 128)`, `ceu_gradiente (4, 256)`, `feixe (256, 96)`) e abrir (Read) `f1_normal.png`: azul-lilás no miolo (normal para a frente), bordas avermelhadas/esverdeadas conforme o lado.
- [ ] **Step 2: escrever** `F1Phong.shader` e `PhongF1.cs`; compilar com `Tools/rodar_testes.sh EditMode` → `exit=0` (114).
- [ ] **Step 3: commit** — `git commit -m "Normal maps, sol/lua/céu e shader Phong do F1"`

---

### Task 5: Ciclo dia/noite, sombra do F1, tecla L, modo aula e selos

**Files:**
- Create: `CarroCG/Assets/Scripts/Jogo/CicloDia.cs`, `CarroCG/Assets/Scripts/Jogo/SombraF1.cs`
- Modify: `Partida.cs`, `ModoAula.cs`, `ConfigurarPartida.cs`, `TecnicasCG.cs`
- Test: `CarroCG/Assets/Tests/Editor/TecnicasCGTests.cs`

**Interfaces:**
- Consumes: `Ceu.Estado` (Task 1), `Sombra` + `Terreno.AlturaEm` (Task 2), `Terreno.Iluminar/ProximoSombreamento/modoSombreamento` (Task 3).
- Produces:
  - `CicloDia : MonoBehaviour` (`[DefaultExecutionOrder(-50)]`): `float duracao = 180; float tempoInicial = 0.12f; bool fixo; float tempoFixo = 0.097f (≈35°); bool lunar; float fatorAmbienteLunar = 0.35f; float intervaloTerreno = 0.5f`; refs `Terreno terreno; Light2D luzGlobal; Camera cam; SpriteRenderer ceuGradiente, sol, lua, estrelas; Light2D farol, lanterna; SpriteRenderer feixe`. `float Tempo { get; set; }`; `EstadoCeu Atual`; `void Aplicar(bool terrenoTambem)`. Em cada frame: avança o tempo (se não fixo), define `_DirSol = (dirLuz.x, dirLuz.y, 0.7, 1)`, `_CorSol`, `_CorAmbiente` (×`fatorAmbienteLunar` se lunar); não-lunar: `cam.backgroundColor = corCeuHorizonte`, `ceuGradiente.color = corCeuTopo`, sol/lua em arco (filhos da câmera, `dirSol·raio` / `−dirSol·raio`, visíveis só acima do horizonte), estrelas com alfa = noite; `luzGlobal.color = corLuz`, `intensity = lerp(0.45, 1, 1−noite)`; faróis e feixe com intensidade/alfa = noite (lunar: 0); a cada `intervaloTerreno` s chama `terreno.Iluminar(dirLuz, corLuz, corAmbiente)`.
  - `SombraF1 : MonoBehaviour` — refs `F1 f1; Terreno terreno; CicloDia ciclo; SpriteRenderer render`; `int largura = 512, altura = 48; float ppu = 32; float espessura = 0.22f; byte alfaMax = 110` (lunar 150). Em `LateUpdate`: pontos = caminho do `PolygonCollider2D` do chassi (mundo) + base das rodas; `Sombra.Faixa(pontos, ciclo.Atual.dirLuz, terreno.AlturaEm)`; polígono = superfície entre xMin..xMax (12 amostras) + mesma linha deslocada `espessura` para baixo; preenche com `Raster.ScanlineSpans` numa textura fixa `largura×altura` posicionada no canto do polígono; alfa = `alfaMax·(1 − noite)`; esconde o renderer quando a faixa é `null`. `bool Visivel` e `Rect Area` (mundo) públicos para teste.
  - `Partida`: `TMP_Text textoAviso` (toast); `void ProximoSombreamento()` (chama `terreno.ProximoSombreamento()` e mostra `"Sombreamento: <modo>"` por 1,5 s); tecla **L** chama, só jogando; precisa de `public Terreno terreno`.
  - `ModoAula`: `public CicloDia ciclo`; cada trecho desenha também `"normais"` (segmentos de 0,8 a partir de cada faceta, magenta, pelas `NormaisDosVertices`) e o rótulo ganha `· Iluminação: Lambert <modo> (L) - normais (magenta), sol (amarelo)`; linha `"Aula: sol"` do carro até `carro + dirLuz·2,5` atualizada em `Update`.
  - `ConfigurarPartida`: `public CicloDia ciclo`; Lua → `ciclo.fixo = true; ciclo.lunar = true`; menu (`forcarTerra`) → `ciclo.fixo = true; ciclo.tempoFixo = 0.2f`.
  - `TecnicasCG`: Iluminação → `Sim("Lambert Flat/Gouraud/Phong no terreno (tecla L)")`, `Sim("shader Phong do F1")`, `Sim("sombra projetada do F1")`; Preenchimento → `Sim("sombra do F1")`.

- [ ] **Step 1: teste** — em `TecnicasCGTests.SelosConformeOQueOJogoTemHoje`, incluir `"Iluminação e sombreamento"` no conjunto esperado.
- [ ] **Step 2: ver falhar** — `Tools/rodar_testes.sh EditMode` → `exit=2`, falha `SelosConformeOQueOJogoTemHoje`.
- [ ] **Step 3: implementar** os componentes e mudanças acima.
- [ ] **Step 4: ver passar** — `exit=0` (114).
- [ ] **Step 5: commit** — `git commit -m "Ciclo dia/noite, sombra do F1, tecla L, modo aula com normais e sol"`

---

### Task 6: Cenas remontadas + testes PlayMode + capturas

**Files:**
- Modify: `CarroCG/Assets/Editor/MontagemComum.cs`, `MontarJogo.cs`, `MontarMenu.cs`
- Modify: `CarroCG/Assets/Tests/PlayMode/JogoTests.cs`, `CarroCG/Assets/Tests/PlayMode/CapturasTests.cs`, `Tools/capturar.sh`

**Interfaces:**
- Consumes: tudo das Tasks 1–5.
- Produces:
  - `MontagemComum`: `LuzGlobal()` passa a devolver `Light2D`; `ConfigurarArte` trata `*_normal.png` como textura linear (`TextureImporterType.Default`, `sRGBTexture = false`, sem mipmap) e as camadas `f1_*` com `SpriteMeshType.FullRect` (UV 0–1 iguais às do normal map); materiais em `Assets/Materiais/`: `TerrenoLuz.mat`, `F1Pintura.mat` (especular 0,7, brilho 32), `F1Detalhes.mat` (0,35), `F1Atras.mat` (0,2), `F1Roda.mat` (`f1_roda_normal`, 0,1, brilho 8) — os três do corpo com `f1_normal`; `CriarF1` aplica os materiais e adiciona `PhongF1` (pecas = 3 camadas + 2 rodas), farol (Light2D point branca na frente, raio 7), lanterna (point vermelha atrás, raio 2,5) e feixe (sprite aditivo à frente); `CicloDia CriarCiclo(Camera cam, Terreno terreno, Light2D luz, PecasF1 f1)` cria gradiente/sol/lua/estrelas como filhos da câmera; `SombraF1 CriarSombra(PecasF1 f1, Terreno terreno, CicloDia ciclo)`; `CriarTerreno` define `materialLuz`.
  - `MontarJogo`/`MontarMenu`: usam as peças acima; `ConfigurarPartida.ciclo`, `ModoAula.ciclo`, `Partida.terreno`, `Partida.textoAviso` (caixa logo abaixo da distância).

- [ ] **Step 1: testes PlayMode** (em `JogoTests.cs`)
```csharp
    [UnityTest]
    public IEnumerator Terreno_UsaOShaderDeLuz()
    {
        yield return Carregar();
        yield return null;
        var r = Object.FindFirstObjectByType<Terreno>().chunks[0].go.GetComponent<SpriteRenderer>();
        Assert.AreEqual("CarroCG/TerrenoLuz", r.sharedMaterial.shader.name);
    }

    [UnityTest]
    public IEnumerator F1_UsaPhongEmTodasAsPecas()
    {
        yield return Carregar();
        foreach (var r in f1.transform.parent.GetComponentsInChildren<SpriteRenderer>())
            if (r.name != "Feixe") Assert.AreEqual("CarroCG/F1Phong", r.sharedMaterial.shader.name, r.name);
    }

    [UnityTest]
    public IEnumerator F1Phong_SemCicloNaoFicaPreto()
    {
        Shader.SetGlobalVector("_DirSol", Vector4.zero);       // como se nenhum CicloDia tivesse rodado
        var cam = new GameObject("cam-teste").AddComponent<Camera>();
        cam.orthographic = true; cam.orthographicSize = 2; cam.transform.position = new Vector3(0, 0, -10);
        cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = Color.black;
        var peca = new GameObject("peca").AddComponent<SpriteRenderer>();
        peca.sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 1);
        peca.sharedMaterial = new Material(Shader.Find("CarroCG/F1Phong"));
        var rt = new RenderTexture(16, 16, 24);
        cam.targetTexture = rt; cam.Render(); cam.Render();
        RenderTexture.active = rt;
        var tex = new Texture2D(16, 16, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, 16, 16), 0, 0);
        RenderTexture.active = null;
        Assert.Greater(tex.GetPixel(8, 8).grayscale, 0.2f, "F1 preto sem a luz global definida");
        yield return null;
    }

    [UnityTest]
    public IEnumerator PhongF1_PassaARotacaoDeCadaPeca()
    {
        yield return Carregar();
        f1.acelerador = 1;
        yield return new WaitForSeconds(1);                    // rodas girando, chassi quase reto
        var mpb = new MaterialPropertyBlock();
        foreach (var r in new[] { f1.eixoTraseiro.connectedBody.GetComponent<SpriteRenderer>(),
                                  f1.transform.Find("Pintura").GetComponent<SpriteRenderer>() })
        {
            r.GetPropertyBlock(mpb);
            var rot = mpb.GetVector("_Rotacao");
            float z = r.transform.eulerAngles.z * Mathf.Deg2Rad;
            Assert.AreEqual(Mathf.Cos(z), rot.x, 0.05f, r.name);
            Assert.AreEqual(Mathf.Sin(z), rot.y, 0.05f, r.name);
        }
    }

    [UnityTest]
    public IEnumerator Ciclo_DefineOSolDoShader()
    {
        yield return Carregar();
        Assert.AreEqual(1f, Shader.GetGlobalVector("_DirSol").w);
    }

    [UnityTest]
    public IEnumerator TeclaL_TrocaOSombreamentoEAvisa()
    {
        yield return Carregar();
        partida.ProximoSombreamento();
        Assert.AreEqual(ModoSombreamento.Flat, Object.FindFirstObjectByType<Terreno>().modoSombreamento);
        Assert.IsTrue(partida.textoAviso.gameObject.activeInHierarchy);
        StringAssert.Contains("Flat", partida.textoAviso.text);
    }

    [UnityTest]
    public IEnumerator Noite_FarolAcesoEEstrelas()
    {
        yield return Carregar();
        var ciclo = Object.FindFirstObjectByType<CicloDia>();
        ciclo.Tempo = 0.75f; ciclo.Aplicar(true);
        Assert.Greater(ciclo.farol.intensity, 0.5f);
        Assert.Greater(ciclo.estrelas.color.a, 0.5f);
        ciclo.Tempo = 0.25f; ciclo.Aplicar(true);
        Assert.Less(ciclo.farol.intensity, 0.1f);
    }

    [UnityTest]
    public IEnumerator Lua_LuzFixa()
    {
        Sessao.Modo = Modo.Lua;
        yield return Carregar();
        var ciclo = Object.FindFirstObjectByType<CicloDia>();
        var antes = ciclo.Atual.dirLuz;
        yield return new WaitForSeconds(2);
        Assert.IsTrue(ciclo.fixo && ciclo.lunar);
        Assert.AreEqual(antes, ciclo.Atual.dirLuz);
    }

    [UnityTest]
    public IEnumerator Sombra_DeDiaPertoDoCarroESobreOChao()
    {
        yield return Carregar();
        var ciclo = Object.FindFirstObjectByType<CicloDia>();
        ciclo.Tempo = 0.25f; ciclo.Aplicar(true);
        yield return new WaitForSeconds(0.5f);
        var sombra = Object.FindFirstObjectByType<SombraF1>();
        Assert.IsTrue(sombra.Visivel);
        Assert.Less(Mathf.Abs(sombra.Area.center.x - f1.transform.position.x), 3f);
        Assert.Less(sombra.Area.yMax, f1.transform.position.y);
    }

    [UnityTest]
    public IEnumerator ModoAula_MostraNormaisESol()
    {
        yield return Carregar();
        var aula = Object.FindFirstObjectByType<ModoAula>();
        aula.DefinirAtivo(true);
        yield return null;
        var trecho = Object.FindFirstObjectByType<Terreno>().chunks[0].go.transform;
        Assert.IsNotNull(trecho.Find("Aula/normais"));
        Assert.IsTrue(GameObject.Find("Aula: sol").GetComponent<LineRenderer>().enabled);
    }
```

- [ ] **Step 2: ver falhar** — `Tools/rodar_testes.sh PlayMode` → `exit=2`, falham os novos (cena antiga).
- [ ] **Step 3: implementar** as mudanças de `MontagemComum`/`MontarJogo`/`MontarMenu`; remontar: `"$UNITY" -batchmode -nographics -projectPath CarroCG -executeMethod MontarJogo.Executar -quit -logFile "$PWD/test-montar.log"` e o mesmo com `MontarMenu` → ambos `exit=0`, sem `error CS`/`Exception`/`referenced script`.
- [ ] **Step 4: ver passar** — `Tools/rodar_testes.sh PlayMode` → `exit=0`, nenhum falho; `Tools/rodar_testes.sh EditMode` → `exit=0` (114).
- [ ] **Step 5: capturas** — acrescentar em `CapturasTests` um `[UnityTest, Explicit] Luz()` que, no jogo parado no 3º trecho, fotografa `luz-dia.png` (t=0,25), `luz-por-do-sol.png` (t=0,47), `luz-noite.png` (t=0,75) e, no pôr do sol, `luz-flat.png`, `luz-gouraud.png`, `luz-phong.png` (chamando `ProximoSombreamento`); `Tools/capturar.sh` inclui `CapturasTests.Luz`. Rodar `Tools/capturar.sh` → `exit=0`; abrir as imagens (Read) e conferir: céu azul/laranja/escuro com sol/lua no arco e estrelas à noite; morros mais claros do lado do sol; Flat com faixas visíveis por faceta, Gouraud liso, Phong liso com contraste maior nas curvas; F1 com brilho na carroceria do lado do sol; sombra escura no chão sob o carro, mais comprida no pôr do sol; faróis acesos à noite; `jogo-lua.png` com sombra dura.
- [ ] **Step 6: commit** — `git commit -m "Etapa 2: iluminação e sombreamento nas cenas, testes e capturas"`

---

## Depois desta etapa

Jogar e ver se o ciclo de 3 min agrada e se o Flat/Gouraud/Phong ficou visível o bastante para a narração (se não, `amostrasLuz` menor deixa o Flat mais facetado). Próximo plano: etapa 3 (projeções: parallax por perspectiva com tecla P; minimapa com Cohen–Sutherland e janela→viewport).
