// GERADO por Tools/gerar_f1.py: não editar. Regiões do F1 para a intro (pixels da arte 960x300, y para baixo).
using UnityEngine;

public static class ContornoF1
{
    public const int PlacaTraseira = 0;
    public const int AsaTraseira = 1;
    public const int AsaTraseira2 = 2;
    public const int Assoalho = 3;
    public const int Chassi = 4;
    public const int Cobertura = 5;
    public const int Cockpit = 6;
    public const int Capacete = 7;
    public const int AsaDianteira = 8;
    public const int AsaDianteira2 = 9;
    public const int PlacaDianteira = 10;
    public const int RodaTraseira = 11;
    public const int RodaDianteira = 12;
    public static readonly Vector2[][] Regioes =
    {
        new Vector2[] { new(58f, 30f), new(84f, 30f), new(96f, 150f), new(70f, 150f) },
        new Vector2[] { new(50f, 34f), new(178f, 34f), new(178f, 54f), new(50f, 58f) },
        new Vector2[] { new(60f, 66f), new(170f, 64f), new(170f, 78f), new(60f, 80f) },
        new Vector2[] { new(120f, 200f), new(880f, 200f), new(880f, 214f), new(120f, 214f) },
        new Vector2[] { new(105f, 205f), new(115f, 150f), new(230f, 134f), new(330f, 126f), new(440f, 118f), new(560f, 116f), new(650f, 126f), new(770f, 150f), new(880f, 178f), new(935f, 192f), new(935f, 205f) },
        new Vector2[] { new(240f, 136f), new(290f, 78f), new(350f, 56f), new(420f, 58f), new(466f, 96f), new(470f, 122f), new(240f, 138f) },
        new Vector2[] { new(470f, 118f), new(476f, 100f), new(560f, 100f), new(575f, 118f) },
        new Vector2[] { new(541.0f, 92.0f), new(537.5f, 105.0f), new(528.0f, 114.5f), new(515.0f, 118.0f), new(502.0f, 114.5f), new(492.5f, 105.0f), new(489.0f, 92.0f), new(492.5f, 79.0f), new(502.0f, 69.5f), new(515.0f, 66.0f), new(528.0f, 69.5f), new(537.5f, 79.0f) },
        new Vector2[] { new(800f, 212f), new(955f, 208f), new(955f, 222f), new(800f, 224f) },
        new Vector2[] { new(820f, 198f), new(950f, 192f), new(950f, 204f), new(820f, 208f) },
        new Vector2[] { new(930f, 178f), new(956f, 178f), new(956f, 224f), new(930f, 224f) },
        new Vector2[] { new(282.0f, 178.0f), new(269.7f, 224.0f), new(236.0f, 257.7f), new(190.0f, 270.0f), new(144.0f, 257.7f), new(110.3f, 224.0f), new(98.0f, 178.0f), new(110.3f, 132.0f), new(144.0f, 98.3f), new(190.0f, 86.0f), new(236.0f, 98.3f), new(269.7f, 132.0f) },
        new Vector2[] { new(862.0f, 178.0f), new(849.7f, 224.0f), new(816.0f, 257.7f), new(770.0f, 270.0f), new(724.0f, 257.7f), new(690.3f, 224.0f), new(678.0f, 178.0f), new(690.3f, 132.0f), new(724.0f, 98.3f), new(770.0f, 86.0f), new(816.0f, 98.3f), new(849.7f, 132.0f) },
    };
    public static readonly Color32[] Cores =
    {
        new(208, 26, 42, 255),
        new(24, 24, 28, 255),
        new(24, 24, 28, 255),
        new(24, 24, 28, 255),
        new(208, 26, 42, 255),
        new(208, 26, 42, 255),
        new(24, 24, 28, 255),
        new(250, 204, 40, 255),
        new(24, 24, 28, 255),
        new(24, 24, 28, 255),
        new(208, 26, 42, 255),
        new(24, 24, 28, 255),
        new(24, 24, 28, 255),
    };
    public static Vector2 EmUnidades(Vector2 px) => new((px.x - 480) / 256f, (150 - px.y) / 256f);
}
