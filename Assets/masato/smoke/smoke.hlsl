#ifndef SMOKE_SIMPLE_PASS_INCLUDED
#define SMOKE_SIMPLE_PASS_INCLUDED

//==============================================================
// テクスチャ定義
//==============================================================
TEXTURE2D(_NoiseTex);
SAMPLER(sampler_NoiseTex);

//==============================================================
// Inspector から受け取る値
// UnityPerMaterial に入れることで
// Material ごとに値を変えられる
//==============================================================
CBUFFER_START(UnityPerMaterial)
float4 _SmokeColor;
float _Alpha;
float _ScrollX;
float _ScrollY;
float _NoiseTiling;
float _Cutoff;
float _Softness;
float _EdgeFade;
float _Brightness;
CBUFFER_END

//==============================================================
// 頂点入力
// Mesh からそのまま来るデータ
//==============================================================
struct Attributes
{
    float4 positionOS : POSITION;
    float3 normalOS : NORMAL;
    float2 uv : TEXCOORD0;
};

//==============================================================
// Vertex Shader から Pixel Shader に渡すデータ
//==============================================================
struct Varyings
{
    float4 positionCS : SV_POSITION;
    float2 uv : TEXCOORD0;
    float3 positionWS : TEXCOORD1;
    float3 normalWS : TEXCOORD2;
};

//==============================================================
// 頂点シェーダー
// 役割
// 1. 画面表示用の座標に変換する
// 2. UV を渡す
// 3. Pixel Shader で使う World 座標と法線を準備する
//==============================================================
Varyings vert(Attributes IN)
{
    Varyings OUT;

    // オブジェクト座標から各座標系を計算する
    VertexPositionInputs posInputs = GetVertexPositionInputs(IN.positionOS.xyz);

    // オブジェクト法線をワールド法線に変換する
    VertexNormalInputs normalInputs = GetVertexNormalInputs(IN.normalOS);

    // クリップ座標
    // これが最終的に画面に出すために必要
    OUT.positionCS = posInputs.positionCS;

    // ワールド座標
    // Pixel Shader でカメラ方向を出す時に使う
    OUT.positionWS = posInputs.positionWS;

    // ワールド法線
    OUT.normalWS = normalize(normalInputs.normalWS);

    // UV はそのまま渡す
    OUT.uv = IN.uv;

    return OUT;
}

//==============================================================
// ピクセルシェーダー
// ここで最終色と透明度を決める
//==============================================================
half4 frag(Varyings IN) : SV_Target
{
    //==========================================================
    // 1. 元の UV
    //==========================================================
    float2 uv = IN.uv;

    //==========================================================
    // 2. 時間で UV を動かす
    // _Time.y を使うと時間経過で流れる
    //==========================================================
    float2 scroll = float2(_ScrollX, _ScrollY) * _Time.y;

    //==========================================================
    // 3. ノイズを読むための UV を作る
    // Tiling でノイズの大きさを調整
    //==========================================================
    float2 noiseUV = uv * _NoiseTiling + scroll;

    //==========================================================
    // 4. ノイズテクスチャを読む
    // 今回は赤成分だけ使う
    // 白黒ノイズなら r だけで十分
    //==========================================================
    float noise = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, noiseUV).r;

    //==========================================================
    // 5. ノイズを煙の濃さに変換する
    // smoothstep を使うと境界がなめらかになる
    //==========================================================
    float density = smoothstep(_Cutoff, _Cutoff + _Softness, noise);

    //==========================================================
    // 6. Quad の端を薄くする
    // UV を -1 ～ 1 に変換して中心からの距離を取る
    //==========================================================
    float2 centerUV = uv * 2.0 - 1.0;
    float edgeMask = 1.0 - saturate(length(centerUV) * _EdgeFade);

    //==========================================================
    // 7. カメラ方向を計算する
    // positionWS を Vertex Shader から渡している理由がここ
    //==========================================================
    float3 normalWS = normalize(IN.normalWS);
    float3 viewDirWS = normalize(_WorldSpaceCameraPos - IN.positionWS);

    //==========================================================
    // 8. 面がカメラを向きすぎている時の見え方を少し調整
    //==========================================================
    float facing = saturate(dot(normalWS, viewDirWS));
    float angleFade = lerp(1.0, 0.7, facing);

    //==========================================================
    // 9. 最終アルファ
    //==========================================================
    float finalAlpha = density * edgeMask * angleFade * _Alpha;
    finalAlpha = saturate(finalAlpha);

    //==========================================================
    // 10. 最終色
    //==========================================================
    float3 finalColor = _SmokeColor.rgb * _Brightness;

    return half4(finalColor, finalAlpha);
}

#endif