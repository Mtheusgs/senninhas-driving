using UnityEngine;

// Regras de combustível, sem dependência de cena.
[System.Serializable]
public class Combustivel
{
    public float maximo = 100f;
    public float consumoPorSegundo = 1.2f;
    public float consumoAcelerando = 2.5f;
    public float tempoParadoParaPane = 2f;
    public float velocidadeParado = 0.3f;

    float parado;

    public float Atual { get; private set; }
    public bool Vazio => Atual <= 0;
    public float Fracao => Atual / maximo;

    public void Encher()
    {
        Atual = maximo;
        parado = 0;
    }

    public void Consumir(float dt, float acelerador) =>
        Atual = Mathf.Max(0, Atual - dt * (consumoPorSegundo + consumoAcelerando * Mathf.Abs(acelerador)));

    public void Abastecer(float qtd) => Atual = Mathf.Min(maximo, Atual + qtd);

    // Fim de jogo por pane seca: sem combustível e parado por tempoParadoParaPane segundos seguidos.
    public bool VerificarPane(float dt, float velocidade)
    {
        parado = Vazio && Mathf.Abs(velocidade) < velocidadeParado ? parado + dt : 0;
        return parado >= tempoParadoParaPane;
    }
}
