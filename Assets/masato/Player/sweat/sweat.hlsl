#ifndef SWEAT_INCLUDED
#define SWEAT_INCLUDED

// ================================
// Meshから受け取る情報
// ================================
struct Attributes
{
    float4 positionOS : POSITION;
    float2 uv : TEXCOORD0;
};

// ================================
// VertexからFragmentへ渡す情報
// ================================
struct Varyings
{
    float4 positionHCS : SV_POSITION;
    float2 uv : TEXCOORD0;

    // 汗の流れをObject座標で作るために渡す
    float3 positionOS : TEXCOORD1;
};

// ================================
// Texture宣言
// ================================
TEXTURE2D(_BaseMap);
SAMPLER(sampler_BaseMap);

TEXTURE2D(_DropMask);
SAMPLER(sampler_DropMask);

// ================================
// Propertiesの値をHLSL側で受け取る
// ================================
CBUFFER_START(UnityPerMaterial)
half4 _BaseColor;
float4 _BaseMap_ST;

float _UseDropTexture;
float _DropStrength;
float _DropSize;
float _DropEdgeSoftness;

float _HighlightStrength;
float _ShadowStrength;

float _DropDensity;
float _DropExistThreshold;
float _DropFlowSpeed;
float _ObjectFlowScale;

CBUFFER_END

// ================================
// 疑似ランダム
// 同じ入力なら同じ値が返る
// 重要
// ================================
float Random01(float2 p)
{
    return frac(sin(dot(p, float2(12.9898, 78.233))) * 43758.5453);
}

// ================================
// 今いるマス番号を取得する
// 雫ごとのランダム値を決める基準
// ================================
float2 GetCellID(float2 coord)
{
    float2 tiledCoord = coord * _DropDensity;
    return floor(tiledCoord);
}

// ================================
// 雫中心から見た現在位置を返す
//
// 座標：
//   汗用の座標
//   今回はUVではなくObject座標から作る
//
// 戻り値：
//   現在ピクセルが雫中心からどれだけ離れているか
// ================================
float2 GetDropLocal(float2 coord)
{
    float2 tiledCoord = coord * _DropDensity;

    // どのマスか
    float2 cellID = floor(tiledCoord);

    // マス内の0〜1座標
    float2 cellUV = frac(tiledCoord);

    // マスごとに雫の位置を少しズラす
    float randX = Random01(cellID);
    float randY = Random01(cellID + float2(13.37, 27.91));

    float2 randomOffset = float2(randX, randY) - 0.5;
    randomOffset *= 0.4;

     // 各マス内での雫中心
    float2 center = float2(0.5, 0.5) + randomOffset;

    return cellUV - center;
}

// ================================
// 雫ごとの大きさ倍率
// 小さい粒と大きい粒を混ぜる
// ================================
float GetDropSizeScale(float2 coord)
{
    float2 cellID = GetCellID(coord);
    float r = Random01(cellID + float2(41.7, 9.2));

    return lerp(0.5, 1.3, r);
}

// ================================
// 雫を出すかどうか
//
// _DropExistThreshold が高いほど
// 表示される雫が少なくなる
// ================================
float GetDropExist(float2 coord)
{
    float2 cellID = GetCellID(coord);
    float r = Random01(cellID + float2(91.1, 17.3));

    return step(_DropExistThreshold, r);
}

// ================================
// Textureなしの場合の雫マスク
//
// 1 = 雫の中
// 0 = 雫の外
// ================================
float ProceduralDropletMask(float2 coord)
{
    float2 local = GetDropLocal(coord);

    float sizeScale = GetDropSizeScale(coord);
    float dropSize = _DropSize * sizeScale;

    // 雫を縦長にする
    local.x *= 1.6;
    local.y *= 0.8;

     // 丸い本体
    float roundPart =
                    1.0 - smoothstep(dropSize, dropSize + _DropEdgeSoftness, length(local));

    // 下に垂れる部分
    float2 tailPos = float2(0.0, -dropSize * 1.2);
    float tailDist = distance(local, tailPos);

    float tailPart =
                    1.0 - smoothstep(dropSize * 0.4, dropSize * 0.8, tailDist);

    // このマスに雫を出すか
    float exist = GetDropExist(coord);

    return saturate(max(roundPart, tailPart)) * exist;
}

// ================================
// Texture雫 / Procedural雫 の切り替え
// ================================
float GetDropMask(float2 coord, float textureMask, float useTexture)
{
    float proceduralMask = ProceduralDropletMask(coord);
    return lerp(proceduralMask, textureMask, useTexture);
}

// ================================
// 頂点シェーダー
// ================================
Varyings vert(Attributes IN)
{
    Varyings OUT;

    OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
    OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);

    // Fragment側でObject座標を使う
    OUT.positionOS = IN.positionOS.xyz;

    return OUT;
}

// ================================
// フラグメントシェーダー
// ================================
half4 frag(Varyings IN) : SV_Target
{
    // 肌・服の元Texture
    // BaseMapはUVで読む
    half4 color =
      SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv) * _BaseColor;

// ================================
// 汗用座標
//
// ここが重要：
// 汗の流れはUVではなくObject座標で作る
// これにより、UVの上下反転に影響されにくくする
// ================================
    float2 sweatCoord = IN.positionOS.xy * _ObjectFlowScale;

   // Object座標のY方向へ流す
   // 逆方向に見える場合は -= を += に変える
    sweatCoord.y += _Time.y * _DropFlowSpeed;

    // Textureマスク使用時用
    // Procedural時は主に使われない
    float texMask =
     SAMPLE_TEXTURE2D(_DropMask, sampler_DropMask, IN.uv).r;

     // 雫本体マスク
    float dropMask = GetDropMask(sweatCoord, texMask, _UseDropTexture);

    // 雫の縁だけを取り出す
    float dropEdge =
     smoothstep(0.15, 0.6, dropMask) *
      (1.0 - smoothstep(0.6, 1.0, dropMask));

   // 雫中心から見た位置
    float2 dropLocal = GetDropLocal(sweatCoord);

    // 粒ごとのサイズ倍率
    float sizeScale = GetDropSizeScale(sweatCoord);

   // ================================
   // ハイライト
   // 雫の左上に細長い光を作る
   // ================================
    float2 highlightLocal = dropLocal - float2(-0.015, 0.02);
    highlightLocal.x *= 2.5;
    highlightLocal.y *= 0.7;

    float highlightDist = length(highlightLocal);

    float highlight =
      1.0 - smoothstep(0.005 * sizeScale, 0.03 * sizeScale, highlightDist);

    // ================================
    // 影
    // 雫の右下に少し暗い部分を作る
    // ================================
    float2 shadowPos = float2(0.015, -0.025);
    float shadowDist = distance(dropLocal, shadowPos);

    float shadow =
                    1.0 - smoothstep(0.02 * sizeScale, 0.06 * sizeScale, shadowDist);

    // ================================
    // 最終合成
    // ================================
    color.rgb += highlight * dropMask * _HighlightStrength;
    color.rgb -= dropEdge * 0.15;
    color.rgb -= shadow * dropMask * _ShadowStrength;
    color.rgb += dropMask * _DropStrength;

    return color;
}

#endif
