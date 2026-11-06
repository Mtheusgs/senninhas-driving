using UnityEngine;

// Passa a rotação de cada peça para o shader Phong: o normal map gira com o chassi e com cada roda.
public class PhongF1 : MonoBehaviour
{
    public SpriteRenderer[] pecas;

    static readonly int IdRotacao = Shader.PropertyToID("_Rotacao");
    MaterialPropertyBlock mpb;

    void LateUpdate()
    {
        mpb ??= new MaterialPropertyBlock();
        foreach (var r in pecas)
        {
            float a = r.transform.eulerAngles.z * Mathf.Deg2Rad;
            r.GetPropertyBlock(mpb);                       // mantém a textura do sprite
            mpb.SetVector(IdRotacao, new Vector4(Mathf.Cos(a), Mathf.Sin(a), 0, 0));
            r.SetPropertyBlock(mpb);
        }
    }
}
