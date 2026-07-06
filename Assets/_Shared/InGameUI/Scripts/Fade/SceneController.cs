using UnityEngine;
using Cysharp.Threading.Tasks;

public class SceneController : MonoBehaviour
{
    private async void Start()
    {
        await UniTask.Yield(); // 1フレーム待ってUI初期化を安定させる
        await TransitionManager.FadeIn(0.5f);
    }
}
