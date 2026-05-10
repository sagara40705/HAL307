using Pose;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Enemy
{
    public class EnemyPoseController : MonoBehaviour
    {
        // EnemyRegistry
        [SerializeField] private EnemyRegistry _enemyRegistry;

        // ポーズ中かどうか
        private bool _isPaused = false;

        // ポーズ中のEnemy
        private Enemy _pausedEnemy;

        // Update is called once per frame
        private void Update()
        {
            if (_isPaused) { return; }

            if (DebugShortcuts.Combo(Key.E, Key.P, Key.A) )
            {
                _pausedEnemy = _enemyRegistry.GetRandomEnemy();
                _isPaused = true;
                Debug.Log($"Paused enemy: {_pausedEnemy?.name ?? "None"}");
            }
        }

        // ポーズ状態を解除する
        public void Unpause()
        {
            _pausedEnemy = null;
            _isPaused = false;
        }
    }
}
