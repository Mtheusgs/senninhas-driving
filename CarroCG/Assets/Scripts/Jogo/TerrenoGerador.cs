using System.Linq;
using UnityEngine;

public struct Trecho { public Vector2 p0, p1, p2, p3; }

// Sequência infinita de Bézier cúbicas com emenda C1. Morros crescem com a distância.
public class TerrenoGerador
{
    public float largura = 20f;
    public float amplitudeInicial = 2.5f;     // Terra (ModoJogo); morros crescem até ~45° perto de 300 m
    public float ganhoPorTrecho = 0.5f;
    public float amplitudeMax = 12f;
    public float limiteY = 30f;

    readonly System.Random rnd;
    Trecho? anterior;

    public int Indice { get; private set; }

    public TerrenoGerador(int semente) => rnd = new System.Random(semente);

    float Sorteio(float a, float b) => a + (float)rnd.NextDouble() * (b - a);

    public Trecho Proximo()
    {
        Trecho t;
        if (anterior == null)   // largada plana
        {
            t = new Trecho { p0 = new(0, 0), p1 = new(largura / 3, 0), p2 = new(2 * largura / 3, 0), p3 = new(largura, 0) };
        }
        else
        {
            var a = anterior.Value;
            float amp = Mathf.Min(amplitudeMax, amplitudeInicial + ganhoPorTrecho * Indice);
            float x0 = a.p3.x;
            float y3 = Mathf.Clamp(a.p3.y + Sorteio(-amp, amp), -limiteY, limiteY);
            t = new Trecho
            {
                p0 = a.p3,
                p1 = Raster.BezierJoin(a.p2, a.p3),                              // espelhamento → C1
                p2 = new(x0 + largura * Sorteio(0.6f, 0.75f), y3 + Sorteio(-0.3f, 0.3f) * amp),
                p3 = new(x0 + largura, y3),
            };
        }
        anterior = t;
        Indice++;
        return t;
    }

    public static Vector2[] Amostrar(Trecho t, int n) =>
        Enumerable.Range(0, n).Select(i => Raster.BezierPoint(t.p0, t.p1, t.p2, t.p3, i / (float)(n - 1))).ToArray();

    // Normal unitária à esquerda da tangente: "para cima" quando a curva avança em +x.
    public static Vector2 Normal(Trecho t, float u)
    {
        var d = Raster.BezierTangent(t.p0, t.p1, t.p2, t.p3, u).normalized;
        return new Vector2(-d.y, d.x);
    }
}
