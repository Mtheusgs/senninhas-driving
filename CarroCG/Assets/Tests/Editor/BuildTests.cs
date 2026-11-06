using System.IO;
using NUnit.Framework;
using UnityEditor;

public class BuildTests
{
    [Test]
    public void CenasDoBuild_ExistemComGuid()
    {
        var cenas = EditorBuildSettings.scenes;
        CollectionAssert.AreEqual(new[] { "Assets/Scenes/Menu.unity", "Assets/Scenes/Jogo.unity", "Assets/Scenes/Main.unity" },
                                  System.Array.ConvertAll(cenas, c => c.path));
        foreach (var c in cenas)
        {
            Assert.IsTrue(File.Exists(c.path), $"{c.path} não existe");
            Assert.IsFalse(c.guid.Empty(), $"{c.path} está no build sem GUID");
        }
        // o arquivo salvo é o que vale para a janela Build Settings e para um build
        StringAssert.DoesNotContain("guid: 00000000000000000000000000000000",
                                    File.ReadAllText("ProjectSettings/EditorBuildSettings.asset"));
    }
}
