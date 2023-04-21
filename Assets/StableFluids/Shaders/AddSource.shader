Shader "StableFluid/AddSource" {
	Properties {
		_Source ("Adding source", Vector) = (0, 1, 0.5, 0.5) //xy = velocity, zw = center pos
		_Radius ("Radius", Float) = 10
	}
	SubShader {
		Cull Off ZWrite Off ZTest Always

		Pass {
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			#include "UnityCG.cginc"

			struct appdata {
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f {
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};

			// マウスの移動距離(正規化)，マウス位置のuv座標
			float4 _Source;
			float _Radius;

			v2f vert (appdata v) {
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				return o;
			}
			
			float4 frag (v2f i) : SV_Target {
				// (i.uv - マウス位置のuv座標) / 半径
				// abs(dpdt) >= 1 -> saturate = 0;
				// abs(dpdt) <  1 -> 0 < saturate < =1
				float2 dpdt = (i.uv - _Source.zw) / _Radius;

				// saturate : clamp [0, 1]
				return float4(_Source.xy * saturate(1.0 - dot(dpdt, dpdt)), saturate(1.0 - dot(dpdt, dpdt)), 0);
			}
			ENDCG
		}
	}
}
