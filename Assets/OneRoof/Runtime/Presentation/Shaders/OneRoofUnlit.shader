Shader "OneRoof/Unlit"
{
    Properties
    {
        [MainTexture] _BaseMap("Texture", 2D) = "white" {}
        [MainColor] _BaseColor("Color", Color) = (1, 1, 1, 1)
        [PerRendererData] [HideInInspector] _MainTex("Texture", 2D) = "white" {}
        [HideInInspector] _Color("Color", Color) = (1, 1, 1, 1)
        _Cutoff("AlphaCutout", Range(0.0, 1.0)) = 0.5

        [HideInInspector] _SrcBlend("__src", Float) = 1.0
        [HideInInspector] _DstBlend("__dst", Float) = 0.0
        [HideInInspector] _ZWrite("__zw", Float) = 1.0
        [HideInInspector] _Cull("__cull", Float) = 0.0
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry"
        }

        Blend [_SrcBlend] [_DstBlend]
        ZWrite [_ZWrite]
        Cull [_Cull]

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        struct Attributes
        {
            float4 positionOS : POSITION;
            float2 uv : TEXCOORD0;
            float4 color : COLOR;
            UNITY_VERTEX_INPUT_INSTANCE_ID
        };

        struct Varyings
        {
            float4 positionCS : SV_POSITION;
            float2 uv : TEXCOORD0;
            float4 color : COLOR;
            UNITY_VERTEX_INPUT_INSTANCE_ID
            UNITY_VERTEX_OUTPUT_STEREO
        };

        TEXTURE2D(_BaseMap);
        SAMPLER(sampler_BaseMap);
        TEXTURE2D(_MainTex);
        SAMPLER(sampler_MainTex);

        UNITY_INSTANCING_BUFFER_START(UnityPerMaterial)
            UNITY_DEFINE_INSTANCED_PROP(float4, _BaseColor)
            UNITY_DEFINE_INSTANCED_PROP(float4, _Color)
            UNITY_DEFINE_INSTANCED_PROP(float4, _BaseMap_ST)
            UNITY_DEFINE_INSTANCED_PROP(float, _Cutoff)
            UNITY_DEFINE_INSTANCED_PROP(float, _SrcBlend)
            UNITY_DEFINE_INSTANCED_PROP(float, _DstBlend)
            UNITY_DEFINE_INSTANCED_PROP(float, _ZWrite)
            UNITY_DEFINE_INSTANCED_PROP(float, _Cull)
        UNITY_INSTANCING_BUFFER_END(UnityPerMaterial)

        Varyings vert(Attributes input)
        {
            Varyings output = (Varyings)0;
            UNITY_SETUP_INSTANCE_ID(input);
            UNITY_TRANSFER_INSTANCE_ID(input, output);
            UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

            VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
            output.positionCS = vertexInput.positionCS;

            float4 st = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _BaseMap_ST);
            float2 scale = (st.x != 0.0 || st.y != 0.0) ? st.xy : float2(1.0, 1.0);
            output.uv = input.uv * scale + st.zw;
            output.color = input.color;
            return output;
        }

        float4 frag(Varyings input) : SV_Target
        {
            UNITY_SETUP_INSTANCE_ID(input);

            float4 baseColor = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _BaseColor);
            float4 color = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _Color);
            float cutoff = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _Cutoff);

            float4 tint = (baseColor.a > 0.0 || any(baseColor.rgb > 0.0)) ? baseColor : color;

            float4 vertColor = (any(input.color.rgb > 0.0) || input.color.a > 0.0) ? input.color : float4(1.0, 1.0, 1.0, 1.0);

            float4 baseMap = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
            float4 mainTex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
            float4 texColor = baseMap * mainTex;

            float4 finalColor = texColor * tint * vertColor;

            #if defined(_ALPHATEST_ON)
            clip(finalColor.a - cutoff);
            #endif

            return finalColor;
        }
        ENDHLSL

        Pass
        {
            Name "SRPDefaultUnlit"
            Tags { "LightMode" = "SRPDefaultUnlit" }

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            ENDHLSL
        }
    }
    Fallback Off
}
