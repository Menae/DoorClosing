Shader "GraduationProject/Wet Floor"
{
    Properties
    {
        _BaseMap("床のテクスチャ", 2D) = "white" {}
        _BaseColor("床の色", Color) = (.72,.73,.72,1)
        _WorldScale("1mあたりの繰り返し", Float) = .8333333
        _Coverage("水たまりの広がり", Range(.2,.9)) = .65
        _Darkening("濡れた床の明るさ", Range(.2,1)) = .55
        _Smoothness("水面の滑らかさ", Range(.7,1)) = .97
        _Ripple("波紋の強さ", Range(0,.05)) = .008
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="TransparentCutout" "Queue"="AlphaTest" "UniversalMaterialType"="Lit" }
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        CBUFFER_START(UnityPerMaterial)
        float4 _BaseMap_ST, _BaseColor;
        float _WorldScale, _Coverage, _Darkening, _Smoothness, _Ripple;
        CBUFFER_END
        TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
        struct Attributes { float4 positionOS:POSITION; float3 normalOS:NORMAL; };
        struct Varyings { float4 positionCS:SV_POSITION; float3 positionWS:TEXCOORD0; float3 normalWS:TEXCOORD1; };
        Varyings Vert(Attributes i)
        {
            Varyings o; o.positionWS=TransformObjectToWorld(i.positionOS.xyz);
            o.positionCS=TransformWorldToHClip(o.positionWS); o.normalWS=TransformObjectToWorldNormal(i.normalOS); return o;
        }
        float Hash(float2 p) { return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453); }
        float Noise(float2 p)
        {
            float2 a=floor(p),f=frac(p); f=f*f*(3-2*f);
            return lerp(lerp(Hash(a),Hash(a+float2(1,0)),f.x),lerp(Hash(a+float2(0,1)),Hash(a+1),f.x),f.y);
        }
        void Mask(float3 p)
        {
            float broad=Noise(p.xz*.95+float2(3.1,7.7));
            float detail=Noise(p.xz*4.3)*.13;
            float margin=1-smoothstep(1.18,1.55,abs(p.x+.16*sin(p.z*1.2)));
            clip((broad+detail-(1-_Coverage))*margin-.012);
        }
        half4 Frag(Varyings i):SV_Target
        {
            Mask(i.positionWS);
            float2 uv=i.positionWS.xz;
            float3 n=normalize(i.normalWS+float3(sin(uv.y*13+uv.x*7+_Time.y*.8),0,cos(uv.x*11-uv.y*8+_Time.y*.6))*_Ripple);
            InputData data=(InputData)0;
            data.positionWS=i.positionWS; data.positionCS=i.positionCS; data.normalWS=n;
            data.viewDirectionWS=GetWorldSpaceNormalizeViewDir(i.positionWS);
            data.shadowCoord=TransformWorldToShadowCoord(i.positionWS);
            data.bakedGI=SampleSH(n); data.normalizedScreenSpaceUV=GetNormalizedScreenSpaceUV(i.positionCS);
            data.shadowMask=half4(1,1,1,1);
            SurfaceData surface=(SurfaceData)0;
            surface.albedo=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,uv*_WorldScale).rgb*_BaseColor.rgb*_Darkening;
            surface.alpha=1; surface.metallic=0; surface.smoothness=_Smoothness; surface.occlusion=1; surface.normalTS=half3(0,0,1);
            half4 c=UniversalFragmentPBR(data,surface);
            c.rgb=MixFog(c.rgb,ComputeFogFactor(TransformWorldToHClip(i.positionWS).z)); return c;
        }
        half4 Depth(Varyings i):SV_Target { Mask(i.positionWS); return 0; }
        half4 Normals(Varyings i):SV_Target
        {
            Mask(i.positionWS); float3 n=normalize(i.normalWS);
            #if defined(_GBUFFER_NORMALS_OCT)
            return half4(PackFloat2To888(saturate(PackNormalOctQuadEncode(n)*.5+.5)),0);
            #else
            return half4(n,0);
            #endif
        }
        ENDHLSL
        Pass
        {
            Name "Forward" Tags { "LightMode"="UniversalForwardOnly" }
            HLSLPROGRAM
            #pragma target 3.5
            #pragma editor_sync_compilation
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BOX_PROJECTION
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_ATLAS
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile_fog
            ENDHLSL
        }
        Pass
        {
            Name "DepthOnly" Tags { "LightMode"="DepthOnly" } ColorMask R
            HLSLPROGRAM
            #pragma target 3.5
            #pragma editor_sync_compilation
            #pragma vertex Vert
            #pragma fragment Depth
            ENDHLSL
        }
        Pass
        {
            Name "DepthNormals" Tags { "LightMode"="DepthNormalsOnly" }
            HLSLPROGRAM
            #pragma target 3.5
            #pragma editor_sync_compilation
            #pragma vertex Vert
            #pragma fragment Normals
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
            ENDHLSL
        }
    }
}
