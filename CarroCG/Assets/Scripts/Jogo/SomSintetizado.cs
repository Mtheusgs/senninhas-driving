using UnityEngine;

// Sons gerados por código (puro): nenhuma amostra baixada. Todos em [-1, 1].
public static class SomSintetizado
{
    // Motor: 1 s em loop. Frequências inteiras em 1 s → o fim emenda no começo sem estalo.
    public static float[] Motor(int taxa)
    {
        var a = new float[taxa];
        var rnd = new System.Random(1);
        for (int i = 0; i < taxa; i++)
        {
            float t = i / (float)taxa;
            float s = 0.5f * Seno(55, t) + 0.25f * Seno(110, t) + 0.15f * (float)System.Math.Tanh(4 * Seno(165, t)) + 0.1f * Seno(220, t);
            s *= 0.75f + 0.25f * Seno(11, t);                              // pulsar dos cilindros
            a[i] = 0.7f * s + 0.015f * (float)(rnd.NextDouble() * 2 - 1);
        }
        return a;
    }

    public static float[] Moeda(int taxa)
    {
        int n = Mathf.RoundToInt(0.12f * taxa);
        var a = new float[2 * n];
        for (int i = 0; i < 2 * n; i++)
        {
            float t = (i % n) / (float)taxa;
            a[i] = 0.5f * Quadrada(i < n ? 988 : 1319, t) * Mathf.Exp(-t * 18);
        }
        return a;
    }

    public static float[] Galao(int taxa)
    {
        int n = Mathf.RoundToInt(0.3f * taxa);
        var a = new float[n];
        float fase = 0;
        for (int i = 0; i < n; i++)
        {
            float k = i / (float)n;
            fase += Mathf.Lerp(300, 900, k) / taxa;                          // varredura: integra a frequência
            a[i] = 0.6f * Mathf.Sin(2 * Mathf.PI * fase) * (1 - k) * Mathf.Min(1, k * 40);
        }
        return a;
    }

    public static float[] Batida(int taxa)
    {
        int n = Mathf.RoundToInt(0.25f * taxa);
        var a = new float[n];
        var rnd = new System.Random(2);
        float filtrado = 0;
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)taxa;
            filtrado += 0.15f * ((float)(rnd.NextDouble() * 2 - 1) - filtrado);   // passa-baixa: batida surda
            a[i] = Mathf.Clamp(4f * filtrado * Mathf.Exp(-t * 14) + 0.5f * Seno(60, t) * Mathf.Exp(-t * 20), -1, 1);
        }
        return a;
    }

    public static float[] Fim(int taxa)
    {
        float[] notas = { 523.25f, 392f, 261.63f };
        int n = Mathf.RoundToInt(0.3f * taxa);
        var a = new float[3 * n];
        for (int i = 0; i < a.Length; i++)
        {
            float t = (i % n) / (float)taxa;
            a[i] = 0.5f * (Seno(notas[i / n], t) + 0.3f * Seno(2 * notas[i / n], t)) / 1.3f * Mathf.Exp(-t * 6) * Mathf.Min(1, t * 200);
        }
        return a;
    }

    // Rotação da roda traseira (graus/s) → pitch do motor.
    public static float PitchMotor(float grausPorSegundoRoda) => 0.6f + 1.4f * Mathf.Clamp01(Mathf.Abs(grausPorSegundoRoda) / 2400f);

    static float Seno(float f, float t) => Mathf.Sin(2 * Mathf.PI * f * t);
    static float Quadrada(float f, float t) => Seno(f, t) >= 0 ? 1 : -1;
}
