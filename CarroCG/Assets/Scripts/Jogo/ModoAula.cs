using System.Collections;
using System.Linq;
using TMPro;
using UnityEngine;

// Sobreposição didática (Tab): Bézier, emenda espelhada, recorte, eixos e velocidade do F1.
// F: congela o jogo e repinta por scanline o trecho no centro da tela.
public class ModoAula : MonoBehaviour
{
    public Terreno terreno;
    public F1 f1;
    public Partida partida;
    public CicloDia ciclo;
    public Minimapa minimapa;
    public Parallax parallax;
    public Material materialLinha;
    public TMP_Text rotulo;
    public float duracaoScanline = 2f;

    public bool Ativo { get; private set; }
    public bool Repintando { get; private set; }

    static readonly Color Branco = Color.white, Amarelo = new(1f, 0.85f, 0.1f), Vermelho = new(1f, 0.25f, 0.25f),
                          Verde = new(0.3f, 1f, 0.4f), Ciano = new(0.2f, 0.9f, 1f);

    const string Texto =
        "MODO AULA\n" +
        "<size=80%>· Curva de Bézier: pontos e polígono de controle (branco)\n" +
        "· Espelhamento na emenda: P1 = 2·P3 - P2 (ciano)\n" +
        "· Recorte Sutherland-Hodgman: janela (amarelo), antes (vermelho), depois (verde)\n" +
        "· Translação e rotação: eixos x/y e velocidade do F1\n" +
        "· Iluminação: Lambert {0} (tecla L) - normais (magenta), luz do sol (amarelo)\n" +
        "· Projeção: {1} (P) - minimapa: recorte Cohen-Sutherland, janela->viewport, Bresenham (laranja)\n" +
        "· Espelhamento: reflexo no lago (y' = 2h - y), recortado contra a água (azul)\n" +
        "F: preenchimento scanline em câmera lenta</size>";

    GameObject eixos;
    LineRenderer velocidade, scanline, raioSol, janelaMinimapa;
    string textoNoRotulo;
    static readonly Color Laranja = new(1f, 0.55f, 0.1f);
    static readonly Color Magenta = new(1f, 0.3f, 0.9f);
    static readonly Color AzulClaro = new(0.55f, 0.8f, 1f);
    Rigidbody2D rb;
    CameraSegue segue;

    void OnEnable() => terreno.TrechoCriado += Desenhar;
    void OnDisable() => terreno.TrechoCriado -= Desenhar;

    void Start()
    {
        rb = f1.GetComponent<Rigidbody2D>();
        segue = Camera.main.GetComponent<CameraSegue>();
        eixos = new GameObject("Aula: eixos");
        eixos.transform.SetParent(f1.transform, false);
        Linha(eixos.transform, "x", false, 0.05f, Vermelho, new Vector3[] { Vector3.zero, Vector3.right * 1.2f });
        Linha(eixos.transform, "y", false, 0.05f, Verde, new Vector3[] { Vector3.zero, Vector3.up * 0.9f });
        velocidade = Linha(transform, "Aula: velocidade", true, 0.05f, Amarelo, new Vector3[2]);
        scanline = Linha(transform, "Aula: scanline", true, 0.08f, Ciano, new Vector3[2]);
        scanline.enabled = false;
        raioSol = Linha(transform, "Aula: sol", true, 0.06f, Amarelo, new Vector3[2]);
        janelaMinimapa = Linha(transform, "Aula: janela do minimapa", true, 0.12f, Laranja, new Vector3[5]);
        foreach (var c in terreno.chunks) Desenhar(c);
        DefinirAtivo(Preferencias.ModoAula);
    }

    void Update()
    {
        if (!IntroF1.EmAndamento && Input.GetKeyDown(KeyCode.Tab)) DefinirAtivo(!Ativo);
        if (Ativo && !IntroF1.EmAndamento && Input.GetKeyDown(KeyCode.F)) RepintarScanline();
        var texto = string.Format(Texto, terreno.modoSombreamento, parallax && !parallax.perspectiva ? "paralela" : "perspectiva");
        if (texto != textoNoRotulo) rotulo.text = textoNoRotulo = texto;
        if (Ativo)
        {
            if (ciclo)
            {
                raioSol.SetPosition(0, rb.position);
                raioSol.SetPosition(1, rb.position + ciclo.Atual.dirLuz * 2.5f);
            }
            if (minimapa) janelaMinimapa.SetPositions(AulaGeometria.Retangulo(minimapa.Janela));
            velocidade.SetPosition(0, rb.position);
            velocidade.SetPosition(1, rb.position + rb.linearVelocity * 0.3f);
            var atual = terreno.chunks.Find(k => k.janela.xMin <= rb.position.x && rb.position.x < k.janela.xMax);
            if (atual != null && segue)    // do fundo do polígono original (pedaço cortado) até acima da superfície (espaço do HUD)
                segue.enquadrar = Rect.MinMaxRect(atual.janela.xMin, atual.poligonoOriginal.Min(p => p.y) - 1,
                                                  atual.janela.xMax, atual.janela.yMax + 7);
        }
    }

    public void DefinirAtivo(bool sim)
    {
        Ativo = sim;
        eixos.SetActive(sim);
        velocidade.enabled = sim;
        raioSol.enabled = sim;
        janelaMinimapa.enabled = sim;
        rotulo.transform.parent.gameObject.SetActive(sim);
        if (!sim && segue) segue.enquadrar = null;
        foreach (var c in terreno.chunks)
        {
            var aula = c.go.transform.Find("Aula");
            if (aula) aula.gameObject.SetActive(sim);
        }
    }

    public void RepintarScanline()
    {
        if (!Ativo || Repintando || partida.EstadoAtual != Partida.Estado.Jogando) return;
        float x = Camera.main.transform.position.x;
        var c = terreno.chunks.Find(k => k.janela.xMin <= x && x < k.janela.xMax);
        if (c != null) StartCoroutine(Repintar(c));
    }

    IEnumerator Repintar(Terreno.Chunk c)
    {
        Repintando = true;
        partida.Congelar(true);
        scanline.enabled = true;
        yield return terreno.RepintarAnimado(c, duracaoScanline, y =>
        {
            scanline.SetPosition(0, new Vector3(c.janela.xMin, y));
            scanline.SetPosition(1, new Vector3(c.janela.xMax, y));
        });
        scanline.enabled = false;
        partida.Congelar(false);
        Repintando = false;
    }

    void Desenhar(Terreno.Chunk c)
    {
        var raiz = new GameObject("Aula").transform;
        raiz.SetParent(c.go.transform, false);          // some junto com o trecho
        Linha(raiz, "controle", true, 0.04f, Branco, AulaGeometria.Controle(c.trecho));
        foreach (var p in AulaGeometria.Controle(c.trecho))
            Linha(raiz, "ponto", true, 0.05f, Branco, AulaGeometria.Quadrado(p, 0.3f));
        Linha(raiz, "janela", true, 0.05f, Amarelo, AulaGeometria.Retangulo(c.janela));
        Linha(raiz, "antes do recorte", true, 0.04f, Vermelho, AulaGeometria.Fechado(c.poligonoOriginal));
        Linha(raiz, "recortado", true, 0.04f, Verde, AulaGeometria.Fechado(c.poligonoRecortado));
        var normais = new GameObject("normais").transform;
        normais.SetParent(raiz, false);
        var nv = Sombreamento.NormaisDosVertices(c.facetas);
        for (int k = 0; k < nv.Length; k++)
            Linha(normais, "normal", true, 0.04f, Magenta, new Vector3[] { c.facetas[k], c.facetas[k] + nv[k] * 0.8f });
        int i = terreno.chunks.IndexOf(c);
        if (c.agua is Rect agua)
        {
            Linha(raiz, "janela da agua", true, 0.06f, AzulClaro, AulaGeometria.Retangulo(agua));
            Linha(raiz, "linha da agua", true, 0.08f, Ciano,
                  new Vector3[] { new(agua.xMin - 2, agua.yMax), new(agua.xMax + 2, agua.yMax) });
        }
        if (i > 0) Linha(raiz, "emenda", true, 0.08f, Ciano, AulaGeometria.Emenda(terreno.chunks[i - 1].trecho, c.trecho));
        raiz.gameObject.SetActive(Ativo);
    }

    LineRenderer Linha(Transform pai, string nome, bool mundo, float largura, Color cor, Vector3[] pontos)
    {
        var go = new GameObject(nome);
        go.transform.SetParent(pai, false);
        var lr = go.AddComponent<LineRenderer>();
        lr.sharedMaterial = materialLinha;
        lr.useWorldSpace = mundo;
        lr.widthMultiplier = largura;
        lr.startColor = lr.endColor = cor;
        lr.sortingOrder = 50;                         // acima do carro, abaixo do HUD (100)
        lr.positionCount = pontos.Length;
        lr.SetPositions(pontos);
        return lr;
    }
}
