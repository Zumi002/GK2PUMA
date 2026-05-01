#include "constantBuffers.hlsli"
#include "lighting.hlsli"

Texture2D Texture : register(t0);
SamplerState Sampler : register(s0);

struct PS_INPUT
{
    float4 Pos : SV_POSITION;
    float2 UV : TEXCOORD0;
    float3 Norm : NORMAL;
};

float4 PS(PS_INPUT input) : SV_Target
{
    return float4(SurfaceColor.xyz * ka * 2 * Texture.Sample(Sampler, input.UV), 1);
}