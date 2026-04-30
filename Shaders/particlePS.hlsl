Texture2D Texture : register(t0);
SamplerState Sampler : register(s0);

struct PS_INPUT
{
    float4 Pos : SV_POSITION;
    float2 UV : TEXCOORD0;
    float AgeAlpha : COLOR0;
};

float4 PS(PS_INPUT input) : SV_Target
{
    float4 texColor = Texture.Sample(Sampler, input.UV);
    float3 particleColor = lerp(float3(1.0f, 1.0f, 0), float3(1.0f, 0.3f, 0), input.AgeAlpha);
    float finalAlpha = texColor.a * input.AgeAlpha;
    
    return float4(particleColor * texColor.rgb, finalAlpha);
}