using System.Collections;
using System.Reflection;
using UnityEditor.SceneManagement;
using UnityEngine;

// Renderiza momentos da cena do vídeo em PNG (pasta em CAPTURA_DIR). Uso em batchmode, com GPU.
public static class Captura
{
    static void Rodar(IEnumerator e) { while (e.MoveNext()) { } }

    public static void CapturarMain()
    {
        string saida = System.Environment.GetEnvironmentVariable("CAPTURA_DIR");
        EditorSceneManager.OpenScene("Assets/Scenes/Main.unity");
        var d = Object.FindFirstObjectByType<Director>();
        var cam = d.GetComponent<Camera>();
        d.canvas.Inicializar();
        typeof(BezierPath).GetMethod("Awake", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(d.pista, null);

        d.carro.Posicionar(0);
        d.cenario.SetActive(false);
        cam.transform.position = d.carro.transform.position + new Vector3(0, 0.6f, -10);
        cam.orthographicSize = d.zoomMontagem;
        Rodar(d.canvas.DesenharContornos(0));
        Rodar(d.canvas.Preencher(0));
        Foto(cam, saida + "/main-montagem.png");

        d.canvas.MostrarGrade(false);
        d.cenario.SetActive(true);
        cam.transform.position = d.posGeral;
        cam.orthographicSize = d.zoomGeral;
        Rodar(d.pista.Revelar(0));
        d.carro.Posicionar(0.55f);
        Foto(cam, saida + "/main-ida.png");
    }

    public static void Foto(Camera cam, string caminho)
    {
        var rt = new RenderTexture(1920, 1080, 24);
        var antes = cam.targetTexture;
        cam.targetTexture = rt;
        Canvas.ForceUpdateCanvases();
        cam.Render();   // aquecimento: no 1º render após criar uma textura, o URP 2D em batchmode ainda usa a anterior
        cam.Render();
        RenderTexture.active = rt;
        var tex = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0);
        tex.Apply();
        System.IO.File.WriteAllBytes(caminho, tex.EncodeToPNG());
        cam.targetTexture = antes;
        RenderTexture.active = null;
        Object.DestroyImmediate(rt);
        Object.DestroyImmediate(tex);
    }
}
