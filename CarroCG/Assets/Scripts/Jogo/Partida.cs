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
    public Combustivel combustivel = new();
    public float galaoQuantidade = 45f;

    [Header("HUD")]
    public RectTransform barraCombustivel;
    public TMP_Text textoMoedas, textoDistancia, textoRecorde, textoFim;
    public GameObject painelPausa, painelFim;

    public Estado EstadoAtual { get; private set; }
    public int Moedas { get; private set; }
    public float Distancia { get; private set; }

    float x0;

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
        AtualizarHud();
    }

    void Update()
    {
        if (EstadoAtual == Estado.Congelado) return;
        if (Input.GetKeyDown(KeyCode.R)) { Reiniciar(); return; }
        if (Input.GetKeyDown(KeyCode.Escape) && EstadoAtual != Estado.Fim) AlternarPausa();
        if (EstadoAtual != Estado.Jogando) return;

        combustivel.Consumir(Time.deltaTime, f1.motorLigado ? f1.acelerador : 0);
        f1.motorLigado = !combustivel.Vazio;
        Distancia = Mathf.Max(Distancia, f1.transform.position.x - x0);
        if (combustivel.VerificarPane(Time.deltaTime, f1.VelocidadeX)) Terminar("Sem combustível");
        AtualizarHud();
    }

    public void Reiniciar() => SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);

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
        AtualizarHud();
    }

    static void Selecionar(GameObject painel)
    {
        var botao = painel.GetComponentInChildren<Button>();
        if (botao && EventSystem.current) EventSystem.current.SetSelectedGameObject(botao.gameObject);
    }

    void AtualizarHud()
    {
        var a = barraCombustivel.anchorMax;
        a.x = combustivel.Fracao;
        barraCombustivel.anchorMax = a;
        textoMoedas.text = $"Moedas  {Moedas}";
        textoDistancia.text = $"{Distancia:0} m";
        textoRecorde.text = $"Recorde {Preferencias.Recorde(Sessao.Modo):0} m";
    }
}
