cbuffer ConstantBufferCamera : register(b1)
{
    matrix View;
    matrix Projection;
}

struct VS_INPUT
{
    float3 Pos : POSITION0;
    float2 UV : TEXCOORD0;
    
    float3 CurrentPos : INSTANCE_CURRPOS;
    float Age : INSTANCE_AGE;
    float3 PreviousPos : INSTANCE_PREVPOS;
    float MaxAge : INSTANCE_MAXAGE;
};

struct PS_INPUT
{
    float4 Pos : SV_POSITION;
    float2 UV : TEXCOORD0;
    float AgeAlpha : COLOR0;
};

PS_INPUT VS(VS_INPUT input)
{
    PS_INPUT output;

    float3 basePos = lerp(input.PreviousPos, input.CurrentPos, input.UV.x);
    
    float thickness = 0.05f;
    basePos += float3(0.0f, 1.0f, 0.0f) * (input.UV.y * thickness);

    float4 v = mul(View, float4(basePos, 1.0f));
    output.Pos = mul(Projection, v);
    
    output.UV = input.UV;
    output.AgeAlpha = saturate(1.0f - (input.Age / input.MaxAge));

    return output;
}