using System.Linq;
using NUnit.Framework;
using UnityEngine;

public class IntroDesenhoTests
{
    const int W = 240, H = 104;
    static readonly Rect Janela = new(-3, -1.3f, 6, 2.6f);
    static readonly Color32 Branco = new(255, 255, 255, 255);

    static int Pintados(Color32[] px) => px.Count(c => c.a > 0);

    [Test]
    public void Wireframe_PintaPixels_NenhumForaDoBuffer()
    {
        var px = new Color32[W * H];
        IntroDesenho.Wireframe(px, W, H, Janela, ModeloF1.Rotacao(30, 15), 1, 4, Branco);
        Assert.Greater(Pintados(px), 100);
    }

    [Test]
    public void Wireframe_LongeDoBuffer_NaoExplode()
    {
        var px = new Color32[W * H];
        IntroDesenho.Wireframe(px, W, H, new Rect(50, 50, 6, 2.6f), ModeloF1.Rotacao(30, 15), 1, 4, Branco);
        Assert.AreEqual(0, Pintados(px));
    }

    [Test]
    public void Perspectiva_DiferenteDaParalela()
    {
        var a = new Color32[W * H];
        var b = new Color32[W * H];
        IntroDesenho.Wireframe(a, W, H, Janela, ModeloF1.Rotacao(30, 15), 0, 4, Branco);
        IntroDesenho.Wireframe(b, W, H, Janela, ModeloF1.Rotacao(30, 15), 1, 4, Branco);
        CollectionAssert.AreNotEqual(a, b);
    }

    [Test]
    public void Contorno_ZeroRegioes_NadaPintado()
    {
        var px = new Color32[W * H];
        IntroDesenho.Contorno(px, W, H, Janela, 0, Branco);
        Assert.AreEqual(0, Pintados(px));
        IntroDesenho.Contorno(px, W, H, Janela, ContornoF1.Regioes.Length, Branco);
        Assert.Greater(Pintados(px), 100);
    }

    static Vector2Int Pixel(Vector2 artePx)
    {
        var u = ContornoF1.EmUnidades(artePx);
        return Vector2Int.FloorToInt(Raster.JanelaViewport(u, Janela, new Rect(0, 0, W, H)));
    }

    [Test]
    public void Preencher_TodasAsRegioes_CentroDoChassiComCorDoChassi()
    {
        var px = new Color32[W * H];
        IntroDesenho.Preencher(px, W, H, Janela, ContornoF1.Regioes.Length);
        var p = Pixel(new Vector2(620, 160));
        Assert.AreEqual(ContornoF1.Cores[ContornoF1.Chassi], px[p.y * W + p.x]);
    }

    [Test]
    public void Preencher_SoAsPrimeiras_NaoPintaRodas()
    {
        var px = new Color32[W * H];
        IntroDesenho.Preencher(px, W, H, Janela, ContornoF1.Chassi + 1);
        var roda = Pixel(new Vector2(190, 250));      // parte da roda traseira abaixo do assoalho
        Assert.AreEqual(0, px[roda.y * W + roda.x].a);
    }

    [Test]
    public void Limpar_ZeraTudo()
    {
        var px = Enumerable.Repeat(Branco, W * H).ToArray();
        IntroDesenho.Limpar(px);
        Assert.AreEqual(0, Pintados(px));
    }
}
