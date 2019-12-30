Shader "Hidden/SC Post Effects/Overlay"
{
	HLSLINCLUDE

	#include "../../../Shaders/StdLib.hlsl"
	#include "../../../Shaders/Blending.hlsl"

	TEXTURE2D_SAMPLER2D(_MainTex, sampler_MainTex);
	TEXTURE2D_SAMPLER2D(_OverlayTex, sampler_OverlayTex);
	float4 _Params;
	//X: Intensity
	//Y: Tiling
	//Z: Auto aspect (bool)
	//W: Blend mode

	float4 Frag(VaryingsDefault i) : SV_Target
	{
		float4 screenColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.texcoordStereo);

		float2 uv = i.texcoord;

		if(_Params.z == 1) uv.x *= _ScreenParams.x / _ScreenParams.y;

		#if UNITY_SINGLE_PASS_STEREO 
			uv = float2(i.texcoordStereo.x / 1, i.texcoordStereo.y);
		#endif
		float4 overlay = SAMPLE_TEXTURE2D(_OverlayTex, sampler_OverlayTex, uv * _Params.y);

		float3 color = 0;

		if (_Params.w == 0) color = lerp(screenColor.rgb, overlay.rgb, overlay.a * _Params.x);
		if (_Params.w == 1) color = lerp(screenColor.rgb, BlendAdditive(overlay.rgb, screenColor.rgb), overlay.a * _Params.x);
		if (_Params.w == 2) color = lerp(screenColor.rgb, overlay.rgb * screenColor.rgb, overlay.a * _Params.x);
		if (_Params.w == 3) color = lerp(screenColor.rgb, BlendScreen(overlay.rgb, screenColor.rgb), overlay.a * _Params.x);

		return float4(color.rgb, screenColor.a);
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