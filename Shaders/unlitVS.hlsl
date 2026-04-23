cbuffer ConstantBuffer : register(b0)
{
    matrix Model;
    matrix View;
    matrix Projection;
    float4 SurfaceColor;
}

struct VS_INPUT
{
    float3 Pos : POSITION;
    float3 Norm : NORMAL;
};

struct PS_INPUT
{
    float4 Pos : SV_POSITION;
    float3 Norm : NORMAL;
};

PS_INPUT VS(VS_INPUT input)
{
    PS_INPUT output = (PS_INPUT) 0;
    
    float4 worldPos = mul(float4(input.Pos, 1.0f), Model);
    float4 viewPos = mul(worldPos, View);
    output.Pos = mul(viewPos, Projection);
    
    output.Norm = mul(input.Norm, (float3x3) Model);
    
    return output;
}