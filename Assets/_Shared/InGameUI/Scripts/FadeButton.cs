using UnityEngine;
using Cysharp.Threading.Tasks;

public class FadeButton : MonoBehaviour
{
    public void OnClickFade()
    {
        FadeAsync().Forget();
    }

    private async UniTask FadeAsync()
    {
        await TransitionManager.PlayDumbbellFadeOut();

        await UniTask.Delay(2000);

        await TransitionManager.FadeIn();

    }
}
