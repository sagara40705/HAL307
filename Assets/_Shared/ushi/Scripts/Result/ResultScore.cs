using UnityEngine;
using UnityEngine.UI;
namespace ushi.Sandbox
{
    public class ResultScore : SimpleAI
    {
        public GameObject scoreObject = null; // Textオブジェクト
        int Resultpoint;
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            //Simpleで関数で足した物を全部足す
            Resultpoint = Powerpoint + Massurupoint + Yaapoint;
            // Textコンポーネントを取得
        }

        // Update is called once per frame
        void Update()
        {
            Text scoreText = scoreObject.GetComponent<Text>();
            scoreText.text = "パリィ成功回数:"+ Resultpoint;
        }
    }
}
