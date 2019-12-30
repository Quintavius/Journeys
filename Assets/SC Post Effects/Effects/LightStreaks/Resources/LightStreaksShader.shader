Shader "Hidden/SC Post Effects/Light Streaks"
{
	HLSLINCLUDE

	#include "../../../Shaders/StdLib.hlsl"
	#include "../../../Shaders/Sampling.hlsl"
	#include "../../../Shaders/SCPE.hlsl"

	TEXTURE2D_SAMPLER2D(_BloomTex, sampler_BloomTex);

	float4 _Params;
	//X: Luminance threshold
	//Y: Intensity
	//Z: ...
	//W: ...

	float4 FragLuminanceDiff(VaryingsDefault i) : SV_Target
	{
		float4 luminance = LuminanceThreshold(SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.texcoordStereo), _Params.x);
		luminance *= _Params.y;

		return luminance;
	}

	float4 FragBlend(VaryingsDefault i) : SV_Target
	{
		float4 original = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.texcoordStereo);
		float3 bloom = SAMPLE_TEXTURE2D(_BloomTex, sampler_BloomTex, i.texcoordStereo).rgb;

		return float4(original.rgb + bloom, original.a);
	}

	float4 FragDebug(VaryingsDefault i) : SV_Target
	{
		return SAMPLE_TEXTURE2D(_BloomTex, sampler_BloomTex, i.texcoordStereo);
	}

	ENDHLSL

	SubShader
	{
		Cull Off ZWrite Off ZTest Always

		Pass //0
		{
			HLSLPROGRAM

			#pragma vertex VertDefault
			#pragma fragment FragLuminanceDiff

			ENDHLSL
		}
		Pass //1
		{
			HLSLPROGRAM

			#pragma vertex VertDefault
			#pragma fragment FragBlurBox

			ENDHLSL
		}
		Pass //2
		{
			HLSLPROGRAM

			#pragma vertex VertGaussian
			#pragma fragment FragBlurGaussian

			ENDHLSL
		}
		Pass //3
		{
			HLSLPROGRAM

			#pragma vertex VertDefault
			#pragma fragment FragBlend

			ENDHLSL
		}
		Pass //4
		{
			HLSLPROGRAM

			#pragma vertex VertDefault
			#pragma fragment FragDebug

			ENDHLSL
		}
	}
}