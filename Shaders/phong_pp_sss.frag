#version 330 core

uniform vec4    MaterialColor = vec4(1,1,0,1);
uniform vec2    MaterialSpecular = vec2(0,1);
uniform vec4    MaterialColorEmissive = vec4(0,0,0,1);
uniform vec2    MaterialTiling = vec2(1, 1);
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


// Screen space shadows parameters
// -------------------------------

// Max steps taken by the ray
const int SSS_MAX_STEPS = 16;

// Max distance travelled by the ray
const float SSS_MAX_RAY_DISTANCE = 0.02;

// Max depth delta considered for occlusion
const float SSS_THICKNESS = 0.005;

// Max depth threshold (limited to avoid artifacts at long distances)
const float SSS_MAX_DEPTH = 5;


float saturate(float v)
{
    return clamp(v, 0, 1);
}

float GetLinearDepth(float z)
{
    return 1.0 / (EnvZBufferParams.z * z + EnvZBufferParams.w);
}

float ComputeAttenuation(Light light, vec3 worldPos)
{
    float d = length(worldPos - light.position) / light.range;

    return saturate(saturate(5 * (1 - d)) / (1 + 25 * d * d));
}

vec3 ComputeDirectional(Light light, vec3 worldPos, vec3 worldNormal, vec4 materialColor)
{
    float d = clamp(-dot(worldNormal, light.direction), 0, 1);
    vec3  v = normalize(ViewPos - worldPos);
    // Light dir is from light to point, but we want the other way around, hence the V - L
    vec3  h =  normalize(v - light.direction);
    float s = MaterialSpecular.x * pow(max(dot(h, worldNormal), 0), MaterialSpecular.y);

    return clamp(d * materialColor.xyz + s, 0, 1) * light.color.rgb * light.intensity;
}

vec3 ComputePoint(Light light, vec3 worldPos, vec3 worldNormal, vec4 materialColor)
{
    vec3  lightDir = normalize(worldPos - light.position);
    float d = clamp(-dot(worldNormal, lightDir), 0, 1);
    vec3  v = normalize(ViewPos - worldPos);
    // Light dir is from light to point, but we want the other way around, hence the V - L
    vec3  h =  normalize(v - lightDir);
    float s = MaterialSpecular.x * pow(max(dot(h, worldNormal), 0), MaterialSpecular.y);

    return clamp(d * materialColor.xyz + s, 0, 1) * light.color.rgb * light.intensity * ComputeAttenuation(light, worldPos);
}

vec3 ComputeSpot(Light light, vec3 worldPos, vec3 worldNormal, vec4 materialColor)
{
    vec3  lightDir = normalize(worldPos - light.position);
    float cosAngle = dot(lightDir, light.direction);
    float spot = clamp((cosAngle - light.spot.w) / (light.spot.z - light.spot.w), 0, 1);

    float d = spot * clamp(-dot(worldNormal, lightDir), 0, 1);

    vec3  v = normalize(ViewPos - worldPos);
    // Light dir is from light to point, but we want the other way around, hence the V - L
    vec3  h =  normalize(v - lightDir);
    float s = spot * max(0, spot * MaterialSpecular.x * pow(max(dot(h, worldNormal), 0), MaterialSpecular.y));

    // Compute shadow
    vec4 shadowProj = light.shadowMatrix * vec4(worldPos, 1);
    // Perpective divide
    shadowProj = shadowProj / shadowProj.w;
    // Convert to UV (NDC range [-1,1] to tex coord range [0, 1])
    shadowProj = shadowProj * 0.5 + 0.5;
    // Fetch depth while comparing
    float bias = 0.0;
    float shadowFactor = texture(light.shadowmap, shadowProj.xyz);
   
    return shadowFactor * clamp(d * materialColor.xyz + s, 0, 1) * light.color.rgb * light.intensity * ComputeAttenuation(light, worldPos);
}

vec3 ComputeLight(Light light, vec3 worldPos, vec3 worldNormal, vec4 materialColor)
{
    if (light.type == 0)
    {
        return ComputeDirectional(light, worldPos, worldNormal, materialColor);
    }
    else if (light.type == 1)
    {
        return ComputePoint(light, worldPos, worldNormal, materialColor);
    }
    else if (light.type == 2)
    {
        return ComputeSpot(light, worldPos, worldNormal, materialColor);
    }
}

float ScreenSpaceShadow(Light light, vec3 worldPos)
{
    float stepLength = SSS_MAX_RAY_DISTANCE / float(SSS_MAX_STEPS);

    vec3 rayPos = (MatrixCamera * vec4(worldPos, 1.0)).xyz;

    // Find ray direction
    vec3 toLight;
    if (light.type == 0)
    {
        toLight = -light.direction;
    }
    else if (light.type == 1 || light.type == 2)
    {
        vec3 lightPos = (MatrixCamera * vec4(light.position, 1.0)).xyz;
        toLight = normalize(lightPos - rayPos);
    }

    vec3 rayStep = toLight * stepLength;

    float occlusion = 0.0;
    for (int i = 0; i < SSS_MAX_STEPS; i++)
    {
        rayPos += rayStep;

        // Determine current ray UV coords
        vec4 uv = MatrixProjection * vec4(rayPos, 1.0);
        uv = uv / uv.w;
        uv = uv * 0.5 + 0.5;

        // If ray UVs are valid, sample depth texture and compare to ray Z coord
        if (uv.x >= 0.0 && uv.x <= 1.0 && uv.y >= 0.0 && uv.y <= 1.0)
        {
            float depth = texture(EnvTextureDepth, uv.xy).r;
            depth = GetLinearDepth(depth);

            // Exit early if depth is too large
            if (depth > SSS_MAX_DEPTH) break;

            float depthDelta = -rayPos.z - depth;

            // If ray Z is greater than depth, mark pixel as occluded
            if (depthDelta > 0.0 && depthDelta < SSS_THICKNESS)
            {
                occlusion = 1.0;

                break;
            }
        }
    }

    // Convert to visibility
    return 1.0 - occlusion;
}

in vec3 fragPos;
in vec3 fragNormal;
in vec4 fragTangent;
in vec2 fragUV;

out vec4 OutputColor;

void main()
{
    vec3 worldPos = fragPos.xyz;
    vec3 worldNormal;
    if (HasTextureNormalMap)
    {
        vec3 n = normalize(fragNormal);
        vec3 t = normalize(fragTangent.xyz);

        // Create tangent space
        vec3 binormal = cross(n, t) * fragTangent.w;
        mat3 TBN = mat3(t, binormal, n);
        vec3 normalMap = texture(TextureNormalMap, fragUV * MaterialTiling).xyz * 2 - 1;

        worldNormal = TBN * normalMap;
    }
    else
    {
        worldNormal = normalize(fragNormal.xyz);
    }

    // Compute material color
    vec4 matColor = MaterialColor;
    if (HasTextureBaseColor) matColor *= texture(TextureBaseColor, fragUV * MaterialTiling);

    // Ambient component - get data from 4th mipmap of the cubemap (effective hardware blur)
    vec3 envLighting = EnvColor.xyz * MaterialColor.xyz * textureLod(EnvTextureCubeMap, worldNormal, 8).xyz;

    // Emissive component
    vec3 emissiveLighting = MaterialColorEmissive.rgb;

    // Direct light
    vec3 directLight = vec3(0,0,0);
    for (int i = 0; i < LightCount; i++)
    {
        directLight += ComputeLight(Lights[i], worldPos, worldNormal, matColor)
            * ScreenSpaceShadow(Lights[i], worldPos);
    }

    // Lighting
    vec3 c = emissiveLighting + envLighting + directLight;
    OutputColor = vec4(c, 1);

    // Fog
    float distToCamera = length(worldPos - ViewPos);
    float fogFactor = 1 / pow(2, EnvFogDensity * distToCamera * distToCamera);
    OutputColor = mix(EnvFogColor, OutputColor, fogFactor);
}
