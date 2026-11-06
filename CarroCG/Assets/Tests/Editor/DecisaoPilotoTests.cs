using NUnit.Framework;

public class DecisaoPilotoTests
{
    [Test]
    public void SubidaForte_NoChao_ContinuaAcelerando() => Assert.AreEqual(1f, new DecisaoPiloto().Acelerador(35, true, 0));

    [Test]
    public void Empinando_NoChao_Alivia() => Assert.AreEqual(0f, new DecisaoPiloto().Acelerador(60, true, 0));

    [Test]
    public void NoAr_BicoParaCima_Abaixa() => Assert.Less(new DecisaoPiloto().Acelerador(30, false, 0), 0f);

    [Test]
    public void NoAr_BicoParaBaixo_Levanta() => Assert.Greater(new DecisaoPiloto().Acelerador(-30, false, 0), 0f);

    [Test]
    public void SubidaLenta_NaoRecomeca()
    {
        var d = new DecisaoPiloto();
        for (int i = 0; i < 100; i++) Assert.IsFalse(d.DeveRecomecar(0.1f, 30, true, 1.0f), $"recomeçou aos {i * 0.1f:0.0} s subindo devagar");
    }

    [Test]
    public void ParadoNoChao_RecomecaDepoisDeQuatroSegundos()
    {
        var d = new DecisaoPiloto();
        bool r = false;
        for (int i = 0; i < 38; i++) r = d.DeveRecomecar(0.1f, 5, true, 0.1f);
        Assert.IsFalse(r);
        for (int i = 0; i < 4; i++) r = d.DeveRecomecar(0.1f, 5, true, 0.1f);
        Assert.IsTrue(r);
    }

    [Test]
    public void Virado_Recomeca()
    {
        var d = new DecisaoPiloto();
        bool r = false;
        for (int i = 0; i < 17; i++) r = d.DeveRecomecar(0.1f, 170, true, 0);
        Assert.IsTrue(r);
    }
}
