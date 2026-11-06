using UnityEngine;

// Fim de jogo por capotagem além do sensor do capacete: carro virado (> anguloMax) ou preso numa ponta
// (rodas fora do chão e parado), por tempoLimite segundos seguidos.
public class DetectorCapotagem
{
    public float anguloMax = 110f;
    public float tempoLimite = 1.5f;
    public float velocidadeParado = 0.5f;

    float virado, preso;

    public bool Atualizar(float dt, float angulo, bool noChao, float velocidade)
    {
        virado = Mathf.Abs(Mathf.DeltaAngle(0, angulo)) > anguloMax ? virado + dt : 0;
        preso = !noChao && velocidade < velocidadeParado ? preso + dt : 0;
        return virado >= tempoLimite || preso >= tempoLimite;
    }
}
