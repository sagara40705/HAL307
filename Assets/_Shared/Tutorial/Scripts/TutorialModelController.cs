using UnityEngine;

public class TutorialModelController : MonoBehaviour
{
    [Header("モデルのAnimator")]
    [SerializeField] private Animator playerAnimator;
    [SerializeField] private Animator enemyAnimator;

    [Header("アニメーション名")]
    [SerializeField] private string idleStateName = "Idle";
    [SerializeField] private string playerPoseStateName = "power";
    [SerializeField] private string enemyPoseStateName = "power";

    [Header("切り替え時間")]
    [SerializeField] private float fadeTime = 0.2f;

    public void PlayByPage(int pageIndex)
    {
        switch (pageIndex)
        {
            case 0:
                // 1ページ目：基本説明
                PlayIdle();
                break;

            case 1:
                // 2ページ目：「パワー」説明
                PlayIdle();
                break;

            case 2:
                // 3ページ目：プレイヤーがポーズ
                PlayPlayerPose();
                break;

            case 3:
                // 4ページ目：ハート化説明
                PlayIdle();
                break;

            case 4:
                // 5ページ目：スコア増加説明
                PlayIdle();
                break;

            case 5:
                // 6ページ目：フィーバーゲージMAX
                PlayIdle();
                break;

            case 6:
                // 7ページ目：フィーバー中の説明
                PlayIdle();
                break;

            case 7:
                // 8ページ目：敵がポーズ
                PlayEnemyPose();
                break;

            case 8:
                // 9ページ目：プレイヤーがポーズ
                PlayPlayerPose();
                break;

            case 9:
                // 10ページ目：パリィ
                PlayIdle();
                break;

            default:
                PlayIdle();
                break;
        }
    }

    private void PlayIdle()
    {
        Play(playerAnimator, idleStateName);
        Play(enemyAnimator, idleStateName);
    }

    private void PlayPlayerPose()
    {
        Play(playerAnimator, playerPoseStateName);
        Play(enemyAnimator, idleStateName);
    }

    private void PlayEnemyPose()
    {
        Play(playerAnimator, idleStateName);
        Play(enemyAnimator, enemyPoseStateName);
    }

    private void Play(Animator targetAnimator, string stateName)
    {
        if (targetAnimator == null)
        {
            return;
        }

        if (string.IsNullOrEmpty(stateName))
        {
            return;
        }

        targetAnimator.CrossFade(stateName, fadeTime);
    }
}
