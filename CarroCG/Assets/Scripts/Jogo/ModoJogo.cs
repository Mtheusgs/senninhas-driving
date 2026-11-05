using UnityEngine;

public struct ParametrosModo
{
    public float gravidade;                 // y, negativo
    public int semente;
    public float amplitudeInicial, ganhoPorTrecho, amplitudeMax;
    public float torqueNoAr;
    public Color ceu;
    public TerrenoChunk.Paleta paleta;
    public bool ceuLunar;
}

// Terra e Lua: mesma geração (Bézier, recorte, scanline), parâmetros diferentes.
public static class ModoJogo
{
    public const float GravidadeTerra = -9.81f;

    public static ParametrosModo Parametros(Modo m) => m == Modo.Lua
        ? new ParametrosModo
        {
            gravidade = GravidadeTerra * 0.22f,            // "gravidade bugada": o carro flutua
            semente = 42,
            amplitudeInicial = 5f, ganhoPorTrecho = 0.5f, amplitudeMax = 13f,
            torqueNoAr = 16f,
            ceu = Color.black,
            ceuLunar = true,
            paleta = new TerrenoChunk.Paleta
            {
                gramaBorda = new Color(0.86f, 0.86f, 0.9f),
                grama = new Color(0.7f, 0.7f, 0.74f),
                terraClara = new Color(0.56f, 0.56f, 0.6f),
                terraEscura = new Color(0.24f, 0.24f, 0.28f),
            },
        }
        : new ParametrosModo
        {
            gravidade = GravidadeTerra,
            semente = 7,
            amplitudeInicial = 2.5f, ganhoPorTrecho = 0.5f, amplitudeMax = 12f,
            torqueNoAr = 16f,
            ceu = new Color(0.55f, 0.78f, 0.95f),
            ceuLunar = false,
            paleta = new TerrenoChunk.Paleta(),
        };
}
