using UnityEngine;

// Aplica o modo (Terra/Lua) e a qualidade antes de qualquer outro componente da cena.
[DefaultExecutionOrder(-100)]
public class ConfigurarPartida : MonoBehaviour
{
    public Terreno terreno;
    public F1 f1;
    public Camera cam;
    public GameObject ceuLunar;
    public bool forcarTerra;            // fundo do menu: sempre Terra

    public Modo ModoAtual { get; private set; }

    void Awake()
    {
        ModoAtual = forcarTerra ? Modo.Terra : Sessao.Modo;
        var p = ModoJogo.Parametros(ModoAtual);
        Physics2D.gravity = new Vector2(0, p.gravidade);      // global: definido sempre, nas duas cenas
        terreno.semente = p.semente;
        terreno.amplitudeInicial = p.amplitudeInicial;
        terreno.ganhoPorTrecho = p.ganhoPorTrecho;
        terreno.amplitudeMax = p.amplitudeMax;
        terreno.paleta = p.paleta;
        terreno.ppu = Preferencias.PpuTerreno[Preferencias.Qualidade];
        f1.torqueNoAr = p.torqueNoAr;
        cam.backgroundColor = p.ceu;
        if (ceuLunar) ceuLunar.SetActive(p.ceuLunar);
    }
}
