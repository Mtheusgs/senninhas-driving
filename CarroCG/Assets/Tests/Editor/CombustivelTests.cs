using NUnit.Framework;

public class CombustivelTests
{
    static Combustivel Novo()
    {
        var c = new Combustivel { maximo = 100, consumoPorSegundo = 1, consumoAcelerando = 2 };
        c.Encher();
        return c;
    }

    [Test]
    public void ConsomePorTempo()
    {
        var c = Novo();
        c.Consumir(10, 0);
        Assert.AreEqual(90f, c.Atual, 1e-4);
    }

    [Test]
    public void AcelerandoConsomeMais()
    {
        var c = Novo();
        c.Consumir(10, 1);
        Assert.AreEqual(70f, c.Atual, 1e-4);
    }

    [Test]
    public void Galao_EncheAteOMaximo()
    {
        var c = Novo();
        c.Consumir(10, 0);
        c.Abastecer(50);
        Assert.AreEqual(100f, c.Atual, 1e-4);
        Assert.AreEqual(1f, c.Fracao, 1e-4);
    }

    [Test]
    public void NaoFicaNegativo_EFicaVazio()
    {
        var c = Novo();
        c.Consumir(1000, 1);
        Assert.AreEqual(0f, c.Atual);
        Assert.IsTrue(c.Vazio);
    }

    [Test]
    public void Pane_SoDepoisDeDoisSegundosParado()
    {
        var c = Novo();
        c.Consumir(1000, 0);
        Assert.IsFalse(c.VerificarPane(1.5f, 0));
        Assert.IsTrue(c.VerificarPane(0.6f, 0));
    }

    [Test]
    public void Pane_AndarZeraOContador()
    {
        var c = Novo();
        c.Consumir(1000, 0);
        c.VerificarPane(1.5f, 0);
        c.VerificarPane(0.1f, 3);
        Assert.IsFalse(c.VerificarPane(0.6f, 0));
    }

    [Test]
    public void Pane_ComCombustivelNunca()
    {
        Assert.IsFalse(Novo().VerificarPane(10, 0));
    }
}
