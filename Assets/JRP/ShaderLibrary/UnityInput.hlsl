#ifndef JRP_UNITY_INPUT_INCLUDED
#define JRP_UNITY_INPUT_INCLUDED

CBUFFER_START(UnityPerDraw)
	float4x4 unity_ObjectToWorld;
	float4x4 unity_WorldToObject;
	float4 unity_LODFade;
	real4 unity_WorldTransformParams;
CBUFFER_END

float4x4 unity_Prev_MatrixM;
float4x4 unity_Prev_MatrixIM;
float4x4 unity_MatrixVP;
float4x4 unity_MatrixV;
float4x4 glstate_matrix_projection;

#endif