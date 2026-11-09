using System.Collections.Generic;
using UnityEngine;

// Reflexo no lago: o quad de um sprite é espelhado na linha da água e recortado (Sutherland–Hodgman) contra a água.
public static class ReflexoGeometria
{
    // Devolve false quando nada do reflexo cai dentro da água. Saída: polígono convexo (triangular em leque).
    public static bool Quad(Vector2[] cantos, Vector2[] uvs, float h, Rect agua, List<Vector2> verts, List<Vector2> uvsSaida)
    {
        verts.Clear();
        uvsSaida.Clear();
        var refletido = new Vector2[cantos.Length];
        for (int i = 0; i < cantos.Length; i++) refletido[i] = Raster.Refletir(cantos[i], h);
        var p = Raster.ClipPolygon(refletido, agua, uvs, out var uv);
        if (p.Count < 3) return false;
        verts.AddRange(p);
        uvsSaida.AddRange(uv);
        return true;
    }
}
