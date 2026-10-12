Shader "MidnightLegacy/Sprite"
{
    // Textured, alpha blended quads for the painted art (cars, traffic, trees, rooftops, garage).
    //  - texture * vertex colour (vertex colour is the paint tint, vertex alpha is opacity)
    //  - _Light = 1 applies the night lighting: dim ambient plus the player's headlight cone
    //  - the same vignette fog as MidnightLegacy/Stylized, so sprites fade out at the screen edge
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Tint ("Tint", Color) = (1,1,1,1)
        _Light ("Night lighting (0 to 1)", Float) = 1
        [HideInInspector] _SrcBlend ("Src Blend", Float) = 5
        [HideInInspector] _DstBlend ("Dst Blend", Float) = 10
        [HideInInspector] _ZWrite ("ZWrite", Float) = 0
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" }

        Pass
        {
            Name "ForwardUnlit"
            Tags { "LightMode"="UniversalForward" }
            Blend [_SrcBlend] [_DstBlend]
            ZWrite [_ZWrite]
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            half3 MLSRGBToLinear(half3 c)
            {
                return c * (c * (c * 0.305306011h + 0.682171111h) + 0.012522878h);
            }

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half4 _Tint;
                float _Light;
                float _SrcBlend;
                float _DstBlend;
                float _ZWrite;
            CBUFFER_END

            // Globals set by NightLighting.cs
            float4 _MLHeadPos;
            float4 _MLHeadDir;
            half4  _MLHeadColor;
            half4  _MLAmbient;
            half4  _MLMoonColor;
            half4  _MLFogColor;
            float4 _MLFogParams;
            float4 _MLView;

            struct Attributes
            {
                float4 positionOS : POSITION;
                half4  color      : COLOR;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                half4  color      : COLOR;
                float2 uv         : TEXCOORD1;
            };

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionWS = TransformObjectToWorld(v.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(o.positionWS);
                o.color = v.color;
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
                half3 albedo = tex.rgb * MLSRGBToLinear(i.color.rgb) * _Tint.rgb;
                half alpha = tex.a * i.color.a * _Tint.a;

                bool topDown = _MLView.x > 0.5;
                float camDist = topDown
                    ? length(i.positionWS.xz - _WorldSpaceCameraPos.xz)
                    : distance(_WorldSpaceCameraPos, i.positionWS);
                half fogT = saturate((camDist - _MLFogParams.x) / max(0.01, _MLFogParams.y - _MLFogParams.x));

                // night light: a floor so painted art never goes black, a little ambient and moon, plus the headlight cone
                float3 toP = i.positionWS - _MLHeadPos.xyz;
                float dist = length(toP);
                float3 dir = toP / max(dist, 0.001);
                half cone = smoothstep(_MLHeadDir.w, _MLHeadDir.w + 0.12, dot(dir, _MLHeadDir.xyz));
                half fall = saturate(1.0 - dist / max(_MLHeadPos.w, 0.001));
                fall *= fall;
                half3 light = 0.78h + _MLAmbient.rgb * 0.35h + _MLMoonColor.rgb * 0.2h + _MLHeadColor.rgb * (cone * fall * 0.9h);
                light = min(light, 1.45h);
                albedo *= lerp(half3(1, 1, 1), light, _Light);

                albedo = lerp(albedo, _MLFogColor.rgb, fogT);
                return half4(albedo, alpha);
            }
            ENDHLSL
        }
    }
}
