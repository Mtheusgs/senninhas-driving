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

    public static void DefinirBuild()
    {
        AssetDatabase.Refresh();    // cena recém-salva precisa estar importada, senão entra no build com GUID zerado
        EditorBuildSettings.scenes = new EditorBuildSettingsScene[0];   // força regravar o arquivo (mesma lista não é salva)
        EditorBuildSettings.scenes = Cenas;
        AssetDatabase.SaveAssets();
    }

    static EditorBuildSettingsScene[] Cenas => new[]
    {
        new EditorBuildSettingsScene("Assets/Scenes/Menu.unity", true),
        new EditorBuildSettingsScene("Assets/Scenes/Jogo.unity", true),
        new EditorBuildSettingsScene("Assets/Scenes/Main.unity", true),
    };

    public static Sprite CarregarSprite(string caminho) => AssetDatabase.LoadAssetAtPath<Sprite>(caminho);

    public static void ConfigurarArte()
    {
        AssetDatabase.Refresh();
        foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Art/F1", "Assets/Art/Ceu", "Assets/Art/Fundo" }))
        {
            var caminho = AssetDatabase.GUIDToAssetPath(guid);
            var imp = (TextureImporter)AssetImporter.GetAtPath(caminho);
            if (caminho.EndsWith("_normal.png"))           // normal map: dado linear, não é sprite
            {
                imp.textureType = TextureImporterType.Default;
                imp.sRGBTexture = false;
                imp.mipmapEnabled = false;
                imp.wrapMode = TextureWrapMode.Clamp;
                imp.textureCompression = TextureImporterCompression.Uncompressed;
                imp.SaveAndReimport();
                continue;
            }
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.filterMode = FilterMode.Bilinear;
            imp.textureCompression = TextureImporterCompression.Uncompressed;
            imp.mipmapEnabled = true;
            imp.alphaIsTransparency = true;
            imp.spritePixelsPerUnit = caminho.Contains("/f1_") ? Ppu
                                    : caminho.Contains("/Fundo/") ? 32
                                    : caminho.EndsWith("estrelas.png") ? 20
                                    : caminho.EndsWith("terra.png") ? 128
                                    : caminho.EndsWith("sol.png") || caminho.EndsWith("lua.png") ? 40
                                    : caminho.EndsWith("ceu_gradiente.png") ? 16
                                    : caminho.EndsWith("feixe.png") ? 64 : 200;
            if (caminho.Contains("/Fundo/")) imp.wrapMode = TextureWrapMode.Repeat;   // camadas repetidas (tiled)
            if (caminho.Contains("/f1_") || caminho.Contains("/Fundo/"))   // UV 0-1 (normal map) / malha retangular (tiled)
            {
                var s = new TextureImporterSettings();
                imp.ReadTextureSettings(s);
                s.spriteMeshType = SpriteMeshType.FullRect;
                imp.SetTextureSettings(s);
            }
            imp.SaveAndReimport();
        }
    }

    // Material salvo em Assets/Materiais (recriado com os valores atuais a cada montagem).
    public static Material Material(string nome, string shader, System.Action<Material> configurar = null)
    {
        Directory.CreateDirectory("Assets/Materiais");
        string caminho = $"Assets/Materiais/{nome}.mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(caminho);
        if (m == null)
        {
            m = new Material(Shader.Find(shader));
            AssetDatabase.CreateAsset(m, caminho);
        }
        m.shader = Shader.Find(shader);
        configurar?.Invoke(m);
        EditorUtility.SetDirty(m);
        AssetDatabase.SaveAssets();
        return m;
    }

    static Material Phong(string nome, string normal, float especular, float brilho) => Material(nome, "CarroCG/F1Phong", m =>
    {
        m.SetTexture("_NormalMap", AssetDatabase.LoadAssetAtPath<Texture2D>($"Assets/Art/F1/{normal}.png"));
        m.SetFloat("_Especular", especular);
        m.SetFloat("_Brilho", brilho);
    });

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

    public static Light2D LuzGlobal() => Luz("Luz Global", null, Light2D.LightType.Global, Color.white, 1f, 0);

    static Light2D Luz(string nome, Transform pai, Light2D.LightType tipo, Color cor, float intensidade, float raio)
    {
        var luz = new GameObject(nome).AddComponent<Light2D>();
        if (pai) luz.transform.SetParent(pai, false);
        var so = new SerializedObject(luz);
        so.FindProperty("m_LightType").intValue = (int)tipo;
        so.ApplyModifiedPropertiesWithoutUndo();
        luz.color = cor;
        luz.intensity = intensidade;
        if (raio > 0) luz.pointLightOuterRadius = raio;
        return luz;
    }

    // ---------- F1 ----------

    public class PecasF1
    {
        public GameObject raiz, chassi;
        public F1 f1;
        public SpriteRenderer pintura, feixe;
        public Light2D farol, lanterna;
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
        var atras = Camada(chassi.transform, "Atras", "f1_atras", 2, Phong("F1Atras", "f1_normal", 0.2f, 32));
        var pintura = Camada(chassi.transform, "Pintura", "f1_pintura", 3, Phong("F1Pintura", "f1_normal", 0.7f, 32));
        var frente = Camada(chassi.transform, "Frente", "f1_frente", 4, Phong("F1Detalhes", "f1_normal", 0.35f, 32));

        var f1 = chassi.AddComponent<F1>();
        chassi.AddComponent<PinturaF1>().pintura = pintura;
        var rodaT = NovaRoda("Roda Traseira", raiz.transform, Largada + RodaT, pneu);
        var rodaD = NovaRoda("Roda Dianteira", raiz.transform, Largada + RodaD, pneu);
        f1.eixoTraseiro = Eixo(chassi, rodaT, RodaT);
        f1.eixoDianteiro = Eixo(chassi, rodaD, RodaD);
        chassi.AddComponent<PhongF1>().pecas = new[]
            { atras, pintura, frente, rodaT.GetComponent<SpriteRenderer>(), rodaD.GetComponent<SpriteRenderer>() };

        // à noite: farol, luz traseira vermelha e o feixe do farol (o CicloDia acende)
        var farol = Luz("Farol", chassi.transform, Light2D.LightType.Point, Color.white, 0, 7f);
        farol.transform.localPosition = Local(950, 195);
        var lanterna = Luz("Lanterna", chassi.transform, Light2D.LightType.Point, new Color(1f, 0.15f, 0.1f), 0, 2.5f);
        lanterna.transform.localPosition = Local(70, 150);
        var feixe = new GameObject("Feixe").AddComponent<SpriteRenderer>();
        feixe.transform.SetParent(chassi.transform, false);
        feixe.transform.localPosition = Local(950, 195) + new Vector2(2f, 0);
        feixe.sprite = CarregarSprite("Assets/Art/F1/feixe.png");
        feixe.sharedMaterial = MaterialUnlit;
        feixe.sortingOrder = 6;
        feixe.color = new Color(1, 1, 1, 0);

        var capacete = new GameObject("Capacete") { tag = "Player" };
        capacete.transform.SetParent(chassi.transform, false);
        capacete.transform.localPosition = Capacete;
        var cc = capacete.AddComponent<CircleCollider2D>();
        cc.isTrigger = true;
        cc.radius = 0.3f;   // cobre capacete + halo: de teto no chão, passa da asa traseira (0,47) e encosta
        capacete.AddComponent<SensorCapacete>().f1 = f1;

        return new PecasF1 { raiz = raiz, chassi = chassi, f1 = f1, pintura = pintura, feixe = feixe, farol = farol, lanterna = lanterna };
    }

    static SpriteRenderer Camada(Transform pai, string nome, string arquivo, int ordem, Material material)
    {
        var go = new GameObject(nome);
        go.transform.SetParent(pai, false);
        var r = go.AddComponent<SpriteRenderer>();
        r.sprite = CarregarSprite($"Assets/Art/F1/{arquivo}.png");
        r.sortingOrder = ordem;
        r.sharedMaterial = material;
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
        sr.sharedMaterial = Phong("F1Roda", "f1_roda_normal", 0.1f, 8);
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

    static SpriteRenderer Fundo(Transform pai, string nome, string sprite, Vector3 pos, int ordem)
    {
        var go = new GameObject(nome);
        go.transform.SetParent(pai, false);
        go.transform.localPosition = pos;
        var r = go.AddComponent<SpriteRenderer>();
        r.sprite = CarregarSprite(sprite);
        r.sharedMaterial = MaterialUnlit;     // céu não recebe a luz do jogo
        r.sortingOrder = ordem;
        return r;
    }

    // Ciclo dia/noite: gradiente do céu, sol, lua e estrelas presos à câmera.
    public static CicloDia CriarCiclo(Camera cam, Terreno terreno, Light2D luzGlobal, PecasF1 f1)
    {
        var ciclo = new GameObject("Ciclo Dia").AddComponent<CicloDia>();
        ciclo.terreno = terreno;
        ciclo.cam = cam;
        ciclo.luzGlobal = luzGlobal;
        ciclo.farol = f1.farol;
        ciclo.lanterna = f1.lanterna;
        ciclo.feixe = f1.feixe;
        var ceu = new GameObject("Ceu").transform;
        ceu.SetParent(cam.transform, false);
        ciclo.ceuGradiente = Fundo(ceu, "Gradiente", "Assets/Art/Ceu/ceu_gradiente.png", new Vector3(0, 4, 25), -55);
        ciclo.ceuGradiente.transform.localScale = new Vector3(240, 1.6f, 1);
        ciclo.estrelas = Fundo(ceu, "Estrelas", "Assets/Art/Ceu/estrelas.png", new Vector3(0, 0, 24), -54);
        ciclo.estrelas.color = new Color(1, 1, 1, 0);
        ciclo.sol = Fundo(ceu, "Sol", "Assets/Art/Ceu/sol.png", new Vector3(0, 6, 15), -53);
        ciclo.lua = Fundo(ceu, "Lua", "Assets/Art/Ceu/lua.png", new Vector3(0, -6, 15), -53);
        return ciclo;
    }

    // Fundo em 3 camadas com profundidade (perto → longe): pinheiros z=5, morros z=20, montanhas z=60.
    public static Parallax CriarParallax(Camera cam, CicloDia ciclo)
    {
        var p = new GameObject("Parallax").AddComponent<Parallax>();
        p.cam = cam;
        p.ciclo = ciclo;
        p.camadas = new[]
        {
            Camada(p.transform, "pinheiros", 5, -1, new Color(0.35f, 0.55f, 0.35f), -20, false),
            Camada(p.transform, "morros", 20, 0, new Color(0.45f, 0.62f, 0.45f), -30, true),
            Camada(p.transform, "montanhas", 60, 2, new Color(0.55f, 0.6f, 0.72f), -40, true),
        };
        return p;
    }

    static Parallax.Camada Camada(Transform pai, string arte, float z, float yBase, Color cor, int ordem, bool naLua)
    {
        var baixo = new GameObject("Preenchimento " + arte).AddComponent<SpriteRenderer>();
        baixo.transform.SetParent(pai, false);
        baixo.sprite = CarregarSprite("Assets/Art/Gerado/quadrado.png");   // 1×1 unidade
        baixo.sharedMaterial = MaterialUnlit;
        baixo.sortingOrder = ordem;
        var r = new GameObject("Camada " + arte).AddComponent<SpriteRenderer>();
        r.transform.SetParent(pai, false);
        r.sprite = CarregarSprite($"Assets/Art/Fundo/{arte}.png");
        r.drawMode = SpriteDrawMode.Tiled;
        r.sharedMaterial = MaterialUnlit;
        r.sortingOrder = ordem;
        return new Parallax.Camada { render = r, z = z, yBase = yBase, cor = cor, preenchimento = baixo, naLua = naLua };
    }

    // Minimapa no canto superior direito do HUD.
    public static Minimapa CriarMinimapa(Transform hud, Terreno terreno, F1 f1)
    {
        var caixa = Caixa("Minimapa", hud, new Vector2(1, 1), new Vector2(-40, -40), new Vector2(256 * 1.6f + 8, 96 * 1.6f + 8),
                          new Color(0, 0, 0, 0.55f));
        var go = new GameObject("Imagem", typeof(RectTransform), typeof(RawImage));
        var rt = (RectTransform)go.transform;
        rt.SetParent(caixa, false);
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(4, 4);
        rt.offsetMax = new Vector2(-4, -4);
        go.GetComponent<RawImage>().raycastTarget = false;
        var m = caixa.gameObject.AddComponent<Minimapa>();
        m.terreno = terreno;
        m.f1 = f1;
        m.imagem = go.GetComponent<RawImage>();
        return m;
    }

    public static SombraF1 CriarSombra(PecasF1 f1, Terreno terreno, CicloDia ciclo)
    {
        var r = new GameObject("Sombra F1").AddComponent<SpriteRenderer>();
        r.sharedMaterial = MaterialUnlit;
        r.sortingOrder = 1;
        var s = r.gameObject.AddComponent<SombraF1>();
        s.f1 = f1.f1;
        s.terreno = terreno;
        s.ciclo = ciclo;
        s.render = r;
        return s;
    }

    public static Terreno CriarTerreno(Transform alvo, PhysicsMaterial2D chao)
    {
        var terreno = new GameObject("Terreno").AddComponent<Terreno>();
        terreno.alvo = alvo;
        terreno.materialChao = chao;
        terreno.materialLuz = Material("TerrenoLuz", "CarroCG/TerrenoLuz");
        terreno.quadrado = CarregarSprite("Assets/Art/Gerado/quadrado.png");
        return terreno;
    }

    // Reflexo do F1 nos lagos (todas as peças desenhadas: camadas do chassi e rodas).
    public static Reflexo CriarReflexo(PecasF1 f1, Terreno terreno)
    {
        var r = new GameObject("Reflexo F1").AddComponent<Reflexo>();
        r.terreno = terreno;
        r.pecas = f1.chassi.GetComponent<PhongF1>().pecas;
        r.material = AssetDatabase.LoadAssetAtPath<Material>(
            "Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Lit-Default.mat");
        return r;
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
