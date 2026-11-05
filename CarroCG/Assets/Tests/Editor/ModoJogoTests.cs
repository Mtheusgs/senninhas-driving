using NUnit.Framework;
using UnityEngine;

public class ModoJogoTests
{
    [Test]
    public void Lua_GravidadeEntre20e25PorCento()
    {
        float f = ModoJogo.Parametros(Modo.Lua).gravidade / ModoJogo.GravidadeTerra;
        Assert.That(f, Is.InRange(0.2f, 0.25f));
    }

    [Test]
    public void Terra_GravidadeNormal() =>
        Assert.AreEqual(ModoJogo.GravidadeTerra, ModoJogo.Parametros(Modo.Terra).gravidade);

    [Test]
    public void Lua_MaisAcidentadaQueATerraDesdeOInicio() =>
        Assert.Greater(ModoJogo.Parametros(Modo.Lua).amplitudeInicial, ModoJogo.Parametros(Modo.Terra).amplitudeInicial);

    [Test]
    public void Terra_IgualAosPadroesDoGerador()
    {
        var p = ModoJogo.Parametros(Modo.Terra);
        var g = new TerrenoGerador(0);
        Assert.AreEqual(g.amplitudeInicial, p.amplitudeInicial);
        Assert.AreEqual(g.ganhoPorTrecho, p.ganhoPorTrecho);
        Assert.AreEqual(g.amplitudeMax, p.amplitudeMax);
    }

    [Test]
    public void Lua_TerrenoJogavel()
    {
        var p = ModoJogo.Parametros(Modo.Lua);
        var g = new TerrenoGerador(p.semente)
            { amplitudeInicial = p.amplitudeInicial, ganhoPorTrecho = p.ganhoPorTrecho, amplitudeMax = p.amplitudeMax };
        for (int i = 0; i < 300; i++)
        {
            var t = g.Proximo();
            Assert.Less(t.p1.x, t.p2.x, $"trecho {i}");
            for (int k = 0; k <= 32; k++)
            {
                var d = Raster.BezierTangent(t.p0, t.p1, t.p2, t.p3, k / 32f);
                Assert.LessOrEqual(Mathf.Abs(d.y / d.x), 1.5f, $"trecho {i}, t={k / 32f}");
            }
        }
    }

    [Test]
    public void Lua_PaletaCinzaECeuPreto()
    {
        var p = ModoJogo.Parametros(Modo.Lua);
        Assert.AreEqual(Color.black, p.ceu);
        Assert.IsTrue(p.ceuLunar);
        Assert.AreEqual(p.paleta.grama.r, p.paleta.grama.g, 0.05f, "chão lunar não é cinza");
        Assert.IsFalse(ModoJogo.Parametros(Modo.Terra).ceuLunar);
    }
}
