using System.Collections;
using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEngine;
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
}
