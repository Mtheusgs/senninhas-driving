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
}
