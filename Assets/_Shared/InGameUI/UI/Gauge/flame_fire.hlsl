#ifndef FLAME_FIRE_INCLUDED
#define FLAME_FIRE_INCLUDED

// リング本体の揺れに使うノイズ
sampler2D _NoiseTex;
float4 _NoiseTex_ST;

// 先端炎の形に使うテクスチャ
sampler2D _FlameTex;
float4 _FlameTex_ST;

// リング本体の流れる速さ
float _FlameSpeed;

// リング本体の炎模様の強さ
float _FlameStrength;

// 先端炎の向き補正角度
float _FlameAngleOffset;

// 先端炎の長さ
float _TipLength;

// 先端炎の横幅
float _TipWidth;

// 先端炎の強さ
float _TipStrength;

// 外側境界だけのぼかし幅
float _OuterEdgeSoftness;

// 2Dベクトルを角度で回転
float2 Rotate2D(float2 v, float rad)
{
    float s = sin(rad);
    float c = cos(rad);
    return float2(v.x * c - v.y * s, v.x * s + v.y * c);
}

// 炎色とアルファを返す
float4 GetFlameColor(
    float2 uv,
    float4 baseColor,
    float angle01,
    float dist,
    float innerRadius,
    float outerRadius,
    float fillAmount,
    float startAngle)
{
    float t = _Time.y;
    float2 center = float2(0.5, 0.5);

    // ------------------------------------------------------------
    // 1. リング本体を作る
    // ------------------------------------------------------------

    // リング本体の揺れ
    float2 bodyUV1 = uv * 2.6 + float2(0.0, -t * _FlameSpeed);
    float2 bodyUV2 = uv * 5.0 + float2(t * 0.18, -t * (_FlameSpeed * 1.7));

    float bodyNoise1 = tex2D(_NoiseTex, TRANSFORM_TEX(bodyUV1, _NoiseTex)).r;
    float bodyNoise2 = tex2D(_NoiseTex, TRANSFORM_TEX(bodyUV2, _NoiseTex)).r;
    float bodyNoise = lerp(bodyNoise1, bodyNoise2, 0.5);

    // リング境界を少しだけ崩す
    float bodyDistort = (bodyNoise - 0.5) * 0.018 * _FlameStrength;

    float innerEdge = innerRadius + bodyDistort * 0.25;
    float outerEdge = outerRadius + bodyDistort * 0.65;

    // 外側だけ Inspector の値でぼかす
    float outerMask = 1.0 - smoothstep(outerEdge - _OuterEdgeSoftness, outerEdge + _OuterEdgeSoftness, dist);

    // 内側は今まで通り固定値のまま
    float innerMask = smoothstep(innerEdge - 0.010, innerEdge + 0.010, dist);

    float ringMask = saturate(innerMask * outerMask);

    // リング内での位置
    float ringLerp = saturate((dist - innerRadius) / max(outerRadius - innerRadius, 0.0001));

    // 内側を少し熱くする
    float radialHeat = 1.0 - ringLerp;

    // 基本の熱量
    float heat = saturate(bodyNoise * _FlameStrength + radialHeat * 0.70);
    heat = pow(heat, 1.35);

    // リング本体の炎色
    float3 darkColor = float3(0.10, 0.01, 0.00);
    float3 midColor = float3(0.80, 0.16, 0.01);
    float3 hotColor = float3(1.00, 0.72, 0.12);

    float3 flameColor = lerp(darkColor, midColor, heat);
    flameColor = lerp(flameColor, hotColor, pow(saturate(heat), 2.3));

    // ------------------------------------------------------------
    // 2. 先端位置を求める
    // ------------------------------------------------------------

    // 開始角度を 0～1 に変換
    float start01 = startAngle / 360.0;

    // FillAmount の位置に対応する実角度を復元
    // 0 = 12時, 時計回りに進む
    float tipAngle01 = frac(0.25 - start01 - fillAmount);
    float tipAngle = tipAngle01 * (2.0 * 3.14159265);

    // 先端の外周位置
    float2 tipDir = float2(cos(tipAngle), sin(tipAngle));
    float2 tipPoint = center + tipDir * outerRadius;

    // 外向きベクトル
    float2 outward = normalize(tipDir);

    // 外向きを基準に、Inspector で炎方向を回せるようにする
    float flameAngleRad = radians(_FlameAngleOffset);
    float2 flameDir = normalize(Rotate2D(outward, flameAngleRad));

    // 炎の横方向
    float2 flameSide = float2(-flameDir.y, flameDir.x);

    // ------------------------------------------------------------
    // 3. 先端専用の局所座標を作る
    // ------------------------------------------------------------

    // 先端を原点にした相対位置
    float2 rel = uv - tipPoint;

    // flameDir 方向が「炎の伸びる向き」
    float localY = dot(rel, flameDir);

    // flameSide 方向が「炎の横幅」
    float localX = dot(rel, flameSide);

    // 炎のローカル UV
    // localY=0 が炎の根元、+側へ伸びる
    float flameU = 0.5 + localX / max(_TipWidth, 0.0001);
    float flameV = 1.0 - (localY / max(_TipLength, 0.0001));

    // ------------------------------------------------------------
    // 4. 先端炎テクスチャを使って炎の形を作る
    // ------------------------------------------------------------

    // 炎は少し流す
    float2 flameUV = float2(flameU, flameV + t * (_FlameSpeed * 0.45));

    // 炎テクスチャを取得
    float4 flameTex = tex2D(_FlameTex, TRANSFORM_TEX(flameUV, _FlameTex));

    // 写真系でも白黒系でも使いやすいように輝度を使う
    float flameLuma = dot(flameTex.rgb, float3(0.299, 0.587, 0.114));

    // テクスチャの alpha か輝度の大きい方を採用
    float flameShape = max(flameTex.a, flameLuma);

    // 炎の外側で勝手に出ないように、ローカル範囲でもマスク
    float widthMask = saturate(1.0 - abs(localX) / max(_TipWidth, 0.0001));
    float lengthMask = saturate(1.0 - saturate(localY / max(_TipLength, 0.0001)));
    float baseTipMask = saturate(step(0.0, localY) * widthMask * lengthMask);

    // 先端炎の最終形
    float tipMask = flameShape * baseTipMask * _TipStrength;

    // 先端の炎がリング外へ少しはみ出せるようにする
    float finalMask = max(ringMask, tipMask);

    // ------------------------------------------------------------
    // 5. 先端炎の色を作る
    // ------------------------------------------------------------

    // 炎テクスチャの根元は赤橙、明るいところは黄白
    float3 tipOuterColor = float3(1.00, 0.28, 0.02);
    float3 tipInnerColor = float3(1.00, 0.95, 0.72);

    // 炎の芯
    float tipCore = pow(saturate(flameShape), 2.5) * baseTipMask;

    // 先端炎を加算
    flameColor += tipOuterColor * tipMask;
    flameColor += tipInnerColor * tipCore * (_TipStrength * 0.85);

    // リングと先端のつなぎ目を少し明るくする
    float joinMask = saturate(1.0 - abs(dist - outerRadius) / 0.04) * baseTipMask;
    flameColor += hotColor * joinMask * 0.35 * _TipStrength;

    // ------------------------------------------------------------
    // 6. 最終色
    // ------------------------------------------------------------

    float alpha = baseColor.a * saturate(finalMask);

    // 暗部を少し残して、ベタッとしすぎないようにする
    flameColor *= lerp(0.78, 1.12, heat);

    return float4(flameColor * baseColor.rgb, alpha);
}

#endif
