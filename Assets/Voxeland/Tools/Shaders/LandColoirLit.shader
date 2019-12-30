Shader "Voxeland/Land Color Lit" 
{
	Properties 
	{
		[Enum(Off, 0, Front, 1, Back, 2)] _Culling("Culling", Int) = 2 

		_Color0("Color 1", Color) = (1,0,0,1)
		_Color1("Color 2", Color) = (0,1,0,1)
		_Color2("Color 3", Color) = (0,0,1,1)
		_Color3("Color 4", Color) = (1,1,1,1)
		_Color4("Color 5", Color) = (1,1,1,1)
		_Color5("Color 6", Color) = (1,1,1,1)
		_Color6("Color 7", Color) = (1,1,1,1)
		_Color7("Color 8", Color) = (1,1,1,1)
		_Color8("Color 9", Color) = (1,1,1,1)
		_Color9("Color 10", Color) = (1,1,1,1)
		_Color10("Color 11", Color) = (1,1,1,1)
		_Color11("Color 12", Color) = (1,1,1,1)
		_Color12("Color 13", Color) = (1,1,1,1)
		_Color13("Color 14", Color) = (1,1,1,1)
		_Color14("Color 15", Color) = (1,1,1,1)
		_Color15("Color 16", Color) = (1,1,1,1)
		_Color16("Color 17", Color) = (1,1,1,1)
		_Color17("Color 18", Color) = (1,1,1,1)
		_Color18("Color 19", Color) = (1,1,1,1)
		_Color19("Color 20", Color) = (1,1,1,1)
		_Color20("Color 21", Color) = (1,1,1,1)
		_Color21("Color 22", Color) = (1,1,1,1)
		_Color22("Color 23", Color) = (1,1,1,1)
		_Color23("Color 24", Color) = (1,1,1,1)

		_AmbientOcclusion("Ambient Occlusion", Float) = 1

		_PreviewType("Preview Type", Int) = 0

		_HorizonHeightScale("Horizon Height Scale", Float) = 200
		_HorizonHeightmap("Horizon Heightmap", 2D) = "black" {}
		_HorizonTypemap("Horizon Typemap", 2D) = "black" {}
		_HorizonVisibilityMap("Horizon Visibility Map", 2D) = "white" {}
		_HorizonBorderLower("Horizon Border Lower", Float) = 2

		_HorizonColorTint("Horizon Color Tint", Color) = (0,0,0,0)

		//_Applied("tmp", Float) = 0
	}
	SubShader 
	{
		Cull[_Culling]
		Tags { "RenderType"="Opaque" }
		LOD 200
		
		CGPROGRAM

		//#pragma shader_feature _TRIPLANAR
		//#pragma shader_feature _DOUBLELAYER
		#pragma shader_feature _PREVIEW
		#pragma shader_feature _HORIZON
		//#pragma shader_feature _TEX2DARRAY

		#pragma surface surf StandardSpecular addshadow fullforwardshadows vertex:Vert nolightmap  //should be outside "if defined dx11"

		#pragma target 3.0


		struct Input
		{
			float3 wPos;
			half4 color;
			half ambient;

			#if _HORIZON
				half2 wTexcoord; //should be float, but it does not work in DX9
				half visibility;
			#endif
		};


		half4 _Color0, _Color1, _Color2, _Color3, _Color4, _Color5, _Color6, _Color7, _Color8, _Color9, _Color10, _Color11, _Color12, _Color13, _Color14, _Color15, _Color16, _Color17, _Color18, _Color19, _Color20, _Color21, _Color22, _Color23;

		float _AmbientOcclusion;

		half _PreviewType;

		#if _HORIZON
			float _HorizonHeightScale;
			sampler2D _HorizonHeightmap;
			sampler2D _HorizonTypemap;
			sampler2D _HorizonVisibilityMap; 
			float _HorizonBorderLower;
			half4 _HorizonColorTint;
		#endif


		inline float4 GetTangent(float3 worldNormal)
		{
			float4 tangent;
			float3 absWorldNormal = abs(worldNormal);

			if (absWorldNormal.z >= absWorldNormal.x && absWorldNormal.z >= absWorldNormal.y)
			{
				if (worldNormal.z>0) tangent = float4(-1, 0, 0, -1);
				else tangent = float4(1, 0, 0, -1);
			}
			else if (absWorldNormal.y >= absWorldNormal.x && absWorldNormal.y >= absWorldNormal.z)
			{
				if (worldNormal.y>0) tangent = float4(0, 0, -1, -1);
				else tangent = float4(0, 0, 1, -1);
			}
			else //if (absWorldNormal.x >= absWorldNormal.x && absWorldNormal.y >= absWorldNormal.z)
			{
				if (worldNormal.x>0) tangent = float4(0, 0, 1, -1);
				else tangent = float4(0, 0, -1, -1);
			}
			return tangent;
		}


		void Vert(inout appdata_full v, out Input data)
		{
			UNITY_INITIALIZE_OUTPUT(Input, data);

			data.color = half4(0,0,0,0);
			data.color += _Color0 * (((int)v.tangent.x >> 0) & 0xF) / 16;
			data.color += _Color1 * (((int)v.tangent.x >> 4) & 0xF) / 16;
			data.color += _Color2 * (((int)v.tangent.x >> 8) & 0xF) / 16;
			data.color += _Color3 * (((int)v.tangent.x >> 12) & 0xF) / 16;
			data.color += _Color4 * (((int)v.tangent.x >> 16) & 0xF) / 16;
			data.color += _Color5 * (((int)v.tangent.x >> 20) & 0xF) / 16;
			data.color += _Color6 * (((int)v.tangent.y >> 0) & 0xF) / 16;
			data.color += _Color7 * (((int)v.tangent.y >> 4) & 0xF) / 16;
			data.color += _Color8 * (((int)v.tangent.y >> 8) & 0xF) / 16;
			data.color += _Color9 * (((int)v.tangent.y >> 12) & 0xF) / 16;
			data.color += _Color10 * (((int)v.tangent.y >> 16) & 0xF) / 16;
			data.color += _Color11 * (((int)v.tangent.y >> 20) & 0xF) / 16;
			data.color += _Color12 * (((int)v.tangent.z >> 0) & 0xF) / 16;
			data.color += _Color13 * (((int)v.tangent.z >> 4) & 0xF) / 16;
			data.color += _Color14 * (((int)v.tangent.z >> 8) & 0xF) / 16;
			data.color += _Color15 * (((int)v.tangent.z >> 12) & 0xF) / 16;
			data.color += _Color16 * (((int)v.tangent.z >> 16) & 0xF) / 16;
			data.color += _Color17 * (((int)v.tangent.z >> 20) & 0xF) / 16;
			data.color += _Color18 * (((int)v.tangent.w >> 0) & 0xF) / 16;
			data.color += _Color19 * (((int)v.tangent.w >> 4) & 0xF) / 16;
			data.color += _Color20 * (((int)v.tangent.w >> 8) & 0xF) / 16;
			data.color += _Color21 * (((int)v.tangent.w >> 12) & 0xF) / 16;
			data.color += _Color22 * (((int)v.tangent.w >> 16) & 0xF) / 16;
			data.color += _Color23 * (((int)v.tangent.w >> 20) & 0xF) / 16;

			//pos, normal, tangent, ambient
			data.wPos = mul(unity_ObjectToWorld, v.vertex);
			float3 wNormal = normalize(mul(unity_ObjectToWorld, float4(v.normal, 0))); //world normal 
			v.tangent = GetTangent(wNormal); //vertex tangent
			data.ambient = v.texcoord3.x; 

			#if _HORIZON
				//height
				half4 heightColor = tex2Dlod(_HorizonHeightmap, float4(v.texcoord.xy, 0, 0));
				v.vertex.y = (heightColor.r*250 + heightColor.g)*256;

				//visibility and border
				float4 visibilityDirs = float4(
					tex2Dlod(_HorizonVisibilityMap, float4(v.texcoord.x+0.001, v.texcoord.y, 0, 0)).a,
					tex2Dlod(_HorizonVisibilityMap, float4(v.texcoord.x-0.001, v.texcoord.y, 0, 0)).a,
					tex2Dlod(_HorizonVisibilityMap, float4(v.texcoord.x, v.texcoord.y+0.001, 0, 0)).a,
					tex2Dlod(_HorizonVisibilityMap, float4(v.texcoord.x, v.texcoord.y-0.001, 0, 0)).a ); 
				data.wPos.x += (visibilityDirs.x - visibilityDirs.y)*_HorizonBorderLower;
				data.wPos.z += (visibilityDirs.z - visibilityDirs.w)*_HorizonBorderLower;

				data.visibility = (visibilityDirs.x+ visibilityDirs.y+ visibilityDirs.z+ visibilityDirs.w)*4; //if >0 then visible, if <1 then border
				if (data.visibility < 0.999) v.vertex.y -= _HorizonBorderLower * (1-data.visibility);

				//filling ambient with white
				data.ambient = 1;
			#endif
		}



		void surf (Input IN, inout SurfaceOutputStandardSpecular o) 
		{
			o.Albedo = IN.color;
			o.Occlusion = 1 - _AmbientOcclusion + IN.ambient*_AmbientOcclusion;

			//horizon mesh normal
			#if _HORIZON
				//calculating normal
				half4 heightColor = tex2D(_HorizonHeightmap, IN.wTexcoord);

				//visibility
				if (IN.visibility<0.01 || heightColor.r+heightColor.g<0.01) clip(-1);

				float3 baseNormal = float3(0,0,1);
				baseNormal.x = (heightColor.a - 0.5f)*2;
				baseNormal.y = -(heightColor.b - 0.5f)*2;
				baseNormal.z = sqrt(1 - saturate(dot(o.Normal.xy, o.Normal.xy)));

				//add to existing one
				o.Normal = baseNormal + float3(o.Normal.x, o.Normal.y, 0);
				o.Normal = normalize(o.Normal);
			#endif

			#if _PREVIEW
			if (_PreviewType != 0)
			{
				if (_PreviewType == 1) o.Emission = o.Albedo;
				if (_PreviewType == 2) o.Emission = o.Occlusion;
				//if (_PreviewType == 3) o.Emission = float3(totalNormal.g/blendSum, totalNormal.a/blendSum, 0); //IN.wNormal / 2 + 0.5;
				if (_PreviewType == 4) o.Emission = o.Specular;
				if (_PreviewType == 5) o.Emission = o.Smoothness;
				//if (_PreviewType == 6) o.Emission = absDirections;
				//if (_PreviewType == 7) o.Emission = IN.blendsA;
				//if (_PreviewType == 8) o.Emission = float3(blends[0], blends[1], blends[2]);
				if (_PreviewType == 9) o.Emission = frac(IN.wPos);

				#if defined(SHADER_API_D3D11) || defined(SHADER_API_D3D11_9X) || defined(SHADER_API_GLES3)
				if (_PreviewType == 10) o.Emission = half3(1,0,0);
				#elif defined(SHADER_API_GLCORE)
				if (_PreviewType == 10) o.Emission = half3(0,1,0);
				#else
				if (_PreviewType == 10) o.Emission = half3(0,0,1);
				#endif

				if (_PreviewType != 0)
				{
					o.Alpha = 0;
					o.Albedo = 0;
					//o.Metallic = 0;
					o.Specular = 0;
					o.Smoothness = 1;
					o.Occlusion = 0;
					//o.Normal = 0;
				}
			}
			#endif
		}

		ENDCG
	}
	FallBack "Diffuse"
	//CustomEditor "LandMaterialInspector"
}
