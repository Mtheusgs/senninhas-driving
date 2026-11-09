using System.Collections.Generic;
using UnityEngine;

// Minimapa (puro): cada segmento do terreno é recortado contra a janela (Cohen–Sutherland), levado da janela
// para a viewport (os pixels da textura) e rasterizado com Bresenham. O carro é um quadradinho no seu pixel.
public static class MinimapaDesenho
{
    public static void Desenhar(Color32[] px, int w, int h, Rect janela, IEnumerable<(Vector2 a, Vector2 b)> segmentos,
                                Vector2 carro, Color32 corLinha, Color32 corCarro, Color32 corFundo)
    {
        for (int i = 0; i < px.Length; i++) px[i] = corFundo;
        var viewport = Rect.MinMaxRect(0, 0, w - 1, h - 1);
        foreach (var (a, b) in segmentos)
        {
            if (!Raster.ClipLine(a, b, janela, out var a2, out var b2)) continue;                // recorte
            var pa = Vector2Int.RoundToInt(Raster.JanelaViewport(a2, janela, viewport));      // janela → viewport
            var pb = Vector2Int.RoundToInt(Raster.JanelaViewport(b2, janela, viewport));
            foreach (var p in Raster.Line(pa, pb))                                            // rasterização
                if (p.x >= 0 && p.y >= 0 && p.x < w && p.y < h) px[p.y * w + p.x] = corLinha;
        }
        var c = Vector2Int.RoundToInt(Raster.JanelaViewport(carro, janela, viewport));
        for (int y = c.y - 2; y <= c.y + 2; y++)
            for (int x = c.x - 2; x <= c.x + 2; x++)
                if (x >= 0 && y >= 0 && x < w && y < h) px[y * w + x] = corCarro;
    }

    public static IEnumerable<(Vector2, Vector2)> Segmentos(IEnumerable<Vector2[]> curvas)
    {
        foreach (var c in curvas)
            for (int i = 0; i + 1 < c.Length; i++) yield return (c[i], c[i + 1]);
    }
}
