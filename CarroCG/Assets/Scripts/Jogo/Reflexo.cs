using System.Collections.Generic;
using UnityEngine;

// Reflexo do F1 no lago: a cada frame, o quad de cada peça é espelhado na linha da água e recortado
// (Sutherland–Hodgman com UV) contra o retângulo da água. Fora do lago, as malhas ficam vazias.
[DefaultExecutionOrder(110)]
public class Reflexo : MonoBehaviour
{
    public Terreno terreno;
    public SpriteRenderer[] pecas;
    public Material material;                  // lit: escurece à noite com a luz global
    public Color tinta = new(0.6f, 0.8f, 1f, 0.45f);
    public int ordem = 0;                      // acima da água (-2) e dos pilares (-1)

    public Mesh[] Malhas { get; private set; }

    readonly List<Vector2> verts = new(), uvs = new();
    readonly List<Vector3> v3 = new();
    readonly List<int> tris = new();
    readonly List<Color> cores = new();
    readonly Vector2[] cantos = new Vector2[4], cantosUv = new Vector2[4];

    void Start()
    {
        Malhas = new Mesh[pecas.Length];
        for (int i = 0; i < pecas.Length; i++)
        {
            var go = new GameObject("Reflexo " + pecas[i].name);
            go.transform.SetParent(transform, false);
            Malhas[i] = new Mesh { name = go.name };
            Malhas[i].MarkDynamic();
            go.AddComponent<MeshFilter>().sharedMesh = Malhas[i];
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = new Material(material) { mainTexture = pecas[i].sprite.texture };
            r.sortingOrder = ordem;
        }
    }

    void LateUpdate()
    {
        for (int i = 0; i < pecas.Length; i++) Atualizar(pecas[i], Malhas[i]);
    }

    void Atualizar(SpriteRenderer peca, Mesh malha)
    {
        malha.Clear();
        var agua = AguaSob(peca.transform.position.x);
        if (agua == null || !peca.enabled) return;

        // cantos do sprite (no espaço local) → mundo; UV pelo retângulo do sprite na textura
        var s = peca.sprite;
        Bounds b = s.bounds;
        Rect tr = s.textureRect;
        float tw = s.texture.width, th = s.texture.height;
        var loc = new[] { new Vector2(b.min.x, b.min.y), new(b.max.x, b.min.y), new(b.max.x, b.max.y), new(b.min.x, b.max.y) };
        var uv = new[] { new Vector2(tr.xMin / tw, tr.yMin / th), new(tr.xMax / tw, tr.yMin / th),
                         new(tr.xMax / tw, tr.yMax / th), new(tr.xMin / tw, tr.yMax / th) };
        for (int k = 0; k < 4; k++)
        {
            cantos[k] = peca.transform.TransformPoint(loc[k]);
            cantosUv[k] = uv[k];
        }
        if (!ReflexoGeometria.Quad(cantos, cantosUv, agua.Value.yMax, agua.Value, verts, uvs)) return;

        v3.Clear(); tris.Clear(); cores.Clear();
        var cor = peca.color * tinta;
        foreach (var p in verts)
        {
            v3.Add(transform.InverseTransformPoint(p));
            cores.Add(cor);
        }
        for (int k = 1; k + 1 < verts.Count; k++) { tris.Add(0); tris.Add(k); tris.Add(k + 1); }   // leque (polígono convexo)
        malha.SetVertices(v3);
        malha.SetUVs(0, uvs);
        malha.SetColors(cores);
        malha.SetTriangles(tris, 0);
    }

    Rect? AguaSob(float x)
    {
        foreach (var c in terreno.chunks)
            if (c.agua is Rect a && x >= a.xMin && x <= a.xMax) return a;
        return null;
    }
}
