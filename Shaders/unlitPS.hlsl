cbuffer ConstantBuffer : register(b0)
{
    matrix Model;
    matrix ModelInvT;
    matrix View;
    matrix Projection;
    float4 SurfaceColor;
}

struct PS_INPUT
{
    float4 Pos : SV_POSITION;
    float3 Norm : NORMAL;
};

float4 PS(PS_INPUT input) : SV_Target
{
    return SurfaceColor;
}