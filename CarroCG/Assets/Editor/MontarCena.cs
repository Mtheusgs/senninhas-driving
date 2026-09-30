using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Monta Assets/Scenes/Main.unity (Task 6 do plano). Menu: CarroCG → Montar Cena. Sobrescreve a cena.
public static class MontarCena
{
    const string Cena = "Assets/Scenes/Main.unity";
    const float Zoom = 6.75f;   // 1080 / (2 * 6.75) = 80 px por unidade = 5 px por pixel do carro (16 px/unidade)

    [MenuItem("CarroCG/Montar Cena")]
    public static void Executar()
    {
        ConfigurarSprites();
        var cena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var matLinha = AssetDatabase.GetBuiltinExtraResource<Material>("Sprites-Default.mat");
        var quadrado = CarregarSprite("Assets/Art/Gerado/quadrado.png");
        var circulo = CarregarSprite("Assets/Art/Gerado/circulo.png");

        // Câmera (com o Director) e o fade preto preso a ela
        var cam = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener)).GetComponent<Camera>();
        cam.tag = "MainCamera";
        cam.orthographic = true;
        cam.orthographicSize = Zoom;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.black;
        cam.transform.position = new Vector3(0, 0, -10);
        var diretor = cam.gameObject.AddComponent<Director>();
        diretor.zoomGeral = Zoom;

        var fade = NovoSprite("Fade", quadrado, 100, cam.transform);
        fade.transform.localPosition = new Vector3(0, 0, 10);
        fade.transform.localScale = new Vector3(40, 40, 1);
        fade.color = new Color(0, 0, 0, 0);

        // Pista: estrada no próprio objeto, pontos de controle e linhas-guia como filhos
        var pista = new GameObject("Pista");
        var estrada = pista.AddComponent<LineRenderer>();
        ConfigurarLinha(estrada, matLinha, 0.3f, new Color(0.35f, 0.35f, 0.4f), 0);
        estrada.numCapVertices = 4;
        var bezier = pista.AddComponent<BezierPath>();
        var pontos = new[] { new Vector2(-8, -3), new Vector2(-3, -3), new Vector2(1, 4), new Vector2(8, 1) };
        var ts = new Transform[4];
        for (int i = 0; i < 4; i++)
        {
            var p = NovoSprite("P" + i, circulo, 2, pista.transform);
            p.transform.position = pontos[i];
            p.transform.localScale = Vector3.one * 0.3f;
            ts[i] = p.transform;
        }
        bezier.p0 = ts[0]; bezier.p1 = ts[1]; bezier.p2 = ts[2]; bezier.p3 = ts[3];
        var guias = new GameObject("Guias").AddComponent<LineRenderer>();
        guias.transform.SetParent(pista.transform, false);
        ConfigurarLinha(guias, matLinha, 0.05f, Color.white, 1);
        bezier.guias = guias;

        // Carro
        var carro = new GameObject("Carro", typeof(SpriteRenderer));
        carro.GetComponent<SpriteRenderer>().sortingOrder = 3;
        var canvas = carro.AddComponent<PixelCanvas>();
        var follower = carro.AddComponent<CarFollower>();
        follower.pista = bezier;

        // Cenário: tiles 24x24 do Kenney Pixel Platformer (CC0), 1.5 unidade cada. Horizonte acima da pista.
        var cenario = new GameObject("Cenario");
        const float tam = 1.5f;
        for (int ix = -8; ix <= 8; ix++)
            for (int iy = -5; iy <= 5; iy++)
            {
                int linha = iy > 2 ? 0 : iy == 2 ? 1 : 2;       // céu, horizonte, chão
                int col = ((ix % 4) + 4) % 4;
                var tile = NovoSprite("tile", CarregarSprite($"Assets/Art/Kenney/tile_{linha * 8 + col:0000}.png"), -10, cenario.transform);
                tile.transform.position = new Vector3(ix * tam, iy * tam, 0);
            }

        diretor.canvas = canvas;
        diretor.carro = follower;
        diretor.pista = bezier;
        diretor.cenario = cenario;
        diretor.fade = fade;

        Directory.CreateDirectory("Assets/Scenes");
        EditorSceneManager.SaveScene(cena, Cena);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(Cena, true) };
        Debug.Log("MontarCena: cena salva em " + Cena);
    }

    static Sprite CarregarSprite(string caminho) => AssetDatabase.LoadAssetAtPath<Sprite>(caminho);

    static SpriteRenderer NovoSprite(string nome, Sprite sprite, int ordem, Transform pai)
    {
        var r = new GameObject(nome).AddComponent<SpriteRenderer>();
        r.transform.SetParent(pai, false);
        r.sprite = sprite;
        r.sortingOrder = ordem;
        return r;
    }

    static void ConfigurarLinha(LineRenderer lr, Material mat, float largura, Color cor, int ordem)
    {
        lr.sharedMaterial = mat;
        lr.widthMultiplier = largura;
        lr.startColor = lr.endColor = cor;
        lr.useWorldSpace = true;
        lr.sortingOrder = ordem;
        lr.positionCount = 0;
    }

    // Pixel art: filtro Point, sem compressão, malha retangular (evita frestas entre tiles).
    static void ConfigurarSprites()
    {
        foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Art" }))
        {
            var caminho = AssetDatabase.GUIDToAssetPath(guid);
            var imp = (TextureImporter)AssetImporter.GetAtPath(caminho);
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.filterMode = FilterMode.Point;
            imp.textureCompression = TextureImporterCompression.Uncompressed;
            imp.mipmapEnabled = false;
            imp.spritePixelsPerUnit = caminho.EndsWith("circulo.png") ? 32 : caminho.EndsWith("quadrado.png") ? 4 : 16;
            var s = new TextureImporterSettings();
            imp.ReadTextureSettings(s);
            s.spriteMeshType = SpriteMeshType.FullRect;
            imp.SetTextureSettings(s);
            imp.SaveAndReimport();
        }
    }
}
