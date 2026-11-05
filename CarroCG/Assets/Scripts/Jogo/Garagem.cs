using UnityEngine;

// Cores da pintura do F1. A escolha fica em Preferencias.Cor.
public static class Garagem
{
    public static readonly Color[] Cores =
    {
        new(0.97f, 0.12f, 0.2f), new(0.12f, 0.36f, 0.95f), new(0.1f, 0.75f, 0.32f), new(1f, 0.5f, 0.05f),
        new(1f, 0.85f, 0.1f), new(0.8f, 0.82f, 0.86f), new(0.16f, 0.16f, 0.18f), new(1f, 0.42f, 0.72f),
    };

    public static readonly string[] Nomes = { "Vermelho", "Azul", "Verde", "Laranja", "Amarelo", "Prata", "Preto", "Rosa" };

    public static int IndiceEscolhido => Preferencias.Cor >= 0 && Preferencias.Cor < Cores.Length ? Preferencias.Cor : 0;
    public static Color CorEscolhida => Cores[IndiceEscolhido];
}
