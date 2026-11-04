using System.Linq;
using NUnit.Framework;

public class ColetaveisTests
{
    [Test]
    public void PrimeiroTrecho_SemColetaveis()
    {
        Assert.IsEmpty(new PlanoColetaveis().Para(new TerrenoGerador(7).Proximo(), 0));
    }

    [Test]
    public void Moedas_AcimaDaSuperficie()
    {
        var g = new TerrenoGerador(7);
        g.Proximo();
        var t = g.Proximo();
        var curva = TerrenoGerador.Amostrar(t, 64);
        var moedas = new PlanoColetaveis().Para(t, 1).Where(c => c.tipo == TipoColetavel.Moeda).ToArray();
        Assert.AreEqual(5, moedas.Length);
        foreach (var m in moedas) Assert.Greater(m.pos.y, TerrenoChunk.AlturaEm(curva, m.pos.x) + 0.4f);
    }

    [Test]
    public void Galoes_IntervaloCrescente()
    {
        var g = new TerrenoGerador(7);
        var plano = new PlanoColetaveis();
        var comGalao = Enumerable.Range(0, 25)
            .Where(i => plano.Para(g.Proximo(), i).Any(c => c.tipo == TipoColetavel.Galao)).ToArray();
        CollectionAssert.AreEqual(new[] { 2, 5, 9, 14, 20 }, comGalao);
    }
}
