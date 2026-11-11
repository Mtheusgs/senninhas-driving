using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class IntroTests
{
    [SetUp]
    public void Isolar()
    {
        Preferencias.Prefixo = "teste.";
        Sessao.Modo = Modo.Terra;
        Sessao.IntroVista = false;
    }

    [TearDown]
    public void Limpar()
    {
        Preferencias.ApagarTudo();
        Preferencias.Prefixo = "";
        Sessao.IntroVista = true;
        Time.timeScale = 1;
    }

    static IEnumerator Carregar()
    {
        SceneManager.LoadScene("Jogo");
        yield return null;
        yield return null;
    }

    [UnityTest]
    public IEnumerator Intro_CongelaEMostraWireframe()
    {
        yield return Carregar();
        var intro = Object.FindFirstObjectByType<IntroF1>();
        var partida = Object.FindFirstObjectByType<Partida>();
        Assert.IsTrue(intro.Ativa);
        Assert.AreEqual(Partida.Estado.Congelado, partida.EstadoAtual);
        yield return new WaitForSecondsRealtime(1);
        Assert.Greater(intro.Textura.GetPixels32().Count(c => c.a > 0), 100, "wireframe não desenhado");
        Assert.AreEqual(0f, intro.hud.alpha);
    }

    [UnityTest]
    public IEnumerator PularNoInicio_JogoNormal()
    {
        yield return Carregar();
        var intro = Object.FindFirstObjectByType<IntroF1>();
        var partida = Object.FindFirstObjectByType<Partida>();
        intro.Pular();
        yield return null;
        Assert.IsFalse(intro.Ativa);
        Assert.AreEqual(Partida.Estado.Jogando, partida.EstadoAtual);
        Assert.AreEqual(1f, Time.timeScale);
        Assert.AreEqual(1f, intro.hud.alpha);
        foreach (var p in intro.camadas) Assert.AreEqual(1f, p.color.a, p.name);
        Assert.IsFalse(intro.tela.enabled);
    }

    [UnityTest]
    public IEnumerator Intro_NaoRepeteNaSessao()
    {
        yield return Carregar();
        Object.FindFirstObjectByType<IntroF1>().Pular();
        yield return null;
        Object.FindFirstObjectByType<Partida>().Reiniciar();
        yield return null;
        yield return null;
        Assert.IsFalse(Object.FindFirstObjectByType<IntroF1>().Ativa);
        Assert.AreEqual(Partida.Estado.Jogando, Object.FindFirstObjectByType<Partida>().EstadoAtual);
    }

    [UnityTest]
    public IEnumerator Intro_TerminaSozinhaNoJogo()
    {
        yield return Carregar();
        var intro = Object.FindFirstObjectByType<IntroF1>();
        intro.duracao = 1;      // roteiro inteiro em 1 s
        intro.Comecar();
        yield return new WaitForSecondsRealtime(1.5f);
        Assert.IsFalse(intro.Ativa);
        Assert.AreEqual(Partida.Estado.Jogando, Object.FindFirstObjectByType<Partida>().EstadoAtual);
    }
}
