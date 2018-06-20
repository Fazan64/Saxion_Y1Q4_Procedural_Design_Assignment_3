Shader "Handout/VectorOperations" {
	Properties
	{
		_ColorFilter ("ColorFilter", Color) = (1,1,0,1)
		_HighColor ("HighColor", Color) = (0,1,1,1)
		_LowColor ("LowColor", Color) = (1,0,1,1)
	}

	SubShader {
		Pass {
			CGPROGRAM
			#pragma vertex myVertexShader
			#pragma fragment myFragmentShader
			#define UNITY_SHADER_NO_UPGRADE 1

			float4 _ColorFilter;
			float4 _HighColor;
			float4 _LowColor;

			struct vertexInput {
				float4 vertex : POSITION;
			};

			struct vertexToFragment {
				float4 vertex : SV_POSITION;
				float4 modelPosition : TEXCOORD0;
			};

			vertexToFragment myVertexShader (vertexInput v) {
				vertexToFragment o;
				// Transform the point to clip space:
				o.vertex = mul(UNITY_MATRIX_MVP,v.vertex);
				// pass the original model position:
				o.modelPosition = v.vertex;
				return o;
			}
			
			fixed4 myFragmentShader (vertexToFragment i) : SV_Target {
				float multiplier = i.modelPosition.y;	
				// linear interpolation (and extrapolation!), featuring scalar multiplication and (vector) addition:
				float4 output = _HighColor * multiplier + _LowColor * (1-multiplier); 
				// coordinate-wise multiplication!:
				output *= _ColorFilter;
				return output;	// cast down to fixed4 + GPU automatically clamps color components between 0 and 1!
			}
			ENDCG
		}
	}
}

