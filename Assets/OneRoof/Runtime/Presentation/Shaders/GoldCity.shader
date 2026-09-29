Shader "OneRoof/Gold City"
{
    Properties
    {
        _Night("Night", Range(0,1)) = 0
        _Weather("Weather", Range(0,1)) = 0
        _Clock("Animation clock", Float) = 0
        _Sky("Sky mode", Float) = 0
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }
        Cull Off
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct A { float4 p:POSITION; float4 c:COLOR; float2 uv:TEXCOORD0; };
            struct V { float4 p:SV_POSITION; float4 c:COLOR; float2 uv:TEXCOORD0; float2 world:TEXCOORD1; };
            CBUFFER_START(UnityPerMaterial)
            float _Night, _Weather, _Clock, _Sky;
            CBUFFER_END
            V vert(A i) { V o; o.p=TransformObjectToHClip(i.p.xyz); o.c=i.c; o.uv=i.uv; o.world=TransformObjectToWorld(i.p.xyz).xy; return o; }
            float hash(float2 p) { return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453); }
            half4 frag(V i):SV_Target
            {
                if (_Sky > .5)
                {
                    float h=saturate((i.world.y+4)/90);
                    float3 day=lerp(float3(.74,.64,.53),float3(.12,.30,.43),pow(h,.6));
                    float3 night=lerp(float3(.16,.22,.29),float3(.025,.055,.12),h);
                    float3 col=lerp(day,night,_Night);
                    float2 cell=floor(i.world*1.7);
                    float stars=step(.985,hash(cell))*pow(saturate(1-length(frac(i.world*1.7)-.5)*2),12);
                    col+=stars*_Night*(1-_Weather)*float3(.8,.86,1);
                    float sun=length((i.world-float2(-29,35))/float2(5,5));
                    col+=exp(-sun*sun*.45)*float3(.28,.16,.06)*(1-_Night)*(1-_Weather);
                    col=lerp(col,col*float3(.63,.72,.81),_Weather*.6);
                    return half4(col,1);
                }
                // Vertex alpha encodes lit glass. Geometry itself remains opaque and batches by mesh.
                float glow=1-i.c.a;
                float3 base=lerp(i.c.rgb,i.c.rgb*float3(.28,.38,.58),_Night*(1-glow));
                base=lerp(base,base*float3(.69,.78,.88),_Weather*.4*(1-glow));
                base+=glow*_Night*float3(.36,.17,.035)*( .92+.08*sin(_Clock*.35+i.world.x));
                return half4(base,1);
            }
            ENDHLSL
        }
    }
}
