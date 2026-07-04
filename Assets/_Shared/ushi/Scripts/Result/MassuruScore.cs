using UnityEngine;
using UnityEngine.UI;
namespace ushi.Sandbox
{
    public class MassuruScore : MonoBehaviour
    {
        public GameObject scoreObject = null; // Textオブジェクト
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            Text scoreText = scoreObject.GetComponent<Text>();
            scoreText.text = "マッスル乱入回数:";

        }

        // Update is called once per frame
        void Update()
        {
        
        }
    }
}
