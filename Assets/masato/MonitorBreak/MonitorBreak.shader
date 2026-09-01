Shader "Custom/MonitorBreak"
{
    Properties
    {
        // モニターに表示する映像テクスチャ
        // Render Texture をここに入れる
        [MainTexture] _BaseMap("Monitor Texture", 2D) = "white" {}

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

        //==========================================
        //
        //追加点
        //
        //==========================================

        _MainTex ("Texture", 2D) = "white" {}

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
            // URP用のShaderとして使う指定
            "RenderPipeline" = "UniversalPipeline"

            // 不透明なオブジェクトとして描画する
            "RenderType" = "Opaque"

            // 通常の3Dオブジェクトと同じタイミングで描画する
            "Queue" = "Geometry"

            //==================================
            //
            //追加点
            //
            //==================================

             "RenderType"="Transparent"
            "IgnoreProjector"="True"
        }

        //======================================
        //
        //追加点
        //
        //======================================

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

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
             #include "UnityCG.cginc"

            // モニター表現の本体
            // vert / frag / LED表現などは monitor.hlsl に書く
            #include "MonitorBreak.hlsl"

            //==============================================
            //
            //追加点
            //
            //===============================================

            #pragma vertex BreakStageVertex
            #pragma fragment BreakStageFragment

           
            #include "Assets/masato/breakstage/breakstage.hlsl"

            ENDHLSL
        }
    }
}
