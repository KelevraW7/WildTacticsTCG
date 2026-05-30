// Made with Amplify Shader Editor v1.9.7.1
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "Vefects/SH_Vefects_BIRP_Unlit_Combat_Flipbook_Advanced_01"
{
	Properties
	{
		[Space(33)][Header(Flipbook Frames)][Space(13)]_FlipbookX("Flipbook X", Float) = 4
		_FlipbookY("Flipbook Y", Float) = 4
		[Space(33)][Header(Mask Texture)][Space(13)]_MaskTexture("Mask Texture", 2D) = "white" {}
		_MaskUVScale("Mask UV Scale", Vector) = (1,1,0,0)
		_MaskUVPan("Mask UV Pan", Vector) = (0,0,0,0)
		[HDR]_R("R", Color) = (1,0.9719134,0.5896226,0)
		[HDR]_G("G", Color) = (1,0.7230805,0.25,0)
		[HDR]_B("B", Color) = (0.5943396,0.259371,0.09812209,0)
		[HDR]_Outline("Outline", Color) = (0.2169811,0.03320287,0.02354041,0)
		_FlatColor("Flat Color", Range( 0 , 1)) = 0
		_Emissive("Emissive", Float) = 1
		[Space(33)][Header(Alpha Texture)][Space(13)]_AlphaTexture("Alpha Texture", 2D) = "white" {}
		_AlphaGlow("Alpha Glow", Float) = 0
		[Space(33)][Header(Erosion Noise)][Space(13)]_ErosionNoise("Erosion Noise", 2D) = "white" {}
		_ErosionNoiseUVScaleOverall("Erosion Noise UV Scale Overall", Float) = 1
		_ErosionNoiseUVPan("Erosion Noise UV Pan", Vector) = (0,0,0,0)
		_ErosionIntensity("Erosion Intensity", Float) = 0
		_ErosionSmoothness("Erosion Smoothness", Float) = 1
		_DepthFade("Depth Fade", Float) = 0.1
		[Space(33)][Header(Distortion)][Space(13)]_DistortionTexture("Distortion Texture", 2D) = "white" {}
		_DistortionLerp("Distortion Lerp", Range( 0 , 0.1)) = 0
		_DistortionUVScale("Distortion UV Scale", Vector) = (1,1,0,0)
		_DistortionUVPan("Distortion UV Pan", Vector) = (0.1,-0.2,0,0)
		[Space(33)][Header(Pixelate)][Space(13)][Toggle(_PIXELATE_ON)] _Pixelate("Pixelate", Float) = 0
		_PixelsMultiplier("Pixels Multiplier", Float) = 1
		_PixelsX("Pixels X", Float) = 32
		_PixelsY("Pixels Y", Float) = 32
		[Space(33)][Header(AR)][Space(13)]_Cull("Cull", Float) = 2
		_Src("Src", Float) = 5
		_Dst("Dst", Float) = 10
		_ZWrite("ZWrite", Float) = 0
		_ZTest("ZTest", Float) = 2
		[HideInInspector] _texcoord2( "", 2D ) = "white" {}
		[HideInInspector] _texcoord( "", 2D ) = "white" {}
		[HideInInspector] __dirty( "", Int ) = 1
	}

	SubShader
	{
		Tags{ "RenderType" = "Transparent"  "Queue" = "Transparent+0" "IsEmissive" = "true"  }
		Cull [_Cull]
		ZWrite [_ZWrite]
		ZTest [_ZTest]
		Blend [_Src] [_Dst]
		
		CGPROGRAM
		#include "UnityShaderVariables.cginc"
		#include "UnityCG.cginc"
		#pragma target 3.5
		#pragma shader_feature_local _PIXELATE_ON
		#define ASE_VERSION 19701
		#pragma surface surf Unlit keepalpha noshadow 
		#undef TRANSFORM_TEX
		#define TRANSFORM_TEX(tex,name) float4(tex.xy * name##_ST.xy + name##_ST.zw, tex.z, tex.w)
		struct Input
		{
			float4 vertexColor : COLOR;
			float4 uv_texcoord;
			float4 uv2_texcoord2;
			float4 screenPos;
		};

		uniform float _Src;
		uniform float _Dst;
		uniform float _ZTest;
		uniform float _ZWrite;
		uniform float _Cull;
		uniform float4 _Outline;
		uniform float4 _B;
		uniform sampler2D _MaskTexture;
		uniform float2 _MaskUVPan;
		uniform float2 _MaskUVScale;
		uniform sampler2D _DistortionTexture;
		uniform float2 _DistortionUVPan;
		uniform float2 _DistortionUVScale;
		uniform float _FlipbookX;
		uniform float _FlipbookY;
		uniform float _DistortionLerp;
		uniform float _PixelsX;
		uniform float _PixelsMultiplier;
		uniform float _PixelsY;
		uniform float4 _G;
		uniform float4 _R;
		uniform float _FlatColor;
		uniform float _Emissive;
		uniform sampler2D _AlphaTexture;
		uniform float _AlphaGlow;
		uniform float _ErosionSmoothness;
		uniform sampler2D _ErosionNoise;
		uniform float2 _ErosionNoiseUVPan;
		uniform float _ErosionNoiseUVScaleOverall;
		uniform float _ErosionIntensity;
		UNITY_DECLARE_DEPTH_TEXTURE( _CameraDepthTexture );
		uniform float4 _CameraDepthTexture_TexelSize;
		uniform float _DepthFade;

		inline half4 LightingUnlit( SurfaceOutput s, half3 lightDir, half atten )
		{
			return half4 ( 0, 0, 0, s.Alpha );
		}

		void surf( Input i , inout SurfaceOutput o )
		{
			float2 panner140 = ( 1.0 * _Time.y * _MaskUVPan + ( i.uv_texcoord.xy * _MaskUVScale ));
			float2 appendResult151 = (float2(_FlipbookX , _FlipbookY));
			float2 FlipbookFrames220 = appendResult151;
			float2 panner127 = ( 1.0 * _Time.y * _DistortionUVPan + ( ( i.uv_texcoord.xy * _DistortionUVScale ) * FlipbookFrames220 ));
			float Random_Offset159 = i.uv2_texcoord2.x;
			float2 lerpResult139 = lerp( float2( 0,0 ) , ( ( (tex2D( _DistortionTexture, ( panner127 + Random_Offset159 ) )).rg + -0.5 ) * 2.0 ) , _DistortionLerp);
			float2 PrePixelateUVs149 = ( panner140 + lerpResult139 );
			float pixelWidth115 =  1.0f / ( _PixelsX * _PixelsMultiplier );
			float pixelHeight115 = 1.0f / ( _PixelsY * _PixelsMultiplier );
			half2 pixelateduv115 = half2((int)(PrePixelateUVs149.x / pixelWidth115) * pixelWidth115, (int)(PrePixelateUVs149.y / pixelHeight115) * pixelHeight115);
			#ifdef _PIXELATE_ON
				float2 staticSwitch118 = pixelateduv115;
			#else
				float2 staticSwitch118 = PrePixelateUVs149;
			#endif
			float2 FinalUVs224 = staticSwitch118;
			float4 tex2DNode45 = tex2D( _MaskTexture, FinalUVs224 );
			float4 lerpResult97 = lerp( _Outline , _B , tex2DNode45.b);
			float4 lerpResult112 = lerp( lerpResult97 , _G , tex2DNode45.g);
			float4 lerpResult111 = lerp( lerpResult112 , _R , tex2DNode45.r);
			float4 lerpResult88 = lerp( ( i.vertexColor * lerpResult111 ) , i.vertexColor , _FlatColor);
			float4 color183 = ( lerpResult88 * _Emissive );
			o.Emission = color183.rgb;
			float4 tex2DNode185 = tex2D( _AlphaTexture, FinalUVs224 );
			float lerpResult186 = lerp( tex2DNode185.g , tex2DNode185.r , saturate( _AlphaGlow ));
			float mainTex_alpha48 = saturate( lerpResult186 );
			float Opacity_VTC_W158 = i.uv_texcoord.z;
			float temp_output_208_0 = saturate( Opacity_VTC_W158 );
			float2 panner216 = ( 1.0 * _Time.y * _ErosionNoiseUVPan + ( ( i.uv_texcoord.xy * FlipbookFrames220 ) * _ErosionNoiseUVScaleOverall ));
			float2 DistortUVs231 = lerpResult139;
			float smoothstepResult200 = smoothstep( temp_output_208_0 , ( temp_output_208_0 + _ErosionSmoothness ) , tex2D( _ErosionNoise, ( ( panner216 + Random_Offset159 ) + DistortUVs231 ) ).g);
			float lerpResult196 = lerp( mainTex_alpha48 , saturate( ( mainTex_alpha48 * saturate( smoothstepResult200 ) ) ) , _ErosionIntensity);
			float mainTex_VC_alha52 = i.vertexColor.a;
			float4 ase_screenPos = float4( i.screenPos.xyz , i.screenPos.w + 0.00000000001 );
			float4 ase_screenPosNorm = ase_screenPos / ase_screenPos.w;
			ase_screenPosNorm.z = ( UNITY_NEAR_CLIP_VALUE >= 0 ) ? ase_screenPosNorm.z : ase_screenPosNorm.z * 0.5 + 0.5;
			float screenDepth211 = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE( _CameraDepthTexture, ase_screenPosNorm.xy ));
			float distanceDepth211 = saturate( ( screenDepth211 - LinearEyeDepth( ase_screenPosNorm.z ) ) / ( _DepthFade ) );
			float OpacityRegister179 = saturate( ( saturate( ( saturate( lerpResult196 ) * mainTex_VC_alha52 ) ) * saturate( distanceDepth211 ) ) );
			o.Alpha = OpacityRegister179;
		}

		ENDCG
	}
	CustomEditor "ASEMaterialInspector"
}
/*ASEBEGIN
Version=19701
Node;AmplifyShaderEditor.CommentaryNode;222;-4658,1742;Inherit;False;836;290.95;Flipbook Frames;4;138;137;151;220;Flipbook Frames;0,0,0,1;0;0
Node;AmplifyShaderEditor.RangedFloatNode;137;-4608,1792;Inherit;False;Property;_FlipbookX;Flipbook X;1;0;Create;True;0;0;0;False;3;Space(33);Header(Flipbook Frames);Space(13);False;4;4;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;138;-4608,1920;Inherit;False;Property;_FlipbookY;Flipbook Y;2;0;Create;True;0;0;0;False;0;False;4;4;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.CommentaryNode;122;-4736,-1664;Inherit;False;1992;995;Distortion;23;149;145;140;139;136;135;134;133;132;131;130;129;128;127;126;125;124;123;192;193;219;223;231;;0,0,0,1;0;0
Node;AmplifyShaderEditor.DynamicAppendNode;151;-4352,1792;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.CommentaryNode;141;-4720,-256;Inherit;False;1768.791;450.8129;Opacity;14;179;177;175;159;158;153;152;148;207;209;210;211;212;213;;0,0,0,1;0;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;123;-4736,-1024;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.Vector2Node;124;-4480,-896;Inherit;False;Property;_DistortionUVScale;Distortion UV Scale;22;0;Create;True;0;0;0;False;0;False;1,1;1,1;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.RegisterLocalVarNode;220;-4096,1792;Inherit;False;FlipbookFrames;-1;True;1;0;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.TexCoordVertexDataNode;153;-4608,0;Inherit;False;1;4;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.GetLocalVarNode;223;-4224,-896;Inherit;False;220;FlipbookFrames;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;125;-4480,-1024;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;159;-4224,0;Inherit;False;Random Offset;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;219;-4224,-1024;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.Vector2Node;126;-3968,-896;Inherit;False;Property;_DistortionUVPan;Distortion UV Pan;23;0;Create;True;0;0;0;False;0;False;0.1,-0.2;0.1,-0.2;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.PannerNode;127;-3968,-1024;Inherit;False;3;0;FLOAT2;0,0;False;2;FLOAT2;0,0;False;1;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.GetLocalVarNode;193;-3712,-1152;Inherit;False;159;Random Offset;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;192;-3712,-1024;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SamplerNode;128;-3584,-1024;Inherit;True;Property;_DistortionTexture;Distortion Texture;20;0;Create;True;0;0;0;False;3;Space(33);Header(Distortion);Space(13);False;-1;4a897e6098e8f804a9ec44f2426669c2;4a897e6098e8f804a9ec44f2426669c2;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.TextureCoordinatesNode;131;-4608,-1600;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.ComponentMaskNode;129;-3328,-1152;Inherit;False;True;True;False;False;1;0;COLOR;0,0,0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.Vector2Node;130;-4224,-1472;Inherit;False;Property;_MaskUVScale;Mask UV Scale;4;0;Create;True;0;0;0;False;0;False;1,1;1,1;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;136;-4224,-1600;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode;133;-3584,-1280;Inherit;False;Property;_DistortionLerp;Distortion Lerp;21;0;Create;True;0;0;0;False;0;False;0;0.003;0;0.1;0;1;FLOAT;0
Node;AmplifyShaderEditor.FunctionNode;135;-3072,-1152;Inherit;False;ConstantBiasScale;-1;;2;63208df05c83e8e49a48ffbdce2e43a0;0;3;3;FLOAT2;0,0;False;1;FLOAT;-0.5;False;2;FLOAT;2;False;1;FLOAT2;0
Node;AmplifyShaderEditor.Vector2Node;132;-3968,-1472;Inherit;False;Property;_MaskUVPan;Mask UV Pan;5;0;Create;True;0;0;0;False;0;False;0,0;0,0;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.Vector2Node;134;-3584,-1408;Inherit;False;Constant;_Vector0;Vector 0;8;0;Create;True;0;0;0;False;0;False;0,0;0,0;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.PannerNode;140;-3968,-1600;Inherit;False;3;0;FLOAT2;0,0;False;2;FLOAT2;0,0;False;1;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.LerpOp;139;-3200,-1408;Inherit;False;3;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.CommentaryNode;225;-3762,-2610;Inherit;False;932;802.95;Pixelate;9;117;116;121;119;120;115;118;38;224;Pixelate;0,0,0,1;0;0
Node;AmplifyShaderEditor.SimpleAddOpNode;145;-3136,-1616;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode;121;-3712,-1920;Inherit;False;Property;_PixelsMultiplier;Pixels Multiplier;25;0;Create;True;0;0;0;False;0;False;1;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;117;-3712,-2048;Inherit;False;Property;_PixelsY;Pixels Y;27;0;Create;True;0;0;0;False;0;False;32;128;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;116;-3712,-2176;Inherit;False;Property;_PixelsX;Pixels X;26;0;Create;True;0;0;0;False;0;False;32;128;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;149;-2976,-1616;Inherit;False;PrePixelateUVs;-1;True;1;0;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;119;-3456,-2176;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;120;-3456,-2048;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.CommentaryNode;142;-4704,208;Inherit;False;1759;1201.453;DisolveMaping;24;157;165;169;166;215;214;199;196;198;197;194;205;195;200;203;171;204;208;201;156;216;217;221;230;;0.1037736,0.1037736,0.1037736,1;0;0
Node;AmplifyShaderEditor.GetLocalVarNode;38;-3712,-2560;Inherit;False;149;PrePixelateUVs;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.TFHCPixelate;115;-3712,-2304;Inherit;False;3;0;FLOAT2;0,0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.GetLocalVarNode;221;-4352,1024;Inherit;False;220;FlipbookFrames;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;230;-4608,896;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.StaticSwitch;118;-3456,-2560;Inherit;False;Property;_Pixelate;Pixelate;24;0;Create;True;0;0;0;False;3;Space(33);Header(Pixelate);Space(13);False;0;0;0;True;;Toggle;2;Key0;Key1;Create;True;True;All;9;1;FLOAT2;0,0;False;0;FLOAT2;0,0;False;2;FLOAT2;0,0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT2;0,0;False;6;FLOAT2;0,0;False;7;FLOAT2;0,0;False;8;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;156;-4352,896;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode;157;-4096,1280;Inherit;False;Property;_ErosionNoiseUVScaleOverall;Erosion Noise UV Scale Overall;15;0;Create;True;0;0;0;False;0;False;1;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.CommentaryNode;36;-2416,-1728;Inherit;False;1896;1857.979;Color;20;48;45;112;111;106;105;101;97;96;93;92;88;52;47;185;186;189;190;191;226;;0,0,0,1;0;0
Node;AmplifyShaderEditor.TexCoordVertexDataNode;148;-4608,-192;Inherit;False;0;4;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RegisterLocalVarNode;224;-3072,-2560;Inherit;False;FinalUVs;-1;True;1;0;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.Vector2Node;217;-3968,1152;Inherit;False;Property;_ErosionNoiseUVPan;Erosion Noise UV Pan;16;0;Create;True;0;0;0;False;0;False;0,0;0,0;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;165;-4096,896;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode;189;-1792,0;Inherit;False;Property;_AlphaGlow;Alpha Glow;13;0;Create;True;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;158;-4224,-192;Inherit;False;Opacity_VTC_W;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.PannerNode;216;-3968,896;Inherit;False;3;0;FLOAT2;0,0;False;2;FLOAT2;0,0;False;1;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.GetLocalVarNode;166;-3712,1152;Inherit;False;159;Random Offset;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;226;-2304,-640;Inherit;False;224;FinalUVs;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;231;-2944,-1408;Inherit;False;DistortUVs;-1;True;1;0;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SaturateNode;190;-1536,0;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;201;-3840,512;Inherit;False;158;Opacity_VTC_W;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;185;-1792,-256;Inherit;True;Property;_AlphaTexture;Alpha Texture;12;0;Create;True;0;0;0;False;3;Space(33);Header(Alpha Texture);Space(13);False;-1;f793340976f7177468341cc3d511ef13;9c17fb8391ded4342a4304a61ef53ff1;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.SimpleAddOpNode;169;-3584,896;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.GetLocalVarNode;215;-3456,1152;Inherit;False;231;DistortUVs;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.LerpOp;186;-1408,-256;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;208;-3584,512;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;204;-3840,640;Inherit;False;Property;_ErosionSmoothness;Erosion Smoothness;18;0;Create;True;0;0;0;False;0;False;1;0.03;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;214;-3456,896;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SaturateNode;191;-1280,-256;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;171;-3328,896;Inherit;True;Property;_ErosionNoise;Erosion Noise;14;0;Create;True;0;0;0;False;3;Space(33);Header(Erosion Noise);Space(13);False;-1;4a897e6098e8f804a9ec44f2426669c2;4a897e6098e8f804a9ec44f2426669c2;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.SimpleAddOpNode;203;-3456,512;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;48;-1024,-256;Inherit;False;mainTex_alpha;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SmoothstepOpNode;200;-3456,384;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;195;-4608,384;Inherit;False;48;mainTex_alpha;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;205;-3200,384;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;194;-4352,640;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.VertexColorNode;47;-1280,-1024;Inherit;False;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SaturateNode;197;-4096,640;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;198;-4224,512;Inherit;False;Property;_ErosionIntensity;Erosion Intensity;17;0;Create;True;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;92;-2304,-1152;Inherit;False;Property;_B;B;8;1;[HDR];Create;True;0;0;0;False;0;False;0.5943396,0.259371,0.09812209,0;9.082411,9.082411,9.082411,0;True;True;0;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.ColorNode;93;-2304,-896;Inherit;False;Property;_Outline;Outline;9;1;[HDR];Create;True;0;0;0;False;0;False;0.2169811,0.03320287,0.02354041,0;0,0,0,0;True;True;0;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.SamplerNode;45;-1792,-640;Inherit;True;Property;_MaskTexture;Mask Texture;3;0;Create;True;0;0;0;False;3;Space(33);Header(Mask Texture);Space(13);False;-1;b1b69c961c2e3854798854b5df548783;567c431e9acb8664088227faa98bcdc6;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.RegisterLocalVarNode;52;-1280,-768;Inherit;False;mainTex_VC_alha;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;196;-4224,384;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;96;-2304,-1408;Inherit;False;Property;_G;G;7;1;[HDR];Create;True;0;0;0;False;0;False;1,0.7230805,0.25,0;1.976675,1.976675,1.976675,1;True;True;0;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.LerpOp;97;-1792,-1280;Inherit;True;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.GetLocalVarNode;175;-3840,0;Inherit;False;52;mainTex_VC_alha;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;199;-3968,384;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;212;-3456,128;Inherit;False;Property;_DepthFade;Depth Fade;19;0;Create;True;0;0;0;False;0;False;0.1;0.1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;112;-1408,-1408;Inherit;True;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.ColorNode;101;-2304,-1664;Inherit;False;Property;_R;R;6;1;[HDR];Create;True;0;0;0;False;0;False;1,0.9719134,0.5896226,0;0.06666667,0.06666667,0.06666667,1;True;True;0;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;177;-3840,-128;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.DepthFade;211;-3456,0;Inherit;False;True;True;False;2;1;FLOAT3;0,0,0;False;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;111;-1152,-1664;Inherit;True;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SaturateNode;207;-3712,-128;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;213;-3200,0;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;105;-768,-1152;Inherit;True;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;106;-768,-384;Inherit;False;Property;_FlatColor;Flat Color;10;0;Create;True;0;0;0;False;0;False;0;0;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;209;-3456,-128;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;88;-768,-640;Inherit;True;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;184;-384,-512;Inherit;False;Property;_Emissive;Emissive;11;0;Create;True;0;0;0;False;0;False;1;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;210;-3328,-128;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;180;-384,-640;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.CommentaryNode;63;336,-48;Inherit;False;1243;166;AR;5;110;80;78;82;83;;0,0,0,1;0;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;179;-3200,-128;Inherit;False;OpacityRegister;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;183;-128,-640;Inherit;False;color;-1;True;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;78;640,0;Inherit;False;Property;_Src;Src;29;0;Create;True;0;0;0;True;0;False;5;5;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;110;896,0;Inherit;False;Property;_Dst;Dst;30;0;Create;True;0;0;0;True;0;False;10;10;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;82;1408,0;Inherit;False;Property;_ZTest;ZTest;32;0;Create;True;0;0;0;True;0;False;2;2;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;83;1152,0;Inherit;False;Property;_ZWrite;ZWrite;31;0;Create;True;0;0;0;True;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;181;-384,0;Inherit;False;183;color;1;0;OBJECT;;False;1;COLOR;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;152;-4224,-112;Inherit;False;VTC_T;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;73;-384,128;Inherit;False;179;OpacityRegister;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;80;384,0;Inherit;False;Property;_Cull;Cull;28;0;Create;True;0;0;0;True;3;Space(33);Header(AR);Space(13);False;2;2;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;233;0,0;Float;False;True;-1;3;ASEMaterialInspector;0;0;Unlit;Vefects/SH_Vefects_BIRP_Unlit_Combat_Flipbook_Advanced_01;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;Back;0;True;_ZWrite;0;True;_ZTest;False;0;False;;0;False;;False;0;Custom;0.5;True;False;0;True;Transparent;;Transparent;All;12;all;True;True;True;True;0;False;;False;0;False;;255;False;;255;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;False;2;15;10;25;False;0.5;False;1;5;True;_Src;10;True;_Dst;0;0;False;;0;False;;0;False;;0;False;;0;False;0;0,0,0,0;VertexOffset;True;False;Cylindrical;False;True;Relative;0;;0;-1;-1;-1;0;False;0;0;True;_Cull;-1;0;False;;0;0;0;False;0.1;False;;0;False;;False;16;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT;0;False;4;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;16;FLOAT4;0,0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
Node;AmplifyShaderEditor.CommentaryNode;113;336,-224;Inherit;False;304;100;Ge Lush was here! <3;0;Ge Lush was here! <3;0,0,0,1;0;0
WireConnection;151;0;137;0
WireConnection;151;1;138;0
WireConnection;220;0;151;0
WireConnection;125;0;123;0
WireConnection;125;1;124;0
WireConnection;159;0;153;1
WireConnection;219;0;125;0
WireConnection;219;1;223;0
WireConnection;127;0;219;0
WireConnection;127;2;126;0
WireConnection;192;0;127;0
WireConnection;192;1;193;0
WireConnection;128;1;192;0
WireConnection;129;0;128;0
WireConnection;136;0;131;0
WireConnection;136;1;130;0
WireConnection;135;3;129;0
WireConnection;140;0;136;0
WireConnection;140;2;132;0
WireConnection;139;0;134;0
WireConnection;139;1;135;0
WireConnection;139;2;133;0
WireConnection;145;0;140;0
WireConnection;145;1;139;0
WireConnection;149;0;145;0
WireConnection;119;0;116;0
WireConnection;119;1;121;0
WireConnection;120;0;117;0
WireConnection;120;1;121;0
WireConnection;115;0;38;0
WireConnection;115;1;119;0
WireConnection;115;2;120;0
WireConnection;118;1;38;0
WireConnection;118;0;115;0
WireConnection;156;0;230;0
WireConnection;156;1;221;0
WireConnection;224;0;118;0
WireConnection;165;0;156;0
WireConnection;165;1;157;0
WireConnection;158;0;148;3
WireConnection;216;0;165;0
WireConnection;216;2;217;0
WireConnection;231;0;139;0
WireConnection;190;0;189;0
WireConnection;185;1;226;0
WireConnection;169;0;216;0
WireConnection;169;1;166;0
WireConnection;186;0;185;2
WireConnection;186;1;185;1
WireConnection;186;2;190;0
WireConnection;208;0;201;0
WireConnection;214;0;169;0
WireConnection;214;1;215;0
WireConnection;191;0;186;0
WireConnection;171;1;214;0
WireConnection;203;0;208;0
WireConnection;203;1;204;0
WireConnection;48;0;191;0
WireConnection;200;0;171;2
WireConnection;200;1;208;0
WireConnection;200;2;203;0
WireConnection;205;0;200;0
WireConnection;194;0;195;0
WireConnection;194;1;205;0
WireConnection;197;0;194;0
WireConnection;45;1;226;0
WireConnection;52;0;47;4
WireConnection;196;0;195;0
WireConnection;196;1;197;0
WireConnection;196;2;198;0
WireConnection;97;0;93;0
WireConnection;97;1;92;0
WireConnection;97;2;45;3
WireConnection;199;0;196;0
WireConnection;112;0;97;0
WireConnection;112;1;96;0
WireConnection;112;2;45;2
WireConnection;177;0;199;0
WireConnection;177;1;175;0
WireConnection;211;0;212;0
WireConnection;111;0;112;0
WireConnection;111;1;101;0
WireConnection;111;2;45;1
WireConnection;207;0;177;0
WireConnection;213;0;211;0
WireConnection;105;0;47;0
WireConnection;105;1;111;0
WireConnection;209;0;207;0
WireConnection;209;1;213;0
WireConnection;88;0;105;0
WireConnection;88;1;47;0
WireConnection;88;2;106;0
WireConnection;210;0;209;0
WireConnection;180;0;88;0
WireConnection;180;1;184;0
WireConnection;179;0;210;0
WireConnection;183;0;180;0
WireConnection;152;0;148;4
WireConnection;233;2;181;0
WireConnection;233;9;73;0
ASEEND*/
//CHKSM=F37864776FF27FAD339D3CF61E781F5E4DCEA1FA