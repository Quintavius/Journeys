Shader "Hidden/SC Post Effects/Refraction"
{
	HLSLINCLUDE

	#include "../../../Shaders/StdLib.hlsl"

	TEXTURE2D_SAMPLER2D(_MainTex, sampler_MainTex);
	TEXTURE2D_SAMPLER2D(_RefractionTex, sampler_RefractionTex);
	uniform float _Amount;

	float4 Frag(VaryingsDefault i) : SV_Target
	{
		float4 dudv = SAMPLE_TEXTURE2D(_RefractionTex, sampler_RefractionTex, i.texcoord).rgba;

		float2 refraction = lerp(i.texcoordStereo, (i.texcoordStereo) * dudv.rg, _Amount * dudv.rg);

		float4 screenColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, refraction);

		return float4(screenColor.rgb, screenColor.a);
	}

	float4 FragNormalMap(VaryingsDefault i) : SV_Target
	{
		float4 dudv = SAMPLE_TEXTURE2D(_RefractionTex, sampler_RefractionTex, i.texcoord).rgba;

#if UNITY_VERSION >= 20172 //Pre 2017.2
		dudv.x *= dudv.w;
#else
		dudv.x = 1 - dudv.x;
#endif
		//Remap to -1,-1
		dudv.xy = dudv.xy * 2 - 1;

		float2 refraction = lerp(i.texcoordStereo, (i.texcoordStereo) + dudv.rg, _Amount * dudv.rg);

		float4 screenColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, refraction);

		return float4(screenColor.rgb, screenColor.a);
	}

	ENDHLSL

	SubShader
	{
		Cull Off ZWrite Off ZTest Always

		Pass
		{
			HLSLPROGRAM

			#pragma vertex VertDefault
			#pragma fragment Frag

			ENDHLSL
		}
		Pass
		{
			HLSLPROGRAM

			#pragma vertex VertDefault
			#pragma fragment FragNormalMap

			ENDHLSL
		}
	}
}