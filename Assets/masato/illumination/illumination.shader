Shader "Custom/illumination"
{
    Properties
    {
        // ベースカラー
        [MainColor] _BaseColor("Base Color", Color) = (1, 1, 1, 1)

        // ベーステクスチャ
        [MainTexture] _BaseMap("Base Map", 2D) = "white" {}

        // 発光色
        _EmissionColor("Emission Color", Color) = (1, 1, 1, 1)

        // 発光の強さ
        _EmissionIntensity("Emission Intensity", Range(0, 5)) = 0
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
            #include "illumination.hlsl"

            ENDHLSL
        }
    }
}
