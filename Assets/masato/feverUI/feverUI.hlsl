#ifndef FEVER_SCREEN_EFFECT_INCLUDED
#define FEVER_SCREEN_EFFECT_INCLUDED

// ================================================================
// 頂点シェーダーへ渡される入力情報
// ================================================================
struct AppData
{
    // UI Imageを構成している頂点の位置。
    float4 vertex : POSITION;

    // テクスチャ上の座標。
    // 左下が(0, 0)、右上が(1, 1)。
    float2 uv : TEXCOORD0;

    // ImageコンポーネントのColor。
    // Image側で設定した透明度も、この値に含まれる。
    float4 color : COLOR;
};


// ================================================================
// 頂点シェーダーからフラグメントシェーダーへ渡す情報
// ================================================================
struct VertexToFragment
{
    // 最終的に画面へ描画する頂点位置。
    float4 vertex : SV_POSITION;

    // フラグメントシェーダーで使用するUV座標。
    float2 uv : TEXCOORD0;

    // Imageコンポーネントから受け取った色。
    float4 color : COLOR;
};


// ================================================================
// MaterialとUnity UIから渡される変数
// ================================================================

// ImageのSource Imageに設定されたSprite Texture。
// Source Imageが空の場合は、基本的に白テクスチャが使用される。
sampler2D _MainTex;

// 画面の一番外側に表示する色。
float4 _Color;

// 枠の内側に表示する発光色。
float4 _GlowColor;

// 枠を表示する太さ。
float _Width;

// 枠と透明部分の境界をぼかす幅。
float _Softness;

// エフェクト全体の表示強度。
float _Intensity;

// 明滅する速度。
float _PulseSpeed;

// 集中線の本数
float _LineCount;
// 集中線の細さ
float _LineSharpness;
// 集中線が中央方向へ伸びる長さ
float _LineLength;
// 点滅する速度
float _BlinkSpeed;

// ================================================================
// 頂点シェーダー
//
// UI Imageの頂点位置を画面描画用の座標へ変換する。
// 色の計算はほとんど行わず、必要な情報を次の処理へ渡す。
// ================================================================
VertexToFragment FeverVertex(AppData input)
{
    // フラグメントシェーダーへ渡す情報を作る。
    VertexToFragment output;

    // UI Imageの頂点位置を、
    // Unityのオブジェクト座標からクリップ座標へ変換する。
    output.vertex = UnityObjectToClipPos(input.vertex);

    // ImageのUV座標をそのまま渡す。
    output.uv = input.uv;

    // ImageコンポーネントのColorをそのまま渡す。
    output.color = input.color;

    return output;
}


// ================================================================
// フラグメントシェーダー
//
// 画面上の各ピクセルについて、最終的な色と透明度を計算する。
// ================================================================
float4 FeverFragment(VertexToFragment input) : SV_Target
{
    // ------------------------------------------------------------
    // 1. UV座標を取得
    // ------------------------------------------------------------

    float2 uv = input.uv;


    // ------------------------------------------------------------
    // 2. 上下左右の画面端までの距離を計算
    // ------------------------------------------------------------

    float distanceLeft = uv.x;
    float distanceRight = 1.0 - uv.x;
    float distanceBottom = uv.y;
    float distanceTop = 1.0 - uv.y;

    // 現在のピクセルから最も近い画面端までの距離。
    // 画面端では0、中央へ進むほど大きくなる。
    float edgeDistance = min(
        min(distanceLeft, distanceRight),
        min(distanceBottom, distanceTop)
    );


    // ------------------------------------------------------------
    // 3. 黄色い画面枠を作る
    // ------------------------------------------------------------

    // 画面端を1、中央を0にする。
    float edgeMask = 1.0 - smoothstep(
        _Width,
        _Width + _Softness,
        edgeDistance
    );


    // ------------------------------------------------------------
    // 4. 画面中央を原点にした座標を作る
    // ------------------------------------------------------------

    // UVの中心を(0, 0)にする。
    //
    // 左下は約(-0.5, -0.5)
    // 右上は約( 0.5,  0.5)
    float2 centeredUV = uv - 0.5;

    // Game画面の横長比率を補正する。
    // これを入れないと、集中線の角度が横方向に歪む。
    float aspect = _ScreenParams.x / _ScreenParams.y;
    centeredUV.x *= aspect;


    // ------------------------------------------------------------
    // 5. 中央から見た角度を計算
    // ------------------------------------------------------------

    // atan2によって、現在のピクセルが
    // 中央から見てどの方向にあるかを取得する。
    //
    // 結果は約 -3.14 から 3.14。
    float angle = atan2(
        centeredUV.y,
        centeredUV.x
    );


    // ------------------------------------------------------------
    // 6. 角度を使って漫画の集中線を作る
    // ------------------------------------------------------------

    // angleへLineCountを掛けてcosへ渡すことで、
    // 中央から放射状に並ぶ模様を作る。
    float lineWave = cos(
        angle * _LineCount
    );

    // cosの負の部分を消して、線になる部分だけを残す。
    lineWave = saturate(lineWave);

    // LineSharpnessで線を細くする。
    //
    // 小さい値：太い光の筋
    // 大きい値：細い漫画の集中線
    float lineMask = pow(
        lineWave,
        _LineSharpness
    );


    // ------------------------------------------------------------
    // 7. 集中線を画面端付近だけに表示する
    // ------------------------------------------------------------

    // 画面中央まで線が入りすぎないようにする。
    //
    // edgeDistanceが小さい画面端では1。
    // 中央へ進むにつれて0になる。
    float lineLengthMask = 1.0 - smoothstep(
        _LineLength,
        _LineLength + _Softness,
        edgeDistance
    );

    // 放射状模様と長さ制限を掛け合わせる。
    lineMask *= lineLengthMask;


    // ------------------------------------------------------------
    // 8. 点滅を作る
    // ------------------------------------------------------------

    // 0から1の間を往復する値を作る。
    float blinkWave =
        sin(_Time.y * _BlinkSpeed) * 0.5 + 0.5;

    // smoothstepで明るい時間と暗い時間をはっきり分ける。
    //
    // 通常のsinだけより、漫画的な点滅になる。
    float blink = smoothstep(
        0.3,
        0.75,
        blinkWave
    );


    // ------------------------------------------------------------
    // 9. 通常の発光も少し残す
    // ------------------------------------------------------------

    // 完全に消えると見づらいため、
    // 弱い発光を残しながら点滅させる。
    float pulse = lerp(
        0.45,
        1.0,
        blink
    );


    // ------------------------------------------------------------
    // 10. 色を計算
    // ------------------------------------------------------------

    // 画面端から枠の内側までを0から1へ変換する。
    float glowPosition = saturate(
        edgeDistance / max(_Width, 0.001)
    );

    // 外側をオレンジ、内側を黄色にする。
    float3 baseColor = lerp(
        _Color.rgb,
        _GlowColor.rgb,
        glowPosition
    );

    // 集中線は通常の枠より明るくする。
    float3 lineColor = _GlowColor.rgb * 1.8;

    // lineMaskを使って通常色と集中線の色を混ぜる。
    float3 finalColor = lerp(
        baseColor,
        lineColor,
        lineMask
    );


    // ------------------------------------------------------------
    // 11. 最終的な透明度を計算
    // ------------------------------------------------------------

    float textureAlpha = tex2D(
        _MainTex,
        uv
    ).a;

    // 枠と集中線を合成する。
    //
    // 集中線部分は強く点滅させる。
    float combinedMask = max(
        edgeMask * 0.6,
        lineMask
    );

    float alpha =
        combinedMask *
        pulse *
        _Intensity *
        input.color.a *
        textureAlpha;

    return float4(
        finalColor,
        saturate(alpha)
    );
}
#endif
