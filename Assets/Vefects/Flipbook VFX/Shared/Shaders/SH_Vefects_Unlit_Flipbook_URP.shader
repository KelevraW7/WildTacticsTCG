// Made with Amplify Shader Editor v1.9.7.1
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "Vefects/SH_Vefects_Unlit_Flipbook_URP"
{
	Properties
	{
		[Space(13)][Header(Main Texture)][Space(13)]_MainTexture("Main Texture", 2D) = "white" {}
		_UVS("UV S", Vector) = (1,1,0,0)
		_UVP("UV P", Vector) = (0,0,0,0)
		[HDR]_R("R", Color) = (1,0.9719134,0.5896226,0)
		[HDR]_G("G", Color) = (1,0.7230805,0.25,0)
		[HDR]_B("B", Color) = (0.5943396,0.259371,0.09812209,0)
		[HDR]_Outline("Outline", Color) = (0.2169811,0.03320287,0.02354041,0)
		[Space(13)][Header(DisolveMapping)][Space(13)]_disolveMap("disolveMap", 2D) = "white" {}
		[Header(TextureProps)][Space(13)]_Intensity("Intensity", Range( 0 , 5)) = 1
		_ErosionSmoothness("Erosion Smoothness", Range( 0.1 , 15)) = 0.1
		_FlatColor("Flat Color", Range( 0 , 1)) = 0
		_UVDS1("UV D S", Vector) = (1,1,0,0)
		[Space(13)][Header(Distortion)][Space(13)]_DistortionTexture("Distortion Texture", 2D) = "white" {}
		_UVDP1("UV D P", Vector) = (0.1,-0.2,0,0)
		_DistortionLerp("Distortion Lerp", Range( 0 , 0.1)) = 0
		[Header(SecondDistortion)][Space(13)]_DistortionSecond("DistortionSecond", 2D) = "white" {}
		_SecondDistortionLerp("SecondDistortionLerp", Range( 0.5 , 1)) = 0.5
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
		Cull [_Cull]
		ZWrite [_ZWrite]
		ZTest [_ZTest]
		Blend [_Src] [_Dst]
		CGPROGRAM
		#include "UnityShaderVariables.cginc"
		#pragma target 3.0
		#pragma shader_feature_local _PIXELATE_ON
		#define ASE_VERSION 19701
		#pragma surface surf Unlit keepalpha noshadow 
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
		uniform float _Intensity;
		uniform sampler2D _DistortionSecond;
		uniform float2 _UVDP1;
		uniform float2 _UVDS1;
		uniform float _SecondDistortionLerp;
		uniform float _ErosionSmoothness;
		uniform sampler2D _disolveMap;
		uniform float4 _disolveMap_ST;

		inline half4 LightingUnlit( SurfaceOutput s, half3 lightDir, half atten )
		{
			return half4 ( 0, 0, 0, s.Alpha );
		}

		void surf( Input i , inout SurfaceOutput o )
		{
			float2 panner27 = ( 1.0 * _Time.y * _UVP + ( i.uv_texcoord.xy * _UVS ));
			float2 panner15 = ( 1.0 * _Time.y * _UVDP + ( i.uv_texcoord.xy * _UVDS ));
			float2 lerpResult26 = lerp( float2( 0,0 ) , ( ( (tex2D( _DistortionTexture, panner15 )).rg + -0.5 ) * 2.0 ) , _DistortionLerp);
			float2 DistortionRegister34 = ( panner27 + lerpResult26 );
			float pixelWidth115 =  1.0f / ( _PixelsX * _PixelsMultiplier );
			float pixelHeight115 = 1.0f / ( _PixelsY * _PixelsMultiplier );
			half2 pixelateduv115 = half2((int)(DistortionRegister34.x / pixelWidth115) * pixelWidth115, (int)(DistortionRegister34.y / pixelHeight115) * pixelHeight115);
			#ifdef _PIXELATE_ON
				float2 staticSwitch118 = pixelateduv115;
			#else
				float2 staticSwitch118 = DistortionRegister34;
			#endif
			float4 tex2DNode45 = tex2D( _MainTexture, staticSwitch118 );
			float4 lerpResult97 = lerp( _Outline , _B , tex2DNode45.b);
			float4 lerpResult112 = lerp( lerpResult97 , _G , tex2DNode45.g);
			float4 lerpResult111 = lerp( lerpResult112 , _R , tex2DNode45.r);
			float4 lerpResult88 = lerp( ( i.vertexColor * lerpResult111 ) , i.vertexColor , _FlatColor);
			float2 panner71 = ( 1.0 * _Time.y * _UVDP1 + ( i.uv_texcoord.xy * _UVDS1 ));
			float4 SecondDistortion108 = ( tex2D( _DistortionSecond, panner71 ) + _SecondDistortionLerp );
			o.Emission = ( ( lerpResult88 * _Intensity ) * SecondDistortion108 ).rgb;
			float mainTex_alpha48 = tex2DNode45.a;
			float smoothstepResult55 = smoothstep( i.uv_texcoord.z , ( i.uv_texcoord.z + _ErosionSmoothness ) , mainTex_alpha48);
			float mainTex_VC_alha52 = i.vertexColor.a;
			float Opacity_VTC_W33 = i.uv_texcoord.z;
			float Opacity_VTC_T30 = i.uv_texcoord.w;
			float temp_output_44_0 = (( Opacity_VTC_T30 - 1.0 ) + (Opacity_VTC_W33 - 0.0) * (1.0 - ( Opacity_VTC_T30 - 1.0 )) / (1.0 - 0.0));
			float2 uv_disolveMap = i.uv_texcoord * _disolveMap_ST.xy + _disolveMap_ST.zw;
			float smoothstepResult54 = smoothstep( temp_output_44_0 , ( temp_output_44_0 + Opacity_VTC_T30 ) , tex2D( _disolveMap, uv_disolveMap ).r);
			float disolveMapping56 = smoothstepResult54;
			float OpacityRegister61 = ( ( smoothstepResult55 * mainTex_VC_alha52 ) * disolveMapping56 );
			o.Alpha = OpacityRegister61;
		}

		ENDCG
	}
	CustomEditor "ASEMaterialInspector"
}
/*ASEBEGIN
Version=19701
Node;AmplifyShaderEditor.CommentaryNode;10;-4480,-1728;Inherit;False;1992;995;Distortion;18;34;31;27;26;25;24;23;22;20;19;18;17;16;15;14;13;12;11;;0,0,0,1;0;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;11;-4416,-1040;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.Vector2Node;12;-4160,-912;Inherit;False;Property;_UVDS;UV D S;18;0;Create;True;0;0;0;False;0;False;1,1;1,1;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;13;-4160,-1040;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.Vector2Node;14;-3904,-912;Inherit;False;Property;_UVDP;UV D P;19;0;Create;True;0;0;0;False;0;False;0.1,-0.2;0.1,-0.2;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.PannerNode;15;-3904,-1040;Inherit;False;3;0;FLOAT2;0,0;False;2;FLOAT2;0,0;False;1;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SamplerNode;16;-3648,-1040;Inherit;True;Property;_DistortionTexture;Distortion Texture;13;0;Create;True;0;0;0;False;3;Space(13);Header(Distortion);Space(13);False;-1;98c3d568d9032a34eb5b038e20fea05d;98c3d568d9032a34eb5b038e20fea05d;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.ComponentMaskNode;17;-3264,-1040;Inherit;False;True;True;False;False;1;0;COLOR;0,0,0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.Vector2Node;18;-3520,-1552;Inherit;False;Property;_UVS;UV S;2;0;Create;True;0;0;0;False;0;False;1,1;1,1;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.TextureCoordinatesNode;19;-3776,-1680;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.Vector2Node;20;-3264,-1552;Inherit;False;Property;_UVP;UV P;3;0;Create;True;0;0;0;False;0;False;0,0;0,0;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.RangedFloatNode;22;-3264,-1168;Inherit;False;Property;_DistortionLerp;Distortion Lerp;15;0;Create;True;0;0;0;False;0;False;0;0;0;0.1;0;1;FLOAT;0
Node;AmplifyShaderEditor.Vector2Node;23;-3264,-1296;Inherit;False;Constant;_Vector0;Vector 0;8;0;Create;True;0;0;0;False;0;False;0,0;0,0;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.FunctionNode;24;-3008,-1040;Inherit;False;ConstantBiasScale;-1;;1;63208df05c83e8e49a48ffbdce2e43a0;0;3;3;FLOAT2;0,0;False;1;FLOAT;-0.5;False;2;FLOAT;2;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;25;-3520,-1680;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.LerpOp;26;-2880,-1296;Inherit;False;3;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.PannerNode;27;-3264,-1680;Inherit;False;3;0;FLOAT2;0,0;False;2;FLOAT2;0,0;False;1;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.CommentaryNode;21;-4464,-320;Inherit;False;1538.791;442.8129;Opacity;12;61;60;59;58;57;55;53;51;46;33;30;28;;0,0,0,1;0;0
Node;AmplifyShaderEditor.SimpleAddOpNode;31;-2880,-1680;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.CommentaryNode;36;-2416,-1728;Inherit;False;1896;1857.979;Color;23;48;45;84;112;111;106;105;101;97;96;93;92;88;52;47;38;115;116;117;118;119;120;121;;0,0,0,1;0;0
Node;AmplifyShaderEditor.TexCoordVertexDataNode;28;-4432,-208;Inherit;False;0;4;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RegisterLocalVarNode;34;-2720,-1680;Inherit;False;DistortionRegister;-1;True;1;0;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode;117;-2304,-128;Inherit;False;Property;_PixelsY;Pixels Y;27;0;Create;True;0;0;0;False;0;False;32;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;116;-2304,-256;Inherit;False;Property;_PixelsX;Pixels X;26;0;Create;True;0;0;0;False;0;False;32;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;121;-2304,0;Inherit;False;Property;_PixelsMultiplier;Pixels Multiplier;25;0;Create;True;0;0;0;False;0;False;1;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.CommentaryNode;29;-4448,144;Inherit;False;1486.067;526.0999;DisolveMaping;13;56;54;50;49;44;43;42;41;40;39;37;35;32;;0.1037736,0.1037736,0.1037736,1;0;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;30;-4112,48;Inherit;False;Opacity_VTC_T;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;38;-2304,-640;Inherit;False;34;DistortionRegister;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;119;-2048,-256;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;120;-2048,-128;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;32;-4400,432;Inherit;False;Constant;_Float2;Float 2;20;0;Create;True;0;0;0;False;0;False;1;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;33;-3936,-32;Inherit;False;Opacity_VTC_W;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;35;-4432,272;Inherit;False;30;Opacity_VTC_T;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.TFHCPixelate;115;-2304,-384;Inherit;False;3;0;FLOAT2;0,0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode;37;-4240,272;Inherit;False;Constant;_Float0;Float 0;20;0;Create;True;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;39;-4240,432;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;40;-4224,352;Inherit;False;Constant;_Float1;Float 1;20;0;Create;True;0;0;0;False;0;False;1;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;41;-4112,192;Inherit;False;33;Opacity_VTC_W;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;42;-4224,528;Inherit;False;Constant;_Float3;Float 3;20;0;Create;True;0;0;0;False;0;False;1;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.StaticSwitch;118;-2048,-640;Inherit;False;Property;_Pixelate;Pixelate;24;0;Create;True;0;0;0;False;3;Space(33);Header(Pixelate);Space(13);False;0;0;0;True;;Toggle;2;Key0;Key1;Create;True;True;All;9;1;FLOAT2;0,0;False;0;FLOAT2;0,0;False;2;FLOAT2;0,0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT2;0,0;False;6;FLOAT2;0,0;False;7;FLOAT2;0,0;False;8;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.CommentaryNode;65;-4464,-704;Inherit;False;1665.348;371.0714;SecondDistortion;9;108;104;103;102;98;95;94;85;71;;0,0,0,1;0;0
Node;AmplifyShaderEditor.GetLocalVarNode;43;-3920,192;Inherit;False;30;Opacity_VTC_T;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.TFHCRemapNode;44;-3984,288;Inherit;False;5;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;3;FLOAT;0;False;4;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;45;-1792,-640;Inherit;True;Property;_MainTexture;Main Texture;1;0;Create;True;0;0;0;False;3;Space(13);Header(Main Texture);Space(13);False;-1;5e9cda599296bd74a9a45a7b3a63c0a9;5e9cda599296bd74a9a45a7b3a63c0a9;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.ColorNode;92;-2304,-1152;Inherit;False;Property;_B;B;6;1;[HDR];Create;True;0;0;0;False;0;False;0.5943396,0.259371,0.09812209,0;0.2641509,0.2616589,0.2554289,0;True;True;0;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.ColorNode;93;-2304,-896;Inherit;False;Property;_Outline;Outline;7;1;[HDR];Create;True;0;0;0;False;0;False;0.2169811,0.03320287,0.02354041,0;0,0,0,0;True;True;0;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.Vector2Node;94;-4160,-496;Inherit;False;Property;_UVDS1;UV D S;12;0;Create;True;0;0;0;False;0;False;1,1;1,1;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.TextureCoordinatesNode;95;-4416,-624;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;46;-4160,-208;Inherit;False;Property;_ErosionSmoothness;Erosion Smoothness;10;0;Create;True;0;0;0;False;0;False;0.1;1.57;0.1;15;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;49;-3712,224;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;50;-4016,448;Inherit;True;Property;_disolveMap;disolveMap;8;0;Create;True;0;0;0;False;3;Space(13);Header(DisolveMapping);Space(13);False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.VertexColorNode;47;-1280,-1024;Inherit;False;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RegisterLocalVarNode;48;-1408,-640;Inherit;False;mainTex_alpha;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.Vector2Node;85;-3904,-496;Inherit;False;Property;_UVDP1;UV D P;14;0;Create;True;0;0;0;False;0;False;0.1,-0.2;0.1,-0.2;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.ColorNode;96;-2304,-1408;Inherit;False;Property;_G;G;5;1;[HDR];Create;True;0;0;0;False;0;False;1,0.7230805,0.25,0;1,0.3523919,0,0;True;True;0;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.LerpOp;97;-1792,-1280;Inherit;True;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;98;-4160,-624;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleAddOpNode;51;-3856,-128;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;53;-3968,-272;Inherit;False;48;mainTex_alpha;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.SmoothstepOpNode;54;-3520,336;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;52;-1280,-768;Inherit;False;mainTex_VC_alha;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.PannerNode;71;-3904,-624;Inherit;False;3;0;FLOAT2;0,0;False;2;FLOAT2;0,0;False;1;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.ColorNode;101;-2304,-1664;Inherit;False;Property;_R;R;4;1;[HDR];Create;True;0;0;0;False;0;False;1,0.9719134,0.5896226,0;0.3679245,0.3679245,0.3679245,0;True;True;0;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.LerpOp;112;-1408,-1408;Inherit;True;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SmoothstepOpNode;55;-3728,-192;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;56;-3328,336;Inherit;False;disolveMapping;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;57;-3776,-272;Inherit;False;52;mainTex_VC_alha;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;102;-3552,-656;Inherit;True;Property;_DistortionSecond;DistortionSecond;16;1;[Header];Create;True;1;SecondDistortion;0;0;False;1;Space(13);False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.RangedFloatNode;103;-3536,-448;Inherit;False;Property;_SecondDistortionLerp;SecondDistortionLerp;17;0;Create;True;0;0;0;False;0;False;0.5;0;0.5;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;111;-1152,-1664;Inherit;True;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;58;-3536,-208;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;59;-3600,0;Inherit;False;56;disolveMapping;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;104;-3232,-512;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;105;-768,-1152;Inherit;True;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;106;-1152,-384;Inherit;False;Property;_FlatColor;Flat Color;11;0;Create;True;0;0;0;False;0;False;0;0;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;60;-3360,-128;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;88;-768,-640;Inherit;True;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;107;-768,-128;Inherit;False;Property;_Intensity;Intensity;9;1;[Header];Create;True;1;TextureProps;0;0;False;1;Space(13);False;1;1;0;5;0;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;108;-3024,-512;Inherit;False;SecondDistortion;-1;True;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.CommentaryNode;62;-4448,704;Inherit;False;1354.227;543.6159;VertexDisplacement;10;109;100;87;86;79;77;76;75;74;72;;0,0,0,1;0;0
Node;AmplifyShaderEditor.CommentaryNode;63;336,-48;Inherit;False;1243;166;AR;5;110;80;78;82;83;;0,0,0,1;0;0
Node;AmplifyShaderEditor.CommentaryNode;64;-2272,448;Inherit;False;1194.858;412.5891;Depth Fade;7;91;89;70;69;68;67;66;;0,0,0,1;0;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;61;-3120,-128;Inherit;False;OpacityRegister;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;99;-384,-256;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.GetLocalVarNode;84;-768,0;Inherit;False;108;SecondDistortion;1;0;OBJECT;;False;1;COLOR;0
Node;AmplifyShaderEditor.ScreenDepthNode;66;-2016,496;Inherit;False;1;True;1;0;FLOAT4;0,0,0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;67;-1776,704;Inherit;False;Property;_Float4;Float 4;23;0;Create;True;0;0;0;False;0;False;0;0;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;68;-1328,608;Inherit;True;DepthFadeRegister;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ScreenPosInputsNode;69;-2224,512;Float;False;1;False;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;70;-1472,608;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.NormalVertexDataNode;72;-4224,752;Inherit;False;0;5;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RegisterLocalVarNode;74;-3344,864;Inherit;False;VertexDisplacement;-1;True;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.TFHCRemapNode;75;-3552,1008;Inherit;False;5;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;COLOR;1,1,1,1;False;3;COLOR;0,0,0,0;False;4;COLOR;1,1,1,1;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleTimeNode;76;-4336,928;Inherit;False;1;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;77;-4432,1024;Inherit;False;Property;_VertexDistortion_Speed;VertexDistortion_Speed;22;0;Create;True;0;0;0;False;0;False;0;0;0;10;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;79;-3792,1056;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;81;-768,256;Inherit;False;74;VertexDisplacement;1;0;OBJECT;;False;1;COLOR;0
Node;AmplifyShaderEditor.SamplerNode;86;-3872,832;Inherit;True;Property;_VertexDistortionNoise_tex;VertexDistortionNoise_tex;20;0;Create;True;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.SimpleAddOpNode;87;-3984,880;Inherit;False;2;2;0;FLOAT3;0,0,0;False;1;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;89;-1792,592;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;90;-384,0;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.AbsOpNode;91;-1616,608;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;100;-4096,1136;Inherit;False;Property;_VertexDistortion_Scale;VertexDistortion_Scale;21;0;Create;True;0;0;0;False;0;False;0;0;-0.1;0.25;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;109;-4144,960;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;78;640,0;Inherit;False;Property;_Src;Src;29;0;Create;True;0;0;0;True;0;False;5;5;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;110;896,0;Inherit;False;Property;_Dst;Dst;30;0;Create;True;0;0;0;True;0;False;10;10;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;82;1408,0;Inherit;False;Property;_ZTest;ZTest;32;0;Create;True;0;0;0;True;0;False;2;2;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;83;1152,0;Inherit;False;Property;_ZWrite;ZWrite;31;0;Create;True;0;0;0;True;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;73;-768,128;Inherit;False;61;OpacityRegister;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;80;384,0;Inherit;False;Property;_Cull;Cull;28;0;Create;True;0;0;0;True;3;Space(13);Header(AR);Space(13);False;2;2;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;122;0,0;Float;False;True;-1;2;ASEMaterialInspector;0;0;Unlit;Vefects/SH_Vefects_Unlit_Flipbook_URP;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;Back;0;True;_ZWrite;0;True;_ZTest;False;0;False;;0;False;;False;0;Custom;0.5;True;False;0;False;Transparent;;Transparent;All;12;all;True;True;True;True;0;False;;False;0;False;;255;False;;255;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;False;2;15;10;25;False;0.5;False;2;5;True;_Src;10;True;_Dst;0;0;False;;0;False;;0;False;;0;False;;0;False;0;0,0,0,0;VertexOffset;True;False;Cylindrical;False;True;Relative;0;;0;-1;-1;-1;0;False;0;0;True;_Cull;-1;0;False;;0;0;0;False;0.1;False;;0;False;;False;16;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT;0;False;4;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;16;FLOAT4;0,0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
Node;AmplifyShaderEditor.CommentaryNode;113;336,-224;Inherit;False;304;100;Lush was here! <3;0;Lush was here! <3;0,0,0,1;0;0
WireConnection;13;0;11;0
WireConnection;13;1;12;0
WireConnection;15;0;13;0
WireConnection;15;2;14;0
WireConnection;16;1;15;0
WireConnection;17;0;16;0
WireConnection;24;3;17;0
WireConnection;25;0;19;0
WireConnection;25;1;18;0
WireConnection;26;0;23;0
WireConnection;26;1;24;0
WireConnection;26;2;22;0
WireConnection;27;0;25;0
WireConnection;27;2;20;0
WireConnection;31;0;27;0
WireConnection;31;1;26;0
WireConnection;34;0;31;0
WireConnection;30;0;28;4
WireConnection;119;0;116;0
WireConnection;119;1;121;0
WireConnection;120;0;117;0
WireConnection;120;1;121;0
WireConnection;33;0;28;3
WireConnection;115;0;38;0
WireConnection;115;1;119;0
WireConnection;115;2;120;0
WireConnection;39;0;35;0
WireConnection;39;1;32;0
WireConnection;118;1;38;0
WireConnection;118;0;115;0
WireConnection;44;0;41;0
WireConnection;44;1;37;0
WireConnection;44;2;40;0
WireConnection;44;3;39;0
WireConnection;44;4;42;0
WireConnection;45;1;118;0
WireConnection;49;0;44;0
WireConnection;49;1;43;0
WireConnection;48;0;45;4
WireConnection;97;0;93;0
WireConnection;97;1;92;0
WireConnection;97;2;45;3
WireConnection;98;0;95;0
WireConnection;98;1;94;0
WireConnection;51;0;28;3
WireConnection;51;1;46;0
WireConnection;54;0;50;1
WireConnection;54;1;44;0
WireConnection;54;2;49;0
WireConnection;52;0;47;4
WireConnection;71;0;98;0
WireConnection;71;2;85;0
WireConnection;112;0;97;0
WireConnection;112;1;96;0
WireConnection;112;2;45;2
WireConnection;55;0;53;0
WireConnection;55;1;28;3
WireConnection;55;2;51;0
WireConnection;56;0;54;0
WireConnection;102;1;71;0
WireConnection;111;0;112;0
WireConnection;111;1;101;0
WireConnection;111;2;45;1
WireConnection;58;0;55;0
WireConnection;58;1;57;0
WireConnection;104;0;102;0
WireConnection;104;1;103;0
WireConnection;105;0;47;0
WireConnection;105;1;111;0
WireConnection;60;0;58;0
WireConnection;60;1;59;0
WireConnection;88;0;105;0
WireConnection;88;1;47;0
WireConnection;88;2;106;0
WireConnection;108;0;104;0
WireConnection;61;0;60;0
WireConnection;99;0;88;0
WireConnection;99;1;107;0
WireConnection;66;0;69;0
WireConnection;68;0;70;0
WireConnection;70;0;91;0
WireConnection;70;1;67;0
WireConnection;74;0;75;0
WireConnection;75;0;86;0
WireConnection;75;3;79;0
WireConnection;75;4;100;0
WireConnection;79;0;100;0
WireConnection;86;1;87;0
WireConnection;87;0;72;0
WireConnection;87;1;109;0
WireConnection;89;0;66;0
WireConnection;89;1;69;4
WireConnection;90;0;99;0
WireConnection;90;1;84;0
WireConnection;91;0;89;0
WireConnection;109;0;76;0
WireConnection;109;1;77;0
WireConnection;122;2;90;0
WireConnection;122;9;73;0
ASEEND*/
//CHKSM=3406E42CA7A9A67535EA7FA97D0BACD9DB92827D