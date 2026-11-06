// F1: Phong próprio (ambiente + difusa·max(0,N·L) + especular·max(0,R·V)^brilho), com N vindo do normal map
// (gerado por Tools/gerar_f1.py) girado pela rotação da peça (_Rotacao, escrita pelo PhongF1).
// A luz vem das globais _DirSol/_CorSol/_CorAmbiente do CicloDia; sem elas (_DirSol.w == 0), usa uma luz padrão.
// Mesma fórmula de Sombreamento.Phong (testada em C#).
Shader "CarroCG/F1Phong"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        _NormalMap ("Normal map (linear)", 2D) = "bump" {}
        _Especular ("Especular", Range(0, 1)) = 0.5
        _Brilho ("Brilho", Range(1, 128)) = 32
        [HideInInspector] _Rotacao ("Rotação (cos, sin)", Vector) = (1, 0, 0, 0)
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
            TEXTURE2D(_NormalMap); SAMPLER(sampler_NormalMap);

            CBUFFER_START(UnityPerMaterial)
                half _Especular;
                half _Brilho;
                float4 _Rotacao;
            CBUFFER_END

            float4 _DirSol;      // xyz direção para a luz, w = 1 quando o CicloDia definiu
            half4 _CorSol;
            half4 _CorAmbiente;

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
                half4 base = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, s.uv) * s.cor;
                float3 nt = SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, s.uv).xyz * 2 - 1;
                float2 eixoX = float2(_Rotacao.x, _Rotacao.y), eixoY = float2(-_Rotacao.y, _Rotacao.x);
                float3 n = normalize(float3(nt.x * eixoX + nt.y * eixoY, nt.z));

                bool definida = _DirSol.w > 0.5;
                float3 l = definida ? normalize(_DirSol.xyz) : normalize(float3(0.4, 0.8, 0.6));
                half3 corSol = definida ? _CorSol.rgb : half3(1, 1, 1);
                half3 ambiente = definida ? _CorAmbiente.rgb : half3(0.42, 0.44, 0.5);

                float3 r = reflect(-l, n);                       // = 2(N·L)N - L
                float difusa = max(0, dot(n, l));
                float especular = pow(max(0, r.z), _Brilho) * _Especular;   // V = (0,0,1)
                half3 cor = base.rgb * (ambiente + corSol * difusa) + corSol * especular * base.a;
                return half4(cor, base.a);
            }
            ENDHLSL
        }
    }
}
