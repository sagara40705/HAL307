using AudienceRequest;
using Pose;
using UnityEngine;

public class TutorialDemoController : MonoBehaviour
{
    [Header("リクエスト関連")]
    [SerializeField] private AudienceRequestSystem requestSystem;

    [Header("チュートリアルで出すリクエスト")]
    [SerializeField] private PoseID tutorialRequestPose;

    [Header("リクエスト表示範囲")]
    [SerializeField] private Vector2 tutorialRequestAreaMin = new Vector2(100f, 100f);
    [SerializeField] private Vector2 tutorialRequestAreaMax = new Vector2(500f, 300f);

    [Header("フィーバーゲージ")]
    [SerializeField] private FeverGaugeController feverGaugeController;

    [Header("演出オブジェクト")]
    [SerializeField] private GameObject powerTextObject;
    [SerializeField] private GameObject parryEffectObject;

    [Header("スコア設定")]
    [SerializeField] private int tutorialScoreValue = 1;

    private void Awake()
    {
        if (requestSystem != null)
        {
            requestSystem.SetTutorialMode(true);
        }

        HideTemporaryObjects();
    }

    private void OnDestroy()
    {
        if (requestSystem != null)
        {
            requestSystem.SetTutorialMode(false);
        }
    }

    public void ApplyPageDemo(int pageIndex)
    {
        HideTemporaryObjects();
        ResetFeverGaugeIfNeeded(pageIndex);
        ResetScoreIfNeeded(pageIndex);
        switch (pageIndex)

        {
            case 0:
                // 1ページ目：リクエストを1個だけ表示
                ShowOnlyOneRequest();
                break;

            case 1:
                // 2ページ目：「パワー」と叫ばせたい
                ShowPowerText();
                break;

            case 2:
                // 3ページ目：プレイヤーがポーズ
                break;

            case 3:
                // 4ページ目：リクエストをハートに変える処理
                ShowOnlyOneRequest();
                ChangeRequestToHeart();
                break;

            case 4:
                // 5ページ目：スコア増加
                AddTutorialScore(tutorialScoreValue);
                break;

            case 5:
                // 6ページ目：フィーバーゲージをMAXにする
                MaxFeverGauge();
                break;

            case 6:
                // 7ページ目：フィーバーゲージMAX時のエネミー挙動説明
                // 説明だけなら処理なし
                break;

            case 7:
                // 8ページ目：敵がポーズ
                break;

            case 8:
                // 9ページ目：プレイヤーがポーズ
                break;

            case 9:
                // 10ページ目：パリィエフェクトを出す
                ShowParryEffect();
                break;

            default:
                break;
        }
    }

    private void HideTemporaryObjects()
    {
        if (powerTextObject != null)
        {
            powerTextObject.SetActive(false);
        }

        if (parryEffectObject != null)
        {
            parryEffectObject.SetActive(false);
        }
    }

    private void ShowOnlyOneRequest()
    {
        if (requestSystem == null)
        {
            return;
        }

        requestSystem.ClearAllTutorialRequests();

        requestSystem.ShowTutorialRequestInArea(
            tutorialRequestPose,
            tutorialRequestAreaMin,
            tutorialRequestAreaMax
        );
    }

    private void ShowPowerText()
    {

        if (powerTextObject != null)

        {

            powerTextObject.SetActive(true);

        }

    }

    private void ChangeRequestToHeart()
    {
        if (requestSystem == null)
        {
            return;
        }

        if (requestSystem.GetRequestCount(tutorialRequestPose) <= 0)
        {
            requestSystem.ShowTutorialRequestInArea(
                tutorialRequestPose,
                tutorialRequestAreaMin,
                tutorialRequestAreaMax
            );
        }

        // チュートリアル用：ハート演出だけ出して、スコア・フィーバーは増やさない
        requestSystem.ClearTutorialRequestWithoutReward(tutorialRequestPose, true);
    }

    private void AddTutorialScore(int value)
    {
        if (ScoreManager.Instance == null)
        {
            return;
        }

        ScoreManager.Instance.AddPlayerScore(value, false);
    }

    private void ResetScoreIfNeeded(int pageIndex)
    {
        if (ScoreManager.Instance == null)
        {
            return;
        }

        // 5ページ目だけスコア増加を見せたいのでリセットしない
        if (pageIndex == 4)
        {
            return;
        }

        ScoreManager.Instance.ResetScoresForTutorial();
    }

    private void MaxFeverGauge()
    {
        if (feverGaugeController == null)
        {
            return;
        }

        feverGaugeController.ForceFeverForTutorial();
    }

    private void ShowParryEffect()
    {
        if (parryEffectObject == null)
        {
            return;
        }

        parryEffectObject.SetActive(true);

        ParticleSystem particle =
            parryEffectObject.GetComponent<ParticleSystem>();

        if (particle == null)
        {
            particle =
                parryEffectObject.GetComponentInChildren<ParticleSystem>();
        }

        if (particle != null)
        {
            particle.Stop(
                true,
                ParticleSystemStopBehavior.StopEmittingAndClear
            );

            particle.Play(true);
        }
    }


    private void ResetFeverGaugeIfNeeded(int pageIndex)
    {
        if (feverGaugeController == null)
        {
            return;
        }

        // 6ページ目だけフィーバーMAXを見せたいのでリセットしない
        if (pageIndex == 5)
        {
            return;
        }

        feverGaugeController.ResetForTutorial();
    }
}
