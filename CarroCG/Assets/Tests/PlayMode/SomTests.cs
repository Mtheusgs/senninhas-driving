using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class SomTests
{
    [SetUp]
    public void Isolar()
    {
        Preferencias.Prefixo = "teste.";
        Sessao.Modo = Modo.Terra;
        Sessao.IntroVista = true;
    }

    [TearDown]
    public void Limpar()
    {
        Preferencias.ApagarTudo();
        Preferencias.Prefixo = "";
        Time.timeScale = 1;
        AudioListener.volume = 1;
    }

    static IEnumerator Carregar()
    {
        SceneManager.LoadScene("Jogo");
        yield return null;
        yield return new WaitForFixedUpdate();
    }

    [UnityTest]
    public IEnumerator Motor_PitchSobeAcelerando()
    {
        yield return Carregar();
        var som = Object.FindFirstObjectByType<Som>();
        var f1 = Object.FindFirstObjectByType<F1>();
        f1.controlePeloTeclado = false;
        yield return new WaitForSeconds(0.5f);
        float parado = som.Motor.pitch;
        f1.acelerador = 1;
        yield return new WaitForSeconds(2);
        Assert.Greater(som.Motor.pitch, parado + 0.3f);
        Assert.IsTrue(som.Motor.isPlaying);
    }

    [UnityTest]
    public IEnumerator VolumeZero_Mudo()
    {
        Preferencias.Volume = 0;
        yield return Carregar();
        Assert.AreEqual(0f, AudioListener.volume);
    }

    [UnityTest]
    public IEnumerator Volume_PadraoSete()
    {
        yield return Carregar();
        Assert.AreEqual(0.7f, AudioListener.volume, 1e-5f);
    }

    [UnityTest]
    public IEnumerator Pausa_MotorSilencia()
    {
        yield return Carregar();
        var som = Object.FindFirstObjectByType<Som>();
        Object.FindFirstObjectByType<Partida>().AlternarPausa();
        yield return null;
        Assert.AreEqual(0f, som.Motor.volume);
    }

    [UnityTest]
    public IEnumerator Moeda_TocaSom()
    {
        yield return Carregar();
        var som = Object.FindFirstObjectByType<Som>();
        Coletavel.NotificarParaTeste(TipoColetavel.Moeda);
        yield return null;
        Assert.AreEqual(som.ClipMoeda, som.UltimoEfeito);
    }
}
