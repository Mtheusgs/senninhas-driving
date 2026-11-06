using UnityEngine;
using UnityEngine.Rendering.Universal;

// Ciclo dia/noite (~3 min): a hora do dia (Ceu.Estado) define a luz do terreno, as globais do shader Phong do F1,
// o céu, o sol e a lua em arco, as estrelas, a luz global 2D e os faróis. Na Lua (lunar + fixo): sol parado,
// ambiente baixo (sombra dura), céu preto intocado.
[DefaultExecutionOrder(-50)]
public class CicloDia : MonoBehaviour
{
    public float duracao = 180f;
    public float tempoInicial = 0.12f;
    public bool fixo;
    public float tempoFixo = 0.097f;            // ≈ 35° de elevação
    public bool lunar;
    public float fatorAmbienteLunar = 0.35f;
    public float intervaloTerreno = 0.5f;
    public float raioArco = 8f;

    public Terreno terreno;
    public Light2D luzGlobal, farol, lanterna;
    public Camera cam;
    public SpriteRenderer ceuGradiente, sol, lua, estrelas, feixe;

    public float Tempo { get; set; }
    public EstadoCeu Atual { get; private set; }

    static readonly int IdDir = Shader.PropertyToID("_DirSol"), IdSol = Shader.PropertyToID("_CorSol"),
                        IdAmbiente = Shader.PropertyToID("_CorAmbiente");
    float proxima;

    void Start()
    {
        Tempo = fixo ? tempoFixo : tempoInicial;
        Aplicar(true);
    }

    void Update()
    {
        if (!fixo) Tempo = Mathf.Repeat(Tempo + Time.deltaTime / duracao, 1);
        Aplicar(Time.time >= proxima);
    }

    public void Aplicar(bool terrenoTambem)
    {
        var e = Ceu.Estado(Tempo);
        if (lunar)
        {
            e.corLuz = Color.white;
            e.corAmbiente *= fatorAmbienteLunar;
            e.noite = 0;
        }
        Atual = e;
        Shader.SetGlobalVector(IdDir, new Vector4(e.dirLuz.x, e.dirLuz.y, 0.7f, 1));
        Shader.SetGlobalColor(IdSol, e.corLuz);
        Shader.SetGlobalColor(IdAmbiente, e.corAmbiente);
        if (luzGlobal)
        {
            luzGlobal.color = e.corLuz;
            luzGlobal.intensity = Mathf.Lerp(0.45f, 1f, 1 - e.noite);
        }
        if (!lunar)
        {
            if (cam) cam.backgroundColor = e.corCeuHorizonte;
            if (ceuGradiente) ceuGradiente.color = e.corCeuTopo;
            Posicionar(sol, e.dirSol);
            Posicionar(lua, -e.dirSol);
            if (estrelas) estrelas.color = new Color(1, 1, 1, e.noite);
        }
        float farois = lunar ? 0 : e.noite;
        if (farol) farol.intensity = farois * 1.2f;
        if (lanterna) lanterna.intensity = farois;
        if (feixe) feixe.color = new Color(1, 1, 1, farois);
        if (terrenoTambem && terreno)
        {
            terreno.Iluminar(e.dirLuz, e.corLuz, e.corAmbiente);
            proxima = Time.time + intervaloTerreno;
        }
    }

    // Sol e lua num arco em volta da câmera; somem abaixo do horizonte.
    void Posicionar(SpriteRenderer astro, Vector2 dir)
    {
        if (!astro) return;
        astro.enabled = dir.y > -0.1f;
        astro.transform.localPosition = new Vector3(dir.x * raioArco * 1.6f, dir.y * raioArco - 2, 15);
    }
}
