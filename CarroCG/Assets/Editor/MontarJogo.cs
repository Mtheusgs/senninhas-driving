using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

// Monta Assets/Scenes/Jogo.unity (etapa 1). Menu: CarroCG → Montar Jogo. Sobrescreve a cena.
public static class MontarJogo
{
    const string Cena = "Assets/Scenes/Jogo.unity";
    const float Ppu = 256f;

    // Layout da arte 960x300 (Tools/gerar_f1.py) em unidades, com o pivô no centro do sprite.
    static Vector2 Local(float x, float y) => new((x - 480) / Ppu, (150 - y) / Ppu);
    static readonly Vector2 RodaT = Local(190, 178), RodaD = Local(770, 178), Capacete = Local(515, 92);
    const float RaioRoda = 92 / Ppu;

    [MenuItem("CarroCG/Montar Jogo")]
    public static void Executar()
    {
        ConfigurarArte();
        var pneu = Material2D("Pneu", 1.6f);
        var chao = Material2D("Chao", 1.0f);
        var cena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // Luz global: sprites do URP 2D são iluminados
        var luz = new GameObject("Luz Global").AddComponent<Light2D>();
        var so = new SerializedObject(luz);
        so.FindProperty("m_LightType").intValue = (int)Light2D.LightType.Global;
        so.ApplyModifiedPropertiesWithoutUndo();
        luz.intensity = 1f;

        // F1: chassi + 2 rodas com suspensão. Largada em x=12, no 1º trecho plano: a câmera não mostra o começo do mundo.
        var carro = new GameObject("Carro");
        var chassi = NovoCorpo("Chassi", carro.transform, new Vector2(12, 1.2f), CarregarSprite("Assets/Art/F1/f1_corpo.png"), 2, 1.5f);
        chassi.AddComponent<PolygonCollider2D>();
        var f1 = chassi.AddComponent<F1>();
        var rodaT = NovaRoda("Roda Traseira", carro.transform, (Vector2)chassi.transform.position + RodaT, pneu);
        var rodaD = NovaRoda("Roda Dianteira", carro.transform, (Vector2)chassi.transform.position + RodaD, pneu);
        f1.eixoTraseiro = Eixo(chassi, rodaT, RodaT);
        f1.eixoDianteiro = Eixo(chassi, rodaD, RodaD);
        var capacete = new GameObject("Capacete") { tag = "Player" };
        capacete.transform.SetParent(chassi.transform, false);
        capacete.transform.localPosition = Capacete;
        var cc = capacete.AddComponent<CircleCollider2D>();
        cc.isTrigger = true;
        cc.radius = 0.3f;   // cobre capacete + halo: de teto no chão, passa da asa traseira (0,47) e encosta
        capacete.AddComponent<SensorCapacete>().f1 = f1;

        // Câmera
        var cam = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener)).GetComponent<Camera>();
        cam.tag = "MainCamera";
        cam.orthographic = true;
        cam.orthographicSize = 7;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.55f, 0.78f, 0.95f);
        cam.transform.position = new Vector3(15, 2.7f, -10);
        cam.gameObject.AddComponent<CameraSegue>().alvo = chassi.GetComponent<Rigidbody2D>();

        // Terreno e coletáveis
        var terreno = new GameObject("Terreno").AddComponent<Terreno>();
        terreno.alvo = chassi.transform;
        terreno.materialChao = chao;
        var coletaveis = new GameObject("Coletaveis").AddComponent<Coletaveis>();
        coletaveis.terreno = terreno;
        coletaveis.moeda = CarregarSprite("Assets/Art/F1/moeda.png");
        coletaveis.galao = CarregarSprite("Assets/Art/F1/galao.png");

        // Partida + HUD
        var partida = new GameObject("Partida").AddComponent<Partida>();
        partida.f1 = f1;
        MontarHud(partida, cam);

        Directory.CreateDirectory("Assets/Scenes");
        EditorSceneManager.SaveScene(cena, Cena);
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(Cena, true),
            new EditorBuildSettingsScene("Assets/Scenes/Main.unity", true),
        };
        Debug.Log("MontarJogo: cena salva em " + Cena);
    }

    static GameObject NovoCorpo(string nome, Transform pai, Vector2 pos, Sprite sprite, int ordem, float massa)
    {
        var go = new GameObject(nome) { tag = "Player" };
        go.transform.SetParent(pai, false);
        go.transform.position = pos;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = ordem;
        var rb = go.AddComponent<Rigidbody2D>();
        rb.mass = massa;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        return go;
    }

    static GameObject NovaRoda(string nome, Transform pai, Vector2 pos, PhysicsMaterial2D pneu)
    {
        var go = NovoCorpo(nome, pai, pos, CarregarSprite("Assets/Art/F1/f1_roda.png"), 3, 0.25f);
        var c = go.AddComponent<CircleCollider2D>();
        c.radius = RaioRoda;
        c.sharedMaterial = pneu;
        return go;
    }

    static WheelJoint2D Eixo(GameObject chassi, GameObject roda, Vector2 ancora)
    {
        var j = chassi.AddComponent<WheelJoint2D>();
        j.connectedBody = roda.GetComponent<Rigidbody2D>();
        j.autoConfigureConnectedAnchor = false;
        j.anchor = ancora;
        j.connectedAnchor = Vector2.zero;
        j.suspension = new JointSuspension2D { dampingRatio = 0.7f, frequency = 6f, angle = 90f };
        j.useMotor = false;
        return j;
    }

    static PhysicsMaterial2D Material2D(string nome, float atrito)
    {
        Directory.CreateDirectory("Assets/Fisica");
        string caminho = $"Assets/Fisica/{nome}.physicsMaterial2D";
        var m = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(caminho);
        if (m == null)
        {
            m = new PhysicsMaterial2D(nome);
            AssetDatabase.CreateAsset(m, caminho);
        }
        m.friction = atrito;
        m.bounciness = 0;
        EditorUtility.SetDirty(m);
        AssetDatabase.SaveAssets();
        return m;
    }

    static void ConfigurarArte()
    {
        AssetDatabase.Refresh();
        foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Art/F1" }))
        {
            var caminho = AssetDatabase.GUIDToAssetPath(guid);
            var imp = (TextureImporter)AssetImporter.GetAtPath(caminho);
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.filterMode = FilterMode.Bilinear;
            imp.textureCompression = TextureImporterCompression.Uncompressed;
            imp.mipmapEnabled = true;
            imp.alphaIsTransparency = true;
            imp.spritePixelsPerUnit = caminho.Contains("/f1_") ? Ppu : 200;
            imp.SaveAndReimport();
        }
    }

    static Sprite CarregarSprite(string caminho) => AssetDatabase.LoadAssetAtPath<Sprite>(caminho);

    // ---------- HUD (Screen Space - Camera, para aparecer nas capturas) ----------

    static void MontarHud(Partida p, Camera cam)
    {
        var go = new GameObject("HUD", typeof(Canvas), typeof(CanvasScaler));
        var canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = cam;
        canvas.planeDistance = 1;
        canvas.sortingOrder = 100;          // por cima de carro (2-3) e moedas (1)
        var scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        var raiz = go.transform;

        var fundoBarra = Caixa("Combustivel", raiz, new Vector2(0, 1), new Vector2(40, -40), new Vector2(420, 34), new Color(0, 0, 0, 0.55f));
        // trilho com a margem; a barra dentro dele não tem margem, então com 0% fica com largura 0 (sem lasca)
        var trilho = new GameObject("Trilho", typeof(RectTransform)).GetComponent<RectTransform>();
        trilho.SetParent(fundoBarra, false);
        trilho.anchorMin = Vector2.zero;
        trilho.anchorMax = Vector2.one;
        trilho.offsetMin = new Vector2(4, 4);
        trilho.offsetMax = new Vector2(-4, -4);
        var barra = Caixa("Nivel", trilho, Vector2.zero, Vector2.zero, Vector2.zero, new Color(1f, 0.55f, 0.1f));
        barra.anchorMin = Vector2.zero;
        barra.anchorMax = Vector2.one;
        barra.offsetMin = barra.offsetMax = Vector2.zero;
        p.barraCombustivel = barra;

        var caixaMoedas = Caixa("Moedas", raiz, new Vector2(0, 1), new Vector2(40, -90), new Vector2(260, 56), new Color(0, 0, 0, 0.45f));
        p.textoMoedas = Texto("Texto", caixaMoedas, Vector2.zero, Vector2.one, 34, TextAlignmentOptions.Center);

        var caixaDist = Caixa("Distancia", raiz, new Vector2(0.5f, 1), new Vector2(0, -30), new Vector2(380, 120), new Color(0, 0, 0, 0.45f));
        p.textoDistancia = Texto("Metros", caixaDist, new Vector2(0, 0.35f), Vector2.one, 60, TextAlignmentOptions.Center);
        p.textoRecorde = Texto("Recorde", caixaDist, Vector2.zero, new Vector2(1, 0.4f), 26, TextAlignmentOptions.Center);

        p.painelPausa = Painel("Pausa", raiz, "PAUSADO\n<size=50%>Esc para continuar</size>").gameObject;
        var fim = Painel("Fim", raiz, "");
        p.painelFim = fim.gameObject;
        p.textoFim = fim.GetComponentInChildren<TMP_Text>();
    }

    static RectTransform Caixa(string nome, Transform pai, Vector2 ancora, Vector2 pos, Vector2 tamanho, Color cor)
    {
        var go = new GameObject(nome, typeof(RectTransform), typeof(Image));
        var rt = (RectTransform)go.transform;
        rt.SetParent(pai, false);
        rt.anchorMin = rt.anchorMax = rt.pivot = ancora;
        rt.anchoredPosition = pos;
        rt.sizeDelta = tamanho;
        go.GetComponent<Image>().color = cor;
        return rt;
    }

    static TMP_Text Texto(string nome, Transform pai, Vector2 ancoraMin, Vector2 ancoraMax, float tamanho, TextAlignmentOptions alinhamento)
    {
        var go = new GameObject(nome, typeof(RectTransform));
        var rt = (RectTransform)go.transform;
        rt.SetParent(pai, false);
        rt.anchorMin = ancoraMin;
        rt.anchorMax = ancoraMax;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        var t = go.AddComponent<TextMeshProUGUI>();
        t.fontSize = tamanho;
        t.alignment = alinhamento;
        t.fontStyle = FontStyles.Bold;
        t.color = Color.white;
        return t;
    }

    static RectTransform Painel(string nome, Transform pai, string texto)
    {
        var rt = Caixa(nome, pai, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, new Color(0, 0, 0, 0.6f));
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        Texto("Texto", rt, new Vector2(0.15f, 0.2f), new Vector2(0.85f, 0.8f), 84, TextAlignmentOptions.Center).text = texto;
        return rt;
    }
}
