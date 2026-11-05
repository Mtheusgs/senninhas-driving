using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

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
        PlayerPrefs.SetFloat("recorde.Terra", 5);
        try
        {
            yield return Carregar();
            f1.acelerador = 1;
            yield return new WaitForSeconds(3);
            partida.Terminar("teste");
            Assert.AreEqual(5f, PlayerPrefs.GetFloat("recorde.Terra"));
        }
        finally
        {
            if (tinha) PlayerPrefs.SetFloat("recorde.Terra", antes); else PlayerPrefs.DeleteKey("recorde.Terra");
        }
    }

    // Só roda quando filtrado pelo nome (Tools/capturar.sh). Precisa de GPU.
    [UnityTest, Explicit]
    public IEnumerator Capturar()
    {
        string dir = System.Environment.GetEnvironmentVariable("CAPTURA_DIR");
        Assume.That(!string.IsNullOrEmpty(dir), "defina CAPTURA_DIR");
        yield return Carregar();
        yield return new WaitForSeconds(0.5f);
        Foto(dir + "/jogo-largada.png");
        f1.acelerador = 1;
        yield return new WaitForSeconds(6);
        Foto(dir + "/jogo-morros.png");
        partida.Terminar("Teste");
        yield return null;
        Foto(dir + "/jogo-fim.png");
    }

    static void Foto(string caminho)
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
        System.IO.File.WriteAllBytes(caminho, tex.EncodeToPNG());
        cam.targetTexture = null;
        RenderTexture.active = null;
        Object.Destroy(rt);
        Object.Destroy(tex);
    }
}
