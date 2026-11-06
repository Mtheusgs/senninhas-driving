using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Terreno infinito: trecho de Bézier → polígono recortado (Sutherland–Hodgman) → textura por scanline + collider.
public class Terreno : MonoBehaviour
{
    public class Chunk
    {
        public Trecho trecho;
        public int indice;
        public Vector2[] curva;
        public Rect janela;
        public List<Vector2> poligonoOriginal, poligonoRecortado;   // antes e depois do Sutherland–Hodgman
        public Vector2[] facetas;         // pontos usados no sombreamento (poucos, para o Flat aparecer)
        public Texture2D luz;             // cor da luz por coluna de pixel (w×1)
        public GameObject go;
        public Texture2D tex;
    }

    public Transform alvo;
    public int semente = 7;
    public float ppu = 64;
    public int amostras = 64;
    public int trechosAFrente = 3;
    public float profundidade = 18f;          // cobre a câmera no zoom máximo olhando para baixo
    public float fundoExtra = 4f;             // o polígono desce além da janela: o recorte sempre corta e o pedaço cortado aparece no modo aula
    public float amplitudeInicial = 2.5f, ganhoPorTrecho = 0.5f, amplitudeMax = 12f;
    public TerrenoChunk.Paleta paleta = new();
    public PhysicsMaterial2D materialChao;
    public Material materialLuz;                       // CarroCG/TerrenoLuz: cor × luz por coluna
    public int amostrasLuz = 12;
    public ModoSombreamento modoSombreamento = ModoSombreamento.Phong;

    Vector2 dirLuz = Vector2.up;
    Color corSol = Color.white, corAmbiente = new(0.42f, 0.44f, 0.5f);
    MaterialPropertyBlock mpb;
    static readonly int IdLuz = Shader.PropertyToID("_Luz");

    public readonly List<Chunk> chunks = new();
    public event System.Action<Chunk> TrechoCriado;
    public TerrenoGerador Gerador { get; private set; }

    EdgeCollider2D parede;   // borda esquerda do mundo: acompanha o início do trecho mais antigo

    void Awake() => Reiniciar();

    void Update()
    {
        if (alvo) Atualizar(alvo.position.x);
    }

    public void Reiniciar()
    {
        foreach (var c in chunks) Liberar(c);
        chunks.Clear();
        Gerador = new TerrenoGerador(semente)
            { amplitudeInicial = amplitudeInicial, ganhoPorTrecho = ganhoPorTrecho, amplitudeMax = amplitudeMax };
        if (!parede)
        {
            parede = GetComponent<EdgeCollider2D>();
            if (!parede) parede = gameObject.AddComponent<EdgeCollider2D>();
            parede.points = new Vector2[] { new(0, -1000), new(0, 1000) };
        }
    }

    // Mantém trechos até trechosAFrente larguras à frente de x e descarta os que ficaram uma largura para trás.
    public void Atualizar(float x)
    {
        while (chunks.Count == 0 || chunks[^1].trecho.p3.x < x + trechosAFrente * Gerador.largura) GerarProximo();
        while (chunks.Count > 1 && chunks[0].trecho.p3.x < x - Gerador.largura)
        {
            Liberar(chunks[0]);
            chunks.RemoveAt(0);
        }
        parede.offset = new Vector2(chunks[0].trecho.p0.x - transform.position.x, 0);
    }

    // Iluminação do terreno: guarda a luz atual e recalcula a luz por coluna de todos os trechos.
    public void Iluminar(Vector2 dir, Color sol, Color ambiente)
    {
        dirLuz = dir;
        corSol = sol;
        corAmbiente = ambiente;
        foreach (var c in chunks) AplicarLuz(c);
    }

    public void ProximoSombreamento()
    {
        modoSombreamento = (ModoSombreamento)(((int)modoSombreamento + 1) % 3);
        foreach (var c in chunks) AplicarLuz(c);
    }

    void AplicarLuz(Chunk c)
    {
        c.luz.SetPixels32(Sombreamento.LuzPorColuna(modoSombreamento, c.facetas, c.janela, ppu, c.luz.width, dirLuz, corSol, corAmbiente));
        c.luz.Apply();
        var r = c.go.GetComponent<SpriteRenderer>();
        if (materialLuz) r.sharedMaterial = materialLuz;
        mpb ??= new MaterialPropertyBlock();
        r.GetPropertyBlock(mpb);
        mpb.SetTexture(IdLuz, c.luz);
        r.SetPropertyBlock(mpb);
    }

    // Altura da superfície em x pelo trecho que o contém; NaN fora dos trechos existentes.
    public float AlturaEm(float x)
    {
        foreach (var c in chunks)
            if (x >= c.trecho.p0.x && x <= c.trecho.p3.x) return TerrenoChunk.AlturaEm(c.curva, x);
        return float.NaN;
    }

    public Chunk GerarProximo()
    {
        var t = Gerador.Proximo();
        int indice = Gerador.Indice - 1;
        var curva = TerrenoGerador.Amostrar(t, amostras);
        float topo = curva.Max(p => p.y) + 0.5f;
        float baseJanela = curva.Min(p => p.y) - profundidade;
        var janela = Rect.MinMaxRect(t.p0.x, baseJanela, t.p3.x, topo);

        var original = TerrenoChunk.Poligono(curva, baseJanela - fundoExtra);
        var poligono = Raster.ClipPolygon(original, janela);                                       // recorte
        var px = TerrenoChunk.Pintar(poligono, janela, ppu, curva, paleta, out int w, out int h);   // preenchimento

        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
            { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear, name = $"Trecho {indice}" };
        tex.SetPixels32(px);
        tex.Apply();

        var go = new GameObject($"Trecho {indice}");
        go.transform.SetParent(transform, false);
        go.transform.position = janela.min;
        go.AddComponent<SpriteRenderer>().sprite = Sprite.Create(tex, new Rect(0, 0, w, h), Vector2.zero, ppu);
        var col = go.AddComponent<EdgeCollider2D>();
        col.points = curva.Select(p => p - janela.min).ToArray();
        col.sharedMaterial = materialChao;
        if (chunks.Count > 0) Emendar(chunks[^1], go, col);
        var luz = new Texture2D(w, 1, TextureFormat.RGBA32, false)
            { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Point, name = $"Luz {indice}" };

        var c = new Chunk
        {
            trecho = t, indice = indice, curva = curva, janela = janela, go = go, tex = tex,
            poligonoOriginal = original, poligonoRecortado = poligono,
            facetas = TerrenoGerador.Amostrar(t, amostrasLuz + 1), luz = luz,
        };
        AplicarLuz(c);
        chunks.Add(c);
        TrechoCriado?.Invoke(c);
        return c;
    }

    // Modo aula: apaga o trecho e o repinta de baixo para cima, uma linha do scanline por vez, em tempo real
    // (funciona com o jogo congelado). Os pixels vêm do mesmo preenchimento do trecho.
    public IEnumerator RepintarAnimado(Chunk c, float duracao, System.Action<float> linhaAtual = null)
    {
        var px = TerrenoChunk.Pintar(c.poligonoRecortado, c.janela, ppu, c.curva, paleta, out int w, out int h);
        c.tex.SetPixels32(new Color32[w * h]);
        c.tex.Apply();
        int feitas = 0;
        return Anim.Durante(duracao, k =>
        {
            int alvo = Mathf.CeilToInt(k * h);
            if (alvo > feitas)
            {
                var linhas = new Color32[(alvo - feitas) * w];
                System.Array.Copy(px, feitas * w, linhas, 0, linhas.Length);
                c.tex.SetPixels32(0, feitas, w, alvo - feitas, linhas);
                c.tex.Apply();
                feitas = alvo;
            }
            linhaAtual?.Invoke(c.janela.yMin + feitas / ppu);
        }, tempoReal: true);
    }

    // Pontos vizinhos nas emendas: a roda passa de um collider para o outro sem "degrau".
    static void Emendar(Chunk anterior, GameObject go, EdgeCollider2D col)
    {
        var colAnt = anterior.go.GetComponent<EdgeCollider2D>();
        Vector2 o = go.transform.position, oAnt = anterior.go.transform.position;
        colAnt.useAdjacentEndPoint = true;
        colAnt.adjacentEndPoint = col.points[1] + o - oAnt;
        col.useAdjacentStartPoint = true;
        col.adjacentStartPoint = colAnt.points[^2] + oAnt - o;
    }

    static void Liberar(Chunk c)
    {
        Destruir(c.go.GetComponent<SpriteRenderer>().sprite);
        Destruir(c.go);
        Destruir(c.tex);
        Destruir(c.luz);
    }

    static void Destruir(Object o)
    {
        if (Application.isPlaying) Destroy(o);
        else DestroyImmediate(o);
    }
}
