using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FinalPoint : MonoBehaviour
{
    [Header("数字スプライト（0〜9）を順番に設定")]
    public Sprite[] numberSprites; // 0〜9の画像を格納

    [Header("数字を並べる親オブジェクト")]
    public Transform scoreParent; // Horizontal Layout Groupが付いているオブジェクト


    private List<Image> digitImages = new List<Image>();
    private int currentScore;
    // public Texture2D texture; // Inspectorで設定
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        currentScore = 0;//ここにプレイヤーの点数の変数を+として書く
        UpdateScore(currentScore); // 初期表示
    }

    // Update is called once per frame
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

