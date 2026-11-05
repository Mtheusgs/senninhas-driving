using System.Collections;
using UnityEngine;

public static class Anim
{
    // Chama f(k) a cada frame com k indo de 0 a 1 ao longo de 'duracao' segundos. Sempre termina em f(1).
    // tempoReal: conta o tempo mesmo com Time.timeScale = 0 (jogo congelado).
    public static IEnumerator Durante(float duracao, System.Action<float> f, bool tempoReal = false)
    {
        float Agora() => tempoReal ? Time.unscaledTime : Time.time;
        float t0 = Agora(), k;
        do
        {
            k = duracao <= 0 ? 1 : Mathf.Clamp01((Agora() - t0) / duracao);
            f(k);
            yield return null;
        } while (k < 1);
    }
}
