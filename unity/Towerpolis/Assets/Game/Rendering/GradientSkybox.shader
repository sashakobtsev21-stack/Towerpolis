// A simple 3-stop gradient skybox (top / horizon / bottom) — replaces the muddy default sky with a
// clean stylized gradient. Rendered by URP via RenderSettings.skybox. Set up in code by LookDev.cs.
Shader "Towerpolis/GradientSkybox"
{
    Properties
    {
        _TopColor ("Top", Color) = (0.27, 0.50, 0.85, 1)
        _HorizonColor ("Horizon", Color) = (0.86, 0.91, 0.97, 1)
        _BottomColor ("Bottom", Color) = (0.55, 0.56, 0.60, 1)
        _Exponent ("Top Falloff", Range(0.2, 4)) = 1.3
        _HorizonSharp ("Bottom Falloff", Range(0.2, 4)) = 1.0
    }
    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
        Cull Off ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata { float4 vertex : POSITION; };
            struct v2f { float4 pos : SV_POSITION; float3 dir : TEXCOORD0; };

            half4 _TopColor;
            half4 _HorizonColor;
            half4 _BottomColor;
            float _Exponent;
            float _HorizonSharp;

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.dir = v.vertex.xyz;
                return o;
            }

            half4 frag (v2f i) : SV_Target
            {
                float y = normalize(i.dir).y;
                float up = pow(saturate(y), _Exponent);
                float down = pow(saturate(-y), _HorizonSharp);
                half3 col = lerp(_HorizonColor.rgb, _TopColor.rgb, up);
                col = lerp(col, _BottomColor.rgb, down);
                return half4(col, 1);
            }
            ENDCG
        }
    }
}
