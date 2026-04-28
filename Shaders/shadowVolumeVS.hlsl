#include "constantBuffers.hlsli"
 
struct VS_INPUT
{
    float3 Pos : POSITION;
    float3 Norm : NORMAL;
};

struct GS_INPUT
{
    float3 WorldPos : POSITION0;
};

GS_INPUT VS(VS_INPUT input)
{
    GS_INPUT output;
    output.WorldPos = mul(Model, float4(input.Pos, 1.0f)).xyz;
    return output;
}