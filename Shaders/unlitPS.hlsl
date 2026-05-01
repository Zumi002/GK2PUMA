#include "constantBuffers.hlsli"

struct PS_INPUT
{
    float4 Pos : SV_POSITION;
    float2 UV : TEXCOORD0;
    float3 Norm : NORMAL;
};

float4 PS(PS_INPUT input) : SV_Target
{
    return SurfaceColor;
}