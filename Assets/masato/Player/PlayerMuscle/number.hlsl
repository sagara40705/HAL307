#ifndef NUMBER_INCLUDED
#define NUMBER_INCLUDED

// ------------------------------------------------------------
// 数字テクスチャ
// ------------------------------------------------------------
TEXTURE2D(_NumTex);
SAMPLER(sampler_NumTex);


// ------------------------------------------------------------
// 元の色に数字を重ねる
// ------------------------------------------------------------
half3 ApplyNumber(half3 baseColor, float2 uv)
{
    // Show Digits が OFF の時は、そのまま返す
    if (_ShowNumber < 0.5)
    {
        return baseColor;
    }

    // 数字を表示する四角の左下
    float2 areaMin =
        _DigitArea.xy;

    // 数字を表示する四角の右上
    float2 areaMax =
        _DigitArea.xy + _DigitArea.zw;

    // 今描いているピクセルが数字エリア内か
    bool insideDigitArea =
        uv.x >= areaMin.x && uv.x <= areaMax.x &&
        uv.y >= areaMin.y && uv.y <= areaMax.y;

    // 数字エリアの中だけ描画
    if (insideDigitArea)
    {
        // 数字エリア内だけで使うローカルUV
        float2 localUV =
            (uv - areaMin) / _DigitArea.zw;

        // 数字画像とモデルUVの向きを合わせる
        localUV.x =
            1.0 - localUV.x;

        localUV.y =
            1.0 - localUV.y;

        // 数字の拡大縮小
        float2 centeredUV =
            localUV - 0.5;

        centeredUV.x /=
            _DigitScale.x;

        centeredUV.y /=
            _DigitScale.y;

        localUV =
            centeredUV + 0.5;

        // はみ出した部分は描かない
        if (localUV.x >= 0.0 && localUV.x <= 1.0 &&
            localUV.y >= 0.0 && localUV.y <= 1.0)
        {
            // 3桁のうち今どの桁か
            float digitIndex =
                floor(localUV.x * 3.0);

            // 今の1文字の中での横位置
            float digitLocalX =
                frac(localUV.x * 3.0);

            // 1文字分のUV
            float2 digitUV =
                float2(digitLocalX, localUV.y);

            // 今の桁で表示する数字
            float selectedDigit =
                0.0;

            if (digitIndex < 0.5)
            {
                selectedDigit =
                    _Digit0;
            }
            else if (digitIndex < 1.5)
            {
                selectedDigit =
                    _Digit1;
            }
            else if (digitIndex < 2.5)
            {
                selectedDigit =
                    _Digit2;
            }

            // 0〜9 が横並びの画像から使う数字を読む
            float2 numUV;

            numUV.x =
                (digitUV.x + selectedDigit) / 10.0;

            numUV.y =
                digitUV.y;

            // 数字画像を読む
            half4 numSample =
                SAMPLE_TEXTURE2D(_NumTex, sampler_NumTex, numUV);

            // 白い部分を数字のマスクとして使う
            half numMask =
                numSample.r;

            // 元の色に数字色を重ねる
            baseColor =
                lerp(baseColor, _DigitColor.rgb, numMask);
        }
    }

    return baseColor;
}

#endif
