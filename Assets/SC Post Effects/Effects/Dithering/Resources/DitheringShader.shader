Shader "Hidden/SC Post Effects/Dithering"
{
	HLSLINCLUDE

	#include "../../../Shaders/StdLib.hlsl"
	#include "../../../Shaders/SCPE.hlsl"

	TEXTURE2D_SAMPLER2D(_LUT, sampler_LUT);

	float4 _Dithering_Coords;
	//X: Size
	//Y: Tiling
	//Z: Luminance influence
	//W: Intensity

	float4 Frag(VaryingsDefault i) : SV_Target
	{
		float4 screenColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.texcoordStereo);

		float luminance = Luminance(screenColor.rgb);

		float2 lutUV = float2(i.texcoord.x *= _ScreenParams.x / _ScreenParams.y, i.texcoord.y * _ScreenParams.w);

		float lut = SAMPLE_TEXTURE2D(_LUT, sampler_LUT, lutUV *_Dithering_Coords.y * 32).r;

		float dither = step(lut, luminance / _Dithering_Coords.z);//

		return lerp(screenColor, screenColor * saturate(dither), _Dithering_Coords.w);

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
	}
}