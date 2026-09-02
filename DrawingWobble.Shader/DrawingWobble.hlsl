Texture2D SourceTexture : register(t0);
SamplerState SourceSampler : register(s0);

cbuffer Constants : register(b0)
{
    float4 inputBounds : packoffset(c0);
    float amplitude : packoffset(c1.x);
    float scale : packoffset(c1.y);
    float edgeRadius : packoffset(c1.z);
    float edgeFocus : packoffset(c1.w);
    int holdIndex : packoffset(c2.x);
    int seed : packoffset(c2.y);
};

static const int RingCount = 8;
static const float TwoPi = 6.283185307179586;

uint4 Hash32(uint4 value)
{
    value ^= value >> 16;
    value *= 0x7feb352du;
    value ^= value >> 15;
    value *= 0x846ca68bu;
    value ^= value >> 16;
    return value;
}

float4 LatticeHash(uint4 lattice)
{
    return Hash32(lattice) / 4294967295.0 * 2.0 - 1.0;
}

float2 WobbleOffset(float2 position)
{
    float2 cell = floor(position);
    float2 fraction = position - cell;
    float2 weight = fraction * fraction * fraction * (fraction * (fraction * 6.0 - 15.0) + 10.0);
    int2 corner = int2(cell);
    uint4 lattice =
        asuint(int4(corner.x, corner.x + 1, corner.x, corner.x + 1)) * 0x9e3779b9u ^
        asuint(int4(corner.y, corner.y, corner.y + 1, corner.y + 1)) * 0x85ebca6bu ^
        (asuint(seed) * 0xc2b2ae35u ^ asuint(holdIndex) * 0x27d4eb2fu);
    float4 offsetX = LatticeHash(lattice);
    float4 offsetY = LatticeHash(lattice ^ 0x165667b1u);
    float2 lower = lerp(float2(offsetX.x, offsetY.x), float2(offsetX.y, offsetY.y), weight.x);
    float2 upper = lerp(float2(offsetX.z, offsetY.z), float2(offsetX.w, offsetY.w), weight.x);
    return lerp(lower, upper, weight.y);
}

float4 SampleAt(float2 uv, float2 pixelStep, float2 scenePosition, float2 offset)
{
    float2 minimum = inputBounds.xy + 0.5;
    float2 maximum = inputBounds.zw - 0.5;
    float2 target = clamp(scenePosition + offset, minimum, maximum);
    return SourceTexture.SampleLevel(SourceSampler, uv + (target - scenePosition) * pixelStep, 0);
}

float4 SampleCovered(float2 uv, float2 pixelStep, float2 scenePosition, float2 offset)
{
    float2 target = scenePosition + offset;
    float2 coverage = saturate(min(target - inputBounds.xy, inputBounds.zw - target) + 1.0);
    return SampleAt(uv, pixelStep, scenePosition, offset) * (coverage.x * coverage.y);
}

float EdgeWeight(float2 uv, float2 pixelStep, float2 scenePosition, float4 source)
{
    if (edgeFocus <= 0.0 || edgeRadius <= 0.0)
        return 1.0;

    float4 deviation = 0.0;

    [unroll]
    for (int index = 0; index < RingCount; index++)
    {
        float angle = TwoPi * index / RingCount;
        float2 direction = float2(cos(angle), sin(angle));
        deviation = max(deviation, abs(SampleAt(uv, pixelStep, scenePosition, edgeRadius * direction) - source));
    }

    float contrast = max(max(deviation.r, deviation.g), max(deviation.b, deviation.a));
    return lerp(1.0, saturate(contrast), edgeFocus);
}

float4 main(
    float4 position : SV_POSITION,
    float4 scenePosition : SCENE_POSITION,
    float4 uv0 : TEXCOORD0
) : SV_TARGET
{
    float4 source = SourceTexture.SampleLevel(SourceSampler, uv0.xy, 0);
    if (amplitude <= 0.0)
        return source;

    float2 center = 0.5 * (inputBounds.xy + inputBounds.zw);
    float weight = EdgeWeight(uv0.xy, uv0.zw, scenePosition.xy, source);
    float2 offset = amplitude * weight * WobbleOffset((scenePosition.xy - center) / scale);
    return SampleCovered(uv0.xy, uv0.zw, scenePosition.xy, offset);
}
