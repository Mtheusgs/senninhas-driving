# Senninhas Driving — Menu, Garagem, Modo Aula, Modo Lua e Ajuste — Plano de Implementação

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** O jogo abre num menu "Senninhas Driving" (fundo animado, Jogar Terra/Lua, Garagem de cores, Controles, Configurações, Técnicas de CG, Recorde, Sair); dentro do jogo há pausa/fim com botões, modo aula (Tab) mostrando Bézier, emenda espelhada, recorte e eixos do F1, com repintura por scanline (F); morros mais íngremes, capotar possível e um modo Lua de gravidade baixa.

**Architecture:** Regras puras novas e testadas em EditMode (`Preferencias`, `Sessao`, `ModoJogo`, `Garagem`, `TecnicasCG`, `AulaGeometria`). Componentes finos: `ConfigurarPartida` aplica o modo e a qualidade antes de tudo (`[DefaultExecutionOrder(-100)]`), `PinturaF1` tinge a camada de pintura, `PilotoAutomatico` dirige o carro do menu, `ModoAula` desenha a sobreposição, `Menu` controla as telas. As cenas `Menu.unity` e `Jogo.unity` são montadas por script, com peças comuns em `Editor/MontagemComum.cs`. Testes PlayMode exercitam o fluxo inteiro clicando nos botões de verdade (`onClick.Invoke()`).

**Tech Stack:** Unity 6000.3.25f1, URP 17.3 2D, uGUI 2.0 + TextMeshPro, Physics 2D, Unity Test Framework 1.6, Python 3 + Pillow 10.2.

**Spec:** `docs/superpowers/specs/2026-11-04-menu-modo-aula-design.md` (sobre `docs/superpowers/specs/2026-09-30-f1-hillclimb-design.md`)

## Global Constraints

- Unity 6000.3.25f1, URP 2D, uGUI + TextMeshPro, Input Manager antigo (`Input.GetKey`). Testes em batchmode com o **Editor fechado**.
- Cenas montadas por script; nada ajustado à mão que se perca ao remontar.
- Algoritmos de CG continuam em funções puras testadas; o modo aula só desenha dados que elas já produzem.
- Persistência em `PlayerPrefs` via `Preferencias`; testes usam `Preferencias.Prefixo = "teste."` e nunca gravam nas chaves reais.
- Build: `Menu.unity` (0), `Jogo.unity` (1), `Main.unity` (2). A cena do vídeo continua funcionando.
- Nenhum botão ou opção sem efeito (anti-objetivo da spec).
- Modo aula desligado por padrão; liga por Tab ou por "Modo aula ao iniciar".
- Só a pintura muda de cor; rodas, piloto, halo, asas pretas, faixa, número e texto ficam iguais; o collider do chassi continua com a silhueta completa.
- Lua: gravidade entre 20% e 25% da Terra; inclinação ≤ 1,5 e x crescente continuam valendo nos dois modos.
- Aceleração, velocidade e combustível não mudam.
- Textos da UI só com caracteres Latin-1 (fonte LiberationSans SDF): usar `-` e `·`, nunca `–`, `−` ou `•`.

## Review Focus

1. **Estado global vazando entre cenas** — `Physics2D.gravity` (Lua → menu/Terra), `Time.timeScale` (pausa/congelado → menu), `Sessao.Modo`. Testes: `MenuTests.VoltarDaLua_MenuComGravidadeDaTerra`, `MenuTests.PausaMenuPrincipal_VoltaAoMenu` (Task 8), `JogoTests.Scanline_CongelaERetoma` (Task 7).
2. **Navegação só por teclado** — toda tela aberta deixa um botão selecionado. Teste: `MenuTests.CadaTela_SelecionaUmBotao` (Task 8).
3. **Botão que não faz nada** — todo `Button` das duas cenas tem ação persistente. Testes: `MenuTests.TodosOsBotoesTemAcao` (Task 8), `JogoTests.BotoesDoHud_TemAcao` (Task 7).
4. **F na hora errada** — com o jogo pausado/terminado ou durante outra repintura, F não pode congelar o jogo. Teste: `JogoTests.Scanline_ForaDoJogo_NaoFazNada` (Task 7).
5. **Preferências dos testes nas chaves reais** — Testes: `PreferenciasTests.Prefixo_NaoMexeNasChavesReais` (Task 1), `JogoTests.Terminar_NaoMexeNoRecordeReal` (Task 1/7).

## Estrutura de arquivos

```
Tools/
├── gerar_f1.py              camadas do F1 (atrás, pintura, frente) + silhueta + céu lunar
└── capturar.sh              novas capturas
CarroCG/Assets/
├── Art/F1/                  + f1_atras.png, f1_pintura.png, f1_frente.png
├── Art/Ceu/                 estrelas.png, terra.png
├── Scripts/
│   ├── CarroCG.asmdef       + UnityEngine.UI
│   ├── Anim.cs              + tempoReal
│   └── Jogo/
│       ├── Preferencias.cs  Modo, Sessao, Preferencias
│       ├── ModoJogo.cs      ParametrosModo, ModoJogo
│       ├── Garagem.cs       cores da garagem
│       ├── TecnicasCG.cs    dados da tela Técnicas de CG
│       ├── AulaGeometria.cs linhas do modo aula (puro)
│       ├── ConfigurarPartida.cs
│       ├── PinturaF1.cs
│       ├── PilotoAutomatico.cs
│       ├── ModoAula.cs
│       ├── Menu.cs
│       ├── Terreno.cs       parâmetros do gerador, polígono profundo, RepintarAnimado
│       ├── TerrenoGerador.cs novos padrões (Terra)
│       ├── F1.cs            novos padrões de giro/centro de massa
│       └── Partida.cs       recorde por modo, botões, Congelar, IrParaMenu
├── Editor/
│   ├── MontagemComum.cs     F1, câmera, terreno, céu lunar, UI, build
│   ├── MontarJogo.cs        reescrito sobre MontagemComum
│   └── MontarMenu.cs
├── Tests/Editor/            + PreferenciasTests, ModoJogoTests, TecnicasCGTests, AulaGeometriaTests
├── Tests/PlayMode/          JogoTests (ajustado), MenuTests, CapturasTests, Fotografo
└── Scenes/Menu.unity, Jogo.unity
```

## Como rodar

```bash
cd "/home/mtheusgc/Área de trabalho/Trabalhos/CG/TrabFinal"
Tools/rodar_testes.sh EditMode     # ou PlayMode
UNITY=$(ls -d ~/Unity/Hub/Editor/6000.3.25f1/Editor/Unity)
```
`exit=0` passou; `exit=2` falhas (listadas); `exit=1` erro de compilação (erros `CS` listados).
Contagens abaixo partem de 48 EditMode e 7 PlayMode (+1 Explicit).

---

### Task 1: Preferências, sessão, modos e garagem (puros)

**Files:**
- Create: `CarroCG/Assets/Scripts/Jogo/Preferencias.cs`, `ModoJogo.cs`, `Garagem.cs`
- Modify: `CarroCG/Assets/Scripts/Jogo/Partida.cs`, `CarroCG/Assets/Scripts/Jogo/TerrenoGerador.cs`, `CarroCG/Assets/Tests/PlayMode/JogoTests.cs`
- Test: `CarroCG/Assets/Tests/Editor/PreferenciasTests.cs`, `CarroCG/Assets/Tests/Editor/ModoJogoTests.cs`

**Interfaces:**
- Produces:
  - `enum Modo { Terra, Lua }`; `static class Sessao { static Modo Modo }`.
  - `static class Preferencias`: `string Prefixo`; `int Cor`; `int Qualidade` (0 Alta, 1 Média, 2 Baixa, clampado); `bool TelaCheia` (padrão true); `bool ModoAula` (padrão false); `float[] PpuTerreno = {64,48,32}`; `string[] NomesQualidade`; `float Recorde(Modo)`; `int MoedasDoRecorde(Modo)`; `bool RegistrarPartida(Modo, float distancia, int moedas)`; `void ZerarRecordes()`; `void ApagarTudo()`. Chaves reais: `cor`, `qualidade`, `telaCheia`, `modoAula`, `recorde.Terra`, `recorde.Lua`, `moedasRecorde.Terra`, `moedasRecorde.Lua`.
  - `struct ParametrosModo { float gravidade; int semente; float amplitudeInicial, ganhoPorTrecho, amplitudeMax; float torqueNoAr; Color ceu; TerrenoChunk.Paleta paleta; bool ceuLunar; }`; `static ParametrosModo ModoJogo.Parametros(Modo)`; `const float ModoJogo.GravidadeTerra = -9.81f`.
  - `static class Garagem`: `Color[] Cores` (8), `string[] Nomes` (8), `int IndiceEscolhido`, `Color CorEscolhida`.
  - `TerrenoGerador` com padrões da Terra: `amplitudeInicial=2.5, ganhoPorTrecho=0.5, amplitudeMax=12`.

- [ ] **Step 1: testes**

`CarroCG/Assets/Tests/Editor/PreferenciasTests.cs`:
```csharp
using NUnit.Framework;
using UnityEngine;

public class PreferenciasTests
{
    [SetUp] public void Isolar() => Preferencias.Prefixo = "teste.";

    [TearDown]
    public void Limpar()
    {
        Preferencias.ApagarTudo();
        Preferencias.Prefixo = "";
    }

    [Test]
    public void RegistrarPartida_GuardaSoOMelhor()
    {
        Assert.IsTrue(Preferencias.RegistrarPartida(Modo.Terra, 100, 5));
        Assert.IsFalse(Preferencias.RegistrarPartida(Modo.Terra, 50, 9));
        Assert.AreEqual(100f, Preferencias.Recorde(Modo.Terra));
        Assert.AreEqual(5, Preferencias.MoedasDoRecorde(Modo.Terra));
    }

    [Test]
    public void Recorde_SeparadoPorModo()
    {
        Preferencias.RegistrarPartida(Modo.Terra, 100, 5);
        Assert.AreEqual(0f, Preferencias.Recorde(Modo.Lua));
    }

    [Test]
    public void ZerarRecordes_ApagaOsDoisModos()
    {
        Preferencias.RegistrarPartida(Modo.Terra, 100, 5);
        Preferencias.RegistrarPartida(Modo.Lua, 70, 2);
        Preferencias.ZerarRecordes();
        Assert.AreEqual(0f, Preferencias.Recorde(Modo.Terra));
        Assert.AreEqual(0, Preferencias.MoedasDoRecorde(Modo.Lua));
    }

    [Test]
    public void Prefixo_NaoMexeNasChavesReais()
    {
        bool tinha = PlayerPrefs.HasKey("recorde.Terra");
        float antes = PlayerPrefs.GetFloat("recorde.Terra");
        Preferencias.RegistrarPartida(Modo.Terra, 12345, 1);
        Preferencias.Cor = 3;
        Assert.AreEqual(tinha, PlayerPrefs.HasKey("recorde.Terra"));
        Assert.AreEqual(antes, PlayerPrefs.GetFloat("recorde.Terra"));
        Assert.AreEqual(3, PlayerPrefs.GetInt("teste.cor"));
    }

    [Test]
    public void Padroes_SemNadaSalvo()
    {
        Assert.AreEqual(0, Preferencias.Cor);
        Assert.AreEqual(0, Preferencias.Qualidade);
        Assert.IsTrue(Preferencias.TelaCheia);
        Assert.IsFalse(Preferencias.ModoAula);
    }

    [Test]
    public void Qualidade_ForaDaFaixa_Clampa()
    {
        PlayerPrefs.SetInt("teste.qualidade", 9);
        Assert.AreEqual(2, Preferencias.Qualidade);
    }

    [Test]
    public void Garagem_CorForaDaFaixa_UsaAPrimeira()
    {
        Preferencias.Cor = 99;
        Assert.AreEqual(0, Garagem.IndiceEscolhido);
        Assert.AreEqual(Garagem.Cores[0], Garagem.CorEscolhida);
    }

    [Test]
    public void Garagem_OitoCoresDiferentesComNome()
    {
        Assert.AreEqual(8, Garagem.Cores.Length);
        Assert.AreEqual(Garagem.Cores.Length, Garagem.Nomes.Length);
        CollectionAssert.AllItemsAreUnique(Garagem.Cores);
    }
}
```

`CarroCG/Assets/Tests/Editor/ModoJogoTests.cs`:
```csharp
using NUnit.Framework;
using UnityEngine;

public class ModoJogoTests
{
    [Test]
    public void Lua_GravidadeEntre20e25PorCento()
    {
        float f = ModoJogo.Parametros(Modo.Lua).gravidade / ModoJogo.GravidadeTerra;
        Assert.That(f, Is.InRange(0.2f, 0.25f));
    }

    [Test]
    public void Terra_GravidadeNormal() =>
        Assert.AreEqual(ModoJogo.GravidadeTerra, ModoJogo.Parametros(Modo.Terra).gravidade);

    [Test]
    public void Lua_MaisAcidentadaQueATerraDesdeOInicio() =>
        Assert.Greater(ModoJogo.Parametros(Modo.Lua).amplitudeInicial, ModoJogo.Parametros(Modo.Terra).amplitudeInicial);

    [Test]
    public void Terra_IgualAosPadroesDoGerador()
    {
        var p = ModoJogo.Parametros(Modo.Terra);
        var g = new TerrenoGerador(0);
        Assert.AreEqual(g.amplitudeInicial, p.amplitudeInicial);
        Assert.AreEqual(g.ganhoPorTrecho, p.ganhoPorTrecho);
        Assert.AreEqual(g.amplitudeMax, p.amplitudeMax);
    }

    [Test]
    public void Lua_TerrenoJogavel()
    {
        var p = ModoJogo.Parametros(Modo.Lua);
        var g = new TerrenoGerador(p.semente)
            { amplitudeInicial = p.amplitudeInicial, ganhoPorTrecho = p.ganhoPorTrecho, amplitudeMax = p.amplitudeMax };
        for (int i = 0; i < 300; i++)
        {
            var t = g.Proximo();
            Assert.Less(t.p1.x, t.p2.x, $"trecho {i}");
            for (int k = 0; k <= 32; k++)
            {
                var d = Raster.BezierTangent(t.p0, t.p1, t.p2, t.p3, k / 32f);
                Assert.LessOrEqual(Mathf.Abs(d.y / d.x), 1.5f, $"trecho {i}, t={k / 32f}");
            }
        }
    }

    [Test]
    public void Lua_PaletaCinzaECeuPreto()
    {
        var p = ModoJogo.Parametros(Modo.Lua);
        Assert.AreEqual(Color.black, p.ceu);
        Assert.IsTrue(p.ceuLunar);
        Assert.AreEqual(p.paleta.grama.r, p.paleta.grama.g, 0.05f, "chão lunar não é cinza");
        Assert.IsFalse(ModoJogo.Parametros(Modo.Terra).ceuLunar);
    }
}
```

- [ ] **Step 2: ver falhar**

Run: `Tools/rodar_testes.sh EditMode`
Expected: `exit=1`, `The name 'Preferencias' does not exist` / `'Modo' could not be found`.

- [ ] **Step 3: implementar**

`CarroCG/Assets/Scripts/Jogo/Preferencias.cs`:
```csharp
using UnityEngine;

public enum Modo { Terra, Lua }

// Modo escolhido no menu; sobrevive à troca de cena (campo estático).
public static class Sessao
{
    public static Modo Modo = Modo.Terra;
}

// Escolhas salvas do jogador. Os testes trocam o Prefixo para nunca mexer nas chaves reais.
public static class Preferencias
{
    public static string Prefixo = "";

    public static readonly float[] PpuTerreno = { 64, 48, 32 };
    public static readonly string[] NomesQualidade = { "Alta", "Média", "Baixa" };

    static readonly string[] Chaves =
    {
        "cor", "qualidade", "telaCheia", "modoAula",
        "recorde.Terra", "recorde.Lua", "moedasRecorde.Terra", "moedasRecorde.Lua",
    };

    static string K(string chave) => Prefixo + chave;

    static void SalvarInt(string chave, int valor)
    {
        PlayerPrefs.SetInt(K(chave), valor);
        PlayerPrefs.Save();
    }

    public static int Cor
    {
        get => PlayerPrefs.GetInt(K("cor"), 0);
        set => SalvarInt("cor", value);
    }

    public static int Qualidade
    {
        get => Mathf.Clamp(PlayerPrefs.GetInt(K("qualidade"), 0), 0, PpuTerreno.Length - 1);
        set => SalvarInt("qualidade", value);
    }

    public static bool TelaCheia
    {
        get => PlayerPrefs.GetInt(K("telaCheia"), 1) == 1;
        set => SalvarInt("telaCheia", value ? 1 : 0);
    }

    public static bool ModoAula
    {
        get => PlayerPrefs.GetInt(K("modoAula"), 0) == 1;
        set => SalvarInt("modoAula", value ? 1 : 0);
    }

    public static float Recorde(Modo m) => PlayerPrefs.GetFloat(K("recorde." + m), 0);
    public static int MoedasDoRecorde(Modo m) => PlayerPrefs.GetInt(K("moedasRecorde." + m), 0);

    // Guarda a partida se for a melhor do modo. Retorna true quando é recorde novo.
    public static bool RegistrarPartida(Modo m, float distancia, int moedas)
    {
        if (distancia <= Recorde(m)) return false;
        PlayerPrefs.SetFloat(K("recorde." + m), distancia);
        PlayerPrefs.SetInt(K("moedasRecorde." + m), moedas);
        PlayerPrefs.Save();
        return true;
    }

    public static void ZerarRecordes()
    {
        foreach (Modo m in System.Enum.GetValues(typeof(Modo)))
        {
            PlayerPrefs.DeleteKey(K("recorde." + m));
            PlayerPrefs.DeleteKey(K("moedasRecorde." + m));
        }
        PlayerPrefs.Save();
    }

    public static void ApagarTudo()
    {
        foreach (var c in Chaves) PlayerPrefs.DeleteKey(K(c));
        PlayerPrefs.Save();
    }
}
```

`CarroCG/Assets/Scripts/Jogo/ModoJogo.cs`:
```csharp
using UnityEngine;

public struct ParametrosModo
{
    public float gravidade;                 // y, negativo
    public int semente;
    public float amplitudeInicial, ganhoPorTrecho, amplitudeMax;
    public float torqueNoAr;
    public Color ceu;
    public TerrenoChunk.Paleta paleta;
    public bool ceuLunar;
}

// Terra e Lua: mesma geração (Bézier, recorte, scanline), parâmetros diferentes.
public static class ModoJogo
{
    public const float GravidadeTerra = -9.81f;

    public static ParametrosModo Parametros(Modo m) => m == Modo.Lua
        ? new ParametrosModo
        {
            gravidade = GravidadeTerra * 0.22f,            // "gravidade bugada": o carro flutua
            semente = 42,
            amplitudeInicial = 5f, ganhoPorTrecho = 0.5f, amplitudeMax = 13f,
            torqueNoAr = 16f,
            ceu = Color.black,
            ceuLunar = true,
            paleta = new TerrenoChunk.Paleta
            {
                gramaBorda = new Color(0.86f, 0.86f, 0.9f),
                grama = new Color(0.7f, 0.7f, 0.74f),
                terraClara = new Color(0.56f, 0.56f, 0.6f),
                terraEscura = new Color(0.24f, 0.24f, 0.28f),
            },
        }
        : new ParametrosModo
        {
            gravidade = GravidadeTerra,
            semente = 7,
            amplitudeInicial = 2.5f, ganhoPorTrecho = 0.5f, amplitudeMax = 12f,
            torqueNoAr = 16f,
            ceu = new Color(0.55f, 0.78f, 0.95f),
            ceuLunar = false,
            paleta = new TerrenoChunk.Paleta(),
        };
}
```

`CarroCG/Assets/Scripts/Jogo/Garagem.cs`:
```csharp
using UnityEngine;

// Cores da pintura do F1. A escolha fica em Preferencias.Cor.
public static class Garagem
{
    public static readonly Color[] Cores =
    {
        new(0.97f, 0.12f, 0.2f), new(0.12f, 0.36f, 0.95f), new(0.1f, 0.75f, 0.32f), new(1f, 0.5f, 0.05f),
        new(1f, 0.85f, 0.1f), new(0.8f, 0.82f, 0.86f), new(0.16f, 0.16f, 0.18f), new(1f, 0.42f, 0.72f),
    };

    public static readonly string[] Nomes = { "Vermelho", "Azul", "Verde", "Laranja", "Amarelo", "Prata", "Preto", "Rosa" };

    public static int IndiceEscolhido => Preferencias.Cor >= 0 && Preferencias.Cor < Cores.Length ? Preferencias.Cor : 0;
    public static Color CorEscolhida => Cores[IndiceEscolhido];
}
```

Em `CarroCG/Assets/Scripts/Jogo/TerrenoGerador.cs`, trocar as três linhas:
```csharp
    public float amplitudeInicial = 1.5f;
    public float ganhoPorTrecho = 0.25f;
    public float amplitudeMax = 5f;
```
por:
```csharp
    public float amplitudeInicial = 2.5f;     // Terra (ModoJogo); morros crescem até ~45° perto de 300 m
    public float ganhoPorTrecho = 0.5f;
    public float amplitudeMax = 12f;
```

Em `CarroCG/Assets/Scripts/Jogo/Partida.cs`:
1. Remover a linha `    public static string ChaveRecorde = "recorde";   // os testes trocam por uma chave própria`.
2. Trocar o corpo de `Terminar` (do `float recorde = ...` até o `}` do `if`) e a linha do `textoFim.text` por:
```csharp
        bool novo = Preferencias.RegistrarPartida(Sessao.Modo, Distancia, Moedas);
        float recorde = Preferencias.Recorde(Sessao.Modo);
        textoFim.text = $"{motivo}\n<size=60%>{Distancia:0} m  ·  {Moedas} moedas\n" +
                        (novo ? "Novo recorde!" : $"Recorde: {recorde:0} m") + "\n\nR para jogar de novo</size>";
```
3. Em `AtualizarHud`, trocar `PlayerPrefs.GetFloat(ChaveRecorde, 0)` por `Preferencias.Recorde(Sessao.Modo)`.

Em `CarroCG/Assets/Tests/PlayMode/JogoTests.cs`:
1. Trocar o bloco `[SetUp] ... RecordeDeTeste ... [TearDown] ... RecordeReal() { ... }` por:
```csharp
    [SetUp]
    public void Isolar()
    {
        Preferencias.Prefixo = "teste.";
        Sessao.Modo = Modo.Terra;
    }

    [TearDown]
    public void Limpar()
    {
        Preferencias.ApagarTudo();
        Preferencias.Prefixo = "";
        Sessao.Modo = Modo.Terra;
        Physics2D.gravity = new Vector2(0, ModoJogo.GravidadeTerra);
        Time.timeScale = 1;
    }
```
2. Em `Terminar_NaoMexeNoRecordeReal`, trocar as 5 ocorrências de `"recorde"` por `"recorde.Terra"`.

- [ ] **Step 4: ver passar**

Run: `Tools/rodar_testes.sh EditMode`
Expected: `exit=0`, `total="62" passed="62"` (48 + 8 + 6). Se `TerrenoTests.Trechos_InclinacaoJogavel` falhar com os padrões novos, baixar `amplitudeMax` (e o da Terra em `ModoJogo`) até passar e registrar no ledger.

Run: `Tools/rodar_testes.sh PlayMode`
Expected: `exit=0`, `total="8" passed="7"`.

- [ ] **Step 5: commit**

```bash
git add CarroCG/Assets/Scripts/Jogo CarroCG/Assets/Tests
git commit -m "Preferências, sessão, modos Terra/Lua e cores da garagem"
```

---

### Task 2: Terreno — parâmetros por modo e recorte que sempre corta

**Files:**
- Modify: `CarroCG/Assets/Scripts/Jogo/Terreno.cs`, `CarroCG/Assets/Scripts/Anim.cs`
- Test: `CarroCG/Assets/Tests/Editor/TerrenoComponenteTests.cs`

**Interfaces:**
- Consumes: `TerrenoGerador` (padrões Task 1), `Raster.ClipPolygon`, `TerrenoChunk.Pintar`.
- Produces:
  - `Terreno`: novos campos `float amplitudeInicial=2.5, ganhoPorTrecho=0.5, amplitudeMax=12, fundoExtra=40` (copiados para o gerador em `Reiniciar`); campo `alturaMaxJanela` removido; janela = `[p0.x, p3.x] × [min − profundidade, topo]`; polígono antes do recorte desce até `min − profundidade − fundoExtra`.
  - `Terreno.Chunk`: + `List<Vector2> poligonoOriginal, poligonoRecortado`.
  - `IEnumerator Terreno.RepintarAnimado(Chunk c, float duracao, Action<float> linhaAtual = null)` — apaga a textura na hora da chamada e a repinta de baixo para cima em tempo real (`Time.unscaledTime`); `linhaAtual(y)` recebe a altura (mundo) da linha atual.
  - `Anim.Durante(float duracao, Action<float> f, bool tempoReal = false)`.

- [ ] **Step 1: testes**

Em `TerrenoComponenteTests.cs`, adicionar `using System.Collections;` no topo e, antes da última `}`:
```csharp
    [Test]
    public void Recorte_SempreCortaOFundo()
    {
        var c = terreno.GerarProximo();
        Assert.Less(c.poligonoOriginal.Min(p => p.y), c.janela.yMin - 1f, "polígono original não passa da janela");
        Assert.AreEqual(c.janela.yMin, c.poligonoRecortado.Min(p => p.y), 1e-3, "recortado não termina na borda da janela");
    }

    [Test]
    public void Gerador_UsaOsParametrosDoComponente()
    {
        terreno.amplitudeInicial = 0.5f;
        terreno.ganhoPorTrecho = 0.1f;
        terreno.amplitudeMax = 1f;
        terreno.Reiniciar();
        Assert.AreEqual(0.5f, terreno.Gerador.amplitudeInicial);
        Assert.AreEqual(0.1f, terreno.Gerador.ganhoPorTrecho);
        Assert.AreEqual(1f, terreno.Gerador.amplitudeMax);
    }

    static void Rodar(IEnumerator e) { while (e.MoveNext()) { } }

    [Test]
    public void RepintarAnimado_ApagaETerminaIgual()
    {
        terreno.GerarProximo();
        var c = terreno.GerarProximo();
        var original = c.tex.GetPixels32();
        int cheio = System.Array.FindIndex(original, p => p.a == 255);
        var e = terreno.RepintarAnimado(c, 0);
        Assert.AreEqual(0, c.tex.GetPixels32()[cheio].a, "não apagou antes de repintar");
        Rodar(e);
        CollectionAssert.AreEqual(original, c.tex.GetPixels32());
    }
```

- [ ] **Step 2: ver falhar**

Run: `Tools/rodar_testes.sh EditMode`
Expected: `exit=1`, `'Terreno.Chunk' does not contain a definition for 'poligonoOriginal'` (e `amplitudeInicial`, `RepintarAnimado`).

- [ ] **Step 3: implementar**

`CarroCG/Assets/Scripts/Anim.cs`:
```csharp
using System.Collections;
using UnityEngine;

public static class Anim
{
    // Chama f(k) a cada frame com k indo de 0 a 1 ao longo de 'duracao' segundos. Sempre termina em f(1).
    // tempoReal: conta o tempo mesmo com Time.timeScale = 0 (jogo congelado).
    public static IEnumerator Durante(float duracao, System.Action<float> f, bool tempoReal = false)
    {
        float Agora() => tempoReal ? Time.unscaledTime : Time.time;
        float t0 = Agora(), k;
        do
        {
            k = duracao <= 0 ? 1 : Mathf.Clamp01((Agora() - t0) / duracao);
            f(k);
            yield return null;
        } while (k < 1);
    }
}
```

Em `CarroCG/Assets/Scripts/Jogo/Terreno.cs`:

1. Em `Chunk`, depois de `public Rect janela;`:
```csharp
        public List<Vector2> poligonoOriginal, poligonoRecortado;   // antes e depois do Sutherland–Hodgman
```
2. Trocar
```csharp
    public float alturaMaxJanela = 32f;
```
por
```csharp
    public float fundoExtra = 40f;            // o polígono desce além da janela: o recorte sempre corta (modo aula)
    public float amplitudeInicial = 2.5f, ganhoPorTrecho = 0.5f, amplitudeMax = 12f;
```
3. Em `Reiniciar`, trocar `Gerador = new TerrenoGerador(semente);` por:
```csharp
        Gerador = new TerrenoGerador(semente)
            { amplitudeInicial = amplitudeInicial, ganhoPorTrecho = ganhoPorTrecho, amplitudeMax = amplitudeMax };
```
4. Em `GerarProximo`, trocar as linhas de `float fundo = ...` até `var px = ...` por:
```csharp
        float baseJanela = curva.Min(p => p.y) - profundidade;
        var janela = Rect.MinMaxRect(t.p0.x, baseJanela, t.p3.x, topo);

        var original = TerrenoChunk.Poligono(curva, baseJanela - fundoExtra);
        var poligono = Raster.ClipPolygon(original, janela);                                       // recorte
        var px = TerrenoChunk.Pintar(poligono, janela, ppu, curva, paleta, out int w, out int h);   // preenchimento
```
5. Trocar a criação do `Chunk` por:
```csharp
        var c = new Chunk
        {
            trecho = t, indice = indice, curva = curva, janela = janela, go = go, tex = tex,
            poligonoOriginal = original, poligonoRecortado = poligono,
        };
```
6. Adicionar, antes de `// Pontos vizinhos nas emendas`:
```csharp
    // Modo aula: apaga o trecho e o repinta de baixo para cima, uma linha do scanline por vez, em tempo real
    // (funciona com o jogo congelado). Os pixels vêm do mesmo preenchimento do trecho.
    public IEnumerator RepintarAnimado(Chunk c, float duracao, System.Action<float> linhaAtual = null)
    {
        var px = TerrenoChunk.Pintar(c.poligonoRecortado, c.janela, ppu, c.curva, paleta, out int w, out int h);
        c.tex.SetPixels32(new Color32[w * h]);
        c.tex.Apply();
        int feitas = 0;
        return Anim.Durante(duracao, k =>
        {
            int alvo = Mathf.CeilToInt(k * h);
            if (alvo > feitas)
            {
                var linhas = new Color32[(alvo - feitas) * w];
                System.Array.Copy(px, feitas * w, linhas, 0, linhas.Length);
                c.tex.SetPixels32(0, feitas, w, alvo - feitas, linhas);
                c.tex.Apply();
                feitas = alvo;
            }
            linhaAtual?.Invoke(c.janela.yMin + feitas / ppu);
        }, tempoReal: true);
    }

```

- [ ] **Step 4: ver passar**

Run: `Tools/rodar_testes.sh EditMode`
Expected: `exit=0`, `total="65" passed="65"`.

- [ ] **Step 5: commit**

```bash
git add CarroCG/Assets/Scripts CarroCG/Assets/Tests/Editor/TerrenoComponenteTests.cs
git commit -m "Terreno: parâmetros por modo, recorte que sempre corta e repintura animada por scanline"
```

---

### Task 3: Arte em camadas e céu lunar

**Files:**
- Modify: `Tools/gerar_f1.py` (reescrito)
- Create: `CarroCG/Assets/Art/F1/f1_atras.png`, `f1_pintura.png`, `f1_frente.png`; `CarroCG/Assets/Art/Ceu/estrelas.png`, `terra.png`
- Modify (regenerados): `CarroCG/Assets/Art/F1/f1_corpo.png`, `f1_roda.png`, `moeda.png`, `galao.png`

**Interfaces:**
- Produces: camadas 960×300 alinhadas pixel a pixel (mesmo pivô central). `f1_pintura.png` em cinza claro (base 215) com sombreado e contorno cinza escuro (95), para tingir por multiplicação. `f1_corpo.png` = atrás + pintura tingida de vermelho + frente (silhueta completa para o collider). `estrelas.png` 1024×512 (PPU 20), `terra.png` 256×256 (PPU 128). Layout do F1 inalterado: rodas (190,178)/(770,178) r=92, capacete (515,92).

- [ ] **Step 1: script**

`Tools/gerar_f1.py`:
```python
"""Gera a arte HD do F1 (vista lateral, frente para +x), da roda, dos coletáveis e do céu lunar.
Uso: python3 -I Tools/gerar_f1.py CarroCG/Assets/Art
Saídas em F1/: f1_atras.png, f1_pintura.png (cinza claro, tingida no Unity pela cor da garagem), f1_frente.png,
f1_corpo.png (silhueta completa em vermelho, usada no collider), f1_roda.png, moeda.png, galao.png.
Saídas em Ceu/: estrelas.png, terra.png.
Coordenadas do F1 em pixels de 960x300, y para baixo. PPU 256 no Unity.
Layout usado por Editor/MontagemComum.cs: rodas em (190,178) e (770,178), raio 92; capacete em (515,92)."""
import math, os, random, sys
from PIL import Image, ImageChops, ImageDraw, ImageFont

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

if __name__ == '__main__':
    raiz = sys.argv[1]
    f1, ceu = os.path.join(raiz, 'F1'), os.path.join(raiz, 'Ceu')
    os.makedirs(f1, exist_ok=True)
    os.makedirs(ceu, exist_ok=True)
    atras, pintura, frente = camada_atras(), camada_pintura(), camada_frente()
    saidas = [(f1, 'f1_atras', atras), (f1, 'f1_pintura', pintura), (f1, 'f1_frente', frente),
              (f1, 'f1_corpo', corpo(atras, pintura, frente)), (f1, 'f1_roda', roda()), (f1, 'moeda', moeda()),
              (f1, 'galao', galao()), (ceu, 'estrelas', estrelas()), (ceu, 'terra', terra())]
    for pasta, nome, img in saidas:
        caminho = os.path.join(pasta, nome + '.png')
        img.save(caminho)
        print(caminho)
```

- [ ] **Step 2: gerar e conferir**

```bash
python3 -I Tools/gerar_f1.py CarroCG/Assets/Art
python3 -I - <<'EOF'
from PIL import Image
base = 'CarroCG/Assets/Art/'
for f in ['F1/f1_atras', 'F1/f1_pintura', 'F1/f1_frente', 'F1/f1_corpo', 'Ceu/estrelas', 'Ceu/terra']:
    print(f, Image.open(base + f + '.png').size)
EOF
```
Expected: as 4 camadas do F1 com `(960, 300)`, `estrelas (1024, 512)`, `terra (256, 256)`.

Montar uma prévia com 3 tintas e conferir (Read):
```bash
python3 -I - <<'EOF'
from PIL import Image
b = 'CarroCG/Assets/Art/F1/'
at, pi, fr, ro = (Image.open(b + n + '.png') for n in ('f1_atras', 'f1_pintura', 'f1_frente', 'f1_roda'))
def tingir(img, c):
    r, g, bb, a = img.split()
    r, g, bb = (ch.point(lambda v, k=k: v * k // 255) for ch, k in zip((r, g, bb), c))
    return Image.merge('RGBA', (r, g, bb, a))
out = Image.new('RGBA', (1000, 1000), (140, 190, 235, 255))
for i, c in enumerate([(247, 31, 50), (31, 92, 242), (255, 217, 26)]):
    y = 20 + i * 320
    for camada in (at, tingir(pi, c), fr): out.alpha_composite(camada, (20, y))
    for cx in (190, 770): out.alpha_composite(ro, (20 + cx - 92, y + 178 - 92))
out.save('/tmp/claude-1000/-home-mtheusgc--rea-de-trabalho-Trabalhos-CG-TrabFinal/58a1911c-c053-4dc7-99fd-5310b6f5d4aa/scratchpad/garagem_previa.png')
EOF
```
Expected (Read na imagem): três F1 idênticos ao atual em forma, com chassi, cobertura e placas das asas em vermelho, azul e amarelo; asas pretas, faixa branca, número, "CG RACING", capacete amarelo, halo e rodas iguais nos três. Abrir `Ceu/terra.png` e `Ceu/estrelas.png`: planeta azul com continentes verdes e lado escuro; estrelas brancas sobre transparente.

- [ ] **Step 3: commit**

```bash
git add Tools/gerar_f1.py CarroCG/Assets/Art
git commit -m "Arte do F1 em camadas (pintura tingível) e céu lunar"
```

---

### Task 4: Componentes de configuração, pintura, piloto automático; partida com botões

**Files:**
- Create: `CarroCG/Assets/Scripts/Jogo/ConfigurarPartida.cs`, `PinturaF1.cs`, `PilotoAutomatico.cs`
- Modify: `CarroCG/Assets/Scripts/Jogo/F1.cs`, `CarroCG/Assets/Scripts/Jogo/Partida.cs`, `CarroCG/Assets/Scripts/CarroCG.asmdef`

**Interfaces:**
- Consumes: `Sessao`, `Preferencias`, `ModoJogo`, `Garagem` (Task 1); `Terreno` (Task 2).
- Produces:
  - `ConfigurarPartida` (`[DefaultExecutionOrder(-100)]`): `Terreno terreno; F1 f1; Camera cam; GameObject ceuLunar (pode ser null); bool forcarTerra`; `Modo ModoAtual`. No `Awake`: gravidade, parâmetros e paleta do terreno, `ppu` pela qualidade, `torqueNoAr`, cor do céu, céu lunar.
  - `PinturaF1`: `SpriteRenderer pintura`; no `Start`, `pintura.color = Garagem.CorEscolhida`.
  - `PilotoAutomatico`: `F1 f1; Terreno terreno; float anguloAlivio=25; float tempoParado=3`; `void Recomecar()` (volta o carro à largada e recria o terreno, sem recarregar a cena).
  - `F1`: padrões `torqueNoAr=16`, `centroDeMassa=(0, 0.05)`.
  - `Partida`: `enum Estado { Jogando, Pausa, Fim, Congelado }`; `void Congelar(bool)`; `void IrParaMenu()` (carrega "Menu"); moedas só contam jogando; `Terminar` desliga o controle (`controlePeloTeclado=false`, `acelerador=0`); ao abrir a pausa ou o fim seleciona o primeiro botão do painel; R e Esc ignorados quando congelado.

- [ ] **Step 1: implementar**

`CarroCG/Assets/Scripts/CarroCG.asmdef`:
```json
{
    "name": "CarroCG",
    "references": ["Unity.TextMeshPro", "UnityEngine.UI"]
}
```

`CarroCG/Assets/Scripts/Jogo/ConfigurarPartida.cs`:
```csharp
using UnityEngine;

// Aplica o modo (Terra/Lua) e a qualidade antes de qualquer outro componente da cena.
[DefaultExecutionOrder(-100)]
public class ConfigurarPartida : MonoBehaviour
{
    public Terreno terreno;
    public F1 f1;
    public Camera cam;
    public GameObject ceuLunar;
    public bool forcarTerra;            // fundo do menu: sempre Terra

    public Modo ModoAtual { get; private set; }

    void Awake()
    {
        ModoAtual = forcarTerra ? Modo.Terra : Sessao.Modo;
        var p = ModoJogo.Parametros(ModoAtual);
        Physics2D.gravity = new Vector2(0, p.gravidade);      // global: definido sempre, nas duas cenas
        terreno.semente = p.semente;
        terreno.amplitudeInicial = p.amplitudeInicial;
        terreno.ganhoPorTrecho = p.ganhoPorTrecho;
        terreno.amplitudeMax = p.amplitudeMax;
        terreno.paleta = p.paleta;
        terreno.ppu = Preferencias.PpuTerreno[Preferencias.Qualidade];
        f1.torqueNoAr = p.torqueNoAr;
        cam.backgroundColor = p.ceu;
        if (ceuLunar) ceuLunar.SetActive(p.ceuLunar);
    }
}
```

`CarroCG/Assets/Scripts/Jogo/PinturaF1.cs`:
```csharp
using UnityEngine;

// Tinge só a camada de pintura do F1 com a cor da garagem; detalhes e rodas ficam como estão.
public class PinturaF1 : MonoBehaviour
{
    public SpriteRenderer pintura;

    void Start() => pintura.color = Garagem.CorEscolhida;
}
```

`CarroCG/Assets/Scripts/Jogo/PilotoAutomatico.cs`:
```csharp
using UnityEngine;

// Dirige o F1 do fundo do menu. Acelera; alivia quando empina; se capotar ou parar, recomeça em silêncio.
public class PilotoAutomatico : MonoBehaviour
{
    public F1 f1;
    public Terreno terreno;
    public float anguloAlivio = 25f;
    public float tempoParado = 3f;

    Rigidbody2D[] corpos;
    Vector2[] posicoes;
    float[] rotacoes;
    float parado;

    void Start()
    {
        f1.controlePeloTeclado = false;
        f1.Capotou += Recomecar;
        corpos = new[] { f1.GetComponent<Rigidbody2D>(), f1.eixoTraseiro.connectedBody, f1.eixoDianteiro.connectedBody };
        posicoes = System.Array.ConvertAll(corpos, c => c.position);
        rotacoes = System.Array.ConvertAll(corpos, c => c.rotation);
    }

    void OnDestroy()
    {
        if (f1) f1.Capotou -= Recomecar;
    }

    void Update()
    {
        float inclinacao = Mathf.DeltaAngle(0, f1.transform.eulerAngles.z);   // + = bico para cima
        f1.acelerador = inclinacao > anguloAlivio ? (f1.NoChao ? 0 : -1) : 1;
        parado = Mathf.Abs(f1.VelocidadeX) < 0.3f ? parado + Time.deltaTime : 0;
        if (parado > tempoParado) Recomecar();
    }

    public void Recomecar()
    {
        parado = 0;
        terreno.Reiniciar();
        for (int i = 0; i < corpos.Length; i++)
        {
            corpos[i].position = posicoes[i];
            corpos[i].rotation = rotacoes[i];
            corpos[i].transform.SetPositionAndRotation(posicoes[i], Quaternion.Euler(0, 0, rotacoes[i]));
            corpos[i].linearVelocity = Vector2.zero;
            corpos[i].angularVelocity = 0;
        }
    }
}
```

Em `CarroCG/Assets/Scripts/Jogo/F1.cs`, trocar:
```csharp
    public float torqueNoAr = 8f;
    public Vector2 centroDeMassa = new(0, -0.15f);
```
por:
```csharp
    public float torqueNoAr = 16f;                 // gira > 90° em ~1 s no ar (ConfigurarPartida usa o do modo)
    public Vector2 centroDeMassa = new(0, 0.05f);  // menos baixo: dá para empinar e capotar
```

`CarroCG/Assets/Scripts/Jogo/Partida.cs` (arquivo inteiro):
```csharp
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Estados do jogo, combustível, contagem, HUD, recorde por modo e botões de pausa/fim.
public class Partida : MonoBehaviour
{
    public enum Estado { Jogando, Pausa, Fim, Congelado }

    public F1 f1;
    public Combustivel combustivel = new();
    public float galaoQuantidade = 45f;

    [Header("HUD")]
    public RectTransform barraCombustivel;
    public TMP_Text textoMoedas, textoDistancia, textoRecorde, textoFim;
    public GameObject painelPausa, painelFim;

    public Estado EstadoAtual { get; private set; }
    public int Moedas { get; private set; }
    public float Distancia { get; private set; }

    float x0;

    void OnEnable()
    {
        Coletavel.Coletado += AoColetar;
        f1.Capotou += AoCapotar;
    }

    void OnDisable()
    {
        Coletavel.Coletado -= AoColetar;
        f1.Capotou -= AoCapotar;
        Time.timeScale = 1;
    }

    void Start()
    {
        Time.timeScale = 1;
        combustivel.Encher();
        x0 = f1.transform.position.x;
        painelPausa.SetActive(false);
        painelFim.SetActive(false);
        AtualizarHud();
    }

    void Update()
    {
        if (EstadoAtual == Estado.Congelado) return;
        if (Input.GetKeyDown(KeyCode.R)) { Reiniciar(); return; }
        if (Input.GetKeyDown(KeyCode.Escape) && EstadoAtual != Estado.Fim) AlternarPausa();
        if (EstadoAtual != Estado.Jogando) return;

        combustivel.Consumir(Time.deltaTime, f1.motorLigado ? f1.acelerador : 0);
        f1.motorLigado = !combustivel.Vazio;
        Distancia = Mathf.Max(Distancia, f1.transform.position.x - x0);
        if (combustivel.VerificarPane(Time.deltaTime, f1.VelocidadeX)) Terminar("Sem combustível");
        AtualizarHud();
    }

    public void Reiniciar() => SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);

    public void IrParaMenu()
    {
        Time.timeScale = 1;
        SceneManager.LoadScene("Menu");
    }

    public void AlternarPausa()
    {
        if (EstadoAtual != Estado.Jogando && EstadoAtual != Estado.Pausa) return;
        bool pausar = EstadoAtual == Estado.Jogando;
        EstadoAtual = pausar ? Estado.Pausa : Estado.Jogando;
        Time.timeScale = pausar ? 0 : 1;
        painelPausa.SetActive(pausar);
        if (pausar) Selecionar(painelPausa);
    }

    // Modo aula: segura o jogo parado (sem painel) enquanto a repintura por scanline acontece.
    public void Congelar(bool sim)
    {
        if (sim && EstadoAtual != Estado.Jogando) return;
        if (!sim && EstadoAtual != Estado.Congelado) return;
        EstadoAtual = sim ? Estado.Congelado : Estado.Jogando;
        Time.timeScale = sim ? 0 : 1;
    }

    void AoColetar(TipoColetavel tipo)
    {
        if (EstadoAtual != Estado.Jogando) return;
        if (tipo == TipoColetavel.Moeda) Moedas++;
        else combustivel.Abastecer(galaoQuantidade);
    }

    void AoCapotar()
    {
        if (EstadoAtual == Estado.Jogando) Terminar("Capotou!");
    }

    public void Terminar(string motivo)
    {
        EstadoAtual = Estado.Fim;
        f1.motorLigado = false;
        f1.controlePeloTeclado = false;     // sem motor e sem giro no ar depois do fim
        f1.acelerador = 0;
        bool novo = Preferencias.RegistrarPartida(Sessao.Modo, Distancia, Moedas);
        float recorde = Preferencias.Recorde(Sessao.Modo);
        textoFim.text = $"{motivo}\n<size=60%>{Distancia:0} m  ·  {Moedas} moedas\n" +
                        (novo ? "Novo recorde!" : $"Recorde: {recorde:0} m") + "</size>";
        painelFim.SetActive(true);
        Selecionar(painelFim);
        AtualizarHud();
    }

    static void Selecionar(GameObject painel)
    {
        var botao = painel.GetComponentInChildren<Button>();
        if (botao && EventSystem.current) EventSystem.current.SetSelectedGameObject(botao.gameObject);
    }

    void AtualizarHud()
    {
        var a = barraCombustivel.anchorMax;
        a.x = combustivel.Fracao;
        barraCombustivel.anchorMax = a;
        textoMoedas.text = $"Moedas  {Moedas}";
        textoDistancia.text = $"{Distancia:0} m";
        textoRecorde.text = $"Recorde {Preferencias.Recorde(Sessao.Modo):0} m";
    }
}
```

- [ ] **Step 2: compilar e rodar**

Run: `Tools/rodar_testes.sh EditMode` → Expected: `exit=0`, `total="65" passed="65"`.
Run: `Tools/rodar_testes.sh PlayMode` → Expected: `exit=0`, `total="8" passed="7"` (a cena atual ainda é a antiga; nada quebra).

- [ ] **Step 3: commit**

```bash
git add CarroCG/Assets/Scripts
git commit -m "Configuração por modo, pintura do F1, piloto automático e partida com botões"
```

---

### Task 5: Geometria e componente do modo aula

**Files:**
- Create: `CarroCG/Assets/Scripts/Jogo/AulaGeometria.cs`, `CarroCG/Assets/Scripts/Jogo/ModoAula.cs`
- Test: `CarroCG/Assets/Tests/Editor/AulaGeometriaTests.cs`

**Interfaces:**
- Consumes: `Terreno.Chunk` (+ polígonos), `Terreno.TrechoCriado`, `Terreno.RepintarAnimado` (Task 2); `Partida.Congelar`, `Partida.EstadoAtual` (Task 4); `Preferencias.ModoAula`.
- Produces:
  - `static class AulaGeometria`: `Vector3[] Controle(Trecho)` (P0..P3); `Vector3[] Retangulo(Rect)` (5 pontos, fechado); `Vector3[] Fechado(IReadOnlyList<Vector2>)` (repete o 1º; vazio → vazio); `Vector3[] Emenda(Trecho anterior, Trecho t)` (P2 anterior, junção, P1); `Vector3[] Quadrado(Vector2 centro, float lado)` (5 pontos).
  - `ModoAula : MonoBehaviour`: `Terreno terreno; F1 f1; Partida partida; Material materialLinha; TMP_Text rotulo; float duracaoScanline=2`; `bool Ativo`; `bool Repintando`; `void DefinirAtivo(bool)`; `void RepintarScanline()`. Tab alterna; F repinta (só ativo). Cada trecho ganha um filho `"Aula"` com as linhas; o F1 ganha `"Aula: eixos"`; o rótulo só aparece ativo.

- [ ] **Step 1: testes**

`CarroCG/Assets/Tests/Editor/AulaGeometriaTests.cs`:
```csharp
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class AulaGeometriaTests
{
    static Trecho T(Vector2 a, Vector2 b, Vector2 c, Vector2 d) => new Trecho { p0 = a, p1 = b, p2 = c, p3 = d };

    [Test]
    public void Controle_OsQuatroPontos()
    {
        var t = T(new(0, 0), new(1, 2), new(3, 1), new(4, 0));
        CollectionAssert.AreEqual(new Vector3[] { new(0, 0), new(1, 2), new(3, 1), new(4, 0) }, AulaGeometria.Controle(t));
    }

    [Test]
    public void Retangulo_FechadoNosCantos()
    {
        CollectionAssert.AreEqual(
            new Vector3[] { new(1, 2), new(4, 2), new(4, 7), new(1, 7), new(1, 2) },
            AulaGeometria.Retangulo(Rect.MinMaxRect(1, 2, 4, 7)));
    }

    [Test]
    public void Fechado_RepeteOPrimeiroEVazioFicaVazio()
    {
        var p = new List<Vector2> { new(0, 0), new(1, 0), new(0, 1) };
        CollectionAssert.AreEqual(new Vector3[] { new(0, 0), new(1, 0), new(0, 1), new(0, 0) }, AulaGeometria.Fechado(p));
        Assert.IsEmpty(AulaGeometria.Fechado(new List<Vector2>()));
    }

    [Test]
    public void Emenda_JuncaoNoMeioDoEspelhamento()
    {
        var g = new TerrenoGerador(3);
        g.Proximo();
        var a = g.Proximo();
        var c = g.Proximo();
        var e = AulaGeometria.Emenda(a, c);
        Assert.AreEqual(3, e.Length);
        Assert.AreEqual((Vector3)a.p2, e[0]);
        Assert.AreEqual((Vector3)c.p1, e[2]);
        Assert.Less(Vector3.Distance((e[0] + e[2]) / 2, e[1]), 1e-4f, "P1 não é o espelho de P2 em torno da junção");
    }

    [Test]
    public void Quadrado_CentradoEFechado()
    {
        var q = AulaGeometria.Quadrado(new Vector2(2, 3), 2);
        Assert.AreEqual(5, q.Length);
        Assert.AreEqual(q[0], q[4]);
        Assert.AreEqual(new Vector3(1, 2), q[0]);
        Assert.AreEqual(new Vector3(3, 4), q[2]);
    }
}
```

- [ ] **Step 2: ver falhar**

Run: `Tools/rodar_testes.sh EditMode`
Expected: `exit=1`, `The name 'AulaGeometria' does not exist`.

- [ ] **Step 3: implementar**

`CarroCG/Assets/Scripts/Jogo/AulaGeometria.cs`:
```csharp
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Linhas do modo aula, calculadas a partir dos dados que o terreno já tem (nada é reimplementado aqui).
public static class AulaGeometria
{
    public static Vector3[] Controle(Trecho t) => new Vector3[] { t.p0, t.p1, t.p2, t.p3 };

    public static Vector3[] Retangulo(Rect r) => new Vector3[]
    {
        new(r.xMin, r.yMin), new(r.xMax, r.yMin), new(r.xMax, r.yMax), new(r.xMin, r.yMax), new(r.xMin, r.yMin),
    };

    public static Vector3[] Fechado(IReadOnlyList<Vector2> p) =>
        p.Count == 0 ? new Vector3[0] : p.Select(v => (Vector3)v).Append(p[0]).ToArray();

    // P2 do trecho anterior, junção e P1 do próximo: colineares, junção no meio (P1 = 2·P3 - P2).
    public static Vector3[] Emenda(Trecho anterior, Trecho t) => new Vector3[] { anterior.p2, t.p0, t.p1 };

    public static Vector3[] Quadrado(Vector2 c, float lado)
    {
        float m = lado / 2;
        return new Vector3[] { new(c.x - m, c.y - m), new(c.x + m, c.y - m), new(c.x + m, c.y + m), new(c.x - m, c.y + m), new(c.x - m, c.y - m) };
    }
}
```

`CarroCG/Assets/Scripts/Jogo/ModoAula.cs`:
```csharp
using System.Collections;
using TMPro;
using UnityEngine;

// Sobreposição didática (Tab): Bézier, emenda espelhada, recorte, eixos e velocidade do F1.
// F: congela o jogo e repinta por scanline o trecho no centro da tela.
public class ModoAula : MonoBehaviour
{
    public Terreno terreno;
    public F1 f1;
    public Partida partida;
    public Material materialLinha;
    public TMP_Text rotulo;
    public float duracaoScanline = 2f;

    public bool Ativo { get; private set; }
    public bool Repintando { get; private set; }

    static readonly Color Branco = Color.white, Amarelo = new(1f, 0.85f, 0.1f), Vermelho = new(1f, 0.25f, 0.25f),
                          Verde = new(0.3f, 1f, 0.4f), Ciano = new(0.2f, 0.9f, 1f);

    const string Texto =
        "MODO AULA\n" +
        "<size=80%>· Curva de Bézier: pontos e polígono de controle (branco)\n" +
        "· Espelhamento na emenda: P1 = 2·P3 - P2 (ciano)\n" +
        "· Recorte Sutherland-Hodgman: janela (amarelo), antes (vermelho), depois (verde)\n" +
        "· Translação e rotação: eixos x/y e velocidade do F1\n" +
        "F: preenchimento scanline em câmera lenta</size>";

    GameObject eixos;
    LineRenderer velocidade, scanline;
    Rigidbody2D rb;

    void OnEnable() => terreno.TrechoCriado += Desenhar;
    void OnDisable() => terreno.TrechoCriado -= Desenhar;

    void Start()
    {
        rb = f1.GetComponent<Rigidbody2D>();
        eixos = new GameObject("Aula: eixos");
        eixos.transform.SetParent(f1.transform, false);
        Linha(eixos.transform, "x", false, 0.05f, Vermelho, new Vector3[] { Vector3.zero, Vector3.right * 1.2f });
        Linha(eixos.transform, "y", false, 0.05f, Verde, new Vector3[] { Vector3.zero, Vector3.up * 0.9f });
        velocidade = Linha(transform, "Aula: velocidade", true, 0.05f, Amarelo, new Vector3[2]);
        scanline = Linha(transform, "Aula: scanline", true, 0.08f, Ciano, new Vector3[2]);
        scanline.enabled = false;
        rotulo.text = Texto;
        foreach (var c in terreno.chunks) Desenhar(c);
        DefinirAtivo(Preferencias.ModoAula);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab)) DefinirAtivo(!Ativo);
        if (Ativo && Input.GetKeyDown(KeyCode.F)) RepintarScanline();
        if (Ativo)
        {
            velocidade.SetPosition(0, rb.position);
            velocidade.SetPosition(1, rb.position + rb.linearVelocity * 0.3f);
        }
    }

    public void DefinirAtivo(bool sim)
    {
        Ativo = sim;
        eixos.SetActive(sim);
        velocidade.enabled = sim;
        rotulo.transform.parent.gameObject.SetActive(sim);
        foreach (var c in terreno.chunks)
        {
            var aula = c.go.transform.Find("Aula");
            if (aula) aula.gameObject.SetActive(sim);
        }
    }

    public void RepintarScanline()
    {
        if (!Ativo || Repintando || partida.EstadoAtual != Partida.Estado.Jogando) return;
        float x = Camera.main.transform.position.x;
        var c = terreno.chunks.Find(k => k.janela.xMin <= x && x < k.janela.xMax);
        if (c != null) StartCoroutine(Repintar(c));
    }

    IEnumerator Repintar(Terreno.Chunk c)
    {
        Repintando = true;
        partida.Congelar(true);
        scanline.enabled = true;
        yield return terreno.RepintarAnimado(c, duracaoScanline, y =>
        {
            scanline.SetPosition(0, new Vector3(c.janela.xMin, y));
            scanline.SetPosition(1, new Vector3(c.janela.xMax, y));
        });
        scanline.enabled = false;
        partida.Congelar(false);
        Repintando = false;
    }

    void Desenhar(Terreno.Chunk c)
    {
        var raiz = new GameObject("Aula").transform;
        raiz.SetParent(c.go.transform, false);          // some junto com o trecho
        Linha(raiz, "controle", true, 0.04f, Branco, AulaGeometria.Controle(c.trecho));
        foreach (var p in AulaGeometria.Controle(c.trecho))
            Linha(raiz, "ponto", true, 0.05f, Branco, AulaGeometria.Quadrado(p, 0.3f));
        Linha(raiz, "janela", true, 0.05f, Amarelo, AulaGeometria.Retangulo(c.janela));
        Linha(raiz, "antes do recorte", true, 0.04f, Vermelho, AulaGeometria.Fechado(c.poligonoOriginal));
        Linha(raiz, "recortado", true, 0.04f, Verde, AulaGeometria.Fechado(c.poligonoRecortado));
        int i = terreno.chunks.IndexOf(c);
        if (i > 0) Linha(raiz, "emenda", true, 0.08f, Ciano, AulaGeometria.Emenda(terreno.chunks[i - 1].trecho, c.trecho));
        raiz.gameObject.SetActive(Ativo);
    }

    LineRenderer Linha(Transform pai, string nome, bool mundo, float largura, Color cor, Vector3[] pontos)
    {
        var go = new GameObject(nome);
        go.transform.SetParent(pai, false);
        var lr = go.AddComponent<LineRenderer>();
        lr.sharedMaterial = materialLinha;
        lr.useWorldSpace = mundo;
        lr.widthMultiplier = largura;
        lr.startColor = lr.endColor = cor;
        lr.sortingOrder = 50;                         // acima do carro, abaixo do HUD (100)
        lr.positionCount = pontos.Length;
        lr.SetPositions(pontos);
        return lr;
    }
}
```

- [ ] **Step 4: ver passar**

Run: `Tools/rodar_testes.sh EditMode`
Expected: `exit=0`, `total="70" passed="70"`.

- [ ] **Step 5: commit**

```bash
git add CarroCG/Assets/Scripts/Jogo/AulaGeometria.cs* CarroCG/Assets/Scripts/Jogo/ModoAula.cs* CarroCG/Assets/Tests/Editor/AulaGeometriaTests.cs*
git commit -m "Modo aula: geometria das linhas e componente com repintura por scanline"
```

---

### Task 6: Técnicas de CG e componente do menu

**Files:**
- Create: `CarroCG/Assets/Scripts/Jogo/TecnicasCG.cs`, `CarroCG/Assets/Scripts/Jogo/Menu.cs`
- Test: `CarroCG/Assets/Tests/Editor/TecnicasCGTests.cs`

**Interfaces:**
- Consumes: `Preferencias`, `Sessao`, `Garagem` (Task 1).
- Produces:
  - `static class TecnicasCG`: `struct Lugar { string onde; bool noJogo; }`, `struct Tecnica { string nome; Lugar[] lugares; }`, `Tecnica[] Lista` (9, na ordem da spec principal), `string Texto()` (rich text TMP).
  - `Menu : MonoBehaviour`: painéis `principal, escolhaModo, garagem, controles, configuracoes, tecnicas, recorde`; textos `textoTecnicas, textoRecorde, textoZerar, textoTelaCheia, textoModoAula, textoQualidade, textoCor`; `Image previaPintura`; `RectTransform previaCarro`; métodos públicos `AbrirPrincipal, AbrirEscolhaModo, AbrirGaragem, AbrirControles, AbrirConfiguracoes, AbrirTecnicas, AbrirRecorde, Jogar(int modo), EscolherCor(int), AlternarTelaCheia, AlternarModoAula, ProximaQualidade, ZerarRecordes, Sair`; `GameObject TelaAtual`. Esc volta à principal; cada tela aberta seleciona seu primeiro botão.

- [ ] **Step 1: testes**

`CarroCG/Assets/Tests/Editor/TecnicasCGTests.cs`:
```csharp
using System.Linq;
using NUnit.Framework;

public class TecnicasCGTests
{
    [Test]
    public void NoveTecnicasDaSpec()
    {
        CollectionAssert.AreEqual(new[]
        {
            "Projeção paralela e perspectiva", "Rasterização", "Preenchimento", "Curva de Bézier", "Recorte",
            "Iluminação e sombreamento", "Translação", "Rotação", "Espelhamento",
        }, TecnicasCG.Lista.Select(t => t.nome));
    }

    [Test]
    public void SelosConformeOQueOJogoTemHoje()
    {
        var comAlgoNoJogo = TecnicasCG.Lista.Where(t => t.lugares.Any(l => l.noJogo)).Select(t => t.nome);
        CollectionAssert.AreEquivalent(
            new[] { "Preenchimento", "Curva de Bézier", "Recorte", "Translação", "Rotação", "Espelhamento" },
            comAlgoNoJogo);
    }

    [Test]
    public void CadaTecnicaTemOndeAparece() =>
        Assert.IsTrue(TecnicasCG.Lista.All(t => t.lugares.Length > 0));

    [Test]
    public void Texto_TemTodosOsNomesESelos()
    {
        var texto = TecnicasCG.Texto();
        foreach (var t in TecnicasCG.Lista) StringAssert.Contains(t.nome, texto);
        StringAssert.Contains("no jogo", texto);
        StringAssert.Contains("em breve", texto);
    }
}
```

- [ ] **Step 2: ver falhar**

Run: `Tools/rodar_testes.sh EditMode`
Expected: `exit=1`, `The name 'TecnicasCG' does not exist`.

- [ ] **Step 3: implementar**

`CarroCG/Assets/Scripts/Jogo/TecnicasCG.cs`:
```csharp
using System.Linq;

// Conteúdo da tela "Técnicas de CG". O selo de cada lugar é a fonte da verdade: muda quando a etapa entrega.
public static class TecnicasCG
{
    public struct Lugar
    {
        public string onde;
        public bool noJogo;
    }

    public struct Tecnica
    {
        public string nome;
        public Lugar[] lugares;
    }

    static Lugar Sim(string onde) => new Lugar { onde = onde, noJogo = true };
    static Lugar Breve(string onde) => new Lugar { onde = onde, noJogo = false };
    static Tecnica T(string nome, params Lugar[] lugares) => new Tecnica { nome = nome, lugares = lugares };

    public static readonly Tecnica[] Lista =
    {
        T("Projeção paralela e perspectiva", Breve("F1 3D em wireframe na intro"), Breve("parallax do fundo (tecla P)")),
        T("Rasterização", Breve("Bresenham: wireframe e contorno do F1 na intro"), Breve("Bresenham: minimapa")),
        T("Preenchimento", Sim("scanline do terreno (tecla F no modo aula)"), Breve("regiões do F1 na intro"), Breve("sombra do F1")),
        T("Curva de Bézier", Sim("terreno infinito de Bézier cúbicas")),
        T("Recorte", Sim("Sutherland-Hodgman: terreno contra a janela do trecho"),
                     Breve("Sutherland-Hodgman: reflexo no lago"), Breve("Cohen-Sutherland: minimapa")),
        T("Iluminação e sombreamento", Breve("Lambert Flat/Gouraud/Phong no terreno"), Breve("shader Phong do F1"),
                                       Breve("sombra projetada do F1")),
        T("Translação", Sim("movimento do carro"), Sim("moedas e galões sobre a curva"), Breve("parallax")),
        T("Rotação", Sim("carro inclinando e rodas girando"), Sim("giro no ar"), Breve("wireframe 3D da intro")),
        T("Espelhamento", Sim("emenda C1 do terreno (P1 = 2·P3 - P2)"), Sim("vitrine da garagem"), Breve("reflexo no lago")),
    };

    public static string Texto() => string.Join("\n", Lista.Select(t =>
        $"<b>{t.nome}</b>\n" + string.Join("\n", t.lugares.Select(l =>
            $"    {(l.noJogo ? "<color=#5BE36B>no jogo</color>" : "<color=#9AA0A6>em breve</color>")}   {l.onde}"))));
}
```

`CarroCG/Assets/Scripts/Jogo/Menu.cs`:
```csharp
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Telas do menu principal. Todas as ações são públicas e ligadas aos botões por MontarMenu.
public class Menu : MonoBehaviour
{
    public GameObject principal, escolhaModo, garagem, controles, configuracoes, tecnicas, recorde;
    public TMP_Text textoTecnicas, textoRecorde, textoZerar, textoTelaCheia, textoModoAula, textoQualidade, textoCor;
    public Image previaPintura;
    public RectTransform previaCarro;
    public float velocidadeVitrine = 1.2f;

    public GameObject TelaAtual { get; private set; }
    bool confirmarZerar;

    GameObject[] Telas => new[] { principal, escolhaModo, garagem, controles, configuracoes, tecnicas, recorde };

    void Start()
    {
        Time.timeScale = 1;
        Screen.fullScreen = Preferencias.TelaCheia;
        textoTecnicas.text = TecnicasCG.Texto();
        Abrir(principal);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) && TelaAtual != principal) Abrir(principal);
        if (TelaAtual == garagem)
        {
            var s = previaCarro.localScale;      // vitrine: escala x de +1 a -1 (espelhamento ao passar pelo meio)
            s.x = Mathf.Cos(Time.time * velocidadeVitrine);
            previaCarro.localScale = s;
        }
    }

    void Abrir(GameObject tela)
    {
        foreach (var t in Telas) t.SetActive(t == tela);
        TelaAtual = tela;
        confirmarZerar = false;
        AtualizarTextos();
        var botao = tela.GetComponentInChildren<Button>();
        if (botao && EventSystem.current) EventSystem.current.SetSelectedGameObject(botao.gameObject);
    }

    public void AbrirPrincipal() => Abrir(principal);
    public void AbrirEscolhaModo() => Abrir(escolhaModo);
    public void AbrirGaragem() => Abrir(garagem);
    public void AbrirControles() => Abrir(controles);
    public void AbrirConfiguracoes() => Abrir(configuracoes);
    public void AbrirTecnicas() => Abrir(tecnicas);
    public void AbrirRecorde() => Abrir(recorde);

    public void Jogar(int modo)
    {
        Sessao.Modo = (Modo)modo;
        SceneManager.LoadScene("Jogo");
    }

    public void EscolherCor(int indice)
    {
        Preferencias.Cor = indice;
        AtualizarTextos();
    }

    public void AlternarTelaCheia()
    {
        Preferencias.TelaCheia = !Preferencias.TelaCheia;
        Screen.fullScreen = Preferencias.TelaCheia;
        AtualizarTextos();
    }

    public void AlternarModoAula()
    {
        Preferencias.ModoAula = !Preferencias.ModoAula;
        AtualizarTextos();
    }

    public void ProximaQualidade()
    {
        Preferencias.Qualidade = (Preferencias.Qualidade + 1) % Preferencias.PpuTerreno.Length;
        AtualizarTextos();
    }

    // Duas etapas: o 1º clique pede confirmação, o 2º zera.
    public void ZerarRecordes()
    {
        if (confirmarZerar) Preferencias.ZerarRecordes();
        confirmarZerar = !confirmarZerar;
        AtualizarTextos();
    }

    public void Sair()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    void AtualizarTextos()
    {
        textoTelaCheia.text = $"Tela cheia: {(Preferencias.TelaCheia ? "sim" : "não")}";
        textoModoAula.text = $"Modo aula ao iniciar: {(Preferencias.ModoAula ? "sim" : "não")}";
        textoQualidade.text = $"Qualidade: {Preferencias.NomesQualidade[Preferencias.Qualidade]}";
        textoCor.text = Garagem.Nomes[Garagem.IndiceEscolhido];
        previaPintura.color = Garagem.CorEscolhida;
        textoRecorde.text = $"<b>Terra</b>\n{Preferencias.Recorde(Modo.Terra):0} m  ·  {Preferencias.MoedasDoRecorde(Modo.Terra)} moedas\n\n" +
                            $"<b>Lua</b>\n{Preferencias.Recorde(Modo.Lua):0} m  ·  {Preferencias.MoedasDoRecorde(Modo.Lua)} moedas";
        textoZerar.text = confirmarZerar ? "Confirmar? Clique de novo" : "Zerar recordes";
    }
}
```

- [ ] **Step 4: ver passar**

Run: `Tools/rodar_testes.sh EditMode`
Expected: `exit=0`, `total="74" passed="74"`.

- [ ] **Step 5: commit**

```bash
git add CarroCG/Assets/Scripts/Jogo/TecnicasCG.cs* CarroCG/Assets/Scripts/Jogo/Menu.cs* CarroCG/Assets/Tests/Editor/TecnicasCGTests.cs*
git commit -m "Tela de Técnicas de CG e componente do menu"
```

---

### Task 7: Peças comuns + cena do jogo remontada (Lua, modo aula, botões) + testes

**Files:**
- Create: `CarroCG/Assets/Editor/MontagemComum.cs`
- Modify: `CarroCG/Assets/Editor/MontarJogo.cs` (reescrito), `CarroCG/Assets/Tests/PlayMode/JogoTests.cs`, `CarroCG/Assets/Tests/PlayMode/CarroCG.PlayTests.asmdef`
- Create: `CarroCG/Assets/Tests/PlayMode/Fotografo.cs`
- Modify (gerado): `CarroCG/Assets/Scenes/Jogo.unity`

**Interfaces:**
- Consumes: tudo das Tasks 1–5; layout da arte (Task 3).
- Produces:
  - `MontagemComum` (Editor): `ConfigurarArte()`, `Material2D(nome, atrito)`, `MaterialUnlit`, `LuzGlobal()`, `PecasF1 CriarF1(PhysicsMaterial2D pneu)` (`PecasF1 { GameObject raiz, chassi; F1 f1; SpriteRenderer pintura; }`), `Camera CriarCamera(Rigidbody2D alvo, float zoomMin, float zoomMax)`, `GameObject CeuLunar(Camera)`, `Terreno CriarTerreno(Transform alvo, PhysicsMaterial2D chao)`, `Canvas CriarCanvas(Camera)`, `CriarEventSystem()`, `RectTransform Caixa(...)`, `TMP_Text Texto(...)`, `RectTransform Painel(nome, pai)`, `RectTransform Coluna(pai, ancora, largura)`, `(Button, TMP_Text) Botao(pai, texto, UnityAction)`, `(Button, TMP_Text) BotaoInt(pai, texto, UnityAction<int>, int)`, `void DefinirBuild()`.
  - `Fotografo.Foto(string caminho)` (PlayMode tests): render da `Camera.main` com aquecimento.
  - Cena `Jogo.unity` com `ConfigurarPartida`, `ModoAula`, `EventSystem`, céu lunar, botões de pausa (Continuar, Reiniciar, Menu principal) e fim (Jogar de novo, Menu).

- [ ] **Step 1: testes PlayMode do jogo**

`CarroCG/Assets/Tests/PlayMode/CarroCG.PlayTests.asmdef`:
```json
{
    "name": "CarroCG.PlayTests",
    "references": ["CarroCG", "UnityEngine.TestRunner", "Unity.TextMeshPro", "UnityEngine.UI"],
    "overrideReferences": true,
    "precompiledReferences": ["nunit.framework.dll"],
    "autoReferenced": false,
    "defineConstraints": ["UNITY_INCLUDE_TESTS"]
}
```

`CarroCG/Assets/Tests/PlayMode/Fotografo.cs`:
```csharp
using UnityEngine;

// Render da câmera principal em PNG (capturas de verificação). Precisa de GPU.
public static class Fotografo
{
    public static string Pasta => System.Environment.GetEnvironmentVariable("CAPTURA_DIR");

    public static void Foto(string nome)
    {
        var cam = Camera.main;
        var rt = new RenderTexture(1920, 1080, 24);
        cam.targetTexture = rt;
        Canvas.ForceUpdateCanvases();
        cam.Render();   // aquecimento: o 1º render após criar texturas ainda usa as anteriores
        cam.Render();
        RenderTexture.active = rt;
        var tex = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0);
        tex.Apply();
        System.IO.File.WriteAllBytes(System.IO.Path.Combine(Pasta, nome), tex.EncodeToPNG());
        cam.targetTexture = null;
        RenderTexture.active = null;
        Object.Destroy(rt);
        Object.Destroy(tex);
    }
}
```

Em `CarroCG/Assets/Tests/PlayMode/JogoTests.cs`:
1. Adicionar `using TMPro;` e `using UnityEngine.UI;` no topo.
2. Remover o teste `Capturar` e o método `Foto` (vão para `CapturasTests`, Task 8).
3. Adicionar, antes da última `}`:
```csharp
    static Button BotaoComTexto(string texto) =>
        Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None)
              .First(b => b.GetComponentInChildren<TMP_Text>(true).text == texto);

    [UnityTest]
    public IEnumerator NoAr_GiraMaisDe120GrausEmUmSegundo()
    {
        yield return Carregar();
        foreach (var corpo in f1.transform.parent.GetComponentsInChildren<Rigidbody2D>())
            corpo.position += Vector2.up * 25;
        yield return new WaitForFixedUpdate();
        var rb = f1.GetComponent<Rigidbody2D>();
        float r0 = rb.rotation;
        f1.acelerador = 1;
        yield return new WaitForSeconds(1);
        Assert.IsFalse(f1.NoChao, "tocou o chão durante o teste");
        Assert.Greater(Mathf.Abs(rb.rotation - r0), 120f);
    }

    [UnityTest]
    public IEnumerator Lua_GravidadeBaixaECeuLunar()
    {
        Sessao.Modo = Modo.Lua;
        yield return Carregar();
        Assert.AreEqual(ModoJogo.Parametros(Modo.Lua).gravidade, Physics2D.gravity.y, 1e-4);
        Assert.AreEqual(Color.black, Camera.main.backgroundColor);
        Assert.IsTrue(GameObject.Find("Ceu Lunar").activeInHierarchy);
        Assert.AreEqual(ModoJogo.Parametros(Modo.Lua).paleta.grama, Object.FindFirstObjectByType<Terreno>().paleta.grama);
    }

    [UnityTest]
    public IEnumerator Terra_SemCeuLunar()
    {
        yield return Carregar();
        Assert.AreEqual(ModoJogo.GravidadeTerra, Physics2D.gravity.y, 1e-4);
        Assert.IsNull(GameObject.Find("Ceu Lunar"), "céu lunar ativo na Terra");
    }

    [UnityTest]
    public IEnumerator CorDaGaragem_SoNaPintura()
    {
        Preferencias.Cor = 2;
        yield return Carregar();
        var pintura = Object.FindFirstObjectByType<PinturaF1>().pintura;
        Assert.AreEqual(Garagem.Cores[2], pintura.color);
        foreach (var r in f1.GetComponentsInChildren<SpriteRenderer>())
            if (r != pintura) Assert.AreEqual(Color.white, r.color, $"{r.name} foi tingido");
    }

    [UnityTest]
    public IEnumerator Chassi_ColliderComASilhuetaCompleta()
    {
        yield return Carregar();
        var col = f1.GetComponent<PolygonCollider2D>();
        Assert.Greater(col.pathCount, 0);
        Assert.Greater(col.bounds.size.x, 3.4f, "collider perdeu a silhueta do F1");
    }

    [UnityTest]
    public IEnumerator ModoAula_TabLigaEDesliga()
    {
        yield return Carregar();
        yield return new WaitForSeconds(0.2f);
        var aula = Object.FindFirstObjectByType<ModoAula>();
        Assert.IsFalse(aula.Ativo, "modo aula ligado por padrão");
        aula.DefinirAtivo(true);
        var trecho = Object.FindFirstObjectByType<Terreno>().chunks[1].go.transform;
        Assert.IsTrue(trecho.Find("Aula").gameObject.activeSelf);
        Assert.IsTrue(f1.transform.Find("Aula: eixos").gameObject.activeSelf);
        Assert.IsTrue(aula.rotulo.gameObject.activeInHierarchy);
        aula.DefinirAtivo(false);
        Assert.IsFalse(trecho.Find("Aula").gameObject.activeSelf);
        Assert.IsFalse(aula.rotulo.gameObject.activeInHierarchy);
    }

    [UnityTest]
    public IEnumerator ModoAula_LigadoPelasConfiguracoes()
    {
        Preferencias.ModoAula = true;
        yield return Carregar();
        Assert.IsTrue(Object.FindFirstObjectByType<ModoAula>().Ativo);
    }

    [UnityTest]
    public IEnumerator Scanline_CongelaERetoma()
    {
        yield return Carregar();
        var aula = Object.FindFirstObjectByType<ModoAula>();
        aula.DefinirAtivo(true);
        aula.RepintarScanline();
        yield return null;
        Assert.AreEqual(0f, Time.timeScale);
        Assert.AreEqual(Partida.Estado.Congelado, partida.EstadoAtual);
        yield return new WaitForSecondsRealtime(aula.duracaoScanline + 0.5f);
        Assert.AreEqual(1f, Time.timeScale);
        Assert.AreEqual(Partida.Estado.Jogando, partida.EstadoAtual);
        Assert.IsFalse(aula.Repintando);
    }

    [UnityTest]
    public IEnumerator Scanline_ForaDoJogo_NaoFazNada()
    {
        yield return Carregar();
        var aula = Object.FindFirstObjectByType<ModoAula>();
        aula.DefinirAtivo(true);
        partida.AlternarPausa();
        aula.RepintarScanline();
        yield return null;
        Assert.IsFalse(aula.Repintando);
        Assert.AreEqual(Partida.Estado.Pausa, partida.EstadoAtual);
        partida.AlternarPausa();
        partida.Terminar("teste");
        aula.RepintarScanline();
        Assert.IsFalse(aula.Repintando);
        Assert.AreEqual(1f, Time.timeScale);
    }

    [UnityTest]
    public IEnumerator Pausa_BotaoContinuarRetoma()
    {
        yield return Carregar();
        partida.AlternarPausa();
        BotaoComTexto("Continuar").onClick.Invoke();
        Assert.AreEqual(Partida.Estado.Jogando, partida.EstadoAtual);
        Assert.AreEqual(1f, Time.timeScale);
    }

    [UnityTest]
    public IEnumerator Fim_JogarDeNovoMantemOModo()
    {
        Sessao.Modo = Modo.Lua;
        yield return Carregar();
        partida.Terminar("teste");
        BotaoComTexto("Jogar de novo").onClick.Invoke();
        yield return null;
        yield return new WaitForFixedUpdate();
        Assert.AreEqual(Partida.Estado.Jogando, Object.FindFirstObjectByType<Partida>().EstadoAtual);
        Assert.AreEqual(ModoJogo.Parametros(Modo.Lua).gravidade, Physics2D.gravity.y, 1e-4);
    }

    [UnityTest]
    public IEnumerator BotoesDoHud_TemAcao()
    {
        yield return Carregar();
        var botoes = Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        Assert.AreEqual(5, botoes.Length, "pausa (3) + fim (2)");
        foreach (var b in botoes) Assert.Greater(b.onClick.GetPersistentEventCount(), 0, $"botão sem ação: {b.name}");
    }

    [UnityTest]
    public IEnumerator Fim_DesligaOControle()
    {
        yield return Carregar();
        f1.controlePeloTeclado = true;
        partida.Terminar("teste");
        Assert.IsFalse(f1.controlePeloTeclado);
        Assert.AreEqual(0f, f1.acelerador);
    }
```

- [ ] **Step 2: ver falhar**

Run: `Tools/rodar_testes.sh PlayMode`
Expected: `exit=2`; falham os testes novos que dependem da cena nova (por exemplo `Lua_GravidadeBaixaECeuLunar`, `CorDaGaragem_SoNaPintura`, `ModoAula_TabLigaEDesliga`, `BotoesDoHud_TemAcao`, `NoAr_GiraMaisDe120GrausEmUmSegundo`), porque a cena ainda é a da etapa 1.

- [ ] **Step 3: peças comuns**

`CarroCG/Assets/Editor/MontagemComum.cs`:
```csharp
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

// Peças comuns às cenas montadas por script (Jogo e Menu): arte, física, F1, câmera, terreno, céu e UI.
public static class MontagemComum
{
    public const float Ppu = 256f;

    // Layout da arte 960x300 (Tools/gerar_f1.py) em unidades, com o pivô no centro do sprite.
    static Vector2 Local(float x, float y) => new((x - 480) / Ppu, (150 - y) / Ppu);
    public static readonly Vector2 RodaT = Local(190, 178), RodaD = Local(770, 178), Capacete = Local(515, 92);
    public const float RaioRoda = 92 / Ppu;
    public static readonly Vector2 Largada = new(12, 1.2f);   // 1º trecho plano: a câmera não vê o começo do mundo

    public static Material MaterialUnlit => AssetDatabase.LoadAssetAtPath<Material>(
        "Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Unlit-Default.mat");

    public static void DefinirBuild() => EditorBuildSettings.scenes = new[]
    {
        new EditorBuildSettingsScene("Assets/Scenes/Menu.unity", true),
        new EditorBuildSettingsScene("Assets/Scenes/Jogo.unity", true),
        new EditorBuildSettingsScene("Assets/Scenes/Main.unity", true),
    };

    public static Sprite CarregarSprite(string caminho) => AssetDatabase.LoadAssetAtPath<Sprite>(caminho);

    public static void ConfigurarArte()
    {
        AssetDatabase.Refresh();
        foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Art/F1", "Assets/Art/Ceu" }))
        {
            var caminho = AssetDatabase.GUIDToAssetPath(guid);
            var imp = (TextureImporter)AssetImporter.GetAtPath(caminho);
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.filterMode = FilterMode.Bilinear;
            imp.textureCompression = TextureImporterCompression.Uncompressed;
            imp.mipmapEnabled = true;
            imp.alphaIsTransparency = true;
            imp.spritePixelsPerUnit = caminho.Contains("/f1_") ? Ppu
                                    : caminho.EndsWith("estrelas.png") ? 20
                                    : caminho.EndsWith("terra.png") ? 128 : 200;
            imp.SaveAndReimport();
        }
    }

    public static PhysicsMaterial2D Material2D(string nome, float atrito)
    {
        Directory.CreateDirectory("Assets/Fisica");
        string caminho = $"Assets/Fisica/{nome}.physicsMaterial2D";
        var m = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(caminho);
        if (m == null)
        {
            m = new PhysicsMaterial2D(nome);
            AssetDatabase.CreateAsset(m, caminho);
        }
        m.friction = atrito;
        m.bounciness = 0;
        EditorUtility.SetDirty(m);
        AssetDatabase.SaveAssets();
        return m;
    }

    public static void LuzGlobal()
    {
        var luz = new GameObject("Luz Global").AddComponent<Light2D>();
        var so = new SerializedObject(luz);
        so.FindProperty("m_LightType").intValue = (int)Light2D.LightType.Global;
        so.ApplyModifiedPropertiesWithoutUndo();
        luz.intensity = 1f;
    }

    // ---------- F1 ----------

    public class PecasF1
    {
        public GameObject raiz, chassi;
        public F1 f1;
        public SpriteRenderer pintura;
    }

    public static PecasF1 CriarF1(PhysicsMaterial2D pneu)
    {
        var raiz = new GameObject("Carro");
        var chassi = new GameObject("Chassi") { tag = "Player" };
        chassi.transform.SetParent(raiz.transform, false);
        chassi.transform.position = Largada;
        var rb = chassi.AddComponent<Rigidbody2D>();
        rb.mass = 1.5f;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        // O collider nasce da silhueta completa (f1_corpo.png); depois o desenho passa para as camadas.
        var silhueta = chassi.AddComponent<SpriteRenderer>();
        silhueta.sprite = CarregarSprite("Assets/Art/F1/f1_corpo.png");
        chassi.AddComponent<PolygonCollider2D>();
        Object.DestroyImmediate(silhueta);
        Camada(chassi.transform, "Atras", "f1_atras", 2);
        var pintura = Camada(chassi.transform, "Pintura", "f1_pintura", 3);
        Camada(chassi.transform, "Frente", "f1_frente", 4);

        var f1 = chassi.AddComponent<F1>();
        chassi.AddComponent<PinturaF1>().pintura = pintura;
        var rodaT = NovaRoda("Roda Traseira", raiz.transform, Largada + RodaT, pneu);
        var rodaD = NovaRoda("Roda Dianteira", raiz.transform, Largada + RodaD, pneu);
        f1.eixoTraseiro = Eixo(chassi, rodaT, RodaT);
        f1.eixoDianteiro = Eixo(chassi, rodaD, RodaD);

        var capacete = new GameObject("Capacete") { tag = "Player" };
        capacete.transform.SetParent(chassi.transform, false);
        capacete.transform.localPosition = Capacete;
        var cc = capacete.AddComponent<CircleCollider2D>();
        cc.isTrigger = true;
        cc.radius = 0.3f;   // cobre capacete + halo: de teto no chão, passa da asa traseira (0,47) e encosta
        capacete.AddComponent<SensorCapacete>().f1 = f1;

        return new PecasF1 { raiz = raiz, chassi = chassi, f1 = f1, pintura = pintura };
    }

    static SpriteRenderer Camada(Transform pai, string nome, string arquivo, int ordem)
    {
        var go = new GameObject(nome);
        go.transform.SetParent(pai, false);
        var r = go.AddComponent<SpriteRenderer>();
        r.sprite = CarregarSprite($"Assets/Art/F1/{arquivo}.png");
        r.sortingOrder = ordem;
        return r;
    }

    static GameObject NovaRoda(string nome, Transform pai, Vector2 pos, PhysicsMaterial2D pneu)
    {
        var go = new GameObject(nome) { tag = "Player" };
        go.transform.SetParent(pai, false);
        go.transform.position = pos;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = CarregarSprite("Assets/Art/F1/f1_roda.png");
        sr.sortingOrder = 5;
        var rb = go.AddComponent<Rigidbody2D>();
        rb.mass = 0.25f;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        var c = go.AddComponent<CircleCollider2D>();
        c.radius = RaioRoda;
        c.sharedMaterial = pneu;
        return go;
    }

    static WheelJoint2D Eixo(GameObject chassi, GameObject roda, Vector2 ancora)
    {
        var j = chassi.AddComponent<WheelJoint2D>();
        j.connectedBody = roda.GetComponent<Rigidbody2D>();
        j.autoConfigureConnectedAnchor = false;
        j.anchor = ancora;
        j.connectedAnchor = Vector2.zero;
        j.suspension = new JointSuspension2D { dampingRatio = 0.7f, frequency = 6f, angle = 90f };
        j.useMotor = false;
        return j;
    }

    // ---------- Câmera, terreno, céu ----------

    public static Camera CriarCamera(Rigidbody2D alvo, float zoomMin, float zoomMax)
    {
        var cam = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener)).GetComponent<Camera>();
        cam.tag = "MainCamera";
        cam.orthographic = true;
        cam.orthographicSize = zoomMin;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.transform.position = new Vector3(Largada.x + 3, Largada.y + 1.5f, -10);
        var segue = cam.gameObject.AddComponent<CameraSegue>();
        segue.alvo = alvo;
        segue.zoomMin = zoomMin;
        segue.zoomMax = zoomMax;
        return cam;
    }

    public static GameObject CeuLunar(Camera cam)
    {
        var ceu = new GameObject("Ceu Lunar");
        ceu.transform.SetParent(cam.transform, false);
        ceu.transform.localPosition = new Vector3(0, 0, 20);
        Fundo(ceu.transform, "Estrelas", "Assets/Art/Ceu/estrelas.png", Vector3.zero, -50);
        Fundo(ceu.transform, "Terra", "Assets/Art/Ceu/terra.png", new Vector3(9, 5, 0), -49);
        return ceu;
    }

    static void Fundo(Transform pai, string nome, string sprite, Vector3 pos, int ordem)
    {
        var go = new GameObject(nome);
        go.transform.SetParent(pai, false);
        go.transform.localPosition = pos;
        var r = go.AddComponent<SpriteRenderer>();
        r.sprite = CarregarSprite(sprite);
        r.sharedMaterial = MaterialUnlit;     // céu não recebe a luz do jogo
        r.sortingOrder = ordem;
    }

    public static Terreno CriarTerreno(Transform alvo, PhysicsMaterial2D chao)
    {
        var terreno = new GameObject("Terreno").AddComponent<Terreno>();
        terreno.alvo = alvo;
        terreno.materialChao = chao;
        return terreno;
    }

    // ---------- UI (Screen Space - Camera, para aparecer nas capturas) ----------

    public static Canvas CriarCanvas(Camera cam)
    {
        var go = new GameObject("UI", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = cam;
        canvas.planeDistance = 1;
        canvas.sortingOrder = 100;          // por cima de carro, moedas e linhas do modo aula
        var scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        return canvas;
    }

    public static void CriarEventSystem() =>
        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

    public static RectTransform Caixa(string nome, Transform pai, Vector2 ancora, Vector2 pos, Vector2 tamanho, Color cor)
    {
        var go = new GameObject(nome, typeof(RectTransform), typeof(Image));
        var rt = (RectTransform)go.transform;
        rt.SetParent(pai, false);
        rt.anchorMin = rt.anchorMax = rt.pivot = ancora;
        rt.anchoredPosition = pos;
        rt.sizeDelta = tamanho;
        go.GetComponent<Image>().color = cor;
        return rt;
    }

    public static TMP_Text Texto(string nome, Transform pai, Vector2 ancoraMin, Vector2 ancoraMax, float tamanho,
                                 TextAlignmentOptions alinhamento)
    {
        var go = new GameObject(nome, typeof(RectTransform));
        var rt = (RectTransform)go.transform;
        rt.SetParent(pai, false);
        rt.anchorMin = ancoraMin;
        rt.anchorMax = ancoraMax;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        var t = go.AddComponent<TextMeshProUGUI>();
        t.fontSize = tamanho;
        t.alignment = alinhamento;
        t.fontStyle = FontStyles.Bold;
        t.color = Color.white;
        return t;
    }

    // Tela cheia escurecida (painéis do menu, pausa, fim).
    public static RectTransform Painel(string nome, Transform pai, float escurecer = 0.6f)
    {
        var rt = Caixa(nome, pai, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, new Color(0, 0, 0, escurecer));
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        return rt;
    }

    // Coluna de botões empilhados, centrada em 'ancora'.
    public static RectTransform Coluna(Transform pai, Vector2 ancora, float largura)
    {
        var go = new GameObject("Botoes", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        var rt = (RectTransform)go.transform;
        rt.SetParent(pai, false);
        rt.anchorMin = rt.anchorMax = rt.pivot = ancora;
        rt.sizeDelta = new Vector2(largura, 0);
        var v = go.GetComponent<VerticalLayoutGroup>();
        v.spacing = 14;
        v.childControlWidth = v.childControlHeight = true;
        v.childForceExpandWidth = true;
        v.childForceExpandHeight = false;
        go.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        return rt;
    }

    public static (Button, TMP_Text) Botao(Transform pai, string texto, UnityAction acao)
    {
        var (b, t) = NovoBotao(pai, texto);
        UnityEventTools.AddPersistentListener(b.onClick, acao);
        return (b, t);
    }

    public static (Button, TMP_Text) BotaoInt(Transform pai, string texto, UnityAction<int> acao, int argumento)
    {
        var (b, t) = NovoBotao(pai, texto);
        UnityEventTools.AddIntPersistentListener(b.onClick, acao, argumento);
        return (b, t);
    }

    static (Button, TMP_Text) NovoBotao(Transform pai, string texto)
    {
        var go = new GameObject(texto, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        go.transform.SetParent(pai, false);
        go.GetComponent<LayoutElement>().preferredHeight = 70;
        var botao = go.GetComponent<Button>();
        var cores = botao.colors;
        cores.normalColor = new Color(0.08f, 0.1f, 0.14f, 0.88f);
        cores.highlightedColor = cores.selectedColor = new Color(1f, 0.55f, 0.1f, 1f);   // teclado: selecionado em laranja
        cores.pressedColor = new Color(0.8f, 0.4f, 0.05f, 1f);
        botao.colors = cores;
        var t = Texto("Texto", go.transform, Vector2.zero, Vector2.one, 36, TextAlignmentOptions.Center);
        t.text = texto;
        return (botao, t);
    }
}
```

- [ ] **Step 4: cena do jogo**

`CarroCG/Assets/Editor/MontarJogo.cs` (arquivo inteiro):
```csharp
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// Monta Assets/Scenes/Jogo.unity. Menu: CarroCG → Montar Jogo. Sobrescreve a cena.
public static class MontarJogo
{
    const string Cena = "Assets/Scenes/Jogo.unity";

    [MenuItem("CarroCG/Montar Jogo")]
    public static void Executar()
    {
        MontagemComum.ConfigurarArte();
        var pneu = MontagemComum.Material2D("Pneu", 1.6f);
        var chao = MontagemComum.Material2D("Chao", 1.0f);
        var cena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        MontagemComum.LuzGlobal();
        var f1 = MontagemComum.CriarF1(pneu);
        var cam = MontagemComum.CriarCamera(f1.chassi.GetComponent<Rigidbody2D>(), 7, 11);
        var ceu = MontagemComum.CeuLunar(cam);
        var terreno = MontagemComum.CriarTerreno(f1.chassi.transform, chao);

        var coletaveis = new GameObject("Coletaveis").AddComponent<Coletaveis>();
        coletaveis.terreno = terreno;
        coletaveis.moeda = MontagemComum.CarregarSprite("Assets/Art/F1/moeda.png");
        coletaveis.galao = MontagemComum.CarregarSprite("Assets/Art/F1/galao.png");

        var config = new GameObject("Configurar Partida").AddComponent<ConfigurarPartida>();
        config.terreno = terreno;
        config.f1 = f1.f1;
        config.cam = cam;
        config.ceuLunar = ceu;

        var partida = new GameObject("Partida").AddComponent<Partida>();
        partida.f1 = f1.f1;
        var canvas = MontagemComum.CriarCanvas(cam);
        MontarHud(partida, canvas.transform);

        var aula = new GameObject("Modo Aula").AddComponent<ModoAula>();
        aula.terreno = terreno;
        aula.f1 = f1.f1;
        aula.partida = partida;
        aula.materialLinha = MontagemComum.MaterialUnlit;
        var caixaAula = MontagemComum.Caixa("Rotulo Aula", canvas.transform, new Vector2(1, 1), new Vector2(-40, -40),
                                            new Vector2(820, 250), new Color(0, 0, 0, 0.55f));
        aula.rotulo = MontagemComum.Texto("Texto", caixaAula, new Vector2(0.03f, 0.05f), new Vector2(0.97f, 0.95f),
                                          30, TextAlignmentOptions.TopLeft);
        MontagemComum.CriarEventSystem();

        Directory.CreateDirectory("Assets/Scenes");
        EditorSceneManager.SaveScene(cena, Cena);
        MontagemComum.DefinirBuild();
        Debug.Log("MontarJogo: cena salva em " + Cena);
    }

    static void MontarHud(Partida p, Transform raiz)
    {
        var fundoBarra = MontagemComum.Caixa("Combustivel", raiz, new Vector2(0, 1), new Vector2(40, -40), new Vector2(420, 34), new Color(0, 0, 0, 0.55f));
        // trilho com a margem; a barra dentro dele não tem margem, então com 0% fica com largura 0 (sem lasca)
        var trilho = new GameObject("Trilho", typeof(RectTransform)).GetComponent<RectTransform>();
        trilho.SetParent(fundoBarra, false);
        trilho.anchorMin = Vector2.zero;
        trilho.anchorMax = Vector2.one;
        trilho.offsetMin = new Vector2(4, 4);
        trilho.offsetMax = new Vector2(-4, -4);
        var barra = MontagemComum.Caixa("Nivel", trilho, Vector2.zero, Vector2.zero, Vector2.zero, new Color(1f, 0.55f, 0.1f));
        barra.anchorMin = Vector2.zero;
        barra.anchorMax = Vector2.one;
        barra.offsetMin = barra.offsetMax = Vector2.zero;
        p.barraCombustivel = barra;

        var caixaMoedas = MontagemComum.Caixa("Moedas", raiz, new Vector2(0, 1), new Vector2(40, -90), new Vector2(260, 56), new Color(0, 0, 0, 0.45f));
        p.textoMoedas = MontagemComum.Texto("Texto", caixaMoedas, Vector2.zero, Vector2.one, 34, TextAlignmentOptions.Center);

        var caixaDist = MontagemComum.Caixa("Distancia", raiz, new Vector2(0.5f, 1), new Vector2(0, -30), new Vector2(380, 120), new Color(0, 0, 0, 0.45f));
        p.textoDistancia = MontagemComum.Texto("Metros", caixaDist, new Vector2(0, 0.35f), Vector2.one, 60, TextAlignmentOptions.Center);
        p.textoRecorde = MontagemComum.Texto("Recorde", caixaDist, Vector2.zero, new Vector2(1, 0.4f), 26, TextAlignmentOptions.Center);

        var pausa = MontagemComum.Painel("Pausa", raiz);
        MontagemComum.Texto("Titulo", pausa, new Vector2(0.2f, 0.62f), new Vector2(0.8f, 0.82f), 96, TextAlignmentOptions.Center).text = "PAUSADO";
        var colPausa = MontagemComum.Coluna(pausa, new Vector2(0.5f, 0.42f), 480);
        MontagemComum.Botao(colPausa, "Continuar", p.AlternarPausa);
        MontagemComum.Botao(colPausa, "Reiniciar", p.Reiniciar);
        MontagemComum.Botao(colPausa, "Menu principal", p.IrParaMenu);
        p.painelPausa = pausa.gameObject;

        var fim = MontagemComum.Painel("Fim", raiz);
        p.textoFim = MontagemComum.Texto("Texto", fim, new Vector2(0.15f, 0.5f), new Vector2(0.85f, 0.85f), 84, TextAlignmentOptions.Center);
        var colFim = MontagemComum.Coluna(fim, new Vector2(0.5f, 0.32f), 480);
        MontagemComum.Botao(colFim, "Jogar de novo", p.Reiniciar);
        MontagemComum.Botao(colFim, "Menu", p.IrParaMenu);
        p.painelFim = fim.gameObject;
    }
}
```

```bash
"$UNITY" -batchmode -nographics -projectPath CarroCG -executeMethod MontarJogo.Executar -quit -logFile "$PWD/test-montar.log"; echo "exit=$?"
grep -E "MontarJogo:|error CS|Exception|referenced script" test-montar.log | head -5
```
Expected: `exit=0`, `MontarJogo: cena salva em Assets/Scenes/Jogo.unity`, nenhum "referenced script".

- [ ] **Step 5: ver passar**

Run: `Tools/rodar_testes.sh PlayMode`
Expected: `exit=0`, `total="20" passed="20"` (7 antigos + 13 novos; o `Capturar` saiu).
Se `NoAr_GiraMaisDe120GrausEmUmSegundo` falhar, ver o ângulo obtido antes de mexer: abaixo de 120° → subir `torqueNoAr` do modo; `NoChao` verdadeiro → subir mais o carro. Se `Acelerando_AtravessaVariosTrechos` falhar por capotar, recuar `centroDeMassa.y` em passos de 0,05. Registrar no ledger.

Run: `Tools/rodar_testes.sh EditMode` → Expected: `exit=0`, `total="74" passed="74"`.

- [ ] **Step 6: commit**

```bash
git add CarroCG/Assets CarroCG/ProjectSettings
git commit -m "Cena do jogo remontada: modo Lua, pintura, modo aula, botões de pausa e fim"
```

---

### Task 8: Cena do menu, testes de fluxo e capturas

**Files:**
- Create: `CarroCG/Assets/Editor/MontarMenu.cs`, `CarroCG/Assets/Tests/PlayMode/MenuTests.cs`, `CarroCG/Assets/Tests/PlayMode/CapturasTests.cs`
- Modify: `Tools/capturar.sh`
- Create (gerado): `CarroCG/Assets/Scenes/Menu.unity`

**Interfaces:**
- Consumes: `MontagemComum` (Task 7); `Menu` (Task 6); `PilotoAutomatico`, `ConfigurarPartida` (Task 4); `Fotografo` (Task 7).
- Produces: `MontarMenu.Executar()` (menu CarroCG → Montar Menu); `Menu.unity` como cena 0 do build.

- [ ] **Step 1: testes**

`CarroCG/Assets/Tests/PlayMode/MenuTests.cs`:
```csharp
using System.Collections;
using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

public class MenuTests
{
    Menu menu;

    [SetUp]
    public void Isolar()
    {
        Preferencias.Prefixo = "teste.";
        Sessao.Modo = Modo.Terra;
    }

    [TearDown]
    public void Limpar()
    {
        Preferencias.ApagarTudo();
        Preferencias.Prefixo = "";
        Sessao.Modo = Modo.Terra;
        Physics2D.gravity = new Vector2(0, ModoJogo.GravidadeTerra);
        Time.timeScale = 1;
    }

    IEnumerator Carregar(string cena)
    {
        SceneManager.LoadScene(cena);
        yield return null;
        yield return null;
        yield return new WaitForFixedUpdate();
        menu = Object.FindFirstObjectByType<Menu>();
    }

    static Button BotaoComTexto(string texto) =>
        Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None)
              .First(b => b.GetComponentInChildren<TMP_Text>(true).text == texto);

    [Test]
    public void PrimeiraCenaDoBuild_EhOMenu() =>
        StringAssert.EndsWith("Menu.unity", SceneUtility.GetScenePathByBuildIndex(0));

    [UnityTest]
    public IEnumerator AbreNaTelaPrincipalComOTitulo()
    {
        yield return Carregar("Menu");
        Assert.AreEqual(menu.principal, menu.TelaAtual);
        Assert.IsFalse(menu.garagem.activeSelf);
        Assert.IsTrue(Object.FindObjectsByType<TMP_Text>(FindObjectsSortMode.None).Any(t => t.text == "Senninhas Driving"));
    }

    [UnityTest]
    public IEnumerator Demo_CarroAndaSozinho()
    {
        yield return Carregar("Menu");
        var f1 = Object.FindFirstObjectByType<F1>();
        float x0 = f1.transform.position.x;
        yield return new WaitForSeconds(4);
        Assert.Greater(f1.transform.position.x - x0, 10f);
    }

    [UnityTest]
    public IEnumerator JogarLua_CarregaOJogoNaLua()
    {
        yield return Carregar("Menu");
        menu.AbrirEscolhaModo();
        BotaoComTexto("Lua").onClick.Invoke();
        yield return null;
        yield return null;
        Assert.AreEqual("Jogo", SceneManager.GetActiveScene().name);
        Assert.AreEqual(ModoJogo.Parametros(Modo.Lua).gravidade, Physics2D.gravity.y, 1e-4);
    }

    [UnityTest]
    public IEnumerator VoltarDaLua_MenuComGravidadeDaTerra()
    {
        Sessao.Modo = Modo.Lua;
        yield return Carregar("Jogo");
        Object.FindFirstObjectByType<Partida>().IrParaMenu();
        yield return null;
        yield return null;
        Assert.AreEqual("Menu", SceneManager.GetActiveScene().name);
        Assert.AreEqual(ModoJogo.GravidadeTerra, Physics2D.gravity.y, 1e-4);
    }

    [UnityTest]
    public IEnumerator Garagem_CorEscolhidaVaiProJogo()
    {
        yield return Carregar("Menu");
        menu.AbrirGaragem();
        menu.EscolherCor(1);
        Assert.AreEqual(Garagem.Cores[1], menu.previaPintura.color);
        menu.Jogar((int)Modo.Terra);
        yield return null;
        yield return null;
        Assert.AreEqual(Garagem.Cores[1], Object.FindFirstObjectByType<PinturaF1>().pintura.color);
    }

    [UnityTest]
    public IEnumerator PausaMenuPrincipal_VoltaAoMenu()
    {
        yield return Carregar("Jogo");
        Object.FindFirstObjectByType<Partida>().AlternarPausa();
        BotaoComTexto("Menu principal").onClick.Invoke();
        yield return null;
        yield return null;
        Assert.AreEqual("Menu", SceneManager.GetActiveScene().name);
        Assert.AreEqual(1f, Time.timeScale);
    }

    [UnityTest]
    public IEnumerator ModoAulaDasConfiguracoes_LigaNoJogo()
    {
        yield return Carregar("Menu");
        menu.AbrirConfiguracoes();
        menu.AlternarModoAula();
        StringAssert.Contains("sim", menu.textoModoAula.text);
        menu.Jogar((int)Modo.Terra);
        yield return null;
        yield return null;
        Assert.IsTrue(Object.FindFirstObjectByType<ModoAula>().Ativo);
    }

    [UnityTest]
    public IEnumerator Configuracoes_SobrevivemAoRecarregar()
    {
        yield return Carregar("Menu");
        menu.ProximaQualidade();
        menu.AlternarTelaCheia();
        yield return Carregar("Menu");
        Assert.AreEqual(1, Preferencias.Qualidade);
        Assert.IsFalse(Preferencias.TelaCheia);
        StringAssert.Contains("Média", menu.textoQualidade.text);
    }

    [UnityTest]
    public IEnumerator Qualidade_MudaAResolucaoDoTerreno()
    {
        Preferencias.Qualidade = 2;
        yield return Carregar("Jogo");
        Assert.AreEqual(Preferencias.PpuTerreno[2], Object.FindFirstObjectByType<Terreno>().ppu);
    }

    [UnityTest]
    public IEnumerator ZerarRecordes_PedeConfirmacao()
    {
        Preferencias.RegistrarPartida(Modo.Terra, 100, 3);
        yield return Carregar("Menu");
        menu.AbrirRecorde();
        StringAssert.Contains("100 m", menu.textoRecorde.text);
        menu.ZerarRecordes();
        Assert.AreEqual(100f, Preferencias.Recorde(Modo.Terra), "zerou sem confirmar");
        menu.ZerarRecordes();
        Assert.AreEqual(0f, Preferencias.Recorde(Modo.Terra));
    }

    [UnityTest]
    public IEnumerator CadaTela_SelecionaUmBotao()
    {
        yield return Carregar("Menu");
        foreach (var abrir in new System.Action[] { menu.AbrirPrincipal, menu.AbrirEscolhaModo, menu.AbrirGaragem,
                     menu.AbrirControles, menu.AbrirConfiguracoes, menu.AbrirTecnicas, menu.AbrirRecorde })
        {
            abrir();
            var sel = EventSystem.current.currentSelectedGameObject;
            Assert.IsNotNull(sel, $"nada selecionado em {menu.TelaAtual.name}");
            Assert.IsTrue(sel.transform.IsChildOf(menu.TelaAtual.transform), $"seleção fora da tela {menu.TelaAtual.name}");
        }
    }

    [UnityTest]
    public IEnumerator TodosOsBotoesTemAcao()
    {
        yield return Carregar("Menu");
        var botoes = Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        Assert.Greater(botoes.Length, 20);
        foreach (var b in botoes) Assert.Greater(b.onClick.GetPersistentEventCount(), 0, $"botão sem ação: {b.name}");
    }

    [UnityTest]
    public IEnumerator Tecnicas_MostraOTextoDosDados()
    {
        yield return Carregar("Menu");
        menu.AbrirTecnicas();
        Assert.AreEqual(TecnicasCG.Texto(), menu.textoTecnicas.text);
    }
}
```

`CarroCG/Assets/Tests/PlayMode/CapturasTests.cs`:
```csharp
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

// Capturas de verificação (Tools/capturar.sh). Explicit: só rodam filtradas pelo nome. Precisam de GPU.
public class CapturasTests
{
    [SetUp]
    public void Isolar()
    {
        Assume.That(!string.IsNullOrEmpty(Fotografo.Pasta), "defina CAPTURA_DIR");
        Preferencias.Prefixo = "teste.";
        Sessao.Modo = Modo.Terra;
    }

    [TearDown]
    public void Limpar()
    {
        Preferencias.ApagarTudo();
        Preferencias.Prefixo = "";
        Sessao.Modo = Modo.Terra;
        Physics2D.gravity = new Vector2(0, ModoJogo.GravidadeTerra);
        Time.timeScale = 1;
    }

    static IEnumerator Carregar(string cena)
    {
        SceneManager.LoadScene(cena);
        yield return null;
        yield return new WaitForFixedUpdate();
    }

    [UnityTest, Explicit]
    public IEnumerator Menu()
    {
        yield return Carregar("Menu");
        yield return new WaitForSeconds(2);
        Fotografo.Foto("menu-principal.png");
        var menu = Object.FindFirstObjectByType<Menu>();
        menu.EscolherCor(1);
        menu.AbrirGaragem();
        yield return new WaitForSeconds(0.3f);
        Fotografo.Foto("menu-garagem.png");
        menu.AbrirTecnicas();
        yield return null;
        Fotografo.Foto("menu-tecnicas.png");
    }

    [UnityTest, Explicit]
    public IEnumerator Jogo()
    {
        yield return Carregar("Jogo");
        var f1 = Object.FindFirstObjectByType<F1>();
        f1.controlePeloTeclado = false;
        yield return new WaitForSeconds(0.5f);
        Fotografo.Foto("jogo-largada.png");
        f1.acelerador = 1;
        yield return new WaitForSeconds(6);
        Fotografo.Foto("jogo-morros.png");
        Object.FindFirstObjectByType<Partida>().Terminar("Teste");
        yield return null;
        Fotografo.Foto("jogo-fim.png");
    }

    [UnityTest, Explicit]
    public IEnumerator Aula()
    {
        yield return Carregar("Jogo");
        var f1 = Object.FindFirstObjectByType<F1>();
        f1.controlePeloTeclado = false;
        var aula = Object.FindFirstObjectByType<ModoAula>();
        aula.DefinirAtivo(true);
        f1.acelerador = 1;
        yield return new WaitForSeconds(4);
        Fotografo.Foto("jogo-aula.png");
        aula.RepintarScanline();
        yield return new WaitForSecondsRealtime(aula.duracaoScanline * 0.5f);
        Fotografo.Foto("jogo-scanline.png");
        yield return new WaitForSecondsRealtime(aula.duracaoScanline);
    }

    [UnityTest, Explicit]
    public IEnumerator Lua()
    {
        Sessao.Modo = Modo.Lua;
        yield return Carregar("Jogo");
        var f1 = Object.FindFirstObjectByType<F1>();
        f1.controlePeloTeclado = false;
        f1.acelerador = 1;
        yield return new WaitForSeconds(5);
        Fotografo.Foto("jogo-lua.png");
    }
}
```

- [ ] **Step 2: ver falhar**

Run: `Tools/rodar_testes.sh PlayMode`
Expected: `exit=2`; falham os testes de `MenuTests` que carregam "Menu" (`Scene 'Menu' couldn't be loaded`) e `PrimeiraCenaDoBuild_EhOMenu`.

- [ ] **Step 3: montar o menu**

`CarroCG/Assets/Editor/MontarMenu.cs`:
```csharp
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// Monta Assets/Scenes/Menu.unity. Menu: CarroCG → Montar Menu. Sobrescreve a cena.
public static class MontarMenu
{
    const string Cena = "Assets/Scenes/Menu.unity";
    static readonly Color Dourado = new(1f, 0.8f, 0.2f);

    [MenuItem("CarroCG/Montar Menu")]
    public static void Executar()
    {
        MontagemComum.ConfigurarArte();
        var pneu = MontagemComum.Material2D("Pneu", 1.6f);
        var chao = MontagemComum.Material2D("Chao", 1.0f);
        var cena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // Fundo animado: o jogo de verdade, na Terra, com o piloto automático
        MontagemComum.LuzGlobal();
        var f1 = MontagemComum.CriarF1(pneu);
        var cam = MontagemComum.CriarCamera(f1.chassi.GetComponent<Rigidbody2D>(), 9, 12);
        var terreno = MontagemComum.CriarTerreno(f1.chassi.transform, chao);
        var config = new GameObject("Configurar Partida").AddComponent<ConfigurarPartida>();
        config.terreno = terreno;
        config.f1 = f1.f1;
        config.cam = cam;
        config.forcarTerra = true;
        var piloto = f1.chassi.AddComponent<PilotoAutomatico>();
        piloto.f1 = f1.f1;
        piloto.terreno = terreno;

        var canvas = MontagemComum.CriarCanvas(cam);
        var menu = canvas.gameObject.AddComponent<Menu>();
        var raiz = canvas.transform;
        menu.principal = TelaPrincipal(raiz, menu);
        menu.escolhaModo = TelaEscolhaModo(raiz, menu);
        menu.garagem = TelaGaragem(raiz, menu);
        menu.controles = TelaControles(raiz, menu);
        menu.configuracoes = TelaConfiguracoes(raiz, menu);
        menu.tecnicas = TelaTecnicas(raiz, menu);
        menu.recorde = TelaRecorde(raiz, menu);
        MontagemComum.CriarEventSystem();

        Directory.CreateDirectory("Assets/Scenes");
        EditorSceneManager.SaveScene(cena, Cena);
        MontagemComum.DefinirBuild();
        Debug.Log("MontarMenu: cena salva em " + Cena);
    }

    static TMP_Text Titulo(Transform pai, string texto, float tamanho = 72)
    {
        var t = MontagemComum.Texto("Titulo", pai, new Vector2(0.1f, 0.8f), new Vector2(0.9f, 0.95f), tamanho, TextAlignmentOptions.Center);
        t.text = texto;
        return t;
    }

    static void Voltar(Transform pai, Menu menu, float y = 0.1f) =>
        MontagemComum.Botao(MontagemComum.Coluna(pai, new Vector2(0.5f, y), 360), "Voltar", menu.AbrirPrincipal);

    static GameObject TelaPrincipal(Transform raiz, Menu menu)
    {
        var tela = MontagemComum.Painel("Principal", raiz, 0.25f);
        var nome = MontagemComum.Texto("Nome", tela, new Vector2(0.05f, 0.72f), new Vector2(0.95f, 0.92f), 120, TextAlignmentOptions.Center);
        nome.text = "Senninhas Driving";
        nome.color = Dourado;
        MontagemComum.Texto("Subtitulo", tela, new Vector2(0.1f, 0.66f), new Vector2(0.9f, 0.73f), 30, TextAlignmentOptions.Center)
                     .text = "Trabalho de Computação Gráfica";
        var col = MontagemComum.Coluna(tela, new Vector2(0.5f, 0.34f), 520);
        MontagemComum.Botao(col, "Jogar", menu.AbrirEscolhaModo);
        MontagemComum.Botao(col, "Garagem", menu.AbrirGaragem);
        MontagemComum.Botao(col, "Controles", menu.AbrirControles);
        MontagemComum.Botao(col, "Configurações", menu.AbrirConfiguracoes);
        MontagemComum.Botao(col, "Técnicas de CG", menu.AbrirTecnicas);
        MontagemComum.Botao(col, "Recorde", menu.AbrirRecorde);
        MontagemComum.Botao(col, "Sair", menu.Sair);
        return tela.gameObject;
    }

    static GameObject TelaEscolhaModo(Transform raiz, Menu menu)
    {
        var tela = MontagemComum.Painel("Escolha do modo", raiz);
        Titulo(tela, "Escolha o modo");
        var col = MontagemComum.Coluna(tela, new Vector2(0.5f, 0.5f), 520);
        MontagemComum.BotaoInt(col, "Terra", menu.Jogar, (int)Modo.Terra);
        MontagemComum.BotaoInt(col, "Lua", menu.Jogar, (int)Modo.Lua);
        MontagemComum.Botao(col, "Voltar", menu.AbrirPrincipal);
        MontagemComum.Texto("Dica", tela, new Vector2(0.15f, 0.18f), new Vector2(0.85f, 0.3f), 28, TextAlignmentOptions.Center)
                     .text = "Lua: gravidade baixa, o carro voa longe nos morros";
        return tela.gameObject;
    }

    static GameObject TelaGaragem(Transform raiz, Menu menu)
    {
        var tela = MontagemComum.Painel("Garagem", raiz);
        Titulo(tela, "Garagem");

        // Vitrine: as camadas do F1 como imagens da UI, alinhadas como no sprite 960x300
        var previa = new GameObject("Previa", typeof(RectTransform)).GetComponent<RectTransform>();
        previa.SetParent(tela, false);
        previa.anchorMin = previa.anchorMax = new Vector2(0.5f, 0.56f);
        previa.sizeDelta = new Vector2(960, 300);
        Imagem(previa, "Atras", "f1_atras", Vector2.zero, new Vector2(960, 300));
        menu.previaPintura = Imagem(previa, "Pintura", "f1_pintura", Vector2.zero, new Vector2(960, 300));
        Imagem(previa, "Frente", "f1_frente", Vector2.zero, new Vector2(960, 300));
        Imagem(previa, "Roda Traseira", "f1_roda", new Vector2(190 - 480, 150 - 178), new Vector2(184, 184));
        Imagem(previa, "Roda Dianteira", "f1_roda", new Vector2(770 - 480, 150 - 178), new Vector2(184, 184));
        menu.previaCarro = previa;

        var linha = new GameObject("Cores", typeof(RectTransform), typeof(HorizontalLayoutGroup)).GetComponent<RectTransform>();
        linha.SetParent(tela, false);
        linha.anchorMin = linha.anchorMax = new Vector2(0.5f, 0.3f);
        linha.sizeDelta = new Vector2(8 * 100, 90);
        var h = linha.GetComponent<HorizontalLayoutGroup>();
        h.spacing = 10;
        h.childControlWidth = h.childControlHeight = true;
        h.childForceExpandWidth = h.childForceExpandHeight = true;
        for (int i = 0; i < Garagem.Cores.Length; i++)
        {
            var (b, t) = MontagemComum.BotaoInt(linha, Garagem.Nomes[i], menu.EscolherCor, i);
            t.text = "";                                   // a amostra de cor é o próprio botão
            var cores = b.colors;
            cores.normalColor = Garagem.Cores[i];
            cores.highlightedColor = cores.selectedColor = Color.Lerp(Garagem.Cores[i], Color.white, 0.45f);
            cores.pressedColor = Color.Lerp(Garagem.Cores[i], Color.black, 0.3f);
            b.colors = cores;
        }
        menu.textoCor = MontagemComum.Texto("Cor", tela, new Vector2(0.3f, 0.18f), new Vector2(0.7f, 0.25f), 36, TextAlignmentOptions.Center);
        Voltar(tela, menu);
        return tela.gameObject;
    }

    static Image Imagem(RectTransform pai, string nome, string arquivo, Vector2 pos, Vector2 tamanho)
    {
        var go = new GameObject(nome, typeof(RectTransform), typeof(Image));
        var rt = (RectTransform)go.transform;
        rt.SetParent(pai, false);
        rt.anchoredPosition = pos;
        rt.sizeDelta = tamanho;
        var img = go.GetComponent<Image>();
        img.sprite = MontagemComum.CarregarSprite($"Assets/Art/F1/{arquivo}.png");
        img.raycastTarget = false;
        return img;
    }

    static GameObject TelaControles(Transform raiz, Menu menu)
    {
        var tela = MontagemComum.Painel("Controles", raiz);
        Titulo(tela, "Controles");
        var caixa = MontagemComum.Caixa("Caixa", tela, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1000, 560), new Color(0, 0, 0, 0.5f));
        MontagemComum.Texto("Texto", caixa, new Vector2(0.06f, 0.05f), new Vector2(0.94f, 0.95f), 38, TextAlignmentOptions.Left).text =
            "Seta direita   acelera\nSeta esquerda   freia / ré\nNo ar: as setas giram o carro\nR   reinicia\nEsc   pausa\nTab   modo aula\nF   scanline em câmera lenta (modo aula)";
        Voltar(tela, menu);
        return tela.gameObject;
    }

    static GameObject TelaConfiguracoes(Transform raiz, Menu menu)
    {
        var tela = MontagemComum.Painel("Configuracoes", raiz);
        Titulo(tela, "Configurações");
        var col = MontagemComum.Coluna(tela, new Vector2(0.5f, 0.5f), 640);
        menu.textoQualidade = MontagemComum.Botao(col, "Qualidade", menu.ProximaQualidade).Item2;
        menu.textoTelaCheia = MontagemComum.Botao(col, "Tela cheia", menu.AlternarTelaCheia).Item2;
        menu.textoModoAula = MontagemComum.Botao(col, "Modo aula ao iniciar", menu.AlternarModoAula).Item2;
        MontagemComum.Botao(col, "Voltar", menu.AbrirPrincipal);
        MontagemComum.Texto("Dica", tela, new Vector2(0.15f, 0.12f), new Vector2(0.85f, 0.25f), 26, TextAlignmentOptions.Center)
                     .text = "A qualidade muda a resolução do terreno e vale a partir da próxima partida.";
        return tela.gameObject;
    }

    static GameObject TelaTecnicas(Transform raiz, Menu menu)
    {
        var tela = MontagemComum.Painel("Tecnicas", raiz, 0.75f);
        Titulo(tela, "Técnicas de CG", 64);
        var t = MontagemComum.Texto("Texto", tela, new Vector2(0.1f, 0.17f), new Vector2(0.9f, 0.8f), 26, TextAlignmentOptions.TopLeft);
        t.fontStyle = FontStyles.Normal;
        t.enableAutoSizing = true;
        t.fontSizeMin = 14;
        t.fontSizeMax = 26;
        menu.textoTecnicas = t;
        Voltar(tela, menu, 0.08f);
        return tela.gameObject;
    }

    static GameObject TelaRecorde(Transform raiz, Menu menu)
    {
        var tela = MontagemComum.Painel("Recorde", raiz);
        Titulo(tela, "Recorde");
        menu.textoRecorde = MontagemComum.Texto("Texto", tela, new Vector2(0.2f, 0.42f), new Vector2(0.8f, 0.78f), 48, TextAlignmentOptions.Center);
        var col = MontagemComum.Coluna(tela, new Vector2(0.5f, 0.28f), 560);
        menu.textoZerar = MontagemComum.Botao(col, "Zerar recordes", menu.ZerarRecordes).Item2;
        MontagemComum.Botao(col, "Voltar", menu.AbrirPrincipal);
        return tela.gameObject;
    }
}
```

`Tools/capturar.sh`:
```bash
#!/bin/bash
# Joga sozinho e salva PNGs em previews/ (precisa de GPU: sem -nographics). Editor fechado.
cd "$(dirname "$0")/.."
U=$(ls -d ~/Unity/Hub/Editor/6000.*/Editor/Unity | head -1)
mkdir -p previews
CAPTURA_DIR="$PWD/previews" "$U" -batchmode -projectPath CarroCG -runTests -testPlatform PlayMode \
  -testFilter "CapturasTests.Menu;CapturasTests.Jogo;CapturasTests.Aula;CapturasTests.Lua" \
  -testResults "$PWD/test-results-captura.xml" -logFile "$PWD/test-captura.log"
rc=$?
echo "exit=$rc"; ls -1 previews/menu-*.png previews/jogo-*.png 2>/dev/null
exit $rc
```

```bash
"$UNITY" -batchmode -nographics -projectPath CarroCG -executeMethod MontarMenu.Executar -quit -logFile "$PWD/test-montar.log"; echo "exit=$?"
grep -E "MontarMenu:|error CS|Exception|referenced script" test-montar.log | head -5
```
Expected: `exit=0`, `MontarMenu: cena salva em Assets/Scenes/Menu.unity`.

- [ ] **Step 4: ver passar**

Run: `Tools/rodar_testes.sh PlayMode`
Expected: `exit=0`, `total="38" passed="34"` (20 + 14 de `MenuTests`; os 4 de `CapturasTests` são Explicit).
Run: `Tools/rodar_testes.sh EditMode` → Expected: `exit=0`, `total="74" passed="74"`.

- [ ] **Step 5: capturas**

Run: `Tools/capturar.sh`
Expected: `exit=0` e 9 PNGs. Abrir (Read) e conferir:
1. `menu-principal.png`: "Senninhas Driving" dourado, subtítulo, 7 botões com o primeiro em laranja (selecionado), F1 dirigindo ao fundo.
2. `menu-garagem.png`: F1 grande pintado de azul (rodas, asas pretas e detalhes iguais), fileira de 8 amostras de cor, nome "Azul".
3. `menu-tecnicas.png`: 9 técnicas legíveis, selos verdes "no jogo" e cinza "em breve".
4. `jogo-aula.png`: polígono de controle branco, janela amarela, vermelho passando da borda de baixo, verde terminando nela, emenda ciano, eixos do F1, rótulo no canto.
5. `jogo-scanline.png`: trecho do meio pela metade, de baixo para cima, com a linha ciano na altura atual.
6. `jogo-lua.png`: céu preto com estrelas e a Terra, chão cinza, F1 alto no ar.
7. `jogo-largada.png`, `jogo-morros.png`, `jogo-fim.png`: como na etapa 1, com o fim mostrando "Jogar de novo" e "Menu".
Texto com quadradinhos no lugar de letras = caractere fora da fonte (ver Global Constraints).

- [ ] **Step 6: commit**

```bash
git add Tools/capturar.sh CarroCG/Assets CarroCG/ProjectSettings previews/menu-*.png previews/jogo-*.png
git commit -m "Menu Senninhas Driving: telas, garagem, configurações, técnicas, recordes; testes de fluxo e capturas"
```

---

## Depois desta entrega

Jogar a partir do menu, nos dois modos, e anotar: se capotar ficou possível sem ser frustrante, se os morros cresceram no ritmo certo, se a Lua tem a sensação "bugada" do Hill Climb. Ajustes vão para `ModoJogo.Parametros` (terreno, gravidade, giro) e `F1` (centro de massa), e as cenas são remontadas (`CarroCG → Montar Jogo` e `Montar Menu`). Depois, etapa 2 (iluminação) com seu próprio plano.
