using UnityEngine;

// Tinge só a camada de pintura do F1 com a cor da garagem; detalhes e rodas ficam como estão.
public class PinturaF1 : MonoBehaviour
{
    public SpriteRenderer pintura;

    void Start() => pintura.color = Garagem.CorEscolhida;
}
