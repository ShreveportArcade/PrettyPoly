﻿Shader "PrettyPoly/Particle" {
	Properties { 
		_MainTex ("Texture", 2D) = "white" {}
		_Color ("Color", Color) = (1,1,1,1)
		_Duration ("Delay (X), Fade In (Y), Fade Out (Z), Offset (W)", Vector) = (1,0.5,3,5)
		_Weight ("Default Position Weight", Range(0,1)) = 1
		_Dist ("Random Displacement Multiplier", Float) = 0
		_Disp ("Displacement", Vector) = (0,0,0,0)
	} 

	SubShader {

		Tags { "Queue"="Transparent" "RenderType"="Transparent" } 
		
		Lighting Off 
		Blend SrcAlpha OneMinusSrcAlpha 
		Cull Off 
		ZWrite Off 
		
		Pass {	
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			#include "UnityCG.cginc"

			struct appdata_t {
				float4 vertex : POSITION;
				fixed4 color : COLOR;
				float2 texcoord : TEXCOORD0;
			};

			struct v2f {
				float4 vertex : POSITION;
				float2 texcoord : TEXCOORD0;
				float alpha : TEXCOORD1;
			};

			sampler2D _MainTex;
			float4 _Color;
			float4 _Duration;
			float _Weight;
			float _Dist;
			float3 _Disp;
			
			uniform float4 _MainTex_ST;
			
			v2f vert (appdata_t v) {
				v2f o;

				half delay = fmod(_Duration.x * v.color.b, 1);
				half fadeIn = _Duration.y;
				half fadeOut = _Duration.z;
				half offset = _Duration.w * v.color.a;

				half t = fmod(_Time.y + offset, delay + fadeIn + fadeOut);
				half pastDelay = ceil(saturate(t - delay));
				half pastFadeIn = ceil(saturate(t - delay - fadeIn));
				half fadeInPct = saturate((t - delay) / fadeIn);
				half fadeOutPct = saturate((t - delay - fadeIn) / fadeOut);

				o.alpha = fadeInPct * pastDelay * (1 - pastFadeIn) + (1 - fadeOutPct) * pastFadeIn;

				float3 disp = v.vertex.xyz;
				
				disp += t * (v.color.xyz - 0.5f) * 2 * _Dist;
				float ang = v.color.z * 2 * 3.1415;
				disp.xy += float2(_Disp.x * cos(ang) - _Disp.y * sin(ang),
	 						   _Disp.y * cos(ang) + _Disp.x * sin(ang));
 								
				v.vertex.xyz = lerp(disp, v.vertex.xyz, _Weight);
				o.vertex = mul(UNITY_MATRIX_MVP, v.vertex);
				
				o.texcoord = TRANSFORM_TEX(v.texcoord,_MainTex);
				return o;
			}

			fixed4 frag (v2f i) : COLOR {
				half4 c = tex2D(_MainTex, i.texcoord) * _Color;
				c.a *= i.alpha;
				return c;
			}
			ENDCG 
		}
	} 	
}
