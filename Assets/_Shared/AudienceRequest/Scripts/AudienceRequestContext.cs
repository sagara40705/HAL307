using UnityEngine;
using Player;
using Enemy;
using Context;

namespace AudienceRequest
{
    public class AudienceRequestContext : ContextBase
    {
        // Player
        [SerializeField] private string _playerSceneName;
        [SerializeField] private string _playerConextName;
        [SerializeField] private PlayerContext _playerContext;
        public PlayerContext GetPlayerContext()
        {
            if (OnlyMyScene) { return null; }
            _playerContext = FindContext<PlayerContext>(_playerSceneName, _playerConextName);
            return _playerContext;
        }

        // Enemy
        [SerializeField] private string _enemySceneName;
        [SerializeField] private string _enemyConextName;
        [SerializeField] private EnemyContext _enemyContext;
        public EnemyContext GetEnemyContext()
        {
            if (OnlyMyScene) { return null; }
            _enemyContext = FindContext<EnemyContext>(_enemySceneName, _enemyConextName);
            return _enemyContext;
        }

        // AudienceRequest

    }
}
