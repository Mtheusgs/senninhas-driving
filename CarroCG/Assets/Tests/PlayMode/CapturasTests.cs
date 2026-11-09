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

    [UnityTest, Explicit]
    public IEnumerator Luz()
    {
        yield return Carregar("Jogo");
        var f1 = Object.FindFirstObjectByType<F1>();
        f1.controlePeloTeclado = false;
        f1.acelerador = 1;
        yield return new WaitForSeconds(4);                     // sobe para os morros
        f1.acelerador = 0;
        yield return new WaitForSeconds(1.5f);
        var ciclo = Object.FindFirstObjectByType<CicloDia>();
        var partida = Object.FindFirstObjectByType<Partida>();
        ciclo.fixo = true;
        foreach (var (t, nome) in new[] { (0.25f, "luz-dia.png"), (0.47f, "luz-por-do-sol.png"), (0.75f, "luz-noite.png") })
        {
            ciclo.tempoFixo = ciclo.Tempo = t;
            ciclo.Aplicar(true);
            yield return null;
            Fotografo.Foto(nome);
        }
        ciclo.tempoFixo = ciclo.Tempo = 0.45f;
        ciclo.Aplicar(true);
        foreach (var nome in new[] { "luz-flat.png", "luz-gouraud.png", "luz-phong.png" })
        {
            partida.ProximoSombreamento();                        // Phong → Flat → Gouraud → Phong
            yield return null;
            Fotografo.Foto(nome);
        }
    }

    [UnityTest, Explicit]
    public IEnumerator Projecao()
    {
        yield return Carregar("Jogo");
        var f1 = Object.FindFirstObjectByType<F1>();
        f1.controlePeloTeclado = false;
        f1.acelerador = 1;
        yield return new WaitForSeconds(4);
        Fotografo.Foto("projecao-perspectiva.png");
        Object.FindFirstObjectByType<Partida>().AlternarProjecao();
        yield return null;
        Fotografo.Foto("projecao-paralela.png");
    }
}
