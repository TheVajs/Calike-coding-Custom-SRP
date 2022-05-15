#ifndef DEPTH_ONLY_INCLUDE
#define DEPTH_ONLY_INCLUDE

#include "Common.hlsl"

struct Attributes {
    float3 positionOS : POSITION;
    float3 normalOS : NORMAL;
};

struct Varyings {
    float4 positionCS : SV_POSITION;
    float3 normalWS : VAR_NORMAL;
};

Varyings Vert(Attributes input) {
    Varyings output;
    output.positionCS = TransformObjectToHClip(input.positionOS);
    output.normalWS = TransformObjectToWorldNormal(input.normalOS);
    return output;
}

void Frag(Varyings input) { // : SV_TARGET
}

#endif
