// Made with Amplify Shader Editor v1.9.7.1
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "Vefects/SH_Vefects_BIRP_Unlit_Flipbook_Pixel_Advanced_01"
{
	Properties
	{
		_Emissive("Emissive", Float) = 1
		_FlipbookX("Flipbook X", Float) = 7
		_FlipbookY("Flipbook Y", Float) = 1
		[Space(13)][Header(Main Texture)][Space(13)]_MainTexture("Main Texture", 2D) = "white" {}
		[HDR]_R("R", Color) = (1,0.9719134,0.5896226,0)
		[HDR]_G("G", Color) = (1,0.7230805,0.25,0)
		[HDR]_B("B", Color) = (0.5943396,0.259371,0.09812209,0)
		[HDR]_Outline("Outline", Color) = (0.2169811,0.03320287,0.02354041,0)
		_FlatColor("Flat Color", Range( 0 , 1)) = 0
		_UVS("UV S", Vector) = (1,1,0,0)
		_UVP("UV P", Vector) = (0,0,0,0)
		[Space(13)][Header(DisolveMapping)][Space(13)]_disolveMap("disolveMap", 2D) = "white" {}
		_DissolveMapScale("Dissolve Map Scale", Float) = 1
		[Space(13)][Header(Distortion)][Space(13)]_DistortionTexture("Distortion Texture", 2D) = "white" {}
		_DistortionLerp("Distortion Lerp", Range( 0 , 0.1)) = 0
		_UVDS("UV D S", Vector) = (1,1,0,0)
		_UVDP("UV D P", Vector) = (0.1,-0.2,0,0)
		[Space(33)][Header(Pixelate)][Space(13)][Toggle(_PIXELATE_ON)] _Pixelate("Pixelate", Float) = 0
		_PixelsMultiplier("Pixels Multiplier", Float) = 1
		_PixelsX("Pixels X", Float) = 32
		_PixelsY("Pixels Y", Float) = 32
		[Space(13)][Header(AR)][Space(13)]_Cull("Cull", Float) = 2
		_Src("Src", Float) = 5
		_Dst("Dst", Float) = 10
		_ZWrite("ZWrite", Float) = 0
		_ZTest("ZTest", Float) = 2
		[HideInInspector] _texcoord( "", 2D ) = "white" {}
		[HideInInspector] __dirty( "", Int ) = 1
	}

	SubShader
	{
		Tags{ "RenderType" = "Transparent"  "Queue" = "Transparent+0" "IsEmissive" = "true"  }
		Cull Back
		ZWrite [_ZWrite]
		ZTest [_ZTest]
		Blend [_Src] [_Dst]
		CGINCLUDE
		#include "UnityShaderVariables.cginc"
		#include "UnityPBSLighting.cginc"
		#include "Lighting.cginc"
		#pragma target 3.0
		#pragma shader_feature_local _PIXELATE_ON
		#define ASE_VERSION 19701
		#undef TRANSFORM_TEX
		#define TRANSFORM_TEX(tex,name) float4(tex.xy * name##_ST.xy + name##_ST.zw, tex.z, tex.w)
		struct Input
		{
			float4 vertexColor : COLOR;
			float4 uv_texcoord;
		};

		uniform float _Src;
		uniform float _Dst;
		uniform float _ZTest;
		uniform float _ZWrite;
		uniform float _Cull;
		uniform float4 _Outline;
		uniform float4 _B;
		uniform sampler2D _MainTexture;
		uniform float2 _UVP;
		uniform float2 _UVS;
		uniform sampler2D _DistortionTexture;
		uniform float2 _UVDP;
		uniform float2 _UVDS;
		uniform float _DistortionLerp;
		uniform float _PixelsX;
		uniform float _PixelsMultiplier;
		uniform float _PixelsY;
		uniform float4 _G;
		uniform float4 _R;
		uniform float _FlatColor;
		uniform float _Emissive;
		uniform sampler2D _disolveMap;
		uniform float _FlipbookX;
		uniform float _FlipbookY;
		uniform float _DissolveMapScale;

		inline half4 LightingUnlit( SurfaceOutput s, half3 lightDir, half atten )
		{
			return half4 ( 0, 0, 0, s.Alpha );
		}

		void surf( Input i , inout SurfaceOutput o )
		{
			float2 panner140 = ( 1.0 * _Time.y * _UVP + ( i.uv_texcoord.xy * _UVS ));
			float2 panner127 = ( 1.0 * _Time.y * _UVDP + ( i.uv_texcoord.xy * _UVDS ));
			float2 lerpResult139 = lerp( float2( 0,0 ) , ( ( (tex2D( _DistortionTexture, panner127 )).rg + -0.5 ) * 2.0 ) , _DistortionLerp);
			float2 DistortionRegister149 = ( panner140 + lerpResult139 );
			float pixelWidth115 =  1.0f / ( _PixelsX * _PixelsMultiplier );
			float pixelHeight115 = 1.0f / ( _PixelsY * _PixelsMultiplier );
			half2 pixelateduv115 = half2((int)(DistortionRegister149.x / pixelWidth115) * pixelWidth115, (int)(DistortionRegister149.y / pixelHeight115) * pixelHeight115);
			#ifdef _PIXELATE_ON
				float2 staticSwitch118 = pixelateduv115;
			#else
				float2 staticSwitch118 = DistortionRegister149;
			#endif
			float4 tex2DNode45 = tex2D( _MainTexture, staticSwitch118 );
			float4 lerpResult97 = lerp( _Outline , _B , tex2DNode45.b);
			float4 lerpResult112 = lerp( lerpResult97 , _G , tex2DNode45.g);
			float4 lerpResult111 = lerp( lerpResult112 , _R , tex2DNode45.r);
			float4 lerpResult88 = lerp( ( i.vertexColor * lerpResult111 ) , i.vertexColor , _FlatColor);
			float4 color183 = ( lerpResult88 * _Emissive );
			o.Emission = color183.rgb;
			float mainTex_alpha48 = tex2DNode45.a;
			float mainTex_VC_alha52 = i.vertexColor.a;
			float Opacity_VTC_W158 = i.uv_texcoord.z;
			float Opacity_VTC_T152 = i.uv_texcoord.w;
			float temp_output_167_0 = (( Opacity_VTC_T152 - 1.0 ) + (Opacity_VTC_W158 - 0.0) * (1.0 - ( Opacity_VTC_T152 - 1.0 )) / (1.0 - 0.0));
			float flipbook_X143 = _FlipbookX;
			float flipbook_Y144 = _FlipbookY;
			float2 appendResult151 = (float2(flipbook_X143 , flipbook_Y144));
			float smoothstepResult172 = smoothstep( temp_output_167_0 , ( temp_output_167_0 + Opacity_VTC_T152 ) , tex2D( _disolveMap, ( ( ( i.uv_texcoord.xy * appendResult151 ) * _DissolveMapScale ) + flipbook_X143 ) ).g);
			float disolveMapping173 = smoothstepResult172;
			float OpacityRegister179 = ( ( mainTex_alpha48 * mainTex_VC_alha52 ) * disolveMapping173 );
			o.Alpha = OpacityRegister179;
		}

		ENDCG
		CGPROGRAM
		#pragma surface surf Unlit keepalpha fullforwardshadows 

		ENDCG
		Pass
		{
			Name "ShadowCaster"
			Tags{ "LightMode" = "ShadowCaster" }
			ZWrite On
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 3.0
			#pragma multi_compile_shadowcaster
			#pragma multi_compile UNITY_PASS_SHADOWCASTER
			#pragma skip_variants FOG_LINEAR FOG_EXP FOG_EXP2
			#include "HLSLSupport.cginc"
			#if ( SHADER_API_D3D11 || SHADER_API_GLCORE || SHADER_API_GLES || SHADER_API_GLES3 || SHADER_API_METAL || SHADER_API_VULKAN )
				#define CAN_SKIP_VPOS
			#endif
			#include "UnityCG.cginc"
			#include "Lighting.cginc"
			#include "UnityPBSLighting.cginc"
			sampler3D _DitherMaskLOD;
			struct v2f
			{
				V2F_SHADOW_CASTER;
				float4 customPack1 : TEXCOORD1;
				float3 worldPos : TEXCOORD2;
				half4 color : COLOR0;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};
			v2f vert( appdata_full v )
			{
				v2f o;
				UNITY_SETUP_INSTANCE_ID( v );
				UNITY_INITIALIZE_OUTPUT( v2f, o );
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO( o );
				UNITY_TRANSFER_INSTANCE_ID( v, o );
				Input customInputData;
				float3 worldPos = mul( unity_ObjectToWorld, v.vertex ).xyz;
				half3 worldNormal = UnityObjectToWorldNormal( v.normal );
				o.customPack1.xyzw = customInputData.uv_texcoord;
				o.customPack1.xyzw = v.texcoord;
				o.worldPos = worldPos;
				TRANSFER_SHADOW_CASTER_NORMALOFFSET( o )
				o.color = v.color;
				return o;
			}
			half4 frag( v2f IN
			#if !defined( CAN_SKIP_VPOS )
			, UNITY_VPOS_TYPE vpos : VPOS
			#endif
			) : SV_Target
			{
				UNITY_SETUP_INSTANCE_ID( IN );
				Input surfIN;
				UNITY_INITIALIZE_OUTPUT( Input, surfIN );
				surfIN.uv_texcoord = IN.customPack1.xyzw;
				float3 worldPos = IN.worldPos;
				half3 worldViewDir = normalize( UnityWorldSpaceViewDir( worldPos ) );
				surfIN.vertexColor = IN.color;
				SurfaceOutput o;
				UNITY_INITIALIZE_OUTPUT( SurfaceOutput, o )
				surf( surfIN, o );
				#if defined( CAN_SKIP_VPOS )
				float2 vpos = IN.pos;
				#endif
				half alphaRef = tex3D( _DitherMaskLOD, float3( vpos.xy * 0.25, o.Alpha * 0.9375 ) ).a;
				clip( alphaRef - 0.01 );
				SHADOW_CASTER_FRAGMENT( IN )
			}
			ENDCG
		}
	}
	Fallback "Diffuse"
	CustomEditor "ASEMaterialInspector"
}
/*ASEBEGIN
Version=19701
Node;AmplifyShaderEditor.CommentaryNode;122;-4736,-1664;Inherit;False;1992;995;Distortion;18;149;145;140;139;136;135;134;133;132;131;130;129;128;127;126;125;124;123;;0,0,0,1;0;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;123;-4672,-976;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.Vector2Node;124;-4416,-848;Inherit;False;Property;_UVDS;UV D S;16;0;Create;True;0;0;0;False;0;False;1,1;1,1;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;125;-4416,-976;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.Vector2Node;126;-4160,-848;Inherit;False;Property;_UVDP;UV D P;17;0;Create;True;0;0;0;False;0;False;0.1,-0.2;0.1,-0.2;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.PannerNode;127;-4160,-976;Inherit;False;3;0;FLOAT2;0,0;False;2;FLOAT2;0,0;False;1;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SamplerNode;128;-3904,-976;Inherit;True;Property;_DistortionTexture;Distortion Texture;14;0;Create;True;0;0;0;False;3;Space(13);Header(Distortion);Space(13);False;-1;98c3d568d9032a34eb5b038e20fea05d;98c3d568d9032a34eb5b038e20fea05d;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.ComponentMaskNode;129;-3520,-976;Inherit;False;True;True;False;False;1;0;COLOR;0,0,0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.Vector2Node;130;-3776,-1488;Inherit;False;Property;_UVS;UV S;10;0;Create;True;0;0;0;False;0;False;1,1;1,1;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.TextureCoordinatesNode;131;-4032,-1616;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.Vector2Node;132;-3520,-1488;Inherit;False;Property;_UVP;UV P;11;0;Create;True;0;0;0;False;0;False;0,0;0,0;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.RangedFloatNode;133;-3520,-1104;Inherit;False;Property;_DistortionLerp;Distortion Lerp;15;0;Create;True;0;0;0;False;0;False;0;0;0;0.1;0;1;FLOAT;0
Node;AmplifyShaderEditor.Vector2Node;134;-3520,-1232;Inherit;False;Constant;_Vector0;Vector 0;8;0;Create;True;0;0;0;False;0;False;0,0;0,0;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.FunctionNode;135;-3264,-976;Inherit;False;ConstantBiasScale;-1;;2;63208df05c83e8e49a48ffbdce2e43a0;0;3;3;FLOAT2;0,0;False;1;FLOAT;-0.5;False;2;FLOAT;2;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;136;-3776,-1616;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.LerpOp;139;-3136,-1232;Inherit;False;3;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.PannerNode;140;-3520,-1616;Inherit;False;3;0;FLOAT2;0,0;False;2;FLOAT2;0,0;False;1;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.CommentaryNode;36;-2416,-1728;Inherit;False;1896;1857.979;Color;22;48;45;112;111;106;105;101;97;96;93;92;88;52;47;38;115;116;117;118;119;120;121;;0,0,0,1;0;0
Node;AmplifyShaderEditor.RangedFloatNode;137;-4608,1472;Inherit;False;Property;_FlipbookX;Flipbook X;2;0;Create;True;0;0;0;False;0;False;7;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;138;-4608,1600;Inherit;False;Property;_FlipbookY;Flipbook Y;3;0;Create;True;0;0;0;False;0;False;1;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;145;-3136,-1616;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.CommentaryNode;141;-4720,-256;Inherit;False;1768.791;450.8129;Opacity;11;179;178;177;176;175;174;159;158;153;152;148;;0,0,0,1;0;0
Node;AmplifyShaderEditor.CommentaryNode;142;-4704,208;Inherit;False;1761.094;1140.744;DisolveMaping;22;173;172;171;170;169;168;167;166;165;164;163;162;161;160;157;156;155;154;151;150;147;146;;0.1037736,0.1037736,0.1037736,1;0;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;143;-4352,1472;Inherit;False;flipbook X;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;144;-4352,1600;Inherit;False;flipbook Y;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;117;-2304,-128;Inherit;False;Property;_PixelsY;Pixels Y;21;0;Create;True;0;0;0;False;0;False;32;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;116;-2304,-256;Inherit;False;Property;_PixelsX;Pixels X;20;0;Create;True;0;0;0;False;0;False;32;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;121;-2304,0;Inherit;False;Property;_PixelsMultiplier;Pixels Multiplier;19;0;Create;True;0;0;0;False;0;False;1;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;149;-2976,-1616;Inherit;False;DistortionRegister;-1;True;1;0;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.GetLocalVarNode;146;-4352,960;Inherit;False;143;flipbook X;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;147;-4352,1088;Inherit;False;144;flipbook Y;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.TexCoordVertexDataNode;148;-4608,-192;Inherit;False;0;4;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;119;-2048,-256;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;120;-2048,-128;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;38;-2304,-640;Inherit;False;149;DistortionRegister;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;150;-4608,832;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.DynamicAppendNode;151;-4096,960;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;152;-4224,-112;Inherit;False;Opacity_VTC_T;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TFHCPixelate;115;-2304,-384;Inherit;False;3;0;FLOAT2;0,0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode;154;-4656,560;Inherit;False;Constant;_Float2;Float 2;20;0;Create;True;0;0;0;False;0;False;1;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;155;-4688,320;Inherit;False;152;Opacity_VTC_T;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;156;-4096,832;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode;157;-3840,960;Inherit;False;Property;_DissolveMapScale;Dissolve Map Scale;13;0;Create;True;0;0;0;False;0;False;1;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;158;-4224,-192;Inherit;False;Opacity_VTC_W;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.StaticSwitch;118;-2048,-640;Inherit;False;Property;_Pixelate;Pixelate;18;0;Create;True;0;0;0;False;3;Space(33);Header(Pixelate);Space(13);False;0;0;0;True;;Toggle;2;Key0;Key1;Create;True;True;All;9;1;FLOAT2;0,0;False;0;FLOAT2;0,0;False;2;FLOAT2;0,0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT2;0,0;False;6;FLOAT2;0,0;False;7;FLOAT2;0,0;False;8;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.GetLocalVarNode;160;-4368,256;Inherit;False;158;Opacity_VTC_W;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;161;-4480,320;Inherit;False;Constant;_Float0;Float 0;20;0;Create;True;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;162;-4480,432;Inherit;False;Constant;_Float1;Float 1;20;0;Create;True;0;0;0;False;0;False;1;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;163;-4480,640;Inherit;False;Constant;_Float3;Float 3;20;0;Create;True;0;0;0;False;0;False;1;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;164;-4480,528;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;165;-3840,832;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.GetLocalVarNode;166;-3584,960;Inherit;False;143;flipbook X;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;45;-1792,-640;Inherit;True;Property;_MainTexture;Main Texture;4;0;Create;True;0;0;0;False;3;Space(13);Header(Main Texture);Space(13);False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.ColorNode;92;-2304,-1152;Inherit;False;Property;_B;B;7;1;[HDR];Create;True;0;0;0;False;0;False;0.5943396,0.259371,0.09812209,0;0.2641509,0.2616589,0.2554289,0;True;True;0;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.ColorNode;93;-2304,-896;Inherit;False;Property;_Outline;Outline;8;1;[HDR];Create;True;0;0;0;False;0;False;0.2169811,0.03320287,0.02354041,0;0,0,0,0;True;True;0;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.TFHCRemapNode;167;-4240,352;Inherit;False;5;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;3;FLOAT;0;False;4;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;168;-4144,256;Inherit;False;152;Opacity_VTC_T;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;169;-3584,832;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.ColorNode;96;-2304,-1408;Inherit;False;Property;_G;G;6;1;[HDR];Create;True;0;0;0;False;0;False;1,0.7230805,0.25,0;1,0.3523919,0,0;True;True;0;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.LerpOp;97;-1792,-1280;Inherit;True;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.VertexColorNode;47;-1280,-1024;Inherit;False;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleAddOpNode;170;-3920,288;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;171;-3328,832;Inherit;True;Property;_disolveMap;disolveMap;12;0;Create;True;0;0;0;False;3;Space(13);Header(DisolveMapping);Space(13);False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.ColorNode;101;-2304,-1664;Inherit;False;Property;_R;R;5;1;[HDR];Create;True;0;0;0;False;0;False;1,0.9719134,0.5896226,0;0.3679245,0.3679245,0.3679245,0;True;True;0;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.LerpOp;112;-1408,-1408;Inherit;True;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;48;-1408,-640;Inherit;False;mainTex_alpha;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;52;-1280,-768;Inherit;False;mainTex_VC_alha;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SmoothstepOpNode;172;-3776,400;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;111;-1152,-1664;Inherit;True;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;173;-3584,400;Inherit;False;disolveMapping;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;174;-3840,-64;Inherit;False;48;mainTex_alpha;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;175;-3840,64;Inherit;False;52;mainTex_VC_alha;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;105;-768,-1152;Inherit;True;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;106;-1152,-384;Inherit;False;Property;_FlatColor;Flat Color;9;0;Create;True;0;0;0;False;0;False;0;0;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;177;-3584,-64;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;176;-3456,64;Inherit;False;173;disolveMapping;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;88;-768,-640;Inherit;True;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;184;-384,-512;Inherit;False;Property;_Emissive;Emissive;1;0;Create;True;0;0;0;False;0;False;1;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;178;-3456,-64;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;180;-384,-640;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.CommentaryNode;63;336,-48;Inherit;False;1243;166;AR;5;110;80;78;82;83;;0,0,0,1;0;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;179;-3200,-64;Inherit;False;OpacityRegister;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;183;-128,-640;Inherit;False;color;-1;True;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;78;640,0;Inherit;False;Property;_Src;Src;23;0;Create;True;0;0;0;True;0;False;5;5;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;110;896,0;Inherit;False;Property;_Dst;Dst;24;0;Create;True;0;0;0;True;0;False;10;10;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;82;1408,0;Inherit;False;Property;_ZTest;ZTest;26;0;Create;True;0;0;0;True;0;False;2;2;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;83;1152,0;Inherit;False;Property;_ZWrite;ZWrite;25;0;Create;True;0;0;0;True;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;80;384,0;Inherit;False;Property;_Cull;Cull;22;0;Create;True;0;0;0;True;3;Space(13);Header(AR);Space(13);False;2;2;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.TexCoordVertexDataNode;153;-4608,0;Inherit;False;1;4;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RegisterLocalVarNode;159;-4224,0;Inherit;False;Random Offset;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;73;-384,128;Inherit;False;179;OpacityRegister;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;181;-384,0;Inherit;False;183;color;1;0;OBJECT;;False;1;COLOR;0
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;186;0,0;Float;False;True;-1;2;ASEMaterialInspector;0;0;Unlit;Vefects/SH_Vefects_BIRP_Unlit_Flipbook_Pixel_Advanced_01;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;Back;0;True;_ZWrite;0;True;_ZTest;False;0;False;;0;False;;False;0;Custom;0.5;True;True;0;False;Transparent;;Transparent;All;12;all;True;True;True;True;0;False;;False;0;False;;255;False;;255;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;False;2;15;10;25;False;0.5;True;1;5;True;_Src;10;True;_Dst;0;0;False;;0;False;;0;False;;0;False;;0;False;0;0,0,0,0;VertexOffset;True;False;Cylindrical;False;True;Relative;0;;0;-1;-1;-1;0;False;0;0;False;;-1;0;False;;0;0;0;False;0.1;False;;0;False;;False;16;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT;0;False;4;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;16;FLOAT4;0,0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
Node;AmplifyShaderEditor.CommentaryNode;113;336,-224;Inherit;False;304;100;Ge Lush was here! <3;0;Ge Lush was here! <3;0,0,0,1;0;0
WireConnection;125;0;123;0
WireConnection;125;1;124;0
WireConnection;127;0;125;0
WireConnection;127;2;126;0
WireConnection;128;1;127;0
WireConnection;129;0;128;0
WireConnection;135;3;129;0
WireConnection;136;0;131;0
WireConnection;136;1;130;0
WireConnection;139;0;134;0
WireConnection;139;1;135;0
WireConnection;139;2;133;0
WireConnection;140;0;136;0
WireConnection;140;2;132;0
WireConnection;145;0;140;0
WireConnection;145;1;139;0
WireConnection;143;0;137;0
WireConnection;144;0;138;0
WireConnection;149;0;145;0
WireConnection;119;0;116;0
WireConnection;119;1;121;0
WireConnection;120;0;117;0
WireConnection;120;1;121;0
WireConnection;151;0;146;0
WireConnection;151;1;147;0
WireConnection;152;0;148;4
WireConnection;115;0;38;0
WireConnection;115;1;119;0
WireConnection;115;2;120;0
WireConnection;156;0;150;0
WireConnection;156;1;151;0
WireConnection;158;0;148;3
WireConnection;118;1;38;0
WireConnection;118;0;115;0
WireConnection;164;0;155;0
WireConnection;164;1;154;0
WireConnection;165;0;156;0
WireConnection;165;1;157;0
WireConnection;45;1;118;0
WireConnection;167;0;160;0
WireConnection;167;1;161;0
WireConnection;167;2;162;0
WireConnection;167;3;164;0
WireConnection;167;4;163;0
WireConnection;169;0;165;0
WireConnection;169;1;166;0
WireConnection;97;0;93;0
WireConnection;97;1;92;0
WireConnection;97;2;45;3
WireConnection;170;0;167;0
WireConnection;170;1;168;0
WireConnection;171;1;169;0
WireConnection;112;0;97;0
WireConnection;112;1;96;0
WireConnection;112;2;45;2
WireConnection;48;0;45;4
WireConnection;52;0;47;4
WireConnection;172;0;171;2
WireConnection;172;1;167;0
WireConnection;172;2;170;0
WireConnection;111;0;112;0
WireConnection;111;1;101;0
WireConnection;111;2;45;1
WireConnection;173;0;172;0
WireConnection;105;0;47;0
WireConnection;105;1;111;0
WireConnection;177;0;174;0
WireConnection;177;1;175;0
WireConnection;88;0;105;0
WireConnection;88;1;47;0
WireConnection;88;2;106;0
WireConnection;178;0;177;0
WireConnection;178;1;176;0
WireConnection;180;0;88;0
WireConnection;180;1;184;0
WireConnection;179;0;178;0
WireConnection;183;0;180;0
WireConnection;159;0;153;1
WireConnection;186;2;181;0
WireConnection;186;9;73;0
ASEEND*/
//CHKSM=05F21E42ECEE75D33E5825A621ADF6E0262E74AD