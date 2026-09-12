Shader "Custom/MonitorBreak"
{
    Properties
    {
        // カメラ映像を受け取るRender Texture
        [MainTexture] _BaseMap("Monitor Texture", 2D) = "white" {}

        // モニター映像の調整
        _Monitorlight("Monitor Brightness", Range(0.0, 5.0)) = 1.8
        _Saturation("Saturation", Range(0.0, 2.0)) = 1.2
        _LedCount("LED Count", Range(20.0, 400.0)) = 160.0
        _LedDotSize("LED Dot Size", Range(0.1, 0.9)) = 0.45
        _AmbientStrength("Ambient Strength", Range(0.0, 1.0)) = 0.05

        // 破壊表現の設定
        _HoleCenter("Break Center", Vector) = (0.5, 0.5, 0, 0)
        _HoleRadius("Hole Radius", Range(0.0, 1.0)) = 0.18
        _BreakSize("Break Size X/Y", Vector) = (0.22, 0.35, 0, 0)
        _HoleSoftness("Hole Softness", Range(0.001, 0.2)) = 0.035
        _CrackCount("Crack Count", Range(4.0, 80.0)) = 18.0
        _CrackSharpness("Crack Sharpness", Range(1.0, 60.0)) = 28.0
        _CrackLength("Crack Length", Range(0.0, 1.0)) = 0.7
        _CrackStrength("Crack Strength", Range(0.0, 1.0)) = 0.85
        _EdgeDarkness("Break Darkness", Range(0.0, 1.0)) = 0.85

        // Inspectorではチェックボックスとして表示される。
        // OFF = 0、ON = 1としてHLSLへ渡す。
        [Toggle] _BreakAmount("Break Amount", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
        }

        // 穴の部分をアルファ0にして、後ろ側を見せるための設定。
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            Name "MonitorBreakForward"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM

            #pragma target 3.0
            #pragma vertex MonitorBreakVertex
            #pragma fragment MonitorBreakFragment

            // 座標変換、テクスチャ宣言用マクロなど、URPの基本機能を読み込む。
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            // モニター表現と破壊表現を統合した計算本体。
            #include "MonitorBreak.hlsl"

            ENDHLSL
        }
    }
}
