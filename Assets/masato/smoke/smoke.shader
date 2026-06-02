Shader "Custom/smoke"
{
     Properties
    {
        // 煙の基本色
        _SmokeColor ("Smoke Color", Color) = (0.8, 0.8, 0.8, 1.0)

        // 煙全体の濃さ
        _Alpha ("Alpha", Range(0.0, 1.0)) = 0.6

        // 白黒ノイズ画像
        _NoiseTex ("Noise Texture", 2D) = "white" {}

        // ノイズを横に流す速さ
        _ScrollX ("Scroll X", Range(-2.0, 2.0)) = 0.05

        // ノイズを縦に流す速さ
        _ScrollY ("Scroll Y", Range(-2.0, 2.0)) = 0.25

        // ノイズの細かさ
        _NoiseTiling ("Noise Tiling", Range(0.1, 10.0)) = 2.5

        // ノイズをどこから煙として見せるか
        _Cutoff ("Density Cutoff", Range(0.0, 1.0)) = 0.25

        // 輪郭の柔らかさ
        _Softness ("Softness", Range(0.001, 1.0)) = 0.2

        // Quad の端をどれくらい薄くするか
        _EdgeFade ("Edge Fade", Range(0.0, 5.0)) = 1.6

        // 明るさ補正
        _Brightness ("Brightness", Range(0.0, 3.0)) = 1.0
    }

    //========================================================
    // SubShader
    // - 実際の描画設定を書く場所
    //========================================================
    SubShader
    {
        Tags
        {
            // 透明物として扱う
            "RenderType"="Transparent"

            // 透明キューで描画する
            "Queue"="Transparent"

            // URP 用
            "RenderPipeline"="UniversalPipeline"
        }

        Pass
        {
            Name "ForwardSmoke"

            // 半透明合成
            Blend SrcAlpha OneMinusSrcAlpha

            // 両面描画
            Cull Off

            // 深度は書き込まない
            ZWrite Off

            HLSLPROGRAM

            // 頂点シェーダ関数
            #pragma vertex vert

            // ピクセルシェーダ関数
            #pragma fragment frag

            // URP の基本関数
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            // 計算本体
            #include "smoke.hlsl"

            ENDHLSL
        }
    }
}
