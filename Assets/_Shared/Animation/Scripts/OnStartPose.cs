using UnityEngine;

namespace Enemy
{
    public class EnemyStartPose : MonoBehaviour
    {
        private Animator _animator;

        private void Awake()
        {
            _animator = GetComponent<Animator>();
        }

        private void Start()
        {
            int randomPose = Random.Range(0, 5);

            _animator.SetInteger("StartPoseIndex", randomPose);
        }
    }
}
