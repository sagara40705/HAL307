using UnityEngine;
using UnityEngine.UI;

public class ScoreSpriteDisplay : MonoBehaviour
{
    [Header("Imageを入れる")]
    [SerializeField] private Image[] digitImages;

    [Header("通常時の数字 0〜9")]
    [SerializeField] private Sprite[] normalSprites;

    [Header("フィーバー中の数字 0〜9")]
    [SerializeField] private Sprite[] feverSprites;

    [SerializeField] private bool hideLeadingZero = true;

    public void SetScore(int score, bool isFever = false)
    {
        Sprite[] sprites = isFever ? feverSprites : normalSprites;

        if (sprites == null || sprites.Length < 10)
        {
            Debug.LogWarning("数字スプライトが0〜9まで設定されてない", this);
            return;
        }

        string scoreText = Mathf.Max(0, score).ToString();

        int maxDigits = digitImages.Length;

        if (scoreText.Length > maxDigits)
        {
            scoreText = scoreText.Substring(scoreText.Length - maxDigits);
        }

        int emptyCount = maxDigits - scoreText.Length;

        for (int i = 0; i < maxDigits; i++)
        {
            Image image = digitImages[i];

            if (image == null) continue;

            if (i < emptyCount)
            {
                image.enabled = !hideLeadingZero;

                if (!hideLeadingZero)
                {
                    image.sprite = sprites[0];
                }

                continue;
            }

            image.enabled = true;

            int number = scoreText[i - emptyCount] - '0';
            image.sprite = sprites[number];
        }
    }
}
