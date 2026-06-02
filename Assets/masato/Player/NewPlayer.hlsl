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

#endif
