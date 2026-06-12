#ifndef PLAYER_MUSCLE_INCLUDED
#define PLAYER_MUSCLE_INCLUDED

// ------------------------------------------------------------
// URP の基本関数
// 座標変換やカメラ関連の関数を使うために必要
// ------------------------------------------------------------
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

// ------------------------------------------------------------
// URP のライト計算用関数
// メインライトや影の情報を使うために必要
// ------------------------------------------------------------
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"


// ------------------------------------------------------------
// マテリアルから GPU へ送られる値
// ------------------------------------------------------------
CBUFFER_START(UnityPerMaterial)

    // ベースカラー
    float4 _BaseColor;

    // ベーステクスチャの Tiling と Offset
    float4 _BaseMap_ST;

    // ノーマルマップの強さ
    float _NormalStrength;

    // 暗さ
    float _Darkness;

    // テカリの色
    float4 _SpecColor;

    // テカリの強さ
    float _SpecStrength;

    // テカリの鋭さ
    float _SpecPower;

    // 環境光の強さ
    float _AmbientStrength;

    // 数字表示の ON / OFF
float _ShowNumber;

    // 数字の中身
    float _Digit0;
    float _Digit1;
    float _Digit2;

    // 数字の色
    float4 _DigitColor;

    // 数字の表示範囲
    float4 _DigitArea;

    // 数字のスケール
    float4 _DigitScale;

    //汗用の変数
float _DropSize;
float _DropEdgeSoftness;
float _HighlightStrength;
float _DropStrength;
float _DropDensity;
float _DropExistThreshold;
float _DropFlowSpeed;
float _ObjectFlowScale;

CBUFFER_END


// ------------------------------------------------------------
// ベーステクスチャ
// ------------------------------------------------------------
TEXTURE2D(_BaseMap);
SAMPLER(sampler_BaseMap);

// ------------------------------------------------------------
// ノーマルマップ
// ------------------------------------------------------------
TEXTURE2D(_NormalMap);
SAMPLER(sampler_NormalMap);

// ------------------------------------------------------------
// 数字処理を読み込む
// ------------------------------------------------------------
#include "number.hlsl"


// ------------------------------------------------------------
// 頂点シェーダへ入るデータ
// ------------------------------------------------------------
struct Attributes
{
    float3 positionOS : POSITION;
    float3 normalOS : NORMAL;
    float4 tangentOS : TANGENT;
    float2 uv : TEXCOORD0;
};


// ------------------------------------------------------------
// 頂点シェーダからピクセルシェーダへ渡すデータ
// ------------------------------------------------------------
struct Varyings
{
    float4 positionCS : SV_POSITION;
    float3 positionWS : TEXCOORD0;
    float3 normalWS : TEXCOORD1;
    float3 tangentWS : TEXCOORD2;
    float3 bitangentWS : TEXCOORD3;
    float2 uv : TEXCOORD4;
    float4 shadowCoord : TEXCOORD5;
};


// ------------------------------------------------------------
// 頂点シェーダ
// ------------------------------------------------------------
Varyings vert(Attributes IN)
{
    Varyings OUT;

    VertexPositionInputs posInputs =
        GetVertexPositionInputs(IN.positionOS);

    VertexNormalInputs normalInputs =
        GetVertexNormalInputs(IN.normalOS, IN.tangentOS);

    OUT.positionCS =
        posInputs.positionCS;

    OUT.positionWS =
        posInputs.positionWS;

    OUT.normalWS =
        normalInputs.normalWS;

    OUT.tangentWS =
        normalInputs.tangentWS;

    OUT.bitangentWS =
        normalInputs.bitangentWS;

    OUT.uv =
        TRANSFORM_TEX(IN.uv, _BaseMap);

    OUT.shadowCoord =
        TransformWorldToShadowCoord(OUT.positionWS);

    return OUT;
}


// ------------------------------------------------------------
// ノーマルマップからワールド法線を作る
// ------------------------------------------------------------
half3 GetNormalFromNormalMap(Varyings IN)
{
    half4 normalSample =
        SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, IN.uv);

    half3 normalTS =
        UnpackNormal(normalSample);

    normalTS.xy *=
        (half)_NormalStrength;

    normalTS.z =
        sqrt(saturate(1.0h - dot(normalTS.xy, normalTS.xy)));

    half3 T =
        normalize(IN.tangentWS);

    half3 B =
        normalize(IN.bitangentWS);

    half3 N =
        normalize(IN.normalWS);

    half3 normalWS =
        normalize(
            normalTS.x * T +
            normalTS.y * B +
            normalTS.z * N
        );

    return normalWS;
}


// ------------------------------------------------------------
// ピクセルシェーダ
// ------------------------------------------------------------
half4 frag(Varyings IN) : SV_Target
{
    // ベーステクスチャの色
    half3 texColor =
        SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv).rgb;

    // 体の基本色
    half3 albedo =
        texColor * (half3)_BaseColor.rgb;

    // 暗さを反映
    half darkFactor =
        lerp(1.0h, 0.25h, (half)_Darkness);

    albedo *=
        darkFactor;

    // ノーマルマップ込みの法線
    half3 N =
        GetNormalFromNormalMap(IN);

    // メインライト
    Light mainLight =
        GetMainLight(IN.shadowCoord);

    // ライト方向
    half3 L =
        normalize(mainLight.direction);

    // カメラ方向
    half3 V =
        normalize(_WorldSpaceCameraPos.xyz - IN.positionWS);

    // 拡散反射
    half NdotL =
        saturate(dot(N, L));

    half3 diffuse =
        albedo *
        mainLight.color *
        NdotL *
        mainLight.shadowAttenuation;

    // テカリ
    half3 H =
        normalize(L + V);

    half NdotH =
        saturate(dot(N, H));

    half specFactor =
        pow(NdotH, _SpecPower);

    half3 specular =
        (half3)_SpecColor.rgb *
        specFactor *
        (half)_SpecStrength *
        mainLight.color *
        mainLight.shadowAttenuation;

    // 環境光
    half3 ambient =
        albedo * (half)_AmbientStrength;

    // 体の最終色
    half3 finalColor =
        diffuse + specular + ambient;

    // 数字を重ねる
    finalColor =
        ApplyNumber(finalColor, IN.uv);

    return half4(finalColor, _BaseColor.a);
}

#endif
