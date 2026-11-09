using System.Linq;
using NUnit.Framework;

public class TecnicasCGTests
{
    [Test]
    public void NoveTecnicasDaSpec()
    {
        CollectionAssert.AreEqual(new[]
        {
            "Projeção paralela e perspectiva", "Rasterização", "Preenchimento", "Curva de Bézier", "Recorte",
            "Iluminação e sombreamento", "Translação", "Rotação", "Espelhamento",
        }, TecnicasCG.Lista.Select(t => t.nome));
    }

    [Test]
    public void SelosConformeOQueOJogoTemHoje()
    {
        var comAlgoNoJogo = TecnicasCG.Lista.Where(t => t.lugares.Any(l => l.noJogo)).Select(t => t.nome);
        CollectionAssert.AreEquivalent(
            new[] { "Projeção paralela e perspectiva", "Rasterização", "Preenchimento", "Curva de Bézier", "Recorte", "Iluminação e sombreamento", "Translação", "Rotação", "Espelhamento" },
            comAlgoNoJogo);
    }

    [Test]
    public void CadaTecnicaTemOndeAparece() =>
        Assert.IsTrue(TecnicasCG.Lista.All(t => t.lugares.Length > 0));

    [Test]
    public void Texto_TemTodosOsNomesESelos()
    {
        var texto = TecnicasCG.Texto();
        foreach (var t in TecnicasCG.Lista) StringAssert.Contains(t.nome, texto);
        StringAssert.Contains("no jogo", texto);
        StringAssert.Contains("em breve", texto);
    }
}
