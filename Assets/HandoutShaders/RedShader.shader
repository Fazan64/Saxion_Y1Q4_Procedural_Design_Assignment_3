Shader "Handout/Red" {
	SubShader {
		Pass {
			CGPROGRAM
			#pragma vertex myVertexShader
			#pragma fragment myFragmentShader
			#define UNITY_SHADER_NO_UPGRADE 1

			struct vertexInput {
				float4 vertex : POSITION;
			};

			struct vertexToFragment {
				float4 vertex : SV_POSITION;
			};

			vertexToFragment myVertexShader (vertexInput v) {
				vertexToFragment o;
				// Transform the point to clip space:
				o.vertex = mul(UNITY_MATRIX_MVP,v.vertex);
				return o;
			}
			
			fixed4 myFragmentShader (vertexToFragment i) : SV_Target {
				// return color red (1,0,0), with alpha=1:
				return float4(1,0,0,1);
			}
			ENDCG
		}
	}
}

