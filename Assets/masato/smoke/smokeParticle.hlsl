#ifndef SMOKE_PARTICLE_INCLUDED
#define SMOKE_PARTICLE_INCLUDED

//==============================================================
// テクスチャ定義
//==============================================================
TEXTURE2D(_MainTex);
SAMPLER(sampler_MainTex);

TEXTURE2D(_NoiseTex1);
SAMPLER(sampler_NoiseTex1);

TEXTURE2D(_NoiseTex2);
SAMPLER(sampler_NoiseTex2);

//==============================================================
// Material から受け取る値
//==============================================================
CBUFFER_START(UnityPerMaterial)
float4 _SmokeColor;
float _Alpha;

float _NoiseScale1;
float _NoiseScale2;

float _ScrollX1;
float _ScrollY1;
float _ScrollX2;
float _ScrollY2;

float _Cutoff;
float _Softness;
float _TopFade;
float _EdgeFade;
CBUFFER_END

//==============================================================
// 頂点入力
//==============================================================
struct Attributes
{
    float4 positionOS : POSITION;
    float2 uv : TEXCOORD0;
    float4 color : COLOR;
};

//==============================================================
// Vertex -> Pixel に渡すデータ
//==============================================================
struct Varyings
{
    float4 positionCS : SV_POSITION;
    float2 uv : TEXCOORD0;
    float4 color : COLOR;
};

//==============================================================
// Vertex Shader
//==============================================================
Varyings vert(Attributes IN)
{
    Varyings OUT;

    // オブジェクト座標から画面表示用の座標へ変換
    VertexPositionInputs posInputs = GetVertexPositionInputs(IN.positionOS.xyz);

    // 画面に出す座標
    OUT.positionCS = posInputs.positionCS;

    // UV を Pixel Shader に渡す
    OUT.uv = IN.uv;

    // Particle System の色を Pixel Shader に渡す
    OUT.color = IN.color;

    return OUT;
}

//==============================================================
// Pixel Shader
//==============================================================
half4 frag(Varyings IN) : SV_Target
{
    // 元の UV
    float2 uv = IN.uv;

    // メインテクスチャの形を読む
    float mainMask = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv).r;

    // ノイズ用UVを作る
    float2 noiseUV1 = uv * _NoiseScale1 + float2(_ScrollX1, _ScrollY1) * _Time.y;
    float2 noiseUV2 = uv * _NoiseScale2 + float2(_ScrollX2, _ScrollY2) * _Time.y;

    // ノイズを2枚読む
    float noise1 = SAMPLE_TEXTURE2D(_NoiseTex1, sampler_NoiseTex1, noiseUV1).r;
    float noise2 = SAMPLE_TEXTURE2D(_NoiseTex2, sampler_NoiseTex2, noiseUV2).r;

    // ノイズを混ぜる
    float noise = (noise1 + noise2) * 0.5;

    // ノイズを煙の濃さに変換
    float density = smoothstep(_Cutoff, _Cutoff + _Softness, noise);

    // 上方向フェード
    float topMask = saturate(1.0 - uv.y * _TopFade);

    // 端フェード
    float2 centerUV = uv * 2.0 - 1.0;
    float edgeMask = 1.0 - saturate(length(centerUV) * _EdgeFade);

    // 最終Alpha
    float finalAlpha = mainMask * density * topMask * edgeMask * _Alpha;

    // Particle 側の Alpha を掛ける
    finalAlpha *= IN.color.a;

    // 0～1に収める
    finalAlpha = saturate(finalAlpha);

    // 最終色
    float3 finalColor = _SmokeColor.rgb * IN.color.rgb;

    return half4(finalColor, finalAlpha);
}

#endif