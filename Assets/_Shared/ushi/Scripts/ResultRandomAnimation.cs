using UnityEngine;

public class ResultRandomAnimation : MonoBehaviour
{
    [SerializeField] private string[] AnimationName;
    private int MaxWaveCount = 5;
    private int RandomAnimationCount = 3;
    [SerializeField] private Animator _animator;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
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

    // Update is called once per frame
    void Update()
    {
        
    }
}
