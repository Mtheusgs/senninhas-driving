using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Algoritmos da disciplina, sem dependência de cena: fáceis de testar e de citar no relatório.
public static class Raster
{
    // Bresenham generalizado para todos os octantes (só inteiros). Inclui os dois extremos.
    public static IEnumerable<Vector2Int> Line(Vector2Int a, Vector2Int b)
    {
        int x = a.x, y = a.y;
        int dx = Mathf.Abs(b.x - a.x), sx = a.x < b.x ? 1 : -1;
        int dy = -Mathf.Abs(b.y - a.y), sy = a.y < b.y ? 1 : -1;
        int err = dx + dy;
        while (true)
        {
            yield return new Vector2Int(x, y);
            if (x == b.x && y == b.y) yield break;
            int e2 = 2 * err;
            if (e2 >= dy) { err += dy; x += sx; }
            if (e2 <= dx) { err += dx; y += sy; }
        }
    }

    // Preenchimento por scanline: para cada linha y, intersecta com as arestas, ordena e pinta entre pares.
    // Cada aresta cobre [yMin, yMax) para que um vértice compartilhado não seja contado duas vezes.
    public static IEnumerable<(int y, int xStart, int xEnd)> ScanlineSpans(Vector2Int[] poly)
    {
        if (poly == null || poly.Length < 3) yield break;
        int yMin = poly.Min(p => p.y), yMax = poly.Max(p => p.y);
        var xs = new List<float>();
        for (int y = yMin; y < yMax; y++)
        {
            xs.Clear();
            for (int i = 0; i < poly.Length; i++)
            {
                Vector2Int a = poly[i], b = poly[(i + 1) % poly.Length];
                if (a.y == b.y) continue;                                   // aresta horizontal
                if (y < Mathf.Min(a.y, b.y) || y >= Mathf.Max(a.y, b.y)) continue;
                xs.Add(a.x + (float)(y - a.y) * (b.x - a.x) / (b.y - a.y));
            }
            xs.Sort();
            for (int i = 0; i + 1 < xs.Count; i += 2)
            {
                int x0 = Mathf.CeilToInt(xs[i]), x1 = Mathf.CeilToInt(xs[i + 1]) - 1;
                if (x0 <= x1) yield return (y, x0, x1);
            }
        }
    }

    // Bézier cúbica na forma de Bernstein.
    public static Vector2 BezierPoint(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float t)
    {
        float u = 1 - t;
        return u * u * u * p0 + 3 * u * u * t * p1 + 3 * u * t * t * p2 + t * t * t * p3;
    }

    // Derivada da cúbica: direção do movimento em t.
    public static Vector2 BezierTangent(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float t)
    {
        float u = 1 - t;
        return 3 * u * u * (p1 - p0) + 6 * u * t * (p2 - p1) + 3 * t * t * (p3 - p2);
    }

    // Emenda C1 entre trechos: o 1º controle do novo trecho é o espelhamento do último controle
    // do anterior em torno do ponto de junção.
    public static Vector2 BezierJoin(Vector2 p2Anterior, Vector2 p3Anterior) => 2 * p3Anterior - p2Anterior;

    // Recorte de polígono de Sutherland–Hodgman: recorta contra cada borda do retângulo, em sequência.
    public static List<Vector2> ClipPolygon(IReadOnlyList<Vector2> poly, Rect r)
    {
        var p = new List<Vector2>(poly);
        p = ClipBorda(p, v => v.x >= r.xMin, (a, b) => Corte(a, b, (r.xMin - a.x) / (b.x - a.x)));
        p = ClipBorda(p, v => v.x <= r.xMax, (a, b) => Corte(a, b, (r.xMax - a.x) / (b.x - a.x)));
        p = ClipBorda(p, v => v.y >= r.yMin, (a, b) => Corte(a, b, (r.yMin - a.y) / (b.y - a.y)));
        p = ClipBorda(p, v => v.y <= r.yMax, (a, b) => Corte(a, b, (r.yMax - a.y) / (b.y - a.y)));
        return p;
    }

    static Vector2 Corte(Vector2 a, Vector2 b, float t) => a + (b - a) * t;

    // Uma borda: para cada aresta (anterior → atual), emite a interseção ao cruzar a borda e o vértice se estiver dentro.
    static List<Vector2> ClipBorda(List<Vector2> entrada, System.Func<Vector2, bool> dentro,
                                   System.Func<Vector2, Vector2, Vector2> intersecao)
    {
        var saida = new List<Vector2>();
        for (int i = 0; i < entrada.Count; i++)
        {
            Vector2 atual = entrada[i], anterior = entrada[(i + entrada.Count - 1) % entrada.Count];
            bool dAtual = dentro(atual), dAnterior = dentro(anterior);
            if (dAtual)
            {
                if (!dAnterior) saida.Add(intersecao(anterior, atual));
                saida.Add(atual);
            }
            else if (dAnterior) saida.Add(intersecao(anterior, atual));
        }
        return saida;
    }

    // Recorte de linha de Cohen–Sutherland. Código de região: 1 esquerda, 2 direita, 4 baixo, 8 cima.
    public static int CodigoRegiao(Vector2 p, Rect r) =>
        (p.x < r.xMin ? 1 : 0) | (p.x > r.xMax ? 2 : 0) | (p.y < r.yMin ? 4 : 0) | (p.y > r.yMax ? 8 : 0);

    public static bool ClipLine(Vector2 a, Vector2 b, Rect r, out Vector2 a2, out Vector2 b2)
    {
        int ca = CodigoRegiao(a, r), cb = CodigoRegiao(b, r);
        while (true)
        {
            if ((ca | cb) == 0) { a2 = a; b2 = b; return true; }            // aceitação trivial
            if ((ca & cb) != 0) { a2 = b2 = default; return false; }        // rejeição trivial
            int fora = ca != 0 ? ca : cb;                                   // corta o ponto que está fora
            Vector2 p;
            if ((fora & 8) != 0) p = new Vector2(a.x + (b.x - a.x) * (r.yMax - a.y) / (b.y - a.y), r.yMax);
            else if ((fora & 4) != 0) p = new Vector2(a.x + (b.x - a.x) * (r.yMin - a.y) / (b.y - a.y), r.yMin);
            else if ((fora & 2) != 0) p = new Vector2(r.xMax, a.y + (b.y - a.y) * (r.xMax - a.x) / (b.x - a.x));
            else p = new Vector2(r.xMin, a.y + (b.y - a.y) * (r.xMin - a.x) / (b.x - a.x));
            if (fora == ca) { a = p; ca = CodigoRegiao(a, r); }
            else { b = p; cb = CodigoRegiao(b, r); }
        }
    }

    // Transformação janela → viewport (mapeamento linear entre os dois retângulos).
    public static Vector2 JanelaViewport(Vector2 p, Rect janela, Rect viewport) => new(
        viewport.xMin + (p.x - janela.xMin) / janela.width * viewport.width,
        viewport.yMin + (p.y - janela.yMin) / janela.height * viewport.height);
}
