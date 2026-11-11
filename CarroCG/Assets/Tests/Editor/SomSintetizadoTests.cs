using System.Linq;
using NUnit.Framework;

public class SomSintetizadoTests
{
    const int Taxa = 44100;

    [Test]
    public void Sons_DentroDeMenosUmAUm_ENaoVazios()
    {
        foreach (var s in new[] { SomSintetizado.Motor(Taxa), SomSintetizado.Moeda(Taxa), SomSintetizado.Galao(Taxa),
                                  SomSintetizado.Batida(Taxa), SomSintetizado.Fim(Taxa) })
        {
            Assert.Greater(s.Length, 1000);
            Assert.IsTrue(s.All(a => a >= -1 && a <= 1));
            Assert.Greater(s.Max(a => System.Math.Abs(a)), 0.1f, "mudo");
        }
    }

    [Test]
    public void Motor_EmendaSemEstalo()
    {
        var a = SomSintetizado.Motor(Taxa);
        Assert.Less(System.Math.Abs(a[0] - a[^1]), 0.05f);
    }

    [Test]
    public void Pitch_ParadoEMaximo()
    {
        Assert.AreEqual(0.6f, SomSintetizado.PitchMotor(0), 1e-5f);
        Assert.AreEqual(2.0f, SomSintetizado.PitchMotor(2400), 1e-5f);
        Assert.AreEqual(2.0f, SomSintetizado.PitchMotor(-5000), 1e-5f);
    }

    [Test]
    public void Moeda_Duracao() => Assert.AreEqual(0.24f * Taxa, SomSintetizado.Moeda(Taxa).Length, 1);
}
