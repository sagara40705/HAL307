Shader "Custom/LiveMonitor"
{
    Properties
    {
        // モニターに表示する映像テクスチャ
        // Render Texture をここに入れる
        [MainTexture] _BaseMap("Monitor Texture", 2D) = "white" {}
        [HideInInspector] _SidePanelLayout("Full frame on side panel atlas", Float) = 0

        // モニター全体の明るさ
        // 数値を上げるほどライブ会場のLED画面のように明るくなる
        _Monitorlight("Monitor Brightness", Range(0.0, 5.0)) = 1.8

        // 色の鮮やかさ
        // 1.0 が元の色、1.0より大きいと派手になる
        _Saturation("Saturation", Range(0.0, 2.0)) = 1.2

        // LEDの細かさ
        // 数値が大きいほどLEDの粒が細かくなる
        _LedCount("LED Count", Range(20.0, 400.0)) = 160.0

        // LEDの粒の大きさ
        // 小さいほど粒の隙間が目立つ
        _LedDotSize("LED Dot Size", Range(0.1, 0.9)) = 0.45

        // 暗い部分の底上げ
        // 0だと黒が強く、上げると全体が少し明るくなる
        _AmbientStrength("Ambient Strength", Range(0.0, 1.0)) = 0.05
    }

    SubShader
    {
        Tags
        {
            // URP用のShaderとして使う指定
            "RenderPipeline" = "UniversalPipeline"

            // 不透明なオブジェクトとして描画する
            "RenderType" = "Opaque"

            // 通常の3Dオブジェクトと同じタイミングで描画する
            "Queue" = "Geometry"
        }

        Pass
        {
            // Unity上で直接見ることは少ないが、整理用に付けている
            Name "LiveMonitorForward"

            // URPの通常描画パスとして使う指定
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM

            // 頂点シェーダーに vert 関数を使う
            // 頂点の位置やUVを処理する
            #pragma vertex vert

            // ピクセルシェーダーに frag 関数を使う
            // 実際の色を決める
            #pragma fragment frag

            // URPの基本関数を使うための読み込み
            // TransformObjectToHClip などが使えるようになる
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            // モニター表現の本体
            // vert / frag / LED表現などは monitor.hlsl に書く
            #include "monitor.hlsl"

            ENDHLSL
        }
    }
}
