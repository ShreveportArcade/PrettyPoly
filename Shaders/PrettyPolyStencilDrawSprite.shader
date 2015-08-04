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

Shader "PrettyPoly/Stenciled/Lit Sprite" {
    Properties {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _BumpTex ("Normal Map", 2D) = "bump" {}
    }

    SubShader {
        Tags { 
            "Queue"="Transparent" 
            "IgnoreProjector"="True" 
            "RenderType"="Transparent" 
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Stencil {
            Ref 1
            Comp Equal
        }

        CGPROGRAM
        #pragma surface surf Lambert alpha vertex:vert
        #pragma multi_compile PIXELSNAP_ON

        sampler2D _MainTex;
        sampler2D _BumpTex;
        fixed4 _Color;

        struct Input {
            float2 uv_MainTex;
            fixed4 color;
        };
        
        void vert (inout appdata_full v, out Input o) {
            #if defined(PIXELSNAP_ON) && !defined(SHADER_API_FLASH)
            v.vertex = UnityPixelSnap (v.vertex);
            #endif
            v.normal = float3(0,0,-1);
            
            UNITY_INITIALIZE_OUTPUT(Input, o);
            o.color = v.color;
        }

        void surf (Input IN, inout SurfaceOutput o) {
            fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * IN.color;
            o.Albedo = c.rgb;
            o.Alpha = c.a;
            o.Normal = UnpackNormal(tex2D(_BumpTex, IN.uv_MainTex));
        }
        ENDCG
    }

Fallback "Transparent/VertexLit"
}
