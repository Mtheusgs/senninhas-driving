using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

public class ReflexoGeometriaTests
{
    static readonly Vector2[] Uvs = { new(0, 0), new(1, 0), new(1, 1), new(0, 1) };
    static readonly Rect Agua = Rect.MinMaxRect(0, -10, 20, 0);    // linha da água em y=0

    static Vector2[] Quad(float x, float y, float w, float h) =>
        new[] { new Vector2(x, y), new(x + w, y), new(x + w, y + h), new(x, y + h) };

    [Test]
    public void QuadAbaixoDaLinha_SoEspelhado()
    {
        var v = new List<Vector2>(); var uv = new List<Vector2>();
        Assert.IsTrue(ReflexoGeometria.Quad(Quad(2, 1, 2, 1), Uvs, 0, Agua, v, uv));
        Assert.AreEqual(4, v.Count);
        CollectionAssert.AreEquivalent(new Vector2[] { new(2, -1), new(4, -1), new(4, -2), new(2, -2) }, v);
    }

    [Test]
    public void NadaAcimaDaLinha()
    {
        var v = new List<Vector2>(); var uv = new List<Vector2>();
        // quad inclinado que atravessa a linha da água e passa da borda direita
        var q = new[] { new Vector2(18, -1), new(23, 0.5f), new(22, 3), new(17, 2) };
        Assert.IsTrue(ReflexoGeometria.Quad(q, Uvs, 0, Agua, v, uv));
        foreach (var p in v) Assert.IsTrue(p.y <= Agua.yMax + 1e-4f && p.x <= Agua.xMax + 1e-4f && p.x >= Agua.xMin - 1e-4f, p.ToString());
        Assert.AreEqual(v.Count, uv.Count);
    }

    [Test]
    public void QuadAcimaDaAgua_TodoRecortado()
    {
        var v = new List<Vector2>(); var uv = new List<Vector2>();
        Assert.IsFalse(ReflexoGeometria.Quad(Quad(2, -5, 2, 1), Uvs, 0, Agua, v, uv));   // refletido vai para y 4..5
        Assert.IsEmpty(v);
    }

    [Test]
    public void UVSegueOVertice()
    {
        var v = new List<Vector2>(); var uv = new List<Vector2>();
        ReflexoGeometria.Quad(Quad(2, 1, 2, 1), Uvs, 0, Agua, v, uv);
        Assert.AreEqual(Vector2.zero, uv[v.IndexOf(new Vector2(2, -1))]);
        Assert.AreEqual(Vector2.one, uv[v.IndexOf(new Vector2(4, -2))]);
    }
}
