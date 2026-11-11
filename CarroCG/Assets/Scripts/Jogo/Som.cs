using UnityEngine;

// Áudio do jogo: motor em loop com pitch pela roda traseira; moeda, galão, batida e fim por eventos.
public class Som : MonoBehaviour
{
    public F1 f1;
    public Partida partida;
    public CameraSegue cam;

    public AudioSource Motor { get; private set; }
    public AudioClip ClipMoeda { get; private set; }
    public AudioClip UltimoEfeito { get; private set; }

    AudioSource efeitos;
    AudioClip galao, batida, fim;
    Rigidbody2D rodaT;

    const int Taxa = 44100;

    static AudioClip Clip(string nome, float[] amostras)
    {
        var c = AudioClip.Create(nome, amostras.Length, 1, Taxa, false);
        c.SetData(amostras, 0);
        return c;
    }

    void Awake()
    {
        Motor = gameObject.AddComponent<AudioSource>();
        Motor.clip = Clip("Motor", SomSintetizado.Motor(Taxa));
        Motor.loop = true;
        Motor.volume = 0;
        efeitos = gameObject.AddComponent<AudioSource>();
        ClipMoeda = Clip("Moeda", SomSintetizado.Moeda(Taxa));
        galao = Clip("Galao", SomSintetizado.Galao(Taxa));
        batida = Clip("Batida", SomSintetizado.Batida(Taxa));
        fim = Clip("Fim", SomSintetizado.Fim(Taxa));
    }

    void OnEnable()
    {
        Coletavel.Coletado += AoColetar;
        partida.Terminou += AoTerminar;
        if (cam) cam.Impacto += AoImpacto;
    }

    void OnDisable()
    {
        Coletavel.Coletado -= AoColetar;
        partida.Terminou -= AoTerminar;
        if (cam) cam.Impacto -= AoImpacto;
    }

    void Start()
    {
        AudioListener.volume = Preferencias.Volume / 10f;
        rodaT = f1.eixoTraseiro.connectedBody;
        Motor.Play();
    }

    void Update()
    {
        bool roncando = partida.EstadoAtual == Partida.Estado.Jogando && f1.motorLigado;
        Motor.volume = roncando ? 0.35f + 0.25f * Mathf.Abs(f1.acelerador) : 0;
        Motor.pitch = SomSintetizado.PitchMotor(rodaT.angularVelocity);
    }

    void Tocar(AudioClip c, float volume = 1)
    {
        UltimoEfeito = c;
        efeitos.PlayOneShot(c, volume);
    }

    void AoColetar(TipoColetavel t) => Tocar(t == TipoColetavel.Moeda ? ClipMoeda : galao);
    void AoTerminar() => Tocar(fim);
    void AoImpacto(float forca) => Tocar(batida, Mathf.Clamp01(forca / 15f));
}
