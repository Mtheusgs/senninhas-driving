using UnityEngine;

// Desenho da intro num buffer de pixels (puro): wireframe 3D projetado, contorno por Bresenham e regiões por scanline.
// A janela (em unidades) é mapeada para a viewport (o buffer) pela transformação janela → viewport.
public static class IntroDesenho
{
    public static void Limpar(Color32[] px) => System.Array.Clear(px, 0, px.Length);

    static Vector2 Pixel(Vector2 u, Rect janela, int w, int h) => Raster.JanelaViewport(u, janela, new Rect(0, 0, w, h));

    // Recorta (Cohen–Sutherland) contra o buffer antes de rasterizar (Bresenham): nada fora, nenhum laço gigante.
    static void Segmento(Color32[] px, int w, int h, Vector2 a, Vector2 b, Color32 cor)
    {
        if (!Raster.ClipLine(a, b, new Rect(0, 0, w - 1, h - 1), out var a2, out var b2)) return;
        foreach (var p in Raster.Line(Vector2Int.RoundToInt(a2), Vector2Int.RoundToInt(b2)))
            if (p.x >= 0 && p.x < w && p.y >= 0 && p.y < h) px[p.y * w + p.x] = cor;
    }

    // k: 0 = paralela, 1 = perspectiva (interpolação das matrizes). d: distância do observador.
    public static void Wireframe(Color32[] px, int w, int h, Rect janela, Matrix4x4 rot, float k, float d, Color32 cor)
    {
        var m = Projecao.Interpolar(Projecao.Ortografica(), Projecao.Perspectiva(d), k) * rot;
        var v = ModeloF1.Vertices;
        var proj = new Vector2[v.Length];
        for (int i = 0; i < v.Length; i++) proj[i] = Pixel(Projecao.Projetar(m, v[i]), janela, w, h);
        foreach (var e in ModeloF1.Arestas) Segmento(px, w, h, proj[e.x], proj[e.y], cor);
    }

    public static void Contorno(Color32[] px, int w, int h, Rect janela, int regioes, Color32 cor)
    {
        for (int r = 0; r < regioes && r < ContornoF1.Regioes.Length; r++)
        {
            var poly = ContornoF1.Regioes[r];
            for (int i = 0; i < poly.Length; i++)
                Segmento(px, w, h, Pixel(ContornoF1.EmUnidades(poly[i]), janela, w, h),
                         Pixel(ContornoF1.EmUnidades(poly[(i + 1) % poly.Length]), janela, w, h), cor);
        }
    }

    public static void Preencher(Color32[] px, int w, int h, Rect janela, int regioes)
    {
        for (int r = 0; r < regioes && r < ContornoF1.Regioes.Length; r++)
        {
            var poly = ContornoF1.Regioes[r];
            var pix = new Vector2Int[poly.Length];
            for (int i = 0; i < poly.Length; i++) pix[i] = Vector2Int.RoundToInt(Pixel(ContornoF1.EmUnidades(poly[i]), janela, w, h));
            foreach (var (y, x0, x1) in Raster.ScanlineSpans(pix))
            {
                if (y < 0 || y >= h) continue;
                for (int x = Mathf.Max(0, x0); x <= Mathf.Min(w - 1, x1); x++) px[y * w + x] = ContornoF1.Cores[r];
            }
        }
    }
}
