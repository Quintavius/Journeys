Shader "Hidden/SC Post Effects/Ambient Occlusion 2D"
{
	HLSLINCLUDE

	#include "../../../Shaders/StdLib.hlsl"
	#include "../../../Shaders/Sampling.hlsl"
	#include "../../../Shaders/SCPE.hlsl"

	TEXTURE2D_SAMPLER2D(_AO, sampler_AO);
	float _SampleDistance;
	float _Threshold;
	float _Blur;
	float _Intensity;

	struct v2flum {
		float4 vertex : POSITION;
		float2 texcoord[3] : TEXCOORD0;
	};

	v2flum Vert(AttributesDefault v)
	{
		v2flum o;
		o.vertex = float4(v.vertex.xy, 0, 1);
		float2 uv = TransformTriangleVertexToUV(v.vertex.xy);

#if UNITY_UV_STARTS_AT_TOP
		uv = uv * float2(1.0, -1.0) + float2(0.0, 1.0);
#endif

		o.texcoord[0] = uv;
		o.texcoord[1] = uv + float2(-_MainTex_TexelSize.x, -_MainTex_TexelSize.y) * _SampleDistance;
		o.texcoord[2] = uv + float2(+_MainTex_TexelSize.x, -_MainTex_TexelSize.y) * _SampleDistance;

		return o;
	}

	float4 FragLuminanceDiff(v2flum i) : SV_Target
	{
		float4 original = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.texcoord[0]).rgba;

		half3 p1 = original.rgb;
		half3 p2 = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.texcoord[1]).rgb;
		half3 p3 = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.texcoord[2]).rgb;

		half3 diff = p1 * 2 - p2 - p3;
		half edge = dot(diff, diff);
		edge = step(edge, _Threshold);

		//Edges only
		original.rgb = lerp(1, edge, _Intensity);

		//return original;
		return original;
	}


	float4 FragBlend(VaryingsDefault i) : SV_Target
	{
		float4 original = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.texcoord);
		float ao = SAMPLE_TEXTURE2D(_AO, sampler_AO, i.texcoord).r;

		return float4(original.rgb * ao, original.a);
	}

	float4 FragDebug(VaryingsDefault i) : SV_Target
	{
		 return SAMPLE_TEXTURE2D(_AO, sampler_AO, i.texcoord);
	}

	ENDHLSL

SubShader
	{
		Cull Off ZWrite Off ZTest Always

		Pass //0
		{
			HLSLPROGRAM

			#pragma vertex Vert
			#pragma fragment FragLuminanceDiff

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

			#pragma vertex Vert
			#pragma fragment FragBlend

			ENDHLSL
		}
		Pass //3
		{
			HLSLPROGRAM

			#pragma vertex Vert
			#pragma fragment FragDebug

			ENDHLSL
		}
	}
}