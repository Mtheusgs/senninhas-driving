// Terreno: a cor do preenchimento (scanline) multiplicada pela luz por coluna calculada em C#
// (Sombreamento.LuzPorColuna: Lambert Flat/Gouraud/Phong). O shader não calcula luz nenhuma.
Shader "CarroCG/TerrenoLuz"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        [PerRendererData] _Luz ("Luz por coluna", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" }
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            Tags { "LightMode" = "Universal2D" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Entrada { float4 pos : POSITION; float4 cor : COLOR; float2 uv : TEXCOORD0; };
            struct Saida { float4 pos : SV_POSITION; half4 cor : COLOR; float2 uv : TEXCOORD0; };

            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            TEXTURE2D(_Luz); SAMPLER(sampler_Luz);

            Saida vert(Entrada e)
            {
                Saida s;
                s.pos = TransformObjectToHClip(e.pos.xyz);
                s.cor = e.cor;
                s.uv = e.uv;
                return s;
            }

            half4 frag(Saida s) : SV_Target
            {
                half4 c = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, s.uv) * s.cor;
                half3 luz = SAMPLE_TEXTURE2D(_Luz, sampler_Luz, float2(s.uv.x, 0.5)).rgb;
                return half4(c.rgb * luz, c.a);
            }
            ENDHLSL
        }
    }
}
