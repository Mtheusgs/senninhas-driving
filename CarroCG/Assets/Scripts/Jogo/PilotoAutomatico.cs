using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// Decisões do piloto do menu (puro, testável): acelera nas subidas, alivia só se empinar; no ar alinha o bico.
// Recomeça quando o carro capota/prende (mesmo detector do jogo) ou fica parado no chão.
public class DecisaoPiloto
{
    public float anguloEmpinar = 50f, ganhoNoAr = 0.05f, amortecimento = 0.01f;
    public float tempoParado = 4f, velocidadeParado = 0.5f;

    readonly DetectorCapotagem capotagem = new();
    float parado;

    public float Acelerador(float inclinacao, bool noChao, float velocidadeAngular) => noChao
        ? (inclinacao > anguloEmpinar ? 0 : 1)
        : Mathf.Clamp(-inclinacao * ganhoNoAr - velocidadeAngular * amortecimento, -1, 1);

    public bool DeveRecomecar(float dt, float angulo, bool noChao, float velocidade)
    {
        bool capotou = capotagem.Atualizar(dt, angulo, noChao, velocidade);
        parado = noChao && velocidade < velocidadeParado ? parado + dt : 0;   // velocidade total: subida lenta não conta
        return capotou || parado >= tempoParado;
    }

    public void Zerar() => parado = 0;
}

// Dirige o F1 do fundo do menu. Se precisar recomeçar, escurece a tela, volta à largada e clareia.
public class PilotoAutomatico : MonoBehaviour
{
    public F1 f1;
    public Terreno terreno;
    public Image fade;
    public float duracaoFade = 0.35f;

    DecisaoPiloto decisao = new();
    Rigidbody2D[] corpos;
    Vector2[] posicoes;
    float[] rotacoes;
    bool recomecando;

    public int Recomecos { get; private set; }

    void Start()
    {
        f1.controlePeloTeclado = false;
        f1.Capotou += IniciarRecomeco;
        corpos = new[] { f1.GetComponent<Rigidbody2D>(), f1.eixoTraseiro.connectedBody, f1.eixoDianteiro.connectedBody };
        posicoes = System.Array.ConvertAll(corpos, c => c.position);
        rotacoes = System.Array.ConvertAll(corpos, c => c.rotation);
    }

    void OnDestroy()
    {
        if (f1) f1.Capotou -= IniciarRecomeco;
    }

    void Update()
    {
        if (recomecando) return;
        float inclinacao = Mathf.DeltaAngle(0, f1.transform.eulerAngles.z);   // + = bico para cima
        f1.acelerador = decisao.Acelerador(inclinacao, f1.NoChao, corpos[0].angularVelocity);
        if (decisao.DeveRecomecar(Time.deltaTime, inclinacao, f1.NoChao, f1.Velocidade)) IniciarRecomeco();
    }

    public void IniciarRecomeco()
    {
        if (!recomecando) StartCoroutine(RecomecarSuave());
    }

    IEnumerator RecomecarSuave()
    {
        recomecando = true;
        yield return Anim.Durante(duracaoFade, k => fade.color = new Color(0, 0, 0, k), tempoReal: true);
        Recomecar();
        yield return Anim.Durante(duracaoFade, k => fade.color = new Color(0, 0, 0, 1 - k), tempoReal: true);
        recomecando = false;
    }

    public void Recomecar()
    {
        Recomecos++;
        decisao = new DecisaoPiloto();
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
