Shader "Custom/sweet"
{
    Properties
    {
    [MainColor] _BaseColor("Base Color", Color) = (1, 1, 1, 1)
    [MainTexture] _BaseMap("Base Map", 2D) = "white" {}

    _DropMask("Drop Mask", 2D) = "white" {}
    _UseDropTexture("Use Drop Texture", Range(0, 1)) = 0

    // 雫そのものの明るさ
    // 高いほど白く目立つ
    _DropStrength("Drop Strength", Range(0, 1)) = 0.2

    // 雫1粒の大きさ
    // Object座標版では Object Flow Scale と Drop Density の影響も受ける
    // 大きくしても変化が見えにくい時は、先に Density / Object Flow Scale を下げる
    _DropSize("Drop Size", Range(0.01, 0.3)) = 0.08

    // 雫の輪郭のぼかし
    // 小さいほど硬い粒、大きいほど柔らかい粒になる
    _DropEdgeSoftness("Drop Edge Softness", Range(0.001, 0.2)) = 0.02

    // 雫内部の白いハイライトの強さ
   [Toggle] _HighlightStrength("Highlight Strength", Range(0, 2)) = 0.8

    // 雫の縁・下側に入る影の強さ
    _ShadowStrength("Shadow Strength", Range(0, 1)) = 0.25

    // 雫の密度
    // 高いほど粒が増える
    // Object座標版では Object Flow Scale と掛け算で効く
    _DropDensity("Drop Density", Range(1, 30)) = 3

    // 雫をどれだけ間引くか
    // 0に近いほど多い、1に近いほど少ない
    _DropExistThreshold("Drop Exist Threshold", Range(0, 1)) = 0.55

    // 雫が下に流れる速さ
    // 0で停止
    _DropFlowSpeed("Drop Flow Speed", Range(0, 2)) = 0.2

    // Object座標を汗用の座標に変換する倍率
    // 高いほど全身に細かい格子状の粒が出やすい
    // まずは 0.2〜0.5 あたりで調整する
    _ObjectFlowScale("Object Flow Scale", Range(0.1, 10)) = 0.3
}

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "sweat.hlsl"

            ENDHLSL
        }
    }
}
