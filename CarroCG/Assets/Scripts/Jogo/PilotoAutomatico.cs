using UnityEngine;

// Dirige o F1 do fundo do menu. Acelera; alivia quando empina; se capotar ou parar, recomeça em silêncio.
public class PilotoAutomatico : MonoBehaviour
{
    public F1 f1;
    public Terreno terreno;
    public float anguloAlivio = 25f;
    public float tempoParado = 3f;

    Rigidbody2D[] corpos;
    Vector2[] posicoes;
    float[] rotacoes;
    float parado;

    public int Recomecos { get; private set; }

    void Start()
    {
        f1.controlePeloTeclado = false;
        f1.Capotou += Recomecar;
        corpos = new[] { f1.GetComponent<Rigidbody2D>(), f1.eixoTraseiro.connectedBody, f1.eixoDianteiro.connectedBody };
        posicoes = System.Array.ConvertAll(corpos, c => c.position);
        rotacoes = System.Array.ConvertAll(corpos, c => c.rotation);
    }

    void OnDestroy()
    {
        if (f1) f1.Capotou -= Recomecar;
    }

    void Update()
    {
        float inclinacao = Mathf.DeltaAngle(0, f1.transform.eulerAngles.z);   // + = bico para cima
        f1.acelerador = inclinacao > anguloAlivio ? (f1.NoChao ? 0 : -1) : 1;
        parado = Mathf.Abs(f1.VelocidadeX) < 0.3f ? parado + Time.deltaTime : 0;
        if (parado > tempoParado) Recomecar();
    }

    public void Recomecar()
    {
        Recomecos++;
        parado = 0;
        terreno.Reiniciar();
        for (int i = 0; i < corpos.Length; i++)
        {
            corpos[i].position = posicoes[i];
            corpos[i].rotation = rotacoes[i];
            corpos[i].transform.SetPositionAndRotation(posicoes[i], Quaternion.Euler(0, 0, rotacoes[i]));
            corpos[i].linearVelocity = Vector2.zero;
            corpos[i].angularVelocity = 0;
        }
        var segue = Camera.main ? Camera.main.GetComponent<CameraSegue>() : null;
        if (segue) segue.Encaixar();
    }
}
