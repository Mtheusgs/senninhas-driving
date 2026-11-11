using System.Linq;
using NUnit.Framework;
using UnityEngine;

public class ModeloF1Tests
{
    [Test]
    public void TemEntre50E90Vertices() => Assert.That(ModeloF1.Vertices.Length, Is.InRange(50, 90));

    [Test]
    public void ArestasValidas()
    {
        int n = ModeloF1.Vertices.Length;
        foreach (var a in ModeloF1.Arestas)
        {
            Assert.That(a.x, Is.InRange(0, n - 1));
            Assert.That(a.y, Is.InRange(0, n - 1));
            Assert.AreNotEqual(a.x, a.y);
        }
    }

    [Test]
    public void SimetricoEmZ()
    {
        foreach (var v in ModeloF1.Vertices)
            Assert.IsTrue(ModeloF1.Vertices.Any(u => (u - new Vector3(v.x, v.y, -v.z)).sqrMagnitude < 1e-8f), v.ToString());
    }

    [Test]
    public void RotacaoZero_VistaLateral()
    {
        var m = Projecao.Ortografica() * ModeloF1.Rotacao(0, 0);
        var v = ModeloF1.Vertices[0];
        Assert.AreEqual(Projecao.Projetar(m, v), Projecao.Projetar(m, new Vector3(v.x, v.y, -v.z)));
        Assert.AreEqual((Vector2)v, Projecao.Projetar(m, v));
    }

    [Test]
    public void Rotacao90_LateralViraProfundidade()
    {
        var p = ModeloF1.Rotacao(90, 0).MultiplyPoint3x4(new Vector3(1, 0, 0));
        Assert.AreEqual(0, p.x, 1e-5f);
        Assert.AreEqual(1, Mathf.Abs(p.z), 1e-5f);
    }

    [Test]
    public void NuncaAtrasDoObservador()
    {
        const float d = 4;
        for (int y = 0; y < 360; y += 15)
            for (int x = -30; x <= 30; x += 15)
                foreach (var v in ModeloF1.Vertices)
                    Assert.Greater(ModeloF1.Rotacao(y, x).MultiplyPoint3x4(v).z, -d);
    }
}
