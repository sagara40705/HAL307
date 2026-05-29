#ifndef ILLUMINATION_INCLUDED
#define ILLUMINATION_INCLUDED

struct Attributes
{
    float4 positionOS : POSITION;
    float2 uv : TEXCOORD0;
};

struct Varyings
{
    float4 positionHCS : SV_POSITION;
    float2 uv : TEXCOORD0;
};

TEXTURE2D(_BaseMap);
SAMPLER(sampler_BaseMap);

CBUFFER_START(UnityPerMaterial)
    // ベースカラー
half4 _BaseColor;

    // BaseMap の Tiling / Offset
float4 _BaseMap_ST;

    // 発光色
float4 _EmissionColor;

    // 発光の強さ
float _EmissionIntensity;
CBUFFER_END

Varyings vert(
Attributes IN)
{
Varyings OUT;

    // 頂点座標を画面描画用座標に変換
    OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);

    // テクスチャの Tiling / Offset を反映した UV
    OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);

    return
OUT;
}

half4 frag(Varyings IN) : SV_Target
{
    // ベーステクスチャを読む
    half4 texColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv);

    // ベーステクスチャにベースカラーを掛ける
    half4 color = texColor * _BaseColor;

    // Emission を追加する
    // これで自分で発光しているように見える
    color.rgb *= _EmissionIntensity;

    return color;
}

#endif
