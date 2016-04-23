﻿// Copyright (C) 2014 Nolan Baker
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

Shader "PrettyPoly/Lit Vertex Color is HSV" {
	Properties {
		_MainTex ("Base (RGB)", 2D) = "white" {}
		_BumpTex ("Normal Map", 2D) = "bump" {}
	}
	SubShader {
		Tags { 
			"Queue"="Transparent" 
			"RenderType"="Transparent" 
			"CanUseSpriteAtlas"="True"
		}
		LOD 200
		ZWrite Off
		Cull off
		Blend SrcAlpha OneMinusSrcAlpha

		CGPROGRAM
		#pragma surface surf Lambert alpha

		sampler2D _MainTex;
		sampler2D _BumpTex;
		fixed _Cutoff;

		struct Input {
			float2 uv_MainTex;
			float4 color : COLOR;
		};

		void surf (Input IN, inout SurfaceOutput o) {
			half4 c = tex2D (_MainTex, IN.uv_MainTex);
			o.Normal = UnpackNormal(tex2D(_BumpTex, IN.uv_MainTex));
 
 			float4 hsv = IN.color;
			float VSU = hsv.z * hsv.y * cos(hsv.x * 6.2831853072);
			float VSW = hsv.z * hsv.y * sin(hsv.x * 6.2831853072);
 
            c = half4(((0.299 * hsv.z + 0.701 * VSU + 0.168 * VSW) * c.x + 
            		   (0.587 * hsv.z - 0.587 * VSU + 0.330 * VSW) * c.y + 
            		   (0.114 * hsv.z - 0.114 * VSU - 0.497 * VSW) * c.z),
            		  ((0.299 * hsv.z - 0.299 * VSU - 0.328 * VSW) * c.x + 
            		   (0.587 * hsv.z + 0.413 * VSU + 0.035 * VSW) * c.y + 
            		   (0.114 * hsv.z - 0.114 * VSU + 0.292 * VSW) * c.z),
           			  ((0.299 * hsv.z - 0.300 * VSU + 1.250 * VSW) * c.x + 
           			   (0.587 * hsv.z - 0.588 * VSU - 1.050 * VSW) * c.y + 
           			   (0.114 * hsv.z + 0.886 * VSU - 0.203 * VSW) * c.z),
           			  c.a);

			o.Albedo = c.rgb * c.a;
			o.Alpha = c.a;
		}
		ENDCG
	} 
	FallBack "Diffuse"
}
