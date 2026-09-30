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
}
