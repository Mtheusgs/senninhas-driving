using System.Collections;
using UnityEngine;

// Pista = uma Bézier cúbica. A estrada é desenhada aos poucos; as linhas-guia mostram o polígono de controle.
[RequireComponent(typeof(LineRenderer))]
public class BezierPath : MonoBehaviour
{
    public Transform p0, p1, p2, p3;
    public LineRenderer guias;
    public int resolucao = 100;

    LineRenderer estrada;

    public Vector2 Point(float t) =>
        Raster.BezierPoint(p0.position, p1.position, p2.position, p3.position, Mathf.Clamp01(t));

    public Vector2 Tangent(float t) =>
        Raster.BezierTangent(p0.position, p1.position, p2.position, p3.position, Mathf.Clamp01(t));

    void Awake()
    {
        estrada = GetComponent<LineRenderer>();
        estrada.positionCount = 0;
        MostrarGuias(false);
    }

    public IEnumerator Revelar(float duracao)
    {
        MostrarGuias(true);
        return Anim.Durante(duracao, k =>
        {
            int n = Mathf.RoundToInt(k * resolucao);
            estrada.positionCount = n + 1;
            for (int i = 0; i <= n; i++) estrada.SetPosition(i, Point(i / (float)resolucao));
        });
    }

    public void EsconderGuias() => MostrarGuias(false);

    void MostrarGuias(bool visivel)
    {
        guias.positionCount = visivel ? 4 : 0;
        if (visivel) guias.SetPositions(new[] { p0.position, p1.position, p2.position, p3.position });
        foreach (var p in new[] { p0, p1, p2, p3 })
            if (p.TryGetComponent(out SpriteRenderer r)) r.enabled = visivel;
    }

    void OnDrawGizmos()
    {
        if (!p0 || !p1 || !p2 || !p3) return;
        Gizmos.color = Color.yellow;
        for (int i = 0; i < 40; i++) Gizmos.DrawLine(Point(i / 40f), Point((i + 1) / 40f));
        Gizmos.color = Color.gray;
        Gizmos.DrawLine(p0.position, p1.position);
        Gizmos.DrawLine(p2.position, p3.position);
    }
}
