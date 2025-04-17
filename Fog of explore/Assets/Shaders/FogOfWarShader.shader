Shader "Custom/FogOfWarShader"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _MaskTex ("Fog Mask", 2D) = "black" {}
        _FogColor ("Fog Color", Color) = (0, 0, 0, 0.7)
    }

    SubShader
    {
        Tags {"Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane"}
        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma fragmentoption ARB_precision_hint_fastest
            #include "UnityCG.cginc"

            uniform sampler2D _MainTex;
            uniform sampler2D _MaskTex;
            uniform float4 _MainTex_ST;
            uniform float4 _MaskTex_ST;
            uniform float4 _FogColor;

            struct appdata
            {
                float4 vertex: POSITION;
                half4 color: COLOR;
                float2 texcoord: TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex: SV_POSITION;
                half4 color: COLOR;
                float2 texcoord: TEXCOORD0;
            };

            v2f vert(appdata input)
            {
                v2f output;
                output.vertex = UnityObjectToClipPos(input.vertex);
                output.color = input.color;
                output.texcoord = TRANSFORM_TEX(input.texcoord, _MainTex);
                return output;
            }

            fixed4 frag(v2f input) : SV_Target
            {
                // 获取主纹理颜色（地图精灵）
                fixed4 mainColor = tex2D(_MainTex, input.texcoord);
                
                // 获取蒙版纹理值（迷雾蒙版，白色表示已探索区域）
                fixed4 maskColor = tex2D(_MaskTex, input.texcoord);
                
                // 计算最终颜色
                // 在未探索区域使用带有迷雾颜色调整的原始纹理
                // 在已探索区域逐渐变为完全透明
                fixed4 finalColor = mainColor * _FogColor;
                finalColor.a = _FogColor.a * (1.0 - maskColor.r);
                
                return finalColor;
            }
            ENDCG
        }
    }
    Fallback "Sprites/Default"
} 