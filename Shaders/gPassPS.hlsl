#include "constantBuffers.hlsli"
#include "gPass.hlsli"

PS_OUTPUT PS(PS_INPUT input)
{
    PS_OUTPUT output;
    output.Color = SurfaceColor;
    output.Normal = float4(normalize(input.Norm), 1.0f);
    output.WorldPos = float4(input.WorldPos, 1.0f);
    
    return output;
}