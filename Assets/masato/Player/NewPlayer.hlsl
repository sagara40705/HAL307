#ifndef NEWSWEAT_SUB_INCLUDED
#define NEWSWEAT_SUB_INCLUDED

float Random01(float2 p)
{
    return frac(sin(dot(p, float2(12.9898, 78.233))) * 43758.5453);
}

float2 GetSweatCellID(float2 coord, float density)
{
    return floor(coord * density);
}

float GetSweatExist(float2 coord, float density, float threshold)
{
    float2 cellID = GetSweatCellID(coord, density);
    float r = Random01(cellID + float2(91.1, 17.3));

    return step(threshold, r);
}

float2 GetSweatLocal(float2 coord, float density)
{
    float2 tiledCoord = coord * density;

    float2 cellID = floor(tiledCoord);
    float2 cellUV = frac(tiledCoord);

    float randX = Random01(cellID);
    float randY = Random01(cellID + float2(13.37, 27.91));

    float2 randomOffset = float2(randX, randY) - 0.5;
    randomOffset *= 0.4;

    float2 center = float2(0.5, 0.5) + randomOffset;

    return cellUV - center;
}

float GetSweatSizeScale(float2 coord, float density)
{
    float2 cellID = GetSweatCellID(coord, density);
    float r = Random01(cellID + float2(41.7, 9.2));

    return lerp(0.5, 1.3, r);
}

float GetSweatMask(float2 coord, float density, float threshold, float dropSize, float edgeSoftness)
{
    float2 local = GetSweatLocal(coord, density);

    float sizeScale = GetSweatSizeScale(coord, density);
    float finalSize = dropSize * sizeScale;

    local.x *= 1.6;
    local.y *= 0.8;

    float roundPart =
        1.0 - smoothstep(finalSize, finalSize + edgeSoftness, length(local));

    float2 tailPos = float2(0.0, -finalSize * 1.2);
    float tailDist = distance(local, tailPos);

    float tailPart =
        1.0 - smoothstep(finalSize * 0.4, finalSize * 0.8, tailDist);

    float exist = GetSweatExist(coord, density, threshold);

    return saturate(max(roundPart, tailPart)) * exist;
}

float GetSweatHighlight(float2 coord, float density, float dropSize)
{
    float2 local = GetSweatLocal(coord, density);

    float sizeScale = GetSweatSizeScale(coord, density);
    float finalSize = dropSize * sizeScale;

    // 雫の左上あたりに白い光を置く
    float2 highlightLocal = local - float2(-finalSize * 0.25, finalSize * 0.25);

    // 細長いハイライトにする
    highlightLocal.x *= 2.5;
    highlightLocal.y *= 0.7;

    float highlightDist = length(highlightLocal);

    return 1.0 - smoothstep(
        finalSize * 0.08,
        finalSize * 0.35,
        highlightDist
    );
}

#endif
