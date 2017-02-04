Shader "Hidden/irishoak/ParticleOnSphere/ParticleRender"
{
	Properties
	{
		_MainTex("Texture", 2D) = "white" {}
		_ParticleRad("ParticleRadius", Float) = 0.05
	    _Color("Particle Color", Color) = (1,1,1,1)
	}

	CGINCLUDE
	#include "UnityCG.cginc"

	struct v2g
	{
		float3 pos   : POSITION_SV;
		float4 color : COLOR;
	};

	struct g2f
	{
		float4 pos   : POSITION;
		float2 tex   : TEXCOORD0;
		float4 color : COLOR;
	};

	struct ParticleData
	{
		float3 velocity;
		float3 position;
		float  age;
		float  pad0;
	};

	sampler2D _MainTex;
	float4 _MainTex_ST;

	StructuredBuffer<ParticleData> _ParticleBuffer;

	float    _ParticleRad;
	float4x4 _InvViewMatrix;
	fixed4   _Color;
	
	// --------------------------------------------------------------------
	// Vertex Shader
	// --------------------------------------------------------------------
	v2g vert(uint id : SV_VertexID)
	{
		v2g o = (v2g)0;
		o.pos   = _ParticleBuffer[id].position;
		float age = _ParticleBuffer[id].age;
		o.color = saturate(min(1.0, 5.0 - abs(5.0 - age * 10)));
		return o;
	}

	static const float3 g_positions[4] =
	{
		float3(-1, 1, 0),
		float3( 1, 1, 0),
		float3(-1,-1, 0),
		float3( 1,-1, 0),
	};

	static const float2 g_texcoords[4] =
	{
		float2(0, 0),
		float2(1, 0),
		float2(0, 1),
		float2(1, 1),
	};

	// --------------------------------------------------------------------
	// Geometry Shader
	// --------------------------------------------------------------------
	[maxvertexcount(4)]
	void geom(point v2g In[1], inout TriangleStream<g2f> SpriteStream)
	{
		g2f output = (g2f)0;
		[unroll]
		for (int i = 0; i < 4; i++)
		{
			float3 position = g_positions[i] * _ParticleRad;
			position = mul(_InvViewMatrix, position) + In[0].pos;
			output.pos = mul(UNITY_MATRIX_MVP, float4(position, 1.0));

			output.color = In[0].color;
			output.tex = g_texcoords[i];
			SpriteStream.Append(output);
		}
		SpriteStream.RestartStrip();
	}

	// --------------------------------------------------------------------
	// Fragment Shader
	// --------------------------------------------------------------------
	fixed4 frag(g2f input) : SV_Target
	{
		return tex2D(_MainTex, input.tex) * input.color * _Color;
	}
	ENDCG

	SubShader
	{
		Tags{ "RenderType" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent" }
		LOD 100

		ZWrite Off
		Blend One One

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