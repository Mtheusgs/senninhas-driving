using NUnit.Framework;
using UnityEngine;

public class SombreamentoTests
{
    static readonly Vector2 Cima = Vector2.up;

    [Test] public void Lambert_NormalParalelaALuz_Maximo() => Assert.AreEqual(1.0f, Sombreamento.Lambert(Cima, Cima, 0.3f, 0.7f), 1e-5);
    [Test] public void Lambert_Perpendicular_SoAmbiente() => Assert.AreEqual(0.3f, Sombreamento.Lambert(Cima, Vector2.right, 0.3f, 0.7f), 1e-5);
    [Test] public void Lambert_Oposta_SoAmbiente() => Assert.AreEqual(0.3f, Sombreamento.Lambert(Cima, Vector2.down, 0.3f, 0.7f), 1e-5);

    [Test]
    public void Phong_EspecularMaximoComReflexoAlinhadoAVisao()
    {
        var n = Vector3.forward; var v = Vector3.forward;
        float alinhado = Sombreamento.Phong(n, Vector3.forward, v, 0, 0, 1, 16);
        float inclinado = Sombreamento.Phong(n, new Vector3(1, 0, 1), v, 0, 0, 1, 16);
        Assert.AreEqual(1f, alinhado, 1e-4);
        Assert.Less(inclinado, 0.1f);
    }

    [Test]
    public void Phong_LuzPorTras_SemDifusaNemEspecular() =>
        Assert.AreEqual(0.2f, Sombreamento.Phong(Vector3.forward, Vector3.back, Vector3.forward, 0.2f, 1, 1, 16), 1e-5);

    // telhado: sobe de (0,0) a (1,1) e desce até (2,0)
    static readonly Vector2[] Telhado = { new(0, 0), new(1, 1), new(2, 0) };
    static readonly Vector2 Diagonal = new Vector2(-1, 1).normalized;     // de frente para a 1ª água

    [Test]
    public void NormaisDosVertices_MediaNoMeio()
    {
        var nv = Sombreamento.NormaisDosVertices(Telhado);
        Assert.AreEqual(3, nv.Length);
        Assert.AreEqual(0f, nv[1].x, 1e-5);
        Assert.AreEqual(1f, nv[1].y, 1e-5);
    }

    [Test]
    public void Flat_ConstanteNoSegmento() =>
        Assert.AreEqual(Sombreamento.DifusaEm(ModoSombreamento.Flat, Telhado, 0.2f, Diagonal),
                        Sombreamento.DifusaEm(ModoSombreamento.Flat, Telhado, 0.8f, Diagonal), 1e-5);

    [Test]
    public void Gouraud_MeioEhMediaDosExtremos()
    {
        float a = Sombreamento.DifusaEm(ModoSombreamento.Gouraud, Telhado, 0f, Diagonal);
        float b = Sombreamento.DifusaEm(ModoSombreamento.Gouraud, Telhado, 1f, Diagonal);
        Assert.AreEqual((a + b) / 2, Sombreamento.DifusaEm(ModoSombreamento.Gouraud, Telhado, 0.5f, Diagonal), 1e-5);
    }

    [Test]
    public void Phong_NormalInterpoladaDiferenteDeGouraud()
    {
        var nv = Sombreamento.NormaisDosVertices(Telhado);
        float esperado = Mathf.Max(0, Vector2.Dot(Vector2.Lerp(nv[0], nv[1], 0.5f).normalized, Diagonal));
        float phong = Sombreamento.DifusaEm(ModoSombreamento.Phong, Telhado, 0.5f, Diagonal);
        Assert.AreEqual(esperado, phong, 1e-5);
        Assert.AreNotEqual(Sombreamento.DifusaEm(ModoSombreamento.Gouraud, Telhado, 0.5f, Diagonal), phong);
    }

    [Test]
    public void LuzPorColuna_UmaCorPorColunaComAmbienteNaSombra()
    {
        var luz = Sombreamento.LuzPorColuna(ModoSombreamento.Flat, Telhado, new Rect(0, -1, 2, 3), 10, 20,
                                            Diagonal, Color.white, new Color(0.2f, 0.2f, 0.2f), 0.6f);
        Assert.AreEqual(20, luz.Length);
        Assert.Greater(luz[5].r, luz[15].r, "água virada para a luz deveria ser mais clara");
        Assert.AreEqual((Color32)new Color(0.2f, 0.2f, 0.2f), luz[15], "água de costas: só ambiente");
    }

    [Test]
    public void GouraudEPhong_DiferemNoBrilhoEspecular()
    {
        // com especular, interpolar a intensidade (Gouraud) ou a normal (Phong) dá resultados visivelmente diferentes
        float maior = 0;
        for (int a = 0; a <= 180; a += 2)
        {
            var l = new Vector2(Mathf.Cos(a * Mathf.Deg2Rad), Mathf.Sin(a * Mathf.Deg2Rad));
            for (float x = 0.05f; x < 2; x += 0.05f)
                maior = Mathf.Max(maior, Mathf.Abs(Sombreamento.EspecularEm(ModoSombreamento.Phong, Telhado, x, l, 12)
                                                 - Sombreamento.EspecularEm(ModoSombreamento.Gouraud, Telhado, x, l, 12)));
        }
        Assert.Greater(maior, 0.2f);
    }

    [Test]
    public void Especular_ZeroNaFaceDeCostas() =>
        Assert.AreEqual(0f, Sombreamento.EspecularEm(ModoSombreamento.Flat, Telhado, 1.5f, Diagonal, 12), 1e-6);
}
