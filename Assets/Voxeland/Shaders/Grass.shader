Shader "Voxeland/Grass" 
{
	Properties 
	{
		[Enum(Off, 0, Front, 1, Back, 2)] _Culling("Culling", Int) = 2
		_Cutoff("Alpha Ref", Range(0, 1)) = 0.33
		_AmbientPower("Ambient Power", Range(0.01, 2)) = 0.25
		_MipFactor("MipMap Factor", Float) = -0.1

		//if _TEX2DARRAY
		_MainTexArr("Albedo (RGB), Height (A)", 2DArray) = "" {}
		_BumpMapArr("Normals", 2DArray) = "" {}
		_SpecGlossMapArr("Spec Map (RGB), Smooth Map (A)", 2DArray) = "" {}
		_SSSVanishMapArr("SSS(R) Vanish(A)", 2DArray) = "" {}

		//if _ATLAS
		_MainTex("Albedo (RGB)", 2D) = "white" {}
		_BumpMap("Normal (RGB)", 2D) = "white" {}
		_SpecGlossMap("SpecGloss (RGB)", 2D) = "white" {}
		_SSSVanishMap("SSSVanish (RGB)", 2D) = "white" {}

		//if ___
		[HideInInspector] _MainTex0("Albedo0 (RGB)", 2D) = "white" {}			[HideInInspector] _MainTex1("Albedo1 (RGB)", 2D) = "white" {}			[HideInInspector] _MainTex2("Albedo2 (RGB)", 2D) = "white" {}			[HideInInspector] _MainTex3("Albedo3 (RGB)", 2D) = "white" {}			[HideInInspector] _MainTex4("Albedo4 (RGB)", 2D) = "white" {}
		[HideInInspector] _BumpMap0("Normal0 (RGB)", 2D) = "white" {}			[HideInInspector] _BumpMap1("Normal1 (RGB)", 2D) = "white" {}			[HideInInspector] _BumpMap2("Normal2 (RGB)", 2D) = "white" {}			[HideInInspector] _BumpMap3("Normal3 (RGB)", 2D) = "white" {}			[HideInInspector] _BumpMap4("Normal4 (RGB)", 2D) = "white" {}
		[HideInInspector] _SpecGlossMap0("SpecGloss0 (RGB)", 2D) = "white" {}	[HideInInspector] _SpecGlossMap1("SpecGloss1 (RGB)", 2D) = "white" {}	[HideInInspector] _SpecGlossMap2("SpecGloss2 (RGB)", 2D) = "white" {}	[HideInInspector] _SpecGlossMap3("SpecGloss3 (RGB)", 2D) = "white" {}	[HideInInspector] _SpecGlossMap4("SpecGloss4 (RGB)", 2D) = "white" {}
		[HideInInspector] _SSSVanishMap0("SSSVanish0 (RGB)", 2D) = "white" {}	[HideInInspector] _SSSVanishMap1("SSSVanish1 (RGB)", 2D) = "white" {}	[HideInInspector] _SSSVanishMap2("SSSVanish2 (RGB)", 2D) = "white" {}	[HideInInspector] _SSSVanishMap3("SSSVanish3 (RGB)", 2D) = "white" {}	[HideInInspector] _SSSVanishMap4("SSSVanish4 (RGB)", 2D) = "white" {}


		[HideInInspector] _Color0("Color0", Color) = (1,1,1,1)		[HideInInspector] _Color1("Color1", Color) = (1,1,1,1)		[HideInInspector] _Color2("Color2", Color) = (1,1,1,1)		[HideInInspector] _Color3("Color3", Color) = (1,1,1,1)		[HideInInspector] _Color4("Color4", Color) = (1,1,1,1)		[HideInInspector] _Color5("Color5", Color) = (1,1,1,1)		[HideInInspector] _Color6("Color6", Color) = (1,1,1,1)		[HideInInspector] _Color7("Color7", Color) = (1,1,1,1)		[HideInInspector] _Color8("Color8", Color) = (1,1,1,1)		[HideInInspector] _Color9("Color9", Color) = (1,1,1,1)
		[HideInInspector] _SpecParams0("SpecParams", Vector) = (-0.75,0.25,-0.35,1.5)		[HideInInspector] _SpecParams1("SpecParams", Vector) = (-0.75,0.25,-0.35,1.5)		[HideInInspector] _SpecParams2("SpecParams", Vector) = (-0.75,0.25,-0.35,1.5)		[HideInInspector] _SpecParams3("SpecParams", Vector) = (-0.75,0.25,-0.35,1.5)		[HideInInspector] _SpecParams4("SpecParams", Vector) = (-0.75,0.25,-0.35,1.5)		[HideInInspector] _SpecParams5("SpecParams", Vector) = (-0.75,0.25,-0.35,1.5)		[HideInInspector] _SpecParams6("SpecParams", Vector) = (-0.75,0.25,-0.35,1.5)		[HideInInspector] _SpecParams7("SpecParams", Vector) = (-0.75,0.25,-0.35,1.5)		[HideInInspector] _SpecParams8("SpecParams", Vector) = (-0.75,0.25,-0.35,1.5)		[HideInInspector] _SpecParams9("SpecParams", Vector) = (-0.75,0.25,-0.35,1.5)
		[HideInInspector] _UVRect0("UV Rect0", Vector) = (0,0,1,1)	[HideInInspector] _UVRect1("UV Rect1", Vector) = (0,0,1,1)	[HideInInspector] _UVRect2("UV Rect2", Vector) = (0,0,1,1)	[HideInInspector] _UVRect3("UV Rect3", Vector) = (0,0,1,1)	[HideInInspector] _UVRect4("UV Rect4", Vector) = (0,0,1,1)	[HideInInspector] _UVRect5("UV Rect5", Vector) = (0,0,1,1)	[HideInInspector] _UVRect6("UV Rect6", Vector) = (0,0,1,1)	[HideInInspector] _UVRect7("UV Rect7", Vector) = (0,0,1,1)	[HideInInspector] _UVRect8("UV Rect8", Vector) = (0,0,1,1)	[HideInInspector] _UVRect9("UV Rect9", Vector) = (0,0,1,1)	

		_VanishAngle("View Angle: Vanish", Range(0,1)) = 0.2
		_AppearAngle("View Angle: Appear", Range(0,1)) = 0.3

		_SSS("SSS", Range(0,2)) = 1.5
		_Saturation("SSS Saturation", Range(0,8)) = 4
		_SSSDistance("SSS Distance", Float) = 100

		_ShakingAmplitude("Shaking Amplitude", Float) = 0.2
		_ShakingFrequency("Shaking Frequency", Float) = 0.2

		_WindTex("Wind(XY)", 2D) = "bump" {}
		_WindSize("Wind Size", Range(0, 300)) = 50
		_WindSpeed("Wind Speed", Float) = 5
		_WindStrength("Wind Strength", Range(0, 2)) = 0.33

		_Mips("MipMap Factor", Float) = 0.4
	}

	CGINCLUDE

		//sharing same vars with shadow pass

		#pragma shader_feature _PREVIEW
		#pragma shader_feature ___ _TEX2DARRAY _ATLAS   //TODO: try shader_feature


		#define LAYER(n)	half4 _Color##n; float4 _SpecParams##n; float4 _UVRect##n;
		LAYER(0) LAYER(1) LAYER(2) LAYER(3) LAYER(4) LAYER(5) LAYER(6) LAYER(7) LAYER(8) LAYER(9)

		half _Cutoff;
		float _MipFactor;
		half _Saturation;
		half _SSS;
		float _SSSDistance;
		float _Mips;


		#if _TEX2DARRAY
			#if defined(SHADER_API_D3D11) || defined(SHADER_API_D3D11_9X) || defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE) || defined(SHADER_API_METAL) || defined(SHADER_API_VULKAN) || defined(SHADER_API_PS4) || defined(SHADER_API_XBOXONE) //these platforms support texture arrays
			Texture2DArray _MainTexArr;		///UNITY_DECLARE_TEX2DARRAY(_MainTexArr);
			SamplerState sampler_MainTexArr;
			Texture2DArray _BumpMapArr;
			Texture2DArray _SpecGlossMapArr;
			Texture2DArray _SSSVanishMapArr;
			#endif	
		#elif _ATLAS
			#if SHADER_API_D3D11 || SHADER_API_D3D11_9X || SHADER_API_GLCORE || SHADER_API_GLES || SHADER_API_GLES3 || SHADER_API_METAL || SHADER_API_VULKAN || SHADER_API_D3D11_9X || SHADER_API_PS4 || SHADER_API_XBOXONE || SHADER_API_PSP2 || SHADER_API_WIIU  //these platforms support samples
			Texture2D _MainTex;		///UNITY_DECLARE_TEX2DARRAY(_MainTexArr);
			SamplerState sampler_MainTex;
			Texture2D _BumpMap;
			Texture2D _SpecGlossMap;
			Texture2D _SSSVanishMap;
			#endif
		#else
			#if SHADER_API_D3D11 || SHADER_API_D3D11_9X || SHADER_API_GLCORE || SHADER_API_GLES || SHADER_API_GLES3 || SHADER_API_METAL || SHADER_API_VULKAN || SHADER_API_D3D11_9X || SHADER_API_PS4 || SHADER_API_XBOXONE || SHADER_API_PSP2 || SHADER_API_WIIU  //these platforms support samples
			SamplerState sampler_MainTex0;
			#define TEXLAYER(n)	Texture2D _MainTex##n;	Texture2D _BumpMap##n; Texture2D _SpecGlossMap##n; Texture2D _SSSVanishMap##n;
			TEXLAYER(0) TEXLAYER(1) TEXLAYER(2) TEXLAYER(3) TEXLAYER(4)
			#endif
		#endif



		inline float4 SampleMainTex (float2 uv, int ch)
		{
			#if _TEX2DARRAY
				#if SHADER_API_D3D11 || SHADER_API_D3D11_9X || SHADER_API_GLES3 || SHADER_API_GLCORE || SHADER_API_METAL || SHADER_API_VULKAN || SHADER_API_PS4 || SHADER_API_XBOXONE //these platforms support texture arrays
				return _MainTexArr.SampleBias(sampler_MainTexArr, float3(uv, ch), _Mips);
				#endif
			#elif _ATLAS
				#if SHADER_API_D3D11 || SHADER_API_D3D11_9X || SHADER_API_GLES3 || SHADER_API_GLCORE || SHADER_API_METAL || SHADER_API_VULKAN || SHADER_API_PS4 || SHADER_API_XBOXONE //these platforms support texture arrays
				return _MainTex.SampleBias(sampler_MainTex, uv, _Mips);
				#endif
			#else
				#if SHADER_API_D3D11 || SHADER_API_D3D11_9X || SHADER_API_GLCORE || SHADER_API_GLES || SHADER_API_GLES3 || SHADER_API_METAL || SHADER_API_VULKAN || SHADER_API_D3D11_9X || SHADER_API_PS4 || SHADER_API_XBOXONE || SHADER_API_PSP2 || SHADER_API_WIIU  //these platforms support samples
				switch (ch)
				{
					case 0: return _MainTex0.SampleBias(sampler_MainTex0, uv, _Mips);	
					case 1: return _MainTex1.SampleBias(sampler_MainTex0, uv, _Mips);	
					case 2: return _MainTex2.SampleBias(sampler_MainTex0, uv, _Mips);	
					case 3: return _MainTex3.SampleBias(sampler_MainTex0, uv, _Mips);
					default: return half4(0, 0, 0, 0.5);
				}
				#endif
			#endif

			return half4(0, 0, 0, 0.5);
		}

		inline float4 SampleBumpMap (float2 uv, int ch)
		{
			#if _TEX2DARRAY
				#if SHADER_API_D3D11 || SHADER_API_D3D11_9X || SHADER_API_GLES3 || SHADER_API_GLCORE || SHADER_API_METAL || SHADER_API_VULKAN || SHADER_API_PS4 || SHADER_API_XBOXONE //these platforms support texture arrays
				return _BumpMapArr.SampleBias(sampler_MainTexArr, float3(uv, ch), _Mips);
				#endif

			#elif _ATLAS
				#if SHADER_API_D3D11 || SHADER_API_D3D11_9X || SHADER_API_GLES3 || SHADER_API_GLCORE || SHADER_API_METAL || SHADER_API_VULKAN || SHADER_API_PS4 || SHADER_API_XBOXONE //these platforms support texture arrays
				return _BumpMap .SampleBias(sampler_MainTex, uv, _Mips);
				#endif

			#else
				#if SHADER_API_D3D11 || SHADER_API_D3D11_9X || SHADER_API_GLCORE || SHADER_API_GLES || SHADER_API_GLES3 || SHADER_API_METAL || SHADER_API_VULKAN || SHADER_API_D3D11_9X || SHADER_API_PS4 || SHADER_API_XBOXONE || SHADER_API_PSP2 || SHADER_API_WIIU  //these platforms support samples
				switch (ch)
				{
					case 0: return _BumpMap0.SampleBias(sampler_MainTex0, uv, _Mips);	
					case 1: return _BumpMap1.SampleBias(sampler_MainTex0, uv, _Mips);	
					case 2: return _BumpMap2.SampleBias(sampler_MainTex0, uv, _Mips);	
					case 3: return _BumpMap3.SampleBias(sampler_MainTex0, uv, _Mips);
					default: return half4(0, 0, 0, 0.5);
				}
				#endif
			#endif

			return half4(0, 0, 0, 0.5);
		}

		inline float4 SampleSSSVanishMap(float2 uv, int ch)
		{
			#if _TEX2DARRAY
				#if SHADER_API_D3D11 || SHADER_API_D3D11_9X || SHADER_API_GLES3 || SHADER_API_GLCORE || SHADER_API_METAL || SHADER_API_VULKAN || SHADER_API_PS4 || SHADER_API_XBOXONE //these platforms support texture arrays
				return _SSSVanishMapArr.SampleBias(sampler_MainTexArr, float3(uv, ch), _Mips);
				#endif
			
			#elif _ATLAS
				#if SHADER_API_D3D11 || SHADER_API_D3D11_9X || SHADER_API_GLES3 || SHADER_API_GLCORE || SHADER_API_METAL || SHADER_API_VULKAN || SHADER_API_PS4 || SHADER_API_XBOXONE //these platforms support texture arrays
				return _SSSVanishMap.SampleBias(sampler_MainTex, uv, _Mips);
				#endif

			#else
				#if SHADER_API_D3D11 || SHADER_API_D3D11_9X || SHADER_API_GLCORE || SHADER_API_GLES || SHADER_API_GLES3 || SHADER_API_METAL || SHADER_API_VULKAN || SHADER_API_D3D11_9X || SHADER_API_PS4 || SHADER_API_XBOXONE || SHADER_API_PSP2 || SHADER_API_WIIU  //these platforms support samples
				switch (ch)
				{
					case 0: return _SSSVanishMap0.SampleBias(sampler_MainTex0, uv, _Mips);
					case 1: return _SSSVanishMap1.SampleBias(sampler_MainTex0, uv, _Mips);
					case 2: return _SSSVanishMap2.SampleBias(sampler_MainTex0, uv, _Mips);
					case 3: return _SSSVanishMap3.SampleBias(sampler_MainTex0, uv, _Mips);
					default: return half4(0, 0, 0, 0.5);
				}
				#endif
			#endif

			return half4(0, 0, 0, 0.5);
		}

		inline float4 SampleSpecGlossMap(float2 uv, int ch)
		{
			#if _TEX2DARRAY
				#if SHADER_API_D3D11 || SHADER_API_D3D11_9X || SHADER_API_GLES3 || SHADER_API_GLCORE || SHADER_API_METAL || SHADER_API_VULKAN || SHADER_API_PS4 || SHADER_API_XBOXONE //these platforms support texture arrays
				return _SpecGlossMapArr.SampleBias(sampler_MainTexArr, float3(uv, ch), _Mips);
				#endif

			#elif _ATLAS
				#if SHADER_API_D3D11 || SHADER_API_D3D11_9X || SHADER_API_GLES3 || SHADER_API_GLCORE || SHADER_API_METAL || SHADER_API_VULKAN || SHADER_API_PS4 || SHADER_API_XBOXONE //these platforms support texture arrays
				return _SpecGlossMap.SampleBias(sampler_MainTex, uv, _Mips);
				#endif

			#else
				#if SHADER_API_D3D11 || SHADER_API_D3D11_9X || SHADER_API_GLCORE || SHADER_API_GLES || SHADER_API_GLES3 || SHADER_API_METAL || SHADER_API_VULKAN || SHADER_API_D3D11_9X || SHADER_API_PS4 || SHADER_API_XBOXONE || SHADER_API_PSP2 || SHADER_API_WIIU  //these platforms support samples
				switch (ch)
				{
					case 0: return _SpecGlossMap0.SampleBias(sampler_MainTex0, uv, _Mips);
					case 1: return _SpecGlossMap1.SampleBias(sampler_MainTex0, uv, _Mips);
					case 2: return _SpecGlossMap2.SampleBias(sampler_MainTex0, uv, _Mips);
					case 3: return _SpecGlossMap3.SampleBias(sampler_MainTex0, uv, _Mips);
					default: return half4(0, 0, 0, 0.5);
				}
				#endif
			#endif

			return half4(0, 0, 0, 0.5);
		}



		//Input is shared with shadow pass too. No matter where it is defined. Anyways.
		struct Input
		{
			//float2 uv_MainTex; //uv_(texname) does not work when there is no _(texname) sampler or no _(texname) *property*. HELL Unity WTF?!
			float2 texCoord;
			float viewAngle;
			float viewDist; //to turn off sss in distance
			float type; //using int geves fizzling for some reason. Using float and rounding it in surface shader
			float ambient;
		};

	ENDCG

	SubShader 
	{
		Cull[_Culling]
		Tags { "RenderType"="Opaque" }
		LOD 200
		
		CGPROGRAM
		#pragma surface surf LeafSSS vertex:vert //alphatest:_Cutoff
		#pragma target 4.0

		#include "LightingLeafSSS.cginc"
		#include "LeafFunctions.cginc"





		void vert(inout appdata_full v, out Input data)
		{
			float2 vad = GetViewAngleDist(v.vertex, v.normal);
			data.viewAngle = vad.x; //for vanish
			data.viewDist = vad.y; //for sss

			v.vertex = WindShake(v.vertex, v.texcoord2);

			data.type = v.texcoord3.y;
			data.ambient = v.texcoord3.x;

			//uv offset
			float4 uvRects[10] = { _UVRect0, _UVRect1, _UVRect2, _UVRect3, _UVRect4, _UVRect5, _UVRect6, _UVRect7, _UVRect8, _UVRect9 };
			//TODO: seems to have no affect to performance, but have to remove it when Unity will be able to serialize material arrays
			int type = data.type + 0.49;
			float4 uvRect = uvRects[type];
			data.texCoord = v.texcoord * uvRect.zw + uvRect.xy;
		}






		void surf (Input IN, inout SurfaceOutputStandardSpecular o)
		{
			//UNITY_ACCESS_INSTANCED_PROP(_Color);
			
			fixed4 colors[10] = { _Color0, _Color1, _Color2, _Color3, _Color4, _Color5, _Color6, _Color7, _Color8, _Color9 };
			float4 specParams[10] = { _SpecParams0, _SpecParams1, _SpecParams2, _SpecParams3, _SpecParams4, _SpecParams5, _SpecParams6, _SpecParams7, _SpecParams8, _SpecParams9 };
			//TODO: seems to have no affect to performance, but have to remove it when Unity will be able to serialize material arrays

			int type = IN.type + 0.49; //rounding interpolated float to prevent fizzling


			fixed4 c = SampleMainTex(IN.texCoord, type);  //_MainTexArr.SampleBias(sampler_MainTexArr, float3(IN.texCoord, type), _MipFactor);
			clip(c.a - _Cutoff);
			o.Albedo = c.rgb * colors[type];
			o.Alpha = c.a;

			fixed4 sssvc = SampleSSSVanishMap(IN.texCoord, type);
			Vanish(sssvc.a, IN.viewAngle);
			float sssPercent = saturate((_SSSDistance - IN.viewDist) / (_SSSDistance - _SSSDistance*0.75));
			o.Emission = sssvc.r * sssPercent; //storing sss value in emission

			fixed4 nc = SampleBumpMap(IN.texCoord, type);
			o.Normal = UnpackNormal(nc);

			fixed4 sc = SampleSpecGlossMap(IN.texCoord, type);
			float4 sp = specParams[type];
			o.Specular = saturate(sc*sp.y + sp.x);
			o.Smoothness = saturate(sc*sp.w + sp.z);

			o.Occlusion = IN.ambient;


			//preview
			//o.Albedo = 0; o.Specular = 0; o.Smoothness = 1;
			//o.Emission = sssvColor;
		} 



		ENDCG
	} 


	//shadow caster - otherwise it displays like hell
	SubShader
	{
		Pass
		{
			Name "ShadowCaster"
			Tags { "LightMode" = "ShadowCaster" }
		   
			Cull[_Culling]
			Fog { Mode Off }
			ZWrite On ZTest Less
			Offset 1, 1
			 
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_shadowcaster
			#pragma fragmentoption ARB_precision_hint_fastest
			#include "LeafFunctions.cginc" 
			#include "UnityCG.cginc"

			struct v2f
			{ 
				 V2F_SHADOW_CASTER;

				 float2 texCoord : TEXCOORD0;
				 float viewAngle : TEXCOORD1;
				 int type : TEXCOORD2;
			};
		   
			v2f vert(appdata_full v)
			{
				v2f o;
				
				float2 vad = GetViewAngleDist(v.vertex, v.normal);
				o.viewAngle = vad.x; //for vanish

				v.vertex = WindShake(v.vertex, v.texcoord2);

				o.type = v.texcoord3.y;
				
				//uv offset
				float4 uvRects[10] = { _UVRect0, _UVRect1, _UVRect2, _UVRect3, _UVRect4, _UVRect5, _UVRect6, _UVRect7, _UVRect8, _UVRect9 };
				//TODO: seems to have no affect to performance, but have to remove it when Unity will be able to serialize material arrays
				int type = o.type + 0.49;
				float4 uvRect = uvRects[type];
				o.texCoord = v.texcoord * uvRect.zw + uvRect.xy;

				TRANSFER_SHADOW_CASTER(o)
				//TRANSFER_SHADOW_CASTER_NORMALOFFSET(o)
				//#ifdef SHADOWS_CUBE
				//	o.vec = mul(unity_ObjectToWorld, v.vertex).xyz - _LightPositionRange.xyz; 
				//	o.pos = UnityObjectToClipPos(v.vertex);
				//#else
					//o.pos = mul(unity_ObjectToWorld, v.vertex);
				//	o.pos = UnityClipSpaceShadowCasterPos(v.vertex, v.normal); //offset should not be applied when using custom normals!
				//	o.pos = UnityApplyLinearShadowBias(o.pos);
				//#endif
				
				return o;
			}
		   
			float4 frag(v2f i) : COLOR
			{
				int type = i.type +0.49;

				fixed4 c = SampleMainTex(i.texCoord,type);
				clip(c.a - _Cutoff);

				fixed4 sssvColor = SampleSSSVanishMap(i.texCoord, type); 
				Vanish(sssvColor.a, i.viewAngle);

				return c;
			}
 
			ENDCG
		}
	}
	
	CustomEditor "VoxelandMaterialInspector"
}
