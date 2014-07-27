// Copyright (C) 2014 Nolan Baker
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated 
// documentation files (the "Software"), to deal in the Software without restriction, including without limitation 
// the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, 
// and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or substantial portions 
// of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED 
// TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL 
// THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF 
// CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER 
// DEALINGS IN THE SOFTWARE.

Shader "PrettyPoly/Lit Particle" 
{
	Properties { 
		_MainTex ("Texture", 2D) = "white" {}
		_BumpTex ("Normal Map", 2D) = "bump" {}
		_Color ("Color", Color) = (1,1,1,1)
		_Duration ("Delay (X), Fade In (Y), Fade Out (Z), Offset (W)", Vector) = (1,0.5,3,5)
		_Weight ("Default Position Weight", Range(0,1)) = 1
		_Dist ("Random Displacement Multiplier", Float) = 0
		_Disp ("Displacement", Vector) = (0,0,0,0)
	} 

	SubShader {

		Tags
		{ 
			"Queue"="Transparent" 
			"IgnoreProjector"="True" 
			"RenderType"="Transparent" 
			"PreviewType"="Plane"
			"CanUseSpriteAtlas"="True"
		}

		Cull Off
		ZWrite Off
		Blend SrcAlpha OneMinusSrcAlpha

		CGPROGRAM
		#pragma surface surf Lambert alpha vertex:vert
		#pragma multi_compile DUMMY PIXELSNAP_ON

		float4 _Duration;
		float _Weight;
		float _Dist;
		float3 _Disp;
			
		sampler2D _MainTex;
		sampler2D _BumpTex;
		fixed4 _Color;

		struct Input {
			float2 uv_MainTex;
			fixed4 color;
		};
		
		void vert (inout appdata_full v, out Input o) {
			half delay = fmod(_Duration.x * v.color.b, 1);
			half fadeIn = _Duration.y;
			half fadeOut = _Duration.z;
			half offset = _Duration.w * v.color.a;

			half t = fmod(_Time.y + offset, delay + fadeIn + fadeOut);
			half pastDelay = ceil(saturate(t - delay));
			half pastFadeIn = ceil(saturate(t - delay - fadeIn));
			half fadeInPct = saturate((t - delay) / fadeIn);
			half fadeOutPct = saturate((t - delay - fadeIn) / fadeOut);

			UNITY_INITIALIZE_OUTPUT(Input, o);
			o.color = _Color;
			o.color.a *= fadeInPct * pastDelay * (1 - pastFadeIn) + (1 - fadeOutPct) * pastFadeIn;

			float3 disp = v.vertex.xyz;
			
			disp += t * (v.color.xyz - 0.5f) * 2 * _Dist;
			float ang = v.color.z * 2 * 3.1415;
			disp.xy += float2(_Disp.x * cos(ang) - _Disp.y * sin(ang),
 						   _Disp.y * cos(ang) + _Disp.x * sin(ang));
								
			v.vertex.xyz = lerp(disp, v.vertex.xyz, _Weight);
			
			v.normal = float3(0,0,-1);
		}

		void surf (Input IN, inout SurfaceOutput o) {
			fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * IN.color;
			o.Albedo = c.rgb;
			o.Alpha = c.a;
			o.Normal = UnpackNormal(tex2D(_BumpTex, IN.uv_MainTex));
		}
		ENDCG
	}
}
