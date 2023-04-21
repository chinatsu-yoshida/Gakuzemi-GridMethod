Shader "GridMethod/Sample"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
	}
	SubShader
	{
		// No culling or depth
		Cull Off ZWrite Off ZTest Always

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				return o;
			}
			
			sampler2D _MainTex;
			sampler2D DensityTex;
			float densityCoef;

			fixed4 frag (v2f i) : SV_Target
			{
				// fixed3 solver = tex2D(SolverTex, i.uv); // 密度の値を取得するため
				fixed4 col    = tex2D(_MainTex, i.uv);
				fixed density = tex2D(DensityTex, i.uv);
				
				// 色の変更
				col *= float4(1 - density * 5, 1, 1, 1);

				if(density >= densityCoef){
					col = float4(1, 0, 0, 1);
				}else if(density >= densityCoef / 2){
					col = float4(density*2/densityCoef - 1, 2 - density*2/densityCoef, 0, 1);
				}else if(density >0){
					col = float4(0, density*2/densityCoef, 1 - density*2/densityCoef, 1);
					//col = density * col + (1-density) * float4(1, 1, 1, 1);
				}else{
					col = float4(1, 1, 1, 1);
				}
				//col.a = 1;
				return col;
			}
			ENDCG
		}
	}
}
