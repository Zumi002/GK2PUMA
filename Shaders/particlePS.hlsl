Texture2D SparkTexture : register(t0);
SamplerState Sampler : register(s0);

struct PS_INPUT
{
    float4 Pos : SV_POSITION;
    float2 UV : TEXCOORD0;
    float AgeAlpha : COLOR0;
};

float4 PS(PS_INPUT input) : SV_Target
{
    float4 texColor = SparkTexture.Sample(Sampler, input.UV);
    float3 sparkColor = float3(1.0f, 1.0f, 1.0f);
    float finalAlpha = texColor.a * input.AgeAlpha;
    
    return float4(sparkColor * texColor.rgb, finalAlpha);
}