Shader "MidnightLegacy/Stylized"
{
    // One shader for the whole look:
    //  - flat vertex colours (alpha channel = emissive amount in opaque mode)
    //  - cheap moon + ambient light
    //  - a procedural headlight cone (no real-time lights, no shadows)
    //  - fog (distance from the camera, or distance from the screen centre in the top-down view)
    // _Unlit = 1 gives a flat, alpha-blended mode used for shadows, smoke, rain and tyre marks.
    // _Additive = 1 (with _Unlit = 1) is for glows: light pools, headlight halos, nitro flames.
    // _Mask = 0 turns off the soft round mask that blended quads normally get from their UVs (tyre marks).
    Properties
    {
        _Tint ("Tint", Color) = (1,1,1,1)
        _Unlit ("Unlit / blended (0 or 1)", Float) = 0
        _Additive ("Additive glow (0 or 1)", Float) = 0
        _Mask ("Round mask (0 or 1)", Float) = 1
        [HideInInspector] _SrcBlend ("Src Blend", Float) = 1
        [HideInInspector] _DstBlend ("Dst Blend", Float) = 0
        [HideInInspector] _ZWrite ("ZWrite", Float) = 1
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        // own sRGB to linear (same curve URP used to ship), so it works in every URP version
        half3 MLSRGBToLinear(half3 c)
        {
            return c * (c * (c * 0.305306011h + 0.682171111h) + 0.012522878h);
        }

        CBUFFER_START(UnityPerMaterial)
            half4 _Tint;
            float _Unlit;
            float _Additive;
            float _Mask;
            float _SrcBlend;
            float _DstBlend;
            float _ZWrite;
        CBUFFER_END

        // Globals set by NightLighting.cs
        float4 _MLHeadPos;    // xyz = position, w = range
        float4 _MLHeadDir;    // xyz = direction, w = cosine of cone edge
        half4  _MLHeadColor;
        half4  _MLAmbient;
        half4  _MLMoonColor;
        float4 _MLMoonDir;    // xyz = direction TO the moon
        half4  _MLFogColor;
        float4 _MLFogParams;  // x = start, y = end
        float4 _MLView;       // x = 1 in the top-down view
        ENDHLSL

        Pass
        {
            Name "ForwardUnlit"
            Tags { "LightMode"="UniversalForward" }
            Blend [_SrcBlend] [_DstBlend]
            ZWrite [_ZWrite]
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                half4  color      : COLOR;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                half3  normalWS   : TEXCOORD1;
                half4  color      : COLOR;
                float2 uv         : TEXCOORD2;
            };

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionWS = TransformObjectToWorld(v.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(o.positionWS);
                o.normalWS   = TransformObjectToWorldNormal(v.normalOS);
                o.color      = v.color;
                o.uv         = v.uv;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                bool topDown = _MLView.x > 0.5;

                // top-down: fog is the distance from the screen centre on the ground plane (a soft vignette)
                float camDist = topDown
                    ? length(i.positionWS.xz - _WorldSpaceCameraPos.xz)
                    : distance(_WorldSpaceCameraPos, i.positionWS);
                half fogT = saturate((camDist - _MLFogParams.x) / max(0.01, _MLFogParams.y - _MLFogParams.x));

                half3 albedo = MLSRGBToLinear(i.color.rgb) * _Tint.rgb;

                if (_Unlit > 0.5)
                {
                    // Flat colour, vertex alpha is opacity, optional soft round mask from UVs.
                    half mask = _Mask > 0.5 ? saturate((1.0 - 2.0 * length(i.uv - 0.5)) * 2.5) : 1.0;
                    half a = i.color.a * _Tint.a * mask;
                    if (_Additive > 0.5)
                        return half4(albedo, a * (1.0 - fogT));
                    half3 c = lerp(albedo, _MLFogColor.rgb, fogT);
                    return half4(c, a);
                }

                half emis = i.color.a;
                float3 n = normalize(i.normalWS);

                half moon = saturate(dot(n, _MLMoonDir.xyz));
                half3 light = _MLAmbient.rgb + _MLMoonColor.rgb * moon;

                float3 toP = i.positionWS - _MLHeadPos.xyz;
                float dist = length(toP);
                float3 dir = toP / max(dist, 0.001);
                half cone = smoothstep(_MLHeadDir.w, _MLHeadDir.w + 0.12, dot(dir, _MLHeadDir.xyz));
                half fall = saturate(1.0 - dist / _MLHeadPos.w);
                fall *= fall;
                half facing = saturate(dot(n, -dir)) * 0.7 + 0.3;
                if (topDown) facing = 1.0;
                light += _MLHeadColor.rgb * (cone * fall * facing);

                half3 col = albedo * light + albedo * (emis * 2.0);
                col = lerp(col, _MLFogColor.rgb, fogT);
                return half4(col, 1.0);
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }
            ZWrite On
            ColorMask R
            Cull Back

            HLSLPROGRAM
            #pragma vertex DepthVert
            #pragma fragment DepthFrag

            float4 DepthVert(float4 positionOS : POSITION) : SV_POSITION
            {
                return TransformObjectToHClip(positionOS.xyz);
            }

            half4 DepthFrag(float4 positionCS : SV_POSITION) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }
    }
}
