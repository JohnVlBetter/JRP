#ifndef JRP_COMMON_INCLUDED
#define JRP_COMMON_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"

#include "UnityInput.hlsl"

#define UNITY_MATRIX_M        unity_ObjectToWorld
#define UNITY_MATRIX_I_M      unity_WorldToObject
#define UNITY_MATRIX_V        unity_MatrixV
#define UNITY_MATRIX_VP       unity_MatrixVP
#define UNITY_MATRIX_P        glstate_matrix_projection
#define UNITY_PREV_MATRIX_M   unity_Prev_MatrixM
#define UNITY_PREV_MATRIX_I_M unity_Prev_MatrixIM

#if defined(_SHADOW_MASK_ALWAYS) ||defined(_SHADOW_MASK_DISTANCE)
	#define SHADOWS_SHADOWMASK
#endif

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/SpaceTransforms.hlsl"

float DistanceSquared(float3 pA, float3 pB) {
	return dot(pA - pB, pA - pB);
}

#endif