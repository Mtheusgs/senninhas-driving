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
        MontagemComum.CriarSombra(f1, terreno, ciclo);

        var partida = new GameObject("Partida").AddComponent<Partida>();
        partida.f1 = f1.f1;
        partida.terreno = terreno;
        var canvas = MontagemComum.CriarCanvas(cam);
        MontarHud(partida, canvas.transform);

        var aula = new GameObject("Modo Aula").AddComponent<ModoAula>();
        aula.terreno = terreno;
        aula.f1 = f1.f1;
        aula.partida = partida;
        aula.ciclo = ciclo;
        aula.materialLinha = MontagemComum.MaterialUnlit;
        var caixaAula = MontagemComum.Caixa("Rotulo Aula", canvas.transform, new Vector2(0, 0), new Vector2(40, 40),   // canto de baixo: não cobre moedas nem HUD
                                            new Vector2(720, 250), new Color(0, 0, 0, 0.55f));   // não cobre a caixa de distância
        aula.rotulo = MontagemComum.Texto("Texto", caixaAula, new Vector2(0.03f, 0.05f), new Vector2(0.97f, 0.95f),
                                          30, TextAlignmentOptions.TopLeft);
        aula.rotulo.enableAutoSizing = true;
        aula.rotulo.fontSizeMin = 16;
        aula.rotulo.fontSizeMax = 30;
        MontagemComum.CriarEventSystem();

        Directory.CreateDirectory("Assets/Scenes");
        EditorSceneManager.SaveScene(cena, Cena);
        MontagemComum.DefinirBuild();
        Debug.Log("MontarJogo: cena salva em " + Cena);
    }

    static void MontarHud(Partida p, Transform raiz)
    {
        var fundoBarra = MontagemComum.Caixa("Combustivel", raiz, new Vector2(0, 1), new Vector2(40, -40), new Vector2(420, 34), new Color(0, 0, 0, 0.55f));
        // trilho com a margem; a barra dentro dele não tem margem, então com 0% fica com largura 0 (sem lasca)
        var trilho = new GameObject("Trilho", typeof(RectTransform)).GetComponent<RectTransform>();
        trilho.SetParent(fundoBarra, false);
        trilho.anchorMin = Vector2.zero;
        trilho.anchorMax = Vector2.one;
        trilho.offsetMin = new Vector2(4, 4);
        trilho.offsetMax = new Vector2(-4, -4);
        var barra = MontagemComum.Caixa("Nivel", trilho, Vector2.zero, Vector2.zero, Vector2.zero, new Color(1f, 0.55f, 0.1f));
        barra.anchorMin = Vector2.zero;
        barra.anchorMax = Vector2.one;
        barra.offsetMin = barra.offsetMax = Vector2.zero;
        p.barraCombustivel = barra;

        var caixaMoedas = MontagemComum.Caixa("Moedas", raiz, new Vector2(0, 1), new Vector2(40, -90), new Vector2(260, 56), new Color(0, 0, 0, 0.45f));
        p.textoMoedas = MontagemComum.Texto("Texto", caixaMoedas, Vector2.zero, Vector2.one, 34, TextAlignmentOptions.Center);

        var caixaDist = MontagemComum.Caixa("Distancia", raiz, new Vector2(0.5f, 1), new Vector2(0, -30), new Vector2(380, 120), new Color(0, 0, 0, 0.45f));
        p.textoDistancia = MontagemComum.Texto("Metros", caixaDist, new Vector2(0, 0.35f), Vector2.one, 60, TextAlignmentOptions.Center);
        p.textoRecorde = MontagemComum.Texto("Recorde", caixaDist, Vector2.zero, new Vector2(1, 0.4f), 26, TextAlignmentOptions.Center);

        var aviso = MontagemComum.Caixa("Sem gasolina", raiz, new Vector2(0.5f, 1), new Vector2(0, -165), new Vector2(440, 64), new Color(0.75f, 0.1f, 0.1f, 0.85f));
        aviso.gameObject.AddComponent<CanvasGroup>();
        MontagemComum.Texto("Texto", aviso, Vector2.zero, Vector2.one, 38, TextAlignmentOptions.Center).text = "SEM GASOLINA!";
        p.avisoSemGasolina = aviso.gameObject;

        var caixaAviso = MontagemComum.Caixa("Aviso", raiz, new Vector2(0.5f, 1), new Vector2(0, -240), new Vector2(520, 60), new Color(0, 0, 0, 0.6f));
        p.textoAviso = MontagemComum.Texto("Texto", caixaAviso, Vector2.zero, Vector2.one, 34, TextAlignmentOptions.Center);

        var pausa = MontagemComum.Painel("Pausa", raiz);
        MontagemComum.Texto("Titulo", pausa, new Vector2(0.2f, 0.62f), new Vector2(0.8f, 0.82f), 96, TextAlignmentOptions.Center).text = "PAUSADO";
        var colPausa = MontagemComum.Coluna(pausa, new Vector2(0.5f, 0.42f), 480);
        MontagemComum.Botao(colPausa, "Continuar", p.AlternarPausa);
        MontagemComum.Botao(colPausa, "Reiniciar", p.Reiniciar);
        MontagemComum.Botao(colPausa, "Menu principal", p.IrParaMenu);
        p.painelPausa = pausa.gameObject;

        var fim = MontagemComum.Painel("Fim", raiz);
        p.textoFim = MontagemComum.Texto("Texto", fim, new Vector2(0.15f, 0.5f), new Vector2(0.85f, 0.85f), 84, TextAlignmentOptions.Center);
        var colFim = MontagemComum.Coluna(fim, new Vector2(0.5f, 0.32f), 480);
        MontagemComum.Botao(colFim, "Jogar de novo", p.Reiniciar);
        MontagemComum.Botao(colFim, "Menu", p.IrParaMenu);
        p.painelFim = fim.gameObject;
    }
}
