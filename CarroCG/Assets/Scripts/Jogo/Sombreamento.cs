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
    public static float DifusaEm(ModoSombreamento modo, Vector2[] f, float x, Vector2 l) =>
        DifusaEm(modo, f, NormaisDosSegmentos(f), NormaisDosVertices(f), x, l);

    // Brilho especular (Phong, V = (0,0,1)) em x, com a normal "levantada" para 3D (z = 1,5) e a luz (lx, ly, 0,7).
    // Só existe onde a face recebe luz (N·L > 0 no plano). Mesmos três modos: é aqui que Gouraud e Phong se separam.
    public static float EspecularEm(ModoSombreamento modo, Vector2[] f, float x, Vector2 l, float brilho) =>
        EspecularEm(modo, f, NormaisDosSegmentos(f), NormaisDosVertices(f), x, l, brilho);

    static float DifusaEm(ModoSombreamento modo, Vector2[] f, Vector2[] ns, Vector2[] nv, float x, Vector2 l)
    {
        int i = Segmento(f, x);
        float u = Mathf.Clamp01(Mathf.InverseLerp(f[i].x, f[i + 1].x, x));
        return modo switch
        {
            ModoSombreamento.Flat => Difusa(ns[i], l),
            ModoSombreamento.Gouraud => Mathf.Lerp(Difusa(nv[i], l), Difusa(nv[i + 1], l), u),
            _ => Difusa(Vector2.Lerp(nv[i], nv[i + 1], u), l),
        };
    }

    static float EspecularEm(ModoSombreamento modo, Vector2[] f, Vector2[] ns, Vector2[] nv, float x, Vector2 l, float brilho)
    {
        int i = Segmento(f, x);
        float u = Mathf.Clamp01(Mathf.InverseLerp(f[i].x, f[i + 1].x, x));
        return modo switch
        {
            ModoSombreamento.Flat => Brilho(ns[i], l, brilho),
            ModoSombreamento.Gouraud => Mathf.Lerp(Brilho(nv[i], l, brilho), Brilho(nv[i + 1], l, brilho), u),
            _ => Brilho(Vector2.Lerp(nv[i], nv[i + 1], u), l, brilho),
        };
    }

    static float Brilho(Vector2 n2, Vector2 l2, float brilho)
    {
        if (Vector2.Dot(n2, l2) <= 0) return 0;
        var n = new Vector3(n2.x, n2.y, 1.5f).normalized;   // levantada para a câmera: senão o reflexo nunca aponta para V
        var l = new Vector3(l2.x, l2.y, 0.7f).normalized;
        var r = 2 * Vector3.Dot(n, l) * n - l;
        return Mathf.Pow(Mathf.Max(0, r.z), brilho);
    }

    // Cor de luz por coluna de pixel de um trecho: ambiente + corSol·difusa·max(0, N·L).
    public static Color32[] LuzPorColuna(ModoSombreamento modo, Vector2[] facetas, Rect janela, float ppu, int w,
                                         Vector2 l, Color corSol, Color corAmbiente, float difusa = 0.6f,
                                         float especular = 0.25f, float brilho = 12f)
    {
        var luz = new Color32[w];
        var ns = NormaisDosSegmentos(facetas);           // uma vez por trecho, não por coluna
        var nv = NormaisDosVertices(facetas);
        for (int x = 0; x < w; x++)
        {
            float xm = janela.xMin + (x + 0.5f) / ppu;
            float k = DifusaEm(modo, facetas, ns, nv, xm, l);
            float e = EspecularEm(modo, facetas, ns, nv, xm, l, brilho);
            var c = corAmbiente + corSol * (difusa * k + especular * e);
            c.a = 1;
            luz[x] = c;
        }
        return luz;
    }
}
