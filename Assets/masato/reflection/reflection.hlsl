#ifndef PLANAR_REFLECTION_INCLUDED
#define PLANAR_REFLECTION_INCLUDED

// ============================================================
// Material から受け取る値
// ============================================================
CBUFFER_START(UnityPerMaterial)

    // 床そのものの色
    // 今回は鏡に寄せたいので、かなり暗くする前提
    // 例: (0, 0, 0, 1) か (0.02, 0.02, 0.02, 1)
float4 _BaseColor;

    // BaseMap の Tiling / Offset
float4 _BaseMap_ST;

    // 反射の基本強度
    // 鏡に近づけたいなら 1.0 推奨
float _ReflectStrength;

    // Fresnel の強さ
    // 小さいと正面から見ても反射しやすい
    // 鏡なら 1.0 - 1.5 くらいが扱いやすい
float _FresnelPower;

    //粗さの強さ
    // 0.0 で完全な鏡、1.0 で完全な拡散反射
float _Roughness;

CBUFFER_END

// ============================================================
// 床の通常テクスチャ
// ============================================================
TEXTURE2D(_BaseMap);
SAMPLER(sampler_BaseMap);

// ============================================================
// C# 側から渡される平面反射テクスチャ
// ============================================================
TEXTURE2D(_ReflectionTex);
SAMPLER(sampler_ReflectionTex);

// ============================================================
// C# 側から渡される反射用 ViewProjection 行列
// ワールド座標を反射カメラ基準の座標へ変換する
// ============================================================
float4x4 _ReflectionVP;

// ============================================================
// 頂点入力
// ============================================================
struct Attributes
{
    // オブジェクト空間の頂点位置
    float4 positionOS : POSITION;

    // オブジェクト空間の法線
    float3 normalOS : NORMAL;

    // UV
    float2 uv : TEXCOORD0;
};

// ============================================================
// Vertex -> Fragment に渡す値
// ============================================================
struct Varyings
{
    // 通常描画用のクリップ座標
    float4 positionCS : SV_POSITION;

    // ワールド座標
    float3 positionWS : TEXCOORD0;

    // ワールド法線
    float3 normalWS : TEXCOORD1;

    // BaseMap 用 UV
    float2 uv : TEXCOORD2;

    // 反射テクスチャ参照用の座標
    float4 reflectionPos : TEXCOORD3;
};

// ============================================================
// 頂点シェーダ
// ============================================================
Varyings vert(Attributes IN)
{
    Varyings OUT;

    // 頂点座標を各空間へ変換
    VertexPositionInputs posInputs = GetVertexPositionInputs(IN.positionOS.xyz);

    // 法線をワールド空間へ変換
    VertexNormalInputs normalInputs = GetVertexNormalInputs(IN.normalOS);

    // 通常描画用座標
    OUT.positionCS = posInputs.positionCS;

    // ワールド座標
    OUT.positionWS = posInputs.positionWS;

    // ワールド法線
    OUT.normalWS = normalize(normalInputs.normalWS);

    // ベーステクスチャ用 UV
    OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);

    // 反射カメラの ViewProjection で変換
    // frag 側でこの値から reflectionUV を計算する
    OUT.reflectionPos = mul(_ReflectionVP, float4(OUT.positionWS, 1.0));

    return OUT;
}

// ============================================================
// ピクセルシェーダ
// ============================================================
half4 frag(Varyings IN) : SV_Target
{
    // --------------------------------------------------------
    // 1. 床のベース色を作る
    // --------------------------------------------------------
    // ただし今回は鏡寄りなので、最終的にはほぼ使わない
    half3 baseTex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv).rgb;
    half3 baseColor = baseTex * _BaseColor.rgb;

    // --------------------------------------------------------
    // 2. 反射テクスチャの UV を作る
    // --------------------------------------------------------
    // clip space -> NDC
    float2 reflectionUV = IN.reflectionPos.xy / max(IN.reflectionPos.w, 0.0001);

    // NDC (-1..1) -> UV (0..1)
    reflectionUV = reflectionUV * 0.5 + 0.5;

    // プラットフォーム差による上下反転対応
#if UNITY_UV_STARTS_AT_TOP
    reflectionUV.y = 1.0 - reflectionUV.y;
#endif

    // --------------------------------------------------------
    // 3. UV が有効範囲内かチェック
    // --------------------------------------------------------
    half insideX = step(0.0, reflectionUV.x) * step(reflectionUV.x, 1.0);
    half insideY = step(0.0, reflectionUV.y) * step(reflectionUV.y, 1.0);
    half insideW = step(0.0001, IN.reflectionPos.w);

    // 有効なときだけ反射を見せる
    half reflectionVisible = insideX * insideY * insideW;

    // --------------------------------------------------------
    // 4. 反射テクスチャを読む
    // --------------------------------------------------------
    float blurSize = _Roughness * 0.02;

    half3 reflectionColor = 0.0;

    reflectionColor += SAMPLE_TEXTURE2D(_ReflectionTex, sampler_ReflectionTex, reflectionUV).rgb;
    reflectionColor += SAMPLE_TEXTURE2D(_ReflectionTex, sampler_ReflectionTex, reflectionUV + float2(blurSize, 0.0)).rgb;
    reflectionColor += SAMPLE_TEXTURE2D(_ReflectionTex, sampler_ReflectionTex, reflectionUV + float2(-blurSize, 0.0)).rgb;
    reflectionColor += SAMPLE_TEXTURE2D(_ReflectionTex, sampler_ReflectionTex, reflectionUV + float2(0.0, blurSize)).rgb;
    reflectionColor += SAMPLE_TEXTURE2D(_ReflectionTex, sampler_ReflectionTex, reflectionUV + float2(0.0, -blurSize)).rgb;

    reflectionColor /= 5.0;

    // --------------------------------------------------------
    // 5. Fresnel を計算
    // --------------------------------------------------------
    // 鏡でも角度によって少し反射の見え方が変わるので入れる
    // ただし今回は正面からもかなり強く反射させたい
    float3 normalWS = normalize(IN.normalWS);
    float3 viewDirWS = SafeNormalize(_WorldSpaceCameraPos.xyz - IN.positionWS);

    // 法線と視線方向の一致度
    // 1 に近い: 正面
    // 0 に近い: 斜め
    half NoV = saturate(dot(normalWS, viewDirWS));

    // 斜めで強くなる反射
    half fresnel = pow(1.0h - NoV, _FresnelPower);

    // --------------------------------------------------------
    // 6. 最終的な反射量
    // --------------------------------------------------------
    // _ReflectStrength をベースにしつつ、
    // 斜めから見たときは 1.0 に寄せる
    //
    // 鏡感を強くしたいので、
    // 反射が見える領域ではほぼ反射が主役になるようにする
    half reflectAmount =
        saturate(lerp(_ReflectStrength, 1.0h, fresnel)) * reflectionVisible;

    // --------------------------------------------------------
    // 7. ベース色は完全に消す
    // --------------------------------------------------------
    // 今回は「鏡」寄りにしたいので床色を残さない
    // 水面っぽさを少し出したい場合だけ 0.02 などにする
    half baseRemain = 0.0h;

    // --------------------------------------------------------
    // 8. ベース色と反射色を合成
    // --------------------------------------------------------
    half3 baseContribution = baseColor * baseRemain;
    half3 reflectionContribution = reflectionColor * reflectAmount;

    half3 finalColor = baseContribution + reflectionContribution;

    // 念のためクランプ
    finalColor = saturate(finalColor);

    // 不透明出力
    return half4(finalColor, 1.0h);
}

#endif
