using System.Collections.Generic;
using UnityEngine;

// Sombra projetada (pura): cada ponto do carro é levado na direção oposta à luz até encontrar a superfície.
public static class Sombra
{
    const float Passo = 0.1f;

    public static Vector2? Projetar(Vector2 p, Vector2 dirLuz, System.Func<float, float> altura, float alcance = 40f)
    {
        if (dirLuz.y < 0.05f) return null;                       // sol rente ao horizonte: sem chão à vista
        var d = -dirLuz.normalized;
        bool Abaixo(float t)
        {
            var q = p + d * t;
            float h = altura(q.x);
            return !float.IsNaN(h) && q.y <= h;
        }
        if (Abaixo(0)) return p;
        float antes = 0;
        for (float t = Passo; t <= alcance; t += Passo)
        {
            if (!Abaixo(t)) { antes = t; continue; }
            float a = antes, b = t;                               // bisseção entre o último acima e o 1º abaixo
            while (b - a > 0.005f)
            {
                float m = (a + b) / 2;
                if (Abaixo(m)) b = m; else a = m;
            }
            return p + d * b;
        }
        return null;
    }

    // Extensão em x da sombra; limitada a comprimentoMax, cortando o lado mais longe do carro.
    public static (float xMin, float xMax)? Faixa(IEnumerable<Vector2> pontos, Vector2 dirLuz,
                                                  System.Func<float, float> altura, float comprimentoMax = 12f)
    {
        float xMin = float.PositiveInfinity, xMax = float.NegativeInfinity, soma = 0;
        int n = 0, projetados = 0;
        foreach (var p in pontos)
        {
            soma += p.x; n++;
            var s = Projetar(p, dirLuz, altura);
            if (s == null) continue;
            projetados++;
            xMin = Mathf.Min(xMin, s.Value.x);
            xMax = Mathf.Max(xMax, s.Value.x);
        }
        if (projetados == 0) return null;
        if (xMax - xMin > comprimentoMax)
        {
            float carro = soma / n;
            if (Mathf.Abs(xMin - carro) > Mathf.Abs(xMax - carro)) xMin = xMax - comprimentoMax;
            else xMax = xMin + comprimentoMax;
        }
        return (xMin, xMax);
    }
}
