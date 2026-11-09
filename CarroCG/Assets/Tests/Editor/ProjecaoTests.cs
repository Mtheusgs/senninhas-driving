using NUnit.Framework;
using UnityEngine;

public class ProjecaoTests
{
    [Test]
    public void Ortografica_PreservaXY()
    {
        var p = Projecao.Projetar(Projecao.Ortografica(), new Vector3(3, -2, 40));
        Assert.AreEqual(3f, p.x, 1e-5);
        Assert.AreEqual(-2f, p.y, 1e-5);
    }

    [Test]
    public void Perspectiva_DivisaoHomogenea()
    {
        var p = Projecao.Projetar(Projecao.Perspectiva(10), new Vector3(4, 2, 10));     // d/(d+z) = 0,5
        Assert.AreEqual(2f, p.x, 1e-5);
        Assert.AreEqual(1f, p.y, 1e-5);
    }

    [Test]
    public void Perspectiva_ZMaiorFicaMaisPertoDoCentro()
    {
        var m = Projecao.Perspectiva(10);
        Assert.Less(Projecao.Projetar(m, new Vector3(5, 0, 60)).x, Projecao.Projetar(m, new Vector3(5, 0, 5)).x);
    }

    [Test]
    public void Perspectiva_PontoNoObservador_NaoExplode()
    {
        var p = Projecao.Projetar(Projecao.Perspectiva(10), new Vector3(1, 1, -10));
        Assert.IsFalse(float.IsNaN(p.x) || float.IsInfinity(p.x));
    }

    [Test]
    public void Interpolar_MeioFicaEntreAsDuas()
    {
        var v = new Vector3(4, 0, 10);
        float orto = Projecao.Projetar(Projecao.Ortografica(), v).x;
        float persp = Projecao.Projetar(Projecao.Perspectiva(10), v).x;
        float meio = Projecao.Projetar(Projecao.Interpolar(Projecao.Ortografica(), Projecao.Perspectiva(10), 0.5f), v).x;
        Assert.Greater(meio, persp);
        Assert.Less(meio, orto);
    }

    [Test]
    public void FatorParallax_PerspectivaDiminuiComAProfundidade_ParalelaUm()
    {
        Assert.AreEqual(1f, Projecao.FatorParallax(0, 10, true), 1e-5);
        Assert.AreEqual(0.5f, Projecao.FatorParallax(10, 10, true), 1e-5);
        Assert.Less(Projecao.FatorParallax(60, 10, true), Projecao.FatorParallax(20, 10, true));
        Assert.AreEqual(1f, Projecao.FatorParallax(60, 10, false));
    }
}
