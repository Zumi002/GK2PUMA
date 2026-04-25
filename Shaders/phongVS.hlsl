#include "phong.hlsli"

cbuffer ConstantBuffer : register(b0)
{
    matrix Model;
    matrix ModelInvT;
    matrix View;
    matrix Projection;
    float4 SurfaceColor;
    float4 CameraPos;
}

struct VS_INPUT
{
    float3 Pos : POSITION;
    float3 Norm : NORMAL;
};

PS_INPUT VS(VS_INPUT input)
{
    PS_INPUT output = (PS_INPUT) 0;

    float4 worldPos = mul(float4(input.Pos, 1.0f), Model);
    output.WorldPos = (float3)worldPos;
    output.Pos = mul(mul(worldPos, View), Projection);
    output.Norm = mul(input.Norm, (float3x3) ModelInvT);
    output.View = CameraPos.xyz - output.WorldPos;

    return output;
}
