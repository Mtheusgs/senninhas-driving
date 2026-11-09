using UnityEngine;

// Fundo em camadas com profundidade z. Na perspectiva, cada camada é deslocada e escalada pelo fator d/(d+z)
// (Projecao.FatorParallax): longe anda menos e fica menor. Na paralela o fator é 1 e a profundidade some.
[DefaultExecutionOrder(100)]                     // depois da câmera
public class Parallax : MonoBehaviour
{
    [System.Serializable]
    public class Camada
    {
        public SpriteRenderer render;
        public float z;
        public float yBase;
        public Color cor = Color.white;
        public SpriteRenderer preenchimento;     // retângulo da mesma cor, da base da camada até embaixo da tela
        public bool naLua = true;
    }

    public Camera cam;
    public CicloDia ciclo;
    public float d = 30f;
    public bool perspectiva = true;
    public Camada[] camadas;

    void LateUpdate()
    {
        var c = cam.transform.position;
        float meiaLargura = cam.orthographicSize * cam.aspect;
        foreach (var k in camadas)
        {
            bool visivel = !(ciclo && ciclo.lunar && !k.naLua);
            k.render.enabled = visivel;
            if (k.preenchimento) k.preenchimento.enabled = visivel;
            if (!visivel) continue;
            float f = Projecao.FatorParallax(k.z, d, perspectiva);
            float rep = k.render.sprite.bounds.size.x;                         // uma repetição, escala 1
            int n = Mathf.CeilToInt((2.2f * meiaLargura / f / rep + 2) / 2) * 2; // número par: emenda fica no centro
            k.render.size = new Vector2(n * rep, k.render.sprite.bounds.size.y);
            k.render.transform.localScale = new Vector3(f, f, 1);              // escala da projeção
            float desloc = Mathf.Repeat(-c.x * f, rep * f);                     // âncora no mundo vista com fator f
            k.render.transform.position = new Vector3(c.x + desloc, c.y + (k.yBase - c.y) * f, 0);
            k.render.color = Cor(k.cor);
            if (k.preenchimento)
            {
                float baseCamada = k.render.transform.position.y - k.render.sprite.bounds.size.y / 2 * f + 0.05f;
                float baseTela = c.y - cam.orthographicSize - 2;
                var t = k.preenchimento.transform;
                t.position = new Vector3(k.render.transform.position.x, (baseCamada + baseTela) / 2, 0);
                t.localScale = new Vector3(n * rep * f, Mathf.Max(0.01f, baseCamada - baseTela), 1);
                k.preenchimento.color = k.render.color;
            }
        }
    }

    Color Cor(Color baseCor)
    {
        if (!ciclo) return baseCor;
        var e = ciclo.Atual;
        if (ciclo.lunar) { float g = baseCor.grayscale; baseCor = new Color(g, g, g * 1.1f); }
        var luz = e.corLuz * 0.5f + e.corAmbiente;
        return new Color(baseCor.r * luz.r, baseCor.g * luz.g, baseCor.b * luz.b, 1);
    }
}
