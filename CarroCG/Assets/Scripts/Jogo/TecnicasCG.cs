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
        T("Projeção paralela e perspectiva", Breve("F1 3D em wireframe na intro"), Sim("parallax do fundo (tecla P)")),
        T("Rasterização", Breve("Bresenham: wireframe e contorno do F1 na intro"), Sim("Bresenham: minimapa")),
        T("Preenchimento", Sim("scanline do terreno (tecla F no modo aula)"), Breve("regiões do F1 na intro"), Sim("sombra do F1")),
        T("Curva de Bézier", Sim("terreno infinito de Bézier cúbicas")),
        T("Recorte", Sim("Sutherland-Hodgman: terreno contra a janela do trecho"),
                     Sim("Sutherland-Hodgman: reflexo no lago"), Sim("Cohen-Sutherland: minimapa")),
        T("Iluminação e sombreamento", Sim("Lambert Flat/Gouraud/Phong no terreno (tecla L)"), Sim("shader Phong do F1"),
                                       Sim("sombra projetada do F1")),
        T("Translação", Sim("movimento do carro"), Sim("moedas e galões sobre a curva"), Sim("parallax")),
        T("Rotação", Sim("carro inclinando e rodas girando"), Sim("giro no ar"), Breve("wireframe 3D da intro")),
        T("Espelhamento", Sim("emenda C1 do terreno (P1 = 2·P3 - P2)"), Sim("reflexo no lago (y' = 2h - y)")),
    };

    public static string Texto() => string.Join("\n", Lista.Select(t =>
        $"<b>{t.nome}</b>\n" + string.Join("\n", t.lugares.Select(l =>
            $"    {(l.noJogo ? "<color=#5BE36B>no jogo</color>" : "<color=#9AA0A6>em breve</color>")}   {l.onde}"))));
}
