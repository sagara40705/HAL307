#ifndef MONITOR_BREAK_INCLUDED
#define MONITOR_BREAK_INCLUDED

// ============================================================
// 頂点シェーダーへ入ってくるデータ
// ============================================================
struct Attributes
{
    // モデル自身を基準にした頂点座標
    float4 positionOS : POSITION;

    // モデルに設定されているUV座標
    float2 uv : TEXCOORD0;
};

// ============================================================
// 頂点シェーダーからフラグメントシェーダーへ渡すデータ
// ============================================================
struct Varyings
{
    // 画面へ描画するためのクリップ空間座標
    float4 positionHCS : SV_POSITION;

    // Render Textureと破壊マスクの両方で使用するUV座標
    float2 uv : TEXCOORD0;
};

// Render Texture本体と、そのサンプラーを宣言する。
TEXTURE2D(_BaseMap);
SAMPLER(sampler_BaseMap);

// ============================================================
// Materialごとに保持するパラメーター
//
// Properties側と同じ名前にする必要がある。
// SRP Batcherへ対応できるよう、1つのCBUFFERへまとめる。
// ============================================================
CBUFFER_START(UnityPerMaterial)
float4 _BaseMap_ST;

half _Monitorlight;
half _Saturation;
half _LedCount;
half _LedDotSize;
half _AmbientStrength;

float4 _HoleCenter;
float _HoleRadius;
float4 _BreakSize;
float _HoleSoftness;
float _CrackCount;
float _CrackSharpness;
float _CrackLength;
float _CrackStrength;
float _EdgeDarkness;

// ShaderLabの[Toggle]は、OFFなら0、ONなら1をこの値へ渡す。
// Material用CBUFFERにboolを直接置くと環境によってサイズが不安定になるため、
// 受け取りはfloatにし、処理内でboolへ変換する。
float _BreakAmount;
CBUFFER_END

// ============================================================
// 頂点シェーダー
// ============================================================
Varyings MonitorBreakVertex(Attributes input)
{
    Varyings output;

    // モデル座標から、最終的に画面へ表示できる座標へ変換する。
    output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);

    // MaterialのTilingとOffsetを反映したUVを作る。
    output.uv = TRANSFORM_TEX(input.uv, _BaseMap);

    return output;
}

// ============================================================
// モニター表示用の補助関数
// ============================================================

// 彩度を調整する。
// saturationが0なら白黒、1なら元の色、1より大きいと鮮やかになる。
half3 ApplySaturation(half3 color, half saturation)
{
    half gray = dot(color, half3(0.299h, 0.587h, 0.114h));
    return lerp(gray.xxx, color, saturation);
}

// UVを格子状に区切り、各マスの中央を明るくしてLEDの粒を作る。
half GetLedMask(float2 uv)
{
    float2 gridUV = frac(uv * _LedCount);
    float distanceFromCenter = distance(gridUV, float2(0.5, 0.5));

    return smoothstep(_LedDotSize, _LedDotSize * 0.5, distanceFromCenter);
}

// ============================================================
// 破壊表示用の補助関数
// ============================================================

// 同じ番号には同じ値を返す、簡易的なランダム関数。
// ヒビごとの長さや形を少しずつ変えるために使用する。
float Random01(float value)
{
    return frac(sin(value * 12.9898) * 43758.5453);
}

// モニター処理済みの色へ、穴・黒い崩れ・ヒビを重ねる。
half4 ApplyBreakEffect(half4 baseColor, float2 uv)
{
    // Break Amountは0/1だけで扱う。
    // 0.5より大きければtrue、そうでなければfalseになる。
    bool breakEnabled = (_BreakAmount > 0.5);

    // OFFなら破壊計算をせず、モニター映像をそのまま返す。
    if (!breakEnabled)
    {
        return baseColor;
    }

    // 破壊中心から現在のピクセルまでの距離を求める。
    float2 difference = uv - _HoleCenter.xy;

    // XとYを別々に割ることで、真円ではなく楕円形の破壊範囲を作る。
    // 0除算を防ぐため、最小値を0.001に制限する。
    float2 safeBreakSize = max(_BreakSize.xy, float2(0.001, 0.001));
    float2 localPosition = difference / safeBreakSize;

    float distanceFromCenter = length(localPosition);
    float angle = atan2(localPosition.y, localPosition.x);

    // 破壊範囲の外側へ行くほど0になるマスク。
    float breakArea = 1.0 - smoothstep(1.0, 1.35, distanceFromCenter);

    // 角度を0～1へ変換し、円周を24個に分割する。
    // 分割ごとに半径を変えて、穴の輪郭をガタガタさせる。
    float angle01 = (angle + 3.14159265) / 6.2831853;
    float jaggedId = floor(angle01 * 24.0);
    float jaggedRandom = Random01(jaggedId);
    float jaggedOffset = lerp(-0.18, 0.18, jaggedRandom);
    float brokenRadius = _HoleRadius + jaggedOffset * _HoleRadius;

    // 穴の内側が0、外側が1になる。
    // 最後にアルファへ掛けることで、穴の内側を透明にする。
    float holeMask = smoothstep(
        brokenRadius,
        brokenRadius + _HoleSoftness,
        distanceFromCenter
    );

    // 穴の周辺を暗くし、割れて崩れた縁を表現する。
    float darkCore = 1.0 - smoothstep(
        _HoleRadius * 0.35,
        _HoleRadius + 0.35,
        distanceFromCenter
    );
    darkCore *= breakArea * _EdgeDarkness;

    // 中心から放射状に伸びるヒビを作る。
    float crackId = floor(angle01 * _CrackCount);
    float crackRandom = Random01(crackId);
    float crackLength = _CrackLength * lerp(0.45, 1.0, crackRandom);

    float crackWave = saturate(cos(angle * _CrackCount + crackRandom * 0.7));
    float crackLine = pow(crackWave, _CrackSharpness);

    // ヒビを穴の少し外側から開始し、指定した長さで止める。
    float crackStart = _HoleRadius * 0.55;
    float crackFromCenter = smoothstep(
        crackStart,
        crackStart + _HoleSoftness,
        distanceFromCenter
    );
    float crackEndMask = 1.0 - smoothstep(
        crackLength,
        crackLength + _HoleSoftness,
        distanceFromCenter
    );

    float crackMask = crackLine;
    crackMask *= crackFromCenter * crackEndMask;
    crackMask *= breakArea * _CrackStrength;

    // 少し角度と本数を変えた線を重ね、枝分かれしたヒビを作る。
    float branchWave = saturate(cos((angle + 0.45) * (_CrackCount * 0.55)));
    float branchLine = pow(branchWave, _CrackSharpness * 1.25);
    float branchMask = branchLine * crackEndMask * breakArea;
    branchMask *= _CrackStrength * 0.45;

    // モニター映像を残したまま、破壊部分だけ順番に暗くする。
    baseColor.rgb = lerp(baseColor.rgb, baseColor.rgb * 0.12, darkCore);
    baseColor.rgb = lerp(baseColor.rgb, half3(0.03h, 0.025h, 0.02h), crackMask);
    baseColor.rgb = lerp(baseColor.rgb, half3(0.08h, 0.075h, 0.07h), branchMask);

    // 穴の中心だけ透明にする。
    baseColor.a *= holeMask;

    return baseColor;
}

// ============================================================
// フラグメントシェーダー
//
// 1. Render Textureを読む
// 2. モニターらしい色とLED粒へ加工する
// 3. Break AmountがONなら破壊表現を重ねる
// ============================================================
half4 MonitorBreakFragment(Varyings input) : SV_Target
{
    // Main Cameraなどが描画したRender Textureを取得する。
    half4 textureColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);

    half3 monitorColor = ApplySaturation(textureColor.rgb, _Saturation);

    // LEDの粒ごとに明暗を付ける。
    half ledMask = GetLedMask(input.uv);
    monitorColor *= lerp(0.6h, 1.2h, ledMask);

    // 暗部を少し持ち上げたあと、モニター全体の明るさを掛ける。
    monitorColor += _AmbientStrength;
    monitorColor *= _Monitorlight;

    half4 monitorOutput = half4(monitorColor, textureColor.a);

    // 最後に破壊表現を重ねた色を返す。
    return ApplyBreakEffect(monitorOutput, input.uv);
}

#endif
