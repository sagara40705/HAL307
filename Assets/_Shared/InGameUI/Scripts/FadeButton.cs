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
    }
}
