using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

// Estados do jogo, combustível, contagem, HUD e recorde.
public class Partida : MonoBehaviour
{
    public enum Estado { Jogando, Pausa, Fim }

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

    const string ChaveRecorde = "recorde";
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

    public void AlternarPausa()
    {
        bool pausar = EstadoAtual == Estado.Jogando;
        EstadoAtual = pausar ? Estado.Pausa : Estado.Jogando;
        Time.timeScale = pausar ? 0 : 1;
        painelPausa.SetActive(pausar);
    }

    void AoColetar(TipoColetavel tipo)
    {
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
        float recorde = PlayerPrefs.GetFloat(ChaveRecorde, 0);
        if (Distancia > recorde)
        {
            recorde = Distancia;
            PlayerPrefs.SetFloat(ChaveRecorde, recorde);
            PlayerPrefs.Save();
        }
        textoFim.text = $"{motivo}\n<size=60%>{Distancia:0} m  ·  {Moedas} moedas\nRecorde: {recorde:0} m\n\nR para jogar de novo</size>";
        painelFim.SetActive(true);
        AtualizarHud();
    }

    void AtualizarHud()
    {
        var a = barraCombustivel.anchorMax;
        a.x = combustivel.Fracao;
        barraCombustivel.anchorMax = a;
        textoMoedas.text = $"Moedas  {Moedas}";
        textoDistancia.text = $"{Distancia:0} m";
        textoRecorde.text = $"Recorde {PlayerPrefs.GetFloat(ChaveRecorde, 0):0} m";
    }
}
