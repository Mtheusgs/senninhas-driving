using UnityEngine;

// Projeções (puro): matrizes 4×4 em coordenadas homogêneas. O observador fica em z = -d olhando para +z
// e o plano de projeção é z = 0.
public static class Projecao
{
    // Paralela (ortográfica): descarta z.
    public static Matrix4x4 Ortografica()
    {
        var m = Matrix4x4.identity;
        m.m22 = 0;
        return m;
    }

    // Perspectiva: w = (z + d)/d, então x' = x·d/(d+z) e y' = y·d/(d+z) depois da divisão homogênea.
    public static Matrix4x4 Perspectiva(float d)
    {
        var m = Matrix4x4.identity;
        m.m22 = 0;
        m.m32 = 1 / d;
        m.m33 = 1;
        return m;
    }

    public static Vector2 Projetar(Matrix4x4 m, Vector3 v)
    {
        var h = m * new Vector4(v.x, v.y, v.z, 1);
        float w = h.w < 1e-4f ? 1e-4f : h.w;               // no/atrás do observador: não divide por ~0
        return new Vector2(h.x / w, h.y / w);
    }

    public static Matrix4x4 Interpolar(Matrix4x4 a, Matrix4x4 b, float k)
    {
        var m = new Matrix4x4();
        for (int i = 0; i < 16; i++) m[i] = Mathf.Lerp(a[i], b[i], k);
        return m;
    }

    // Fator de escala/deslocamento de uma camada na profundidade z: d/(d+z) na perspectiva, 1 na paralela.
    public static float FatorParallax(float z, float d, bool perspectiva) => perspectiva ? d / (d + z) : 1f;
}
