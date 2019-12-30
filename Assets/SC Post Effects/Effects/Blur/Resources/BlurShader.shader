Shader "Hidden/SC Post Effects/Blur"
{
	HLSLINCLUDE

	/* begin-include */
	#include "../../../Shaders/StdLib.hlsl"
	#include "../../../Shaders/Sampling.hlsl"
	#include "../../../Shaders/SCPE.hlsl"
	/* end-include */

	//Separate pass, because this shouldn't be looped
	float4 FragBlend(VaryingsDefault i) : SV_Target
	{
		return SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.texcoordStereo);
	}

	ENDHLSL

	SubShader
	{
		Cull Off ZWrite Off ZTest Always

		Pass //0
		{
			HLSLPROGRAM

			#pragma vertex VertDefault
			#pragma fragment FragBlend

			ENDHLSL
		}
		Pass //1
		{
			HLSLPROGRAM

			#pragma vertex VertGaussian
			#pragma fragment FragBlurGaussian

			ENDHLSL
		}
		Pass //2
		{
			HLSLPROGRAM

			#pragma vertex VertDefault
			#pragma fragment FragBlurBox

			ENDHLSL
		}

	}
}