Texture2D ColorMap : register(t0);
Texture2D NormalMap : register(t1);
Texture2D WorldPosMap : register(t2);
SamplerState Sampler : register(s0);

#define NLIGHTS 2

cbuffer ConstantBufferCamera : register(b1)
{
    matrix View;
    matrix Projection;
}

cbuffer LightBuffer : register(b3)
{
    float4 LightPos[NLIGHTS];
    float4 LightColor[NLIGHTS];
}

struct PS_INPUT
{
    float4 Pos : SV_POSITION;
    float2 TexCoord : TEXCOORD0;
};

float4 PS(PS_INPUT input) : SV_Target
{
    float3 albedo = ColorMap.Sample(Sampler, input.TexCoord).rgb;
    float3 normal = NormalMap.Sample(Sampler, input.TexCoord).xyz;
    float3 worldPos = WorldPosMap.Sample(Sampler, input.TexCoord).xyz;

    float3 cameraPos = mul(-View[3].xyz, (float3x3) View);
    float3 viewDir = normalize(cameraPos - worldPos);

    float ka = 0.1;
    float kd = 0.7;
    float ks = 0.3;
    float m = 20.0;

    float3 color = albedo * ka;

    for (int k = 0; k <  1; ++k)
    {
        float3 lightVec = normalize(LightPos[k].xyz - worldPos);
        float3 halfVec = normalize(viewDir + lightVec);
        
        color += LightColor[k].xyz * kd * albedo * saturate(dot(normal, lightVec));
        color += LightColor[k].xyz * ks * pow(saturate(dot(normal, halfVec)), m);
    }

    return saturate(float4(color, 1.0f));
}