Shader "AshfallCamp/FogOfWarAnimated"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _FogTint ("Fog Color", Color) = (0.25,0.28,0.18,0.35)
        _Opacity ("Opacity", Range(0,1.5)) = 1
        _FlowSpeed ("Flow Speed", Range(0,1)) = 0.12
        _FlowScale ("Flow Scale", Range(0.5,8)) = 2.5
        _PulseStrength ("Pulse Strength", Range(0,1)) = 0.25
        _RadioactiveStrength ("Radioactive Strength", Range(0,2)) = 0
        _RadioactiveColor ("Radioactive Color", Color) = (0.45,1,0.05,1)
        [HideInInspector] _RendererColor ("RendererColor", Color) = (1,1,1,1)
        [HideInInspector] _Flip ("Flip", Vector) = (1,1,1,1)
        [PerRendererData] _AlphaTex ("External Alpha", 2D) = "white" {}
        [PerRendererData] _EnableExternalAlpha ("Enable External Alpha", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                float2 world : TEXCOORD1;
            };

            sampler2D _MainTex;
            fixed4 _Color;
            fixed4 _RendererColor;
            fixed4 _FogTint;
            float _Opacity;
            float _FlowSpeed;
            float _FlowScale;
            float _PulseStrength;
            float _RadioactiveStrength;
            fixed4 _RadioactiveColor;

            v2f vert(appdata_t input)
            {
                v2f output;
                float4 world = mul(unity_ObjectToWorld, input.vertex);
                output.vertex = UnityObjectToClipPos(input.vertex);
                output.texcoord = input.texcoord;
                output.world = world.xy;
                output.color = input.color * _Color * _RendererColor;
                return output;
            }

            fixed4 frag(v2f input) : SV_Target
            {
                fixed4 sprite = tex2D(_MainTex, input.texcoord) * input.color;
                float time = _Time.y * _FlowSpeed;
                float2 flow = input.world * (0.055 * _FlowScale);
                float waveA = sin(flow.x + flow.y * 0.72 + time * 2.1);
                float waveB = sin(flow.x * 1.43 - flow.y * 1.17 - time * 1.35 + 1.8);
                float waveC = sin(flow.x * 0.47 + flow.y * 1.82 + time * 0.72 + 3.2);
                float clouds = saturate(0.5 + waveA * 0.19 + waveB * 0.14 + waveC * 0.10);
                float breathing = 0.5 + 0.5 * sin(_Time.y * (_FlowSpeed * 2.4 + 0.16));
                float modulation = lerp(0.78, 1.16, clouds) * lerp(0.94, 1.06, breathing);
                sprite.rgb = lerp(sprite.rgb, _FogTint.rgb, clouds * _FogTint.a);
                sprite.rgb *= lerp(0.92, 1.08, clouds);
                float radioactiveVeins = smoothstep(0.68, 0.94, saturate(
                    0.5 + waveA * 0.34 - waveB * 0.23 + waveC * 0.17));
                float radioactivePulse = lerp(0.72, 1.18, breathing);
                float radioactiveCoverage = saturate(_RadioactiveStrength * 1.15);
                float3 radioactiveBase = _FogTint.rgb + _RadioactiveColor.rgb * 0.08;
                sprite.rgb = lerp(sprite.rgb, radioactiveBase, radioactiveCoverage);
                sprite.rgb += _RadioactiveColor.rgb * radioactiveVeins * radioactivePulse * _RadioactiveStrength * 0.58;
                sprite.a *= _Opacity * lerp(1.0, modulation, _PulseStrength);
                return sprite;
            }
            ENDCG
        }
    }
}
