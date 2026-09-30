using NUnit.Framework;
using UnityEngine;

public class CarFollowerTests
{
    static CarFollower Novo(Vector2 a, Vector2 b, Vector2 c, Vector2 d)
    {
        var pista = new GameObject("pista").AddComponent<BezierPath>();
        Transform P(Vector2 v) { var t = new GameObject("p").transform; t.position = v; return t; }
        pista.p0 = P(a); pista.p1 = P(b); pista.p2 = P(c); pista.p3 = P(d);
        var carro = new GameObject("carro").AddComponent<CarFollower>();
        carro.pista = pista;
        return carro;
    }

    [Test]
    public void Ida_SegueAPistaOlhandoParaFrente()
    {
        var c = Novo(new(0, 0), new(1, 0), new(2, 0), new(3, 0));
        c.Posicionar(0.5f);
        Assert.AreEqual(1.5f, c.transform.position.x, 1e-4);
        Assert.Greater(c.transform.up.y, 0.99f, "teto para cima");
        Assert.Greater(c.transform.TransformVector(Vector3.right).x, 0, "frente para a direita");
    }

    [Test]
    public void Volta_EspelhadoNaoFicaDeCabecaParaBaixo()
    {
        var c = Novo(new(0, 0), new(1, 0), new(2, 0), new(3, 0));
        c.Espelhar();
        c.Posicionar(0.5f);
        Assert.Greater(c.transform.up.y, 0.99f, "teto para cima");
        Assert.Less(c.transform.TransformVector(Vector3.right).x, 0, "frente para a esquerda");
    }

    [Test]
    public void TangenteNula_MantemUltimoAngulo()
    {
        var c = Novo(new(0, 0), new(0, 0), new(0, 5), new(5, 5));   // P1 == P0 -> Tangent(0) = 0
        c.Posicionar(0.5f);
        float angulo = c.transform.eulerAngles.z;
        c.Posicionar(0f);
        Assert.AreEqual(angulo, c.transform.eulerAngles.z, 1e-3);
    }
}
