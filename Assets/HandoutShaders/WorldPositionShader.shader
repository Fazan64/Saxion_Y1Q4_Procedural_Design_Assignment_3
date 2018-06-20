Shader "Handout/WorldPosition"
{
	Properties
	{
		_Scale ("Scale", float) = 5 
		_ColorClose ("ColorClose", color) = (1,0,0,1)
		_ColorFar ("ColorFar", color) = (0,1,1,1)
		_DistanceClose ("DistanceClose", float) = 1
		_DistanceFar ("DistanceFar", float) = 10
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
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
				float4 cameraSpacePosition : TEXCOORD0; // it's not a texture coordinate, but we must pick something for the semantic...
			};

			v2f vert (appdata v)
			{
				v2f o;
				// Transform the point to clip space:
				o.vertex = mul(UNITY_MATRIX_MVP, v.vertex);
				// Also pass the world position to the fragment shader:
				o.cameraSpacePosition = mul(UNITY_MATRIX_MV, v.vertex);
				return o;
			}

			float _Scale;
			float4 _ColorClose;
			float4 _ColorFar;
			float _DistanceClose;
			float _DistanceFar;
			
			fixed4 frag (v2f i) : SV_Target
			{
			    /*
				// rounds scaled coordinates down to integer:
				float4 roundedPosition = floor(i.worldPosition * _Scale);
				// modulo 2, so returns -1, 0 or 1 in a 3D checkerboard pattern:
				int cell = fmod(roundedPosition.x + roundedPosition.y + roundedPosition.z,2);	
				// Fix negative numbers! Go from -1,0,1 to 0,1:
				cell = fmod(cell+2,2);	
				// return color red or green depending on world position:
				return float4(cell,1-cell,0,1);
				*/
			    
			    float distanceToCamera = length(i.cameraSpacePosition);
			    float t = (distanceToCamera - _DistanceClose) / (_DistanceFar - _DistanceClose);
			    
			    float4 colorClose = float4(1,0,0,1);
			    float4 colorFar = float4(0,1,1,1);
			    return _ColorClose + t * (_ColorFar - _ColorClose);
			}
			ENDCG
		}
	}
}
