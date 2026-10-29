struct Attributes
{
    float4 positionOS : POSITION;
    float2 uv : TEXCOORD0;
};

struct Varyings
{
    float4 positionHCS : SV_POSITION;
    float2 uv : TEXCOORD0;
    float2 monitorUV : TEXCOORD1;
};

TEXTURE2D(_BaseMap);
SAMPLER(sampler_BaseMap);

CBUFFER_START(UnityPerMaterial)
float4 _BaseMap_ST;
half _Monitorlight;
half _Saturation;
half _LedCount;
half _LedDotSize;
half _AmbientStrength;
float _SidePanelLayout;
CBUFFER_END

Varyings vert(Attributes IN)
{
    Varyings OUT;
    OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
    OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);
    OUT.monitorUV = OUT.uv;
    if (_SidePanelLayout > 0.5)
    {
        // SM_FullStage 3P/5P: negative-X panel is rotated in its UV island.
        // Keep the original UV for the LED mask so its existing density is preserved.
        if (IN.uv.x < 0.5)
        {
            float2 panel = (IN.uv - float2(0.0098898, 0.2499135)) / float2(0.2499802, 0.2302413);
            OUT.monitorUV = float2(1.0 - panel.y, panel.x);
        }
        else
        {
            OUT.monitorUV = (IN.uv - float2(0.5198237, 0.0098759)) / float2(0.2302273, 0.2499672);
        }
    }
    return OUT;
}

half3 ApplySaturation(half3 color, half saturation)
{
    half gray = dot(color, half3(0.299, 0.587, 0.114));
    return lerp(gray.xxx, color, saturation);
}

half GetLedMask(float2 uv)
{
    float2 gridUV = frac(uv * _LedCount);
    float dist = distance(gridUV, float2(0.5, 0.5));

    return smoothstep(_LedDotSize, _LedDotSize * 0.5, dist);
}

half4 frag(Varyings IN) : SV_Target
{
    half4 texColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.monitorUV);

    half3 color = texColor.rgb;

    color = ApplySaturation(color, _Saturation);

    half ledMask = GetLedMask(IN.uv);
    color *= lerp(0.6h, 1.2h, ledMask);

    color += _AmbientStrength;
    color *= _Monitorlight;

    return half4(color, texColor.a);
}
