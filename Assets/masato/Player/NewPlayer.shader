Shader "Custom/PlayerMuscle"
{
    Properties
    {
        // ベーステクスチャ
        [MainTexture] _BaseMap("Base Map", 2D) = "white" {}

        // ノーマルマップ
        [NoScaleOffset] _NormalMap("Normal Map", 2D) = "bump" {}

        // ノーマルマップの効き具合
        _NormalStrength("Normal Strength", Range(0.0, 3.0)) = 1.0

        // 体の基本色
        [MainColor] _BaseColor("Base Color", Color) = (0.55, 0.42, 0.32, 1.0)

        // 黒光りの強さ
        // 0 = 暗くしない
        // 1 = 強く暗くする
        _Darkness("Darkness", Range(0.0, 1.0)) = 0.35

        // テカリの色
        _SpecColor("Specular Color", Color) = (1.0, 0.95, 0.85, 1.0)

        // テカリの強さ
        _SpecStrength("Specular Strength", Range(0.0, 4.0)) = 1.2

        // テカリの鋭さ
        // 小さいと広くぼんやり
        // 大きいと狭く強い
        _SpecPower("Specular Power", Range(1.0, 128.0)) = 48.0

        // 環境光の強さ
        _AmbientStrength("Ambient Strength", Range(0.0, 1.0)) = 0.10

        //数字のテクスチャ
        _NumTex("Number Texture", 2D) = "white" {}

        //数字の中身
        _Digit0("Digit 0", Range(0, 9)) = 0
        _Digit1("Digit 1", Range(0, 9)) = 0
        _Digit2("Digit 2", Range(0, 9)) = 0
        //_Digit3("Digit 3", Range(0, 9)) = 0

        //数字の色
        _DigitColor("Digit Color", Color) = (1, 0, 0, 1)

        //数字のUV範囲 (x:開始U, y:開始V, z:幅U, w:幅V)
        _DigitArea("Digit Area", Vector) = (0.3, 0.3, 0.2, 0.2)

        //数字のスケール (x:幅, y:高さ)
        _DigitScale("Digit Scale", Vector) = (1, 1, 0, 0)

        //数字を表示するかどうか
        [Toggle] _ShowNumber("Show Digits", Float) = 1


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
            #include "PlayerMuscle/PlayerMuscle.hlsl"

            ENDHLSL
        }
    }
}
