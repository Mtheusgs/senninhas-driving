using System.Collections.Generic;
using UnityEngine;

// Modelo 3D em wireframe do F1 para a intro (puro), nas mesmas proporções da arte (ContornoF1):
// chassi extrudado, asas como caixas e as 4 rodas como polígonos de 12 lados. Unidades, pivô no centro do sprite.
public static class ModeloF1
{
    public const float MeiaLarguraChassi = 0.35f, MeiaBitola = 0.45f;

    public static readonly Vector3[] Vertices;
    public static readonly Vector2Int[] Arestas;

    static ModeloF1()
    {
        var v = new List<Vector3>();
        var a = new List<Vector2Int>();
        // Um prisma: o contorno em +z e em -z, arestas em volta de cada face e ligando as duas (opcional).
        void Prisma(Vector2[] contorno, float z, bool ligar)
        {
            int n = contorno.Length, i0 = v.Count;
            foreach (var p in contorno) v.Add(new Vector3(ContornoF1.EmUnidades(p).x, ContornoF1.EmUnidades(p).y, z));
            foreach (var p in contorno) v.Add(new Vector3(ContornoF1.EmUnidades(p).x, ContornoF1.EmUnidades(p).y, -z));
            for (int k = 0; k < n; k++)
            {
                a.Add(new Vector2Int(i0 + k, i0 + (k + 1) % n));
                a.Add(new Vector2Int(i0 + n + k, i0 + n + (k + 1) % n));
                if (ligar) a.Add(new Vector2Int(i0 + k, i0 + n + k));
            }
        }
        Prisma(ContornoF1.Regioes[ContornoF1.Chassi], MeiaLarguraChassi, true);
        Prisma(ContornoF1.Regioes[ContornoF1.AsaTraseira], MeiaBitola, true);
        Prisma(ContornoF1.Regioes[ContornoF1.AsaDianteira], MeiaBitola, true);
        Prisma(ContornoF1.Regioes[ContornoF1.RodaTraseira], MeiaBitola, false);   // rodas: um anel de cada lado
        Prisma(ContornoF1.Regioes[ContornoF1.RodaDianteira], MeiaBitola, false);
        Vertices = v.ToArray();
        Arestas = a.ToArray();
    }

    // Rotação em torno de y (giro do carro) composta com rotação em torno de x (inclinação da câmera): Ry · Rx.
    public static Matrix4x4 Rotacao(float yGraus, float xGraus)
    {
        float cy = Mathf.Cos(yGraus * Mathf.Deg2Rad), sy = Mathf.Sin(yGraus * Mathf.Deg2Rad);
        float cx = Mathf.Cos(xGraus * Mathf.Deg2Rad), sx = Mathf.Sin(xGraus * Mathf.Deg2Rad);
        var ry = Matrix4x4.identity;
        ry.m00 = cy; ry.m02 = sy; ry.m20 = -sy; ry.m22 = cy;
        var rx = Matrix4x4.identity;
        rx.m11 = cx; rx.m12 = -sx; rx.m21 = sx; rx.m22 = cx;
        return ry * rx;
    }
}
