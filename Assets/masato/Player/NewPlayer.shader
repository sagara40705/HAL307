Shader "Custom/NewPlayer"
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

            // PlayerMuscle.hlsl の frag を別名にする
            #define frag PlayerMuscleBaseFrag
            #include "PlayerMuscle/PlayerMuscle.hlsl"
            #undef frag
            
            // 汗用の補助関数
            #include "NewPlayer.hlsl"
        
            // 新しい最終frag
            half4 frag(Varyings IN) : SV_Target
            {
                // まず元のPlayerMuscleを描画
                half4 color = PlayerMuscleBaseFrag(IN);

                float2 sweatCoord = IN.positionWS.xy * _ObjectFlowScale;

                // 上から下へ流す
                sweatCoord.y += _Time.y * _DropFlowSpeed;

                float sweat = GetSweatMask(
                    sweatCoord,
                    _DropDensity,
                    _DropExistThreshold,
                    _DropSize,
                    _DropEdgeSoftness
                );

                float highlight = GetSweatHighlight(
                    sweatCoord,
                    _DropDensity,
                    _DropSize
                );
                
                color.rgb += sweat * _DropStrength;
                color.rgb += highlight * sweat * _HighlightStrength;

                return color;

            }
            

            ENDHLSL
        }
    }
}
