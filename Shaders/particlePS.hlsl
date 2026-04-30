#include "particle.hlsli"

Texture2D Texture : register(t0);
Texture2D Texture2 : register(t1);
SamplerState Sampler : register(s0);

float4 PS(PS_INPUT input) : SV_Target
{
    float4 texColor = lerp(Texture.Sample(Sampler, input.UV), Texture2.Sample(Sampler, input.UV), input.Texture);
    float3 particleColor = lerp(float3(1.0f, 1.0f, 0), float3(1.0f, 0.3f, 0), input.AgeAlpha);
    float finalAlpha = texColor.a * input.AgeAlpha;
    
    return float4(particleColor * texColor.rgb, finalAlpha);
}