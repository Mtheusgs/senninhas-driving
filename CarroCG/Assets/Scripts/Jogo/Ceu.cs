using UnityEngine;

public struct EstadoCeu
{
    public Vector2 dirSol, dirLuz;
    public Color corLuz, corAmbiente, corCeuTopo, corCeuHorizonte;
    public float noite;
}

// Estado do céu pela hora do dia (puro). t ∈ [0,1): 0 nascer do sol (à direita), 0,25 meio-dia,
// 0,5 pôr do sol, 0,75 meia-noite. De noite a luz vem da lua (oposta ao sol); a direção é sempre de cima
// e muda de forma contínua.
public static class Ceu
{
    static readonly Color Branco = Color.white, Laranja = new(1f, 0.6f, 0.3f), Lua = new(0.32f, 0.38f, 0.55f);

    public static EstadoCeu Estado(float t)
    {
        float a = Mathf.Repeat(t, 1) * 2 * Mathf.PI;
        var sol = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
        float dia = Mathf.Clamp01(sol.y * 1.5f + 0.5f);
        float horizonte = 1 - Mathf.Clamp01(Mathf.Abs(sol.y) * 3);
        var dirSol = new Vector2(sol.x, Mathf.Max(sol.y, 0.5f)).normalized;
        var dirLua = new Vector2(-sol.x, Mathf.Max(-sol.y, 0.5f)).normalized;
        return new EstadoCeu
        {
            dirSol = sol,
            dirLuz = Vector2.Lerp(dirLua, dirSol, dia).normalized,
            corLuz = Color.Lerp(Lua, Color.Lerp(Branco, Laranja, horizonte), dia),
            corAmbiente = Color.Lerp(new Color(0.12f, 0.14f, 0.25f), new Color(0.42f, 0.44f, 0.5f), dia),
            corCeuTopo = Color.Lerp(new Color(0.02f, 0.03f, 0.08f), new Color(0.3f, 0.55f, 0.95f), dia),
            corCeuHorizonte = Color.Lerp(Color.Lerp(new Color(0.06f, 0.08f, 0.18f), new Color(0.98f, 0.55f, 0.3f), horizonte * 0.8f),
                                         Color.Lerp(new Color(0.62f, 0.82f, 1f), new Color(1f, 0.6f, 0.35f), horizonte), dia),
            noite = 1 - dia,
        };
    }
}
