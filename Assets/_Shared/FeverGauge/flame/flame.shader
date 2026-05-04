Shader "Custom/flame"
{
    Properties
    {
        // UI Image から渡されるスプライト
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}

        // リング本体の揺れに使うノイズ
        _NoiseTex ("Noise Texture", 2D) = "white" {}

        // 先端の炎シルエットに使うテクスチャ
        _FlameTex ("Flame Texture", 2D) = "black" {}

        // 全体色
        _Color ("Color", Color) = (1,1,1,1)

        // リングの内側半径
        _InnerRadius ("Inner Radius", Range(0, 0.5)) = 0.25

        // リングの外側半径
        _OuterRadius ("Outer Radius", Range(0, 0.5)) = 0.4

        // 0～1 の表示量
        _FillAmount ("Fill Amount", Range(0, 1)) = 1.0

        // 開始角度
        // 0 = 12時, 90 = 3時, 180 = 6時, 270 = 9時
        _StartAngle ("Start Angle", Range(0, 360)) = 0

        // リング本体の流れる速さ
        _FlameSpeed ("Flame Speed", Range(0, 5)) = 1.2

        // リング本体の炎模様の強さ
        _FlameStrength ("Flame Strength", Range(0, 2)) = 1.0

        // 先端炎の向き補正角度
        // 0 = 外向き
        _FlameAngleOffset ("Flame Angle Offset", Range(-180, 180)) = 0

        // 先端炎の長さ
        _TipLength ("Tip Length", Range(0.02, 0.35)) = 0.16

        // 先端炎の横幅
        _TipWidth ("Tip Width", Range(0.01, 0.25)) = 0.08

        // 先端炎の強さ
        _TipStrength ("Tip Strength", Range(0, 3)) = 1.4
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline"="UniversalPipeline"
            "Queue"="Transparent"
            "RenderType"="Transparent"
            "IgnoreProjector"="True"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off
        ZTest Always

        Pass
        {
            HLSLPROGRAM
            #pragma vertex FlameVertex
            #pragma fragment FlameFragment

            #include "UnityCG.cginc"
            #include "flame.hlsl"

            ENDHLSL
        }
    }
}
