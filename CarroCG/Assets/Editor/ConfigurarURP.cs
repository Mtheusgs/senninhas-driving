using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// Liga o URP (2D Renderer) como pipeline padrão e em todos os níveis de qualidade. Menu: CarroCG → Configurar URP.
public static class ConfigurarURP
{
    [MenuItem("CarroCG/Configurar URP")]
    public static void Executar()
    {
        var urp = AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>("Assets/Settings/UniversalRP.asset");
        GraphicsSettings.defaultRenderPipeline = urp;
        int atual = QualitySettings.GetQualityLevel();
        for (int i = 0; i < QualitySettings.names.Length; i++)
        {
            QualitySettings.SetQualityLevel(i, false);
            QualitySettings.renderPipeline = urp;
        }
        QualitySettings.SetQualityLevel(atual, false);
        AssetDatabase.SaveAssets();
        Debug.Log("ConfigurarURP: pipeline = " + (GraphicsSettings.defaultRenderPipeline ? GraphicsSettings.defaultRenderPipeline.name : "nenhum"));
    }
}
