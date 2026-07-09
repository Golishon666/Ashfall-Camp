Shader "AshfallCamp/UI/SpriteOutline"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1, 1, 1, 1)
        _OutlineColor ("Outline Color", Color) = (0, 0, 0, 1)
        _OutlineThickness ("Outline Thickness", Range(0, 256)) = 6
        _OutlineTextureSize ("Outline Texture Size", Float) = 1024
        _OutlineScale ("Outline Scale", Range(1, 1.4)) = 1.015
        _InnerOutlineStrength ("Inner Outline Strength", Range(0, 1)) = 0
        _AlphaThreshold ("Alpha Threshold", Range(0, 1)) = 0.12
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        CGINCLUDE
        #include "UnityCG.cginc"

        sampler2D _MainTex;
        fixed4 _Color;
        fixed4 _OutlineColor;
        float _OutlineThickness;
        float _OutlineTextureSize;
        float _OutlineScale;
        float _InnerOutlineStrength;
        float _AlphaThreshold;

        struct appdata_t
        {
            float4 vertex : POSITION;
            fixed4 color : COLOR;
            float2 texcoord : TEXCOORD0;
        };

        struct v2f
        {
            float4 vertex : SV_POSITION;
            fixed4 color : COLOR;
            float2 outlineUv : TEXCOORD0;
            float2 spriteUv : TEXCOORD1;
        };

        float GetOutlineExpandScale()
        {
            float pixelScale = 1.0 + (_OutlineThickness / max(1.0, _OutlineTextureSize)) * 2.0;
            return max(_OutlineScale, pixelScale);
        }

        v2f vert(appdata_t v)
        {
            v2f o;
            float expandScale = GetOutlineExpandScale();
            float border = (expandScale - 1.0) / max(0.0001, expandScale * 2.0);
            float denom = max(0.0001, 1.0 - border * 2.0);
            float4 expandedVertex = v.vertex;

            expandedVertex.xy *= expandScale;
            o.vertex = UnityObjectToClipPos(expandedVertex);
            o.outlineUv = v.texcoord;
            o.spriteUv = (v.texcoord - border) / denom;
            o.color = v.color * _Color;
            return o;
        }

        float IsInsideUv(float2 uv)
        {
            return step(0.0, uv.x) * step(0.0, uv.y) * step(uv.x, 1.0) * step(uv.y, 1.0);
        }

        float SampleAlpha(float2 uv, float2 direction, float radius)
        {
            return tex2D(_MainTex, uv + direction * radius).a;
        }

        float MaxOutlineAlpha(float2 uv, float radius)
        {
            float a = 0.0;

            [unroll] for (int ring = 1; ring <= 8; ring++)
            {
                float r = radius * (ring / 8.0);

                a = max(a, SampleAlpha(uv, float2( 1.0000,  0.0000), r));
                a = max(a, SampleAlpha(uv, float2( 0.9239,  0.3827), r));
                a = max(a, SampleAlpha(uv, float2( 0.7071,  0.7071), r));
                a = max(a, SampleAlpha(uv, float2( 0.3827,  0.9239), r));
                a = max(a, SampleAlpha(uv, float2( 0.0000,  1.0000), r));
                a = max(a, SampleAlpha(uv, float2(-0.3827,  0.9239), r));
                a = max(a, SampleAlpha(uv, float2(-0.7071,  0.7071), r));
                a = max(a, SampleAlpha(uv, float2(-0.9239,  0.3827), r));
                a = max(a, SampleAlpha(uv, float2(-1.0000,  0.0000), r));
                a = max(a, SampleAlpha(uv, float2(-0.9239, -0.3827), r));
                a = max(a, SampleAlpha(uv, float2(-0.7071, -0.7071), r));
                a = max(a, SampleAlpha(uv, float2(-0.3827, -0.9239), r));
                a = max(a, SampleAlpha(uv, float2( 0.0000, -1.0000), r));
                a = max(a, SampleAlpha(uv, float2( 0.3827, -0.9239), r));
                a = max(a, SampleAlpha(uv, float2( 0.7071, -0.7071), r));
                a = max(a, SampleAlpha(uv, float2( 0.9239, -0.3827), r));
            }

            return a;
        }

        float MinOutlineAlpha(float2 uv, float radius)
        {
            float a = 1.0;

            [unroll] for (int ring = 1; ring <= 4; ring++)
            {
                float r = radius * (ring / 4.0);

                a = min(a, SampleAlpha(uv, float2( 1.0000,  0.0000), r));
                a = min(a, SampleAlpha(uv, float2( 0.7071,  0.7071), r));
                a = min(a, SampleAlpha(uv, float2( 0.0000,  1.0000), r));
                a = min(a, SampleAlpha(uv, float2(-0.7071,  0.7071), r));
                a = min(a, SampleAlpha(uv, float2(-1.0000,  0.0000), r));
                a = min(a, SampleAlpha(uv, float2(-0.7071, -0.7071), r));
                a = min(a, SampleAlpha(uv, float2( 0.0000, -1.0000), r));
                a = min(a, SampleAlpha(uv, float2( 0.7071, -0.7071), r));
            }

            return a;
        }

        fixed4 frag(v2f i) : SV_Target
        {
            float outlineRadius = _OutlineThickness / max(1.0, _OutlineTextureSize);
            float spriteInside = IsInsideUv(i.spriteUv);
            fixed4 sprite = tex2D(_MainTex, saturate(i.spriteUv)) * i.color;

            sprite.a *= spriteInside;

            float outlineCenterAlpha = tex2D(_MainTex, i.outlineUv).a;
            float outlineNeighborAlpha = MaxOutlineAlpha(i.outlineUv, outlineRadius);
            float outlineAlpha = step(_AlphaThreshold, max(outlineCenterAlpha, outlineNeighborAlpha));
            float spriteMask = smoothstep(0.0, _AlphaThreshold, sprite.a);
            float innerEdgeAlpha = 1.0 - step(_AlphaThreshold, MinOutlineAlpha(i.spriteUv, outlineRadius * 0.45));
            float innerOutline = innerEdgeAlpha * spriteMask * _InnerOutlineStrength;

            fixed4 outline = _OutlineColor;
            outline.a *= outlineAlpha * (1.0 - spriteMask);

            fixed4 result;
            result.rgb = lerp(outline.rgb, sprite.rgb, sprite.a);
            result.rgb = lerp(result.rgb, _OutlineColor.rgb, innerOutline);
            result.a = max(sprite.a, outline.a);
            return result;
        }
        ENDCG

        Pass
        {
            Name "SpriteOutlineDefault"

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            ENDCG
        }

        Pass
        {
            Name "SpriteOutlineUniversal2D"
            Tags { "LightMode" = "Universal2D" }

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            ENDCG
        }
    }
}
