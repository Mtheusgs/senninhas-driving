using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

// Gera o executável Linux em Build/ (fora de CarroCG). Menu: CarroCG → Gerar executável. Tools/gerar_executavel.sh.
public static class Construir
{
    [MenuItem("CarroCG/Gerar executável")]
    public static void Linux()
    {
        PlayerSettings.productName = "Senninhas Driving";
        PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;
        PlayerSettings.defaultScreenWidth = 1920;
        PlayerSettings.defaultScreenHeight = 1080;
        var cenas = new[] { "Assets/Scenes/Menu.unity", "Assets/Scenes/Jogo.unity" };     // o vídeo antigo fica de fora
        var r = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = cenas,
            locationPathName = "../Build/SenninhasDriving.x86_64",
            target = BuildTarget.StandaloneLinux64,
            options = BuildOptions.None,
        });
        Debug.Log($"Construir: {r.summary.result}, {r.summary.totalSize / 1e6:0.0} MB, erros {r.summary.totalErrors}");
        if (Application.isBatchMode) EditorApplication.Exit(r.summary.result == BuildResult.Succeeded ? 0 : 1);
    }
}
