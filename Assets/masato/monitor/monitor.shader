Shader "Custom/PlayerMuscle"
{
    Properties
    {
        // ベーステクスチャ
        [MainTexture] _BaseMap("Base Map", 2D) = "white" {}

        // 環境光の強さ
        _AmbientStrength("Ambient Strength", Range(0.0, 1.0)) = 0.10

        //モニターのぼかし
        _MonitorBlur("MonitorBlur", Range(0.0, 4.0)) = 0.2

        //枠の削り範囲 
        _MonitorCutting("MonitorCutting", Range(0.0, 4.0)) = 0.5

        //モニターの発光具合
        _Monitorlight("Monitorlight",Range(0.0,4.0)) = 0.8

    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
        }

        Pass
        {
            Name "PlayerMuscleForward"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM

            // 頂点シェーダ関数
            #pragma vertex vert

            // ピクセルシェーダ関数
            #pragma fragment frag

            // メインライトと影の対応
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fragment _ _SHADOWS_SOFT

            // HLSL本体を読み込む
            #include "PlayerMuscle.hlsl"

            ENDHLSL
        }
    }
}
