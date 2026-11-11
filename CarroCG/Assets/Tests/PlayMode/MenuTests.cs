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

    [UnityTest]
    public IEnumerator Recorde_AbreComVoltarSelecionado()
    {
        yield return Carregar("Menu");
        menu.AbrirRecorde();
        Assert.AreEqual("Voltar", EventSystem.current.currentSelectedGameObject.GetComponentInChildren<TMP_Text>().text);
    }

    [UnityTest]
    public IEnumerator Garagem_SelecionaACorEscolhida()
    {
        Preferencias.Cor = 1;
        yield return Carregar("Menu");
        menu.AbrirGaragem();
        Assert.AreEqual(Garagem.Nomes[1], EventSystem.current.currentSelectedGameObject.name);
    }

    [UnityTest]
    public IEnumerator Garagem_CarroDoFundoMudaDeCor()
    {
        yield return Carregar("Menu");
        menu.EscolherCor(2);
        Assert.AreEqual(Garagem.Cores[2], Object.FindFirstObjectByType<PinturaF1>().pintura.color);
    }

    [UnityTest]
    public IEnumerator Demo_RecomecarLevaACameraJunto()
    {
        yield return Carregar("Menu");
        yield return new WaitForSeconds(6);
        Object.FindFirstObjectByType<PilotoAutomatico>().Recomecar();
        yield return null;
        var f1 = Object.FindFirstObjectByType<F1>();
        Assert.Less(Mathf.Abs(Camera.main.transform.position.x - f1.transform.position.x), 12f, "câmera ficou para trás e atravessou o mundo");
    }

    [UnityTest]
    public IEnumerator Garagem_CarroParadoNaVitrine()
    {
        yield return Carregar("Menu");
        menu.AbrirGaragem();
        yield return new WaitForSeconds(1);
        Assert.AreEqual(1f, menu.previaCarro.localScale.x, 1e-4);
    }

    [UnityTest]
    public IEnumerator Demo_NaoFicaRecomecando()
    {
        yield return Carregar("Menu");
        var piloto = Object.FindFirstObjectByType<PilotoAutomatico>();
        yield return new WaitForSeconds(45);
        Assert.LessOrEqual(piloto.Recomecos, 1, "o demo do menu recomeçou várias vezes (cortes no fundo)");
    }

    [UnityTest]
    public IEnumerator Demo_RecomecoAutomaticoEscureceEVolta()
    {
        yield return Carregar("Menu");
        var piloto = Object.FindFirstObjectByType<PilotoAutomatico>();
        piloto.IniciarRecomeco();
        yield return new WaitForSecondsRealtime(0.3f);
        Assert.Greater(piloto.fade.color.a, 0.3f, "recomeço sem transição (corte seco)");
        yield return new WaitForSecondsRealtime(1.2f);
        Assert.AreEqual(0f, piloto.fade.color.a, 0.01f);
        Assert.AreEqual(1, piloto.Recomecos);
    }

    [UnityTest]
    public IEnumerator Demo_MorrosMaisSuavesQueOJogo()
    {
        yield return Carregar("Menu");
        Assert.Less(Object.FindFirstObjectByType<Terreno>().amplitudeMax, ModoJogo.Parametros(Modo.Terra).amplitudeMax);
    }
}
