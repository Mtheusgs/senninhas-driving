using System.Collections;
using TMPro;
using UnityEngine;

// Intro (~35 s, Espaço pula, não repete na sessão): F1 3D em wireframe girando (rotação) e projetado de paralela para
// perspectiva; volta à vista lateral e paralela; contorno por Bresenham; regiões por scanline; crossfade para o HD.
[DefaultExecutionOrder(50)]     // depois de Partida.Start e PinturaF1.Start
public class IntroF1 : MonoBehaviour
{
    public Partida partida;
    public CanvasGroup hud;
    public SpriteRenderer tela;                 // a textura da intro, presa ao chassi
    public SpriteRenderer[] camadas;            // camadas HD do F1 (somem durante a intro)
    public CameraSegue segue;
    public GameObject caixaLegenda;
    public TMP_Text legenda;
    public float duracao = 35f;
    public float d = 4f;
    public int largura = 480, altura = 208;
    public Vector2 tamanho = new(6f, 2.6f);    // janela em unidades, centrada no chassi
    public float zoom = 2.4f;

    public static bool EmAndamento { get; private set; }
    public bool Ativa { get; private set; }
    public Texture2D Textura { get; private set; }

    Color32[] px;
    Rect janela;
    static readonly Color32 CorFio = new(170, 230, 255, 255), CorContorno = new(255, 255, 255, 255);

    void Start()
    {
        janela = new Rect(-tamanho / 2, tamanho);
        Textura = new Texture2D(largura, altura, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, name = "Intro" };
        px = new Color32[largura * altura];
        tela.sprite = Sprite.Create(Textura, new Rect(0, 0, largura, altura), new Vector2(0.5f, 0.5f), largura / tamanho.x);
        if (Sessao.IntroVista) Finalizar();
        else Comecar();
    }

    void Update()
    {
        if (Ativa && Input.GetKeyDown(KeyCode.Space)) Pular();
    }

    public void Comecar()
    {
        StopAllCoroutines();
        Sessao.IntroVista = true;
        Ativa = EmAndamento = true;
        partida.Congelar(true);
        hud.alpha = 0;
        Alfa(0);
        tela.enabled = true;
        tela.color = Color.white;
        caixaLegenda.SetActive(true);
        segue.enabled = false;
        var cam = segue.GetComponent<Camera>();
        cam.orthographicSize = zoom;
        var c = tela.transform.position;
        cam.transform.position = new Vector3(c.x, c.y + 0.3f, cam.transform.position.z);
        StartCoroutine(Roteiro());
    }

    public void Pular()
    {
        StopAllCoroutines();
        Finalizar();
    }

    IEnumerator Roteiro()
    {
        float T(float s) => s / 35f * duracao;
        int n = ContornoF1.Regioes.Length;

        legenda.text = "Projeção paralela -> perspectiva · rotação 3D";
        yield return Anim.Durante(T(12), k => Wire(Mathf.Lerp(0, 750, Suave(k)), 20, Suave(k)), tempoReal: true);

        legenda.text = "Vista lateral · volta para a projeção paralela";
        yield return Anim.Durante(T(6), k => Wire(Mathf.Lerp(750, 720, Suave(k)), Mathf.Lerp(20, 0, Suave(k)), 1 - Suave(k)), tempoReal: true);

        legenda.text = "Rasterização: contorno com Bresenham";
        yield return Anim.Durante(T(8), k =>
        {
            IntroDesenho.Limpar(px);
            IntroDesenho.Wireframe(px, largura, altura, janela, Matrix4x4.identity, 0, d, new Color32(170, 230, 255, 70));
            IntroDesenho.Contorno(px, largura, altura, janela, Mathf.CeilToInt(k * n), CorContorno);
            Aplicar();
        }, tempoReal: true);

        legenda.text = "Preenchimento: scanline região a região";
        yield return Anim.Durante(T(6), k =>
        {
            IntroDesenho.Limpar(px);
            IntroDesenho.Preencher(px, largura, altura, janela, Mathf.CeilToInt(k * n));
            IntroDesenho.Contorno(px, largura, altura, janela, n, CorContorno);
            Aplicar();
        }, tempoReal: true);

        legenda.text = "Arte HD com iluminação Phong";
        segue.enabled = true;                  // a câmera abre enquanto o desenho vira o sprite HD
        yield return Anim.Durante(T(3), k =>
        {
            tela.color = new Color(1, 1, 1, 1 - k);
            Alfa(k);
            hud.alpha = k;
        }, tempoReal: true);
        Finalizar();
    }

    static float Suave(float k) => k * k * (3 - 2 * k);

    void Wire(float yGraus, float xGraus, float k)
    {
        IntroDesenho.Limpar(px);
        IntroDesenho.Wireframe(px, largura, altura, janela, ModeloF1.Rotacao(yGraus, xGraus), k, d, CorFio);
        Aplicar();
    }

    void Aplicar()
    {
        Textura.SetPixels32(px);
        Textura.Apply();
    }

    void Alfa(float a)
    {
        foreach (var c in camadas)
        {
            var cor = c.color;
            cor.a = a;
            c.color = cor;
        }
    }

    void Finalizar()
    {
        Ativa = EmAndamento = false;
        tela.enabled = false;
        Alfa(1);
        hud.alpha = 1;
        caixaLegenda.SetActive(false);
        segue.enabled = true;
        partida.Congelar(false);
    }
}
