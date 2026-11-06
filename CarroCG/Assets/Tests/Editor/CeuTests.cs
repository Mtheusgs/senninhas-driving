using NUnit.Framework;
using UnityEngine;

public class CeuTests
{
    [Test]
    public void MeioDia_SolEmCimaECeuAzul()
    {
        var e = Ceu.Estado(0.25f);
        Assert.Greater(e.dirLuz.y, 0.95f);
        Assert.Less(e.noite, 0.05f);
        Assert.Greater(e.corCeuTopo.b, e.corCeuTopo.r);
    }

    [Test]
    public void MeiaNoite_LuzDaLuaFraca()
    {
        var e = Ceu.Estado(0.75f);
        Assert.Greater(e.noite, 0.95f);
        Assert.Less(e.corLuz.maxColorComponent, 0.6f);
        Assert.Greater(e.dirLuz.y, 0.9f, "luz da lua vem de cima à meia-noite");
    }

    [Test]
    public void PorDoSol_HorizonteAlaranjado()
    {
        var e = Ceu.Estado(0.49f);
        Assert.Greater(e.corCeuHorizonte.r, e.corCeuHorizonte.b);
    }

    [Test]
    public void LuzSempreDeCima()
    {
        for (float t = 0; t < 1; t += 0.005f) Assert.GreaterOrEqual(Ceu.Estado(t).dirLuz.y, 0.15f - 1e-4f, $"t={t}");
    }

    [Test]
    public void DirecaoDaLuz_Continua()
    {
        for (float t = 0; t < 1; t += 0.001f)
        {
            float salto = Vector2.Angle(Ceu.Estado(t).dirLuz, Ceu.Estado(t + 0.001f).dirLuz);
            Assert.Less(salto, 3f, $"salto de {salto:0.0}° em t={t}");
        }
    }
}
