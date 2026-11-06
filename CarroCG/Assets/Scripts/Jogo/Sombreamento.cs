using UnityEngine;

public enum ModoSombreamento { Flat, Gouraud, Phong }

// Iluminação e sombreamento (puro): Lambert, Flat/Gouraud/Phong ao longo de uma polilinha e Phong especular.
public static class Sombreamento
{
    static float Difusa(Vector2 n, Vector2 l) => Mathf.Max(0, Vector2.Dot(n.normalized, l.normalized));

    public static float Lambert(Vector2 n, Vector2 l, float ambiente, float difusa) => ambiente + difusa * Difusa(n, l);

    // Phong: ambiente + difusa·max(0,N·L) + especular·max(0,R·V)^brilho, com R = 2(N·L)N - L.
    public static float Phong(Vector3 n, Vector3 l, Vector3 v, float ambiente, float difusa, float especular, float brilho)
    {
        n = n.normalized; l = l.normalized; v = v.normalized;
        float nl = Vector3.Dot(n, l);
        var r = 2 * nl * n - l;
        return ambiente + difusa * Mathf.Max(0, nl) + especular * Mathf.Pow(Mathf.Max(0, Vector3.Dot(r, v)), brilho);
    }

    // Normal à esquerda de cada segmento ("para cima" quando a curva avança em +x).
    public static Vector2[] NormaisDosSegmentos(Vector2[] f)
    {
        var n = new Vector2[f.Length - 1];
        for (int i = 0; i < n.Length; i++)
        {
            var d = (f[i + 1] - f[i]).normalized;
            n[i] = new Vector2(-d.y, d.x);
        }
        return n;
    }

    // Normal de cada vértice: média normalizada dos segmentos vizinhos (extremos: a do único segmento).
    public static Vector2[] NormaisDosVertices(Vector2[] f)
    {
        var s = NormaisDosSegmentos(f);
        var v = new Vector2[f.Length];
        v[0] = s[0];
        v[^1] = s[^1];
        for (int i = 1; i < f.Length - 1; i++) v[i] = (s[i - 1] + s[i]).normalized;
        return v;
    }

    static int Segmento(Vector2[] f, float x)
    {
        for (int i = 0; i < f.Length - 2; i++) if (x < f[i + 1].x) return i;
        return f.Length - 2;
    }

    // Fator difuso max(0, N·L) em x: Flat = normal do segmento; Gouraud = interpola os fatores dos vértices;
    // Phong = interpola as normais dos vértices e calcula por ponto.
    public static float DifusaEm(ModoSombreamento modo, Vector2[] f, float x, Vector2 l)
    {
        int i = Segmento(f, x);
        float u = Mathf.Clamp01(Mathf.InverseLerp(f[i].x, f[i + 1].x, x));
        switch (modo)
        {
            case ModoSombreamento.Flat:
                return Difusa(NormaisDosSegmentos(f)[i], l);
            case ModoSombreamento.Gouraud:
            {
                var nv = NormaisDosVertices(f);
                return Mathf.Lerp(Difusa(nv[i], l), Difusa(nv[i + 1], l), u);
            }
            default:
            {
                var nv = NormaisDosVertices(f);
                return Difusa(Vector2.Lerp(nv[i], nv[i + 1], u), l);
            }
        }
    }

    // Cor de luz por coluna de pixel de um trecho: ambiente + corSol·difusa·max(0, N·L).
    public static Color32[] LuzPorColuna(ModoSombreamento modo, Vector2[] facetas, Rect janela, float ppu, int w,
                                         Vector2 l, Color corSol, Color corAmbiente, float difusa = 0.6f)
    {
        var luz = new Color32[w];
        for (int x = 0; x < w; x++)
        {
            float k = DifusaEm(modo, facetas, janela.xMin + (x + 0.5f) / ppu, l);
            var c = corAmbiente + corSol * (difusa * k);
            c.a = 1;
            luz[x] = c;
        }
        return luz;
    }
}
