Shader "PrettyPoly/Lit Sprite" {
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

		CGPROGRAM
		#pragma surface surf Lambert alpha vertex:vert
		#pragma multi_compile DUMMY PIXELSNAP_ON

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
