Shader "Voxeland/Highlight" 
{
	Properties 
	{
		_Color ("Main Color", Color) = (1,0.7,0.4,1)
		_MainTex ("Diffuse(RGB). Cutout(A)", 2D) = "gray" {}
		_Brightness ("Brightness", Float) = 5
		_Blend("Blend", Float) = 0.3
		_Opacity("Opacity", Float) = 0.5
		_Offset ("Offset", Float) = 0.05
		_Linear("Linear", Int) = 0
	} 


Category 
{
	Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }
	//Blend One One
	
	AlphaTest Greater .01
	ColorMask RGB
	Cull Off Lighting Off ZWrite Off Fog { Color (0,0,0,0) }

	SubShader
	{
		Tags{ "Queue" = "Transparent" }

		GrabPass
		{
			"_BackgroundTexture"
		}

		Pass
		{
			CGPROGRAM
				#pragma vertex vert
				#pragma fragment frag
				#include "UnityCG.cginc"

				sampler2D _BackgroundTexture; 
				sampler2D _MainTex;
				float4 _MainTex_ST;
				float4 _Color;
				float _Brightness;
				float _Blend;
				float _Opacity;
				float _Offset;
				int _Linear;

				struct appdata {
					float4 vertex : POSITION;
					float4 texcoord : TEXCOORD0;
					float4 texcoord1 : TEXCOORD1;
				};

				struct v2f
				{
					float4 grabPos : TEXCOORD2;
					float4 pos : SV_POSITION;
					float2 texcoord : TEXCOORD0;
					float opacity : TEXCOORD1;
				};

				v2f vert(appdata v) 
				{
					v2f o;
					v.vertex.xyz += normalize(ObjSpaceViewDir(v.vertex)) * _Offset;
					o.pos = UnityObjectToClipPos(v.vertex);
					o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
					o.grabPos = ComputeGrabScreenPos(o.pos);
					o.opacity = v.texcoord1.x;
					return o;
				}

				half4 frag(v2f i) : SV_Target
				{
					float brightness = _Brightness*(1-_Linear) + pow(_Brightness,2.2)*_Linear;
					float opacity = _Opacity;//*(1-_Linear) + pow(_Opacity, 2.2)*_Linear;
					
					half4 bgColor = tex2Dproj(_BackgroundTexture, i.grabPos);
					half4 hlColor = _Color * tex2D(_MainTex, i.texcoord);

					half4 bld = bgColor*(1 - _Blend) + hlColor*_Blend;
					half4 mul = bld * hlColor * brightness;

					return bgColor*(1-opacity*i.opacity) + mul*opacity*i.opacity;
				}
			ENDCG
		}

	}

}
}