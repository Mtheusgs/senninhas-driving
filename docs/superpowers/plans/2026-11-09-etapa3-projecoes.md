# Senninhas Driving — Etapa 3: Projeções e Minimapa — Plano de Implementação

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Fundo em 3 camadas com profundidade, posicionado e escalado pela **projeção perspectiva** (tecla **P** troca para **paralela**); **minimapa** no HUD com o perfil do terreno recortado por **Cohen–Sutherland**, mapeado **janela → viewport** e desenhado com **Bresenham**; modo aula mostrando a janela do minimapa e o modo de projeção.

**Architecture:** Funções puras novas: `Projecao` (matrizes 4×4 ortográfica/perspectiva, projeção com divisão homogênea, interpolação, fator de parallax), `Raster.ClipLine` (Cohen–Sutherland) e `Raster.JanelaViewport`, e `MinimapaDesenho.Desenhar` (recorta, mapeia e rasteriza num buffer). Componentes finos: `Parallax` (posiciona/escala as camadas a partir de `Projecao.FatorParallax`), `Minimapa` (atualiza a textura do HUD a cada 0,1 s). A arte das camadas é gerada pelo `gerar_f1.py` (silhuetas claras e emendáveis, tingidas pela hora do dia).

**Tech Stack:** Unity 6000.3.25f1, URP 2D, uGUI (`RawImage`), Python 3 + Pillow.

**Spec:** `docs/superpowers/specs/2026-09-30-f1-hillclimb-design.md` (seções "Projeções", "Recorte" — minimapa, "Ordem de entrega" etapa 3), com `docs/superpowers/specs/2026-11-04-menu-modo-aula-design.md` (modo aula cresce por etapa; tela Técnicas com selos por lugar).

## Global Constraints

- Unity 6000.3.25f1, URP 2D; testes em batchmode com o Editor fechado; nunca duas Unity no mesmo projeto.
- Algoritmos da disciplina em funções puras testadas (`Projecao`, `Raster.ClipLine`, `Raster.JanelaViewport`, `MinimapaDesenho`); componentes só aplicam.
- Profundidades das camadas: **5, 20, 60** (perto → longe); `d` (distância do observador ao plano de projeção) = **10**. Fator `d/(d+z)` aplicado ao deslocamento em relação à câmera **e** à escala. Paralela: fator 1 para todas.
- Tecla **P** alterna perspectiva/paralela (padrão: perspectiva) com aviso de 1,5 s, como a tecla L.
- Minimapa: janela de **120 × 45** unidades centrada no carro; textura **256 × 96**; canto superior direito do HUD, sem cobrir a distância nem os avisos.
- Textos só Latin-1 (`-`, `·`).
- Cenas remontadas por script; testes não gravam nas chaves reais de `PlayerPrefs`.

## Review Focus

1. **Segmento com os dois extremos fora mas cruzando a janela** (diagonal que corta dois cantos) — tem de aparecer só o pedaço de dentro. Teste: `RasterTests.ClipLine_CruzaDuasBordas` (Task 1).
2. **Segmento vertical/horizontal exatamente na borda da janela** — sem divisão por zero, aceito. Teste: `RasterTests.ClipLine_SobreABorda` (Task 1).
3. **Câmera muito à frente com camada perspectiva** — a faixa repetida tem de cobrir a tela inteira em qualquer x (sem buraco). Teste: `JogoTests.Parallax_CobreATelaEmQualquerX` (Task 4).
4. **Minimapa perto do começo do mundo / fim dos trechos gerados** — sem exceção, só desenha o que existe. Teste: `MinimapaDesenhoTests.SemSegmentos_SoOCarro` (Task 2).
5. **Projeção perspectiva com ponto atrás/no observador (z ≤ −d)** — não pode explodir (divisão por ~0). Teste: `ProjecaoTests.Perspectiva_PontoNoObservador_NaoExplode` (Task 1).

## Estrutura de arquivos

```
Tools/gerar_f1.py                         + Fundo/montanhas.png, Fundo/morros.png, Fundo/pinheiros.png
CarroCG/Assets/
├── Scripts/Raster.cs                     + ClipLine (Cohen–Sutherland), CodigoRegiao, JanelaViewport
├── Scripts/Jogo/
│   ├── Projecao.cs                       matrizes 4×4, Projetar, Interpolar, FatorParallax (puro)
│   ├── MinimapaDesenho.cs                recorte + janela→viewport + Bresenham num buffer (puro)
│   ├── Parallax.cs                       camadas de fundo
│   ├── Minimapa.cs                       textura do minimapa no HUD
│   ├── Partida.cs                        + tecla P e aviso
│   ├── ModoAula.cs                       + janela do minimapa e linha da projeção no rótulo
│   └── TecnicasCG.cs                     selos novos
├── Editor/MontagemComum.cs, MontarJogo.cs, MontarMenu.cs
└── Tests/Editor/ (RasterTests +, ProjecaoTests, MinimapaDesenhoTests), Tests/PlayMode/ (JogoTests +, CapturasTests +)
```

---

### Task 1: Projeções e recorte de linha (puros)

**Files:**
- Create: `CarroCG/Assets/Scripts/Jogo/Projecao.cs`
- Modify: `CarroCG/Assets/Scripts/Raster.cs`
- Test: `CarroCG/Assets/Tests/Editor/ProjecaoTests.cs`, `CarroCG/Assets/Tests/Editor/RasterTests.cs`

**Interfaces:**
- Produces:
  - `static class Projecao`:
    - `Matrix4x4 Ortografica()` — identidade com z zerado (projeta no plano z = 0).
    - `Matrix4x4 Perspectiva(float d)` — observador em z = −d, plano em z = 0: `x' = x·d/(d+z)`, `y' = y·d/(d+z)` via `w = (z + d)/d`.
    - `Vector2 Projetar(Matrix4x4 m, Vector3 v)` — multiplica (v, 1) e divide por w; se `w < 1e-4` usa `1e-4` (ponto no/atrás do observador não explode).
    - `Matrix4x4 Interpolar(Matrix4x4 a, Matrix4x4 b, float k)` — interpolação linear elemento a elemento.
    - `float FatorParallax(float z, float d, bool perspectiva)` — `perspectiva ? d/(d+z) : 1`.
  - `Raster`:
    - `int CodigoRegiao(Vector2 p, Rect r)` — bits: 1 esquerda, 2 direita, 4 baixo, 8 cima.
    - `bool ClipLine(Vector2 a, Vector2 b, Rect r, out Vector2 a2, out Vector2 b2)` — Cohen–Sutherland: aceita trivial (`(ca|cb)==0`), rejeita trivial (`(ca&cb)!=0`), senão corta o ponto de fora contra a primeira borda do seu código e repete.
    - `Vector2 JanelaViewport(Vector2 p, Rect janela, Rect viewport)` — mapeamento linear da janela para a viewport.

- [ ] **Step 1: testes**

`ProjecaoTests.cs`:
```csharp
using NUnit.Framework;
using UnityEngine;

public class ProjecaoTests
{
    [Test]
    public void Ortografica_PreservaXY()
    {
        var p = Projecao.Projetar(Projecao.Ortografica(), new Vector3(3, -2, 40));
        Assert.AreEqual(3f, p.x, 1e-5);
        Assert.AreEqual(-2f, p.y, 1e-5);
    }

    [Test]
    public void Perspectiva_DivisaoHomogenea()
    {
        var p = Projecao.Projetar(Projecao.Perspectiva(10), new Vector3(4, 2, 10));     // d/(d+z) = 0,5
        Assert.AreEqual(2f, p.x, 1e-5);
        Assert.AreEqual(1f, p.y, 1e-5);
    }

    [Test]
    public void Perspectiva_ZMaiorFicaMaisPertoDoCentro()
    {
        var m = Projecao.Perspectiva(10);
        Assert.Less(Projecao.Projetar(m, new Vector3(5, 0, 60)).x, Projecao.Projetar(m, new Vector3(5, 0, 5)).x);
    }

    [Test]
    public void Perspectiva_PontoNoObservador_NaoExplode()
    {
        var p = Projecao.Projetar(Projecao.Perspectiva(10), new Vector3(1, 1, -10));
        Assert.IsFalse(float.IsNaN(p.x) || float.IsInfinity(p.x));
    }

    [Test]
    public void Interpolar_MeioFicaEntreAsDuas()
    {
        var v = new Vector3(4, 0, 10);
        float orto = Projecao.Projetar(Projecao.Ortografica(), v).x;
        float persp = Projecao.Projetar(Projecao.Perspectiva(10), v).x;
        float meio = Projecao.Projetar(Projecao.Interpolar(Projecao.Ortografica(), Projecao.Perspectiva(10), 0.5f), v).x;
        Assert.Greater(meio, persp);
        Assert.Less(meio, orto);
    }

    [Test]
    public void FatorParallax_PerspectivaDiminuiComAProfundidade_ParalelaUm()
    {
        Assert.AreEqual(1f, Projecao.FatorParallax(0, 10, true), 1e-5);
        Assert.AreEqual(0.5f, Projecao.FatorParallax(10, 10, true), 1e-5);
        Assert.Less(Projecao.FatorParallax(60, 10, true), Projecao.FatorParallax(20, 10, true));
        Assert.AreEqual(1f, Projecao.FatorParallax(60, 10, false));
    }
}
```

Em `RasterTests.cs` (antes da última `}`):
```csharp
    static readonly Rect J = Rect.MinMaxRect(0, 0, 10, 5);

    [Test]
    public void CodigoRegiao_BitsPorLado()
    {
        Assert.AreEqual(0, Raster.CodigoRegiao(new(5, 2), J));
        Assert.AreEqual(1, Raster.CodigoRegiao(new(-1, 2), J));
        Assert.AreEqual(2 | 8, Raster.CodigoRegiao(new(11, 6), J));
    }

    [Test]
    public void ClipLine_AceitacaoTrivial()
    {
        Assert.IsTrue(Raster.ClipLine(new(1, 1), new(9, 4), J, out var a, out var b));
        Assert.AreEqual(new Vector2(1, 1), a);
        Assert.AreEqual(new Vector2(9, 4), b);
    }

    [Test]
    public void ClipLine_RejeicaoTrivial() => Assert.IsFalse(Raster.ClipLine(new(-5, 1), new(-1, 4), J, out _, out _));

    [Test]
    public void ClipLine_CruzaDuasBordas()
    {
        Assert.IsTrue(Raster.ClipLine(new(-2, -1), new(12, 6), J, out var a, out var b));   // y = 0,5x
        Assert.AreEqual(0f, a.x, 1e-4); Assert.AreEqual(0f, a.y, 1e-4);
        Assert.AreEqual(10f, b.x, 1e-4); Assert.AreEqual(5f, b.y, 1e-4);
    }

    [Test]
    public void ClipLine_ForaPassandoPeloCanto_Rejeita() => Assert.IsFalse(Raster.ClipLine(new(-1, 3), new(3, 7), J, out _, out _));

    [Test]
    public void ClipLine_SobreABorda()
    {
        Assert.IsTrue(Raster.ClipLine(new(0, -3), new(0, 8), J, out var a, out var b));
        Assert.AreEqual(new Vector2(0, 0), a);
        Assert.AreEqual(new Vector2(0, 5), b);
    }

    [Test]
    public void JanelaViewport_CantosECentro()
    {
        var v = Rect.MinMaxRect(0, 0, 256, 96);
        Assert.AreEqual(new Vector2(0, 0), Raster.JanelaViewport(new(0, 0), J, v));
        Assert.AreEqual(new Vector2(256, 96), Raster.JanelaViewport(new(10, 5), J, v));
        Assert.AreEqual(new Vector2(128, 48), Raster.JanelaViewport(new(5, 2.5f), J, v));
    }
```

- [ ] **Step 2: ver falhar** — `Tools/rodar_testes.sh EditMode` → `exit=1`, `'Projecao' does not exist` / `'Raster' does not contain 'ClipLine'`.
- [ ] **Step 3: implementar** `Projecao.cs` e os três métodos novos em `Raster.cs` (assinaturas acima).
- [ ] **Step 4: ver passar** — `exit=0` (116 + 6 + 7 = 129).
- [ ] **Step 5: commit** — `git commit -m "Projeções (ortográfica, perspectiva, interpolação) e recorte de linha Cohen-Sutherland"`

---

### Task 2: Desenho do minimapa (puro)

**Files:**
- Create: `CarroCG/Assets/Scripts/Jogo/MinimapaDesenho.cs`
- Test: `CarroCG/Assets/Tests/Editor/MinimapaDesenhoTests.cs`

**Interfaces:**
- Consumes: `Raster.ClipLine`, `Raster.JanelaViewport`, `Raster.Line` (Task 1 e anteriores).
- Produces: `static class MinimapaDesenho`:
  - `void Desenhar(Color32[] px, int w, int h, Rect janela, IEnumerable<(Vector2 a, Vector2 b)> segmentos, Vector2 carro, Color32 corLinha, Color32 corCarro, Color32 corFundo)` — limpa com `corFundo`; para cada segmento: `ClipLine` contra a janela, `JanelaViewport` para `Rect(0, 0, w−1, h−1)`, arredonda e pinta com `Raster.Line`; carro = quadrado 5×5 `corCarro` centrado no seu pixel (recortado à textura).
  - `IEnumerable<(Vector2, Vector2)> Segmentos(IEnumerable<Vector2[]> curvas)` — pares consecutivos de cada curva.

- [ ] **Step 1: testes**

```csharp
using System.Linq;
using NUnit.Framework;
using UnityEngine;

public class MinimapaDesenhoTests
{
    const int W = 40, H = 20;
    static readonly Color32 Linha = new(255, 255, 255, 255), Carro = new(255, 0, 0, 255), Fundo = new(0, 0, 0, 0);
    static readonly Rect Janela = Rect.MinMaxRect(0, 0, 40, 20);   // 1 unidade = ~1 pixel

    static Color32[] Desenhar(params (Vector2, Vector2)[] segs)
    {
        var px = new Color32[W * H];
        MinimapaDesenho.Desenhar(px, W, H, Janela, segs, new Vector2(20, 10), Linha, Carro, Fundo);
        return px;
    }

    [Test]
    public void SemSegmentos_SoOCarro()
    {
        var px = Desenhar();
        Assert.AreEqual(25, px.Count(c => c.Equals(Carro)));
        Assert.AreEqual(0, px.Count(c => c.Equals(Linha)));
    }

    [Test]
    public void SegmentoForaDaJanela_NaoAparece() =>
        Assert.AreEqual(0, Desenhar((new(-10, 5), new(-2, 8))).Count(c => c.Equals(Linha)));

    [Test]
    public void SegmentoCruzandoAJanela_SoOPedacoDeDentro()
    {
        var px = Desenhar((new(-20, 2), new(60, 2)));               // linha horizontal passando pela janela inteira
        Assert.AreEqual(W, px.Count(c => c.Equals(Linha)));         // uma linha de pixels de ponta a ponta, sem sair
    }

    [Test]
    public void Segmentos_ParesConsecutivos()
    {
        var s = MinimapaDesenho.Segmentos(new[] { new Vector2[] { new(0, 0), new(1, 1), new(2, 0) } }).ToArray();
        Assert.AreEqual(2, s.Length);
        Assert.AreEqual(new Vector2(1, 1), s[1].Item1);
    }
}
```

- [ ] **Step 2: ver falhar** — `exit=1`, `'MinimapaDesenho' does not exist`.
- [ ] **Step 3: implementar** `MinimapaDesenho.cs`.
- [ ] **Step 4: ver passar** — `exit=0` (133).
- [ ] **Step 5: commit** — `git commit -m "Minimapa: recorte Cohen-Sutherland, janela-viewport e Bresenham num buffer"`

---

### Task 3: Arte das camadas de fundo

**Files:**
- Modify: `Tools/gerar_f1.py`
- Create: `CarroCG/Assets/Art/Fundo/montanhas.png`, `morros.png`, `pinheiros.png`

**Interfaces:**
- Produces: três PNGs **1024 × 256**, silhueta branca/cinza-clara sobre transparente, **emendáveis na horizontal** (formas por funções periódicas de período 1024): `montanhas` (picos pontudos, base cheia até embaixo), `morros` (ondas suaves por soma de senos), `pinheiros` (fileira de pinheiros triangulares sobre uma base, posições sorteadas com semente fixa e repetidas com período 1024). PPU 32 no Unity (32 × 8 unidades por repetição).

- [ ] **Step 1: implementar** `montanhas()`, `morros()`, `pinheiros()` em `gerar_f1.py` (saída em `Fundo/`) e gerar: `python3 -I Tools/gerar_f1.py CarroCG/Assets/Art`.
- [ ] **Step 2: conferir** — tamanhos `(1024, 256)`; montar uma prévia com cada imagem **lado a lado duas vezes** e abrir (Read): sem degrau na emenda do meio.
- [ ] **Step 3: commit** — `git commit -m "Camadas de fundo geradas (montanhas, morros, pinheiros) emendáveis"`

---

### Task 4: Parallax, minimapa no HUD, tecla P, modo aula, selos e cenas

**Files:**
- Create: `CarroCG/Assets/Scripts/Jogo/Parallax.cs`, `CarroCG/Assets/Scripts/Jogo/Minimapa.cs`
- Modify: `Partida.cs`, `ModoAula.cs`, `TecnicasCG.cs`, `CicloDia.cs`, `Editor/MontagemComum.cs`, `Editor/MontarJogo.cs`, `Editor/MontarMenu.cs`
- Test: `CarroCG/Assets/Tests/Editor/TecnicasCGTests.cs`, `CarroCG/Assets/Tests/PlayMode/JogoTests.cs`, `CarroCG/Assets/Tests/PlayMode/CapturasTests.cs`, `Tools/capturar.sh`

**Interfaces:**
- Consumes: `Projecao.FatorParallax` (Task 1), `MinimapaDesenho` (Task 2), arte (Task 3), `CicloDia.Atual` (etapa 2), `Terreno.chunks`.
- Produces:
  - `Parallax : MonoBehaviour` (`[DefaultExecutionOrder(100)]`, roda depois da câmera): `Camera cam; float d = 10; bool perspectiva = true; Camada[] camadas` com `[Serializable] class Camada { SpriteRenderer render; float z; float yBase; Color cor; }`. Em `LateUpdate`, para cada camada: `f = Projecao.FatorParallax(z, d, perspectiva)`; escala `= f`; renderer em `SpriteDrawMode.Tiled` com largura `≥ 2,2 × largura da tela / f`; `x = cam.x + Repeat((0 − cam.x)·f, larguraDaRepeticao·f) − larguraDaRepeticao·f` (ancorada no mundo, desloca `f` vezes o movimento da câmera); `y = cam.y + (yBase − cam.y)·f`; cor = `camada.cor × (corLuz·0,5 + corAmbiente)` do `CicloDia` (Lua: tons de cinza, já vindos do `CicloDia` lunar). Ordem de desenho: mais longe atrás (sorting −40, −30, −20).
  - `Minimapa : MonoBehaviour`: `Terreno terreno; F1 f1; RawImage imagem; Vector2 tamanhoJanela = (120, 45); int largura = 256, altura = 96; float intervalo = 0.1f`; `Rect Janela` (mundo, centrada no carro) e `Texture2D Textura` públicos; a cada intervalo chama `MinimapaDesenho.Desenhar` com as curvas dos trechos (`Segmentos`), linha branca, carro vermelho, fundo preto 50%.
  - `Partida`: `public Parallax parallax`; `void AlternarProjecao()` (troca `parallax.perspectiva`, aviso `"Projeção: perspectiva"` / `"Projeção: paralela"` por 1,5 s); tecla **P** só jogando.
  - `ModoAula`: `public Minimapa minimapa`; linha `"Aula: janela do minimapa"` (retângulo da `Janela` no mundo, laranja, atualizado em `Update`); rótulo ganha `· Projeção: {perspectiva|paralela} (P) - minimapa: recorte Cohen-Sutherland, janela->viewport, Bresenham (laranja)`.
  - `TecnicasCG`: Projeção → `Sim("parallax do fundo (tecla P)")`; Rasterização → `Sim("Bresenham: minimapa")`; Recorte → `Sim("Cohen-Sutherland: minimapa")`; Translação → `Sim("parallax")`.
  - `MontagemComum`: `Parallax CriarParallax(Camera cam, CicloDia ciclo)` (3 camadas: pinheiros z=5 yBase −1 cor (0,35, 0,55, 0,35); morros z=20 yBase 0 cor (0,45, 0,62, 0,45); montanhas z=60 yBase 2 cor (0,55, 0,6, 0,72); arte em `Assets/Art/Fundo`, PPU 32, `SpriteMeshType.FullRect` + `wrapMode Repeat`); `Minimapa CriarMinimapa(Transform hud, Terreno t, F1 f1)` (caixa no canto superior direito, `RawImage` 256×96 escalada ×1,6, borda escura). `MontarJogo` e `MontarMenu` criam o parallax; só o jogo cria o minimapa.

- [ ] **Step 1: testes**

Em `TecnicasCGTests.SelosConformeOQueOJogoTemHoje`, incluir `"Projeção paralela e perspectiva"` e `"Rasterização"` no conjunto esperado.

Em `JogoTests.cs`:
```csharp
    [UnityTest]
    public IEnumerator Parallax_CamadaLongeAndaMenosNaPerspectiva_IgualNaParalela()
    {
        yield return Carregar();
        var p = Object.FindFirstObjectByType<Parallax>();
        var cam = Camera.main;
        float Tela(int i) => p.camadas[i].render.transform.position.x - cam.transform.position.x;
        var segue = cam.GetComponent<CameraSegue>(); segue.enabled = false;
        float perto0 = Tela(0), longe0 = Tela(2);
        cam.transform.position += Vector3.right * 3;
        yield return null;
        float perto = Mathf.Abs(Mathf.Repeat(Tela(0) - perto0 + 50, 100) - 50), longe = Mathf.Abs(Mathf.Repeat(Tela(2) - longe0 + 50, 100) - 50);
        Assert.Greater(perto, longe, "na perspectiva a camada longe deveria andar menos na tela");
        Assert.Less(p.camadas[2].render.transform.localScale.x, p.camadas[0].render.transform.localScale.x);
        partida.AlternarProjecao();
        yield return null;
        Assert.AreEqual(p.camadas[0].render.transform.localScale.x, p.camadas[2].render.transform.localScale.x, 1e-4, "paralela: mesma escala");
        StringAssert.Contains("paralela", partida.textoAviso.text);
    }

    [UnityTest]
    public IEnumerator Parallax_CobreATelaEmQualquerX()
    {
        yield return Carregar();
        var p = Object.FindFirstObjectByType<Parallax>();
        var cam = Camera.main;
        cam.GetComponent<CameraSegue>().enabled = false;
        foreach (float x in new[] { 0f, 137.3f, 999.9f, 5003.7f })
        {
            cam.transform.position = new Vector3(x, cam.transform.position.y, -10);
            yield return null;
            float meia = cam.orthographicSize * cam.aspect;
            foreach (var c in p.camadas)
            {
                var b = c.render.bounds;
                Assert.LessOrEqual(b.min.x, x - meia, $"buraco à esquerda em x={x} ({c.render.name})");
                Assert.GreaterOrEqual(b.max.x, x + meia, $"buraco à direita em x={x} ({c.render.name})");
            }
        }
    }

    [UnityTest]
    public IEnumerator Minimapa_DesenhaTerrenoECarroNoCentro()
    {
        yield return Carregar();
        yield return new WaitForSeconds(0.3f);
        var m = Object.FindFirstObjectByType<Minimapa>();
        var px = m.Textura.GetPixels32();
        Assert.Greater(px.Count(c => c.r == 255 && c.g == 255 && c.b == 255), 50, "sem linha do terreno");
        var centro = m.Textura.GetPixel(m.largura / 2, m.altura / 2);
        Assert.Greater(centro.r, 0.9f); Assert.Less(centro.g, 0.1f);
        Assert.AreEqual(f1.transform.position.x, m.Janela.center.x, 0.5f);
    }

    [UnityTest]
    public IEnumerator ModoAula_MostraJanelaDoMinimapaEProjecao()
    {
        yield return Carregar();
        var aula = Object.FindFirstObjectByType<ModoAula>();
        aula.DefinirAtivo(true);
        yield return null;
        Assert.IsTrue(GameObject.Find("Aula: janela do minimapa").GetComponent<LineRenderer>().enabled);
        StringAssert.Contains("Projeção: perspectiva", aula.rotulo.text);
    }
```
(`using System.Linq;` já existe no arquivo.)

Em `CapturasTests.cs`, `[UnityTest, Explicit] Projecao()`: jogo andando 4 s, foto `projecao-perspectiva.png`; `AlternarProjecao()`, frame, foto `projecao-paralela.png`. `Tools/capturar.sh` inclui `CapturasTests.Projecao`.

- [ ] **Step 2: ver falhar** — `Tools/rodar_testes.sh EditMode` → falha `SelosConformeOQueOJogoTemHoje`; `Tools/rodar_testes.sh PlayMode` → `exit=1` (`'Parallax'`/`'Minimapa'` não existem).
- [ ] **Step 3: implementar** componentes, mudanças de `Partida`/`ModoAula`/`TecnicasCG` e as peças do `MontagemComum`; remontar `MontarJogo` e `MontarMenu` (ambos `exit=0`, sem `error CS`/`Exception`/`referenced script`).
- [ ] **Step 4: ver passar** — EditMode `exit=0` (133); PlayMode `exit=0`, nenhum falho.
- [ ] **Step 5: capturas** — `Tools/capturar.sh` → `exit=0`; abrir `projecao-perspectiva.png`, `projecao-paralela.png`, `jogo-aula.png`, `luz-noite.png`, `jogo-lua.png`, `menu-principal.png` e conferir: três faixas de fundo (montanhas azuladas longe e menores, morros, pinheiros perto) tingidas pela hora; na paralela as três do mesmo tamanho; minimapa no canto superior direito com o perfil branco e o carro vermelho no centro, sem cobrir a distância; no modo aula, retângulo laranja grande da janela do minimapa e a linha da projeção no rótulo; na Lua, camadas cinza.
- [ ] **Step 6: commit** — `git commit -m "Etapa 3: parallax por perspectiva (P), minimapa com Cohen-Sutherland, modo aula e capturas"`

---

## Depois desta etapa

Jogar e ver se o parallax ficou bonito com o ciclo dia/noite e se o minimapa ajuda. Próximos planos: etapa 4 (lago com reflexo recortado) e etapa 5 (intro com o F1 3D em wireframe, que reusa `Projecao` e `Raster.Line`).
