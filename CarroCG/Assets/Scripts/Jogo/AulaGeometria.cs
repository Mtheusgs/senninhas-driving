using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Linhas do modo aula, calculadas a partir dos dados que o terreno já tem (nada é reimplementado aqui).
public static class AulaGeometria
{
    public static Vector3[] Controle(Trecho t) => new Vector3[] { t.p0, t.p1, t.p2, t.p3 };

    public static Vector3[] Retangulo(Rect r) => new Vector3[]
    {
        new(r.xMin, r.yMin), new(r.xMax, r.yMin), new(r.xMax, r.yMax), new(r.xMin, r.yMax), new(r.xMin, r.yMin),
    };

    public static Vector3[] Fechado(IReadOnlyList<Vector2> p) =>
        p.Count == 0 ? new Vector3[0] : p.Select(v => (Vector3)v).Append(p[0]).ToArray();

    // P2 do trecho anterior, junção e P1 do próximo: colineares, junção no meio (P1 = 2·P3 - P2).
    public static Vector3[] Emenda(Trecho anterior, Trecho t) => new Vector3[] { anterior.p2, t.p0, t.p1 };

    public static Vector3[] Quadrado(Vector2 c, float lado)
    {
        float m = lado / 2;
        return new Vector3[] { new(c.x - m, c.y - m), new(c.x + m, c.y - m), new(c.x + m, c.y + m), new(c.x - m, c.y + m), new(c.x - m, c.y - m) };
    }
}
