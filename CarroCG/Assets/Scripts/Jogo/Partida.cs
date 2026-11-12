using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Estados do jogo, combustível, contagem, HUD, recorde por modo e botões de pausa/fim.
public class Partida : MonoBehaviour
{
    public enum Estado { Jogando, Pausa, Fim, Congelado }

    public F1 f1;
    public Terreno terreno;
    public Parallax parallax;
    public Combustivel combustivel = new();
    public float galaoQuantidade = 45f;
    public DetectorCapotagem capotagem = new();

    [Header("HUD")]
    public RectTransform barraCombustivel;
    public TMP_Text textoMoedas, textoDistancia, textoRecorde, textoFim;
    public GameObject painelPausa, painelFim, avisoSemGasolina;
    public TMP_Text textoAviso;               // aviso curto (ex.: modo de sombreamento)

    public Estado EstadoAtual { get; private set; }
    public event System.Action Terminou;
    public int Moedas { get; private set; }
    public float Distancia { get; private set; }

    float x0;
    float avisoAte;

    void OnEnable()
    {
        Coletavel.Coletado += AoColetar;
        f1.Capotou += AoCapotar;
    }

    void OnDisable()
    {
        Coletavel.Coletado -= AoColetar;
        f1.Capotou -= AoCapotar;
        Time.timeScale = 1;
    }

    void Start()
    {
        Time.timeScale = 1;
        combustivel.Encher();
        x0 = f1.transform.position.x;
        painelPausa.SetActive(false);
        painelFim.SetActive(false);
        avisoSemGasolina.SetActive(false);
        if (textoAviso) textoAviso.transform.parent.gameObject.SetActive(false);
        AtualizarHud();
    }

    void Update()
    {
        if (textoAviso && Time.unscaledTime > avisoAte) textoAviso.transform.parent.gameObject.SetActive(false);
        if (EstadoAtual == Estado.Congelado) return;
        if (Input.GetKeyDown(KeyCode.R)) { Reiniciar(); return; }
        if (Input.GetKeyDown(KeyCode.L) && EstadoAtual == Estado.Jogando) ProximoSombreamento();
        if (Input.GetKeyDown(KeyCode.P) && EstadoAtual == Estado.Jogando) AlternarProjecao();
        if (Input.GetKeyDown(KeyCode.Escape) && EstadoAtual != Estado.Fim) AlternarPausa();
        if (EstadoAtual != Estado.Jogando) return;

        combustivel.Consumir(Time.deltaTime, f1.motorLigado ? f1.acelerador : 0);
        f1.motorLigado = !combustivel.Vazio;
        Distancia = Mathf.Max(Distancia, f1.transform.position.x - x0);
        if (capotagem.Atualizar(Time.deltaTime, f1.transform.eulerAngles.z, f1.NoChao, f1.Velocidade)) { Terminar("Capotou!"); return; }
        if (combustivel.VerificarPane(Time.deltaTime, f1.Velocidade, f1.NoChao)) Terminar("Sem combustível");
        AtualizarHud();
    }

    public void Reiniciar() => SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);

    // Tecla L: Flat → Gouraud → Phong no terreno, com aviso por 1,5 s.
    public void ProximoSombreamento()
    {
        terreno.ProximoSombreamento();
        Avisar($"Sombreamento: {terreno.modoSombreamento}");
    }

    // Tecla P: projeção perspectiva ↔ paralela no fundo (parallax).
    public void AlternarProjecao()
    {
        parallax.perspectiva = !parallax.perspectiva;
        Avisar(parallax.perspectiva ? "Projeção: perspectiva" : "Projeção: paralela");
    }

    void Avisar(string texto)
    {
        if (!textoAviso) return;
        textoAviso.text = texto;
        textoAviso.transform.parent.gameObject.SetActive(true);
        avisoAte = Time.unscaledTime + 1.5f;
    }

    public void IrParaMenu()
    {
        Time.timeScale = 1;
        SceneManager.LoadScene("Menu");
    }

    public void AlternarPausa()
    {
        if (EstadoAtual != Estado.Jogando && EstadoAtual != Estado.Pausa) return;
        bool pausar = EstadoAtual == Estado.Jogando;
        EstadoAtual = pausar ? Estado.Pausa : Estado.Jogando;
        Time.timeScale = pausar ? 0 : 1;
        painelPausa.SetActive(pausar);
        if (pausar) Selecionar(painelPausa);
    }

    // Modo aula: segura o jogo parado (sem painel) enquanto a repintura por scanline acontece.
    public void Congelar(bool sim)
    {
        if (sim && EstadoAtual != Estado.Jogando) return;
        if (!sim && EstadoAtual != Estado.Congelado) return;
        EstadoAtual = sim ? Estado.Congelado : Estado.Jogando;
        Time.timeScale = sim ? 0 : 1;
    }

    void AoColetar(TipoColetavel tipo)
    {
        if (EstadoAtual != Estado.Jogando) return;
        if (tipo == TipoColetavel.Moeda) Moedas++;
        else combustivel.Abastecer(galaoQuantidade);
    }

    void AoCapotar()
    {
        if (EstadoAtual == Estado.Jogando) Terminar("Capotou!");
    }

    public void Terminar(string motivo)
    {
        EstadoAtual = Estado.Fim;
        f1.motorLigado = false;
        f1.controlePeloTeclado = false;     // sem motor e sem giro no ar depois do fim
        f1.acelerador = 0;
        bool novo = Preferencias.RegistrarPartida(Sessao.Modo, Distancia, Moedas);
        float recorde = Preferencias.Recorde(Sessao.Modo);
        textoFim.text = $"{motivo}\n<size=60%>{Distancia:0} m  ·  {Moedas} moedas\n" +
                        (novo ? "Novo recorde!" : $"Recorde: {recorde:0} m") + "</size>";
        painelFim.SetActive(true);
        Selecionar(painelFim);
        Terminou?.Invoke();
        AtualizarHud();
    }

    static void Selecionar(GameObject painel)
    {
        var botao = painel.GetComponentInChildren<Button>();
        if (botao && EventSystem.current) EventSystem.current.SetSelectedGameObject(botao.gameObject);
    }

    // Barra de combustível: verde cheia, amarela na metade, vermelha vazia.
    public static Color BarraCor(float fracao)
    {
        Color verde = new(0.3f, 0.85f, 0.35f), amarelo = new(1f, 0.85f, 0.15f), vermelho = new(0.92f, 0.18f, 0.12f);
        return fracao >= 0.5f ? Color.Lerp(amarelo, verde, (fracao - 0.5f) * 2) : Color.Lerp(vermelho, amarelo, fracao * 2);
    }

    void AtualizarHud()
    {
        var a = barraCombustivel.anchorMax;
        a.x = combustivel.Fracao;
        barraCombustivel.anchorMax = a;
        if (barraCombustivel.TryGetComponent(out UnityEngine.UI.Image img)) img.color = BarraCor(combustivel.Fracao);
        textoMoedas.text = $"{Moedas}";
        textoDistancia.text = $"{Distancia:0} m";
        textoRecorde.text = $"Recorde {Preferencias.Recorde(Sessao.Modo):0} m";
        bool semGasolina = combustivel.Vazio && EstadoAtual == Estado.Jogando;     // no embalo: avisa piscando
        avisoSemGasolina.SetActive(semGasolina);
        if (semGasolina && avisoSemGasolina.TryGetComponent(out CanvasGroup g))
            g.alpha = 0.55f + 0.45f * Mathf.Sin(Time.unscaledTime * 8f);
    }
}
