using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Parte pura de um trecho: polígono, altura da superfície e preenchimento por scanline num buffer de cores.
public static class TerrenoChunk
{
    [System.Serializable]
    public class Paleta
    {
        public Color gramaBorda = new(0.62f, 0.86f, 0.36f);
        public Color grama = new(0.36f, 0.66f, 0.24f);
        public Color terraClara = new(0.55f, 0.38f, 0.24f);
        public Color terraEscura = new(0.28f, 0.18f, 0.12f);
        public float espessuraBorda = 0.07f, espessuraGrama = 0.35f, profundidadeGradiente = 8f;

        // Cor pela profundidade abaixo da superfície (em unidades).
        public Color32 Cor(float profundidade) =>
            profundidade < espessuraBorda ? gramaBorda :
            profundidade < espessuraGrama ? grama :
            Color.Lerp(terraClara, terraEscura, Mathf.Clamp01((profundidade - espessuraGrama) / profundidadeGradiente));
    }

    // Curva da superfície + dois cantos de baixo.
    public static List<Vector2> Poligono(Vector2[] curva, float fundoY)
    {
        var p = new List<Vector2>(curva);
        p.Add(new Vector2(curva[^1].x, fundoY));
        p.Add(new Vector2(curva[0].x, fundoY));
        return p;
    }

    // Altura da superfície em x, por interpolação linear entre as amostras (curva com x crescente).
    public static float AlturaEm(Vector2[] curva, float x)
    {
        if (x <= curva[0].x) return curva[0].y;
        for (int i = 1; i < curva.Length; i++)
            if (x <= curva[i].x)
                return Mathf.Lerp(curva[i - 1].y, curva[i].y, (x - curva[i - 1].x) / (curva[i].x - curva[i - 1].x));
        return curva[^1].y;
    }

    // Preenche o polígono (em coordenadas de mundo) por scanline numa grade de ppu pixels por unidade
    // cobrindo a janela. Pixel fora do polígono fica transparente.
    public static Color32[] Pintar(IReadOnlyList<Vector2> poligono, Rect janela, float ppu, Vector2[] curva,
                                   Paleta paleta, out int w, out int h)
    {
        w = Mathf.CeilToInt(janela.width * ppu);
        h = Mathf.CeilToInt(janela.height * ppu);
        var px = new Color32[w * h];
        if (poligono.Count < 3) return px;

        var superficie = new float[w];
        for (int x = 0; x < w; x++) superficie[x] = AlturaEm(curva, janela.xMin + (x + 0.5f) / ppu);

        var pix = poligono.Select(p => Vector2Int.RoundToInt((p - janela.min) * ppu)).ToArray();
        foreach (var (y, x0, x1) in Raster.ScanlineSpans(pix))
        {
            if (y < 0 || y >= h) continue;
            float yMundo = janela.yMin + (y + 0.5f) / ppu;
            for (int x = Mathf.Max(0, x0); x <= Mathf.Min(w - 1, x1); x++)
                px[y * w + x] = paleta.Cor(superficie[x] - yMundo);
        }
        return px;
    }
}
