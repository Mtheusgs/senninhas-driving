using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

// Peças comuns às cenas montadas por script (Jogo e Menu): arte, física, F1, câmera, terreno, céu e UI.
public static class MontagemComum
{
    public const float Ppu = 256f;

    // Layout da arte 960x300 (Tools/gerar_f1.py) em unidades, com o pivô no centro do sprite.
    static Vector2 Local(float x, float y) => new((x - 480) / Ppu, (150 - y) / Ppu);
    public static readonly Vector2 RodaT = Local(190, 178), RodaD = Local(770, 178), Capacete = Local(515, 92);
    public const float RaioRoda = 92 / Ppu;
    public static readonly Vector2 Largada = new(12, 1.2f);   // 1º trecho plano: a câmera não vê o começo do mundo

    public static Material MaterialUnlit => AssetDatabase.LoadAssetAtPath<Material>(
        "Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Unlit-Default.mat");

    public static void DefinirBuild() => EditorBuildSettings.scenes = new[]
    {
        new EditorBuildSettingsScene("Assets/Scenes/Menu.unity", true),
        new EditorBuildSettingsScene("Assets/Scenes/Jogo.unity", true),
        new EditorBuildSettingsScene("Assets/Scenes/Main.unity", true),
    };

    public static Sprite CarregarSprite(string caminho) => AssetDatabase.LoadAssetAtPath<Sprite>(caminho);

    public static void ConfigurarArte()
    {
        AssetDatabase.Refresh();
        foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Art/F1", "Assets/Art/Ceu" }))
        {
            var caminho = AssetDatabase.GUIDToAssetPath(guid);
            var imp = (TextureImporter)AssetImporter.GetAtPath(caminho);
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.filterMode = FilterMode.Bilinear;
            imp.textureCompression = TextureImporterCompression.Uncompressed;
            imp.mipmapEnabled = true;
            imp.alphaIsTransparency = true;
            imp.spritePixelsPerUnit = caminho.Contains("/f1_") ? Ppu
                                    : caminho.EndsWith("estrelas.png") ? 20
                                    : caminho.EndsWith("terra.png") ? 128 : 200;
            imp.SaveAndReimport();
        }
    }

    public static PhysicsMaterial2D Material2D(string nome, float atrito)
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

    public static void LuzGlobal()
    {
        var luz = new GameObject("Luz Global").AddComponent<Light2D>();
        var so = new SerializedObject(luz);
        so.FindProperty("m_LightType").intValue = (int)Light2D.LightType.Global;
        so.ApplyModifiedPropertiesWithoutUndo();
        luz.intensity = 1f;
    }

    // ---------- F1 ----------

    public class PecasF1
    {
        public GameObject raiz, chassi;
        public F1 f1;
        public SpriteRenderer pintura;
    }

    public static PecasF1 CriarF1(PhysicsMaterial2D pneu)
    {
        var raiz = new GameObject("Carro");
        var chassi = new GameObject("Chassi") { tag = "Player" };
        chassi.transform.SetParent(raiz.transform, false);
        chassi.transform.position = Largada;
        var rb = chassi.AddComponent<Rigidbody2D>();
        rb.mass = 1.5f;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        // O collider nasce da silhueta completa (f1_corpo.png); depois o desenho passa para as camadas.
        var silhueta = chassi.AddComponent<SpriteRenderer>();
        silhueta.sprite = CarregarSprite("Assets/Art/F1/f1_corpo.png");
        chassi.AddComponent<PolygonCollider2D>();
        Object.DestroyImmediate(silhueta);
        Camada(chassi.transform, "Atras", "f1_atras", 2);
        var pintura = Camada(chassi.transform, "Pintura", "f1_pintura", 3);
        Camada(chassi.transform, "Frente", "f1_frente", 4);

        var f1 = chassi.AddComponent<F1>();
        chassi.AddComponent<PinturaF1>().pintura = pintura;
        var rodaT = NovaRoda("Roda Traseira", raiz.transform, Largada + RodaT, pneu);
        var rodaD = NovaRoda("Roda Dianteira", raiz.transform, Largada + RodaD, pneu);
        f1.eixoTraseiro = Eixo(chassi, rodaT, RodaT);
        f1.eixoDianteiro = Eixo(chassi, rodaD, RodaD);

        var capacete = new GameObject("Capacete") { tag = "Player" };
        capacete.transform.SetParent(chassi.transform, false);
        capacete.transform.localPosition = Capacete;
        var cc = capacete.AddComponent<CircleCollider2D>();
        cc.isTrigger = true;
        cc.radius = 0.3f;   // cobre capacete + halo: de teto no chão, passa da asa traseira (0,47) e encosta
        capacete.AddComponent<SensorCapacete>().f1 = f1;

        return new PecasF1 { raiz = raiz, chassi = chassi, f1 = f1, pintura = pintura };
    }

    static SpriteRenderer Camada(Transform pai, string nome, string arquivo, int ordem)
    {
        var go = new GameObject(nome);
        go.transform.SetParent(pai, false);
        var r = go.AddComponent<SpriteRenderer>();
        r.sprite = CarregarSprite($"Assets/Art/F1/{arquivo}.png");
        r.sortingOrder = ordem;
        return r;
    }

    static GameObject NovaRoda(string nome, Transform pai, Vector2 pos, PhysicsMaterial2D pneu)
    {
        var go = new GameObject(nome) { tag = "Player" };
        go.transform.SetParent(pai, false);
        go.transform.position = pos;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = CarregarSprite("Assets/Art/F1/f1_roda.png");
        sr.sortingOrder = 5;
        var rb = go.AddComponent<Rigidbody2D>();
        rb.mass = 0.25f;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
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

    // ---------- Câmera, terreno, céu ----------

    public static Camera CriarCamera(Rigidbody2D alvo, float zoomMin, float zoomMax)
    {
        var cam = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener)).GetComponent<Camera>();
        cam.tag = "MainCamera";
        cam.orthographic = true;
        cam.orthographicSize = zoomMin;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.transform.position = new Vector3(Largada.x + 3, Largada.y + 1.5f, -10);
        var segue = cam.gameObject.AddComponent<CameraSegue>();
        segue.alvo = alvo;
        segue.zoomMin = zoomMin;
        segue.zoomMax = zoomMax;
        return cam;
    }

    public static GameObject CeuLunar(Camera cam)
    {
        var ceu = new GameObject("Ceu Lunar");
        ceu.transform.SetParent(cam.transform, false);
        ceu.transform.localPosition = new Vector3(0, 0, 20);
        Fundo(ceu.transform, "Estrelas", "Assets/Art/Ceu/estrelas.png", Vector3.zero, -50);
        Fundo(ceu.transform, "Terra", "Assets/Art/Ceu/terra.png", new Vector3(9, 5, 0), -49);
        return ceu;
    }

    static void Fundo(Transform pai, string nome, string sprite, Vector3 pos, int ordem)
    {
        var go = new GameObject(nome);
        go.transform.SetParent(pai, false);
        go.transform.localPosition = pos;
        var r = go.AddComponent<SpriteRenderer>();
        r.sprite = CarregarSprite(sprite);
        r.sharedMaterial = MaterialUnlit;     // céu não recebe a luz do jogo
        r.sortingOrder = ordem;
    }

    public static Terreno CriarTerreno(Transform alvo, PhysicsMaterial2D chao)
    {
        var terreno = new GameObject("Terreno").AddComponent<Terreno>();
        terreno.alvo = alvo;
        terreno.materialChao = chao;
        return terreno;
    }

    // ---------- UI (Screen Space - Camera, para aparecer nas capturas) ----------

    public static Canvas CriarCanvas(Camera cam)
    {
        var go = new GameObject("UI", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = cam;
        canvas.planeDistance = 1;
        canvas.sortingOrder = 100;          // por cima de carro, moedas e linhas do modo aula
        var scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        return canvas;
    }

    public static void CriarEventSystem() =>
        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

    public static RectTransform Caixa(string nome, Transform pai, Vector2 ancora, Vector2 pos, Vector2 tamanho, Color cor)
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

    public static TMP_Text Texto(string nome, Transform pai, Vector2 ancoraMin, Vector2 ancoraMax, float tamanho,
                                 TextAlignmentOptions alinhamento)
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

    // Tela cheia escurecida (painéis do menu, pausa, fim).
    public static RectTransform Painel(string nome, Transform pai, float escurecer = 0.6f)
    {
        var rt = Caixa(nome, pai, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, new Color(0, 0, 0, escurecer));
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        return rt;
    }

    // Coluna de botões empilhados, centrada em 'ancora'.
    public static RectTransform Coluna(Transform pai, Vector2 ancora, float largura)
    {
        var go = new GameObject("Botoes", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        var rt = (RectTransform)go.transform;
        rt.SetParent(pai, false);
        rt.anchorMin = rt.anchorMax = ancora;
        rt.pivot = new Vector2(0.5f, 0.5f);          // centrada na âncora (a altura cresce para os dois lados)
        rt.sizeDelta = new Vector2(largura, 0);
        var v = go.GetComponent<VerticalLayoutGroup>();
        v.spacing = 14;
        v.childControlWidth = v.childControlHeight = true;
        v.childForceExpandWidth = true;
        v.childForceExpandHeight = false;
        go.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        return rt;
    }

    public static (Button, TMP_Text) Botao(Transform pai, string texto, UnityAction acao)
    {
        var (b, t) = NovoBotao(pai, texto);
        UnityEventTools.AddPersistentListener(b.onClick, acao);
        return (b, t);
    }

    public static (Button, TMP_Text) BotaoInt(Transform pai, string texto, UnityAction<int> acao, int argumento)
    {
        var (b, t) = NovoBotao(pai, texto);
        UnityEventTools.AddIntPersistentListener(b.onClick, acao, argumento);
        return (b, t);
    }

    static (Button, TMP_Text) NovoBotao(Transform pai, string texto)
    {
        var go = new GameObject(texto, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        go.transform.SetParent(pai, false);
        go.GetComponent<LayoutElement>().preferredHeight = 70;
        var botao = go.GetComponent<Button>();
        var cores = botao.colors;
        cores.normalColor = new Color(0.08f, 0.1f, 0.14f, 0.88f);
        cores.highlightedColor = cores.selectedColor = new Color(1f, 0.55f, 0.1f, 1f);   // teclado: selecionado em laranja
        cores.pressedColor = new Color(0.8f, 0.4f, 0.05f, 1f);
        botao.colors = cores;
        var t = Texto("Texto", go.transform, Vector2.zero, Vector2.one, 36, TextAlignmentOptions.Center);
        t.text = texto;
        return (botao, t);
    }
}
