using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class AulaGeometriaTests
{
    static Trecho T(Vector2 a, Vector2 b, Vector2 c, Vector2 d) => new Trecho { p0 = a, p1 = b, p2 = c, p3 = d };

    [Test]
    public void Controle_OsQuatroPontos()
    {
        var t = T(new(0, 0), new(1, 2), new(3, 1), new(4, 0));
        CollectionAssert.AreEqual(new Vector3[] { new(0, 0), new(1, 2), new(3, 1), new(4, 0) }, AulaGeometria.Controle(t));
    }

    [Test]
    public void Retangulo_FechadoNosCantos()
    {
        CollectionAssert.AreEqual(
            new Vector3[] { new(1, 2), new(4, 2), new(4, 7), new(1, 7), new(1, 2) },
            AulaGeometria.Retangulo(Rect.MinMaxRect(1, 2, 4, 7)));
    }

    [Test]
    public void Fechado_RepeteOPrimeiroEVazioFicaVazio()
    {
        var p = new List<Vector2> { new(0, 0), new(1, 0), new(0, 1) };
        CollectionAssert.AreEqual(new Vector3[] { new(0, 0), new(1, 0), new(0, 1), new(0, 0) }, AulaGeometria.Fechado(p));
        Assert.IsEmpty(AulaGeometria.Fechado(new List<Vector2>()));
    }

    [Test]
    public void Emenda_JuncaoNoMeioDoEspelhamento()
    {
        var g = new TerrenoGerador(3);
        g.Proximo();
        var a = g.Proximo();
        var c = g.Proximo();
        var e = AulaGeometria.Emenda(a, c);
        Assert.AreEqual(3, e.Length);
        Assert.AreEqual((Vector3)a.p2, e[0]);
        Assert.AreEqual((Vector3)c.p1, e[2]);
        Assert.Less(Vector3.Distance((e[0] + e[2]) / 2, e[1]), 1e-4f, "P1 não é o espelho de P2 em torno da junção");
    }

    [Test]
    public void Quadrado_CentradoEFechado()
    {
        var q = AulaGeometria.Quadrado(new Vector2(2, 3), 2);
        Assert.AreEqual(5, q.Length);
        Assert.AreEqual(q[0], q[4]);
        Assert.AreEqual(new Vector3(1, 2), q[0]);
        Assert.AreEqual(new Vector3(3, 4), q[2]);
    }
}
