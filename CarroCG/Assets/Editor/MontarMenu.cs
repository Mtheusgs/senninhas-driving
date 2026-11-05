using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// Monta Assets/Scenes/Menu.unity. Menu: CarroCG → Montar Menu. Sobrescreve a cena.
public static class MontarMenu
{
    const string Cena = "Assets/Scenes/Menu.unity";
    static readonly Color Dourado = new(1f, 0.8f, 0.2f);

    [MenuItem("CarroCG/Montar Menu")]
    public static void Executar()
    {
        MontagemComum.ConfigurarArte();
        var pneu = MontagemComum.Material2D("Pneu", 1.6f);
        var chao = MontagemComum.Material2D("Chao", 1.0f);
        var cena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // Fundo animado: o jogo de verdade, na Terra, com o piloto automático
        MontagemComum.LuzGlobal();
        var f1 = MontagemComum.CriarF1(pneu);
        var cam = MontagemComum.CriarCamera(f1.chassi.GetComponent<Rigidbody2D>(), 9, 12);
        cam.GetComponent<CameraSegue>().deslocamento = new Vector2(-8, 4);   // carro à direita, fora da coluna de botões
        var terreno = MontagemComum.CriarTerreno(f1.chassi.transform, chao);
        var config = new GameObject("Configurar Partida").AddComponent<ConfigurarPartida>();
        config.terreno = terreno;
        config.f1 = f1.f1;
        config.cam = cam;
        config.forcarTerra = true;
        var piloto = f1.chassi.AddComponent<PilotoAutomatico>();
        piloto.f1 = f1.f1;
        piloto.terreno = terreno;

        var canvas = MontagemComum.CriarCanvas(cam);
        var menu = canvas.gameObject.AddComponent<Menu>();
        var raiz = canvas.transform;
        menu.principal = TelaPrincipal(raiz, menu);
        menu.escolhaModo = TelaEscolhaModo(raiz, menu);
        menu.garagem = TelaGaragem(raiz, menu);
        menu.controles = TelaControles(raiz, menu);
        menu.configuracoes = TelaConfiguracoes(raiz, menu);
        menu.tecnicas = TelaTecnicas(raiz, menu);
        menu.recorde = TelaRecorde(raiz, menu);
        MontagemComum.CriarEventSystem();

        Directory.CreateDirectory("Assets/Scenes");
        EditorSceneManager.SaveScene(cena, Cena);
        MontagemComum.DefinirBuild();
        Debug.Log("MontarMenu: cena salva em " + Cena);
    }

    static TMP_Text Titulo(Transform pai, string texto, float tamanho = 72)
    {
        var t = MontagemComum.Texto("Titulo", pai, new Vector2(0.1f, 0.8f), new Vector2(0.9f, 0.95f), tamanho, TextAlignmentOptions.Center);
        t.text = texto;
        return t;
    }

    static void Voltar(Transform pai, Menu menu, float y = 0.1f) =>
        MontagemComum.Botao(MontagemComum.Coluna(pai, new Vector2(0.5f, y), 360), "Voltar", menu.AbrirPrincipal);

    static GameObject TelaPrincipal(Transform raiz, Menu menu)
    {
        var tela = MontagemComum.Painel("Principal", raiz, 0.25f);
        var nome = MontagemComum.Texto("Nome", tela, new Vector2(0.05f, 0.72f), new Vector2(0.95f, 0.92f), 120, TextAlignmentOptions.Center);
        nome.text = "Senninhas Driving";
        nome.color = Dourado;
        MontagemComum.Texto("Subtitulo", tela, new Vector2(0.1f, 0.66f), new Vector2(0.9f, 0.73f), 30, TextAlignmentOptions.Center)
                     .text = "Trabalho de Computação Gráfica";
        var col = MontagemComum.Coluna(tela, new Vector2(0.5f, 0.34f), 520);
        MontagemComum.Botao(col, "Jogar", menu.AbrirEscolhaModo);
        MontagemComum.Botao(col, "Garagem", menu.AbrirGaragem);
        MontagemComum.Botao(col, "Controles", menu.AbrirControles);
        MontagemComum.Botao(col, "Configurações", menu.AbrirConfiguracoes);
        MontagemComum.Botao(col, "Técnicas de CG", menu.AbrirTecnicas);
        MontagemComum.Botao(col, "Recorde", menu.AbrirRecorde);
        MontagemComum.Botao(col, "Sair", menu.Sair);
        return tela.gameObject;
    }

    static GameObject TelaEscolhaModo(Transform raiz, Menu menu)
    {
        var tela = MontagemComum.Painel("Escolha do modo", raiz);
        Titulo(tela, "Escolha o modo");
        var col = MontagemComum.Coluna(tela, new Vector2(0.5f, 0.5f), 520);
        MontagemComum.BotaoInt(col, "Terra", menu.Jogar, (int)Modo.Terra);
        MontagemComum.BotaoInt(col, "Lua", menu.Jogar, (int)Modo.Lua);
        MontagemComum.Botao(col, "Voltar", menu.AbrirPrincipal);
        MontagemComum.Texto("Dica", tela, new Vector2(0.15f, 0.18f), new Vector2(0.85f, 0.3f), 28, TextAlignmentOptions.Center)
                     .text = "Lua: gravidade baixa, o carro voa longe nos morros";
        return tela.gameObject;
    }

    static GameObject TelaGaragem(Transform raiz, Menu menu)
    {
        var tela = MontagemComum.Painel("Garagem", raiz);
        Titulo(tela, "Garagem");

        // Vitrine: as camadas do F1 como imagens da UI, alinhadas como no sprite 960x300
        var previa = new GameObject("Previa", typeof(RectTransform)).GetComponent<RectTransform>();
        previa.SetParent(tela, false);
        previa.anchorMin = previa.anchorMax = new Vector2(0.5f, 0.56f);
        previa.sizeDelta = new Vector2(960, 300);
        Imagem(previa, "Atras", "f1_atras", Vector2.zero, new Vector2(960, 300));
        menu.previaPintura = Imagem(previa, "Pintura", "f1_pintura", Vector2.zero, new Vector2(960, 300));
        Imagem(previa, "Frente", "f1_frente", Vector2.zero, new Vector2(960, 300));
        Imagem(previa, "Roda Traseira", "f1_roda", new Vector2(190 - 480, 150 - 178), new Vector2(184, 184));
        Imagem(previa, "Roda Dianteira", "f1_roda", new Vector2(770 - 480, 150 - 178), new Vector2(184, 184));
        menu.previaCarro = previa;

        var linha = new GameObject("Cores", typeof(RectTransform), typeof(HorizontalLayoutGroup)).GetComponent<RectTransform>();
        linha.SetParent(tela, false);
        linha.anchorMin = linha.anchorMax = new Vector2(0.5f, 0.3f);
        linha.sizeDelta = new Vector2(8 * 100, 90);
        var h = linha.GetComponent<HorizontalLayoutGroup>();
        h.spacing = 10;
        h.childControlWidth = h.childControlHeight = true;
        h.childForceExpandWidth = h.childForceExpandHeight = true;
        for (int i = 0; i < Garagem.Cores.Length; i++)
        {
            var (b, t) = MontagemComum.BotaoInt(linha, Garagem.Nomes[i], menu.EscolherCor, i);
            t.text = "";                                   // a amostra de cor é o próprio botão
            var cores = b.colors;
            cores.normalColor = Garagem.Cores[i];
            cores.highlightedColor = cores.selectedColor = Color.Lerp(Garagem.Cores[i], Color.black, 0.35f);   // clarear faria o vermelho parecer rosa
            cores.pressedColor = Color.Lerp(Garagem.Cores[i], Color.black, 0.55f);
            b.colors = cores;
        }
        menu.textoCor = MontagemComum.Texto("Cor", tela, new Vector2(0.3f, 0.18f), new Vector2(0.7f, 0.25f), 36, TextAlignmentOptions.Center);
        Voltar(tela, menu);
        return tela.gameObject;
    }

    static Image Imagem(RectTransform pai, string nome, string arquivo, Vector2 pos, Vector2 tamanho)
    {
        var go = new GameObject(nome, typeof(RectTransform), typeof(Image));
        var rt = (RectTransform)go.transform;
        rt.SetParent(pai, false);
        rt.anchoredPosition = pos;
        rt.sizeDelta = tamanho;
        var img = go.GetComponent<Image>();
        img.sprite = MontagemComum.CarregarSprite($"Assets/Art/F1/{arquivo}.png");
        img.raycastTarget = false;
        return img;
    }

    static GameObject TelaControles(Transform raiz, Menu menu)
    {
        var tela = MontagemComum.Painel("Controles", raiz);
        Titulo(tela, "Controles");
        var caixa = MontagemComum.Caixa("Caixa", tela, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1000, 560), new Color(0, 0, 0, 0.5f));
        MontagemComum.Texto("Texto", caixa, new Vector2(0.06f, 0.05f), new Vector2(0.94f, 0.95f), 38, TextAlignmentOptions.Left).text =
            "Seta direita   acelera\nSeta esquerda   freia / ré\nNo ar: as setas giram o carro\nR   reinicia\nEsc   pausa\nTab   modo aula\nF   scanline em câmera lenta (modo aula)";
        Voltar(tela, menu);
        return tela.gameObject;
    }

    static GameObject TelaConfiguracoes(Transform raiz, Menu menu)
    {
        var tela = MontagemComum.Painel("Configuracoes", raiz);
        Titulo(tela, "Configurações");
        var col = MontagemComum.Coluna(tela, new Vector2(0.5f, 0.5f), 640);
        menu.textoQualidade = MontagemComum.Botao(col, "Qualidade", menu.ProximaQualidade).Item2;
        menu.textoTelaCheia = MontagemComum.Botao(col, "Tela cheia", menu.AlternarTelaCheia).Item2;
        menu.textoModoAula = MontagemComum.Botao(col, "Modo aula ao iniciar", menu.AlternarModoAula).Item2;
        MontagemComum.Botao(col, "Voltar", menu.AbrirPrincipal);
        MontagemComum.Texto("Dica", tela, new Vector2(0.15f, 0.12f), new Vector2(0.85f, 0.25f), 26, TextAlignmentOptions.Center)
                     .text = "A qualidade muda a resolução do terreno e vale a partir da próxima partida.";
        return tela.gameObject;
    }

    static GameObject TelaTecnicas(Transform raiz, Menu menu)
    {
        var tela = MontagemComum.Painel("Tecnicas", raiz, 0.75f);
        Titulo(tela, "Técnicas de CG", 64);
        var t = MontagemComum.Texto("Texto", tela, new Vector2(0.1f, 0.17f), new Vector2(0.9f, 0.8f), 26, TextAlignmentOptions.TopLeft);
        t.fontStyle = FontStyles.Normal;
        t.enableAutoSizing = true;
        t.fontSizeMin = 14;
        t.fontSizeMax = 26;
        menu.textoTecnicas = t;
        Voltar(tela, menu, 0.08f);
        return tela.gameObject;
    }

    static GameObject TelaRecorde(Transform raiz, Menu menu)
    {
        var tela = MontagemComum.Painel("Recorde", raiz);
        Titulo(tela, "Recorde");
        menu.textoRecorde = MontagemComum.Texto("Texto", tela, new Vector2(0.2f, 0.42f), new Vector2(0.8f, 0.78f), 48, TextAlignmentOptions.Center);
        var col = MontagemComum.Coluna(tela, new Vector2(0.5f, 0.28f), 560);
        menu.textoZerar = MontagemComum.Botao(col, "Zerar recordes", menu.ZerarRecordes).Item2;
        MontagemComum.Botao(col, "Voltar", menu.AbrirPrincipal);
        return tela.gameObject;
    }
}
