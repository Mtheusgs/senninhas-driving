using System.Collections.Generic;
using UnityEngine;

public enum TipoColetavel { Moeda, Galao }

// Onde ficam moedas e galões em cada trecho (puro). Posição = ponto da curva + normal · altura (translação).
public class PlanoColetaveis
{
    int proximoGalao = 2, intervalo = 3;

    public List<(Vector2 pos, TipoColetavel tipo)> Para(Trecho t, int indice)
    {
        var lista = new List<(Vector2, TipoColetavel)>();
        if (indice == 0) return lista;
        for (int k = 0; k < 5; k++) lista.Add((Acima(t, 0.3f + 0.05f * k, 1f), TipoColetavel.Moeda));
        if (indice == proximoGalao)
        {
            lista.Add((Acima(t, 0.8f, 1.2f), TipoColetavel.Galao));
            proximoGalao += intervalo;
            intervalo++;
        }
        return lista;
    }

    static Vector2 Acima(Trecho t, float u, float altura) =>
        Raster.BezierPoint(t.p0, t.p1, t.p2, t.p3, u) + TerrenoGerador.Normal(t, u) * altura;
}

public class Coletavel : MonoBehaviour
{
    public TipoColetavel tipo;
    public static event System.Action<TipoColetavel> Coletado;
    public static event System.Action<TipoColetavel, Vector2> ColetadoEm;     // efeitos: onde foi pego

    public static void NotificarParaTeste(TipoColetavel t, Vector2 onde = default)
    {
        Coletado?.Invoke(t);
        ColetadoEm?.Invoke(t, onde);
    }
    bool pego;

    void Update()
    {
        if (tipo != TipoColetavel.Moeda) return;
        var s = transform.localScale;               // moeda girando: escala x = cos(θ)
        s.x = Mathf.Cos(Time.time * 4f + transform.position.x);
        transform.localScale = s;
    }

    void OnTriggerEnter2D(Collider2D outro)
    {
        if (pego || !outro.CompareTag("Player")) return;   // roda e chassi podem tocar no mesmo passo
        pego = true;
        Coletado?.Invoke(tipo);
        ColetadoEm?.Invoke(tipo, transform.position);
        Destroy(gameObject);
    }
}

// Cria os coletáveis de cada trecho novo, como filhos do trecho (somem junto com ele).
public class Coletaveis : MonoBehaviour
{
    public Terreno terreno;
    public Sprite moeda, galao;
    public float raio = 0.3f;

    readonly PlanoColetaveis plano = new();

    void OnEnable() => terreno.TrechoCriado += Popular;
    void OnDisable() => terreno.TrechoCriado -= Popular;

    void Popular(Terreno.Chunk c)
    {
        foreach (var (pos, tipo) in plano.Para(c.trecho, c.indice))
        {
            var go = new GameObject(tipo.ToString());
            go.transform.SetParent(c.go.transform, false);
            go.transform.position = pos;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = tipo == TipoColetavel.Moeda ? moeda : galao;
            sr.sortingOrder = 1;
            var col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = raio;
            go.AddComponent<Coletavel>().tipo = tipo;
        }
    }
}
