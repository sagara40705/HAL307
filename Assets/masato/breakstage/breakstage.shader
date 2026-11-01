Shader "Custom/breakstage"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        // エンドレスの中央画面専用。既存マテリアルは両面描画のまま維持する。
        [Enum(UnityEngine.Rendering.CullMode)] _MonitorCull ("Monitor Cull", Float) = 0

        // 破壊する中心位置
        // UVなので 0～1
        // X=横位置 Y=縦位置
        _HoleCenter ("Break Center", Vector) = (0.5, 0.5, 0, 0)

        // 中心の崩れ穴の大きさ
        // 大きくしすぎると床が抜けたように見えるので小さめ推奨
        _HoleRadius ("Hole Radius", Range(0, 1)) = 0.18

        // 破壊範囲の横・縦サイズ
        // Xを大きくすると横に広がる
        // Yを大きくすると縦に広がる
        _BreakSize ("Break Size X/Y", Vector) = (0.22, 0.35, 0, 0)

        // 穴の境界のぼかし
        _HoleSoftness ("Hole Softness", Range(0.001, 0.2)) = 0.035

        // ヒビの本数
        _CrackCount ("Crack Count", Range(4, 80)) = 18

        // ヒビの細さ
        _CrackSharpness ("Crack Sharpness", Range(1, 60)) = 28

        // ヒビの長さ
        _CrackLength ("Crack Length", Range(0, 1)) = 0.7

        // ヒビの濃さ
        _CrackStrength ("Crack Strength", Range(0, 1)) = 0.85

        // 中心の黒い崩れ跡の強さ
        _EdgeDarkness ("Break Darkness", Range(0, 1)) = 0.85

        // 壊れ具合
        // 0 = 何もなし
        // 1 = 最大破壊
        _BreakAmount ("Break Amount", Range(0, 1)) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderType"="Transparent"
            "Queue"="Transparent"
            "IgnoreProjector"="True"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull [_MonitorCull]

        Pass
        {
            HLSLPROGRAM

            #pragma vertex BreakStageVertex
            #pragma fragment BreakStageFragment

            #include "UnityCG.cginc"
            #include "Assets/masato/breakstage/breakstage.hlsl"

            ENDHLSL
        }
    }
}
