using UnityEngine;

public class ResultRandomAnimation : MonoBehaviour
{
    [SerializeField] private string[] AnimationName;
    private int MaxWaveCount = 5;
    private int RandomAnimationCount = 3;
    [SerializeField] private Animator _animator;

    // MonoBehaviourの生成後、最初のUpdateより前に1回だけ呼び出される
    void Start()
    {
        GameResultManager manager = GameResultManager.Instance;
        if (manager.ClearedWaveCount == MaxWaveCount)
        {
            _animator.Play(AnimationName[4]);
        }
        else
        {
            _animator.Play(AnimationName[Random.Range(0, RandomAnimationCount)]);
        }
    }

    // 毎フレーム呼び出される
    void Update()
    {
        
    }
}
