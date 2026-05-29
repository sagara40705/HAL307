//#ifndef SWEAT_INCLUDED
//#define SWEAT_INCLUDED

//float ProceduralDropletMask(float2 uv)
//{
//    float2 center = float2(0.5, 0.5);
//    float dist = distance(uv, center);

//    float mask = 1.0 - smoothstep(0.12, 0.18, dist);
//    return mask;
//}

//float GetDropMask(float2 uv, float textureMask, float useTexture)
//{
//    float proceduralMask = ProceduralDropletMask(uv);
//    return lerp(proceduralMask, textureMask, useTexture);
//}

//half4 frag(Varyings IN) : SV_Target
//{
//    half4 color = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv) * _BaseColor;

//    float texMask = SAMPLE_TEXTURE2D(_DropMask, sampler_DropMask, IN.uv).r;
//    float dropMask = GetDropMask(IN.uv, texMask, _UseDropTexture);

//    color.rgb += dropMask * _DropStrength;

//    return color;
//}

//#endif
