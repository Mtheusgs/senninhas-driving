using NUnit.Framework;
using UnityEngine;

public class TerrenoComponenteTests
{
    Terreno terreno;

    [SetUp]
    public void Criar()
    {
        terreno = new GameObject("terreno-teste").AddComponent<Terreno>();
        terreno.ppu = 8;            // rápido; a lógica não depende da resolução
        terreno.Reiniciar();
    }

    [TearDown]
    public void Destruir()
    {
        terreno.Reiniciar();
        Object.DestroyImmediate(terreno.gameObject);
    }

    [Test]
    public void Trecho_ColliderSegueACurva()
    {
        var c = terreno.GerarProximo();
        var col = c.go.GetComponent<EdgeCollider2D>();
        Assert.AreEqual(c.curva.Length, col.points.Length);
        for (int i = 0; i < c.curva.Length; i++)
        {
            var p = (Vector2)c.go.transform.position + col.points[i];
            Assert.AreEqual(c.curva[i].x, p.x, 1e-4, $"ponto {i}");
            Assert.AreEqual(c.curva[i].y, p.y, 1e-4, $"ponto {i}");
        }
    }

    [Test]
    public void Atualizar_GeraTrechosAFrente()
    {
        terreno.Atualizar(0);
        Assert.GreaterOrEqual(terreno.chunks[^1].trecho.p3.x, terreno.trechosAFrente * terreno.Gerador.largura);
    }

    [Test]
    public void Atualizar_DescartaTrechosAntigos()
    {
        for (float x = 0; x <= 2000; x += 5) terreno.Atualizar(x);
        Assert.LessOrEqual(terreno.chunks.Count, terreno.trechosAFrente + 3);
        Assert.AreEqual(terreno.chunks.Count, terreno.transform.childCount, "trechos descartados ficaram na cena");
        Assert.Greater(terreno.chunks[0].trecho.p3.x, 2000 - 2 * terreno.Gerador.largura);
    }
}
