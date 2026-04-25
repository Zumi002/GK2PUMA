#include "constantBuffers.hlsli"
#include "blinnPhong.hlsli"

#define NLIGHTS 2

cbuffer LightBuffer : register(b3)
{
    float4 LightPos[NLIGHTS];
    float4 LightColor[NLIGHTS];
}

static const float ks = 0.3;
static const float kd = 0.7;
static const float ka = 0.1;
static const float m = 20.0;

float4 phong(float3 worldPos, float3 norm, float3 view)
{
    view = normalize(view);
    norm = normalize(norm);
    float3 color = SurfaceColor.rgb * ka;
    for (int k = 0; k < NLIGHTS; ++k)
    {
        float3 lightVec = normalize(LightPos[k].xyz - worldPos);
        float3 halfVec = normalize(view + lightVec);
        color += LightColor[k].xyz * kd * SurfaceColor.rgb * saturate(dot(norm, lightVec));
        color += LightColor[k].xyz* ks * pow(saturate(dot(norm, halfVec)), m);
    }

    return saturate(float4(color, 1.0f));
}

float4 PS(PS_INPUT input) : SV_Target
{
    return phong(input.WorldPos, input.Norm, input.View);
}
