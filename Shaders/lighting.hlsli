#define NLIGHTS 2

static const float ks = 0.3;
static const float kd = 0.7;
static const float ka = 0.1;
static const float m = 20.0;

cbuffer LightBuffer : register(b3)
{
    float4 LightPos[NLIGHTS];
    float4 LightColor[NLIGHTS];
}