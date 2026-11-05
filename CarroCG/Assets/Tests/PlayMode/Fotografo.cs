using UnityEngine;

// Render da câmera principal em PNG (capturas de verificação). Precisa de GPU.
public static class Fotografo
{
    public static string Pasta => System.Environment.GetEnvironmentVariable("CAPTURA_DIR");

    public static void Foto(string nome)
    {
        var cam = Camera.main;
        var rt = new RenderTexture(1920, 1080, 24);
        cam.targetTexture = rt;
        Canvas.ForceUpdateCanvases();
        cam.Render();   // aquecimento: o 1º render após criar texturas ainda usa as anteriores
        cam.Render();
        RenderTexture.active = rt;
        var tex = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0);
        tex.Apply();
        System.IO.File.WriteAllBytes(System.IO.Path.Combine(Pasta, nome), tex.EncodeToPNG());
        cam.targetTexture = null;
        RenderTexture.active = null;
        Object.Destroy(rt);
        Object.Destroy(tex);
    }
}
