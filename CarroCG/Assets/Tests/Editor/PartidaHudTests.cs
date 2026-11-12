using NUnit.Framework;

public class PartidaHudTests
{
    [Test]
    public void BarraCor_VerdeCheioVermelhoVazio()
    {
        var cheio = Partida.BarraCor(1);
        Assert.Greater(cheio.g, cheio.r);
        var meio = Partida.BarraCor(0.5f);
        Assert.Greater(meio.r, 0.8f);
        Assert.Greater(meio.g, 0.6f);
        var vazio = Partida.BarraCor(0);
        Assert.Greater(vazio.r, vazio.g);
    }
}
