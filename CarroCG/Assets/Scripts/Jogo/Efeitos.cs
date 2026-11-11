using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// Partículas (poeira, faíscas, brilho da moeda, respingo no lago), tremor da câmera e pós-processamento por hora do dia.
public class Efeitos : MonoBehaviour
{
    public F1 f1;
    public CameraSegue cam;
    public CicloDia ciclo;
    public Terreno terreno;
    public ParticleSystem poeira, faiscas, brilho, respingo;
    public float taxaPoeira = 45f, taxaRespingo = 30f;

    public ParticleSystem Poeira => poeira;
    public ParticleSystem Faiscas => faiscas;
    public ParticleSystem Brilho => brilho;

    Rigidbody2D rodaT, chassi;
    ColorAdjustments ajustes;

    void OnEnable()
    {
        Coletavel.ColetadoEm += AoColetar;
        if (cam) cam.Impacto += AoImpacto;
    }

    void OnDisable()
    {
        Coletavel.ColetadoEm -= AoColetar;
        if (cam) cam.Impacto -= AoImpacto;
    }

    void Start()
    {
        rodaT = f1.eixoTraseiro.connectedBody;
        chassi = f1.GetComponent<Rigidbody2D>();
        var m = poeira.main;
        m.startColor = Physics2D.gravity.y > -5 ? new Color(0.75f, 0.75f, 0.78f, 0.7f) : new Color(0.62f, 0.48f, 0.32f, 0.7f);   // Lua: cinza
        CriarPos();
    }

    void CriarPos()
    {
        var v = gameObject.AddComponent<Volume>();
        v.isGlobal = true;
        v.profile = ScriptableObject.CreateInstance<VolumeProfile>();
        var bloom = v.profile.Add<Bloom>(true);
        bloom.intensity.value = 0.6f;
        bloom.threshold.value = 0.9f;
        bloom.scatter.value = 0.6f;
        var vinheta = v.profile.Add<Vignette>(true);
        vinheta.intensity.value = 0.25f;
        vinheta.smoothness.value = 0.5f;
        ajustes = v.profile.Add<ColorAdjustments>(true);
        var c = cam ? cam.GetComponent<Camera>() : Camera.main;
        c.GetUniversalAdditionalCameraData().renderPostProcessing = true;
    }

    void Update()
    {
        var e = poeira.emission;
        bool acelerando = f1.NoChao && f1.motorLigado && f1.acelerador > 0 && Time.timeScale > 0;
        e.rateOverTime = acelerando ? taxaPoeira : 0;
        poeira.transform.position = rodaT.position + new Vector2(-0.2f, -0.3f);

        var agua = AguaSob(chassi.position.x);
        var r = respingo.emission;
        r.rateOverTime = agua != null && Mathf.Abs(chassi.linearVelocity.x) > 3 ? taxaRespingo : 0;
        if (agua != null) respingo.transform.position = new Vector2(chassi.position.x - 0.5f, agua.Value.yMax);

        if (ciclo && ajustes)           // noite: menos saturação e um pouco mais escuro
        {
            float n = ciclo.Atual.noite;
            ajustes.saturation.value = -20 * n;
            ajustes.postExposure.value = -0.3f * n;
        }
    }

    Rect? AguaSob(float x)
    {
        if (!terreno) return null;
        foreach (var c in terreno.chunks)
            if (c.agua is Rect a && x >= a.xMin && x <= a.xMax) return a;
        return null;
    }

    void AoImpacto(float forca)
    {
        cam.Tremer(forca);
        faiscas.transform.position = chassi.position + Vector2.down * 0.3f;
        faiscas.Emit(20);
    }

    void AoColetar(TipoColetavel tipo, Vector2 onde)
    {
        if (tipo != TipoColetavel.Moeda) return;
        brilho.transform.position = onde;
        brilho.Emit(15);
    }
}
