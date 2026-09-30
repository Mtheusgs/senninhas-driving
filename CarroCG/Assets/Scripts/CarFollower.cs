using System.Collections;
using UnityEngine;

// Move o carro sobre a pista: translação (posição), rotação (tangente) e espelhamento (escala -1 em x).
public class CarFollower : MonoBehaviour
{
    public BezierPath pista;

    float angulo;

    public void Posicionar(float t)
    {
        transform.position = pista.Point(t);                        // translação
        Vector2 d = pista.Tangent(t);
        if (d.sqrMagnitude > 1e-6f) angulo = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angulo);        // rotação
    }

    // ponytail: velocidade por t, não por comprimento de arco; reparametrizar se a variação incomodar no vídeo.
    public IEnumerator Percorrer(float de, float ate, float duracao) =>
        Anim.Durante(duracao, k => Posicionar(Mathf.Lerp(de, ate, k)));

    public void Espelhar()                                          // espelhamento
    {
        var s = transform.localScale;
        s.x *= -1;
        transform.localScale = s;
    }
}
