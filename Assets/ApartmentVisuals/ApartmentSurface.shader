Shader "GraduationProject/Apartment Surface"
{
    Properties
    {
        [MainTexture] _BaseMap("Existing surface texture", 2D) = "white" {}
        [MainColor] _BaseColor("Surface colour", Color) = (0.7,0.7,0.65,1)
        _Metallic("Metallic", Range(0,1)) = 0
        _Smoothness("Smoothness", Range(0,1)) = 0.25
        _WorldScale("Texture repeats per metre", Float) = 1
        _Wear("Dry accumulated wear", Range(0,1)) = 0.4
        _Relief("Relief in metres", Range(0,0.003)) = 0.0004
        _ObjectSpace("Move texture with object", Float) = 0
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "UniversalMaterialType"="Lit" }
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        CBUFFER_START(UnityPerMaterial)
        float4 _BaseMap_ST, _BaseColor;
        float _Metallic, _Smoothness, _WorldScale, _Wear, _Relief, _ObjectSpace;
        CBUFFER_END
        TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
        struct Attributes { float4 positionOS:POSITION; float3 normalOS:NORMAL; UNITY_VERTEX_INPUT_INSTANCE_ID };
        struct Varyings
        {
            float4 positionCS:SV_POSITION;
            float3 positionWS:TEXCOORD0;
            float3 normalWS:TEXCOORD1;
            float3 surfacePosition:TEXCOORD2;
            UNITY_VERTEX_INPUT_INSTANCE_ID
            UNITY_VERTEX_OUTPUT_STEREO
        };
        Varyings Vert(Attributes i)
        {
            Varyings o=(Varyings)0;
            UNITY_SETUP_INSTANCE_ID(i); UNITY_TRANSFER_INSTANCE_ID(i,o); UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
            o.positionWS=TransformObjectToWorld(i.positionOS.xyz);
            o.positionCS=TransformWorldToHClip(o.positionWS);
            o.normalWS=TransformObjectToWorldNormal(i.normalOS);
            // Rigid moving leaves keep their material attached; stationary walls share metre scale.
            o.surfacePosition=lerp(o.positionWS,mul((float3x3)unity_ObjectToWorld,i.positionOS.xyz),_ObjectSpace);
            return o;
        }
        float Hash(float2 p) { return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453); }
        float Noise(float2 p)
        {
            float2 a=floor(p),f=frac(p);f=f*f*(3-2*f);
            return lerp(lerp(Hash(a),Hash(a+float2(1,0)),f.x),lerp(Hash(a+float2(0,1)),Hash(a+1),f.x),f.y);
        }
        float2 SurfaceUV(float3 p,float3 n)
        {
            float3 a=abs(n);
            if(a.y>max(a.x,a.z)) return p.xz;
            return a.x>a.z?p.zy:p.xy;
        }
        half4 Frag(Varyings i):SV_Target
        {
            UNITY_SETUP_INSTANCE_ID(i); UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
            float3 n=normalize(i.normalWS);
            float2 uv=SurfaceUV(i.surfacePosition,n);
            half3 tex=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,uv*_WorldScale).rgb;
            float broad=Noise(uv*1.7+7.3),middle=Noise(uv*13.7);
            float detailVisibility=saturate(.35/max(length(fwidth(uv*170)),.001));
            float fine=lerp(.5,Noise(uv*170),detailVisibility);
            // Dry shoe/rubbing marks and settled dust concentrate low on walls, never wet puddles.
            float lower=1-smoothstep(.08,.85,i.positionWS.y);
            float streak=Noise(uv*float2(28,9));
            float rub=smoothstep(.56,.81,streak)*lower;
            float dirt=_Wear*(.12*broad+.4*lower*(.35+.65*middle)+.25*rub);
            half3 albedo=_BaseColor.rgb*tex*(.92+.13*broad+.045*fine);
            albedo=lerp(albedo,albedo*half3(.33,.30,.24),saturate(dirt));
            // Screen-space surface gradient provides small physical relief, filtered at distance.
            float relief=(fine*.6+middle*.4)*_Relief;
            float3 dx=ddx(i.positionWS),dy=ddy(i.positionWS);
            float3 r1=cross(dy,n),r2=cross(n,dx);
            float det=dot(dx,r1);
            float3 gradient=(ddx(relief)*r1+ddy(relief)*r2)/max(abs(det),1e-7)*sign(det);
            n=normalize(n-gradient);
            InputData data=(InputData)0;
            data.positionWS=i.positionWS;
            data.positionCS=i.positionCS;
            data.normalWS=n;
            data.viewDirectionWS=GetWorldSpaceNormalizeViewDir(i.positionWS);
            data.shadowCoord=TransformWorldToShadowCoord(i.positionWS);
            data.bakedGI=SampleSH(n);
            data.normalizedScreenSpaceUV=GetNormalizedScreenSpaceUV(i.positionCS);
            data.shadowMask=half4(1,1,1,1);
            SurfaceData surface=(SurfaceData)0;
            surface.albedo=albedo;surface.alpha=1;
            surface.metallic=_Metallic*(1-dirt*.55);
            surface.smoothness=saturate(_Smoothness*(.75+.35*middle)-dirt*.15);
            surface.normalTS=half3(0,0,1);surface.occlusion=1;
            half4 c=UniversalFragmentPBR(data,surface);
            c.rgb=MixFog(c.rgb,ComputeFogFactor(TransformWorldToHClip(i.positionWS).z));return c;
        }
        half4 Normals(Varyings i):SV_Target
        {
            float3 n=normalize(i.normalWS);
            #if defined(_GBUFFER_NORMALS_OCT)
            float2 oct=PackNormalOctQuadEncode(n);return half4(PackFloat2To888(saturate(oct*.5+.5)),0);
            #else
            return half4(n,0);
            #endif
        }
        half4 Depth(Varyings i):SV_Target { return 0; }
        float3 _LightDirection, _LightPosition;
        Varyings ShadowVert(Attributes i)
        {
            Varyings o=Vert(i);
            #if defined(_CASTING_PUNCTUAL_LIGHT_SHADOW)
            float3 lightDir=normalize(_LightPosition-o.positionWS);
            #else
            float3 lightDir=_LightDirection;
            #endif
            o.positionCS=TransformWorldToHClip(ApplyShadowBias(o.positionWS,o.normalWS,lightDir));
            o.positionCS=ApplyShadowClamping(o.positionCS);
            return o;
        }
        ENDHLSL
        Pass
        {
            Name "Forward" Tags { "LightMode"="UniversalForwardOnly" }
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
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
            Name "ShadowCaster" Tags { "LightMode"="ShadowCaster" } ColorMask 0
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex ShadowVert
            #pragma fragment Depth
            #pragma multi_compile_instancing
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            ENDHLSL
        }
        Pass
        {
            Name "DepthOnly" Tags { "LightMode"="DepthOnly" } ColorMask R
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Depth
            #pragma multi_compile_instancing
            ENDHLSL
        }
        Pass
        {
            Name "DepthNormals" Tags { "LightMode"="DepthNormalsOnly" }
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Normals
            #pragma multi_compile_instancing
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
            ENDHLSL
        }
    }
    Fallback "Hidden/Universal Render Pipeline/FallbackError"
}
