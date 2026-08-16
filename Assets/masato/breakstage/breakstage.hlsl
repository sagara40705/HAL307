#ifndef BREAKSTAGE_INCLUDED
#define BREAKSTAGE_INCLUDED

sampler2D _MainTex;
float4 _MainTex_ST;

float4 _HoleCenter;
float _HoleRadius;
float4 _BreakSize;
float _HoleSoftness;

float _CrackCount;
float _CrackSharpness;
float _CrackLength;
float _CrackStrength;
float _EdgeDarkness;
float _BreakAmount;

struct Attributes
{
    float4 vertex : POSITION;
    float2 uv : TEXCOORD0;
    float4 color : COLOR;
};

struct Varyings
{
    float4 position : SV_POSITION;
    float2 uv : TEXCOORD0;
    float4 color : COLOR;
};

Varyings BreakStageVertex(Attributes input)
{
    Varyings output;

    output.position = UnityObjectToClipPos(input.vertex);
    output.uv = TRANSFORM_TEX(input.uv, _MainTex);
    output.color = input.color;

    return output;
}

// 0～1のランダムっぽい値を作る関数
// ヒビごとに長さや濃さを変えるために使う
float Random01(float value)
{
    return frac(sin(value * 12.9898) * 43758.5453);
}

float4 BreakStageFragment(Varyings input) : SV_Target
{
    float4 baseColor = tex2D(_MainTex, input.uv) * input.color;

    // BreakAmount が 0 のときは通常表示
    if (_BreakAmount <= 0.001)
    {
        return baseColor;
    }

    float2 uv = input.uv;

    // 破壊中心からの距離
    float2 diff = uv - _HoleCenter.xy;

    // 破壊範囲を横長・縦長に調整する
    // ここが前の丸穴版との大きな違い
    float2 breakSize = max(_BreakSize.xy, float2(0.001, 0.001));
    float2 local = diff / breakSize;

    // 中心からの距離
    // localを使うので、円ではなく指定サイズに合わせた楕円範囲になる
    float dist = length(local);

    // 中心から見た角度
    float angle = atan2(local.y, local.x);

    // 破壊が影響する範囲
    // 1に近いほど破壊範囲内
    float breakArea = 1.0 - smoothstep(1.0, 1.35, dist);

    // ==============================
    // ガタガタした中心崩れ
    // ==============================

    float angle01 = (angle + 3.14159265) / 6.2831853;
    float jagId = floor(angle01 * 24.0);
    float jagRandom = Random01(jagId);

    // 穴の外周を少しガタガタにする
    float jagged = lerp(-0.18, 0.18, jagRandom);

    // BreakAmountが増えるほど中心崩れが大きくなる
    float currentHoleRadius = _HoleRadius * lerp(0.25, 1.0, _BreakAmount);

    // 真円ではなく、少し崩れた形の穴にする
    float brokenRadius = currentHoleRadius + jagged * _HoleRadius;

    // 穴マスク
    // 0に近いほど透明、1に近いほど表示
    float holeMask = smoothstep(
        brokenRadius,
        brokenRadius + _HoleSoftness,
        dist
    );

    // いきなり透明にすると床抜けっぽく見えるので、
    // BreakAmountが高いときだけ透明を強くする
    float holeAlphaPower = smoothstep(0.55, 1.0, _BreakAmount);

    // ==============================
    // 中心の黒い崩れ跡
    // ==============================

    float darkCore = 1.0 - smoothstep(
        currentHoleRadius * 0.35,
        currentHoleRadius + 0.35,
        dist
    );

    darkCore *= breakArea;
    darkCore *= _EdgeDarkness;
    darkCore *= _BreakAmount;

    // ==============================
    // ヒビ
    // ==============================

    float crackId = floor(angle01 * _CrackCount);
    float crackRandom = Random01(crackId);

    // ヒビごとに長さを変える
    float crackLength = _CrackLength * lerp(0.45, 1.0, crackRandom);

    // 放射状の基本線
    float crackWave = cos(angle * _CrackCount + crackRandom * 0.7);
    crackWave = saturate(crackWave);

    // powで細いヒビにする
    float crackLine = pow(crackWave, _CrackSharpness);

    // 中心から外へ伸びる範囲だけヒビを表示
    float crackStart = currentHoleRadius * 0.55;
    float crackEnd = crackLength * lerp(0.35, 1.0, _BreakAmount);

    float crackFromCenter = smoothstep(
        crackStart,
        crackStart + _HoleSoftness,
        dist
    );

    float crackEndMask = 1.0 - smoothstep(
        crackEnd,
        crackEnd + _HoleSoftness,
        dist
    );

    float crackMask = crackLine * crackFromCenter * crackEndMask;
    crackMask *= breakArea;
    crackMask *= _CrackStrength;
    crackMask *= _BreakAmount;

    // ==============================
    // 枝分かれヒビ
    // ==============================

    // 少し角度をずらした細いヒビを追加する
    float branchWave = cos((angle + 0.45) * (_CrackCount * 0.55));
    branchWave = saturate(branchWave);
    float branchLine = pow(branchWave, _CrackSharpness * 1.25);

    float branchMask = branchLine;
    branchMask *= crackEndMask;
    branchMask *= breakArea;
    branchMask *= _CrackStrength * 0.45;
    branchMask *= smoothstep(0.25, 1.0, _BreakAmount);

    // ==============================
    // 色の合成
    // ==============================

    // 中心を黒く崩す
    baseColor.rgb = lerp(
        baseColor.rgb,
        baseColor.rgb * 0.12,
        darkCore
    );

    // ヒビを黒く描く
    baseColor.rgb = lerp(
        baseColor.rgb,
        float3(0.03, 0.025, 0.02),
        crackMask
    );

    // 枝分かれヒビは少し薄く描く
    baseColor.rgb = lerp(
        baseColor.rgb,
        float3(0.08, 0.075, 0.07),
        branchMask
    );

    // ==============================
    // 透明度
    // ==============================

    // 最後の段階だけ中心を透明に近づける
    // 途中は「穴」より「黒い崩れ跡」に見せる
    baseColor.a *= holeMask;

    return baseColor;
}

#endif
