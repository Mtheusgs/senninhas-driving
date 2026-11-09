using System.Linq;
using UnityEngine;
using UnityEngine.UI;

// Minimapa do HUD: perfil do terreno em volta do carro, desenhado por MinimapaDesenho (Cohen–Sutherland,
// janela → viewport, Bresenham) numa textura pequena.
public class Minimapa : MonoBehaviour
{
    public Terreno terreno;
    public F1 f1;
    public RawImage imagem;
    public Vector2 tamanhoJanela = new(120, 45);
    public int largura = 256, altura = 96;
    public float intervalo = 0.1f;

    public Rect Janela { get; private set; }
    public Texture2D Textura { get; private set; }

    static readonly Color32 Linha = new(255, 255, 255, 255), Carro = new(255, 0, 0, 255), Fundo = new(0, 0, 0, 128);
    Color32[] px;
    float proximo;

    void Start()
    {
        Textura = new Texture2D(largura, altura, TextureFormat.RGBA32, false)
            { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp, name = "Minimapa" };
        px = new Color32[largura * altura];
        imagem.texture = Textura;
        Atualizar();
    }

    void OnDestroy()
    {
        if (Textura) Destroy(Textura);
    }

    void Update()
    {
        if (Time.unscaledTime >= proximo) Atualizar();
    }

    public void Atualizar()
    {
        proximo = Time.unscaledTime + intervalo;
        Vector2 carro = f1.transform.position;
        Janela = new Rect(carro - tamanhoJanela / 2, tamanhoJanela);
        MinimapaDesenho.Desenhar(px, largura, altura, Janela, MinimapaDesenho.Segmentos(terreno.chunks.Select(c => c.curva)),
                                 carro, Linha, Carro, Fundo);
        Textura.SetPixels32(px);
        Textura.Apply();
    }
}
