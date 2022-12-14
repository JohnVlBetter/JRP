#ifndef JRP_SURFACE_INCLUDED
#define JRP_SURFACE_INCLUDED

struct Surface {
	float3 position;
	float3 normal;
	float3 color;
	float3 viewDirection;
	float3 interpolatedNormal;
	float  depth;
	float  alpha;
	float  metallic;
	float  smoothness;
	float  fresnelStrength;
	float  occlusion;
	float  dither;
};

#endif