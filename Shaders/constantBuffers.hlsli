
cbuffer ConstantBuffer : register(b0)
{
    matrix Model;
    matrix ModelInv;
}

cbuffer ConstantBufferCamera : register(b1)
{
    matrix View;
    matrix Projection;
    float4 ClipPlane;
    float3 CamPos;
    float Padding;
}

cbuffer ConstantBuffer : register(b2)
{
    float4 SurfaceColor;
}
