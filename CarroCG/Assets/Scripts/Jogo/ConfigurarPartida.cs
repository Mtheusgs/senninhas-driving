using UnityEngine;

// Aplica o modo (Terra/Lua) e a qualidade antes de qualquer outro componente da cena.
[DefaultExecutionOrder(-100)]
public class ConfigurarPartida : MonoBehaviour
{
    public Terreno terreno;
    public F1 f1;
    public Camera cam;
    public GameObject ceuLunar;
    public CicloDia ciclo;
    public bool forcarTerra;            // fundo do menu: sempre Terra
    public float limiteAmplitude;       // > 0: morros mais suaves (fundo do menu)

    public Modo ModoAtual { get; private set; }

    void Awake()
    {
        ModoAtual = forcarTerra ? Modo.Terra : Sessao.Modo;
        var p = ModoJogo.Parametros(ModoAtual);
        Physics2D.gravity = new Vector2(0, p.gravidade);      // global: definido sempre, nas duas cenas
        terreno.semente = p.semente;
        terreno.amplitudeInicial = p.amplitudeInicial;
        terreno.ganhoPorTrecho = p.ganhoPorTrecho;
        terreno.amplitudeMax = limiteAmplitude > 0 ? Mathf.Min(p.amplitudeMax, limiteAmplitude) : p.amplitudeMax;
        terreno.paleta = p.paleta;
        terreno.lagos = p.lagos;
        terreno.ppu = Preferencias.PpuTerreno[Preferencias.Qualidade];
        f1.torqueNoAr = p.torqueNoAr;
        cam.backgroundColor = p.ceu;
        if (ceuLunar) ceuLunar.SetActive(p.ceuLunar);
        if (ciclo && p.ceuLunar) { ciclo.fixo = true; ciclo.lunar = true; }      // Lua: sol parado, sombra dura
        if (ciclo && forcarTerra) { ciclo.fixo = true; ciclo.tempoFixo = 0.12f; } // menu: manhã (sol de lado, longe do título)
    }
}
