using NUnit.Framework;

public class DetectorCapotagemTests
{
    static bool Por(DetectorCapotagem d, float segundos, float angulo, bool noChao, float velocidade)
    {
        bool r = false;
        for (float t = 0; t < segundos; t += 0.1f) r = d.Atualizar(0.1f, angulo, noChao, velocidade);
        return r;
    }

    [Test]
    public void DePe_Andando_Nunca() => Assert.IsFalse(Por(new DetectorCapotagem(), 10, 5, true, 8));

    [Test]
    public void DeCabecaParaBaixo_DepoisDeUmSegundoEMeio()
    {
        var d = new DetectorCapotagem();
        Assert.IsFalse(Por(d, 1.0f, 170, true, 0));
        Assert.IsTrue(Por(d, 0.7f, 170, true, 0));
    }

    [Test]
    public void DesvirarZeraOContador()
    {
        var d = new DetectorCapotagem();
        Por(d, 1.0f, -150, true, 0);
        Por(d, 0.2f, 10, true, 0);
        Assert.IsFalse(Por(d, 1.0f, -150, true, 0));
    }

    [Test]
    public void PresoNaPonta_RodasNoArEParado()
    {
        var d = new DetectorCapotagem();
        Assert.IsFalse(Por(d, 1.0f, 70, false, 0.1f));
        Assert.IsTrue(Por(d, 0.7f, 70, false, 0.1f));
    }

    [Test]
    public void PuloNoAr_NaoEhCapotagem() => Assert.IsFalse(Por(new DetectorCapotagem(), 3, 40, false, 9));

    [Test]
    public void ParadoNoChaoDePe_NaoEhCapotagem() => Assert.IsFalse(Por(new DetectorCapotagem(), 5, 3, true, 0));
}
