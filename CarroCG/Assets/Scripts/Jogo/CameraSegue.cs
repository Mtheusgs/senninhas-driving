using UnityEngine;

// Segue o carro olhando à frente na direção da velocidade e abre o zoom com a velocidade.
[RequireComponent(typeof(Camera))]
public class CameraSegue : MonoBehaviour
{
    public Rigidbody2D alvo;
    public Vector2 deslocamento = new(3f, 1.5f);
    public float suavidade = 0.25f;
    public float olharAFrente = 0.3f, olharMax = 4f;
    public float zoomMin = 7f, zoomMax = 11f, velocidadeZoomMax = 18f, suavidadeZoom = 0.8f;

    public Rect? enquadrar;
    public float limiarImpacto = 6f;                 // variação brusca da velocidade vertical (m/s) = aterrissagem forte
    public event System.Action<float> Impacto;
    public float Tremor { get; private set; }        // amplitude atual do tremor (some em ~0,3 s)
    Vector3 basePos;
    bool temBase;

    public void Tremer(float forca) => Tremor = Mathf.Max(Tremor, Mathf.Min(0.35f, forca * 0.025f));      // modo aula: mostra a janela de recorte inteira do trecho atual

    Camera cam;
    Vector3 vel;
    float velZoom;

    readonly float[] historico = new float[6];     // ~0,12 s de passos de física
    int iHist;

    void Awake() => cam = GetComponent<Camera>();
    void OnEnable() => temBase = false;       // a intro move a câmera à mão: segue a partir de onde ela está

    void FixedUpdate()
    {
        if (!alvo) return;
        // menor velocidade vertical dos últimos passos: a suspensão espalha a freada da aterrissagem por alguns passos
        float vy = alvo.linearVelocity.y;
        historico[iHist] = vy;
        iHist = (iHist + 1) % historico.Length;
        float menor = Mathf.Min(historico);
        if (vy - menor > limiarImpacto && menor < -limiarImpacto * 0.5f)
        {
            Impacto?.Invoke(vy - menor);
            for (int i = 0; i < historico.Length; i++) historico[i] = vy;     // um impacto por aterrissagem
        }
    }

    void LateUpdate()
    {
        var (destino, zoom) = Alvo();
        if (!temBase) { basePos = transform.position; temBase = true; }
        basePos = Vector3.SmoothDamp(basePos, destino, ref vel, suavidade);
        transform.position = basePos + (Vector3)(Random.insideUnitCircle * Tremor);     // tremor por cima do seguir
        Tremor = Mathf.MoveTowards(Tremor, 0, Time.unscaledDeltaTime / 0.3f * 0.35f);     // some até com o jogo pausado
        cam.orthographicSize = Mathf.SmoothDamp(cam.orthographicSize, zoom, ref velZoom, suavidadeZoom);
    }

    // Vai direto para o alvo, sem suavizar (o demo do menu recomeçou na largada).
    public void Encaixar()
    {
        var (destino, zoom) = Alvo();
        transform.position = basePos = destino;
        temBase = true;
        System.Array.Clear(historico, 0, historico.Length);     // teleporte não é aterrissagem
        Tremor = 0;
        cam.orthographicSize = zoom;
        vel = Vector3.zero;
        velZoom = 0;
    }

    (Vector3, float) Alvo()
    {
        Vector2 v = alvo.linearVelocity;
        Vector2 frente = Vector2.ClampMagnitude(v * olharAFrente, olharMax);
        Vector3 destino = alvo.position + deslocamento + frente;
        destino.z = transform.position.z;
        float zoom = Mathf.Lerp(zoomMin, zoomMax, Mathf.Clamp01(v.magnitude / velocidadeZoomMax));
        if (enquadrar is Rect r)
        {
            // a janela inteira e também o carro, com folga acima dele para não ficar atrás do HUD
            r.yMax = Mathf.Max(r.yMax, alvo.position.y + 8);
            destino.y = r.center.y;
            zoom = Mathf.Max(zoom, r.height / 2 + 1);
        }
        return (destino, zoom);
    }
}
