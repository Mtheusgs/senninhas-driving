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

    Camera cam;
    Vector3 vel;
    float velZoom;

    void Awake() => cam = GetComponent<Camera>();

    void LateUpdate()
    {
        Vector2 v = alvo.linearVelocity;
        Vector2 frente = Vector2.ClampMagnitude(v * olharAFrente, olharMax);
        Vector3 destino = alvo.position + deslocamento + frente;
        destino.z = transform.position.z;
        transform.position = Vector3.SmoothDamp(transform.position, destino, ref vel, suavidade);
        float zoom = Mathf.Lerp(zoomMin, zoomMax, Mathf.Clamp01(v.magnitude / velocidadeZoomMax));
        cam.orthographicSize = Mathf.SmoothDamp(cam.orthographicSize, zoom, ref velZoom, suavidadeZoom);
    }
}
