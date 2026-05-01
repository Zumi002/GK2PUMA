#include "constantBuffers.hlsli"
#include "gPass.hlsli"

PS_INPUT VS(VS_INPUT input)
{
    PS_INPUT output = (PS_INPUT) 0;

    float4 worldPos = mul(Model, float4(input.Pos, 1.0f));
    output.WorldPos = (float3) worldPos;
    output.Pos = mul(Projection, mul(View, worldPos));
    output.Norm = normalize(mul(input.Norm, (float3x3) ModelInv));

    output.ClipDist = dot(worldPos, ClipPlane)-0.0001;
    
    return output;
}