Shader "VolumetricFogAndMist/CopySunDepth" {
Properties {
	_MainTex ("", 2D) = "black" {}
}

SubShader {
	Tags { "RenderType"="Opaque" }
	Pass {
		CGPROGRAM
		#pragma vertex vert
		#pragma fragment frag
		#include "UnityCG.cginc"
		struct v2f {
    		float4 pos : SV_POSITION;
    		float dist : TEXCOORD0;
		};
		v2f vert( appdata_base v ) {
    		v2f o;
    		o.pos = UnityObjectToClipPos(v.vertex);
    		float3 worldPos = mul(unity_ObjectToWorld, v.vertex);
    		o.dist = 1.0 / distance(_WorldSpaceCameraPos, worldPos);
    		return o;
		}
		float4 frag(v2f i) : SV_Target {
			return EncodeFloatRGBA(i.dist);
		}
		ENDCG
	}
}
	

SubShader {
	Tags { "RenderType"="TreeOpaque" }
	Pass {
		CGPROGRAM
		#pragma vertex vert
		#pragma fragment frag
		#include "UnityCG.cginc"
		struct v2f {
    		float4 pos : SV_POSITION;
    		float dist : TEXCOORD0;
		};
		v2f vert( appdata_base v ) {
    		v2f o;
    		o.pos = UnityObjectToClipPos(v.vertex);
    		float3 worldPos = mul(unity_ObjectToWorld, v.vertex);
    		o.dist = 1.0 / distance(_WorldSpaceCameraPos, worldPos);
    		return o;
		}
		float4 frag(v2f i) : SV_Target {
			return EncodeFloatRGBA(i.dist);
		}
		ENDCG
	}
}

Fallback Off
}
