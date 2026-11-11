using UnityEngine;

public enum Modo { Terra, Lua }

// Modo escolhido no menu; sobrevive à troca de cena (campo estático).
public static class Sessao
{
    public static Modo Modo = Modo.Terra;
    public static bool IntroVista;          // a intro passa uma vez por sessão (R e voltar do menu não repetem)
}

// Escolhas salvas do jogador. Os testes trocam o Prefixo para nunca mexer nas chaves reais.
public static class Preferencias
{
    public static string Prefixo = "";

    public static readonly float[] PpuTerreno = { 64, 48, 32 };
    public static readonly string[] NomesQualidade = { "Alta", "Média", "Baixa" };

    static readonly string[] Chaves =
    {
        "cor", "qualidade", "telaCheia", "modoAula",
        "recorde.Terra", "recorde.Lua", "moedasRecorde.Terra", "moedasRecorde.Lua",
    };

    static string K(string chave) => Prefixo + chave;

    static void SalvarInt(string chave, int valor)
    {
        PlayerPrefs.SetInt(K(chave), valor);
        PlayerPrefs.Save();
    }

    public static int Cor
    {
        get => PlayerPrefs.GetInt(K("cor"), 0);
        set => SalvarInt("cor", value);
    }

    public static int Qualidade
    {
        get => Mathf.Clamp(PlayerPrefs.GetInt(K("qualidade"), 0), 0, PpuTerreno.Length - 1);
        set => SalvarInt("qualidade", value);
    }

    public static bool TelaCheia
    {
        get => PlayerPrefs.GetInt(K("telaCheia"), 1) == 1;
        set => SalvarInt("telaCheia", value ? 1 : 0);
    }

    public static bool ModoAula
    {
        get => PlayerPrefs.GetInt(K("modoAula"), 0) == 1;
        set => SalvarInt("modoAula", value ? 1 : 0);
    }

    public static float Recorde(Modo m) => PlayerPrefs.GetFloat(K("recorde." + m), 0);
    public static int MoedasDoRecorde(Modo m) => PlayerPrefs.GetInt(K("moedasRecorde." + m), 0);

    // Guarda a partida se for a melhor do modo. Retorna true quando é recorde novo.
    public static bool RegistrarPartida(Modo m, float distancia, int moedas)
    {
        if (distancia <= Recorde(m)) return false;
        PlayerPrefs.SetFloat(K("recorde." + m), distancia);
        PlayerPrefs.SetInt(K("moedasRecorde." + m), moedas);
        PlayerPrefs.Save();
        return true;
    }

    public static void ZerarRecordes()
    {
        foreach (Modo m in System.Enum.GetValues(typeof(Modo)))
        {
            PlayerPrefs.DeleteKey(K("recorde." + m));
            PlayerPrefs.DeleteKey(K("moedasRecorde." + m));
        }
        PlayerPrefs.Save();
    }

    public static void ApagarTudo()
    {
        foreach (var c in Chaves) PlayerPrefs.DeleteKey(K(c));
        PlayerPrefs.Save();
    }
}
