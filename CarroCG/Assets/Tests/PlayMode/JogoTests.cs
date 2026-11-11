using System.Collections;
using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

// Joga a cena de verdade, sem teclado: o teste segura o acelerador.
public class JogoTests
{
    F1 f1;
    Partida partida;

    [SetUp]
    public void Isolar()
    {
        Preferencias.Prefixo = "teste.";
        Sessao.Modo = Modo.Terra;
        Sessao.IntroVista = true;     // testes de jogo começam jogando (a intro tem os seus)
    }

    [TearDown]
    public void Limpar()
    {
        Preferencias.ApagarTudo();
        Preferencias.Prefixo = "";
        Sessao.Modo = Modo.Terra;
        Sessao.IntroVista = true;     // testes de jogo começam jogando (a intro tem os seus)
        Physics2D.gravity = new Vector2(0, ModoJogo.GravidadeTerra);
        Time.timeScale = 1;
    }

    IEnumerator Carregar()
    {
        SceneManager.LoadScene("Jogo");
        yield return null;
        yield return new WaitForFixedUpdate();
        f1 = Object.FindFirstObjectByType<F1>();
        partida = Object.FindFirstObjectByType<Partida>();
        f1.controlePeloTeclado = false;
    }

    [UnityTest]
    public IEnumerator Acelerando_AtravessaVariosTrechos()
    {
        yield return Carregar();
        float x0 = f1.transform.position.x;
        f1.acelerador = 1;
        yield return new WaitForSeconds(10);
        Assert.Greater(f1.transform.position.x - x0, 40f, "não avançou o bastante (sentido do motor? atrito? emenda?)");
        Assert.Greater(partida.Moedas, 0, "passou pelas moedas do trecho 1 sem pegar nenhuma");
    }

    [UnityTest]
    public IEnumerator SemCombustivel_TerminaJogo()
    {
        yield return Carregar();
        partida.combustivel.Consumir(10000, 0);
        f1.acelerador = 0;
        yield return new WaitForSeconds(3);
        Assert.AreEqual(Partida.Estado.Fim, partida.EstadoAtual);
        Assert.IsTrue(partida.painelFim.activeSelf);
        Assert.GreaterOrEqual(partida.barraCombustivel.rect.width, 0f, "barra vazia com largura negativa aparece como lasca");
    }

    [UnityTest]
    public IEnumerator Capotado_TerminaJogo()
    {
        yield return Carregar();
        f1.GetComponent<Rigidbody2D>().rotation = 180;
        yield return new WaitForSeconds(3);
        Assert.AreEqual(Partida.Estado.Fim, partida.EstadoAtual);
    }

    [UnityTest]
    public IEnumerator Pausar_ReiniciarRestauraTempo()
    {
        yield return Carregar();
        partida.AlternarPausa();
        Assert.AreEqual(0f, Time.timeScale);
        partida.Reiniciar();
        yield return null;
        yield return null;
        Assert.AreEqual(1f, Time.timeScale);
        Assert.AreEqual(Partida.Estado.Jogando, Object.FindFirstObjectByType<Partida>().EstadoAtual);
    }

    [UnityTest]
    public IEnumerator Hud_DesenhaPorCimaDoJogo()
    {
        yield return Carregar();
        int maior = Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None).Max(r => r.sortingOrder);
        Assert.Greater(Object.FindFirstObjectByType<Canvas>().sortingOrder, maior);
    }

    [UnityTest]
    public IEnumerator Re_NaoCaiDoMundo()
    {
        yield return Carregar();
        f1.acelerador = -1;
        yield return new WaitForSeconds(6);
        Assert.Greater(f1.transform.position.y, -3f, "caiu pela borda esquerda do terreno");
    }

    [UnityTest]
    public IEnumerator Terminar_NaoMexeNoRecordeReal()
    {
        bool tinha = PlayerPrefs.HasKey("recorde.Terra");
        float antes = PlayerPrefs.GetFloat("recorde.Terra");
        yield return Carregar();
        f1.acelerador = 1;
        yield return new WaitForSeconds(3);
        partida.Terminar("teste");
        Assert.AreEqual(tinha, PlayerPrefs.HasKey("recorde.Terra"));
        Assert.AreEqual(antes, PlayerPrefs.GetFloat("recorde.Terra"));
        Assert.Greater(PlayerPrefs.GetFloat("teste.recorde.Terra"), 0f, "o recorde da partida de teste não foi para a chave de teste");
    }

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
            if (r != pintura && r.name != "Feixe")      // o feixe do farol é aceso pelo ciclo (alfa), não pela garagem
                Assert.AreEqual(Color.white, r.color, $"{r.name} foi tingido");
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

    [UnityTest]
    public IEnumerator ModoAula_AfastaACameraParaMostrarAJanela()
    {
        yield return Carregar();
        var aula = Object.FindFirstObjectByType<ModoAula>();
        aula.DefinirAtivo(true);
        yield return new WaitForSeconds(3);
        var terreno = Object.FindFirstObjectByType<Terreno>();
        var cam = Camera.main;
        var c = terreno.chunks.Find(k => k.janela.xMin <= cam.transform.position.x && cam.transform.position.x < k.janela.xMax);
        float baseDaTela = cam.transform.position.y - cam.orthographicSize;
        Assert.Less(baseDaTela, c.janela.yMin, "a borda de baixo da janela de recorte fica fora da tela");
        aula.DefinirAtivo(false);
        yield return new WaitForSeconds(3);
        Assert.Less(cam.orthographicSize, 12f, "a câmera não voltou ao zoom do jogo");
    }

    [UnityTest]
    public IEnumerator ModoAula_CarroNoArContinuaNaTela()
    {
        yield return Carregar();
        var aula = Object.FindFirstObjectByType<ModoAula>();
        aula.DefinirAtivo(true);
        yield return new WaitForSeconds(2);
        foreach (var corpo in f1.transform.parent.GetComponentsInChildren<Rigidbody2D>())
            corpo.position += Vector2.up * 8;
        yield return new WaitForSeconds(0.6f);
        float y = Camera.main.WorldToViewportPoint(f1.transform.position).y;
        Assert.That(y, Is.InRange(0.05f, 0.82f), "carro fora da tela ou atrás do HUD no modo aula");
    }

    [UnityTest]
    public IEnumerator PresoNaPonta_TerminaJogo()
    {
        yield return Carregar();
        foreach (var corpo in f1.transform.parent.GetComponentsInChildren<Rigidbody2D>())
        {
            corpo.position += Vector2.up * 4;        // longe do chão: o capacete não encosta
            corpo.constraints = RigidbodyConstraints2D.FreezeAll;
        }
        var rb = f1.GetComponent<Rigidbody2D>();
        rb.constraints = RigidbodyConstraints2D.None;
        rb.rotation = 120;
        rb.constraints = RigidbodyConstraints2D.FreezeAll;
        yield return new WaitForSeconds(2.5f);
        Assert.AreEqual(Partida.Estado.Fim, partida.EstadoAtual, "carro preso/virado e o jogo não terminou");
    }

    [UnityTest]
    public IEnumerator SemCombustivel_MostraAvisoEnquantoNoEmbalo()
    {
        yield return Carregar();
        f1.acelerador = 1;
        yield return new WaitForSeconds(2);
        partida.combustivel.Consumir(10000, 0);
        yield return null;
        Assert.IsTrue(partida.avisoSemGasolina.activeSelf);
        Assert.AreEqual(Partida.Estado.Jogando, partida.EstadoAtual, "terminou com o carro ainda andando");
    }

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
            if (r.name != "Feixe" && r.name != "Tela Intro") Assert.AreEqual("CarroCG/F1Phong", r.sharedMaterial.shader.name, r.name);
    }

    [UnityTest, Explicit]   // precisa de GPU: rodado por Tools/capturar.sh
    public IEnumerator F1Phong_SemCicloNaoFicaPreto()
    {
        // precisa de GPU (sem -nographics): rodado por Tools/capturar.sh; no batch sem gráficos fica inconclusivo
        Assume.That(SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null, "sem GPU");
        Shader.SetGlobalVector("_DirSol", Vector4.zero);       // como se nenhum CicloDia tivesse rodado
        var cam = new GameObject("cam-teste").AddComponent<Camera>();
        var peca = new GameObject("peca").AddComponent<SpriteRenderer>();
        var rt = new RenderTexture(16, 16, 24);
        try
        {
            cam.orthographic = true; cam.orthographicSize = 2; cam.transform.position = new Vector3(0, 0, -10);
            cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = Color.black;
            peca.sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 1);
            peca.sharedMaterial = new Material(Shader.Find("CarroCG/F1Phong"));
            cam.targetTexture = rt; cam.Render(); cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(16, 16, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, 16, 16), 0, 0);
            RenderTexture.active = null;
            Assert.Greater(tex.GetPixel(8, 8).grayscale, 0.2f, "F1 preto sem a luz global definida");
        }
        finally
        {
            cam.targetTexture = null;
            Object.Destroy(cam.gameObject);
            Object.Destroy(peca.gameObject);
            rt.Release();
        }
        yield return null;
    }

    [UnityTest]
    public IEnumerator PhongF1_PassaARotacaoDeCadaPeca()
    {
        yield return Carregar();
        f1.acelerador = 1;
        yield return new WaitForSeconds(1);                    // rodas giraram, chassi quase reto
        var roda = f1.eixoTraseiro.connectedBody;
        roda.angularVelocity = 0;
        roda.constraints = RigidbodyConstraints2D.FreezeRotation;  // parada: o valor lido é o do último LateUpdate
        yield return null;
        yield return null;
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

    [UnityTest]
    public IEnumerator Lua_CeuPretoSemGradienteNemAstrosDaTerra()
    {
        Sessao.Modo = Modo.Lua;
        yield return Carregar();
        yield return null;
        var ciclo = Object.FindFirstObjectByType<CicloDia>();
        Assert.IsFalse(ciclo.ceuGradiente.enabled, "gradiente da Terra deixa o céu lunar cinza");
        Assert.IsFalse(ciclo.sol.enabled);
        Assert.IsFalse(ciclo.lua.enabled);
        Assert.IsFalse(ciclo.estrelas.enabled, "a Lua já tem o próprio céu estrelado");
        Assert.AreEqual(Color.black, Camera.main.backgroundColor);
    }

    [UnityTest]
    public IEnumerator Sombra_EmLadeiraNaoCorta()
    {
        yield return Carregar();
        var sombra = Object.FindFirstObjectByType<SombraF1>();
        sombra.Altura = x => x * 0.8f - 9.6f;                  // ladeira de ~39° passando sob o carro (x≈12)
        yield return null;
        yield return null;
        Assert.IsTrue(sombra.Visivel);
        float alturaDaTextura = sombra.render.sprite.rect.height / sombra.render.sprite.pixelsPerUnit;
        Assert.GreaterOrEqual(alturaDaTextura, sombra.Area.height, "a faixa da sombra não cabe na textura (corta em ladeira)");
    }

    [UnityTest]
    public IEnumerator SolAoMeioDia_AbaixoDoHud()
    {
        yield return Carregar();
        var ciclo = Object.FindFirstObjectByType<CicloDia>();
        ciclo.fixo = true;
        ciclo.tempoFixo = ciclo.Tempo = 0.25f;
        ciclo.Aplicar(true);
        yield return null;
        Assert.Less(Camera.main.WorldToViewportPoint(ciclo.sol.transform.position).y, 0.78f, "sol atrás da caixa de distância");
    }

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

    [UnityTest]
    public IEnumerator Parallax_SemCeuEntreCamadaEABaseDaTela()
    {
        yield return Carregar();
        var p = Object.FindFirstObjectByType<Parallax>();
        Object.FindFirstObjectByType<ModoAula>().DefinirAtivo(true);          // câmera bem afastada
        yield return new WaitForSeconds(2);
        float baseDaTela = Camera.main.transform.position.y - Camera.main.orthographicSize;
        foreach (var c in p.camadas)
        {
            Assert.IsNotNull(c.preenchimento, $"{c.render.name} sem preenchimento embaixo");
            Assert.LessOrEqual(c.preenchimento.bounds.min.y, baseDaTela, $"céu aparece embaixo de {c.render.name}");
            Assert.GreaterOrEqual(c.preenchimento.bounds.max.y, c.render.bounds.min.y - 0.01f, $"fresta entre {c.render.name} e o preenchimento");
        }
    }

    [UnityTest]
    public IEnumerator Lua_SemPinheirosNoFundo()
    {
        Sessao.Modo = Modo.Lua;
        yield return Carregar();
        yield return null;
        var p = Object.FindFirstObjectByType<Parallax>();
        Assert.IsFalse(p.camadas[0].render.enabled, "pinheiros na Lua");
        Assert.IsTrue(p.camadas[2].render.enabled);
    }

    // ---------- Lago (etapa 4) ----------

    static Terreno.Chunk AcharLago(Terreno t)
    {
        for (float x = 0; x < 2000; x += 20)
        {
            t.Atualizar(x);
            var c = t.chunks.Find(k => k.agua != null);
            if (c != null) return c;
        }
        return null;
    }

    [UnityTest]
    public IEnumerator Lago_TemAguaEPonteHorizontal()
    {
        yield return Carregar();
        var terreno = Object.FindFirstObjectByType<Terreno>();
        var lago = AcharLago(terreno);
        Assert.IsNotNull(lago, "nenhum lago em 100 trechos");
        var pontos = lago.go.GetComponent<EdgeCollider2D>().points;
        foreach (var p in pontos) Assert.AreEqual(pontos[0].y, p.y, 1e-3f, "ponte não é plana");
        Assert.Less(lago.agua.Value.yMax, lago.trecho.p0.y, "água acima da ponte");
        Assert.IsNotNull(lago.go.transform.Find("Agua"));
    }

    [UnityTest]
    public IEnumerator Reflexo_ApareceSoSobreOLago()
    {
        yield return Carregar();
        var reflexo = Object.FindFirstObjectByType<Reflexo>();
        yield return null;
        Assert.IsTrue(reflexo.Malhas.All(m => m.vertexCount == 0), "reflexo na largada, sem lago");

        var lago = AcharLago(Object.FindFirstObjectByType<Terreno>());
        var destino = new Vector2(lago.agua.Value.center.x, lago.trecho.p0.y + 0.6f);
        var delta = destino - (Vector2)f1.transform.position;
        foreach (var rb in f1.transform.parent.GetComponentsInChildren<Rigidbody2D>())
        {
            rb.position += delta;
            rb.linearVelocity = Vector2.zero;
            rb.transform.position += (Vector3)delta;
        }
        yield return null;
        yield return null;
        var pintura = reflexo.Malhas[1];
        Assert.Greater(pintura.vertexCount, 2, "sem reflexo sobre o lago");
        foreach (var v in pintura.vertices)
        {
            var w = reflexo.transform.TransformPoint(v);
            Assert.LessOrEqual(w.y, lago.agua.Value.yMax + 1e-3f, "reflexo acima da linha da água");
        }
    }

    [UnityTest]
    public IEnumerator Lua_SemLago()
    {
        Sessao.Modo = Modo.Lua;
        yield return Carregar();
        var terreno = Object.FindFirstObjectByType<Terreno>();
        bool viu = false;
        terreno.TrechoCriado += c => viu |= c.agua != null;
        for (float x = 0; x < 600; x += 20) terreno.Atualizar(x);
        Assert.IsFalse(viu);
    }

    // ---------- Efeitos (etapa 6) ----------

    [UnityTest]
    public IEnumerator Poeira_EmiteAcelerandoNoChao()
    {
        yield return Carregar();
        var efeitos = Object.FindFirstObjectByType<Efeitos>();
        yield return new WaitForSeconds(0.5f);
        Assert.AreEqual(0, efeitos.Poeira.particleCount, "poeira parado");
        f1.acelerador = 1;
        yield return new WaitForSeconds(1);
        Assert.Greater(efeitos.Poeira.particleCount, 0);
    }

    [UnityTest]
    public IEnumerator Aterrissagem_Forte_TremeEFaisca()
    {
        yield return Carregar();
        var efeitos = Object.FindFirstObjectByType<Efeitos>();
        var segue = Object.FindFirstObjectByType<CameraSegue>();
        yield return new WaitForSeconds(0.5f);
        float forca = 0;
        segue.Impacto += f => forca = f;
        foreach (var rb in f1.transform.parent.GetComponentsInChildren<Rigidbody2D>())
        {
            rb.position += Vector2.up * 6;
            rb.transform.position += Vector3.up * 6;
        }
        float t0 = Time.time;
        while (forca == 0 && Time.time - t0 < 3) yield return null;
        Assert.Greater(forca, 0, "sem impacto ao cair de 6 m");
        Assert.Greater(segue.Tremor, 0);
        Assert.Greater(efeitos.Faiscas.particleCount, 0);
    }

    [UnityTest]
    public IEnumerator Moeda_Brilho()
    {
        yield return Carregar();
        var efeitos = Object.FindFirstObjectByType<Efeitos>();
        Coletavel.NotificarParaTeste(TipoColetavel.Moeda, f1.transform.position);
        yield return null;
        Assert.Greater(efeitos.Brilho.particleCount, 0);
    }

    [UnityTest]
    public IEnumerator PosProcessamento_TemBloomVinheta()
    {
        yield return Carregar();
        var volume = Object.FindFirstObjectByType<Volume>();
        Assert.IsNotNull(volume);
        Assert.IsTrue(volume.isGlobal);
        Assert.IsTrue(volume.profile.TryGet<Bloom>(out var b) && b.active);
        Assert.IsTrue(volume.profile.TryGet<Vignette>(out var v) && v.active);
        Assert.IsTrue(volume.profile.TryGet<ColorAdjustments>(out _));
        Assert.IsTrue(Camera.main.GetUniversalAdditionalCameraData().renderPostProcessing);
    }
}
