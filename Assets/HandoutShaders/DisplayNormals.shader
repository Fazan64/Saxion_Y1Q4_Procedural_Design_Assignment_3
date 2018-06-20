Shader "Handout/DisplayNormals" {
	SubShader {
		Pass {
			CGPROGRAM
			#pragma vertex myVertexShader
			#pragma fragment myFragmentShader
			#define UNITY_SHADER_NO_UPGRADE 1

			struct vertexInput {
				float4 vertex : POSITION;
				float4 normal : NORMAL;
			};

			struct vertexToFragment {
				float4 vertex : SV_POSITION;
				float4 color : COLOR;
			};

			vertexToFragment myVertexShader (vertexInput v) {
				vertexToFragment o;
				// Transform the point to clip space:
				o.vertex = mul(UNITY_MATRIX_MVP,v.vertex);
				o.color = v.normal; // vectors, colors... it's all the same in CG!
				return o;
			}
			
			fixed4 myFragmentShader (vertexToFragment i) : SV_Target {
				return i.color;
			}
			ENDCG
		}
	}
}

