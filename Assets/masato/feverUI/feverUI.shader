Shader "UI/FeverScreenEffect"
{
    // ============================================================
    // MaterialのInspectorに表示する設定項目
    // ============================================================
    Properties
    {
        // UI Imageが使用するSprite Texture。
        // PerRendererDataを付けることで、Image側のSpriteが自動的に渡される。
        [PerRendererData]
        _MainTex ("Sprite Texture", 2D) = "white" {}

        // 画面の一番外側に表示する色。
        // 赤寄りのオレンジを初期値にしている。
        _Color ("Outer Color", Color) = (1, 0.2, 0, 1)

        // 枠の内側に表示する明るい発光色。
        // 黄色に近い色を初期値にしている。
        _GlowColor ("Glow Color", Color) = (1, 1, 0.2, 1)

        // 画面端から、どこまで枠を表示するか。
        // 値を大きくすると黄色い範囲が太くなる。
        _Width ("Edge Width", Range(0.01, 0.5)) = 0.12

        // 枠と透明部分の境界をぼかす幅。
        // 値を大きくすると、中央方向へ柔らかく消える。
        _Softness ("Softness", Range(0.001, 0.3)) = 0.08

        // エフェクト全体の表示強度。
        // 0で非表示、1で通常表示、2で強く表示する。
        _Intensity ("Intensity", Range(0, 2)) = 1

        // 発光が明滅する速度。
        _PulseSpeed ("Pulse Speed", Range(0, 10)) = 3

        // 集中線の本数
        _LineCount ("Line Count", Range(8, 100)) = 48
        
        // 集中線の細さ
        _LineSharpness ("Line Sharpness", Range(1, 30)) = 12
        
        // 集中線が中央方向へ伸びる長さ
        _LineLength ("Line Length", Range(0, 1)) = 0.35
        
        // 点滅する速度
        _BlinkSpeed ("Blink Speed", Range(0, 20)) = 6
    }

    // ============================================================
    // 実際の描画設定
    // ============================================================
    SubShader
    {
        Tags
        {
            // 透明オブジェクトとして描画する。
            "Queue" = "Transparent"

            // このShaderが透明描画用であることをUnityへ伝える。
            "RenderType" = "Transparent"

            // Projectorの影響を受けないようにする。
            "IgnoreProjector" = "True"

            // UnityのSprite Atlasを使用可能にする。
            "CanUseSpriteAtlas" = "True"

            // Materialのプレビューを平面として表示する。
            "PreviewType" = "Plane"
        }

        // Shaderの色と背景色をアルファ値で合成する。
        // 一般的な透明UI用のブレンド設定。
        Blend SrcAlpha OneMinusSrcAlpha

        // 表面と裏面の両方を描画する。
        Cull Off

        // UIなので深度バッファへ書き込まない。
        ZWrite Off

        // 常に描画する。
        ZTest Always

        // ========================================================
        // 1回分の描画処理
        // ========================================================
        Pass
        {
            HLSLPROGRAM

            // 頂点シェーダーとして使用する関数名。
            // FeverScreenEffect.hlsl内に同名の関数が必要。
            #pragma vertex FeverVertex

            // フラグメントシェーダーとして使用する関数名。
            // FeverScreenEffect.hlsl内に同名の関数が必要。
            #pragma fragment FeverFragment

            // UnityObjectToClipPosや_Timeなど、
            // Unity標準の機能を使用するために読み込む。
            #include "UnityCG.cginc"

            // 実際の頂点処理と色計算を別ファイルから読み込む。
            #include "Assets/masato/feverUI/feverUI.hlsl"

            ENDHLSL
        }
    }
}
