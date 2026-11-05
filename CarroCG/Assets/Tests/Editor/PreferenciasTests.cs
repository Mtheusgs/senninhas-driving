using NUnit.Framework;
using UnityEngine;

public class PreferenciasTests
{
    [SetUp] public void Isolar() => Preferencias.Prefixo = "teste.";

    [TearDown]
    public void Limpar()
    {
        Preferencias.ApagarTudo();
        Preferencias.Prefixo = "";
    }

    [Test]
    public void RegistrarPartida_GuardaSoOMelhor()
    {
        Assert.IsTrue(Preferencias.RegistrarPartida(Modo.Terra, 100, 5));
        Assert.IsFalse(Preferencias.RegistrarPartida(Modo.Terra, 50, 9));
        Assert.AreEqual(100f, Preferencias.Recorde(Modo.Terra));
        Assert.AreEqual(5, Preferencias.MoedasDoRecorde(Modo.Terra));
    }

    [Test]
    public void Recorde_SeparadoPorModo()
    {
        Preferencias.RegistrarPartida(Modo.Terra, 100, 5);
        Assert.AreEqual(0f, Preferencias.Recorde(Modo.Lua));
    }

    [Test]
    public void ZerarRecordes_ApagaOsDoisModos()
    {
        Preferencias.RegistrarPartida(Modo.Terra, 100, 5);
        Preferencias.RegistrarPartida(Modo.Lua, 70, 2);
        Preferencias.ZerarRecordes();
        Assert.AreEqual(0f, Preferencias.Recorde(Modo.Terra));
        Assert.AreEqual(0, Preferencias.MoedasDoRecorde(Modo.Lua));
    }

    [Test]
    public void Prefixo_NaoMexeNasChavesReais()
    {
        bool tinha = PlayerPrefs.HasKey("recorde.Terra");
        float antes = PlayerPrefs.GetFloat("recorde.Terra");
        Preferencias.RegistrarPartida(Modo.Terra, 12345, 1);
        Preferencias.Cor = 3;
        Assert.AreEqual(tinha, PlayerPrefs.HasKey("recorde.Terra"));
        Assert.AreEqual(antes, PlayerPrefs.GetFloat("recorde.Terra"));
        Assert.AreEqual(3, PlayerPrefs.GetInt("teste.cor"));
    }

    [Test]
    public void Padroes_SemNadaSalvo()
    {
        Assert.AreEqual(0, Preferencias.Cor);
        Assert.AreEqual(0, Preferencias.Qualidade);
        Assert.IsTrue(Preferencias.TelaCheia);
        Assert.IsFalse(Preferencias.ModoAula);
    }

    [Test]
    public void Qualidade_ForaDaFaixa_Clampa()
    {
        PlayerPrefs.SetInt("teste.qualidade", 9);
        Assert.AreEqual(2, Preferencias.Qualidade);
    }

    [Test]
    public void Garagem_CorForaDaFaixa_UsaAPrimeira()
    {
        Preferencias.Cor = 99;
        Assert.AreEqual(0, Garagem.IndiceEscolhido);
        Assert.AreEqual(Garagem.Cores[0], Garagem.CorEscolhida);
    }

    [Test]
    public void Garagem_OitoCoresDiferentesComNome()
    {
        Assert.AreEqual(8, Garagem.Cores.Length);
        Assert.AreEqual(Garagem.Cores.Length, Garagem.Nomes.Length);
        CollectionAssert.AllItemsAreUnique(Garagem.Cores);
    }
}
