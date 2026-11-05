using System.Linq;

// Conteúdo da tela "Técnicas de CG". O selo de cada lugar é a fonte da verdade: muda quando a etapa entrega.
public static class TecnicasCG
{
    public struct Lugar
    {
        public string onde;
        public bool noJogo;
    }

    public struct Tecnica
    {
        public string nome;
        public Lugar[] lugares;
    }

    static Lugar Sim(string onde) => new Lugar { onde = onde, noJogo = true };
    static Lugar Breve(string onde) => new Lugar { onde = onde, noJogo = false };
    static Tecnica T(string nome, params Lugar[] lugares) => new Tecnica { nome = nome, lugares = lugares };

    public static readonly Tecnica[] Lista =
    {
        T("Projeção paralela e perspectiva", Breve("F1 3D em wireframe na intro"), Breve("parallax do fundo (tecla P)")),
        T("Rasterização", Breve("Bresenham: wireframe e contorno do F1 na intro"), Breve("Bresenham: minimapa")),
        T("Preenchimento", Sim("scanline do terreno (tecla F no modo aula)"), Breve("regiões do F1 na intro"), Breve("sombra do F1")),
        T("Curva de Bézier", Sim("terreno infinito de Bézier cúbicas")),
        T("Recorte", Sim("Sutherland-Hodgman: terreno contra a janela do trecho"),
                     Breve("Sutherland-Hodgman: reflexo no lago"), Breve("Cohen-Sutherland: minimapa")),
        T("Iluminação e sombreamento", Breve("Lambert Flat/Gouraud/Phong no terreno"), Breve("shader Phong do F1"),
                                       Breve("sombra projetada do F1")),
        T("Translação", Sim("movimento do carro"), Sim("moedas e galões sobre a curva"), Breve("parallax")),
        T("Rotação", Sim("carro inclinando e rodas girando"), Sim("giro no ar"), Breve("wireframe 3D da intro")),
        T("Espelhamento", Sim("emenda C1 do terreno (P1 = 2·P3 - P2)"), Sim("vitrine da garagem"), Breve("reflexo no lago")),
    };

    public static string Texto() => string.Join("\n", Lista.Select(t =>
        $"<b>{t.nome}</b>\n" + string.Join("\n", t.lugares.Select(l =>
            $"    {(l.noJogo ? "<color=#5BE36B>no jogo</color>" : "<color=#9AA0A6>em breve</color>")}   {l.onde}"))));
}
