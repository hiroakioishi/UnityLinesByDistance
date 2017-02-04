Shader "Hidden/irishoak/LinesByDistance/LineRender"
{
	Properties
	{
		_MainTex("Texture", 2D) = "white" {}
	}

	CGINCLUDE
	#include "UnityCG.cginc"

	struct lineData
	{
		float3 position0;
		float  alpha0;
		float3 position1;
		float  alpha1;
	};

	struct v2g
	{
		float4 vertex0 : POSITION;
		float4 vertex1 : TEXCOORD2;
		float  alpha0  : TEXCOORD0;
		float  alpha1  : TEXCOORD1;
	};

	struct g2f
	{
		float4 position : SV_POSITION;
		float4 color    : COLOR;
	};

	sampler2D _MainTex;
	float4 _MainTex_ST;

	StructuredBuffer<lineData> _LineDataBuffer;

	float4 _Color;

	// --------------------------------------------------------------------
	// Vertex Shader
	// --------------------------------------------------------------------
	v2g vert(uint id : SV_VertexID)
	{
		v2g o = (v2g)0;
		o.vertex0 = UnityObjectToClipPos(float4(_LineDataBuffer[id].position0, 1.0));
		o.alpha0  = _LineDataBuffer[id].alpha0;
		o.vertex1 = UnityObjectToClipPos(float4(_LineDataBuffer[id].position1, 1.0));
		o.alpha1  = _LineDataBuffer[id].alpha1;
		return o;
	}

	// --------------------------------------------------------------------
	// Geometry Shader
	// --------------------------------------------------------------------
	[maxvertexcount(2)]
	void geom(point v2g points[1], inout LineStream<g2f> stream)
	{
		g2f o = (g2f)0;

		float4 pos0 = points[0].vertex0;
		float4 pos1 = points[0].vertex1;

		float a0 = points[0].alpha0;
		float a1 = points[0].alpha1;

		o.color = float4(1, 1, 1, 1) * a0;
		o.position = pos0;
		stream.Append(o);

		o.color = float4(1, 1, 1, 1) * a1;
		o.position = pos1;
		stream.Append(o);

		stream.RestartStrip();
	}

	// --------------------------------------------------------------------
	// Fragment Shader
	// --------------------------------------------------------------------
	fixed4 frag(g2f i) : SV_Target
	{
		fixed4 col = i.color * _Color;
		return col;
	}
	ENDCG

	SubShader
	{
		Tags{ "RenderType" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent" }
		LOD 100
		
		Blend One One
		ZWrite Off

		Pass
		{
			CGPROGRAM
			#pragma target   5.0
			#pragma vertex   vert
			#pragma geometry geom
			#pragma fragment frag
			ENDCG
		}
	}
}
