using System.Collections;
using UnityEngine;

public class Enemy_SampleEffect : MonoBehaviour
{
    [SerializeField, Min(0f)] private float _incomingDuration = 1.0f;
    [SerializeField, Min(0f)] private float _incomingSpeed = 1.0f;
    [SerializeField, Min(0f)] private float _deadDuration = 1.0f;
    [SerializeField, Min(0f)] private float _deadSpeed = 1.0f;

    //　ウェーブ切り替え時の演出を開始する関数(Sample_Incoming)
    public void Start_SampleEffect_Incoming(Enemy.Enemy enemy)
    {
        StartMoveEffect(enemy, Vector3.up, _incomingDuration, _incomingSpeed, "Incoming");
    }

    //　敵が死亡した時の演出を開始する関数(Sample_Dead)
    public void Start_SampleEffect_Dead(Enemy.Enemy enemy)
    {
        StartMoveEffect(enemy, Vector3.down, _deadDuration, _deadSpeed, "Dead");
    }

    private void StartMoveEffect(Enemy.Enemy enemy, Vector3 direction, float duration, float speed, string effectName)
    {
        if (enemy == null)
        {
            Debug.LogWarning($"[Enemy_SampleEffect] Enemy is null. Effect={effectName}");
            return;
        }

        if (duration <= 0f || speed <= 0f)
        {
            return;
        }

        StartCoroutine(MoveEnemy(enemy.transform, direction, duration, speed));
    }

    private IEnumerator MoveEnemy(Transform target, Vector3 direction, float duration, float speed)
    {
        float elapsed = 0f;
        Vector3 normalizedDirection = direction.normalized;

        while (elapsed < duration)
        {
            if (target == null)
            {
                yield break;
            }

            float deltaTime = Time.deltaTime;
            float moveTime = Mathf.Min(deltaTime, duration - elapsed);
            target.position += normalizedDirection * speed * moveTime;
            elapsed += moveTime;
            yield return null;
        }
    }
}
