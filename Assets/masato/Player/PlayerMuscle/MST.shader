Shader "Custom/MST"
{
    Properties
    {
        // ベーステクスチャ
        [MainTexture] _BaseMap("Base Map", 2D) = "white" {}

        // ベースカラー
        [MainColor] _BaseColor("Base Color", Color) = (1,1,1,1)
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType"     = "Opaque"
            "Queue"          = "Geometry"
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM

            // 頂点シェーダ関数名
            #pragma vertex vert

            // ピクセルシェーダ関数名
            #pragma fragment frag

            // メインライト影対応
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fragment _ _SHADOWS_SOFT

            // HLSL本体を外部ファイルから読み込む
            #include "MyShader.hlsl"

            ENDHLSL
        }
    }
}