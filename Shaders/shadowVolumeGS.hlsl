cbuffer ConstantBufferCamera : register(b1)
{
    matrix View;
    matrix Projection;
}

#define NLIGHTS 2

cbuffer LightBuffer : register(b3)
{
    float4 LightPos[NLIGHTS];
    float4 LightColor[NLIGHTS];
}

struct GS_INPUT
{
    float3 WorldPos : POSITION0;
};

struct PS_INPUT
{
    float4 Pos : SV_POSITION;
};

float4 Transform(float3 vert)
{
    float4 v = mul(View, float4(vert, 1.0));
    return mul(Projection, v);
}

const float EPS = 0.001;
static const float SHADOW_LENGTH = 100.0; // Ustaw 100, żeby cień sięgał podłogi

void EmitQuad(float3 vStart, float3 vEnd, float3 lightPos, inout TriangleStream<PS_INPUT> output)
{
    PS_INPUT o;
    
    float3 dirStart = -normalize(vStart - lightPos);
    float3 dirEnd = -normalize(vEnd - lightPos);

    float4 s0 = Transform(vStart + dirStart * EPS);
    float4 s1 = Transform(vStart + dirStart * SHADOW_LENGTH);
    float4 e0 = Transform(vEnd + dirEnd * EPS);
    float4 e1 = Transform(vEnd + dirEnd * SHADOW_LENGTH);

    // Krawędź boczna dla układu Clockwise (CW)
    o.Pos = e0;
    output.Append(o);
    o.Pos = e1;
    output.Append(o);
    o.Pos = s1;
    output.Append(o);
    o.Pos = s0;
    output.Append(o);

    output.RestartStrip();
}

[maxvertexcount(18)]
void GS(triangle GS_INPUT input[3], inout TriangleStream<PS_INPUT> output)
{
    float3 lPos = LightPos[0].xyz;

    float3 v0 = input[0].WorldPos;
    float3 v1 = input[1].WorldPos;
    float3 v2 = input[2].WorldPos;

    float3 faceNorm = cross(v1 - v0, v2 - v0);
    float3 lightDir = -(lPos - v0);

    // Tylko oświetlone trójkąty
    if (dot(faceNorm, lightDir) >= 0.0)
    {
        //return;
    }

    // 1. Wyciągnij 3 ściany boczne
    EmitQuad(v0, v1, lPos, output);
    EmitQuad(v1, v2, lPos, output);
    EmitQuad(v2, v0, lPos, output);

    // 2. Zamknij przód (Front Cap)
    PS_INPUT o;
    o.Pos = Transform(v0 - normalize(v0 - lPos) * EPS);
    output.Append(o);
    o.Pos = Transform(v1 - normalize(v1 - lPos) * EPS);
    output.Append(o);
    o.Pos = Transform(v2 - normalize(v2 - lPos) * EPS);
    output.Append(o);
    output.RestartStrip();

    // 3. Zamknij tył (Back Cap - odwrócona kolejność dla CW)
    o.Pos = Transform(v2  - normalize(v2 - lPos) * SHADOW_LENGTH);
    output.Append(o);
    o.Pos = Transform(v1 - normalize(v1 - lPos) * SHADOW_LENGTH);
    output.Append(o);
    o.Pos = Transform(v0 - normalize(v0 - lPos) * SHADOW_LENGTH);
    output.Append(o);
    output.RestartStrip();
}