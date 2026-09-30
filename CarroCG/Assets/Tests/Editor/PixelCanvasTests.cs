using System.Collections;
using NUnit.Framework;
using UnityEngine;

public class PixelCanvasTests
{
    static PixelCanvas Novo(params PixelCanvas.Poly[] polys)
    {
        var go = new GameObject("carro-teste");
        go.AddComponent<SpriteRenderer>();
        var c = go.AddComponent<PixelCanvas>();
        c.largura = 10; c.altura = 10;
        c.corContorno = Color.black;
        c.poligonos = polys;
        c.Inicializar();
        return c;
    }

    static void Rodar(IEnumerator e) { while (e.MoveNext()) { } }

    static PixelCanvas.Poly Quadrado(int a, int b, Color cor) => new PixelCanvas.Poly
    {
        nome = "q", cor = cor,
        vertices = new Vector2Int[] { new(a, a), new(b, a), new(b, b), new(a, b) }
    };

    [Test]
    public void Preenchimento_NaoSobrescreveContorno()
    {
        var c = Novo(Quadrado(1, 8, Color.red));
        Rodar(c.DesenharContornos(0));
        Rodar(c.Preencher(0));
        Assert.AreEqual(Color.black, c.Textura.GetPixel(1, 1), "contorno");
        Assert.AreEqual(Color.red, c.Textura.GetPixel(4, 4), "interior");
        Assert.AreEqual(0f, c.Textura.GetPixel(0, 0).a, "fora do polígono fica transparente");
    }

    [Test]
    public void ForaDaTextura_NaoLancaEPreencheDentro()
    {
        var c = Novo(Quadrado(-2, 12, Color.red));
        Assert.DoesNotThrow(() => { Rodar(c.DesenharContornos(0)); Rodar(c.Preencher(0)); });
        Assert.AreEqual(Color.red, c.Textura.GetPixel(5, 5));
        Assert.AreEqual(Color.red, c.Textura.GetPixel(0, 0));
    }
}
