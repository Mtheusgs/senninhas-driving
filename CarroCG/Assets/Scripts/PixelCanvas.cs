using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Textura de baixa resolução onde o carro é rasterizado (contorno) e preenchido (scanline) aos poucos.
[RequireComponent(typeof(SpriteRenderer))]
public class PixelCanvas : MonoBehaviour
{
    [System.Serializable]
    public class Poly { public string nome; public Vector2Int[] vertices; public Color cor = Color.red; }

    public int largura = 48, altura = 24;
    public int pixelsPorUnidade = 16;
    public Color corContorno = new Color(0.08f, 0.08f, 0.12f);
    public Color corGrade = new Color(1, 1, 1, 0.15f);
    public Poly[] poligonos = CarroPadrao();

    Texture2D tex;
    int[] dono;   // 0 = sem contorno; n = contorno do polígono de índice n-1
    SpriteRenderer grade;

    public Texture2D Textura => tex;

    void Awake() => Inicializar();

    public void Inicializar()
    {
        tex = new Texture2D(largura, altura, TextureFormat.RGBA32, false)
            { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
        tex.SetPixels32(new Color32[largura * altura]);   // tudo transparente
        tex.Apply();
        dono = new int[largura * altura];
        var pivo = new Vector2(0.5f, 2f / altura);          // base das rodas: o carro "anda" sobre a pista
        GetComponent<SpriteRenderer>().sprite =
            Sprite.Create(tex, new Rect(0, 0, largura, altura), pivo, pixelsPorUnidade);
        CriarGrade(pivo);
    }

    // Grade de "papel quadriculado": 8 texels por pixel do carro, mesma área na cena.
    void CriarGrade(Vector2 pivo)
    {
        const int k = 8;
        var g = new Texture2D(largura * k, altura * k, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
        var px = new Color[g.width * g.height];
        for (int y = 0; y < g.height; y++)
            for (int x = 0; x < g.width; x++)
                px[y * g.width + x] = (x % k == 0 || y % k == 0) ? corGrade : Color.clear;
        g.SetPixels(px);
        g.Apply();
        if (grade == null)
        {
            grade = new GameObject("Grade").AddComponent<SpriteRenderer>();
            grade.transform.SetParent(transform, false);
        }
        grade.sprite = Sprite.Create(g, new Rect(0, 0, g.width, g.height), pivo, pixelsPorUnidade * k);
        grade.sortingOrder = GetComponent<SpriteRenderer>().sortingOrder + 1;
        grade.sharedMaterial = GetComponent<SpriteRenderer>().sharedMaterial;
    }

    public void MostrarGrade(bool visivel) => grade.enabled = visivel;

    bool Dentro(int x, int y) => x >= 0 && y >= 0 && x < largura && y < altura;

    // Rasterização: cada pixel de cada aresta (Bresenham) é um passo da animação.
    public IEnumerator DesenharContornos(float duracao)
    {
        var passos = new List<System.Action>();
        for (int n = 0; n < poligonos.Length; n++)
        {
            var v = poligonos[n].vertices;
            int id = n + 1;
            for (int i = 0; i < v.Length; i++)
                foreach (var px in Raster.Line(v[i], v[(i + 1) % v.Length]))
                    if (Dentro(px.x, px.y))
                        passos.Add(() =>
                        {
                            dono[px.y * largura + px.x] = id;
                            tex.SetPixel(px.x, px.y, corContorno);
                        });
        }
        return Executar(passos, duracao);
    }

    // Preenchimento: cada linha de scanline de cada polígono é um passo.
    // Não pinta sobre o próprio contorno nem sobre o de polígonos posteriores; cobre o de anteriores (ex.: roda sobre a lataria).
    public IEnumerator Preencher(float duracao)
    {
        var passos = new List<System.Action>();
        for (int n = 0; n < poligonos.Length; n++)
        {
            var cor = poligonos[n].cor;
            int id = n + 1;
            foreach (var (y, x0, x1) in Raster.ScanlineSpans(poligonos[n].vertices))
                passos.Add(() =>
                {
                    for (int x = x0; x <= x1; x++)
                        if (Dentro(x, y) && dono[y * largura + x] < id) tex.SetPixel(x, y, cor);
                });
        }
        return Executar(passos, duracao);
    }

    IEnumerator Executar(List<System.Action> passos, float duracao)
    {
        int feitos = 0;
        return Anim.Durante(duracao, k =>
        {
            for (int alvo = Mathf.CeilToInt(k * passos.Count); feitos < alvo; feitos++) passos[feitos]();
            tex.Apply();
        });
    }

    static Vector2Int[] Octogono(int cx, int cy, int r) =>
        Enumerable.Range(0, 8).Select(i => new Vector2Int(
            cx + Mathf.RoundToInt(r * Mathf.Cos(i * Mathf.PI / 4)),
            cy + Mathf.RoundToInt(r * Mathf.Sin(i * Mathf.PI / 4)))).ToArray();

    // Carro visto de lado, virado para +x, em 48x24. Ordem = ordem de desenho.
    static Poly[] CarroPadrao() => new[]
    {
        new Poly { nome = "lataria", cor = new Color(0.85f, 0.15f, 0.2f), vertices = new Vector2Int[]
            { new(2, 6), new(46, 6), new(46, 12), new(40, 14), new(34, 14), new(28, 20), new(14, 20), new(9, 14), new(2, 13) } },
        new Poly { nome = "janela traseira", cor = new Color(0.6f, 0.85f, 1f), vertices = new Vector2Int[]
            { new(12, 14), new(20, 14), new(20, 18), new(15, 18) } },
        new Poly { nome = "janela dianteira", cor = new Color(0.6f, 0.85f, 1f), vertices = new Vector2Int[]
            { new(22, 14), new(31, 14), new(27, 18), new(22, 18) } },
        new Poly { nome = "farol", cor = new Color(1f, 0.9f, 0.3f), vertices = new Vector2Int[]
            { new(43, 10), new(46, 10), new(46, 12), new(43, 12) } },
        new Poly { nome = "roda traseira", cor = new Color(0.15f, 0.15f, 0.18f), vertices = Octogono(11, 6, 4) },
        new Poly { nome = "roda dianteira", cor = new Color(0.15f, 0.15f, 0.18f), vertices = Octogono(37, 6, 4) },
        new Poly { nome = "calota traseira", cor = new Color(0.7f, 0.7f, 0.75f), vertices = Octogono(11, 6, 2) },
        new Poly { nome = "calota dianteira", cor = new Color(0.7f, 0.7f, 0.75f), vertices = Octogono(37, 6, 2) },
    };
}
