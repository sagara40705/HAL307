#ifndef FLAME_UI_INCLUDED
#define FLAME_UI_INCLUDED

// UI Image から渡されるスプライト
sampler2D _MainTex;

// テクスチャの tiling / offset
float4 _MainTex_ST;

// 炎描画ロジック
#include "Assets/masato/flame/flame_fire.hlsl"

// 頂点入力
struct Attributes
{
    float4 positionOS : POSITION;
    float2 uv : TEXCOORD0;
};

// 頂点からフラグメントへ渡す値
struct Varyings
{
    float4 positionCS : SV_POSITION;
    float2 uv : TEXCOORD0;
};

// Inspector の色
float4 _Color;

// リング形状
float _InnerRadius;
float _OuterRadius;

// 表示量
float _FillAmount;

// 開始角度
float _StartAngle;

// 頂点シェーダー
Varyings FlameVertex(Attributes input)
{
    Varyings output;

    // オブジェクト座標 -> 画面描画座標
    output.positionCS = UnityObjectToClipPos(input.positionOS);

    // UV に tiling / offset を反映
    output.uv = TRANSFORM_TEX(input.uv, _MainTex);

    return output;
}

// フラグメントシェーダー
float4 FlameFragment(Varyings input) : SV_Target
{
    // 円の中心
    float2 center = float2(0.5, 0.5);

    // 中心からの方向
    float2 dir = input.uv - center;

    // 中心からの距離
    float dist = length(dir);

    // 現在ピクセルの角度
    float angle = atan2(dir.y, dir.x);

    // 開始角度を 0～1 に直す
    float start01 = _StartAngle / 360.0;

    // 12時開始・時計回り基準に、開始角度オフセットを加える
    float angle01 = frac(0.25 - angle / (2.0 * 3.14159265) - start01);

    // Fill が 0 のときは何も出さない
    if (_FillAmount <= 0.0001)
    {
        return float4(0, 0, 0, 0);
    }

    // 進捗より先は透明
    if (angle01 > _FillAmount)
    {
        return float4(0, 0, 0, 0);
    }

    // ベースの UI 色
    float4 baseColor = tex2D(_MainTex, input.uv) * _Color;

    // 炎の見た目計算へ渡す
    return GetFlameColor(
        input.uv,
        baseColor,
        angle01,
        dist,
        _InnerRadius,
        _OuterRadius,
        _FillAmount,
        _StartAngle
    );
}
#endif
