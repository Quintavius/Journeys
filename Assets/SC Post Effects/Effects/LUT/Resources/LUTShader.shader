Shader "Hidden/SC Post Effects/LUT"
{
	HLSLINCLUDE

	#include "../../../Shaders/StdLib.hlsl"
	#include "../../../Shaders/Colors.hlsl"

	TEXTURE2D_SAMPLER2D(_MainTex, sampler_MainTex);
	TEXTURE2D_SAMPLER2D(_LUT_Near, sampler_LUT_Near);
	TEXTURE2D_SAMPLER2D(_LUT_Far, sampler_LUT_Far);
	TEXTURE2D_SAMPLER2D(_CameraDepthTexture, sampler_CameraDepthTexture);

	float4 _LUT_Params;
	float _Distance;

	//Note: When the GLES/OpenCL API is used TEXTURE2D_ARGS does not accept a sampler parameter

	float3 FormatStrip(half3 uvw, half3 scaleOffset)
	{
		//Strip format where `height = sqrt(width)`
		uvw.z *= scaleOffset.z;
		half shift = floor(uvw.z);
		uvw.xy = uvw.xy * scaleOffset.z * scaleOffset.xy + scaleOffset.xy * 0.5;
		uvw.x += shift * scaleOffset.y;

		uvw.z -= shift;

		return uvw;
	}

	half3 ApplyLut2d(TEXTURE2D_ARGS(tex, samplerTex), half3 uvw, half3 scaleOffset)
	{
		uvw = FormatStrip(uvw, scaleOffset);

		float3 centerLut = SAMPLE_TEXTURE2D(tex, samplerTex, uvw.xy).rgb;
		float3 offsetLut = SAMPLE_TEXTURE2D(tex, samplerTex, uvw.xy + half2(scaleOffset.y, 0)).rgb;

		uvw.xyz = lerp(centerLut, offsetLut, uvw.z);

		return uvw;
	}
    
    inline float3 Grade(TEXTURE2D_ARGS(lut, samplerTex), half3 rgb) {

		half3 colorGraded;
        
        #if !UNITY_COLORSPACE_GAMMA //Linear
        rgb = LinearToSRGB(rgb);
        #endif


#if SHADER_API_MOBILE || SHADER_API_GLCORE
        colorGraded = ApplyLut2d(lut, rgb, _LUT_Params.xyz);
#else
        colorGraded = ApplyLut2d(lut, samplerTex, rgb, _LUT_Params.xyz);
#endif

#if !UNITY_COLORSPACE_GAMMA //Linear
        colorGraded = SRGBToLinear(colorGraded);
#endif//End linear

		return colorGraded;
	}

	float4 FragSingle(VaryingsDefault i) : SV_Target
	{
		float4 screenColor = saturate(SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.texcoordStereo));
#if SHADER_API_MOBILE || SHADER_API_GLCORE
		half3 colorGraded = Grade(_LUT_Near, screenColor.rgb);
#else
		half3 colorGraded = Grade(_LUT_Near, sampler_LUT_Near, screenColor.rgb);
#endif
		float3 color = lerp(screenColor.rgb, colorGraded.rgb, _LUT_Params.w);

		return float4(color.rgb, screenColor.a);
	}

	float4 FragDuo(VaryingsDefault i) : SV_Target
	{
		float4 screenColor = saturate(SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.texcoordStereo));
		float depth = (SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, sampler_CameraDepthTexture, i.texcoordStereo));
		depth *= _Distance;
		depth = saturate(depth);

#if SHADER_API_MOBILE || SHADER_API_GLCORE
		half3 gradedNear = Grade(_LUT_Near, screenColor.rgb);
		half3 gradedFar = Grade(_LUT_Far, screenColor.rgb);
#else
		half3 gradedNear = Grade(_LUT_Near, sampler_LUT_Near, screenColor.rgb);
		half3 gradedFar = Grade(_LUT_Far, sampler_LUT_Far, screenColor.rgb);
#endif

		float3 color = lerp(gradedFar, gradedNear, depth);

		return float4(lerp(screenColor.rgb, color.rgb, _LUT_Params.w), screenColor.a);
	}

	ENDHLSL

	SubShader
	{
		Cull Off ZWrite Off ZTest Always

		Pass
		{
			HLSLPROGRAM

			#pragma vertex VertDefault
			#pragma fragment FragSingle

			ENDHLSL
		}
		Pass //Depth based
		{
			HLSLPROGRAM

			#pragma vertex VertDefault
			#pragma fragment FragDuo

			ENDHLSL
		}
	}
}