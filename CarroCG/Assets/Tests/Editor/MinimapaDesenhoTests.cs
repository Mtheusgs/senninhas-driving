using System.Linq;
using NUnit.Framework;
using UnityEngine;

public class MinimapaDesenhoTests
{
    const int W = 40, H = 20;
    static readonly Color32 Linha = new(255, 255, 255, 255), Carro = new(255, 0, 0, 255), Fundo = new(0, 0, 0, 0);
    static readonly Rect Janela = Rect.MinMaxRect(0, 0, 40, 20);   // 1 unidade = ~1 pixel

    static Color32[] Desenhar(params (Vector2, Vector2)[] segs)
    {
        var px = new Color32[W * H];
        MinimapaDesenho.Desenhar(px, W, H, Janela, segs, new Vector2(20, 10), Linha, Carro, Fundo);
        return px;
    }

    [Test]
    public void SemSegmentos_SoOCarro()
    {
        var px = Desenhar();
        Assert.AreEqual(25, px.Count(c => c.Equals(Carro)));
        Assert.AreEqual(0, px.Count(c => c.Equals(Linha)));
    }

    [Test]
    public void SegmentoForaDaJanela_NaoAparece() =>
        Assert.AreEqual(0, Desenhar((new(-10, 5), new(-2, 8))).Count(c => c.Equals(Linha)));

    [Test]
    public void SegmentoCruzandoAJanela_SoOPedacoDeDentro()
    {
        var px = Desenhar((new(-20, 2), new(60, 2)));               // linha horizontal passando pela janela inteira
        Assert.AreEqual(W, px.Count(c => c.Equals(Linha)));         // uma linha de pixels de ponta a ponta, sem sair
    }

    [Test]
    public void Segmentos_ParesConsecutivos()
    {
        var s = MinimapaDesenho.Segmentos(new[] { new Vector2[] { new(0, 0), new(1, 1), new(2, 0) } }).ToArray();
        Assert.AreEqual(2, s.Length);
        Assert.AreEqual(new Vector2(1, 1), s[1].Item1);
    }
}
