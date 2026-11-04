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
        public GameObject go;
        public Texture2D tex;
    }

    public Transform alvo;
    public int semente = 7;
    public float ppu = 64;
    public int amostras = 64;
    public int trechosAFrente = 3;
    public float profundidade = 12f;
    public float alturaMaxJanela = 32f;
    public TerrenoChunk.Paleta paleta = new();
    public PhysicsMaterial2D materialChao;

    public readonly List<Chunk> chunks = new();
    public event System.Action<Chunk> TrechoCriado;
    public TerrenoGerador Gerador { get; private set; }

    void Awake() => Reiniciar();

    void Update()
    {
        if (alvo) Atualizar(alvo.position.x);
    }

    public void Reiniciar()
    {
        foreach (var c in chunks) Liberar(c);
        chunks.Clear();
        Gerador = new TerrenoGerador(semente);
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
    }

    public Chunk GerarProximo()
    {
        var t = Gerador.Proximo();
        int indice = Gerador.Indice - 1;
        var curva = TerrenoGerador.Amostrar(t, amostras);
        float topo = curva.Max(p => p.y) + 0.5f;
        float fundo = curva.Min(p => p.y) - profundidade;
        var janela = Rect.MinMaxRect(t.p0.x, Mathf.Max(fundo, topo - alturaMaxJanela), t.p3.x, topo);

        var poligono = Raster.ClipPolygon(TerrenoChunk.Poligono(curva, fundo), janela);          // recorte
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

        var c = new Chunk { trecho = t, indice = indice, curva = curva, janela = janela, go = go, tex = tex };
        chunks.Add(c);
        TrechoCriado?.Invoke(c);
        return c;
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
    }

    static void Destruir(Object o)
    {
        if (Application.isPlaying) Destroy(o);
        else DestroyImmediate(o);
    }
}
