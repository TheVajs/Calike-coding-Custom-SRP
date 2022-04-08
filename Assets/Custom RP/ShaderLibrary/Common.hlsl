#ifndef CUSTOM_COMMON_INCLUDED
#define CUSTOM_COMMON_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
#include "UnityInput.hlsl"

#define UNITY_MATRIX_M unity_ObjectToWorld
#define UNITY_MATRIX_I_M unity_WorldToObject
#define UNITY_MATRIX_V unity_MatrixV
#define UNITY_MATRIX_VP unity_MatrixVP
#define UNITY_MATRIX_P glstate_matrix_projection

/*
What UnityInstancing.hlsl does is redefine those macros to access the instanced data arrays instead. 
But to make that work it needs to know the index of the object that's currently being rendered. The 
index is provided via the vertex data, so we have to make it available. UnityInstancing.hlsl defines
macros to make this easy, but they assume that our vertex function has a struct parameter.
*/
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl" // UNITY_TRANSFER_INSTANCE_ID
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/SpaceTransforms.hlsl"
	
#endif