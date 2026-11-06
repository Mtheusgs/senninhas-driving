using NUnit.Framework;
using UnityEngine;

public class SombraTests
{
    static float Plano(float x) => 0;

    [Test]
    public void Projetar_CaiNaSuperficieDoLadoOpostoAoSol()
    {
        var p = Sombra.Projetar(new Vector2(0, 2), new Vector2(1, 1), Plano).Value;
        Assert.AreEqual(-2f, p.x, 0.02f);
        Assert.AreEqual(0f, p.y, 0.02f);
    }

    [Test]
    public void Projetar_SolBaixo_SombraMaisLonge()
    {
        float alto = Sombra.Projetar(new Vector2(0, 2), new Vector2(1, 1), Plano).Value.x;
        float baixo = Sombra.Projetar(new Vector2(0, 2), new Vector2(1, 0.3f), Plano).Value.x;
        Assert.Less(baixo, alto);
        Assert.AreEqual(-2f / 0.3f, baixo, 0.05f);
    }

    [Test]
    public void SolNoHorizonte_SemSombra() => Assert.IsNull(Sombra.Projetar(new Vector2(0, 2), new Vector2(1, 0), Plano));

    [Test]
    public void ForaDoTerreno_SemSombra() => Assert.IsNull(Sombra.Projetar(new Vector2(0, 2), Vector2.up, x => float.NaN));

    [Test]
    public void Faixa_ExtensaoDosPontosProjetados()
    {
        var f = Sombra.Faixa(new[] { new Vector2(0, 1), new Vector2(1, 1) }, Vector2.up, Plano).Value;
        Assert.AreEqual(0f, f.xMin, 0.02f);
        Assert.AreEqual(1f, f.xMax, 0.02f);
    }

    [Test]
    public void Faixa_LimitadaAoMaximo()
    {
        // sol baixo: (0,1) projeta em x≈−12,5 e (0,2,5) em x≈−31 → faixa de ~19 cortada em 12
        var f = Sombra.Faixa(new[] { new Vector2(0, 1), new Vector2(0, 2.5f) }, new Vector2(1, 0.08f), Plano, 12f).Value;
        Assert.LessOrEqual(f.xMax - f.xMin, 12f + 1e-3f);
    }
}
