using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
public class ResultParry : SimpleAI
{
    [Header("数字スプライト（0〜9）を順番に設定")]
    public Sprite[] numberSprites; // 0〜9の画像を格納

    [Header("数字を並べる親オブジェクト")]
    public Transform scoreParent; // Horizontal Layout Groupが付いているオブジェクト


    private List<Image> digitImages = new List<Image>();
    private int currentScore;
   // public Texture2D texture; // Inspectorで設定
    // MonoBehaviourの生成後、最初のUpdateより前に1回だけ呼び出される
    void Start()
    {
        currentScore = PowerPoint + MassuruPont + YaaPoint + 10;
        UpdateScore(currentScore); // 初期表示
    }

    // 毎フレーム呼び出される
    void Update()
    {
        //if (Input.GetKeyDown(KeyCode.Space))
        //{
        //    UpdateScore(currentScore + 10);
        //}
    }



    public void UpdateScore(int newScore)
    {
        currentScore = Mathf.Max(0, newScore); // 負数防止
        string scoreStr = currentScore.ToString();

        // 桁数が足りない場合はImageを追加
        while (digitImages.Count < scoreStr.Length)
        {
            GameObject digitObj = new GameObject("Digit", typeof(Image));
            digitObj.transform.SetParent(scoreParent, false);
            Image img = digitObj.GetComponent<Image>();
            digitImages.Add(img);
        }

        // 各桁の画像を設定
        for (int i = 0; i < scoreStr.Length; i++)
        {
            int digit = scoreStr[i] - '0';
            digitImages[i].sprite = numberSprites[digit];
            digitImages[i].enabled = true;
        }

        // 余った桁は非表示
        for (int i = scoreStr.Length; i < digitImages.Count; i++)
        {
            digitImages[i].enabled = false;
        }
    }
   
}
