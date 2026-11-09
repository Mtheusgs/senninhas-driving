using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

public class RasterTests
{
    static bool Conexa(Vector2Int[] px) =>
        Enumerable.Range(1, px.Length - 1).All(i =>
            Mathf.Abs(px[i].x - px[i - 1].x) <= 1 && Mathf.Abs(px[i].y - px[i - 1].y) <= 1);

    [Test]
    public void Linha_Rasa_PixelsEsperados()
    {
        var px = Raster.Line(new(0, 0), new(5, 2)).ToArray();
        CollectionAssert.AreEqual(
            new Vector2Int[] { new(0, 0), new(1, 0), new(2, 1), new(3, 1), new(4, 2), new(5, 2) }, px);
    }

    [Test]
    public void Linhas_TodosOctantes_ConexasComExtremos()
    {
        var fins = new Vector2Int[] { new(5, 0), new(0, 5), new(2, 7), new(-3, -8), new(-6, 2), new(4, -4), new(-7, -1) };
        foreach (var b in fins)
        {
            var px = Raster.Line(Vector2Int.zero, b).ToArray();
            Assert.AreEqual(Vector2Int.zero, px.First(), $"inicio para {b}");
            Assert.AreEqual(b, px.Last(), $"fim para {b}");
            Assert.IsTrue(Conexa(px), $"buraco na linha para {b}");
            Assert.AreEqual(Mathf.Max(Mathf.Abs(b.x), Mathf.Abs(b.y)) + 1, px.Length, $"tamanho para {b}");
        }
    }

    [Test]
    public void Linha_PontoUnico()
    {
        CollectionAssert.AreEqual(new Vector2Int[] { new(3, 3) }, Raster.Line(new(3, 3), new(3, 3)).ToArray());
    }

    static int Area(Vector2Int[] poly) => Raster.ScanlineSpans(poly).Sum(s => s.xEnd - s.xStart + 1);

    [Test]
    public void Scanline_Quadrado4x4_16Pixels()
    {
        Assert.AreEqual(16, Area(new Vector2Int[] { new(0, 0), new(4, 0), new(4, 4), new(0, 4) }));
    }

    [Test]
    public void Scanline_Concavo_U()
    {
        // U: 6x4 com um entalhe 2x2 no topo do meio -> 24 - 4 = 20
        var u = new Vector2Int[] { new(0, 0), new(6, 0), new(6, 4), new(4, 4), new(4, 2), new(2, 2), new(2, 4), new(0, 4) };
        Assert.AreEqual(20, Area(u));
        var linha3 = Raster.ScanlineSpans(u).Where(s => s.y == 3).ToArray();
        CollectionAssert.AreEqual(new[] { (3, 0, 1), (3, 4, 5) }, linha3);
    }

    [Test]
    public void Scanline_VerticeNaLinha_NaoDuplica()
    {
        // losango: vértices laterais em y=4 caem exatamente numa scanline
        var losango = new Vector2Int[] { new(4, 0), new(8, 4), new(4, 8), new(0, 4) };
        var spans = Raster.ScanlineSpans(losango).ToArray();
        Assert.IsTrue(spans.GroupBy(s => s.y).All(g => g.Count() == 1), "mais de um span numa linha");
        CollectionAssert.Contains(spans, (4, 0, 7));
    }

    [Test]
    public void Scanline_Degenerado_Vazio()
    {
        Assert.IsEmpty(Raster.ScanlineSpans(null));
        Assert.IsEmpty(Raster.ScanlineSpans(new Vector2Int[] { new(0, 0), new(5, 5) }));
        Assert.IsEmpty(Raster.ScanlineSpans(new Vector2Int[] { new(0, 0), new(5, 0), new(9, 0) }));
    }

    static readonly Vector2 A = new(0, 0), B = new(1, 2), C = new(3, 3), D = new(4, 0);

    [Test]
    public void Bezier_Extremos()
    {
        Assert.AreEqual(A, Raster.BezierPoint(A, B, C, D, 0));
        Assert.AreEqual(D, Raster.BezierPoint(A, B, C, D, 1));
    }

    [Test]
    public void Bezier_Meio()
    {
        var p = Raster.BezierPoint(new(0, 0), new(0, 4), new(4, 4), new(4, 0), 0.5f);
        Assert.AreEqual(2f, p.x, 1e-5);
        Assert.AreEqual(3f, p.y, 1e-5);
    }

    [Test]
    public void Bezier_TangenteInicialParalelaAP1menosP0()
    {
        var t = Raster.BezierTangent(A, B, C, D, 0);
        Assert.AreEqual(1f, Vector2.Dot(t.normalized, (B - A).normalized), 1e-5);
        Assert.AreEqual(3 * (B - A), t);
    }

    [Test]
    public void BezierJoin_EmendaC1()
    {
        Vector2 a0 = new(0, 0), a1 = new(1, 2), a2 = new(3, 1), a3 = new(4, 0);
        Vector2 b1 = Raster.BezierJoin(a2, a3), b2 = new(7, 3), b3 = new(9, 1);
        Assert.AreEqual(new Vector2(5, -1), b1);
        Assert.AreEqual(Raster.BezierTangent(a0, a1, a2, a3, 1), Raster.BezierTangent(a3, b1, b2, b3, 0));
    }

    static readonly Rect Janela = new(-1, -1, 3, 10);   // x ∈ [-1, 2], y ∈ [-1, 9]

    static void AssertPontos(Vector2[] esperado, List<Vector2> obtido)
    {
        Assert.AreEqual(esperado.Length, obtido.Count, "quantidade de vértices");
        for (int i = 0; i < esperado.Length; i++)
        {
            Assert.AreEqual(esperado[i].x, obtido[i].x, 1e-4, $"x do vértice {i}");
            Assert.AreEqual(esperado[i].y, obtido[i].y, 1e-4, $"y do vértice {i}");
        }
    }

    [Test]
    public void Clip_TodoDentro_Inalterado()
    {
        var q = new Vector2[] { new(0, 0), new(1, 0), new(1, 1), new(0, 1) };
        AssertPontos(q, Raster.ClipPolygon(q, Janela));
    }

    [Test]
    public void Clip_TodoFora_Vazio()
    {
        Assert.IsEmpty(Raster.ClipPolygon(new Vector2[] { new(5, 5), new(6, 5), new(6, 6) }, Janela));
    }

    [Test]
    public void Clip_TrianguloCortadoNaDireita()
    {
        var tri = new Vector2[] { new(0, 0), new(4, 0), new(0, 4) };
        AssertPontos(new Vector2[] { new(0, 0), new(2, 0), new(2, 2), new(0, 4) }, Raster.ClipPolygon(tri, Janela));
    }

    [Test]
    public void Clip_Vazio_Vazio()
    {
        Assert.IsEmpty(Raster.ClipPolygon(new List<Vector2>(), Janela));
    }

    static readonly Rect J = Rect.MinMaxRect(0, 0, 10, 5);

    [Test]
    public void CodigoRegiao_BitsPorLado()
    {
        Assert.AreEqual(0, Raster.CodigoRegiao(new(5, 2), J));
        Assert.AreEqual(1, Raster.CodigoRegiao(new(-1, 2), J));
        Assert.AreEqual(2 | 8, Raster.CodigoRegiao(new(11, 6), J));
    }

    [Test]
    public void ClipLine_AceitacaoTrivial()
    {
        Assert.IsTrue(Raster.ClipLine(new(1, 1), new(9, 4), J, out var a, out var b));
        Assert.AreEqual(new Vector2(1, 1), a);
        Assert.AreEqual(new Vector2(9, 4), b);
    }

    [Test]
    public void ClipLine_RejeicaoTrivial() => Assert.IsFalse(Raster.ClipLine(new(-5, 1), new(-1, 4), J, out _, out _));

    [Test]
    public void ClipLine_CruzaDuasBordas()
    {
        Assert.IsTrue(Raster.ClipLine(new(-2, -1), new(12, 6), J, out var a, out var b));   // y = 0,5x
        Assert.AreEqual(0f, a.x, 1e-4); Assert.AreEqual(0f, a.y, 1e-4);
        Assert.AreEqual(10f, b.x, 1e-4); Assert.AreEqual(5f, b.y, 1e-4);
    }

    [Test]
    public void ClipLine_ForaPassandoPeloCanto_Rejeita() => Assert.IsFalse(Raster.ClipLine(new(-1, 4.5f), new(1, 7), J, out _, out _));   // y = 1,25x + 5,75: passa acima do canto (0,5)

    [Test]
    public void ClipLine_SobreABorda()
    {
        Assert.IsTrue(Raster.ClipLine(new(0, -3), new(0, 8), J, out var a, out var b));
        Assert.AreEqual(new Vector2(0, 0), a);
        Assert.AreEqual(new Vector2(0, 5), b);
    }

    [Test]
    public void JanelaViewport_CantosECentro()
    {
        var v = Rect.MinMaxRect(0, 0, 256, 96);
        Assert.AreEqual(new Vector2(0, 0), Raster.JanelaViewport(new(0, 0), J, v));
        Assert.AreEqual(new Vector2(256, 96), Raster.JanelaViewport(new(10, 5), J, v));
        Assert.AreEqual(new Vector2(128, 48), Raster.JanelaViewport(new(5, 2.5f), J, v));
    }
}
