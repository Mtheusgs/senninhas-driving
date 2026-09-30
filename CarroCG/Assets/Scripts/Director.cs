using System.Collections;
using UnityEngine;

// Roteiro do vídeo. Ajuste as durações no Inspector para casar com a narração.
[RequireComponent(typeof(Camera))]
public class Director : MonoBehaviour
{
    public PixelCanvas canvas;
    public CarFollower carro;
    public BezierPath pista;
    public GameObject cenario;
    public SpriteRenderer fade;

    [Header("Câmera")]
    public float zoomMontagem = 1.1f;
    public float zoomGeral = 6f;
    public Vector3 posGeral = new Vector3(0, 0, -10);

    [Header("Durações (s)")]
    public float abertura = 10;
    public float contorno = 25;
    public float preenchimento = 20;
    public float transicao = 10;
    public float tracarPista = 8;
    public float mostrarGuias = 2;
    public float ida = 30;
    public float volta = 10;
    public float final = 10;

    IEnumerator Start()
    {
        var cam = GetComponent<Camera>();
        cenario.SetActive(false);
        fade.color = Color.clear;
        carro.Posicionar(0);
        var posMontagem = carro.transform.position + new Vector3(0, 0.6f, -10);
        cam.transform.position = posMontagem;
        cam.orthographicSize = zoomMontagem;

        yield return new WaitForSeconds(abertura);
        yield return canvas.DesenharContornos(contorno);        // rasterização
        yield return canvas.Preencher(preenchimento);           // preenchimento

        canvas.MostrarGrade(false);
        cenario.SetActive(true);
        yield return Anim.Durante(transicao, k =>
        {
            k = Mathf.SmoothStep(0, 1, k);
            cam.orthographicSize = Mathf.Lerp(zoomMontagem, zoomGeral, k);
            cam.transform.position = Vector3.Lerp(posMontagem, posGeral, k);
        });

        yield return pista.Revelar(tracarPista);                // curva de Bézier
        yield return new WaitForSeconds(mostrarGuias);
        pista.EsconderGuias();

        yield return carro.Percorrer(0, 1, ida);                // translação + rotação
        carro.Espelhar();                                       // espelhamento
        yield return carro.Percorrer(1, 0, volta);

        yield return Anim.Durante(final, k => fade.color = new Color(0, 0, 0, k));
    }
}
