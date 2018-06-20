Shader "Handout/BreatheShader"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_TimeScale ("TimeScale", float) = 10 
		_Strength ("Strength", float) = 0.5
	}
	SubShader
	{
		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#define UNITY_SHADER_NO_UPGRADE 1

			struct appdata
			{
				float4 vertex : POSITION;
				float4 normal : NORMAL;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};

			sampler2D _MainTex;
			float _TimeScale;
			float _Strength;

			v2f vert (appdata v)
			{
				v2f o;
				// Add/subtract a fraction of the vertex normal to its position:
				float4 modifiedPosition = v.vertex + v.normal * sin(_Time.x * _TimeScale) * _Strength;
				// Transform the point to clip space:
				o.vertex=UnityObjectToClipPos(modifiedPosition);
				// The next two lines are equivalent to the above line:
				//o.vertex.w = 1;
				//o.vertex = mul(UNITY_MATRIX_MVP,modifiedPosition);
				// Pass the UVs to the fragment shader unmodified:
				o.uv = v.uv;
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				// sample the texture
				fixed4 col = tex2D(_MainTex, i.uv);
				return col;
			}
			ENDCG
		}
	}
}
