Shader "Wobblewares/Toon Wobble"
{
	Properties
	{
		_Color("Color", Color) = (1,1,1,1)
		_ShadowColor("Shadow Color", Color) = (0,0,0,1)
		_WobbleFrequency("Frequency per second", Float) = 4.0
		_WobbleFrameAmount("Frames", Int) = 4
		_VertexWobble("Vertex Offset In Units", Float) = 0.01
		_MainTex("Main Texture", 2D) = "white" {}
		_TextureWobble("Texture Offset", Float) = 0.01
	}
	SubShader
	{
		Pass
		{
			// Setup our pass to use Forward rendering, and only receive
			// data on the main directional light and ambient light.
			Tags
			{
				"LightMode" = "ForwardBase"
				"PassFlags" = "OnlyDirectional"
			}

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_fwdbase
			
			#include "UnityCG.cginc"
			#include "Lighting.cginc"
			#include "AutoLight.cginc"

			struct appdata
			{
				float4 vertex : POSITION;				
				float4 uv : TEXCOORD0;
				float3 normal : NORMAL;
			};

			struct v2f
			{
				float4 pos : SV_POSITION;
				float3 worldNormal : NORMAL;
				float2 uv : TEXCOORD0;
				float3 viewDir : TEXCOORD1;	
				SHADOW_COORDS(2)
			};

			sampler2D _MainTex;
			float4 _MainTex_ST;
			float _WobbleFrequency;
			uint _WobbleFrameAmount;
			float _VertexWobble;
			float _TextureWobble;
			
			v2f vert (appdata v)
			{
				// using time as a randomiser, calculate which frame we are currently on
				float time = (1.0f + _Time.z) * _WobbleFrequency;
				uint frame = (time % _WobbleFrameAmount) + 1; //makes it between 1 and frameCount

				// offset the local vertex based on the frame and desired VertexWobble.
				// Multiply by y,x,z to get a more uneven wobble across the mesh.
				v.vertex.x += sin(v.vertex.y * cos(frame)) * _VertexWobble;
				v.vertex.y += sin(v.vertex.x * cos(frame)) * _VertexWobble;
				v.vertex.z += sin(v.vertex.z * cos(frame)) * _VertexWobble;
				
				v2f o;
				o.worldNormal = UnityObjectToWorldNormal(v.normal);		
				o.pos = UnityObjectToClipPos(v.vertex);
				o.viewDir = WorldSpaceViewDir(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);

				// apply a slight uv warp based on _TextureWobble.
				// Multiply by x,y again to get an uneven wobble.
				o.uv.x += sin(v.vertex.x * cos(frame)) * _TextureWobble;
				o.uv.y += sin(v.vertex.y * cos(frame)) * _TextureWobble;

				TRANSFER_SHADOW(o)
				
				return o;
			}
			
			float4 _Color;
			float4 _AmbientColor;
			float4 _ShadowColor;
			
			float4 frag (v2f i) : SV_Target
			{
				float3 normal = normalize(i.worldNormal);
				float NdotL = dot(_WorldSpaceLightPos0, normal);
				float shadow = SHADOW_ATTENUATION(i);
				float lightIntensity = smoothstep(0, 0.001, NdotL * shadow);
				 
				// calculate color by lerping between shadow and color based on light intensity
				float4 color = lerp(_ShadowColor, _Color, lightIntensity);
				float4 sample = tex2D(_MainTex, i.uv);

				return color * sample;
			}
			ENDCG
		}

		// Shadow casting support.
        UsePass "Legacy Shaders/VertexLit/SHADOWCASTER"
	}
}