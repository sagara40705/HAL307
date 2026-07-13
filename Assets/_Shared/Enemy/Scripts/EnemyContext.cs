using System;
using Context;
using Pose;
using UnityEngine;

namespace Enemy
{
    public class EnemyContext : ContextBase
    {
        [SerializeField] private EnemyManager _enemyManager;
        [SerializeField] private EnemyPoseController _enemyPoseController;

        public event Action<Enemy, PoseID> OnEnemyPoseStart;

        [SerializeField] private string _poseContextName;
        [SerializeField] private PoseContext _poseContext;

        private void Start()
        {
            ResolveExternalContexts();
        }

        public void NotifyEnemyPoseStart(Enemy enemy, PoseID poseID)
        {
            OnEnemyPoseStart?.Invoke(enemy, poseID);
        }

        public PoseContext GetPoseContext()
        {
            ResolveExternalContexts();
            return _poseContext;
        }

        public EnemyPoseController GetEnemyPoseController()
        {
            if (_enemyPoseController == null)
            {
                _enemyPoseController = FindFirstObjectByType<EnemyPoseController>();
            }

            return _enemyPoseController;
        }

        private void ResolveExternalContexts()
        {
            if (OnlyMyScene || _poseContext != null)
            {
                return;
            }

            _poseContext = FindContext<PoseContext>(_poseContextName);
        }
    }
}
