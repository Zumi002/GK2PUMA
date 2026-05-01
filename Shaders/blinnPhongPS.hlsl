#include "constantBuffers.hlsli"
#include "blinnPhong.hlsli"
#include "lighting.hlsli"

Texture2D Texture : register(t0);
SamplerState Sampler : register(s0);

float4 phong(float3 worldPos, float2 uv, float3 norm, float3 view)
{
    view = normalize(view);
    norm = normalize(norm);
    float3 color = SurfaceColor * Texture.Sample(Sampler, uv);
    float3 finalColor = color * ka;
    for (int k = 0; k < NLIGHTS; ++k)
    {
        float3 lightVec = normalize(LightPos[k].xyz - worldPos);
        float3 halfVec = normalize(view + lightVec);
        finalColor += LightColor[k].xyz * kd * color.rgb * saturate(dot(norm, lightVec));
        finalColor += LightColor[k].xyz * ks * pow(saturate(dot(norm, halfVec)), m);
    }

    return saturate(float4(finalColor, 1.0f));
}

float4 PS(PS_INPUT input) : SV_Target
{
    return phong(input.WorldPos, input.UV, input.Norm, input.View);
}
