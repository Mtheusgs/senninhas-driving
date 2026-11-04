using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

public class TerrenoTests
{
    static Trecho[] Gerar(int semente, int n)
    {
        var g = new TerrenoGerador(semente);
        return Enumerable.Range(0, n).Select(_ => g.Proximo()).ToArray();
    }

    [Test]
    public void MesmaSemente_MesmosTrechos() => CollectionAssert.AreEqual(Gerar(7, 10), Gerar(7, 10));

    [Test]
    public void SementesDiferentes_Diferem() => CollectionAssert.AreNotEqual(Gerar(7, 10), Gerar(8, 10));

    [Test]
    public void PrimeiroTrecho_PlanoNaOrigem()
    {
        var t = Gerar(7, 1)[0];
        Assert.AreEqual(Vector2.zero, t.p0);
        foreach (var p in new[] { t.p1, t.p2, t.p3 }) Assert.AreEqual(0f, p.y);
    }

    [Test]
    public void Trechos_EmendaC1()
    {
        var ts = Gerar(3, 50);
        for (int i = 1; i < ts.Length; i++)
        {
            Assert.AreEqual(ts[i - 1].p3, ts[i].p0, $"trecho {i} não começa onde o anterior termina");
            var fim = Raster.BezierTangent(ts[i - 1].p0, ts[i - 1].p1, ts[i - 1].p2, ts[i - 1].p3, 1);
            var ini = Raster.BezierTangent(ts[i].p0, ts[i].p1, ts[i].p2, ts[i].p3, 0);
            Assert.AreEqual(fim.x, ini.x, 1e-3, $"tangente x na emenda {i}");
            Assert.AreEqual(fim.y, ini.y, 1e-3, $"tangente y na emenda {i}");
        }
    }

    [Test]
    public void Trechos_XCrescenteEAlturaLimitada()
    {
        var g = new TerrenoGerador(5);
        for (int i = 0; i < 300; i++)
        {
            var t = g.Proximo();
            Assert.Less(t.p0.x, t.p1.x, $"trecho {i}");
            Assert.Less(t.p1.x, t.p2.x, $"trecho {i}");
            Assert.Less(t.p2.x, t.p3.x, $"trecho {i}");
            Assert.LessOrEqual(Mathf.Abs(t.p3.y - t.p0.y), g.amplitudeMax + 1e-4f, $"trecho {i}");
            foreach (var p in TerrenoGerador.Amostrar(t, 16))
                Assert.LessOrEqual(Mathf.Abs(p.y), g.limiteY + g.amplitudeMax, $"trecho {i}");
        }
    }

    [Test]
    public void Trechos_InclinacaoJogavel()
    {
        var g = new TerrenoGerador(11);
        for (int i = 0; i < 300; i++)
        {
            var t = g.Proximo();
            for (int k = 0; k <= 32; k++)
            {
                var d = Raster.BezierTangent(t.p0, t.p1, t.p2, t.p3, k / 32f);
                Assert.LessOrEqual(Mathf.Abs(d.y / d.x), 1.5f, $"trecho {i}, t={k / 32f}");
            }
        }
    }

    [Test]
    public void Amostrar_ExtremosNaCurva()
    {
        var t = Gerar(7, 2)[1];
        var c = TerrenoGerador.Amostrar(t, 64);
        Assert.AreEqual(64, c.Length);
        Assert.AreEqual(t.p0, c[0]);
        Assert.AreEqual(t.p3, c[63]);
    }

    [Test]
    public void Normal_NoPlano_ApontaParaCima()
    {
        var n = TerrenoGerador.Normal(Gerar(7, 1)[0], 0.5f);
        Assert.AreEqual(0f, n.x, 1e-5);
        Assert.AreEqual(1f, n.y, 1e-5);
    }

    static readonly Vector2[] Morro = { new(0, 1), new(1, 2), new(2, 1) };

    [Test]
    public void Poligono_FechaPeloFundo()
    {
        CollectionAssert.AreEqual(
            new Vector2[] { new(0, 1), new(1, 2), new(2, 1), new(2, -3), new(0, -3) },
            TerrenoChunk.Poligono(Morro, -3));
    }

    [Test]
    public void AlturaEm_InterpolaEClampa()
    {
        Assert.AreEqual(1.5f, TerrenoChunk.AlturaEm(Morro, 0.5f), 1e-5);
        Assert.AreEqual(1f, TerrenoChunk.AlturaEm(Morro, -1f), 1e-5);
        Assert.AreEqual(1f, TerrenoChunk.AlturaEm(Morro, 5f), 1e-5);
    }

    [Test]
    public void Pintar_AbaixoPreenchidoAcimaVazio()
    {
        var curva = new Vector2[] { new(0, 0), new(2, 0) };
        var paleta = new TerrenoChunk.Paleta();
        var px = TerrenoChunk.Pintar(TerrenoChunk.Poligono(curva, -1), new Rect(0, -1, 2, 2), 4, curva, paleta,
                                     out int w, out int h);
        Assert.AreEqual(8, w);
        Assert.AreEqual(8, h);
        Assert.AreEqual(255, px[3 * w + 2].a, "linha logo abaixo da superfície");
        Assert.AreEqual(0, px[4 * w + 2].a, "linha logo acima da superfície");
        Assert.AreEqual((Color32)paleta.grama, px[3 * w + 2], "faixa de grama");
        Assert.AreEqual(paleta.Cor(0.875f), px[0 * w + 2], "terra no fundo");
    }

    [Test]
    public void Pintar_PoligonoDegenerado_Transparente()
    {
        var linha = new Vector2[] { new(0, 0), new(1, 1) };
        var px = TerrenoChunk.Pintar(new List<Vector2>(linha), new Rect(0, 0, 1, 1), 4, linha,
                                     new TerrenoChunk.Paleta(), out _, out _);
        Assert.IsTrue(px.All(c => c.a == 0));
    }
}
