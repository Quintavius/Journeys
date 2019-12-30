#ifndef SAMPLE_INCLUDED
#define SAMPLE_INCLUDED

		//required:

		//for texArr
		//SamplerState sampler_MainTexArr;
		//Texture2DArray _MainTexArr;
		//Texture2DArray _BumpMapArr;

		//for atlas
		//SamplerState sampler_MainTex;
		//Texture2D _MainTex;
		//Texture2D _BumpMap;
		//float4 _Offset;


		//for multi tex
		//SamplerState sampler_MainTex0;
		//Texture2D _MainTex0, _MainTex1, _MainTex2, _MainTex3;
		//Texture2D _BumpMap0, _BumpMap1, _BumpMap2, _BumpMap3;



		#define SAMPLECASE(nm,ch) case (ch): return _##nm##ch.SampleBias(sampler_MainTex0, uv, _Mips)
		//#define SAMPLECASE(nm,ch) case (ch): return tex2D (_##nm, uv)
		#define SAMPLEMULTITEX(n) SAMPLECASE(n,0); SAMPLECASE(n,1); SAMPLECASE(n,2); SAMPLECASE(n,3);

		#define SAMPLEL(nm,uv,ch) { return half4(0, 0, 0, 0.5) }




		inline float4 SampleMainTex (float2 uv, int ch)
		{
			
			
			/*#if _TEX2DARRAY
				#if defined(SHADER_API_D3D11) || defined(SHADER_API_D3D11_9X) || defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE) || defined(SHADER_API_METAL) || defined(SHADER_API_VULKAN) || defined(SHADER_API_PS4) || defined(SHADER_API_XBOXONE) //these platforms support texture arrays
				return _MainTexArr.SampleBias(sampler_MainTexArr, float3(uv, ch), _Mips);
				#endif
			
			//#elif _ATLAS
			//	float2 ouv = uv*_Offset.zw + _Offset.xy;
			//	return _MainTex.SampleBias(sampler_MainTex, ouv, _Mips);
			
			#else
				#if defined(SHADER_API_D3D11) || defined(SHADER_API_D3D11_9X) || defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE) || defined(SHADER_API_METAL) || defined(SHADER_API_VULKAN) || defined(SHADER_API_PS4) || defined(SHADER_API_XBOXONE) //these platforms support texture arrays

				return half4(0, 0, 0, 0.5);
				switch (ch)
				{
					//SAMPLEMULTITEX(MainTex)
					case 0: return _MainTex0.SampleBias(sampler_MainTex0, uv, _Mips)
					
					default: return half4(0, 0, 0, 0.5);
				}

				#endif

			#endif*/
			return half4(0, 0, 0, 0.5);  //not needed, but does not compile without this
		}

		inline float4 SampleBumpMap(float2 uv, int ch)
		{
			return half4(0, 0, 0, 0.5);  //not needed, but does not compile without this
		}



#endif