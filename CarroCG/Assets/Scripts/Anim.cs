using System.Collections;
using UnityEngine;

public static class Anim
{
    // Chama f(k) a cada frame com k indo de 0 a 1 ao longo de 'duracao' segundos. Sempre termina em f(1).
    public static IEnumerator Durante(float duracao, System.Action<float> f)
    {
        float t0 = Time.time, k;
        do
        {
            k = duracao <= 0 ? 1 : Mathf.Clamp01((Time.time - t0) / duracao);
            f(k);
            yield return null;
        } while (k < 1);
    }
}
