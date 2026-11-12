using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// Monta Assets/Scenes/Jogo.unity. Menu: CarroCG → Montar Jogo. Sobrescreve a cena.
public static class MontarJogo
{
    const string Cena = "Assets/Scenes/Jogo.unity";

    [MenuItem("CarroCG/Montar Jogo")]
    public static void Executar()
    {
        MontagemComum.ConfigurarArte();
        var pneu = MontagemComum.Material2D("Pneu", 1.6f);
        var chao = MontagemComum.Material2D("Chao", 1.0f);
        var cena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var luz = MontagemComum.LuzGlobal();
        var f1 = MontagemComum.CriarF1(pneu);
        var cam = MontagemComum.CriarCamera(f1.chassi.GetComponent<Rigidbody2D>(), 7, 11);
        var ceu = MontagemComum.CeuLunar(cam);
        var terreno = MontagemComum.CriarTerreno(f1.chassi.transform, chao);

        var coletaveis = new GameObject("Coletaveis").AddComponent<Coletaveis>();
        coletaveis.terreno = terreno;
        coletaveis.moeda = MontagemComum.CarregarSprite("Assets/Art/F1/moeda.png");
        coletaveis.galao = MontagemComum.CarregarSprite("Assets/Art/F1/galao.png");

        var config = new GameObject("Configurar Partida").AddComponent<ConfigurarPartida>();
        config.terreno = terreno;
        config.f1 = f1.f1;
        config.cam = cam;
        config.ceuLunar = ceu;
        var ciclo = MontagemComum.CriarCiclo(cam, terreno, luz, f1);
        config.ciclo = ciclo;
        var sombra = MontagemComum.CriarSombra(f1, terreno, ciclo);
        MontagemComum.CriarReflexo(f1, terreno);
        MontagemComum.CriarEfeitos(f1, cam, ciclo, terreno);
        var parallax = MontagemComum.CriarParallax(cam, ciclo);

        var partida = new GameObject("Partida").AddComponent<Partida>();
        partida.f1 = f1.f1;
        partida.terreno = terreno;
        partida.parallax = parallax;
        var canvas = MontagemComum.CriarCanvas(cam);
        MontarHud(partida, canvas.transform);

        var aula = new GameObject("Modo Aula").AddComponent<ModoAula>();
        aula.terreno = terreno;
        aula.f1 = f1.f1;
        aula.partida = partida;
        aula.ciclo = ciclo;
        aula.parallax = parallax;
        aula.minimapa = MontagemComum.CriarMinimapa(canvas.transform, terreno, f1.f1);
        aula.materialLinha = MontagemComum.MaterialUnlit;
        var caixaAula = MontagemComum.Caixa("Rotulo Aula", canvas.transform, new Vector2(0, 0), new Vector2(40, 40),   // canto de baixo: não cobre moedas nem HUD
                                            new Vector2(720, 250), new Color(0, 0, 0, 0.55f));   // não cobre a caixa de distância
        aula.rotulo = MontagemComum.Texto("Texto", caixaAula, new Vector2(0.03f, 0.05f), new Vector2(0.97f, 0.95f),
                                          30, TextAlignmentOptions.TopLeft);
        aula.rotulo.enableAutoSizing = true;
        aula.rotulo.fontSizeMin = 16;
        aula.rotulo.fontSizeMax = 30;
        var intro = CriarIntro(partida, canvas, f1, cam);
        intro.aula = aula;
        intro.sombra = sombra.gameObject;
        var som = new GameObject("Som").AddComponent<Som>();
        som.f1 = f1.f1;
        som.partida = partida;
        som.cam = cam.GetComponent<CameraSegue>();
        MontagemComum.CriarEventSystem();

        Directory.CreateDirectory("Assets/Scenes");
        EditorSceneManager.SaveScene(cena, Cena);
        MontagemComum.DefinirBuild();
        Debug.Log("MontarJogo: cena salva em " + Cena);
    }

    // Intro: tela presa ao chassi (por cima do F1) e legenda num canvas próprio (o HUD some durante a intro).
    static IntroF1 CriarIntro(Partida partida, Canvas canvas, MontagemComum.PecasF1 f1, Camera cam)
    {
        var intro = new GameObject("Intro").AddComponent<IntroF1>();
        intro.partida = partida;
        intro.hud = canvas.gameObject.AddComponent<CanvasGroup>();
        intro.camadas = f1.chassi.GetComponent<PhongF1>().pecas;
        intro.segue = cam.GetComponent<CameraSegue>();
        var tela = new GameObject("Tela Intro").AddComponent<SpriteRenderer>();
        tela.transform.SetParent(f1.chassi.transform, false);
        tela.sharedMaterial = MontagemComum.MaterialUnlit;
        tela.sortingOrder = 8;
        intro.tela = tela;
        var canvasIntro = MontagemComum.CriarCanvas(cam);
        canvasIntro.name = "UI Intro";
        canvasIntro.sortingOrder = 110;
        var caixa = MontagemComum.Caixa("Legenda", canvasIntro.transform, new Vector2(0.5f, 0), new Vector2(0, 70),
                                        new Vector2(1100, 80), new Color(0, 0, 0, 0.6f));
        intro.legenda = MontagemComum.Texto("Texto", caixa, Vector2.zero, Vector2.one, 38, TextAlignmentOptions.Center);
        MontagemComum.Texto("Pular", canvasIntro.transform, new Vector2(1, 1), new Vector2(1, 1), 26, TextAlignmentOptions.TopRight)
            .text = "Espaço: pular";
        var pular = canvasIntro.transform.Find("Pular") as RectTransform;
        pular.sizeDelta = new Vector2(300, 50);
        pular.anchoredPosition = new Vector2(-180, -40);
        intro.caixaLegenda = canvasIntro.gameObject;
        return intro;
    }

    static void MontarHud(Partida p, Transform raiz)
    {
        var escuro = new Color(0.04f, 0.06f, 0.1f, 0.6f);
        // Combustível: ícone do galão + barra arredondada que muda de cor (verde → amarelo → vermelho)
        var comb = MontagemComum.Caixa("Combustivel", raiz, new Vector2(0, 1), new Vector2(32, -28), new Vector2(480, 76), escuro);
        MontagemComum.Icone(comb, "icone_galao", new Vector2(0, 0.5f), new Vector2(14, 0), 58);
        var trilho = MontagemComum.Caixa("Trilho", comb, new Vector2(0, 0.5f), new Vector2(84, 0), new Vector2(380, 34), new Color(0, 0, 0, 0.55f));
        var area = new GameObject("Area", typeof(RectTransform)).GetComponent<RectTransform>();
        area.SetParent(trilho, false);
        area.anchorMin = Vector2.zero;
        area.anchorMax = Vector2.one;
        area.offsetMin = new Vector2(4, 4);       // a barra dentro da área não tem margem: com 0% fica com largura 0
        area.offsetMax = new Vector2(-4, -4);
        var barra = MontagemComum.Caixa("Nivel", area, Vector2.zero, Vector2.zero, Vector2.zero, Partida.BarraCor(1));
        barra.anchorMin = Vector2.zero;
        barra.anchorMax = Vector2.one;
        barra.offsetMin = barra.offsetMax = Vector2.zero;
        p.barraCombustivel = barra;

        // Moedas: ícone + número grande
        var moedas = MontagemComum.Caixa("Moedas", raiz, new Vector2(0, 1), new Vector2(32, -114), new Vector2(220, 76), escuro);
        MontagemComum.Icone(moedas, "icone_moeda", new Vector2(0, 0.5f), new Vector2(12, 0), 58);
        p.textoMoedas = MontagemComum.Texto("Texto", moedas, new Vector2(0.38f, 0), new Vector2(0.95f, 1), 48, TextAlignmentOptions.Left);

        // Distância: bandeira + metros em itálico; recorde com troféu embaixo
        var dist = MontagemComum.Caixa("Distancia", raiz, new Vector2(0.5f, 1), new Vector2(0, -24), new Vector2(440, 136), escuro);
        MontagemComum.Icone(dist, "icone_bandeira", new Vector2(0, 1), new Vector2(18, -12), 64);
        p.textoDistancia = MontagemComum.Texto("Metros", dist, new Vector2(0.18f, 0.38f), new Vector2(1, 1), 70, TextAlignmentOptions.Center);
        p.textoDistancia.font = MontagemComum.FonteItalica;
        MontagemComum.Icone(dist, "icone_trofeu", new Vector2(0.5f, 0), new Vector2(-120, 10), 36);
        p.textoRecorde = MontagemComum.Texto("Recorde", dist, new Vector2(0.1f, 0.02f), new Vector2(0.95f, 0.36f), 28, TextAlignmentOptions.Center);
        p.textoRecorde.color = new Color(1f, 0.85f, 0.4f);

        var aviso = MontagemComum.Caixa("Sem gasolina", raiz, new Vector2(0.5f, 1), new Vector2(0, -165), new Vector2(440, 64), new Color(0.75f, 0.1f, 0.1f, 0.85f));
        aviso.gameObject.AddComponent<CanvasGroup>();
        MontagemComum.Texto("Texto", aviso, Vector2.zero, Vector2.one, 38, TextAlignmentOptions.Center).text = "SEM GASOLINA!";
        p.avisoSemGasolina = aviso.gameObject;

        var caixaAviso = MontagemComum.Caixa("Aviso", raiz, new Vector2(0.5f, 1), new Vector2(0, -240), new Vector2(520, 60), new Color(0, 0, 0, 0.6f));
        p.textoAviso = MontagemComum.Texto("Texto", caixaAviso, Vector2.zero, Vector2.one, 34, TextAlignmentOptions.Center);

        var pausa = MontagemComum.Painel("Pausa", raiz);
        MontagemComum.Cartao(pausa, new Vector2(680, 640), 20);
        MontagemComum.Texto("Titulo", pausa, new Vector2(0.2f, 0.62f), new Vector2(0.8f, 0.82f), 96, TextAlignmentOptions.Center).text = "PAUSADO";
        var colPausa = MontagemComum.Coluna(pausa, new Vector2(0.5f, 0.42f), 480);
        MontagemComum.Botao(colPausa, "Continuar", p.AlternarPausa);
        MontagemComum.Botao(colPausa, "Reiniciar", p.Reiniciar);
        MontagemComum.Botao(colPausa, "Menu principal", p.IrParaMenu);
        p.painelPausa = pausa.gameObject;

        var fim = MontagemComum.Painel("Fim", raiz);
        MontagemComum.Cartao(fim, new Vector2(900, 720), 20);
        MontagemComum.Icone(fim, "icone_bandeira", new Vector2(0.5f, 0.5f), new Vector2(0, 322), 96);
        p.textoFim = MontagemComum.Texto("Texto", fim, new Vector2(0.15f, 0.42f), new Vector2(0.85f, 0.76f), 84, TextAlignmentOptions.Center);
        var colFim = MontagemComum.Coluna(fim, new Vector2(0.5f, 0.32f), 480);
        MontagemComum.Botao(colFim, "Jogar de novo", p.Reiniciar);
        MontagemComum.Botao(colFim, "Menu", p.IrParaMenu);
        p.painelFim = fim.gameObject;
    }
}
