using System.Linq;
using NUnit.Framework;
using UnityEngine;

public class TerrenoLagoTests
{
    static Trecho[] Gerar(bool lagos, int n)
    {
        var g = new TerrenoGerador(7) { lagos = lagos };
        return Enumerable.Range(0, n).Select(_ => g.Proximo()).ToArray();
    }

    static int[] Lagos(Trecho[] ts) => Enumerable.Range(0, ts.Length).Where(i => ts[i].lago).ToArray();

    [Test]
    public void Lagos_NuncaNosTresPrimeiros_ECada8a12()
    {
        var idx = Lagos(Gerar(true, 200));
        Assert.Greater(idx.Length, 10);
        Assert.GreaterOrEqual(idx[0], 4);
        for (int i = 1; i < idx.Length; i++) Assert.That(idx[i] - idx[i - 1], Is.InRange(8, 12));
    }

    [Test]
    public void TrechoDoLago_Horizontal()
    {
        var ts = Gerar(true, 60);
        foreach (int i in Lagos(ts))
        {
            var t = ts[i];
            Assert.AreEqual(t.p0.y, t.p1.y, 1e-4f);
            Assert.AreEqual(t.p0.y, t.p2.y, 1e-4f);
            Assert.AreEqual(t.p0.y, t.p3.y, 1e-4f);
        }
    }

    [Test]
    public void TrechoAntesDoLago_TerminaHorizontal()
    {
        var ts = Gerar(true, 60);
        foreach (int i in Lagos(ts)) Assert.AreEqual(ts[i - 1].p3.y, ts[i - 1].p2.y, 1e-4f);
    }

    [Test]
    public void SemLagos_TerrenoIgualAoDeAntes()
    {
        var g = new TerrenoGerador(7);
        var antes = Enumerable.Range(0, 40).Select(_ => g.Proximo()).ToArray();
        CollectionAssert.AreEqual(antes, Gerar(false, 40));
        Assert.IsEmpty(Lagos(antes));
    }

    [Test]
    public void Lagos_NaoMudamOsTrechosAntesDoPrimeiro()
    {
        var com = Gerar(true, 20);
        int primeiro = Lagos(com)[0];
        CollectionAssert.AreEqual(Gerar(false, primeiro - 1), com.Take(primeiro - 1).ToArray());
    }
}
