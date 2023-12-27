#version 330 core

uniform vec4    MaterialColor = vec4(1,1,0,1);
uniform vec2    MaterialSpecular = vec2(0,1);
uniform vec4    MaterialColorEmissive = vec4(0,0,0,1);
uniform vec4    EnvColor;
uniform vec4    EnvColorTop;
uniform vec4    EnvColorMid;
uniform vec4    EnvColorBottom;
uniform float   EnvFogDensity;
uniform vec4    EnvFogColor;
uniform vec3    ViewPos;

uniform mat4 MatrixCamera;
uniform mat4 MatrixProjection;

uniform sampler2D   TextureBaseColor;
uniform sampler2D   TextureNormalMap;
uniform samplerCube EnvTextureCubeMap;

uniform bool        HasTextureBaseColor;
uniform bool        HasTextureNormalMap;

const int MAX_LIGHTS = 8;
struct Light
{
    int             type;
    vec3            position;
    vec3            direction;
    vec4            color;
    float           intensity;
    vec4            spot;
    float           range;
    bool            shadowmapEnable;
    sampler2DShadow shadowmap;
    mat4            shadowMatrix;
};
uniform int     LightCount;
uniform Light   Lights[MAX_LIGHTS];

uniform sampler2D EnvTextureDepth;
uniform vec4 EnvZBufferParams;
uniform vec2 EnvCameraParams;

float saturate(float v)
{
    return clamp(v, 0, 1);
}

float LinearDepth(float z)
{
    return 1.0 / (EnvZBufferParams.z * z + EnvZBufferParams.w);
}

in vec3 fragPos;
in vec3 fragNormal;
in vec4 fragTangent;
in vec2 fragUV;

out vec4 OutputColor;

void main()
{
    vec3 worldPos = fragPos.xyz;

    vec3 cameraPos = (MatrixCamera * vec4(worldPos, 1.0)).xyz;
    vec4 uv = MatrixProjection * vec4(cameraPos, 1.0);
    uv = uv / uv.w;
    uv = uv * 0.5 + 0.5;
    float d = texture(EnvTextureDepth, uv.xy).r;
    d = LinearDepth(d);
    d = (d - EnvCameraParams.x) / (EnvCameraParams.y - EnvCameraParams.x);
    OutputColor = vec4(d, d, d, 1);
}
