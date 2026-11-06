using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Sombra projetada do F1: cada ponto do contorno do chassi e a base das rodas vão na direção oposta à luz até
// o chão (Sombra.Faixa); a faixa resultante, colada na superfície, é preenchida por scanline numa textura pequena.
public class SombraF1 : MonoBehaviour
{
    public F1 f1;
    public Terreno terreno;
    public CicloDia ciclo;
    public SpriteRenderer render;
    public int largura = 512, altura = 48;
    public float ppu = 32f;
    public float espessura = 0.22f;
    public float raioRoda = 0.36f;
    public byte alfaMax = 110, alfaLunar = 150;

    public bool Visivel => render && render.enabled;
    public Rect Area { get; private set; }

    Texture2D tex;
    Color32[] limpo, px;
    PolygonCollider2D contorno;
    Rigidbody2D[] rodas;

    void Start()
    {
        tex = new Texture2D(largura, altura, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, name = "Sombra" };
        limpo = new Color32[largura * altura];
        px = new Color32[largura * altura];
        render.sprite = Sprite.Create(tex, new Rect(0, 0, largura, altura), Vector2.zero, ppu);
        contorno = f1.GetComponent<PolygonCollider2D>();
        rodas = new[] { f1.eixoTraseiro.connectedBody, f1.eixoDianteiro.connectedBody };
    }

    void OnDestroy()
    {
        if (tex) Destroy(tex);
    }

    void LateUpdate()
    {
        float alfa = (ciclo.lunar ? alfaLunar : alfaMax) * (1 - ciclo.Atual.noite);
        var pontos = new List<Vector2>();
        for (int i = 0; i < contorno.pathCount; i++)
            foreach (var p in contorno.GetPath(i)) pontos.Add(f1.transform.TransformPoint(p));
        foreach (var r in rodas) pontos.Add(r.position + Vector2.down * raioRoda);
        var faixa = alfa < 2 ? null : Sombra.Faixa(pontos, ciclo.Atual.dirLuz, terreno.AlturaEm, largura / ppu - 0.5f);
        if (faixa == null) { render.enabled = false; return; }

        var (x0, x1) = faixa.Value;
        var superficie = new List<Vector2>();
        for (int k = 0; k <= 12; k++)
        {
            float x = Mathf.Lerp(x0, x1, k / 12f), y = terreno.AlturaEm(x);
            if (!float.IsNaN(y)) superficie.Add(new Vector2(x, y));
        }
        if (superficie.Count < 2) { render.enabled = false; return; }

        var origem = new Vector2(x0 - 0.1f, superficie.Min(p => p.y) - espessura - 0.1f);
        Area = Rect.MinMaxRect(x0, origem.y, x1, superficie.Max(p => p.y) + 0.02f);
        var poligono = superficie.Concat(Enumerable.Reverse(superficie).Select(p => p + Vector2.down * espessura))
                                 .Select(p => Vector2Int.RoundToInt((p - origem) * ppu)).ToArray();
        System.Array.Copy(limpo, px, px.Length);
        var cor = new Color32(0, 0, 0, (byte)alfa);
        foreach (var (y, a, b) in Raster.ScanlineSpans(poligono))                // preenchimento
        {
            if (y < 0 || y >= altura) continue;
            for (int x = Mathf.Max(0, a); x <= Mathf.Min(largura - 1, b); x++) px[y * largura + x] = cor;
        }
        tex.SetPixels32(px);
        tex.Apply();
        render.transform.position = origem;
        render.enabled = true;
    }
}
