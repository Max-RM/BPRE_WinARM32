Shader "Hidden/SnapshotEffect"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_Saturation ("Saturation", Float) = 1
		_RampOffset ("Ramp Offset", Float) = 0
	}
	SubShader
	{
		Cull Off
		ZWrite Off
		ZTest Always

		Pass
		{
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment frag
			#include "UnityCG.cginc"

			sampler2D _MainTex;
			float _Saturation;
			float _RampOffset;

			fixed4 frag (v2f_img i) : SV_Target
			{
				fixed4 c = tex2D(_MainTex, i.uv);
				float g = dot(c.rgb, float3(0.299, 0.587, 0.114));
				float3 rgb = lerp(g.xxx, c.rgb, saturate(_Saturation));
				rgb = lerp(rgb, float3(1.0, 1.0, 1.0), saturate(_RampOffset));
				return fixed4(rgb, c.a);
			}
			ENDCG
		}
	}
	Fallback Off
}
