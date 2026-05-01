#include "constantBuffers.hlsli"
#include "lighting.hlsli"

struct PS_INPUT
{
    float4 Pos : SV_POSITION;
    float3 Norm : NORMAL;
};

float4 PS(PS_INPUT input) : SV_Target
{
    return float4(SurfaceColor.xyz * ka*2, 1);
}