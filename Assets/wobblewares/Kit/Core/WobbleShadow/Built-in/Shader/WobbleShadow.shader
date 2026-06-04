   Shader "Wobblewares/Wobble Shadow" {
Properties {
	_Color ("Tint Color", Color) = (1,1,1,1)
	_Attenuation ("Falloff", Range(0.0, 1.0)) = 1.0
	_ShadowTex ("Cookie", 2D) = "gray" {}
	_WobbleFrequency("Frequency per second", Float) = 4.0
	_WobbleFrameAmount("Frames", Int) = 4
	_TextureWobble("Texture Offset", Float) = 0.01
}
Subshader {
	Tags {"Queue"="Transparent"}
	Pass {
		ZWrite Off
		ColorMask RGB
		Blend One OneMinusSrcAlpha // Additive blending
		Offset -1, -1

		CGPROGRAM
		#pragma vertex vert
		#pragma fragment frag
		#include "UnityCG.cginc"
		
		struct v2f {
			float4 uvShadow : TEXCOORD0;
			float4 pos : SV_POSITION;
		};
		
		float _WobbleFrequency;
		uint _WobbleFrameAmount;
		float _TextureWobble;
		
		float4x4 unity_Projector;
		float4x4 unity_ProjectorClip;
		
		v2f vert (float4 vertex : POSITION, float3 normal : NORMAL)
		{
			v2f o;
			o.pos = UnityObjectToClipPos (vertex);
			o.uvShadow = mul (unity_Projector, vertex);

			// using time as a randomiser, calculate which frame we are currently on
			float time = (1.0f + _Time.z) * _WobbleFrequency;
			uint frame = (time % _WobbleFrameAmount) + 1; //makes it between 1 and frameCount
			
			// adjust the uv based on wobble
			o.uvShadow.x += sin(o.uvShadow.y * cos(frame)) * _TextureWobble;
			o.uvShadow.y += sin(o.uvShadow.x * cos(frame)) * _TextureWobble;
			
			return o;
		}
		
		sampler2D _ShadowTex;
		fixed4 _Color;
		float _Attenuation;
		
		fixed4 frag (v2f i) : SV_Target
		{
			// Apply tint & alpha mask
			fixed4 texCookie = tex2Dproj (_ShadowTex, UNITY_PROJ_COORD(i.uvShadow));
			fixed4 outColor = _Color * texCookie.a;
			// Distance attenuation
			float depth = i.uvShadow.z; // [-1(near), 1(far)]
			return outColor * clamp(1.0 - abs(depth) + _Attenuation, 0.0, 1.0);
		}
		ENDCG
	}
}
}