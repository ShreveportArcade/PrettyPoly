Shader "PrettyPoly/Lit" {
	Properties {
		_MainTex ("Base (RGB)", 2D) = "white" {}
		_BumpTex ("Normal Map", 2D) = "bump" {}
		_Cutoff ("Cutoff", Range(0,1)) = 0.5
	}
	SubShader {
		Tags { "Queue"="AlphaTest" "RenderType"="TransparentCutout" }
		LOD 200
		
		CGPROGRAM
		#pragma surface surf Lambert

		sampler2D _MainTex;
		sampler2D _BumpTex;
		fixed _Cutoff;

		struct Input {
			float2 uv_MainTex;
			float4 color : COLOR;
		};

		void surf (Input IN, inout SurfaceOutput o) {
			half4 c = tex2D (_MainTex, IN.uv_MainTex) * IN.color;
			o.Normal = UnpackNormal(tex2D(_BumpTex, IN.uv_MainTex));
			o.Albedo = c.rgb;
			o.Alpha = c.a;
			clip (o.Alpha - _Cutoff);
		}
		ENDCG
	} 
	FallBack "Diffuse"
}
