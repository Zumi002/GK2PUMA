cbuffer ConstantBufferCamera : register(b1)
{
    matrix View;
    matrix Projection;
    float3 CamPos;
}

struct VS_INPUT
{
    float3 Pos : POSITION0;
    
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

    float3 velocity = input.CurrentPos - input.PreviousPos;
    float3 dir = normalize(velocity);

    float3 basePos = input.PreviousPos + velocity * 0.5f;

    float3 cameraPos = CamPos;
    float3 viewDir = normalize(cameraPos - basePos);

    float3 Ydir = normalize(cross(viewDir, dir));

    float XThickness = 0.2f;
    float YThickness = 0.04f;

    float2 centeredUV = input.Pos.xy * 2.0f - 1.0f;
    float3 pos = basePos + (centeredUV.x * XThickness * dir) + (centeredUV.y * YThickness * Ydir);

    float4 v = mul(View, float4(pos, 1));
    output.Pos = mul(Projection, v);
    
    output.UV = input.Pos.xy;
    output.AgeAlpha = saturate(1.0f - (input.Age / input.MaxAge));

    return output;
}