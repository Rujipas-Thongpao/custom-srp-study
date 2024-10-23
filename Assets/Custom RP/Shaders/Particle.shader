Shader "Custom RP/Particle/Unlit" {
	
	Properties {
		_BaseMap("Texture", 2D) = "white" {}
		[HDR] _BaseColor("Color", Color) = (1.0, 1.0, 1.0, 1.0)
		_Cutoff ("Alpha Cutoff", Range(0.0, 1.0)) = 0.5
		[Toggle(_CLIPPING)] _Clipping ("Alpha Clipping", Float) = 0
		[Toggle(_VERTEX_COLORS)] _VertexColor("Vertex Color", Float) = 0
		[Toggle(_FLIPBOOK_BLENDING)] _FlipbookBlending ("Flipbook blending", Float) = 0

		[Toggle(_NEAR_FADE)] _NearFade("Near Fade", Float) = 1
		_NearFadeDistance("Near Fade Distance", Range(0.01, 10.)) = 1
		_NearFadeRange("Near Fade Range", Range(0.01, 10.)) = 1

		[Toggle(_SOFT_PARTICLE)] _SoftParticle("Soft Particle", Float) = 1
		_SoftParticleDistance("Soft Particle Distance", Range(0.01, 10.)) = 1
		_SoftParticleRange("Soft Particle Range", Range(0.01, 10.)) = 1

		[KeywordEnum(On, Clip, Dither, Off)] _Shadows ("Shadows", Float) = 0

		[Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Src Blend", Float) = 1
		[Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Dst Blend", Float) = 0
		[Enum(Off, 0, On, 1)] _ZWrite ("Z Write", Float) = 1
	}
	
	SubShader {
		HLSLINCLUDE
			#include "../ShaderLibrary/Common.hlsl"
			#include "UnlitInput.hlsl"

		ENDHLSL
		Pass {
			Blend [_SrcBlend] [_DstBlend]
			ZWrite [_ZWrite]

			HLSLPROGRAM
			#pragma target 3.5
			#pragma shader_feature _CLIPPING
			#pragma shader_feature _VERTEX_COLORS
			#pragma shader_feature _FLIPBOOK_BLENDING
			#pragma shader_feature _NEAR_FADE 
			#pragma shader_feature _SOFT_PARTICLE
			#pragma multi_compile_instancing

			#pragma vertex UnlitPassVertex
			#pragma fragment UnlitPassFragment
			#include "UnlitPass.hlsl"
			ENDHLSL
		}

		Pass {
			Tags {
				"LightMode" = "ShadowCaster"
			}

			ColorMask 0

			HLSLPROGRAM
			#pragma target 3.5
			#pragma shader_feature _ _SHADOWS_CLIP _SHADOWS_DITHER
			#pragma multi_compile_instancing
			#pragma vertex ShadowCasterPassVertex
			#pragma fragment ShadowCasterPassFragment
			#include "ShadowCasterPass.hlsl"
			ENDHLSL
		}
	}

	CustomEditor "CustomShaderGUI"
}
