using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Telas do menu principal. Todas as ações são públicas e ligadas aos botões por MontarMenu.
public class Menu : MonoBehaviour
{
    public GameObject principal, escolhaModo, garagem, controles, configuracoes, tecnicas, recorde;
    public TMP_Text textoTecnicas, textoRecorde, textoZerar, textoTelaCheia, textoModoAula, textoQualidade, textoCor;
    public Image previaPintura;
    public RectTransform previaCarro;
    public float velocidadeVitrine = 1.2f;

    public GameObject TelaAtual { get; private set; }
    bool confirmarZerar;

    GameObject[] Telas => new[] { principal, escolhaModo, garagem, controles, configuracoes, tecnicas, recorde };

    void Start()
    {
        Time.timeScale = 1;
        Screen.fullScreen = Preferencias.TelaCheia;
        textoTecnicas.text = TecnicasCG.Texto();
        Abrir(principal);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) && TelaAtual != principal) Abrir(principal);
        if (TelaAtual == garagem)
        {
            var s = previaCarro.localScale;      // vitrine: escala x de +1 a -1 (espelhamento ao passar pelo meio)
            s.x = Mathf.Cos(Time.time * velocidadeVitrine);
            previaCarro.localScale = s;
        }
    }

    void Abrir(GameObject tela)
    {
        foreach (var t in Telas) t.SetActive(t == tela);
        TelaAtual = tela;
        confirmarZerar = false;
        AtualizarTextos();
        var botao = tela.GetComponentInChildren<Button>();
        if (botao && EventSystem.current) EventSystem.current.SetSelectedGameObject(botao.gameObject);
    }

    public void AbrirPrincipal() => Abrir(principal);
    public void AbrirEscolhaModo() => Abrir(escolhaModo);
    public void AbrirGaragem() => Abrir(garagem);
    public void AbrirControles() => Abrir(controles);
    public void AbrirConfiguracoes() => Abrir(configuracoes);
    public void AbrirTecnicas() => Abrir(tecnicas);
    public void AbrirRecorde() => Abrir(recorde);

    public void Jogar(int modo)
    {
        Sessao.Modo = (Modo)modo;
        SceneManager.LoadScene("Jogo");
    }

    public void EscolherCor(int indice)
    {
        Preferencias.Cor = indice;
        AtualizarTextos();
    }

    public void AlternarTelaCheia()
    {
        Preferencias.TelaCheia = !Preferencias.TelaCheia;
        Screen.fullScreen = Preferencias.TelaCheia;
        AtualizarTextos();
    }

    public void AlternarModoAula()
    {
        Preferencias.ModoAula = !Preferencias.ModoAula;
        AtualizarTextos();
    }

    public void ProximaQualidade()
    {
        Preferencias.Qualidade = (Preferencias.Qualidade + 1) % Preferencias.PpuTerreno.Length;
        AtualizarTextos();
    }

    // Duas etapas: o 1º clique pede confirmação, o 2º zera.
    public void ZerarRecordes()
    {
        if (confirmarZerar) Preferencias.ZerarRecordes();
        confirmarZerar = !confirmarZerar;
        AtualizarTextos();
    }

    public void Sair()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    void AtualizarTextos()
    {
        textoTelaCheia.text = $"Tela cheia: {(Preferencias.TelaCheia ? "sim" : "não")}";
        textoModoAula.text = $"Modo aula ao iniciar: {(Preferencias.ModoAula ? "sim" : "não")}";
        textoQualidade.text = $"Qualidade: {Preferencias.NomesQualidade[Preferencias.Qualidade]}";
        textoCor.text = Garagem.Nomes[Garagem.IndiceEscolhido];
        previaPintura.color = Garagem.CorEscolhida;
        textoRecorde.text = $"<b>Terra</b>\n{Preferencias.Recorde(Modo.Terra):0} m  ·  {Preferencias.MoedasDoRecorde(Modo.Terra)} moedas\n\n" +
                            $"<b>Lua</b>\n{Preferencias.Recorde(Modo.Lua):0} m  ·  {Preferencias.MoedasDoRecorde(Modo.Lua)} moedas";
        textoZerar.text = confirmarZerar ? "Confirmar? Clique de novo" : "Zerar recordes";
    }
}
