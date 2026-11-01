using UnityEngine;

namespace masato.Sandbox
{
    public class NumMove : MonoBehaviour
    {
        //数字の初期値
        [SerializeField] private Renderer targetRenderer;

        // 表示する数字
        [SerializeField] private int number = 0;

        private static readonly int Digit0Id = Shader.PropertyToID("_Digit0");
        private static readonly int Digit1Id = Shader.PropertyToID("_Digit1");
        private static readonly int Digit2Id = Shader.PropertyToID("_Digit2");
        //private static readonly int Digit3Id = Shader.PropertyToID("_Digit3");

        // --------------------------------------------------------
        // このオブジェクト専用の Material
        // --------------------------------------------------------
        private Material runtimeMaterial;

        // MonoBehaviourの生成後、最初のUpdateより前に1回だけ呼び出される
        void Start()
        {
            // --------------------------------------------------------
            // targetRenderer を Inspector で入れ忘れた時の保険
            //--------------------------------------------------------
            if (targetRenderer == null)
            {
                targetRenderer = GetComponent<Renderer>();
                Debug.LogError("NumMove: targetRenderer が設定されていません。");
                return;
            }

            // --------------------------------------------------------
            // Renderer 専用の Material を取得
            // --------------------------------------------------------
            runtimeMaterial = targetRenderer.material;

            // --------------------------------------------------------
            // ゲーム開始時点の number を shader に反映する
            // --------------------------------------------------------
            UpdateDigits();
        }

        // 毎フレーム呼び出される
        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Return))
            {
                // 数字を 1 増やす
                number++;

                // ----------------------------------------------------
                // 9999 を超えたら 0000 に戻す
                //
                // 4桁表示なので、10000 以上は表示しない前提
                // ----------------------------------------------------
                if (number > 9999)
                {
                    number = 9999;
                }

                // 増やした結果を shader に反映する
                UpdateDigits();
            }
        }

        private void UpdateDigits()
        {
            // --------------------------------------------------------
            // 例: number = 1234 のとき
            //
            // 千の位 = 1
            // 百の位 = 2
            // 十の位 = 3
            // 一の位 = 4
            //
            // というふうに、1つの整数を4桁に分ける
            // --------------------------------------------------------

            // 千の位
            int thousands = number / 100;

            // 百の位
            int hundreds = (number / 10) % 10;

            // 十の位
            int tens = number % 10;

            // 一の位
            //int ones = number % 10;

            // --------------------------------------------------------
            // shader に 1桁ずつ渡す
            //
            // 今回は
            // _Digit0 = 左端
            // _Digit1 = 左から2番目
            // _Digit2 = 左から3番目
            // _Digit3 = 右端
            //
            // という前提で渡している
            // --------------------------------------------------------
            runtimeMaterial.SetFloat(Digit0Id, thousands);
            runtimeMaterial.SetFloat(Digit1Id, hundreds);
            runtimeMaterial.SetFloat(Digit2Id, tens);
            //runtimeMaterial.SetFloat(Digit3Id, ones);
        }
    }
}
